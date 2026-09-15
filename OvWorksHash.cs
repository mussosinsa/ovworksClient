using System;
using System.Collections.Generic;
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
                hash512Value = hashFileReader(hashFileName);
                byte[] targetHash512Value = GetHashSha512(targetFileName);
                if (!hash512Value.SequenceEqual(targetHash512Value))
                {
                    hashStatus = "error";
                    if (showResult)
                    {
                        MessageBox.Show("무결성 검사 결과 변형된 부분이 발견되었습니다.", "무결성 검사 결과",
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
    }
}
