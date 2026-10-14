using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>서버에 보내지 못한 감사 알림을 사용자 프로필에 영구 보관한다.</summary>
    internal static class OvWorksAuditQueue
    {
        private static readonly SemaphoreSlim FlushLock = new SemaphoreSlim(1, 1);

        private static string QueueDirectory
        {
            get { return Path.Combine(OvWorksApplicationFiles.ApplicationDataDirectory, "PendingAudit"); }
        }

        public static void Enqueue(string description, string severity)
        {
            Directory.CreateDirectory(QueueDirectory);
            string escapedDescription = SecurityElement.Escape(description ?? string.Empty);
            string escapedSeverity = SecurityElement.Escape(severity ?? "warning");
            long customId = DateTime.UtcNow.Ticks % 2000000000L;
            string xml = "<event>" +
                "<description>" + escapedDescription + "</description>" +
                "<severity>" + escapedSeverity + "</severity>" +
                "<origin>OV-Works Client 4.5.1</origin>" +
                "<custom_id>" + customId + "</custom_id>" +
                "</event>";

            string finalPath = Path.Combine(QueueDirectory,
                DateTime.UtcNow.ToString("yyyyMMddHHmmssfffffff") + "-" + Guid.NewGuid().ToString("N") + ".xml");
            string temporaryPath = finalPath + ".tmp";
            File.WriteAllText(temporaryPath, xml);
            File.Move(temporaryPath, finalPath);
        }

        public static async Task FlushAsync(Func<string, Task<bool>> sender)
        {
            await FlushLock.WaitAsync();
            try
            {
                if (!Directory.Exists(QueueDirectory)) return;
                string[] files = Directory.GetFiles(QueueDirectory, "*.xml");
                Array.Sort(files, StringComparer.Ordinal);
                foreach (string file in files)
                {
                    string xml;
                    try { xml = File.ReadAllText(file); }
                    catch (IOException) { continue; }

                    if (!await sender(xml)) break;
                    try { File.Delete(file); }
                    catch (IOException) { }
                }
            }
            finally
            {
                FlushLock.Release();
            }
        }
    }
}
