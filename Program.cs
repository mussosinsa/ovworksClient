using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    internal static class Program
    {
        private static int abnormalTerminationRecorded;

        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                RecordAbnormalTermination(e.Exception);
                Environment.Exit(1);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                RecordAbnormalTermination(e.ExceptionObject as Exception);
            };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 로그인 화면을 만들기 전에 실행 파일과 관련 파일을 암호화 DB 기준으로 검증한다.
            // 실패한 프로세스에서는 어떤 로그인/네트워크 동작도 시작하지 않는다.
            var integrity = new OvWorksHash();
            if (!integrity.integrityCheck(false, false))
            {
                MessageBox.Show(
                    "애플리케이션 또는 관련 파일의 무결성 검사에 실패했습니다.\r\n" +
                    "안전을 위해 프로그램을 종료합니다.\r\n\r\n" + integrity.LastFailureDetail,
                    "무결성 검사 실패",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop);
                return;
            }

            Application.Run(new OvWorksLoginForm());
        }

        private static void RecordAbnormalTermination(Exception exception)
        {
            if (Interlocked.Exchange(ref abnormalTerminationRecorded, 1) != 0) return;
            try
            {
                string detail = exception == null ? "unknown error" :
                    exception.GetType().Name + ": " + exception.Message;
                OvWorksAuditQueue.Enqueue(
                    "OV-Works Client terminated abnormally at " +
                    DateTime.UtcNow.ToString("o") + ". " + detail, "error");
            }
            catch
            {
                // 종료 처리 중에는 감사 파일 기록 실패가 원래 예외를 덮어쓰면 안 된다.
            }
        }
    }
}
