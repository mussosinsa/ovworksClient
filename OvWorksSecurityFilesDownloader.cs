using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>로그인에 필요한 oVirt CA와 RSA 공개키를 서버에서 내려받는다.</summary>
    internal static class OvWorksSecurityFilesDownloader
    {
        private const string PublicKeyClientSerial = "saeoll20250322";

        public static async Task DownloadMissingFilesAsync(string host)
        {
            Uri caUri = BuildUri("https", host,
                "/ovirt-engine/services/pki-resource?resource=ca-certificate&format=X509-PEM-CA");
            Uri publicKeyUri = BuildUri("http", host,
                "/ovirt-engine/sso/oauth/public-key");

            if (!File.Exists(OvWorksApplicationFiles.CaCertificatePath))
            {
                // ca.crt 자체를 신뢰점으로 설치하기 전의 부트스트랩 요청이므로 curl -k와
                // 동일하게 이 요청에 한해서만 서버 인증서 검사를 생략한다.
                using (var handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        delegate { return true; };
                    using (var client = new HttpClient(handler))
                    {
                        await DownloadPemAsync(client, caUri,
                            OvWorksApplicationFiles.CaCertificatePath, "CERTIFICATE");
                    }
                }
            }

            if (!File.Exists(OvWorksApplicationFiles.PublicKeyPath))
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Client-Serial", PublicKeyClientSerial);
                    await DownloadPemAsync(client, publicKeyUri,
                        OvWorksApplicationFiles.PublicKeyPath, "PUBLIC KEY");
                }
            }
        }

        private static Uri BuildUri(string scheme, string host, string pathAndQuery)
        {
            string trimmedHost = (host ?? string.Empty).Trim();
            Uri suppliedUri;
            string hostWithScheme = trimmedHost.IndexOf("://", StringComparison.Ordinal) >= 0
                ? trimmedHost
                : scheme + "://" + trimmedHost;
            if (!Uri.TryCreate(hostWithScheme, UriKind.Absolute, out suppliedUri) ||
                string.IsNullOrWhiteSpace(suppliedUri.Host))
            {
                throw new ArgumentException("올바른 서버 주소를 입력해 주세요.", "host");
            }

            UriBuilder builder = new UriBuilder(suppliedUri);
            builder.Scheme = scheme;
            builder.Path = "/";
            builder.Query = string.Empty;
            return new Uri(builder.Uri, pathAndQuery);
        }

        private static async Task DownloadPemAsync(
            HttpClient client, Uri uri, string destinationPath, string pemLabel)
        {
            byte[] contents;
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                response.EnsureSuccessStatusCode();
                contents = await response.Content.ReadAsByteArrayAsync();
            }

            string text = Encoding.ASCII.GetString(contents);
            if (contents.Length == 0 ||
                text.IndexOf("-----BEGIN " + pemLabel + "-----", StringComparison.Ordinal) < 0)
            {
                throw new InvalidDataException(
                    string.Format("서버가 올바른 {0} PEM 파일을 반환하지 않았습니다.", pemLabel));
            }

            string temporaryPath = destinationPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, contents);
                File.Move(temporaryPath, destinationPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}
