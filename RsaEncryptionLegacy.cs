using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace csharpOvWorksClient_1._0._0
{
    internal class RsaEncryptionLegacy
    {
        /*
        public static string EncryptWithPemPublicKey(string publicKeyPemPath, string plainText)
        {
            try
            {
                // 1. PEM 파일에서 공개키 읽기
                using (var reader = File.OpenText(publicKeyPemPath))
                {
                    PemReader pemReader = new PemReader(reader);
                    AsymmetricKeyParameter keyParameter = (AsymmetricKeyParameter)pemReader.ReadObject();

                    if (keyParameter == null)
                    {
                        throw new InvalidOperationException("공개키를 읽을 수 없습니다.");
                    }

                    // 2. Bouncy Castle 키를 RSA 객체로 변환
                    RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)DotNetUtilities.ToRSA((Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)keyParameter);

                    // 3. 평문을 바이트 배열로 변환
                    byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);

                    // 4. 공개키로 암호화
                    //byte[] encryptedData = rsa.Encrypt(dataToEncrypt, true); // OAEP 패딩을 사용하려면 `true`
                    byte[] encryptedBytes = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.Pkcs1);

                    // 5. 암호화된 데이터를 Base64 문자열로 변환하여 반환
                    //return Convert.ToBase64String(encryptedData);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"암호화 오류: {ex.Message}");
                return null;
            }
        }
        */
        // 2026-07-24 Modify        
        public static string EncryptWithPemPublicKey(
            string publicKeyPemPath,
            string plainText)
        {
            if (string.IsNullOrWhiteSpace(publicKeyPemPath))
            {
                throw new ArgumentException("공개키 파일 경로가 없습니다.", "publicKeyPemPath");
            }

            if (!File.Exists(publicKeyPemPath))
            {
                throw new FileNotFoundException("RSA 공개키 파일을 찾을 수 없습니다.", publicKeyPemPath);
            }

            if (plainText == null)
            {
                throw new ArgumentNullException("plainText");
            }

            try
            {
                RsaKeyParameters publicKey;

                // -----------------------------------------
                // PEM 공개키 읽기
                // -----------------------------------------
                using (TextReader reader =
                    File.OpenText(publicKeyPemPath))
                {
                    PemReader pemReader = new PemReader(reader);
                    object pemObject = pemReader.ReadObject();
                    if (pemObject == null)
                    {
                        throw new InvalidOperationException("PEM 공개키를 읽을 수 없습니다.");
                    }
                    // -----BEGIN PUBLIC KEY-----
                    // -----BEGIN RSA PUBLIC KEY-----
                    if (pemObject is RsaKeyParameters)
                    {
                        publicKey = (RsaKeyParameters)pemObject;
                    }
                    // KeyPair로 반환되는 경우
                    else if (pemObject is AsymmetricCipherKeyPair)
                    {
                        AsymmetricCipherKeyPair keyPair = (AsymmetricCipherKeyPair)pemObject;
                        publicKey = (RsaKeyParameters)keyPair.Public;
                    }
                    else
                    {
                        throw new InvalidOperationException("지원하지 않는 RSA PEM 형식입니다.\r\n" + "PEM Object Type: " + pemObject.GetType().FullName);
                    }
                }
                // -----------------------------------------
                // RSA-OAEP-SHA256
                //
                // OAEP Hash  : SHA-256
                // MGF1 Hash  : SHA-256
                // -----------------------------------------
                OaepEncoding cipher = new OaepEncoding(new RsaEngine(), new Sha256Digest(), new Sha256Digest(), null);
                cipher.Init(true, publicKey); // true = encryption
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                // RSA 키 크기에 따른 최대 평문 길이 체크
                int maxInputLength = cipher.GetInputBlockSize();

                if (plainBytes.Length > maxInputLength)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "RSA-OAEP-SHA256 암호화 가능한 최대 길이를 초과했습니다.\r\n" +
                            "입력 길이: {0} bytes\r\n" +
                            "최대 길이: {1} bytes",
                            plainBytes.Length,
                            maxInputLength
                        )
                    );
                }
                byte[] encryptedBytes = cipher.ProcessBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "RSA-OAEP-SHA256 암호화에 실패했습니다.\r\n" +
                    "Public Key: " +
                    publicKeyPemPath +
                    "\r\n\r\n" +
                    "원인: " +
                    ex.Message,
                    ex
                );
            }
        }
    }
}
