using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using ClosedXML.Excel;

internal class JsonToExcel
{
    private static int JsonToExcelMain(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet run -- <input.json> <output.xlsx>");
            return 1;
        }

        var inputPath = args[0];
        var outputPath = args[1];

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Input file not found: {inputPath}");
            return 2;
        }

        var jsonText = File.ReadAllText(inputPath);
        using var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        var arrays = new List<(string path, JsonElement array)>();
        FindArrays(root, "$", arrays);

        if (arrays.Count == 0)
        {
            Console.WriteLine("No arrays found in JSON.");
            return 0;
        }

        using var wb = new XLWorkbook();
        foreach (var (path, array) in arrays)
        {
            var sheetName = SanitizeSheetName(path);
            var ws = wb.Worksheets.Add(sheetName);

            if (array.ValueKind != JsonValueKind.Array)
                continue;

            var items = array.EnumerateArray().ToList();
            if (items.Count == 0)
            {
                ws.Cell(1, 1).Value = "(empty array)";
                continue;
            }

            // If items are objects, collect all keys (flattened)
            var allKeys = new HashSet<string>();
            var flattenedItems = new List<Dictionary<string, string>>();

            foreach (var item in items)
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var dict = new Dictionary<string, string>();
                    FlattenObject(item, "", dict);
                    foreach (var k in dict.Keys) allKeys.Add(k);
                    flattenedItems.Add(dict);
                }
                else
                {
                    // non-object -> single "Value" column
                    allKeys.Add("Value");
                    var dict = new Dictionary<string, string>
                    {
                        ["Value"] = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString()
                    };
                    flattenedItems.Add(dict);
                }
            }

            var headers = allKeys.ToList();
            headers.Sort(StringComparer.Ordinal);

            // write header
            for (int c = 0; c < headers.Count; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            // write rows
            for (int r = 0; r < flattenedItems.Count; r++)
            {
                var dict = flattenedItems[r];
                for (int c = 0; c < headers.Count; c++)
                {
                    dict.TryGetValue(headers[c], out var v);
                    ws.Cell(r + 2, c + 1).Value = v ?? "";
                }
            }

            // adjust columns
            ws.Columns().AdjustToContents();
        }

        wb.SaveAs(outputPath);
        Console.WriteLine($"Exported {arrays.Count} array(s) to {outputPath}");
        return 0;
    }

    private static void FindArrays(JsonElement el, string path, List<(string, JsonElement)> outList)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Array:
                outList.Add((path, el));
                // also search inside array elements in case nested arrays exist deeper
                foreach (var item in el.EnumerateArray())
                    FindArrays(item, path + "[]", outList);
                break;

            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                    FindArrays(prop.Value, path == "$" ? prop.Name : path + "." + prop.Name, outList);
                break;

            default:
                break;
        }
    }

    private static void FlattenObject(JsonElement el, string prefix, Dictionary<string, string> outDict)
    {
        if (el.ValueKind != JsonValueKind.Object)
        {
            outDict[prefix.TrimEnd('.')] = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
            return;
        }

        foreach (var prop in el.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? prop.Name : prefix + prop.Name;
            var v = prop.Value;
            switch (v.ValueKind)
            {
                case JsonValueKind.Object:
                    FlattenObject(v, key + ".", outDict);
                    break;

                case JsonValueKind.Array:
                    // put array as JSON string
                    outDict[key] = v.GetRawText();
                    break;

                case JsonValueKind.String:
                    outDict[key] = v.GetString();
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    outDict[key] = v.ToString();
                    break;

                default:
                    outDict[key] = v.ToString();
                    break;
            }
        }
    }

    private static string SanitizeSheetName(string path)
    {
        var name = path.Replace("$", "root").Replace("..", ".").Replace("[]", "_array");
        var invalid = new[] { '\\', '/', '*', '[', ']', ':', '?', '\'', '\"' };
        foreach (var c in invalid) name = name.Replace(c, '_');
        if (name.Length > 31) name = name.Substring(0, 31);
        if (string.IsNullOrWhiteSpace(name)) name = "Sheet1";
        return name;
    }
}