using System.IO;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>설치된 실행 파일과 함께 배포되는 보안 파일의 절대 경로.</summary>
    internal static class OvWorksApplicationFiles
    {
        private const string IntegrityHashFileName =
            "d04b98f48e8f8bcc15c6ae5ac050801cd6dcfd428fb5f9e65c4e16e7807340fa.fot";

        public static string PublicKeyPath
        {
            get { return Path.Combine(Application.StartupPath, "public_key.pem"); }
        }

        public static string CaCertificatePath
        {
            get { return Path.Combine(Application.StartupPath, "ca.crt"); }
        }

        public static string IntegrityHashPath
        {
            get { return Path.Combine(Application.StartupPath, IntegrityHashFileName); }
        }

        public static string ExecutablePath
        {
            get { return Application.ExecutablePath; }
        }
    }
}
