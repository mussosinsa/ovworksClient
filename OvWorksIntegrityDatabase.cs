using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>무결성 DB의 암호화와 위변조 검증을 담당한다.</summary>
    internal static class OvWorksIntegrityDatabase
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("OVWIDB01");
        private const int SaltLength = 16;
        private const int IvLength = 16;
        private const int MacLength = 32;
        private const int Iterations = 100000;

        // 이 값은 DB의 평문 노출을 막는 애플리케이션 키다. 배포 환경에서는 빌드 파이프라인이
        // 소스의 기본값을 조직별 비밀값으로 치환해야 한다. 무결성은 별도의 HMAC 키가 보장한다.
        private const string ApplicationSecret =
            "OVWorks-Integrity-Database-v1-Change-This-In-Release-Build";

        /// <summary>빌드/설치 단계에서 평문 매니페스트를 인증 암호화 DB로 만든다.</summary>
        internal static void EncryptManifest(string manifest, string databasePath)
        {
            if (manifest == null) throw new ArgumentNullException("manifest");
            byte[] salt = new byte[SaltLength];
            byte[] iv = new byte[IvLength];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
                random.GetBytes(iv);
            }

            byte[] encryptionKey;
            byte[] authenticationKey;
            DeriveKeys(salt, out encryptionKey, out authenticationKey);
            try
            {
                byte[] cipherText;
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = encryptionKey;
                    aes.IV = iv;
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        byte[] plainText = new UTF8Encoding(false).GetBytes(manifest);
                        cipherText = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
                        Array.Clear(plainText, 0, plainText.Length);
                    }
                }

                byte[] payload;
                using (var stream = new MemoryStream())
                {
                    stream.Write(Magic, 0, Magic.Length);
                    stream.Write(salt, 0, salt.Length);
                    stream.Write(iv, 0, iv.Length);
                    stream.Write(cipherText, 0, cipherText.Length);
                    payload = stream.ToArray();
                }
                byte[] mac;
                using (var hmac = new HMACSHA256(authenticationKey)) mac = hmac.ComputeHash(payload);

                string temporaryPath = databasePath + ".tmp";
                using (var output = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    output.Write(payload, 0, payload.Length);
                    output.Write(mac, 0, mac.Length);
                }
                if (File.Exists(databasePath)) File.Delete(databasePath);
                File.Move(temporaryPath, databasePath);
            }
            finally
            {
                Array.Clear(encryptionKey, 0, encryptionKey.Length);
                Array.Clear(authenticationKey, 0, authenticationKey.Length);
            }
        }

        public static string DecryptManifest(string databasePath)
        {
            byte[] database = File.ReadAllBytes(databasePath);
            int minimumLength = Magic.Length + SaltLength + IvLength + 16 + MacLength;
            if (database.Length < minimumLength || !Matches(database, 0, Magic))
                throw new InvalidDataException("암호화된 무결성 DB 형식이 아닙니다.");

            int payloadLength = database.Length - MacLength;
            byte[] salt = Slice(database, Magic.Length, SaltLength);
            byte[] iv = Slice(database, Magic.Length + SaltLength, IvLength);
            byte[] suppliedMac = Slice(database, payloadLength, MacLength);
            byte[] encryptionKey;
            byte[] authenticationKey;
            DeriveKeys(salt, out encryptionKey, out authenticationKey);
            try
            {
                byte[] calculatedMac;
                using (var hmac = new HMACSHA256(authenticationKey))
                    calculatedMac = hmac.ComputeHash(database, 0, payloadLength);
                if (!FixedTimeEquals(suppliedMac, calculatedMac))
                    throw new CryptographicException("무결성 DB가 변조되었거나 암호화 키가 올바르지 않습니다.");

                int cipherOffset = Magic.Length + SaltLength + IvLength;
                byte[] cipherText = Slice(database, cipherOffset, payloadLength - cipherOffset);
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = encryptionKey;
                    aes.IV = iv;
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                        return new UTF8Encoding(false, true).GetString(plainText);
                    }
                }
            }
            finally
            {
                Array.Clear(encryptionKey, 0, encryptionKey.Length);
                Array.Clear(authenticationKey, 0, authenticationKey.Length);
            }
        }

        private static void DeriveKeys(byte[] salt, out byte[] encryptionKey, out byte[] authenticationKey)
        {
            using (var derive = new Rfc2898DeriveBytes(ApplicationSecret, salt, Iterations))
            {
                byte[] keyMaterial = derive.GetBytes(64);
                encryptionKey = Slice(keyMaterial, 0, 32);
                authenticationKey = Slice(keyMaterial, 32, 32);
                Array.Clear(keyMaterial, 0, keyMaterial.Length);
            }
        }

        private static byte[] Slice(byte[] source, int offset, int count)
        {
            var result = new byte[count];
            Buffer.BlockCopy(source, offset, result, 0, count);
            return result;
        }

        private static bool Matches(byte[] source, int offset, byte[] expected)
        {
            if (source.Length - offset < expected.Length) return false;
            int difference = 0;
            for (int i = 0; i < expected.Length; i++) difference |= source[offset + i] ^ expected[i];
            return difference == 0;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
