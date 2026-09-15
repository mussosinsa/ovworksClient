using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksURI
    {
        public readonly string[] vm_table_col_name = {
            "가상머신 이름","상태","호스트","IP","FQDN","메모리","CPU","콘솔","설명","시작시간"
        };

        public readonly string _sw_name = "OV-Works Client";
        public readonly string _version = "4.5.1-1.w11";
        public readonly string _company = "주식회사 새올정보기술";
        public readonly string _api_uri = "/ovirt-engine";
        public readonly string _api_token = "/sso/oauth/token";
        public readonly string _api_vms = "/api/vms";
        public readonly string _api_users = "/api/users";
        public readonly string _api_events = "/api/events";
        public readonly string _api_logout = "/api/logout";
        public readonly string _api_session = "/api/session";

        // REST API 의 컨텍스트 경로. 세션 쿠키(JSESSIONID)는 이 경로에 매여 발급되므로, 쿠키가
        // 있는지 확인할 때 반드시 이 경로로 물어야 한다. 한 단계 위(/ovirt-engine)로 물으면
        // 쿠키의 경로가 요청 경로의 접두어가 아니게 되어, 쿠키가 있어도 없다고 답한다.
        public readonly string _api_root = "/api";
        public readonly string _api_cert = "/services/pki-resource?resource=ca-certificate&format=X509-PEM-CA";
        public readonly string _api_console = "/api/vms"; // /api/vms/{vm_id}/graphicsconsoles/{protocol}

        public string _vm_console = "graphicsconsoles"; // Console
        public string _vm_protocol = string.Empty;
        
        public readonly string SPICE = "5350494345";
        public readonly string VNC = "564e43";
        public readonly string ACCEPT_JSON = "application/json";
        public readonly string ACCEPT_XML = "application/xml";
        public readonly string ACCEPT_VIEWER = "application/x-virt-viewer";

        // 엔진은 이 헤더가 있는 요청에 대해서만 세션을 유지한다. 없으면 RestApiSessionMgmtFilter가
        // 요청이 끝날 때마다 LogoutSession을 실행해 세션을 닫는다.
        public readonly string PREFER_PERSISTENT_AUTH = "persistent-auth";

        // 세션 유휴 한도(분). 엔진이 UserSessionTimeOutInterval(정책상 최대 10분)로 잘라내므로
        // 그보다 큰 값을 보내면 클라이언트만 더 길다고 오해하게 된다.
        public readonly string SESSION_TTL_MINUTES = "10";

        // 엔진이 세션이 끝난 이유를 실어 보내는 헤더. 값은 SessionEndReason.java 의 wire name 이다.
        public readonly string SESSION_END_REASON_HEADER = "X-OVirt-Session-End-Reason";

        // 세션이 끝난 이유. 사용자가 해야 할 일이 다른 두 가지 — 관리자가 끊었는가, 시간이
        // 지나 끊겼는가 — 를 구분하려고 받는다.
        public readonly string ENDED_TERMINATED_BY_ADMIN = "terminated-by-admin";
        public readonly string ENDED_IDLE_TIMEOUT = "idle-timeout";
        public readonly string ENDED_MAX_DURATION = "max-duration";
        public readonly string ENDED_SIGNED_OUT = "signed-out";
        public readonly string ENDED_SINGLE_SIGN_ON = "single-sign-on-ended";

        // 세션이 아직 살아 있는지 엔진에 묻는 주기(밀리초).
        //
        // 관리자가 세션을 끊은 순간 엔진은 그 세션의 요청을 곧바로 거절한다. 그래서 사용자가
        // 무언가를 하고 있었다면 그 동작에서 바로 끊긴다. 이 주기가 정하는 것은 남은 경우 —
        // 콘솔만 띄워 두고 대시보드를 건드리지 않는 사용자가 몇 초 뒤에 알게 되는가 — 뿐이다.
        //
        // 이 요청은 세션의 유휴 시간을 되살리지 않는다. 그러지 않으면 물어보는 것만으로 세션이
        // 영영 만료되지 않아, 물어보는 목적 자체가 뒤집힌다. 엔진 쪽 근거는
        // RestApiSessionStatusFilter.java 에 있다.
        public const int SESSION_POLL_INTERVAL_MS = 5000;

        public class OAuthResponseMessage
        {
            public string access_token { get; set; }
            public string scope { get; set; }
            public string exp { get; set; }
            public string token_type { get; set; }
        }

        public class UserResponseMessage
        {
            public string grant_type { get; set; }
            public string scope { get; set; }
            public string username { get; set; }
            public string password { get; set; }
        }
        public enum URI
        {
            GET_CONSOLE,
            GET_USERS,
            GET_VMS,
            GET_CERT,
            POST_OAUTH
        }
    }
}
