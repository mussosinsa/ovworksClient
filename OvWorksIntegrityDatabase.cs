using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Text;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>공급자 개인키로 서명된 무결성 DB를 검증하고 매니페스트를 읽는다.</summary>
    internal static class OvWorksIntegrityDatabase
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("OVWIDB02");
        private const int MaximumManifestBytes = 1024 * 1024;

        public static string ReadVerifiedManifest(string databasePath, string publicKeyPath)
        {
            byte[] database = File.ReadAllBytes(databasePath);
            if (database.Length < Magic.Length + 8 || !Matches(database, Magic))
                throw new InvalidDataException("서명된 무결성 DB 형식이 아닙니다.");

            int offset = Magic.Length;
            int manifestLength = ReadInt32(database, ref offset);
            if (manifestLength <= 0 || manifestLength > MaximumManifestBytes ||
                manifestLength > database.Length - offset - 4)
                throw new InvalidDataException("무결성 DB의 매니페스트 길이가 올바르지 않습니다.");
            byte[] manifest = Slice(database, offset, manifestLength);
            offset += manifestLength;
            int signatureLength = ReadInt32(database, ref offset);
            if (signatureLength <= 0 || signatureLength != database.Length - offset)
                throw new InvalidDataException("무결성 DB의 서명 길이가 올바르지 않습니다.");
            byte[] signature = Slice(database, offset, signatureLength);

            AsymmetricKeyParameter publicKey;
            using (TextReader reader = File.OpenText(publicKeyPath))
            {
                publicKey = new PemReader(reader).ReadObject() as AsymmetricKeyParameter;
            }
            if (publicKey == null || publicKey.IsPrivate)
                throw new InvalidDataException("무결성 검증 공개키를 읽을 수 없습니다.");

            ISigner verifier = SignerUtilities.GetSigner("SHA-256withRSA");
            verifier.Init(false, publicKey);
            verifier.BlockUpdate(manifest, 0, manifest.Length);
            if (!verifier.VerifySignature(signature))
                throw new InvalidDataException("무결성 DB의 디지털 서명이 올바르지 않습니다.");

            return new UTF8Encoding(false, true).GetString(manifest);
        }

        private static int ReadInt32(byte[] source, ref int offset)
        {
            if (source.Length - offset < 4) throw new InvalidDataException("무결성 DB가 잘렸습니다.");
            int value = source[offset] | source[offset + 1] << 8 |
                source[offset + 2] << 16 | source[offset + 3] << 24;
            offset += 4;
            return value;
        }

        private static byte[] Slice(byte[] source, int offset, int count)
        {
            var result = new byte[count];
            Buffer.BlockCopy(source, offset, result, 0, count);
            return result;
        }

        private static bool Matches(byte[] source, byte[] expected)
        {
            if (source.Length < expected.Length) return false;
            int difference = 0;
            for (int i = 0; i < expected.Length; i++) difference |= source[i] ^ expected[i];
            return difference == 0;
        }
    }
}
