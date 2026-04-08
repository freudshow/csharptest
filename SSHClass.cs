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

        public static async Task<string> ExecCmdAsync(string ip, string user, string pass, string cmd, int timeoutSeconds)
        {
            try
            {
                using (var client = new SshClient(ip, user, pass))
                {
                    client.Connect();

                    var terminalModes = new Dictionary<TerminalModes, uint>
                    {
                        { TerminalModes.ECHO, 0 }
                    };

                    var stream = client.CreateShellStream("dumb", 80, 24, 0, 0, 1024, terminalModes);

                    var writer = new StreamWriter(stream) { AutoFlush = true };
                    var reader = new StreamReader(stream);

                    var startMarker = "__CMD_START_9f3b6a__";
                    var endMarker = "__CMD_END_9f3b6a__";

                    writer.WriteLine($"echo '{startMarker}'");
                    writer.WriteLine(cmd);
                    writer.WriteLine($"echo '{endMarker}'");
                    writer.WriteLine("exit");

                    while (!stream.DataAvailable)
                    {
                        await Task.Delay(50);
                    }

                    string result = await reader.ReadToEndAsync();

                    var startIdx = result.IndexOf(startMarker);
                    if (startIdx >= 0)
                    {
                        var afterStart = result.IndexOf('\n', startIdx);
                        var contentStart = afterStart >= 0 ? afterStart + 1 : startIdx + startMarker.Length;
                        var endIdx = result.IndexOf(endMarker, contentStart);
                        if (endIdx >= 0 && endIdx > contentStart)
                        {
                            var output = result.Substring(contentStart, endIdx - contentStart);

                            output = Regex.Replace(output, "\u001B\\[[0-?]*[ -/]*[@-~]", "");
                            output = Regex.Replace(output, @"\[\d+\]\s*\d+", "");
                            output = Regex.Replace(output, @"(?:\[[^\]]+\]|[^@\s:\[\]]+@[^\s:]+:[^\s]+)[#\$]\s*", "", RegexOptions.Multiline);

                            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.None)
                                              .Select(l => l.Trim())
                                              .Where(l => !string.IsNullOrEmpty(l))
                                              .Where(l => !string.Equals(l, cmd.Trim(), System.StringComparison.Ordinal))
                                              .ToArray();

                            var cleaned = string.Join("\n", lines).Trim();
                            stream.Close();
                            client.Disconnect();
                            return cleaned;
                        }
                    }

                    stream.Close();
                    client.Disconnect();
                    return string.Empty;
                }
            }
            catch (Exception)
            {
                throw;
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
                        "dumb",
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

                    // 退出shell
                    writer.WriteLine("exit");

                    // 等待有数据可读
                    while (!stream.DataAvailable)
                    {
                        await Task.Delay(50);
                    }

                    // 读取结果（可能包含提示、命令回显等）
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task Main(string[] args)
        {
            string ip = "192.168.0.232";
            int port = 22;
            string user = "root";
            string pass = "123456";
            string cmd = "/home/sysadm/src/e9361app >/dev/null 2>&1 &";

            //string cmd = "ls -al /etc/4g";

            try
            {
                string result = await ExecCmdWithResultAsync(ip, port, user, pass, cmd, 2);
                Console.WriteLine($"命令执行结果: [{result}]");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行命令时发生错误: {ex.Message}");
                Console.ReadLine();
            }
        }
    }
}