using Newtonsoft.Json.Linq;
using System;
using System.IO;

public class JsonPrinter
{
    public static void PrintJsonKeyValuePairs(string filePath)
    {
        try
        {
            string jsonText = File.ReadAllText(filePath);
            JObject jsonObject = JObject.Parse(jsonText);

            // 使用缩进级别 0 开始打印
            PrintKeyValuePairsRecursive(jsonObject, 0);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"文件未找到：{filePath}");
        }
        catch (Newtonsoft.Json.JsonReaderException ex)
        {
            Console.WriteLine($"JSON 解析错误：{ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误：{ex.Message}");
        }
    }

    private static void PrintKeyValuePairsRecursive(JObject jsonObject, int indentLevel)
    {
        // 计算缩进字符串
        string indent = new string('\t', indentLevel); // 每次缩进 2 个空格

        foreach (var property in jsonObject.Properties())
        {
            if (property.Value is JObject nestedObject)
            {
                Console.WriteLine($"{indent}{property.Name}: ");
                PrintKeyValuePairsRecursive(nestedObject, indentLevel + 1); // 递归调用，增加缩进级别
                Console.WriteLine();
            }
            else if (property.Value is JArray array)
            {
                Console.WriteLine($"{indent}{property.Name}: ");
                foreach (var item in array)
                {
                    if (item is JObject itemObject)
                    {
                        PrintKeyValuePairsRecursive(itemObject, indentLevel + 1);
                    }
                    else
                    {
                        Console.WriteLine($"{new string(' ', (indentLevel + 1) * 2)}Value: {item}");
                    }

                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine($"{indent}{property.Name}: {property.Value}");
            }
        }
    }

    public static void printJsonFile(string[] args)
    {
        string jsonFilePath = "c0711.json";
        if (args.Length > 0)
        {
            jsonFilePath = args[0];
        }
        PrintJsonKeyValuePairs(jsonFilePath);
        Console.ReadKey();
    }
}