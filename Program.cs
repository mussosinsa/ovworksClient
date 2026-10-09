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
