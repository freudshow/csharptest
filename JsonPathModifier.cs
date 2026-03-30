using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ConsoleApp1
{
    public class DevTypeC0WRItemClass
    {
        /// <summary>
        /// 从哪个zip文件中获取配置文件
        /// </summary>
        public string FromZipFile { get; set; }

        /// <summary>
        /// Zip文件解压到哪个路径下
        /// </summary>
        public string ZipExtractToPath { get; set; }

        /// <summary>
        /// 配置文件的名称, 包含后缀, 如devtype.json
        /// </summary>
        public string ConfigFileName { get; set; }

        /// <summary>
        /// 要修改的参数名称,
        /// 如 UseJson, GprsCardMode
        /// </summary>
        public string AttributeName { get; set; }

        /// <summary>
        /// 将参数修改的目标值, 如1, 0等
        /// </summary>
        public object AttributeValue { get; set; }

        public string Description { get; set; }
    }

    public class ModifyDevTypeClass
    {
        public List<DevTypeC0WRItemClass> ModifyList { get; set; }
    }

    /// <summary>
    /// JSON路径修改器 - 支持通过路径表达式修改JSON中的属性值
    /// 路径格式: attr0.attr1.attr2[3].attr4
    /// </summary>
    public static class JsonPathModifier
    {
        /// <summary>
        /// 根据路径表达式修改JSON对象中的属性值
        /// </summary>
        /// <param name="root">JSON根对象</param>
        /// <param name="attributeName">属性路径，如 "UseJson" 或 "atr0.attr1.att2[3].attr4"</param>
        /// <param name="attributeValue">要设置的目标值</param>
        /// <returns>是否修改成功</returns>
        public static bool SetValueByPath(JObject root, string attributeName, object attributeValue)
        {
            if (root == null || string.IsNullOrWhiteSpace(attributeName))
                return false;

            // 解析路径
            var pathSegments = ParsePath(attributeName);
            if (pathSegments.Count == 0)
                return false;

            // 遍历路径，找到目标位置的父对象
            JToken current = root;
            for (int i = 0; i < pathSegments.Count - 1; i++)
            {
                var segment = pathSegments[i];
                current = NavigateToSegment(current, segment);
                if (current == null)
                    return false;
            }

            // 设置最终值
            var lastSegment = pathSegments.Last();
            return SetValueAtSegment(current, lastSegment, attributeValue);
        }

        /// <summary>
        /// 解析路径字符串为路径段列表
        /// </summary>
        private static List<PathSegment> ParsePath(string path)
        {
            var segments = new List<PathSegment>();
            // 匹配: 属性名 或 属性名[索引]
            var pattern = @"([^\.\[\]]+)(?:\[(\d+)\])?";
            var matches = Regex.Matches(path, pattern);

            foreach (Match match in matches)
            {
                string propertyName = match.Groups[1].Value;
                int? arrayIndex = null;

                if (match.Groups[2].Success)
                {
                    arrayIndex = int.Parse(match.Groups[2].Value);
                }

                segments.Add(new PathSegment
                {
                    PropertyName = propertyName,
                    ArrayIndex = arrayIndex
                });
            }

            return segments;
        }

        /// <summary>
        /// 导航到指定的路径段
        /// </summary>
        private static JToken NavigateToSegment(JToken current, PathSegment segment)
        {
            // 获取属性
            JToken next = null;

            if (current is JObject obj)
            {
                if (!obj.TryGetValue(segment.PropertyName, out next))
                    return null;
            }
            else if (current is JArray arr)
            {
                // 如果当前是数组，尝试按索引访问
                return null; // 路径中间不应该直接是数组
            }
            else
            {
                return null;
            }

            // 如果有数组索引，获取数组元素
            if (segment.ArrayIndex.HasValue)
            {
                if (next is JArray array && segment.ArrayIndex.Value < array.Count)
                {
                    return array[segment.ArrayIndex.Value];
                }
                return null;
            }

            return next;
        }

        /// <summary>
        /// 在指定路径段设置值
        /// </summary>
        private static bool SetValueAtSegment(JToken current, PathSegment segment, object value)
        {
            JToken target = null;

            if (current is JObject obj)
            {
                if (!obj.TryGetValue(segment.PropertyName, out target))
                    return false;
            }
            else if (current is JArray arr && segment.ArrayIndex.HasValue)
            {
                if (segment.ArrayIndex.Value < arr.Count)
                {
                    target = arr[segment.ArrayIndex.Value];
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            // 如果有数组索引，获取数组元素
            if (segment.ArrayIndex.HasValue && target is JArray array)
            {
                if (segment.ArrayIndex.Value >= array.Count)
                    return false;
                target = array[segment.ArrayIndex.Value];
            }

            if (target == null)
                return false;

            // 转换值为JToken并设置
            JToken newValue = ConvertToJToken(value);

            // 根据目标类型替换值
            if (target.Parent is JObject parentObj)
            {
                // 找到目标在父对象中的属性名
                var property = target.Parent.Children<JProperty>()
                    .FirstOrDefault(p => p.Value == target);
                if (property != null)
                {
                    property.Value = newValue;
                    return true;
                }
            }
            else if (target.Parent is JArray parentArr)
            {
                int index = parentArr.IndexOf(target);
                if (index >= 0)
                {
                    parentArr[index] = newValue;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 将object转换为JToken
        /// </summary>
        private static JToken ConvertToJToken(object value)
        {
            if (value == null)
                return JValue.CreateNull();

            // 已经是JToken
            if (value is JToken token)
                return token;

            // 基本类型
            if (value is string str)
                return new JValue(str);
            if (value is int intVal)
                return new JValue(intVal);
            if (value is long longVal)
                return new JValue(longVal);
            if (value is bool boolVal)
                return new JValue(boolVal);
            if (value is double doubleVal)
                return new JValue(doubleVal);
            if (value is float floatVal)
                return new JValue(floatVal);
            if (value is decimal decimalVal)
                return new JValue(decimalVal);
            if (value is DateTime dateVal)
                return new JValue(dateVal);

            // 其他类型尝试序列化
            return JToken.FromObject(value);
        }

        /// <summary>
        /// 路径段定义
        /// </summary>
        private class PathSegment
        {
            /// <summary>
            /// 属性名
            /// </summary>
            public string PropertyName { get; set; }

            /// <summary>
            /// 数组索引（如果有）
            /// </summary>
            public int? ArrayIndex { get; set; }

            public override string ToString()
            {
                return ArrayIndex.HasValue
                    ? $"{PropertyName}[{ArrayIndex.Value}]"
                    : PropertyName;
            }
        }
    }

    public class JsonPathModifierExample
    {
        /// <summary>
        /// 修改设备类型配置的示例方法
        /// </summary>
        public void ModifyDevTypeConfigExample()
        {
            try
            {
                // 示例1: 简单的属性修改
                var item1 = new DevTypeC0WRItemClass
                {
                    AttributeName = "UseJson",
                    AttributeValue = true
                };

                // 示例2: 嵌套属性修改
                var item2 = new DevTypeC0WRItemClass
                {
                    AttributeName = "Network.Timeout",
                    AttributeValue = 30
                };

                // 示例3: 数组元素属性修改
                var item3 = new DevTypeC0WRItemClass
                {
                    AttributeName = "Channels[0].BaudRate",
                    AttributeValue = 9600
                };

                // 示例4: 深层嵌套+数组
                var item4 = new DevTypeC0WRItemClass
                {
                    AttributeName = "atr0.attr1.att2[3].attr4",
                    AttributeValue = "new value"
                };

                // 加载JSON配置文件
                string jsonContent = @"{
                                            ""MainDevType"": 6,
                                            ""DevType"": 23,
                                            ""SubDevEnum"": 23,
                                            ""ParameterTable"": 42,
                                            ""RtdbTable"": 41,
                                            ""UseJson"": false,
                                            ""GprsCardMode"": 1,
                                            ""UpCommPort"": 111,
                                            ""UpLedMode"": 0,
                                            ""DownCommPort"": 2,
                                            ""DownLedMode"": 0,
                                            ""CanWriteFiles"": 0,
                                            ""GprsCardMode_APN2"": 1,
                                            ""Gprs_APN2_Enable"": 1,
                                            ""Gprs_APN1_Enable"": 1,
                                            ""Network"": 100,
                                            ""Channels"": [
                                                {
                                                    ""BaudRate"": 2400
                                                },
                                                {
                                                    ""BaudRate"": 9600
                                                }
                                            ],
                                            ""atr0"": {
                                                ""attr1"": {
                                                    ""att2"": [
                                                        {
                                                            ""attr4"": ""first value""
                                                        },
                                                        {
                                                            ""attr4"": ""second value""
                                                        },
                                                        {
                                                            ""attr4"": ""third value""
                                                        },
                                                        {
                                                            ""attr4"": ""fourth value""
                                                        },
                                                        {
                                                            ""attr4"": ""fifth value""
                                                        },
                                                        {
                                                            ""attr4"": ""senventh value""
                                                        }
                                                    ]
                                                }
                                            }
                                        }";
                JObject root = JObject.Parse(jsonContent);

                // 应用修改
                bool success1 = JsonPathModifier.SetValueByPath(root, item1.AttributeName, item1.AttributeValue);
                bool success2 = JsonPathModifier.SetValueByPath(root, item2.AttributeName, item2.AttributeValue);
                bool success3 = JsonPathModifier.SetValueByPath(root, item3.AttributeName, item3.AttributeValue);
                bool success4 = JsonPathModifier.SetValueByPath(root, item4.AttributeName, item4.AttributeValue);

                // 保存修改后的配置
                File.WriteAllText("devtype_modified.json", root.ToString(Formatting.Indented));
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 批量修改配置
        /// </summary>
        public void BatchModifyConfig(ModifyDevTypeClass modifyConfig, string configFilePath)
        {
            // 读取原始配置
            string jsonContent = File.ReadAllText(configFilePath);
            JObject root = JObject.Parse(jsonContent);

            // 遍历所有修改项
            foreach (var item in modifyConfig.ModifyList)
            {
                bool success = JsonPathModifier.SetValueByPath(
                    root,
                    item.AttributeName,
                    item.AttributeValue
                );

                if (!success)
                {
                    Console.WriteLine($"修改失败: {item.AttributeName}");
                }
            }

            // 保存修改后的配置
            File.WriteAllText(configFilePath, root.ToString(Formatting.Indented));
        }

        private static void Main(string[] args)
        {
            try
            {
                var example = new JsonPathModifierExample();
                example.ModifyDevTypeConfigExample();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常: {ex.Message}");
            }

            Console.ReadLine();
        }
    }
}