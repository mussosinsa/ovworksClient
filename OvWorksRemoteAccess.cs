using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksRemoteAccess
    {
        public string[] portToBlocks;//"8080";
        public string[] ruleNames;// = "Block_TCP_" + portToBlock;
        public OvWorksRemoteAccess() 
        {
            portToBlocks = new string[] { "6568", "3389", "5900" };
            ruleNames = new string[] { "anydesk", "mstsc", "vncviewer" };
        }
        //BlockPort(ruleName, portToBlock, "in");
        public async Task BlockPort(string ruleName, string port, string direction)
        {
            try
            {
                // netsh advfirewall firewall add rule 명령어 조합
                // dir=in (inbound), action=block (block), protocol=TCP
                string arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir={direction} action=block protocol=TCP localport={port}";

                ProcessStartInfo procInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    //UseShellExecute = true,         // 관리자 권한 승인 
                    //Verb = "runas",                 // 관리자 권한
                    CreateNoWindow = true,          
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process process = Process.Start(procInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {                        
                    }
                    else
                    {                        
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"오류 발생: {ex.Message}");
                //Console.WriteLine("주의: 관리자 권한이 거부되었거나 올바르지 않은 접근입니다.");
            }
        }
        //DeletePortRule(ruleName);
        public async Task DeletePortRule(string ruleName)
        {
            string arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"";
            ProcessStartInfo procInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                //UseShellExecute = true,
                //Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (Process process = Process.Start(procInfo))
            {
                process.WaitForExit();
                //Console.WriteLine($"[삭제 완료] 방화벽 규칙 '{ruleName}'이(가) 제거되었습니다.");
            }
        }
        //CheckInboundRuleByNetsh(ruleName)
        public async Task<bool> CheckInboundRuleByNetsh(string ruleName)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 규칙이 없으면 "지정한 조건을 만족하는 규칙을 찾지 못했습니다." 등의 메시지가 반환됨
                if (output.Contains(ruleName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
