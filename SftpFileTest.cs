using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ConsoleApp1
{
    public class SSHCommandClass
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public int Delay { get; set; }
    }

    public class SftpFileTransferParameters
    {
        public bool IsUploadFileToTerminal = true;
        public string FullFileNameTerminal;
        public string FullFileNameComputer;
    }

    public class CheckSingleSftpClass
    {
        public string Description { get; set; }
        public SSHCommandClass SSHCmdBeforeDownload { get; set; }
        public SftpFileTransferParameters SftpParam { get; set; }
        public SSHCommandClass SSHCmdAfterDownload { get; set; }
    }

    public class CheckMultiFileSftpClass
    {
        public List<CheckSingleSftpClass> SftpList { get; set; }
    }

    public class TestSFTP
    {
        public static void TestSFTPMain(string[] args)
        {
            string str = """
                                {
                    "SftpList": [
                        {
                            "Description": "更新e9361app",
                            "SSHCmdBeforeDownload": {
                                "Command": "echo 0 > /sys/class/leds/wd_enable/brightness && /bin/ps |/bin/grep e9361app |/bin/grep -v /bin/grep |/usr/bin/awk '{print  $1}'|/usr/bin/xargs kill -9",
                                "Description": "杀死e9361app进程",
                                "Delay": 1000
                            },
                            "SftpParam": {
                                "IsUploadFileToTerminal": true,
                                "FullFileNameTerminal": "/home/sysadm/src/e9361app",
                                "FullFileNameComputer": "upload\\e9361app"
                            },
                            "SSHCmdAfterDownload": {
                                "Command": "chmod 777 /home/sysadm/src/e9361app",
                                "Description": "给e9361app赋予可执行权限",
                                "Delay": 1000
                            }
                        },
                        {
                            "Description": "更新printfore9361",
                            "SSHCmdBeforeDownload": null,
                            "SftpParam": {
                                "IsUploadFileToTerminal": true,
                                "FullFileNameTerminal": "/home/sysadm/src/printfore9361",
                                "FullFileNameComputer": "upload\\printfore9361"
                            },
                            "SSHCmdAfterDownload": {
                                "Command": "chmod 777 /home/sysadm/src/printfore9361",
                                "Description": "给printfore9361赋予可执行权限",
                                "Delay": 1000
                            }
                        },
                        {
                            "Description": "更新tlcpapp",
                            "SSHCmdBeforeDownload": {
                                "Command": "/bin/ps |/bin/grep tlcpapp |/bin/grep -v /bin/grep |/usr/bin/awk '{print  $1}'|/usr/bin/xargs kill -9",
                                "Description": "杀死tlcpapp进程",
                                "Delay": 1000
                            },
                            "SftpParam": {
                                "IsUploadFileToTerminal": true,
                                "FullFileNameTerminal": "/home/sysadm/src/tlcpapp",
                                "FullFileNameComputer": "upload\\tlcpapp"
                            },
                            "SSHCmdAfterDownload": {
                                "Command": "chmod 777 /home/sysadm/src/tlcpapp",
                                "Description": "给tlcpapp赋予可执行权限",
                                "Delay": 1000
                            }
                        },
                        {
                            "Description": "更新data_profile",
                            "SSHCmdBeforeDownload": {
                                "Command": "/bin/mv /etc/config/data_profile /etc/config/data_profile.bak",
                                "Description": "备份原data_profile",
                                "Delay": 1000
                            },
                            "SftpParam": {
                                "IsUploadFileToTerminal": true,
                                "FullFileNameTerminal": "/etc/config/data_profile",
                                "FullFileNameComputer": "upload\\data_profile"
                            },
                            "SSHCmdAfterDownload": {
                                "Command": "chmod 777 /etc/config/data_profile",
                                "Description": "给data_profile赋予最高读写权限",
                                "Delay": 1000
                            }
                        },
                        {
                            "Description": "更新huawei_iot",
                            "SSHCmdBeforeDownload": {
                                "Command": "/bin/ps |/bin/grep huawei_iot |/bin/grep -v /bin/grep |/usr/bin/awk '{print  $1}'|/usr/bin/xargs kill -9",
                                "Description": "杀死huawei_iot进程",
                                "Delay": 1000
                            },
                            "SftpParam": {
                                "IsUploadFileToTerminal": true,
                                "FullFileNameTerminal": "/home/sysadm/src/huawei_iot",
                                "FullFileNameComputer": "upload\\huawei_iot"
                            },
                            "SSHCmdAfterDownload": {
                                "Command": "chmod 777 /home/sysadm/src/huawei_iot",
                                "Description": "给huawei_iot赋予可执行权限",
                                "Delay": 1000
                            }
                        },
                        {
                            "Description": "更新防火墙配置文件firewall",
                            "SSHCmdBeforeDownload": {
                                "Command": "/bin/mv /etc/config/firewall /etc/config/firewall.bak",
                                "Description": "备份原firewall",
                                "Delay": 1000
                            },
                            "SftpParam": {
                                "IsUploadFileToTerminal": true,
                                "FullFileNameTerminal": "/etc/config/firewall",
                                "FullFileNameComputer": "upload\\firewall"
                            },
                            "SSHCmdAfterDownload": {
                                "Command": "chmod 777 /etc/config/firewall",
                                "Description": "给firewall赋予最高读写权限",
                                "Delay": 1000
                            }
                        }
                    ]
                }
                """;

            CheckMultiFileSftpClass? para = JsonConvert.DeserializeObject<CheckMultiFileSftpClass>(str);
            JObject root = JObject.FromObject(para);
            Console.WriteLine(root.ToString());
            Console.ReadLine();
        }
    }
}