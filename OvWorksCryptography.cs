using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksCryptography
    {
        // 암호화에 사용할 32bytes 의 키 값.
        public string defaultKeyASE256 = "SAEOLLINFORMATIONTECHNOLOGYCOLTD";
        //private static readonly string defaultKeyASE256 = "ABCDEABCDEABCDEABCDEABCDEABCDEAB";

        // 설치 파일 해시 변수
        private SHA256 Sha256 = SHA256.Create();
        //string targetf = "\"C:\\Program Files (x86)\\Saeoll\\OV-Works Client 1.0.0\\csharpOvWorksClient_1.0.0.exe\"";

        public OvWorksCryptography()
        {

        }
        // SHA-512 HASH
        public string EncryptSHA512(string Data)
        {
            SHA512 sha = new SHA512Managed();
            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(Data));
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in hash)
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }
            return stringBuilder.ToString();
        }
        //암호화
        public String AESEncrypt256(String Input)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(defaultKeyASE256);
            aes.IV = new byte[] { 95, 31, 24, 68, 87, 10, 53, 36, 48, 22, 84, 79, 82, 98, 41, 29 };
            var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] xBuff = new byte[512];
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Encoding.UTF8.GetBytes(Input);
                    cs.Write(xXml, 0, xXml.Length);
                    cs.FlushFinalBlock();
                }
                xBuff = ms.ToArray();
            }
            String Output = Convert.ToBase64String(xBuff);
            return Output;
        }
        // 복호화
        public String AESDecrypt256(String Input)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(defaultKeyASE256);
            aes.IV = new byte[] { 95, 31, 24, 68, 87, 10, 53, 36, 48, 22, 84, 79, 82, 98, 41, 29 };
            var decrypt = aes.CreateDecryptor();
            //byte[] xBuff = null;
            byte[] xBuff = new byte[512];
            using (var ms = new MemoryStream())
            {
                
                    using (var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Write))
                    {
                        byte[] xXml = Convert.FromBase64String(Input);
                        cs.Write(xXml, 0, xXml.Length);
                        cs.FlushFinalBlock();                    
                    }
                    xBuff = ms.ToArray();                    
                
            }            
            decrypt.Dispose();
            String Output = Encoding.UTF8.GetString(xBuff);
            return Output;
        }

        // 무작위 난수(문자열) 발생
        // param : 문자열 길이 정수값
        public string RndString(int length)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
            Thread.Sleep(7);
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Compute the file's hash.
        private byte[] GetHashSha256(string filename)
        {
            using (FileStream stream = File.OpenRead(filename))
            {
                return Sha256.ComputeHash(stream);
            }
        }
        // Return a byte array as a sequence of hex values.
        public static string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
        }
    }
}
