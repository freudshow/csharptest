using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System.Text.Json;

public class JsonToExcelConverter
{
    public static void ConvertJsonToExcel(string jsonFilePath, string excelFilePath)
    {
        try
        {
            // 读取 JSON 文件
            string jsonText = File.ReadAllText(jsonFilePath);
            JObject jsonObject = JObject.Parse(jsonText);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            // 创建 Excel 工作簿
            using (ExcelPackage excelPackage = new())
            {
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Sheet1");
                ConvertJsonToExcelRecursive(jsonObject, worksheet);
                // 保存 Excel 文件
                FileInfo excelFile = new FileInfo(excelFilePath);
                excelPackage.SaveAs(excelFile);
            }

            Console.WriteLine($"JSON 文件已成功转换为 Excel 文件：{excelFilePath}");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"文件未找到：{jsonFilePath}");
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

    public static void ConvertJsonToExcelRecursive(JObject jsonObject, ExcelWorksheet worksheet)
    {
        int row = 1;
        foreach (var property in jsonObject.Properties())
        {
            string key = property.Name;
            JToken value = property.Value;

            // 在 Excel 中创建列标题
            worksheet.Cells[1, row].Value = key;

            if (value is JArray array)
            {
                int arrayRow = 2; // 从第二行开始写入数组元素
                foreach (var item in array)
                {
                    worksheet.Cells[arrayRow, row].Value = item.ToString();
                    arrayRow++;
                }
            }
            else if (value is JObject obj)
            {
                //处理对象类型，将对象的属性展开到同一列
                int objRow = 2;
                foreach (var objProperty in obj.Properties())
                {
                    worksheet.Cells[objRow, row].Value = $"{objProperty.Name}:{objProperty.Value}";
                    objRow++;
                }
            }
            else
            {
                worksheet.Cells[2, row].Value = value.ToString();
            }

            row++;
        }

        //自动调整列宽
        worksheet.Cells.AutoFitColumns();
    }

    public static void Main(string[] args)
    {
        string jsonFilePath = "c0711.json"; // 替换为您的 JSON 文件路径
        string excelFilePath = "output.xlsx"; // 输出 Excel 文件路径
        if (args.Length > 0)
        {
            jsonFilePath = args[0];
            if (args.Length > 1)
            {
                excelFilePath = args[1];
            }
        }

        ConvertJsonToExcel(jsonFilePath, excelFilePath);
        Console.ReadKey();
    }
}