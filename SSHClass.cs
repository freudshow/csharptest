using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Renci.SshNet;
using Renci.SshNet.Async;
using Renci.SshNet.Common;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    public static class SSHClass
    {
        public static async Task<ShellStream> CreateShell(string ip, string user, string pass, string terminalType)
        {
            using (var client = new SshClient(ip, user, pass))
            {
                Dictionary<TerminalModes, uint> terminalMode = new Dictionary<TerminalModes, uint>();
                terminalMode.Add(TerminalModes.ECHO, 0);
                return client.CreateShellStream(terminalType, 65536, 65536, 65536, 65536, 262144, terminalMode);
            }
        }

        public static async Task<string> ExecCmdAsync(string ip, int port, string user, string pass, string cmd, int timeoutSeconds)
        {
            try
            {
                PasswordConnectionInfo info = new PasswordConnectionInfo(ip, port, user, pass);
                info.Timeout = new TimeSpan(0, 0, timeoutSeconds);

                using (SshClient client = new SshClient(info))
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        await client.ConnectAsync(cts.Token);
                    }

                    using (SshCommand x = client.RunCommand(cmd))
                    {
                        return x.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<string> ExecCmdWithResultAsync(string ip, int port, string user, string pass, string cmd, int timeoutSeconds)
        {
            try
            {
                PasswordConnectionInfo info = new PasswordConnectionInfo(ip, port, user, pass);
                info.Timeout = new TimeSpan(0, 0, timeoutSeconds);

                using (SshClient client = new SshClient(info))
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        await client.ConnectAsync(cts.Token);
                    }

                    // 创建带 PTY 的 Shell（和 WindTerm 一样）
                    // 禁用远端回显（TerminalModes.ECHO = 0）并使用唯一标记来提取命令输出，
                    // 这样可以避免返回命令本身以及后续的 exit 命令输出。
                    var terminalModes = new Dictionary<TerminalModes, uint>
                    {
                        { TerminalModes.ECHO, 0 }
                    };

                    var stream = client.CreateShellStream(
                        "xterm",
                        80,
                        24,
                        0,
                        0,
                        1024,
                        terminalModes
                    );

                    var writer = new StreamWriter(stream) { AutoFlush = true };
                    var reader = new StreamReader(stream);
                    // 执行命令：先打印起始标记，执行命令，再打印结束标记，最后退出。
                    // 通过标记提取命令的标准输出，避免包含命令回显和 exit 相关输出。
                    writer.WriteLine(cmd);
                    // 退出shell
                    writer.WriteLine("exit");

                    // 等待有数据可读
                    while (!stream.DataAvailable)
                    {
                        await Task.Delay(50);
                    }

                    // 读取结果（可能包含提示、命令回显等）
                    string result = await reader.ReadToEndAsync();

                    // 清理 ANSI 转义序列
                    result = Regex.Replace(result, "\u001B\\[[0-?]*[ -/]*[@-~]", "");
                    // 清理作业控制信息如 [1] 1234
                    result = Regex.Replace(result, @"\[\d+\]\s*\d+", "");
                    // 清理命令提示符 (user@host:~# 或 [user@host ~]# 等格式)
                    result = Regex.Replace(result, @"(?:\[[^\]]+\]|[^@\s:\[\]]+@[^\s:]+:[^\s]+)[#\$]\s*", "",
                                           RegexOptions.Multiline);

                    // 按行分割，过滤空行、命令本身、logout、exit 和 shell 错误信息
                    var lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.None)
                                    .Select(l => l.Trim())
                                    .Where(l => !string.IsNullOrEmpty(l))
                                    .Where(l => !string.Equals(l, cmd.Trim(), System.StringComparison.Ordinal))
                                    .Where(l => !l.Equals("logout", StringComparison.OrdinalIgnoreCase))
                                    .Where(l => !l.Equals("exit", StringComparison.OrdinalIgnoreCase))
                                    .Where(l => !l.StartsWith("-sh:", StringComparison.Ordinal))
                                    .ToArray();

                    return string.Join("\n", lines).Trim();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task SSHMainAsync(string[] args)
        {
            string ip = "192.168.195.183";
            int port = 22;
            string user = "floyd";
            string pass = "a";
            // string cmd = "/home/sysadm/src/e9361app >/dev/null 2>&1 &";

            //string cmd = "ls -al /etc/4g";

            while (true)
            {
                try
                {
                    Console.Write($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}]>");
                    string? cmd = Console.ReadLine();
                    if (string.IsNullOrEmpty(cmd))
                    {
                        continue;
                    }

                    if (cmd == "exit")
                    {
                        break;
                    }

                    string result = await ExecCmdAsync(ip, port, user, pass, cmd, 2);
                    Console.Write($"{result}\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"执行命令时发生错误: {ex.Message}");
                }
            }
        }
    }
}