using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.OpenSsl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    public class integritySW
    {
        private static bool _expired;
        public static bool Expired
        {
            get
            {
                // Reads are usually simple
                return _expired;
            }
            set
            {
                // You can add logic here for race conditions,
                // or other measurements
                _expired = value;
            }
        }
        // Perhaps extend this to have Read-Modify-Write static methods
        // for data integrity during concurrency? Situational.
    }
    internal class OvWorksHash
    {        
        public SHA512 Sha512 = SHA512.Create();
        public string hashFileName;
        public string targetFileName;        
        public byte[] hash512Value;
        public string hashStatus;
        public string LastFailureDetail { get; private set; }

        public OvWorksHash()
        {
            hash512Value = null;
            hashFileName = OvWorksApplicationFiles.IntegrityDatabasePath;
            targetFileName = OvWorksApplicationFiles.ExecutablePath;
            hashStatus = "normal"; // normal(Success), warning, error(Failure), alert
            LastFailureDetail = string.Empty;
        }
        // Compute the file's hash.
        public byte[] GetHashSha512(string filename)
        {
            using (FileStream stream = File.OpenRead(filename))
            {
                return Sha512.ComputeHash(stream);
            }
        }
        // Return a byte array as a sequence of hex values.
        public string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
        }
        // Hash File Reader
        public byte[] hashFileReader(string filename)
        {
            byte[] bytes = File.ReadAllBytes(filename);
            if (bytes.Length != 64)
            {
                throw new InvalidDataException("무결성 해시 파일의 크기는 64바이트여야 합니다.");
            }
            return bytes;
        }

        public bool integrityCheck(bool showResult = true, bool includeSecurityFiles = true)
        {
            try
            {
                string failedFile;
                if (!VerifyIntegrityManifest(includeSecurityFiles, out failedFile))
                {
                    hashStatus = "error";
                    LastFailureDetail = "변형되거나 누락된 파일: " + failedFile;
                    if (showResult)
                    {
                        MessageBox.Show("무결성 검사 결과 변형되거나 누락된 파일이 발견되었습니다.\r\n\r\n" +
                            failedFile, "무결성 검사 결과",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return false;
                }

                hashStatus = "normal";
                LastFailureDetail = string.Empty;
                if (showResult)
                {
                    MessageBox.Show("무결성 검사 결과 변형된 부분이 없습니다.", "무결성 검사 결과",
                        MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                return true;
            }
            catch (CryptographicException ex)
            {
                return ReportIntegrityDatabaseFailure(ex, showResult);
            }
            catch (InvalidDataException ex)
            {
                return ReportIntegrityDatabaseFailure(ex, showResult);
            }
            catch (FormatException ex)
            {
                return ReportIntegrityDatabaseFailure(ex, showResult);
            }
            catch (Exception ex)
            {
                // 설치 패키지에 해시 파일이 빠져도 시작 시 원시 FileNotFoundException 팝업을
                // 표시하지 않는다. 상태는 warning으로 남겨 서버 감사 이벤트에 포함한다.
                hashStatus = "warning";
                LastFailureDetail = ex.Message;
                if (showResult)
                {
                    MessageBox.Show("무결성 검사를 수행할 수 없습니다.\r\n\r\n" + ex.Message,
                        "무결성 검사 결과", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }
            finally
            {
                integritySW.Expired = true;
            }
        }

        private bool ReportIntegrityDatabaseFailure(Exception exception, bool showResult)
        {
            hashStatus = "error";
            LastFailureDetail = exception.Message;
            if (showResult)
            {
                MessageBox.Show("무결성 검증용 DB가 손상되었거나 변조되었습니다.\r\n\r\n" +
                    exception.Message, "무결성 검사 결과", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        private bool VerifyIntegrityManifest(bool includeSecurityFiles, out string failedFile)
        {
            failedFile = null;
            if (!File.Exists(hashFileName))
                throw new FileNotFoundException("설치 패키지의 서명된 무결성 DB가 없습니다.", hashFileName);
            string publicKeyPath = OvWorksApplicationFiles.IntegritySigningPublicKeyPath;
            if (!File.Exists(publicKeyPath))
                throw new FileNotFoundException("무결성 검증 공개키가 없습니다.", publicKeyPath);
            string manifest = OvWorksIntegrityDatabase.ReadVerifiedManifest(hashFileName, publicKeyPath);

            var expectedHashes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StringReader(manifest))
            {
                string rawLine;
                while ((rawLine = reader.ReadLine()) != null)
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int separator = line.IndexOfAny(new[] { ' ', '\t' });
                    if (separator <= 0) throw new InvalidDataException("잘못된 무결성 DB 항목입니다.");
                    string hash = line.Substring(0, separator);
                    string fileName = line.Substring(separator).TrimStart(' ', '\t', '*');
                    if (hash.Length != 128 || fileName.Length == 0 || Path.GetFileName(fileName) != fileName)
                        throw new InvalidDataException("잘못된 무결성 DB 항목입니다: " + line);
                    expectedHashes[fileName] = HexToBytes(hash);
                }
            }

            Dictionary<string, string> requiredFiles = RequiredFiles(includeSecurityFiles);

            foreach (KeyValuePair<string, string> required in requiredFiles)
            {
                byte[] expected;
                if (!expectedHashes.TryGetValue(required.Key, out expected) || !File.Exists(required.Value) ||
                    !expected.SequenceEqual(GetHashSha512(required.Value)))
                {
                    failedFile = required.Key;
                    return false;
                }
            }
            return true;
        }

        public bool demoSecurityFilesCheck()
        {
            try
            {
                using (var certificate = new X509Certificate2(OvWorksApplicationFiles.CaCertificatePath)) { }
                using (TextReader reader = File.OpenText(OvWorksApplicationFiles.PublicKeyPath))
                {
                    if (new PemReader(reader).ReadObject() == null)
                        throw new InvalidDataException("공개키 PEM을 읽을 수 없습니다.");
                }
                LastFailureDetail = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                LastFailureDetail = ex.Message;
                return false;
            }
        }

        private static Dictionary<string, string> RequiredFiles(bool includeSecurityFiles)
        {
            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { Path.GetFileName(OvWorksApplicationFiles.ExecutablePath), OvWorksApplicationFiles.ExecutablePath },
                { "csharpOvWorksClient_1.0.0.exe.config", Path.Combine(Application.StartupPath, "csharpOvWorksClient_1.0.0.exe.config") },
                { "BouncyCastle.Crypto.dll", Path.Combine(Application.StartupPath, "BouncyCastle.Crypto.dll") },
                { "integrity_signing_public_key.pem", OvWorksApplicationFiles.IntegritySigningPublicKeyPath }
            };
            if (includeSecurityFiles)
            {
                files.Add("ca.crt", OvWorksApplicationFiles.CaCertificatePath);
                files.Add("public_key.pem", OvWorksApplicationFiles.PublicKeyPath);
            }
            return files;
        }

        private static byte[] HexToBytes(string value)
        {
            var result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return result;
        }
    }
}
