using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;

namespace csharpOvWorksClient_1._0._0
{
    internal class RsaEncryptionLegacy
    {
        public static string EncryptWithPemPublicKey(string publicKeyPemPath, byte[] plainBytes)
        {
            if (plainBytes == null) throw new ArgumentNullException("plainBytes");
            RsaKeyParameters publicKey;
            using (TextReader reader = File.OpenText(publicKeyPemPath))
            {
                object pemObject = new PemReader(reader).ReadObject();
                if (pemObject is RsaKeyParameters) publicKey = (RsaKeyParameters)pemObject;
                else if (pemObject is AsymmetricCipherKeyPair)
                    publicKey = (RsaKeyParameters)((AsymmetricCipherKeyPair)pemObject).Public;
                else throw new InvalidOperationException("지원하지 않는 RSA PEM 형식입니다.");
            }
            OaepEncoding cipher = new OaepEncoding(
                new RsaEngine(), new Sha256Digest(), new Sha256Digest(), null);
            cipher.Init(true, publicKey);
            if (plainBytes.Length > cipher.GetInputBlockSize())
                throw new InvalidOperationException("RSA-OAEP-SHA256 암호화 가능한 최대 길이를 초과했습니다.");
            return Convert.ToBase64String(cipher.ProcessBlock(plainBytes, 0, plainBytes.Length));
        }
    }
}
