using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace ConsoleApp1
{
    public static class InterfaceReflectHelper
    {
        /// <summary>
        /// 从【当前执行程序集】获取所有实现目标接口的可实例化类
        /// </summary>
        /// <typeparam name="TInterface">目标接口</typeparam>
        /// <returns>符合条件的类型列表</returns>
        public static List<Type> GetImplementTypesInCurrentAssembly<TInterface>()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            return GetImplementTypesFromAssembly<TInterface>(asm);
        }

        /// <summary>
        /// 从【整个应用所有已加载程序集】获取所有实现接口的类
        /// </summary>
        /// <typeparam name="TInterface">目标接口</typeparam>
        /// <returns>符合条件的类型列表</returns>
        public static List<Type> GetImplementTypesInAllAssembly<TInterface>()
        {
            List<Type> result = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in assemblies)
            {
                try
                {
                    var types = GetImplementTypesFromAssembly<TInterface>(asm);
                    result.AddRange(types);
                }
                catch
                {
                    // 跳过加载异常的程序集
                }
            }

            return result;
        }

        /// <summary>
        /// 从指定程序集筛选实现接口的可实例化类
        /// </summary>
        private static List<Type> GetImplementTypesFromAssembly<TInterface>(Assembly assembly)
        {
            Type interfaceType = typeof(TInterface);

            return assembly.GetTypes()
                .Where(t =>
                    // 不是接口
                    !t.IsInterface &&
                    // 不是抽象类
                    !t.IsAbstract &&
                    // 实现了目标接口
                    interfaceType.IsAssignableFrom(t) &&
                    // 有无参构造函数，可以直接实例化
                    t.GetConstructor(Type.EmptyTypes) != null
                )
                .ToList();
        }

        /// <summary>
        /// 自动创建所有实现类的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <returns>实例列表</returns>
        public static List<TInterface> CreateAllInstance<TInterface>()
        {
            var types = GetImplementTypesInAllAssembly<TInterface>();
            List<TInterface> list = new List<TInterface>();

            foreach (var type in types)
            {
                var obj = Activator.CreateInstance(type);
                if (obj != null)
                {
                    list.Add((TInterface)obj);
                }
            }

            return list;
        }
    }

    public interface ITestInterface
    {
        int ID { get; set; }
        string Name { get; }
    }

    public class TestInterface : ITestInterface
    {
        public int ID { get; set; } = 0;

        public string Name { get; } = "test0";
    }

    public class TestInterface2 : ITestInterface
    {
        public int ID { get; set; } = 1;

        public string Name { get; } = "test1";
    }

    public class TestInterface3 : ITestInterface
    {
        public int ID { get; set; } = 2;
        public string Name { get; set; } = "test2";
    }

    public class GetClassesImplementInterface
    {
        public static void InterfaceMain(string[] args)
        {
            List<Type> classes = InterfaceReflectHelper.GetImplementTypesInCurrentAssembly<ITestInterface>();

            foreach (var type in classes)
            {
                Console.WriteLine($"type name:{type.Name}");
            }

            var imp = InterfaceReflectHelper.CreateAllInstance<ITestInterface>();
            foreach (var i in imp)
            {
                Console.WriteLine($"ID: {i.ID}, Name:{i.Name}, class type: {i.GetType()}");
            }

            Console.ReadLine();
        }
    }
}