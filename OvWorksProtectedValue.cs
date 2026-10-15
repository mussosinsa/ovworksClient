using System;
using System.Security.Cryptography;
using System.Text;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>민감한 문자열을 Windows DPAPI로 암호화하여 프로세스 메모리에 보관한다.</summary>
    public sealed class OvWorksProtectedValue : IDisposable
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("OVWorks-Credential-v1");
        private byte[] protectedBytes;

        public OvWorksProtectedValue(string value)
        {
            if (value == null) throw new ArgumentNullException("value");
            byte[] plainBytes = Encoding.UTF8.GetBytes(value);
            try
            {
                protectedBytes = ProtectedData.Protect(
                    plainBytes, Entropy, DataProtectionScope.CurrentUser);
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
            }
        }

        public OvWorksProtectedValue(byte[] value)
        {
            if (value == null) throw new ArgumentNullException("value");
            protectedBytes = ProtectedData.Protect(
                value, Entropy, DataProtectionScope.CurrentUser);
        }

        /// <summary>필요한 작업 동안에만 복호화하고 원본 바이트를 즉시 지운다.</summary>
        public T Use<T>(Func<string, T> action)
        {
            if (action == null) throw new ArgumentNullException("action");
            if (protectedBytes == null) throw new ObjectDisposedException(GetType().Name);
            byte[] plainBytes = ProtectedData.Unprotect(
                protectedBytes, Entropy, DataProtectionScope.CurrentUser);
            try
            {
                return action(new UTF8Encoding(false, true).GetString(plainBytes));
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
            }
        }

        public T UseBytes<T>(Func<byte[], T> action)
        {
            if (action == null) throw new ArgumentNullException("action");
            if (protectedBytes == null) throw new ObjectDisposedException(GetType().Name);
            byte[] plainBytes = ProtectedData.Unprotect(
                protectedBytes, Entropy, DataProtectionScope.CurrentUser);
            try { return action(plainBytes); }
            finally { Array.Clear(plainBytes, 0, plainBytes.Length); }
        }

        public void Dispose()
        {
            if (protectedBytes == null) return;
            Array.Clear(protectedBytes, 0, protectedBytes.Length);
            protectedBytes = null;
        }
    }
}
