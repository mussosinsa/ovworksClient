using System;
using System.Security.Cryptography;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>
    /// 자격증명을 재전송할 수 없는 형태로 감싼다.
    ///
    /// 암호화는 자격증명의 내용을 가릴 뿐, 암호문을 손에 넣은 사람이 그것을 다시 보내는 것을 막지
    /// 못한다. 프록시로 요청을 한 번 가로채면 그 암호문은 패스워드가 바뀔 때까지, 어느 PC에서든,
    /// 몇 번이든 로그인에 쓸 수 있다. 서버로서는 방금 받은 암호문과 한 시간 전에 받은 암호문을
    /// 구별할 방법이 없기 때문이다.
    ///
    /// 그래서 암호화 안쪽에 두 가지를 함께 넣는다. 언제 만들었는지, 그리고 두 번 다시 쓰지 않을 값.
    ///
    ///   ovirt-login:v1:&lt;epoch 초&gt;:&lt;논스&gt;:&lt;자격증명&gt;
    ///
    /// 자격증명이 콜론을 포함할 수 있으므로 맨 뒤에 둔다. 엔진 쪽 형식 정의는
    /// LoginEnvelope.java 에 있다.
    /// </summary>
    internal static class OvWorksLoginEnvelope
    {
        /// <summary>엔진의 LoginEnvelope.PREFIX 와 같아야 한다.</summary>
        public const string Prefix = "ovirt-login:v1:";

        private const string Separator = ":";

        /// <summary>논스는 추측할 수 없기만 하면 되고, 16바이트면 그 조건을 넉넉히 넘는다.</summary>
        private const int NonceBytes = 16;

        /// <summary>
        /// 인증할 때마다 새로 불러야 한다. 한 번 만든 값을 재사용하면 엔진이 그것을 재전송으로
        /// 보고 거절한다 — 그것이 이 봉투의 목적이다.
        /// </summary>
        public static string Wrap(string credential)
        {
            long issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Prefix + issuedAt + Separator + NewNonce() + Separator + credential;
        }

        private static string NewNonce()
        {
            byte[] bytes = new byte[NonceBytes];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            // base64 의 '+' 와 '/' 를 URL 안전 문자로 바꾸고 패딩을 뗀다. 구분자인 ':' 가 논스에
            // 섞이지 않는다는 것만 지키면 되지만, 어디에 실려도 탈이 없는 형태로 둔다.
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
