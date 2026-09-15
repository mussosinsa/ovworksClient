using System;
using System.Net.Http;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>엔진에 세션이 살아 있는지 물은 결과.</summary>
    internal enum OvWorksSessionState
    {
        /// <summary>세션이 살아 있다.</summary>
        Live,

        /// <summary>세션이 끝났다. 이유는 <see cref="OvWorksSessionStatus.Reason"/> 에 있다.</summary>
        Ended,

        /// <summary>
        /// 답을 얻지 못했다. 엔진에 닿지 못했거나 엔진이 답하지 못한 경우다.
        ///
        /// 세션이 끝난 것과 같지 않다. 이것을 종료로 읽으면 잠깐의 네트워크 끊김이나 엔진의
        /// 일시적인 문제가 모든 사용자를 로그아웃시킨다. 그래서 두 가지를 한 값으로 합치지 않는다.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// 세션 상태와, 끝났다면 그 이유.
    ///
    /// 이유는 엔진의 SessionEndReason 이 정한 문자열 그대로다. 클라이언트가 사용자에게 보여 줄
    /// 문구를 고르는 데에만 쓴다.
    /// </summary>
    internal struct OvWorksSessionStatus
    {
        public OvWorksSessionState State { get; private set; }

        /// <summary>세션이 끝난 이유. 살아 있거나 엔진이 이유를 싣지 않았으면 null 이다.</summary>
        public string Reason { get; private set; }

        public static OvWorksSessionStatus Live()
        {
            return new OvWorksSessionStatus { State = OvWorksSessionState.Live, Reason = null };
        }

        public static OvWorksSessionStatus Ended(string reason)
        {
            return new OvWorksSessionStatus { State = OvWorksSessionState.Ended, Reason = reason };
        }

        public static OvWorksSessionStatus Unknown()
        {
            return new OvWorksSessionStatus { State = OvWorksSessionState.Unknown, Reason = null };
        }

        /// <summary>
        /// 응답에 실린 종료 사유를 읽는다. 헤더가 없으면 null 이다 — 이 헤더를 보내지 않는
        /// 예전 엔진에 붙었을 때가 그렇고, 그때는 이유 없는 일반 안내로 떨어진다.
        /// </summary>
        public static string ReasonOf(HttpResponseMessage response, string headerName)
        {
            System.Collections.Generic.IEnumerable<string> values;
            if (response != null &&
                response.Headers.TryGetValues(headerName, out values))
            {
                foreach (string value in values)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value.Trim();
                    }
                }
            }
            return null;
        }
    }
}
