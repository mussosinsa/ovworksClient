using System;
using System.IO;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>애플리케이션이 사용하는 파일의 절대 경로.</summary>
    internal static class OvWorksApplicationFiles
    {
        private const string IntegrityHashFileName =
            "d04b98f48e8f8bcc15c6ae5ac050801cd6dcfd428fb5f9e65c4e16e7807340fa.fot";

        public static string PublicKeyPath
        {
            get { return GetSecurityFilePath("public_key.pem"); }
        }

        public static string CaCertificatePath
        {
            get { return GetSecurityFilePath("ca.crt"); }
        }

        public static string SecurityFilesDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OVWorks",
                    "OVWorksClient",
                    "Security");
            }
        }

        public static string IntegrityHashPath
        {
            get { return Path.Combine(Application.StartupPath, IntegrityHashFileName); }
        }

        public static string ExecutablePath
        {
            get { return Application.ExecutablePath; }
        }

        private static string GetSecurityFilePath(string fileName)
        {
            // 기존 배포 파일이 있으면 계속 사용한다. Program Files는 일반 사용자에게
            // 쓰기 권한이 없으므로 새로 받는 파일은 사용자별 LocalAppData에 저장한다.
            string installedPath = Path.Combine(Application.StartupPath, fileName);
            if (File.Exists(installedPath))
            {
                return installedPath;
            }

            return Path.Combine(SecurityFilesDirectory, fileName);
        }
    }
}
