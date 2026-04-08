using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class GitHashCode
    {
        public void GitMain(string[] args)
        {
            // 方式1：从程序集元数据读
            var gitHash = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "GitCommit")?.Value ?? "unknown";

            // 方式2：直接用编译常量（更高效）
            //string gitHash2 = GIT_COMMIT;
            Console.WriteLine($"当前版本 Git 提交：{gitHash}");
            Console.ReadLine();
        }
    }
}