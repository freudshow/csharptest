using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public static class OpenWrtIfconfigParser
    {
        /// <summary>
        /// 核心：精确匹配网卡名后的 inet addr
        /// </summary>
        public static string ExtractIPForAdapter(string text, string adapterName)
        {
            // 正则：匹配 网卡名行 + 后续行（直到下一个网卡或文本结束）中的 inet addr:IP
            // 使用 (?s) 启用单行模式，. 匹配换行符
            // adapterName 前匹配 0 或多个字符、数字或空白字符
            string pattern = $@"(?s)[\s\S]*?{adapterName}\b.*?(?=(?:^|\n)[A-Za-z]|\z)";
            Match adapterMatch = Regex.Match(text, pattern, RegexOptions.Multiline);

            if (!adapterMatch.Success)
                return null;

            // 在匹配到的网卡块中查找 inet addr
            string adapterBlock = adapterMatch.Value;
            string ipPattern = @"inet addr:(\d+\.\d+\.\d+\.\d+)";
            Match ipMatch = Regex.Match(adapterBlock, ipPattern);

            return ipMatch.Success ? ipMatch.Groups[1].Value : null;
        }

        /// <summary>
        /// 获取指定网卡的完整配置信息
        /// </summary>
        public static string GetAdapterInfo(string text, string adapterName)
        {
            string pattern = $@"(?s)[\s\S]*?{adapterName}\b.*?(?=(?:^|\n)[A-Za-z]|\z)";
            Match adapterMatch = Regex.Match(text, pattern, RegexOptions.Multiline);

            return adapterMatch.Success ? adapterMatch.Value.Trim() : null;
        }

        public static void ExtractMain(string[] args)
        {
            string ifconfigOutput = @"
br-lan    Link encap:Ethernet  HWaddr 00:C8:A4:EC:35:E9
          inet addr:192.168.0.232  Bcast:192.168.0.255  Mask:255.255.255.0
          inet6 addr: fe80::2c8:a4ff:feec:35e9/64 Scope:Link
          UP BROADCAST RUNNING MULTICAST  MTU:1500  Metric:1
          RX packets:39819 errors:0 dropped:0 overruns:0 frame:0
          TX packets:7370 errors:0 dropped:0 overruns:0 carrier:0
          collisions:0 txqueuelen:1000
          RX bytes:55269516 (52.7 MiB)  TX bytes:1139191 (1.0 MiB)

ccinet0   Link encap:UNSPEC  HWaddr 00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
          inet addr:40.116.1.110  Mask:255.255.255.0
          UP RUNNING NOARP  MTU:1500  Metric:1
          RX packets:0 errors:0 dropped:0 overruns:0 frame:0
          TX packets:0 errors:0 dropped:0 overruns:0 carrier:0
          collisions:0 txqueuelen:1000
          RX bytes:0 (0.0 B)  TX bytes:0 (0.0 B)

ccinet1   Link encap:UNSPEC  HWaddr 00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
          inet addr:10.15.201.188  Mask:255.255.255.0
          UP RUNNING NOARP  MTU:1500  Metric:1
          RX packets:140 errors:0 dropped:0 overruns:0 frame:0
          TX packets:169 errors:0 dropped:0 overruns:0 carrier:0
          collisions:0 txqueuelen:1000
          RX bytes:6052 (5.9 KiB)  TX bytes:12752 (12.4 KiB)

eth0      Link encap:Ethernet  HWaddr 00:C8:A4:EC:35:E9
          UP BROADCAST RUNNING MULTICAST  MTU:1500  Metric:1
          RX packets:46386 errors:0 dropped:13 overruns:0 frame:0
          TX packets:7377 errors:0 dropped:0 overruns:0 carrier:0
          collisions:0 txqueuelen:1000
          RX bytes:55547979 (52.9 MiB)  TX bytes:1140027 (1.0 MiB)
          Interrupt:26 Base address:0x1800

lo        Link encap:Local Loopback
          inet addr:127.0.0.1  Mask:255.0.0.0
          inet6 addr: ::1/128 Scope:Host
          UP LOOPBACK RUNNING  MTU:65536  Metric:1
          RX packets:0 errors:0 dropped:0 overruns:0 frame:0
          TX packets:0 errors:0 dropped:0 overruns:0 carrier:0
          collisions:0 txqueuelen:1000
          RX bytes:0 (0.0 B)  TX bytes:0 (0.0 B)

usbnet0   Link encap:Ethernet  HWaddr AE:D2:21:E3:9B:B6
          UP BROADCAST MULTICAST  MTU:1500  Metric:1
          RX packets:0 errors:0 dropped:0 overruns:0 frame:0
          TX packets:0 errors:0 dropped:0 overruns:0 carrier:0
          collisions:0 txqueuelen:1000
          RX bytes:0 (0.0 B)  TX bytes:0 (0.0 B)
                    ";

            var ip0 = ExtractIPForAdapter(ifconfigOutput, "ccinet0");
            var ip1 = ExtractIPForAdapter(ifconfigOutput, "ccinet1");
            var ip2 = ExtractIPForAdapter(ifconfigOutput, "br-lan");
            var eth0 = ExtractIPForAdapter(ifconfigOutput, "eth0");
            var lo = ExtractIPForAdapter(ifconfigOutput, "lo");

            Console.WriteLine("ccinet0 IP: " + ip0);
            Console.WriteLine("ccinet1 IP: " + ip1);
            Console.WriteLine("ccinet1 IP: " + ip2);
            Console.WriteLine("eth0 IP: " + eth0);
            Console.WriteLine("lo IP: " + lo);

            Console.WriteLine();
            Console.WriteLine("=== eth0 信息 ===");
            var eth0Info = GetAdapterInfo(ifconfigOutput, "eth0");
            Console.WriteLine(eth0Info ?? "未找到");

            Console.WriteLine();
            Console.WriteLine("=== lo 信息 ===");
            var loInfo = GetAdapterInfo(ifconfigOutput, "lo");
            Console.WriteLine(loInfo ?? "未找到");

            Console.WriteLine();
            Console.WriteLine("=== ccinet0 信息 ===");
            Console.WriteLine(GetAdapterInfo(ifconfigOutput, "ccinet0") ?? "未找到");

            Console.WriteLine();
            Console.WriteLine("=== ccinet1 信息 ===");
            Console.WriteLine(GetAdapterInfo(ifconfigOutput, "ccinet1") ?? "未找到");

            Console.WriteLine();
            Console.WriteLine("=== br-lan 信息 ===");
            Console.WriteLine(GetAdapterInfo(ifconfigOutput, "br-lan") ?? "未找到");

            string result = @"

BusyBox v1.36.1 (2025-08-27 07:29:03 UTC) built-in shell (ash)

  _______                     ________        __
 |       |.-----.-----.-----.|  |  |  |.----.|  |_
 |   -   ||  _  |  -__|     ||  |  |  ||   _||   _|
 |_______||   __|_____|__|__||________||__|  |____|
          |__| W I R E L E S S   F R E E D O M
 -----------------------------------------------------
 OpenWrt 24.10-SNAPSHOT, r0-996dd43
 -----------------------------------------------------
OW24.10_asr1803a7600_rls1536_1.057.081_20250827_07_58_bld29_SDK_FOR1828
-----------------------------------------------------
root@OpenWrt:~# ccinet0   Link encap:UNSPEC  HWaddr 00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
          inet addr:40.116.1.110  Mask:255.255.255.0
          UP RUNNING NOARP  MTU:1500  Metric:1
          RX packets:0 errors:0 dropped:0 overruns:0 frame:0
          TX packets:0 errors:0 dropped:0 overruns:0 carrier:0
          collisions:0 txqueuelen:1000
          RX bytes:0 (0.0 B)  TX bytes:0 (0.0 B)

root@OpenWrt:~# ";

            var ip3 = ExtractIPForAdapter(result, "ccinet0");
            Console.WriteLine("从 result 中提取 ccinet0 IP: " + ip3);
            Console.ReadLine();
        }
    }
}