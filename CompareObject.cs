using System.Reflection;

namespace ConsoleApp1
{
    public static class ObjectComparer
    {
        // 通用值比较方法（支持任意引用类型）
        public static bool AreValuesEqual<T>(T obj1, T obj2) where T : class
        {
            // 1. 处理 null 情况
            if (obj1 == null && obj2 == null)
            {
                return true;
            }

            if (obj1 == null || obj2 == null)
            {
                return false;
            }

            // 2. 类型判断
            if (obj1.GetType() != obj2.GetType())
            {
                return false;
            }

            // 3. 反射获取所有公共属性（可改为 BindingFlags.NonPublic 包含私有成员）
            PropertyInfo[] properties = obj1.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in properties)
            {
                // 获取属性值
                object? value1 = prop.GetValue(obj1);
                object? value2 = prop.GetValue(obj2);

                // 递归比较引用类型（值类型直接比较）
                if (!AreValuesEqualRecursive(value1, value2))
                {
                    return false;
                }
            }

            // 4. 反射获取所有公共字段（可选，根据需求添加）
            FieldInfo[] fields = obj1.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                object? value1 = field.GetValue(obj1);
                object? value2 = field.GetValue(obj2);
                if (!AreValuesEqualRecursive(value1, value2))
                {
                    return false;
                }
            }

            return true;
        }

        // 递归比较（处理嵌套引用类型）
        private static bool AreValuesEqualRecursive(object? value1, object? value2)
        {
            // 1. 处理 null 情况
            if (value1 == null && value2 == null)
            {
                return true;
            }

            if (value1 == null || value2 == null)
            {
                return false;
            }

            // 2. 处理值类型（包括结构体、枚举）
            Type type = value1.GetType();
            if (type.IsValueType)
            {
                return value1.Equals(value2);
            }

            // 3. 处理字符串（特殊引用类型，直接比较值）
            if (type == typeof(string))
            {
                return string.Equals((string)value1, (string)value2);
            }

            // 4. 处理嵌套引用类型（递归调用）
            return AreValuesEqual(value1, value2);
        }
    }

    // 使用
    public class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public Address? Address { get; set; } // 嵌套引用类型
    }

    public class Address
    {
        public string? City { get; set; }
        public string? Street { get; set; }
    }

    public static class AddressComparer
    {
        public static void Main(string[] args)
        {
            // 测试
            Person p1 = new()
            {
                Name = "张三",
                Age = 20,
                Address = new Address { City = "北京", Street = "朝阳路" }
            };

            Person p2 = new()
            {
                Name = "张三",
                Age = 20,
                Address = new Address { City = "北京", Street = "朝阳路" }
            };

            Console.WriteLine(ObjectComparer.AreValuesEqual(p1, p2)); // True（支持嵌套对象比较）
            Console.ReadLine();
        }
    }
}