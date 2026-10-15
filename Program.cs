using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess; // 프로젝트 참조 추가 필요
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            /*
            // RDP 세션인지 확인
            if (SystemInformation.TerminalServerSession)
            {
                //MessageBox.Show("프로그램을 실행할 수 없습니다.", "실행 제한", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MessageBox.Show($"사용할 수 없는 프로그램입니다.\r\n", "경고 메시지", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                return; // 프로그램 즉시 종료
            }
            */
            /*
            string serviceName = "TermService"; // 원격 데스크톱 서비스 이름

            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    // 서비스가 실행 중이면 중지
                    if (sc.Status == ServiceControllerStatus.Running ||
                        sc.Status == ServiceControllerStatus.StartPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        //Console.WriteLine("원격 데스크톱 서비스(TermService)를 중지했습니다.");
                        MessageBox.Show($"사용할 수 없는 프로그램입니다.\r\n", "경고 메시지", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    }
                    else
                    {
                        //Console.WriteLine("서비스가 이미 중지되어 있습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"서비스 제어 중 오류 발생 (관리자 권한 필요): {ex.Message}");
            }
            */
            /*
            string keyPath = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
            // 0 = 허용, 1 = 차단
            int value = 1;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath, writable: true))
            {
                if (key != null)
                {
                    key.SetValue("fDenyTSConnections", value, RegistryValueKind.DWord);
                }
            }
            */
            /*
            using (var sc = new ServiceController("TermService"))// ;
            {
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
            }
            
            // 시작 유형을 "사용 안 함"으로 변경 (재부팅 후에도 자동 시작 안 되게)
            
            using (var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\TermService", writable: true))// ;
            key.SetValue("Start", 4, RegistryValueKind.DWord); // 4 = Disabled
            */
            //System.Diagnostics.Process.Start("mstsc.exe", "/v:192.168.50.54:3389");
            Application.Run(new OvWorksLoginForm());
        }
    }
}
