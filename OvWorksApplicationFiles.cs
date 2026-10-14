using System;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>애플리케이션이 사용하는 파일의 절대 경로.</summary>
    internal static class OvWorksApplicationFiles
    {
        private const string IntegrityRegistryPath = @"Software\OVWorks\OVWorksClient\Integrity";
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
            get { return Path.Combine(ApplicationDataDirectory, "Security"); }
        }

        public static string ApplicationDataDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OVWorks", "OVWorksClient");
            }
        }

        public static string IntegrityDatabasePath
        {
            get
            {
                return Path.Combine(ApplicationDataDirectory, "Integrity",
                    "integrity-" + OvWorksProductInfo.DetailVersion + ".db");
            }
        }

        public static string ExecutablePath
        {
            get { return Application.ExecutablePath; }
        }

        public static bool IsIntegrityDatabaseProvisioned
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(IntegrityRegistryPath))
                {
                    return key != null && string.Equals(
                        key.GetValue(OvWorksProductInfo.DetailVersion) as string,
                        "provisioned", StringComparison.Ordinal);
                }
            }
        }

        public static void MarkIntegrityDatabaseProvisioned()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(IntegrityRegistryPath))
            {
                key.SetValue(OvWorksProductInfo.DetailVersion, "provisioned", RegistryValueKind.String);
            }
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
