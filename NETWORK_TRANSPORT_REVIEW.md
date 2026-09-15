# 서버 패킷 전송 검토

## 결론

클라이언트의 oVirt 요청은 HTTPS를 사용하고, 인증 요청에는 암호화된 ID/PW가 들어간다. 이번
검토에서 실제 전송 장애로 이어질 수 있는 주소 검증, 응답 수명 관리, 오류 응답 처리 및 세션
판정 문제를 보완했다.

## 요청 구성

- 기본 주소: `https://<engine-host>[:port]/`
- API 경로: `/ovirt-engine/api/...`
- 기본 `Accept`: `application/xml`
- 콘솔 티켓 `Accept`: `application/x-virt-viewer`
- VM 동작 및 이벤트 본문: UTF-8 `application/xml`
- 세션 유지: `Prefer: persistent-auth`, `Session-TTL: 10`
- 최초 인증: RSA-OAEP-SHA256 ID/PW를 포함한 Basic Authorization
- 인증 후: Authorization을 다시 보내지 않고 세션 쿠키 사용

## 보완 내용

1. 엔진 입력값에 경로, query, fragment 또는 user-info가 포함되면 요청 목적지가 예상과 달라질
   수 있으므로 호스트/IP와 선택적 포트만 허용한다.
2. Basic payload를 UTF-8로 변환해 profile에 비 ASCII 문자가 있어도 `?`로 손실되지 않게 했다.
3. 최초 요청의 500/503 같은 오류를 인증 성공으로 처리하지 않고 2xx 응답만 세션 성공으로 본다.
4. `HttpRequestMessage`와 `HttpResponseMessage`를 사용 직후 dispose해 연결 자원이 누적되지 않게
   했다. 공유 `HttpClient`와 쿠키 저장소는 로그인 세션 동안 계속 재사용한다.
5. 콘솔 protocol이 `spice` 또는 `vnc`가 아니면 빈 URI를 서버 루트로 보내지 않고 요청을 중단한다.
6. CA 또는 콘솔 티켓 요청이 실패하면 서버 오류 본문을 인증서나 `.vv` 파일로 저장하지 않는다.
7. VM 목록의 기타 HTTP 오류는 더 이상 `Continue`로 뭉개지 않고 실제 상태 코드를 반환한다.
8. 엔진을 DNS 이름으로 접속하면 인증서 이름 일치를 강제한다. IP로 직접 접속하고 인증서에 IP SAN이
   없는 기존 설치는 이름 불일치를 허용하되, 인증서 체인이 배포된 `ca.crt`로 끝나는지 확인하는
   CA pin 검증은 그대로 강제한다.
9. TCP 연결 또는 TLS handshake 단계에서 실패하면 이를 잘못된 ID/PW로 표시하지 않고 서버 주소,
   포트, 방화벽 또는 인증서 문제임을 구분해 안내한다. 이 단계에서는 HTTP 요청이 서버에 도착하지
   않으므로 서버 access log가 비어 있을 수 있다.

## 확인이 필요한 서버 계약

클라이언트 저장소만으로 다음 항목은 검증할 수 없으므로 실제 엔진과 통합 시험이 필요하다.

- 서버가 `X-OVirt-Credentials-Encryption: RSA-OAEP-SHA256` 계약을 구현했는지
- 서버 개인키가 배포된 `public_key.pem`과 쌍을 이루는지
- password envelope의 시각과 nonce 중복을 검증하는지
- `Prefer`, `Session-TTL`, `/api/session` 및 세션 종료 사유 헤더 확장을 지원하는지
- 로그인 성공 응답이 클라이언트가 재사용할 세션 쿠키를 발급하는지
- SPICE/VNC graphics console 식별자와 MIME 형식이 현재 서버 버전과 일치하는지

## 권장 통합 시험

정상 로그인, 잘못된 암호(401), 서버 내부 오류(500), 일시 중단(503), TLS 이름 불일치, 잘못된 CA,
세션 만료, 관리자 강제 종료, SPICE/VNC 티켓 발급, VM start/suspend/reboot/stop 및 이벤트 등록을
실제 서버에서 각각 확인해야 한다. 패킷 캡처에서는 TLS handshake 이후 내용이 평문으로 보이지
않고, 로그인 후 후속 API 요청에 Authorization 헤더가 다시 나타나지 않는지도 확인한다.
