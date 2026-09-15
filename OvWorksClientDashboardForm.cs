using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;
using static csharpOvWorksClient_1._0._0.OvWorksURI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace csharpOvWorksClient_1._0._0
{
    public partial class OvWorksClientDashboardForm : Form
    {
        private string _serialNum; // Serial Number
        private string _host; // Server IP or domain
        private string _Vusername;
        private string _host_protocol; // HTTPS

        // 평문이다. 인증할 때마다 새로 감싸 암호화해야 하므로 암호문을 들고 있을 수 없다.
        private string _username;
        private string _profile;
        private string _password;
        private string _publicKeyPath;

        private string _hashStatus;

        private ListViewItem vm_item;

        private OvWorksHash ovWorksHash;
        private OvWorksURI ovWorksURI;// = new OvWorksURI();
        private OvWorksConfig ovWorksConfig;
        private OvWorksClientHandler ovWorksClientHandler;
        private OvWorksCryptography ovWorksCryptography;

        HttpClientHandler handler;
        HttpClient client;
        FreshCredentialsHandler credentialsHandler;

        // 세션이 한 번 선 뒤로는 자격증명을 다시 보내지 않는다. 이 값이 그 경계다.
        //
        // 요청마다 자격증명을 실어 보내면, 엔진은 세션이 사라진 뒤에도 그 헤더로 조용히 새 세션을
        // 만든다. 관리자가 webadmin 에서 세션을 끊어도 클라이언트는 아무 일도 없었던 것처럼
        // 이어가고, 목록에는 같은 사용자의 새 세션이 다시 나타난다 — 끊은 것이 끊은 것이 되지
        // 않는다. 로그인할 때 한 번만 보내고, 그 뒤로는 세션 쿠키만으로 요청한다.
        private volatile bool _sessionEstablished;

        // 세션 종료를 이미 처리했는지. 거절된 요청이 여럿이어도 로그아웃은 한 번만 한다.
        private volatile bool _sessionEndHandled;

        // 엔진에 세션 상태를 묻는 타이머. 사용자가 콘솔만 쓰고 있어도 종료를 알아채게 한다.
        private System.Windows.Forms.Timer sessionPollTimer;
        private bool _sessionPollInProgress;

        public OvWorksClientDashboardForm(string serialNum, string host, string Vusername, string username, string profile, string password, string hashStatus)
        {
            InitializeComponent();

            // 변수 초기화
            _serialNum = serialNum;
            _host = host;
            _Vusername = Vusername;
            _host_protocol = "https";
            _username = username;
            _profile = profile;
            _password = password;
            _publicKeyPath = OvWorksApplicationFiles.PublicKeyPath;
            
            _hashStatus = hashStatus;
            //MessageBox.Show(_password);

            // Listview 초기화
            vmListView.View=View.Details;
            vmListView.GridLines = true;
            vmListView.FullRowSelect = true;
            vmListView.Items.Clear();            
            vmListView.SmallImageList = imageList2;
            vmListView.Scrollable = true;
            
            // 무결성 검사 -2026-08-27-
            ovWorksHash = new OvWorksHash();

            ovWorksURI = new OvWorksURI();
            foreach (string cname in ovWorksURI.vm_table_col_name)
            {
                //vmListView.Columns.Add(cname, panel2.Size.Width/ovWorksURI.vm_table_col_name.Length, HorizontalAlignment.Center);
                vmListView.Columns.Add(cname, -2, HorizontalAlignment.Left);
            }
            // config(설정 경로, 설정 파일 : console.vv, 사용자 정보 등) 초기화
            ovWorksConfig = new OvWorksConfig();

            // config 파일 이름 무작위 생성
            File_Name_Generate(32, 3); // 32 문자열 파일.3 문자열 확장자

            // client handler 초기화
            InitHttpClient();
            /*
            handler =new HttpClientHandler();
            // HTTPS(TLS) bypass(만약 사용할 경우 oVirt 엔진 서버의 인증서(ca.cert)를 다운받아 적용해야 함.
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            // Authentication Credential(현재 사용하지 않음)
            // handler.Credentials = AuthenticationCredential(_host_protocol + "://" + _host + ovirtHandler._uri, _DIGEST, _digest_id, _digest_password);
            // 클라이언트 생성
            client = new HttpClient(handler);
            // 기본 프로토콜로 URL 설정 = https:// oVirt-engine 서버 URL
            client.BaseAddress = new Uri(_host_protocol + "://" + _host);
            */

            // GET, POST handler
            ovWorksClientHandler = new OvWorksClientHandler();

            // 테두리 없는 대시보드의 상단 제목 영역에서 창을 이동한다. 로그아웃/최소화/최대화
            // 컨트롤은 연결하지 않아 각각의 클릭 동작을 그대로 유지한다.
            tableLayoutPanel2.MouseDown += WindowDrag_MouseDown;
            titleLabel.MouseDown += WindowDrag_MouseDown;
            pictureBox2.MouseDown += WindowDrag_MouseDown;
            usernameLabel.MouseDown += WindowDrag_MouseDown;
            label2.MouseDown += WindowDrag_MouseDown;

        }

        private void WindowDrag_MouseDown(object sender, MouseEventArgs e)
        {
            BorderlessWindowDrag.Begin(this, e);
        }

        // 인증 방식을 적용할 경우 credential 생성
        public CredentialCache AuthenticationCredential(string url, string auth, string id, string password)
        {
            var digest_uri = new Uri(url);
            var credentialCache = new CredentialCache();
            credentialCache.Add(
                new Uri(digest_uri.GetLeftPart(UriPartial.Authority)),
                        auth, // Authentication type(Basic or Digest)
                        new NetworkCredential(id, password) // Credientials(id, password)
                        );
            return credentialCache;
        }
        // 로그인부터 로그아웃까지 하나의 HttpClient를 쓴다. 요청마다 새로 만들면 핸들러가 들고 있는
        // 쿠키 저장소가 함께 버려져 엔진이 발급한 JSESSIONID가 사라지고, 그러면 세션을 유지해 달라고
        // 요청해도 이어갈 세션을 가리킬 수단이 없다.
        public void InitHttpClient()
        {
            if (client != null)
            {
                return;
            }

            Uri baseAddress = BuildEngineBaseAddress(_host);

            // 프로그램 설치 폴더에 public_key.pem과 함께 배포된 oVirt CA를 사용한다.
            string caCertPath = OvWorksApplicationFiles.CaCertificatePath;
            if (!File.Exists(caCertPath))
            {
                throw new FileNotFoundException("oVirt CA 인증서 파일을 찾을 수 없습니다.", caCertPath);
            }
            X509Certificate2 ovirtCaCert = new X509Certificate2(caCertPath);
            IPAddress engineIpAddress;
            bool connectingByIpAddress = IPAddress.TryParse(baseAddress.Host, out engineIpAddress);

            handler = new HttpClientHandler();
            // 세션 쿠키(JSESSIONID)를 이 클라이언트가 살아 있는 동안 보관한다.
            handler.CookieContainer = new CookieContainer();
            handler.UseCookies = true;
            // HTTPS(TLS) bypass
            // handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            // HTTPS(SSL) ca.crt 인증서 적용 -2026-08-21-
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (cert == null) return false;

                // DNS 이름으로 접속할 때는 인증서 이름이 반드시 일치해야 한다. 현장에서는 엔진을
                // IP로 직접 입력하지만 기존 oVirt 인증서에 IP SAN이 없는 경우가 있으므로, IP 접속의
                // 이름 불일치는 아래의 배포 CA pin 검증이 성공할 때에만 허용한다.
                if ((errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0
                    && !connectingByIpAddress)
                {
                    return false;
                }

                // ExtraStore는 인증서 후보만 추가할 뿐 신뢰 대상을 고정하지 않는다. 따라서 체인을
                // 만든 뒤 루트가 배포된 oVirt CA와 정확히 같은지도 확인한다. AllowUnknown...만
                // 사용하면 공격자가 만든 자체 서명 CA도 통과할 수 있다.
                using (X509Chain customChain = new X509Chain())
                {
                    customChain.ChainPolicy.ExtraStore.Add(ovirtCaCert);
                    customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    bool isTrusted = customChain.Build(new X509Certificate2(cert));
                    if (!isTrusted || customChain.ChainElements.Count == 0)
                    {
                        return false;
                    }

                    X509Certificate2 chainRoot = customChain.ChainElements[
                        customChain.ChainElements.Count - 1].Certificate;
                    return string.Equals(chainRoot.Thumbprint, ovirtCaCert.Thumbprint,
                        StringComparison.OrdinalIgnoreCase);
                }
            };

            // 클라이언트 생성. 인증할 때 자격증명을 새로 만들어 붙이고, 세션이 거절당하면 그것을
            // 알리는 핸들러를 끼운다.
            credentialsHandler = new FreshCredentialsHandler(
                handler, BuildAuthorization, SessionEstablished, MarkSessionEstablished,
                OnSessionRejected, ovWorksURI.SESSION_END_REASON_HEADER,
                ovWorksURI._api_uri + ovWorksURI._api_root);
            client = new HttpClient(credentialsHandler);
            // User-Agent : SAEOLL
            //client.DefaultRequestHeaders.Add("User-Agent","SAEOLL");
            client.DefaultRequestHeaders.Add("X-Client-Serial", _serialNum);

            //REST API Encryption Header- 2026-07-24
            client.DefaultRequestHeaders.Add("X-OVirt-Credentials-Encryption", "RSA-OAEP-SHA256");

            // 요청마다 같은 값을 넣던 헤더들. 클라이언트를 재사용하게 되면서 여기서 한 번만
            // 설정한다. DefaultRequestHeaders.Add는 같은 헤더를 덮어쓰지 않고 덧붙이므로,
            // 요청마다 부르면 값이 중복되거나 예외가 난다.
            client.DefaultRequestHeaders.Add("User-Agent", "C# oVirt remote viewer program");
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(ovWorksURI.ACCEPT_XML));

            // 세션 유지 요청. 이것이 없으면 엔진은 요청이 끝날 때마다 세션을 닫는다.
            client.DefaultRequestHeaders.Add("Prefer", ovWorksURI.PREFER_PERSISTENT_AUTH);
            client.DefaultRequestHeaders.Add("Session-TTL", ovWorksURI.SESSION_TTL_MINUTES);

            // Authorization 은 여기서 넣지 않는다. 한 번 만든 값을 계속 쓰면 그것을 가로챈
            // 쪽이 나중에 그대로 다시 보낼 수 있으므로, FreshCredentialsHandler 가 요청마다
            // 새로 만들어 붙인다.

            client.BaseAddress = baseAddress;
        }

        private static Uri BuildEngineBaseAddress(string host)
        {
            Uri uri;
            string candidate = Uri.UriSchemeHttps + "://" + (host ?? string.Empty).Trim().TrimEnd('/') + "/";
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out uri)
                || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(uri.Host)
                || !string.IsNullOrEmpty(uri.UserInfo)
                || uri.AbsolutePath != "/"
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new UriFormatException("엔진 주소에는 호스트 이름 또는 IP 주소와 포트만 입력해야 합니다.");
            }
            return uri;
        }
        /// <summary>
        /// 이번 요청에 쓸 Basic 자격증명을 만든다.
        ///
        /// 패스워드는 OvWorksLoginEnvelope 로 감싸 시각과 1회용 논스를 함께 넣은 뒤 암호화한다.
        /// 그래서 이 값을 가로채도 잠깐 동안, 단 한 번밖에 쓰지 못한다. 사용자 이름은 비밀이
        /// 아니고 엔진이 마지막 '@' 뒤를 프로파일로 읽으므로 형태를 그대로 둔다.
        /// </summary>
        private AuthenticationHeaderValue BuildAuthorization()
        {
            string encryptedUsername = RsaEncryptionLegacy.EncryptWithPemPublicKey(_publicKeyPath, _username);
            string encryptedPassword = RsaEncryptionLegacy.EncryptWithPemPublicKey(
                _publicKeyPath, OvWorksLoginEnvelope.Wrap(_password));

            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(
                encryptedUsername + "@" + _profile + ":" + encryptedPassword)));
        }

        /// <summary>
        /// 로그인하는 요청에만 자격증명을 붙이고, 세션이 거절당하면 그 사실을 알린다.
        ///
        /// 자격증명은 세션을 세울 때 한 번만 보낸다. 세션이 선 뒤에도 계속 보내면 엔진은 세션이
        /// 사라진 뒤 그 헤더로 다시 인증해 새 세션을 만드는데, 그것이 조용히 일어나므로 관리자가
        /// 세션을 끊어도 클라이언트는 끊긴 줄을 모른다. 보내지 않으면 엔진은 401 로 거절하고,
        /// 그 401 이 곧 "세션이 끝났다"는 신호가 된다. 요청마다 새 봉투로 감싸던 것도 함께 사라져
        /// 자격증명이 선을 타는 횟수 자체가 로그인 한 번으로 줄어든다.
        ///
        /// 401 에는 엔진이 사유를 실어 보낸다(EnforceAuthFilter). 사유를 알아내려고 다시 묻지
        /// 않는 것은, 그때쯤이면 그 세션을 가리키던 HTTP 세션이 이미 버려져 물어도 "그런 세션 없음"
        /// 밖에 돌아오지 않기 때문이다. 사유는 거절 그 자체에 실려 와야 한다.
        /// </summary>
        private class FreshCredentialsHandler : DelegatingHandler
        {
            private readonly Func<AuthenticationHeaderValue> authorization;
            private readonly Func<bool> sessionEstablished;
            private readonly Action sessionOpened;
            private readonly Action<string> sessionRejected;
            private readonly string endReasonHeader;
            private readonly string apiPath;

            private int requestsInFlight;

            public FreshCredentialsHandler(HttpMessageHandler inner,
                Func<AuthenticationHeaderValue> authorization,
                Func<bool> sessionEstablished,
                Action sessionOpened,
                Action<string> sessionRejected,
                string endReasonHeader,
                string apiPath)
                : base(inner)
            {
                this.authorization = authorization;
                this.sessionEstablished = sessionEstablished;
                this.sessionOpened = sessionOpened;
                this.sessionRejected = sessionRejected;
                this.endReasonHeader = endReasonHeader;
                this.apiPath = apiPath;
            }

            /// <summary>아직 답을 기다리는 요청 수. 클라이언트를 버려도 되는 때를 판단하는 데 쓴다.</summary>
            public int RequestsInFlight
            {
                get { return Volatile.Read(ref requestsInFlight); }
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // 세션을 세우는 요청에만 자격증명을 붙인다. 그 밖의 요청은 세션 쿠키만으로 간다.
                bool apiRequest = IsApiRequest(request);
                bool authenticating = apiRequest && !IsSessionRequest(request) && !sessionEstablished();
                if (authenticating)
                {
                    request.Headers.Authorization = authorization();
                }

                Interlocked.Increment(ref requestsInFlight);
                HttpResponseMessage response;
                try
                {
                    response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Decrement(ref requestsInFlight);
                }

                if (authenticating)
                {
                    // 오류 응답(예: 500/503)을 인증 성공으로 오인하면 다음 요청부터 자격증명을
                    // 빼서 연쇄 401을 만든다. 성공 응답을 받은 경우에만 세션을 열었다고 표시한다.
                    if (response.IsSuccessStatusCode)
                    {
                        sessionOpened();
                    }
                }
                else if (apiRequest && response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // 자격증명을 싣지 않은 요청의 401 이라야 세션이 끝났다는 뜻이 된다. 실어 보낸
                    // 요청의 401 은 로그인 실패이지 세션 종료가 아니다.
                    sessionRejected(OvWorksSessionStatus.ReasonOf(response, endReasonHeader));
                }
                return response;
            }

            /// <summary>
            /// REST API 로 가는 요청인지.
            ///
            /// 자격증명도 세션도 REST API 에만 해당한다. CA 인증서를 받아 오는
            /// /ovirt-engine/services/pki-resource 처럼 인증이 필요 없는 곳에 자격증명을 보낼
            /// 이유가 없고, 그곳이 돌려준 200 을 세션이 섰다는 신호로 읽어서도 안 된다.
            /// </summary>
            private bool IsApiRequest(HttpRequestMessage request)
            {
                string path = request.RequestUri.AbsolutePath;
                return path.Equals(apiPath, StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(apiPath + "/", StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// 세션 자체를 다루는 요청인지. 로그아웃과 세션 상태 조회가 그것이고, 둘 다 자격증명을
            /// 실으면 안 된다. 로그아웃은 새 세션을 만든 직후 그것을 끝내는 요청이 되어 버리고,
            /// 상태 조회는 세션이 끝난 것을 알아내려다 도리어 새 세션을 여는 요청이 된다.
            /// </summary>
            private bool IsSessionRequest(HttpRequestMessage request)
            {
                string path = request.RequestUri.AbsolutePath;
                return path.Equals(apiPath + "/logout", StringComparison.OrdinalIgnoreCase)
                    || path.Equals(apiPath + "/session", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 이 클라이언트가 이미 세션을 세웠는지.
        ///
        /// 세션 쿠키를 받은 순간부터 참이 되고, 그 뒤로는 자격증명을 보내지 않는다. 쿠키가 아니라
        /// 이 값을 기준으로 삼는 것은, 쿠키가 어떤 이유로든 사라졌을 때 자격증명이 다시 실려
        /// 나가면 그것만으로 새 세션이 서 버리기 때문이다. 한 번 선 세션은 끝날 때까지 세션이고,
        /// 끝나면 로그인 화면으로 돌아간다.
        /// </summary>
        private bool SessionEstablished()
        {
            if (!_sessionEstablished && HasEngineSessionCookie())
            {
                _sessionEstablished = true;
            }
            return _sessionEstablished;
        }

        /// <summary>
        /// 자격증명을 실은 요청이 인증을 통과했다 — 엔진에 이 클라이언트의 세션이 섰다.
        ///
        /// 쿠키 저장소를 들여다보는 것과 별개로 이 신호를 쓰는 것은, 세션이 섰다는 사실의 근거가
        /// 응답 그 자체이기 때문이다. 쿠키를 어느 경로로 물어야 하는지 같은 것을 틀려도, 자격증명을
        /// 두 번 보내는 일만은 일어나지 않아야 한다.
        /// </summary>
        private void MarkSessionEstablished()
        {
            _sessionEstablished = true;
        }

        /// <summary>
        /// 세션 쿠키만 실어 보낸 요청이 401 로 거절당했을 때 불린다.
        ///
        /// 요청을 보낸 스레드에서 불리므로 여기서 폼을 만지면 안 되고, 진행 중인 요청 한가운데서
        /// 대시보드를 닫아서도 안 된다. 실제 처리는 UI 스레드에 맡긴다.
        /// </summary>
        private void OnSessionRejected(string reason)
        {
            if (_sessionEndHandled)
            {
                return;
            }
            try
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action<string>(HandleSessionEnded), reason);
                }
            }
            catch (ObjectDisposedException)
            {
                // 폼이 이미 닫혔다. 돌아갈 로그인 화면은 이미 그곳에 있다.
            }
            catch (InvalidOperationException)
            {
                // 창 핸들이 사라지는 사이였다. 위와 같다.
            }
        }

        private async void HandleSessionEnded(string reason)
        {
            await EndSessionAsync(reason);
        }

        /// <summary>
        /// 세션이 끝났다는 것을 확인했을 때, 사용자를 로그인 화면으로 돌린다.
        ///
        /// 콘솔을 먼저 닫는다. 엔진 세션과 SPICE/VNC 연결은 별개라서, 세션이 끝나도 remote-viewer
        /// 는 호스트에 붙은 채로 남는다. 로그아웃된 사용자의 화면에 데스크톱이 그대로 떠 있는 것은
        /// 세션을 끊는 일 자체를 무의미하게 만들므로, 안내창보다 먼저 한다.
        /// </summary>
        private async Task EndSessionAsync(string reason)
        {
            if (_sessionEndHandled)
            {
                return;
            }
            _sessionEndHandled = true;

            StopSessionPolling();
            KillRemoteViewer();

            await WaitForRequestsInFlightAsync();

            MessageBox.Show(SessionEndedMessage(reason), "세션 종료",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // 끝낼 세션이 없으므로 로그아웃 요청은 보내지 않는다. 보내면 엔진에 남는 것은 이미
            // 끝난 세션에 대한 두 번째 종료 기록뿐이다.
            await ReturnToLoginAsync(false);
        }

        /// <summary>
        /// 답을 기다리는 요청이 끝날 때까지 잠깐 기다린다.
        ///
        /// 로그인 화면으로 돌아가는 길에 HttpClient 를 버리는데, 그때 진행 중인 요청이 있으면
        /// 그것을 기다리던 쪽이 ObjectDisposedException 을 받는다. 사용자에게는 세션이 끝났다는
        /// 사실과 아무 상관 없어 보이는 오류창으로 보인다. 끝나지 않으면 그냥 진행한다 — 세션
        /// 종료가 요청 하나에 발목 잡히는 편이 더 나쁘다.
        /// </summary>
        private async Task WaitForRequestsInFlightAsync()
        {
            for (int i = 0; i < 20; i++)
            {
                if (credentialsHandler == null || credentialsHandler.RequestsInFlight == 0)
                {
                    return;
                }
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// 세션이 왜 끝났는지를 사용자가 할 일의 언어로 옮긴다.
        ///
        /// 관리자가 끊은 것과 시간이 지나 끊긴 것은 사용자가 다음에 해야 할 일이 다르다. 앞의
        /// 경우는 누군가가 그렇게 하기로 한 것이고, 뒤의 경우는 아무 일도 없었던 것이다. 하나를
        /// 다른 하나로 알리는 것은 아무것도 알리지 않는 것보다 나쁘다.
        ///
        /// 사유를 싣지 않는 예전 엔진에 붙었을 때는 일반 안내로 떨어진다. 모르는 것을 아는 척하지
        /// 않는 편이 낫다.
        /// </summary>
        private string SessionEndedMessage(string reason)
        {
            if (string.Equals(reason, ovWorksURI.ENDED_TERMINATED_BY_ADMIN, StringComparison.OrdinalIgnoreCase))
            {
                return "관리자가 세션을 종료했습니다.\n\n계속하시려면 다시 로그인해 주십시오.";
            }
            if (string.Equals(reason, ovWorksURI.ENDED_IDLE_TIMEOUT, StringComparison.OrdinalIgnoreCase))
            {
                return "사용하지 않은 시간이 길어 세션이 만료되었습니다.\n\n다시 로그인해 주십시오.";
            }
            if (string.Equals(reason, ovWorksURI.ENDED_MAX_DURATION, StringComparison.OrdinalIgnoreCase))
            {
                return "세션의 허용 시간이 끝나 만료되었습니다.\n\n다시 로그인해 주십시오.";
            }
            if (string.Equals(reason, ovWorksURI.ENDED_SIGNED_OUT, StringComparison.OrdinalIgnoreCase))
            {
                return "세션이 로그아웃되었습니다.\n\n계속하시려면 다시 로그인해 주십시오.";
            }
            if (string.Equals(reason, ovWorksURI.ENDED_SINGLE_SIGN_ON, StringComparison.OrdinalIgnoreCase))
            {
                return "통합 인증(SSO) 세션이 종료되었습니다.\n\n다시 로그인해 주십시오.";
            }
            return "세션이 종료되었습니다.\n\n다시 로그인해 주십시오.";
        }

        /// <summary>
        /// 세션이 살아 있는지 주기적으로 엔진에 묻기 시작한다.
        ///
        /// 사용자가 무언가를 하고 있다면 그 요청이 거절당하는 것으로 끝난 것을 알게 되므로, 이
        /// 타이머가 있어야 하는 경우는 하나다 — 콘솔만 띄워 두고 대시보드를 건드리지 않는 사용자.
        /// 그 사용자에게는 이것 말고 알려 줄 길이 없다.
        /// </summary>
        private void StartSessionPolling()
        {
            if (sessionPollTimer != null)
            {
                return;
            }
            sessionPollTimer = new System.Windows.Forms.Timer();
            sessionPollTimer.Interval = OvWorksURI.SESSION_POLL_INTERVAL_MS;
            sessionPollTimer.Tick += sessionPollTimer_Tick;
            sessionPollTimer.Start();
        }

        private void StopSessionPolling()
        {
            if (sessionPollTimer == null)
            {
                return;
            }
            sessionPollTimer.Stop();
            sessionPollTimer.Tick -= sessionPollTimer_Tick;
            sessionPollTimer.Dispose();
            sessionPollTimer = null;
        }

        private async void sessionPollTimer_Tick(object sender, EventArgs e)
        {
            // 앞선 질문이 아직 답을 기다리는 중이면 이번 차례는 건너뛴다. 엔진이 느릴 때 질문만
            // 쌓이게 할 이유가 없다.
            if (_sessionEndHandled || _sessionPollInProgress || client == null || !_sessionEstablished)
            {
                return;
            }
            _sessionPollInProgress = true;
            try
            {
                OvWorksSessionStatus status =
                    await ovWorksClientHandler.GetSessionStatusAsync(client, ovWorksURI);
                if (status.State == OvWorksSessionState.Ended)
                {
                    await EndSessionAsync(status.Reason);
                }
                // Unknown 은 아무것도 하지 않는다. 답을 얻지 못한 것이지 세션이 끝난 것이 아니다.
            }
            finally
            {
                _sessionPollInProgress = false;
            }
        }

        /// <summary>
        /// remote-viewer 를 닫는다. 사용자가 콘솔을 한 번도 열지 않았다면 프로세스가 없으므로
        /// null 일 수 있다.
        /// </summary>
        private void KillRemoteViewer()
        {
            try
            {
                var remoteViewerProcess = ovWorksClientHandler.remoteviewerProc;
                if (remoteViewerProcess != null && !remoteViewerProcess.HasExited)
                {
                    remoteViewerProcess.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // 프로세스 상태를 확인하는 사이 이미 종료되었다.
            }
            catch (Win32Exception)
            {
                // 이미 종료 중이거나 프로세스를 종료할 권한이 없다. 로그아웃은 계속한다.
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 창이 어떤 길로 닫히든 타이머는 멈춘다.
            StopSessionPolling();
            base.OnFormClosed(e);
        }

        // 작업이 끝날 때마다 불리던 자리. 여기서 클라이언트를 버리면 세션 쿠키도 함께 사라지므로
        // 이제는 아무것도 하지 않는다. 실제 정리는 로그아웃 시 CloseHttpClient()가 한다.
        public void DisposeHttpClient()
        {
        }

        // 세션을 끝낼 때만 부른다.
        public void CloseHttpClient()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
            if (handler != null)
            {
                handler.Dispose();
                handler = null;
            }
            credentialsHandler = null;
            // 쿠키 저장소가 사라졌으니 세션도 사라졌다. 다음에 다시 여는 클라이언트는 처음부터,
            // 곧 자격증명을 실은 요청 하나로 세션을 세운다.
            _sessionEstablished = false;
        }

        private Boolean HasEngineSessionCookie()
        {
            if (client == null || handler == null || client.BaseAddress == null)
            {
                return false;
            }

            // REST API 의 컨텍스트 경로로 묻는다. 쿠키는 그 경로에 매여 발급되고, CookieContainer
            // 는 쿠키의 경로가 묻는 경로의 접두어일 때에만 그 쿠키를 돌려준다. 상위 경로로 물으면
            // 접두어 관계가 반대가 되어, 세션이 멀쩡한데도 쿠키가 없다는 답이 돌아온다.
            Uri apiUri = new Uri(client.BaseAddress, ovWorksURI._api_uri + ovWorksURI._api_root + "/");
            return handler.CookieContainer.GetCookies(apiUri).Count > 0;
        }

        private void File_Name_Generate(int fnameSize, int extSize)
        {
            ovWorksCryptography = new OvWorksCryptography();
            ovWorksConfig._config_fname = ovWorksCryptography.RndString(fnameSize) + "." + ovWorksCryptography.RndString(extSize);
            ovWorksConfig._vm_console_fname = ovWorksCryptography.RndString(fnameSize) + "." + ovWorksCryptography.RndString(extSize);
            ovWorksConfig._ca_cert_fname = ovWorksCryptography.RndString(fnameSize) + "." + ovWorksCryptography.RndString(extSize);
        }
        private int ListviewSelectedIndex()
        {
            int index = 0;
            foreach (ListViewItem item in vmListView.Items)
            {
                if (item.Selected)
                    index = item.Index;
            }
            return index;
        }

        private string ListviewSelectedDisplayProtocol()
        {
            string str1 = string.Empty;
            //System.Windows.Forms.ListView.SelectedListViewItemCollection itemColl = vmListView.SelectedItems;            
            foreach (ListViewItem item in vmListView.Items)
            {
                if (item.Selected)
                    str1 = item.SubItems[7].Text;
            }
            return str1;
        }
        private string ListviewSelectedItem(int cols)
        {
            string str = string.Empty;
            foreach (ListViewItem item in vmListView.Items)
            {
                if (item.Selected)
                    str = item.SubItems[cols].Text;
            }
            return str;
        }
        
        private void exitButton_Click(object sender, EventArgs e)
        {
            CloseHttpClient();
            this.Close();
        }

        private void minimizeButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void maximizeButton_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }

        //private async void OvWorksClientDashboardForm_Load(object sender, EventArgs e)
        private async void OvWorksClientDashboardForm_Load(object sender, EventArgs e)
        {
            vmListView.Items.Clear();
            string result;
            using (var waitForm = new OvWorksWaitingForm(this,
                "가상 머신 목록을 불러오는 중입니다.\r\n잠시만 기다려 주십시오."))
            {
                // Vusername -2026-09-02-
                //usernameLabel.Text = _username;
                usernameLabel.Text = _Vusername;
                // HttpClient 초기화
                InitHttpClient();
                // 사용자의 VM 정보 가져오기
                result = await ovWorksClientHandler.GETvmsSync(client, ovWorksURI, _username, _profile, _password);
            }
                if (result.Equals("Unauthorized") || result.Equals("-2146233088"))
                {
                    // 첫 VM 조회에서 인증이 성립하지 않았으므로 종료할 엔진 세션도 없다.
                    // 이때 사용자 로그아웃 이벤트를 호출하면 세션 쿠키가 없는 상태로
                    // /api/logout 을 보내 로그인 직후 로그아웃한 것처럼 기록된다.
                    await ReturnToLoginAsync(false);
                    return;
                }
                else
                {
                    byte[] ba = Encoding.UTF8.GetBytes(result);
                    MemoryStream stream = new MemoryStream(ba);
                    XmlSerializer serializer = new XmlSerializer(typeof(Vms));
                    Vms vms = (Vms)serializer.Deserialize(stream);

                    string[] vm_table_items = new string[ovWorksURI.vm_table_col_name.Length];

                    ovWorksClientHandler._vm_id = new string[vms.Vm.Count];
                    for (int i = 0; i < vms.Vm.Count; i++)
                    {
                        vm_table_items[0] = vms.Vm[i].Name; // VM 이름
                        vm_table_items[1] = vms.Vm[i].Status; // VM 상태
                        vm_table_items[2] = vms.Vm[i].Display.Address; // 호스트 IP
                        vm_table_items[3] = vms.Vm[i].Display.Address; // VM IP                    
                        vm_table_items[4] = vms.Vm[i].Fqdn; // FQDN
                        vm_table_items[5] = vms.Vm[i].Memory;// VM 메로리
                        vm_table_items[6] = vms.Vm[i].Cpu.Architecture;// VM CPU 타입            
                        vm_table_items[7] = vms.Vm[i].Display.Type;// Graphics console 
                        vm_table_items[8] = vms.Vm[i].Description; // 설명
                        vm_table_items[9] = vms.Vm[i].Start_time; // 시작 시간
                        vm_item = new ListViewItem(vm_table_items);
                        vmListView.Items.Add(vm_item);
                    switch (vms.Vm[i].Status)
                    {
                        case "up":
                            //vm_item.ImageIndex = 2;                            
                            vmListView.Items[i].ImageIndex = 2;
                            break;
                        case "down":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 0;
                            break;
                        case "reboot":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 1;
                            break;
                        case "stopping":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 4;
                            break;
                        case "suspended":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 5;
                            break;
                        case "wait":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 6;
                            break;
                        case "starting":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 3;
                            break;
                    }
                        ovWorksClientHandler._vm_id[i] = vms.Vm[i].Id;
                    //=======================================================================
                    if (vms.Vm[i].Status.Equals("up"))
                    {
                        string result2 = await ovWorksClientHandler.GETGuestVMinfo(client, ovWorksURI, ovWorksClientHandler, i, _username, _profile, _password, "reporteddevices");
                        //MessageBox.Show(result2);
                        byte[] tmp = Encoding.UTF8.GetBytes(result2);
                        MemoryStream stream2 = new MemoryStream(tmp);
                        XmlSerializer serializer2 = new XmlSerializer(typeof(ReportedDevices));                        
                        ReportedDevices rds = (ReportedDevices)serializer2.Deserialize(stream2);
                        //MessageBox.Show(rds.ReportedDevice.Name);
                        if (rds.ReportedDevice != null)
                        {
                            vm_item.SubItems[3].Text = rds.ReportedDevice.Ips.Ip[0].Address;
                        }
                        else
                        {
                            vm_item.SubItems[3].Text = string.Empty;
                        }
                    }
                        //=======================================================================

                    }
                    vm_table_items = Array.Empty<string>();

                    // Httpclient 자원 해제
                    DisposeHttpClient();
                }
            //}
            SetVMcontrolButton(false, false, false, false, false);            

            vmListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);            
            vmListView.EndUpdate();
            //InitHttpClient();
            //bool _pkicert = await ovWorksClientHandler.GETPKIcert(client, ovWorksURI, ovWorksConfig);
            //DisposeHttpClient();
            //if (_pkicert)
            //{
                await auditAction();
            //}

            // 세션이 선 뒤부터 묻는다. 이 지점까지 왔다는 것은 첫 요청이 인증을 통과했다는 뜻이다.
            StartSessionPolling();
        }
        private async Task auditAction()
        {
            Random rnd = new Random();
            
            // Get local IP Address(IPv4) -2026-08-21-
            string hostName = Dns.GetHostName();
            string localIPv4Address = string.Empty;
            IPHostEntry ipEntry = Dns.GetHostEntry(hostName);
            foreach (IPAddress ip in ipEntry.AddressList)
            {
                // Filter to get only IPv4 addresses
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIPv4Address = ip.ToString();
                }
            }

            string _xml = $"<event>" +
                $"<description>User {_username}@{_profile}-authz connected from '{localIPv4Address}' OV-Works Client 4.5.1 Integrity {_hashStatus}</description>" +
                $"<severity>{_hashStatus}</severity>" +
                $"<origin>OV-Works Client 4.5.1</origin>" +
                $"<custom_id>{DateTime.Now.ToString("Md") + rnd.Next(10, 100).ToString()}</custom_id>" +
                $"</event>";
            int selectedIndex = ListviewSelectedIndex();
            //string audit_info;
            // PKI cert hash

            InitHttpClient();
            bool _pkicert = await ovWorksClientHandler.GETPKIcert(client, ovWorksURI, ovWorksConfig);
            if (_pkicert)
            {
                string rx = await ovWorksClientHandler.POSTAuditactionAsync(client, ovWorksURI, ovWorksClientHandler,
                        _xml, _username, _profile, _password);
            }
            DisposeHttpClient();
        }
        private void Serializer2_UnreferencedObject(object sender, UnreferencedObjectEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void logoutPictureBox_Click(object sender, EventArgs e)
        {
            await ReturnToLoginAsync(true);
        }

        private async Task ReturnToLoginAsync(Boolean endEngineSession)
        {
            StopSessionPolling();
            KillRemoteViewer();
            // File flush
            ovWorksConfig.FileConfigFlush(ovWorksConfig._config_path + "\\" + ovWorksConfig._vm_console_fname);
            ovWorksConfig.FileConfigFlush(ovWorksConfig._config_path + "\\" + ovWorksConfig._config_fname);
            ovWorksConfig.FileConfigFlush(ovWorksConfig._config_path + "\\" + ovWorksConfig._ca_cert_fname);
            
            // 엔진 세션 종료. 클라이언트만 닫으면 세션은 유휴 한도(Session-TTL)가 지날 때까지
            // 엔진에 남아 있다.
            // 세션을 세운 적이 없으면 끝낼 것도 없다. 그 상태에서 logout 요청을 보내면 로그인
            // 직후 로그아웃한 것처럼 기록될 뿐이다.
            if (endEngineSession && _sessionEstablished)
            {
                await ovWorksClientHandler.LogoutSessionAsync(client, ovWorksURI);
            }

            CloseHttpClient();
            this.Close();
        }

        private void vmListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            /*
            int selectedIndex = ListviewSelectedIndex();
            using (var waitForm = new OvWorksWaitingForm(this,
                "콘솔 연결 정보를 준비하는 중입니다.\r\n잠시만 기다려 주십시오."))
            {
                InitHttpClient();
                                
                //public async Task<Boolean> RemoteViewer(HttpClient client, OvWorksURI _URI, OvWorksClientHandler ovwhandler, OvWorksConfig ovwconfig,
                //int index, string _username, string _profile, string _password)
                Boolean rx = await ovWorksClientHandler.RemoteViewer(client, ovWorksURI, ovWorksClientHandler, ovWorksConfig,
                    selectedIndex, _username, _profile, _password);
                
                if (rx) 
                {
                    ovWorksClientHandler.VirtViewer(ovWorksConfig);
                }
                DisposeHttpClient();
            }
            */
        }
        private void SetVMcontrolButton(Boolean run, Boolean suspend, Boolean stop, Boolean reboot, Boolean console)
        {
            runButton.Enabled = run;
            suspendButton.Enabled = suspend;
            stopButton.Enabled = stop;
            rebootButton.Enabled = reboot;
            consoleButton.Enabled = console;
        }
        private void vmListView_MouseClick(object sender, MouseEventArgs e)
        {
            int selectedindex = ListviewSelectedIndex();
            if (vmListView.Focused == false)
                MessageBox.Show("OK");
            //MessageBox.Show(ovWorksClientHandler._vm_id[selectedindex].ToString());
            //MessageBox.Show(ListviewSelectedItem(1));
            switch (ListviewSelectedItem(1))
            {
                case "up":
                    SetVMcontrolButton(false, true, true, true, true);
                    break;
                case "down":
                    SetVMcontrolButton(true, false, false, false, false);
                    break;
                case "suspended":
                    SetVMcontrolButton(true, false, true, false, false);
                    break;
            }
            //MessageBox.Show(ListviewSelectedDisplayProtocol());
        }

        private async void consoleButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = ListviewSelectedIndex();
            string selectedConsoleProtocol = ListviewSelectedDisplayProtocol();
            using (var waitForm = new OvWorksWaitingForm(this,
                "콘솔 연결 정보를 준비하는 중입니다.\r\n잠시만 기다려 주십시오."))
            {
                InitHttpClient();
                Boolean rx = await ovWorksClientHandler.RemoteViewer(client, ovWorksURI, ovWorksClientHandler, ovWorksConfig,
                    selectedIndex, selectedConsoleProtocol, _username, _profile, _password);
                if (rx)
                {
                    ovWorksClientHandler.VirtViewer(ovWorksConfig);
                }
                DisposeHttpClient();
            }
            //this.WindowState = FormWindowState.Minimized;
        }

        private async void runButton_Click(object sender, EventArgs e)
        {
            using (var waitForm = new OvWorksWaitingForm(this,
                "가상 머신을 실행하는 중입니다.\r\n잠시만 기다려 주십시오."))
            {
                int selectedIndex = ListviewSelectedIndex();
                InitHttpClient();
                string rx = await ovWorksClientHandler.POSTVMactionAsync(client, ovWorksURI, ovWorksClientHandler,
                    selectedIndex, _username, _profile, _password, ovWorksClientHandler._vm_run);
                DisposeHttpClient();
            }
            MessageBox.Show("가상머신(VM)을 실행 하였습니다.\n수 초(second) 동안 대기 후 부팅이 완료됩니다.", "가상머신(VM) 실행(run)",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void suspendButton_Click(object sender, EventArgs e)
        {
            using (var waitForm = new OvWorksWaitingForm(this,
                "가상 머신을 일시 정지하는 중입니다.\r\n잠시만 기다려 주십시오."))
            {
                int selectedIndex = ListviewSelectedIndex();
                InitHttpClient();
                string rx = await ovWorksClientHandler.POSTVMactionAsync(client, ovWorksURI, ovWorksClientHandler,
                    selectedIndex, _username, _profile, _password, ovWorksClientHandler._vm_suspend);                
                DisposeHttpClient();
            }
            MessageBox.Show("가상머신(VM)을 정지 하였습니다.\n수 초(second) 동안 대기 후 정지가 완료됩니다.", "가상머신(VM) 정지(suspend)",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void rebootButton_Click(object sender, EventArgs e)
        {
            using (var waitForm = new OvWorksWaitingForm(this,
                "가상 머신을 재시작하는 중입니다.\r\n잠시만 기다려 주십시오."))
            {
                int selectedIndex = ListviewSelectedIndex();
                InitHttpClient();
                string rx = await ovWorksClientHandler.POSTVMactionAsync(client, ovWorksURI, ovWorksClientHandler,
                    selectedIndex, _username, _profile, _password, ovWorksClientHandler._vm_reboot);

                DisposeHttpClient();
            }
            MessageBox.Show("가상머신(VM)을 재시작 하였습니다.\n수 초(second) 동안 대기 후 재시작이 완료됩니다.", "가상머신(VM) 재시작(reboot)",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void stopButton_Click(object sender, EventArgs e)
        {
            using (var waitForm = new OvWorksWaitingForm(this,
                "가상 머신을 종료하는 중입니다.\r\n잠시만 기다려 주십시오."))
            {
                int selectedIndex = ListviewSelectedIndex();
                InitHttpClient();
                string rx = await ovWorksClientHandler.POSTVMactionAsync(client, ovWorksURI, ovWorksClientHandler,
                    selectedIndex, _username, _profile, _password, ovWorksClientHandler._vm_stop);

                DisposeHttpClient(); 
            }
            MessageBox.Show("가상머신(VM)을 종료 하였습니다.\n수 초(second) 동안 대기 후 종료가 완료됩니다.", "가상머신(VM) 종료(shutdown)",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void VMListRefreshButton_Click(object sender, EventArgs e)
        {
            vmListView.Items.Clear();
            using (var waitForm = new OvWorksWaitingForm(this,
                "가상 머신 목록을 새로 고치는 중입니다.\r\n잠시만 기다려 주십시오."))
            {
                usernameLabel.Text = _username;
                // HttpClient 초기화
                InitHttpClient();
                // 사용자의 VM 정보 가져오기. 이 시점의 401은 로그인 실패가 아니라 세션 종료다.
                string result = await ovWorksClientHandler.GETvmsSync(client, ovWorksURI, _username, _profile, _password, false);

                // 목록 대신 상태 문자열이 돌아왔다. 그대로 XML로 읽으면 예외가 나고, 세션이 끝난
                // 경우라면 그 예외가 사용자가 보는 유일한 설명이 된다 - 세션 종료는 세션 종료대로
                // 안내되므로 여기서는 조용히 물러난다.
                if (!result.TrimStart().StartsWith("<", StringComparison.Ordinal))
                {
                    return;
                }

                byte[] ba = Encoding.UTF8.GetBytes(result);
                MemoryStream stream = new MemoryStream(ba);
                XmlSerializer serializer = new XmlSerializer(typeof(Vms));
                Vms vms = (Vms)serializer.Deserialize(stream);

                string[] vm_table_items = new string[ovWorksURI.vm_table_col_name.Length];

                ovWorksClientHandler._vm_id = new string[vms.Vm.Count];
                for (int i = 0; i < vms.Vm.Count; i++)
                {
                    vm_table_items[0] = vms.Vm[i].Name; // VM 이름
                    vm_table_items[1] = vms.Vm[i].Status; // VM 상태
                    vm_table_items[2] = vms.Vm[i].Display.Address; // 호스트 IP
                    vm_table_items[3] = vms.Vm[i].Display.Address; // VM IP
                    vm_table_items[4] = vms.Vm[i].Fqdn; // FQDN
                    vm_table_items[5] = vms.Vm[i].Memory;// VM 메로리
                    vm_table_items[6] = vms.Vm[i].Cpu.Architecture;// VM CPU 타입            
                    vm_table_items[7] = vms.Vm[i].Display.Type;// Graphics console 
                    vm_table_items[8] = vms.Vm[i].Description; // 설명
                    vm_table_items[9] = vms.Vm[i].Start_time; // 시작 시간
                    vm_item = new ListViewItem(vm_table_items);
                    //vm_item.ImageIndex = 2;                    
                    vmListView.Items.Add(vm_item);
                    switch (vms.Vm[i].Status)
                    {
                        case "up":
                            //vm_item.ImageIndex = 2;                            
                            vmListView.Items[i].ImageIndex = 2;
                            break;
                        case "down":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 0;
                            break;
                        case "reboot":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 1;
                            break;
                        case "stopping":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 4;
                            break;
                        case "suspended":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 5;
                            break;
                        case "wait":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 6;
                            break;
                        case "starting":
                            //vm_item.ImageIndex = 0;
                            vmListView.Items[i].ImageIndex = 3;
                            break;
                    }
                    ovWorksClientHandler._vm_id[i] = vms.Vm[i].Id;
                    //=======================================================================
                    if (vms.Vm[i].Status.Equals("up")) {
                        string result2 = await ovWorksClientHandler.GETGuestVMinfo(client, ovWorksURI, ovWorksClientHandler, i, _username, _profile, _password, "reporteddevices");
                        //MessageBox.Show(result2);
                        byte[] tmp = Encoding.UTF8.GetBytes(result2);
                        MemoryStream stream2 = new MemoryStream(tmp);
                        XmlSerializer serializer2 = new XmlSerializer(typeof(ReportedDevices));
                        ReportedDevices rds = (ReportedDevices)serializer2.Deserialize(stream2);
                        //MessageBox.Show(rds.ReportedDevice.Ips.Ip[0].Address);
                        if (rds.ReportedDevice != null)
                        {
                            vm_item.SubItems[3].Text = rds.ReportedDevice.Ips.Ip[0].Address;
                        }
                        else
                        {
                            vm_item.SubItems[3].Text = string.Empty;
                        }
                    }
                    //=======================================================================
                }
                vm_table_items = Array.Empty<string>();

                // Httpclient 자원 해제
                DisposeHttpClient();
            }
            SetVMcontrolButton(false, false, false, false, false);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void versionButton_Click(object sender, EventArgs e)
        {
            string swInfo = " 소프트웨어 버전 : " + ovWorksURI._sw_name + " " + ovWorksURI._version + " \n\n";
            string userInfo = " 사용자 이름 : " + _username + "\n\n";
            string companyInfo = " Copyright by " + ovWorksURI._company +" \n\n ";
            string versionInfo = swInfo + userInfo + companyInfo + System.DateTime.Now;
            MessageBox.Show(versionInfo, " OV-Works Client 4.5.1 버전 정보 ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void integrityButton_Click(object sender, EventArgs e)
        {
            ovWorksHash.integrityCheck();
            await auditAction();
        }
    }
}
