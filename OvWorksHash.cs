using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        public OvWorksHash()
        {
            hash512Value = null;
            hashFileName = OvWorksApplicationFiles.IntegrityHashPath;
            targetFileName = OvWorksApplicationFiles.ExecutablePath;
            hashStatus = "normal"; // normal(Success), warning, error(Failure), alert
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

        public bool integrityCheck(bool showResult = true)
        {
            try
            {
                string failedFile;
                if (!VerifyIntegrityManifest(out failedFile))
                {
                    hashStatus = "error";
                    if (showResult)
                    {
                        MessageBox.Show("무결성 검사 결과 변형되거나 누락된 파일이 발견되었습니다.\r\n\r\n" +
                            failedFile, "무결성 검사 결과",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return false;
                }

                hashStatus = "normal";
                if (showResult)
                {
                    MessageBox.Show("무결성 검사 결과 변형된 부분이 없습니다.", "무결성 검사 결과",
                        MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                return true;
            }
            catch (Exception ex)
            {
                // 설치 패키지에 해시 파일이 빠져도 시작 시 원시 FileNotFoundException 팝업을
                // 표시하지 않는다. 상태는 warning으로 남겨 서버 감사 이벤트에 포함한다.
                hashStatus = "warning";
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

        private bool VerifyIntegrityManifest(out string failedFile)
        {
            failedFile = null;
            byte[] manifestBytes = File.ReadAllBytes(hashFileName);
            if (manifestBytes.Length == 64)
            {
                throw new InvalidDataException(
                    "기존 단일 실행 파일 해시 형식은 관련 파일을 검증할 수 없습니다. " +
                    "SHA-512 무결성 매니페스트를 배포해 주세요.");
            }

            var expectedHashes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in File.ReadAllLines(hashFileName))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int separator = line.IndexOfAny(new[] { ' ', '\t' });
                if (separator <= 0) throw new InvalidDataException("잘못된 무결성 매니페스트 형식입니다.");
                string hash = line.Substring(0, separator);
                string fileName = line.Substring(separator).TrimStart(' ', '\t', '*');
                if (hash.Length != 128 || fileName.Length == 0 || Path.GetFileName(fileName) != fileName)
                    throw new InvalidDataException("잘못된 무결성 매니페스트 항목입니다: " + line);
                expectedHashes[fileName] = HexToBytes(hash);
            }

            var requiredFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { Path.GetFileName(OvWorksApplicationFiles.ExecutablePath), OvWorksApplicationFiles.ExecutablePath },
                { "csharpOvWorksClient_1.0.0.exe.config", Path.Combine(Application.StartupPath, "csharpOvWorksClient_1.0.0.exe.config") },
                { "BouncyCastle.Crypto.dll", Path.Combine(Application.StartupPath, "BouncyCastle.Crypto.dll") },
                { "ca.crt", OvWorksApplicationFiles.CaCertificatePath },
                { "public_key.pem", OvWorksApplicationFiles.PublicKeyPath }
            };

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

        private static byte[] HexToBytes(string value)
        {
            var result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return result;
        }
    }
}
