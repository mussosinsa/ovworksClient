using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksClientHandler
    {
        public string[] _vm_id; // VM ID
        public readonly string _vm_run = "start";
        public readonly string _vm_stop = "stop";
        public readonly string _vm_shutdown = "shutdown";
        public readonly string _vm_suspend = "suspend";
        public readonly string _vm_reboot = "reboot";

        public Process remoteviewerProc;
        public IntPtr remoteviewerHandle = IntPtr.Zero;

        public OvWorksClientHandler() { }

        public async Task<Boolean> Txt2FileSave(string txt, string path, string fname) => await Task.Run(() =>
                                                                                                {
                                                                                                    try
                                                                                                    {
                                                                                                        StreamWriter file = new StreamWriter(path + "\\" + fname);
                                                                                                        file.WriteLine(txt);
                                                                                                        file.Flush();
                                                                                                        file.Close();
                                                                                                        return true;
                                                                                                    }
                                                                                                    catch (Exception e)
                                                                                                    {
                                                                                                        MessageBox.Show("파일 저장시 Exception 발생" + " : " + e.ToString());
                                                                                                        return false;
                                                                                                    }
                                                                                                });
        public void VirtViewer(OvWorksConfig ovWorksConfig)
        {
            // 화면 캡처 제외는 창을 소유한 프로세스만 적용할 수 있다. 따라서 이 클라이언트가
            // remote-viewer의 HWND에 SetWindowDisplayAffinity를 호출해서는 안 된다. 보호가 필요한
            // 배포판은 remote-viewer 자체가 콘솔 창 생성 시 WDA_EXCLUDEFROMCAPTURE를 적용해야 한다.
            ProcessStartInfo startInfo = new ProcessStartInfo();            
            startInfo.FileName = ovWorksConfig._viewer_path + "\\remote-viewer.exe";
            startInfo.Arguments = ovWorksConfig._config_path + "\\" + ovWorksConfig._vm_console_fname;
            remoteviewerProc = Process.Start(startInfo);
        }

        // 엔진 세션을 끝낸다.
        //
        // 세션 쿠키를 가진 것이 곧 그 세션을 끝낼 권한이므로 별도 자격증명은 필요 없다. 엔진은
        // 204 를 돌려주며, 끝낼 세션이 없었어도 마찬가지다.
        //
        // /sso/oauth/revoke 는 쓸 수 없다. 그쪽은 SSO 토큰을 요구하는데 이 클라이언트는 Basic
        // 인증을 쓰고 토큰 교환은 엔진 안에서 일어나므로 토큰을 손에 쥐지 않는다.
        public async Task<Boolean> LogoutSessionAsync(HttpClient client, OvWorksURI _URI)
        {
            try
            {
                using (var request = new HttpRequestMessage(
                    HttpMethod.Post, _URI._api_uri + _URI._api_logout))
                using (var response = await client.SendAsync(request))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception)
            {
                // 로그아웃 요청이 실패해도 클라이언트 종료를 막지는 않는다. 남은 세션은 유휴 한도가
                // 지나면 엔진이 정리한다.
                return false;
            }
        }

        // 엔진에 이 세션이 아직 살아 있는지 묻는다.
        //
        // 클라이언트는 요청을 보냈다가 거절당할 때에만 세션이 끝난 것을 안다. 콘솔만 띄워 두고
        // 대시보드를 건드리지 않는 사용자는 아무것도 요청하지 않으므로, 관리자가 세션을 끊어도
        // 로그아웃된 사람의 화면에 데스크톱이 계속 떠 있게 된다. 그래서 주기적으로 이것을 묻는다.
        //
        // 자격증명을 싣지 않는다. 세션 쿠키만으로 묻는 것이 핵심이다 — 자격증명이 실리면 엔진이
        // 그것으로 새 세션을 만들어, 세션이 끝난 것을 알아채려는 요청이 도리어 새 세션을 여는
        // 꼴이 된다. 헤더를 붙이지 않는 일은 FreshCredentialsHandler 가 한다.
        //
        // 이 요청은 세션의 유휴 시간을 되살리지 않으므로, 물어보는 것만으로 세션이 연장되지는
        // 않는다. 엔진 쪽 근거는 RestApiSessionStatusFilter.java 에 있다.
        public async Task<OvWorksSessionStatus> GetSessionStatusAsync(HttpClient client, OvWorksURI _URI)
        {
            try
            {
                using (var request = new HttpRequestMessage(
                    HttpMethod.Get, _URI._api_uri + _URI._api_session))
                using (var response = await client.SendAsync(request))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return OvWorksSessionStatus.Ended(
                            OvWorksSessionStatus.ReasonOf(response, _URI.SESSION_END_REASON_HEADER));
                    }
                    if (response.IsSuccessStatusCode)
                    {
                        return OvWorksSessionStatus.Live();
                    }
                    // 503 을 포함해 나머지는 모두 "답을 얻지 못했다"이다. 세션이 끝났다고 읽으면
                    // 엔진이 잠깐 흔들릴 때 모두가 로그아웃된다.
                    return OvWorksSessionStatus.Unknown();
                }
            }
            catch (Exception)
            {
                // 엔진에 닿지 못한 것은 세션이 끝난 것이 아니다. 다음 주기에 다시 묻는다.
                return OvWorksSessionStatus.Unknown();
            }
        }

        public async Task<string> GETusersSync(HttpClient client, OvWorksURI _URI, string _username, string _profile, string _password)
        {
            // User-Agent, Accept, Authorization 은 InitHttpClient()에서 한 번만 설정한다.
            // DefaultRequestHeaders 는 클라이언트 전체에 적용되므로 요청마다 Add 하면 값이 쌓인다.
            using (var response = await client.GetAsync(_URI._api_uri + _URI._api_users))
            {
                if (!response.IsSuccessStatusCode) return string.Empty;
                return await response.Content.ReadAsStringAsync();
            }
        }
        // authenticating: 이 요청이 세션을 세우는 요청인가.
        //
        // 같은 401 이라도 뜻이 다르다. 로그인하는 요청이 거절당한 것은 자격증명이 틀렸다는 뜻이고,
        // 이미 세션을 가진 요청이 거절당한 것은 세션이 끝났다는 뜻이다. 뒤의 경우에 "로그인에
        // 실패했습니다"를 띄우면 사용자는 자기 비밀번호를 의심하게 된다. 세션이 끝난 것은 왜
        // 끝났는지까지 담아 대시보드가 알리므로, 여기서는 아무 말도 하지 않고 돌려준다.
        public async Task<string> GETvmsSync(HttpClient client, OvWorksURI _URI, string _username, string _profile, string _password,
            Boolean authenticating = true)
        {
            // 헤더는 InitHttpClient()에서 한 번만 설정한다. 요청마다 Add 하면 같은 헤더가 중복되고,
            // 재사용되는 클라이언트에서는 InvalidOperationException 으로 이어진다.
            try
            {
                using (var response = await client.GetAsync(_URI._api_uri + _URI._api_vms))
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return await response.Content.ReadAsStringAsync();
                        case HttpStatusCode.Unauthorized: // username, profile, password를 잘못입력하면 발생함.
                            if (authenticating)
                            {
                                MessageBox.Show("로그인에 실패했습니다.", "사용자 로그인 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            return HttpStatusCode.Unauthorized.ToString();
                        default: return response.StatusCode.ToString();
                    }
                }
            }
            catch (InvalidOperationException ex) 
            {
                ReportVmListFailure(authenticating);
                return ex.Message;
            }
            catch (HttpRequestException ex) // 잘못된 호스트로 접속할 경우 발생함.
            {
                ReportVmListTransportFailure(authenticating, ex.Message);
                return ex.HResult.ToString();
            }
            catch (TaskCanceledException ex)
            {
                ReportVmListTransportFailure(authenticating, "서버 응답 시간이 초과되었습니다.");
                return ex.Message;
            }
            //return result;
        }

        // 로그인하다 실패한 것과, 이미 들어와 있는 상태에서 목록을 못 받아 온 것은 사용자가 할 일이
        // 다르다. 앞은 입력을 다시 보게 하고, 뒤는 잠시 뒤 다시 시도하게 한다.
        private void ReportVmListFailure(Boolean authenticating)
        {
            if (authenticating)
            {
                MessageBox.Show("로그인에 실패했습니다.", "사용자 로그인 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("가상머신 목록을 가져오지 못했습니다.\n\n잠시 후 다시 시도해 주십시오.",
                    "가상머신 목록 조회 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReportVmListTransportFailure(Boolean authenticating, string detail)
        {
            string action = authenticating
                ? "로그인 요청이 서버에 도달하지 못했습니다."
                : "가상머신 목록 요청이 서버에 도달하지 못했습니다.";
            MessageBox.Show(action + "\n\n서버 주소, 포트, 방화벽 및 TLS 인증서를 확인해 주십시오.\n\n" + detail,
                "서버 연결 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public async Task<string> GETGuestVMinfo(HttpClient client, OvWorksURI _URI, OvWorksClientHandler ovwhandler,
            int index, string _username, string _profile, string _password, string action)
        {
            /*
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "C# oVirt remote viewer program");
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(_URI.ACCEPT_XML));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
               $"{_username + "@" + _profile}:{_password}"))); 
            */
            using (var response = await client.GetAsync(_URI._api_uri + _URI._api_vms + $"/{ovwhandler._vm_id[index]}/" + action))
            {
                if (!response.IsSuccessStatusCode) return string.Empty;
                return await response.Content.ReadAsStringAsync();
            }
        }
        public async Task<Boolean> GETPKIcert(HttpClient client, OvWorksURI _URI, OvWorksConfig ovwconfig
            )
        {
            /*
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "C# oVirt remote viewer program");
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(_URI.ACCEPT_XML));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
               $"{_username + "@" + _profile}:{_password}"))); 
            */
            using (var response = await client.GetAsync(_URI._api_uri + _URI._api_cert))
            {
                if (!response.IsSuccessStatusCode) return false;
                string result = await response.Content.ReadAsStringAsync();
                return await Txt2FileSave(result, ovwconfig._config_path, ovwconfig._ca_cert_fname);
            }
            //MessageBox.Show(_URI._api_uri + _URI._api_vms + $"/{ovwhandler._vm_id[index]}/" + action);
            //return result;
        }
        public async Task<Boolean> RemoteViewer(HttpClient client, OvWorksURI _URI, OvWorksClientHandler ovwhandler, OvWorksConfig ovwconfig, 
            int index, string protocol, string _username, string _profile, string _password)
        {
            string _uri_console = string.Empty;
            switch (protocol)
            {
                case "spice":
                    _uri_console = _URI._api_uri + _URI._api_vms + $"/{ovwhandler._vm_id[index]}/" + _URI._vm_console + "/" + _URI.SPICE;
                    break;
                case "vnc":
                    _uri_console = _URI._api_uri + _URI._api_vms + $"/{ovwhandler._vm_id[index]}/" + _URI._vm_console + "/" + _URI.VNC;
                    break;
            }
            //_uri_console = _URI._api_uri + _URI._api_vms + $"/{ovwhandler._vm_id[index]}/" + _URI._vm_console + "/" + _URI.SPICE;
            //MessageBox.Show(_uri_console);
            //logRichTextBox.AppendText(_uri_console + "\n\n");
            // 이 요청만 Accept 가 다르므로 요청 단위로 지정한다. 요청에 붙은 Accept 가 있으면
            // 클라이언트의 기본 Accept 는 적용되지 않는다.
            if (string.IsNullOrEmpty(_uri_console)) return false;
            using (var request = new HttpRequestMessage(HttpMethod.Get, _uri_console))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_URI.ACCEPT_VIEWER));
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode) return false;
                    string result = await response.Content.ReadAsStringAsync();
                    return await Txt2FileSave(result, ovwconfig._config_path, ovwconfig._vm_console_fname);
                }
            }
        }
        public async Task<string> POSTVMactionAsync(HttpClient client, OvWorksURI _URI, OvWorksClientHandler ovwhandler, 
            int index, string _username, string _profile, string _password, string action)
        {
            /* Action to start a virtual machine --------------------------------------------
             * POST /ovirt-engine/api/vms/vm_id/start HTTP/1.1
             * POST /ovirt-engine/api/vms/vm_id/stop HTTP/1.1
             * POST /ovirt-engine/api/vms/vm_id/shutdown HTTP/1.1
             * POST /ovirt-engine/api/vms/vm_id/suspend HTTP/1.1
             * POST /ovirt-engine/api/vms/vm_id/reboot HTTP/1.1
             * Accept: application/xml
             * Content-type: application/xml
             * 
             * <action/>
             * ------------------------------------------------------------------------------
             */
            string _uri_console = string.Empty;
            _uri_console = _URI._api_uri + _URI._api_vms + $"/{ovwhandler._vm_id[index]}/" + action;
            var xml = @"<action/>";
            using (var request = new HttpRequestMessage(HttpMethod.Post, _uri_console))
            {
                request.Content = new StringContent(xml, Encoding.UTF8, "application/xml");
                using (var response = await client.SendAsync(request))
                {
                    return response.StatusCode.ToString();
                }
            }
        }
        public async Task<string> POSTAuditactionAsync(HttpClient client, OvWorksURI _URI, OvWorksClientHandler ovwhandler,
            string _xml, string _username, string _profile, string _password)
        {
            /* Action to start a virtual machine --------------------------------------------
            POST /ovirt-engine/api/events HTTP/1.1
            Accept: application/xml
            Content-type: application/xml
            <event>
                <description>The heat of the host is above 30 Oc</description>
                <severity>warning</severity>
                <origin>HP Openview</origin>
                <custom_id>1</custom_id>
                <flood_rate>30</flood_rate>
                <host id="f59a29cd-587d-48a3-b72a-db537eb21957" >
                    <external_status>
                        <state>warning</state>
                </external_status>
                </host>
            </event>
             * ------------------------------------------------------------------------------
             */
            string _uri_console = string.Empty;
            _uri_console = _URI._api_uri + _URI._api_events;
            var xml = _xml;
            /*
            var xml = @"
<event> 
<description>The OV-Works Client 1.0.0 Integrity warning</description> 
  <severity>warning</severity> 
  <origin>OV-Works</origin> 
  <custom_id>12</custom_id> 
</event>";  
            */
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, _uri_console))
                {
                    request.Content = new StringContent(xml, Encoding.UTF8, "application/xml");
                    using (var response = await client.SendAsync(request))
                    {
                        return response.StatusCode.ToString();
                    }
                }
            }
            catch (SocketException sockEx)
            {
                MessageBox.Show($"Socket error code: {sockEx.SocketErrorCode}" + "\r\n" + $"Message: {sockEx.Message}");
                return string.Empty;
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException sockEx)
            {
                // Handle specific SocketErrors (e.g., connection refused, host not found)
                //Console.WriteLine($"Socket error code: {sockEx.SocketErrorCode}");
                //Console.WriteLine($"Message: {sockEx.Message}");
                MessageBox.Show($"Socket error code: {sockEx.SocketErrorCode}" + "\r\n" + $"Message: {sockEx.Message}");
                return string.Empty;
            }
            catch (HttpRequestException ex)
            {
                // Handle other HTTP request errors (e.g., non-success status codes)
                //Console.WriteLine($"HTTP error: {ex.Message}");
                MessageBox.Show($"HTTP error: {ex.Message}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                // Handle general fallbacks
                //Console.WriteLine($"Unexpected error: {ex.Message}");
                MessageBox.Show($"Unexpected error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
