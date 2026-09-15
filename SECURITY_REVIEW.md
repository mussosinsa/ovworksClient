# ID/PW 암호화 전송 코드 검토

## 결론

로그인 요청의 ID와 비밀번호는 다음 두 계층으로 보호되어 평문 그대로 전송되지 않는다.

1. 애플리케이션 계층에서 배포된 `public_key.pem`을 사용해 RSA-OAEP-SHA256으로 암호화한다.
2. 전송 계층에서 oVirt API 요청을 HTTPS(TLS)로 전송한다.

단, Basic 인증에 사용하는 Base64는 **인코딩일 뿐 암호화가 아니다**. 이 구현에서 Base64
안쪽의 ID와 비밀번호 암호문이 공개키 암호화되어 있고, 전체 HTTP 요청이 TLS로 한 번 더
보호되는 구조이다.

## 코드 흐름

### 1. 로그인 화면에서 원본 값과 공개키 경로를 전달

`OvWorksApplicationFiles.PublicKeyPath`는 `Application.StartupPath`를 기준으로 프로그램이
설치된 실행 파일 폴더의 `public_key.pem` 절대 경로를 만든다. `OvWorksLoginForm.loginButton_Click`는
이 파일의 존재 여부를 확인한다. 입력한 ID/PW 원본은 `OvWorksClientDashboardForm` 생성자로 전달되고,
대시보드도 같은 경로 계산을 사용한다.
이 단계의 값은 프로세스 메모리 안에서는 문자열 평문이지만 네트워크로 전송되지는 않는다.

관련 코드:

- `OvWorksApplicationFiles.cs`: `PublicKeyPath`
- `OvWorksLoginForm.cs`: `loginButton_Click`
- `OvWorksClientDashboardForm.cs`: 생성자와 `_username`, `_password`, `_publicKeyPath` 필드

### 2. 비밀번호에 재전송 방지 envelope 추가

`OvWorksLoginEnvelope.Wrap`은 비밀번호를 암호화하기 전에 아래 형식으로 감싼다.

```text
ovirt-login:v1:<UTC epoch seconds>:<16-byte random nonce>:<password>
```

nonce는 `RandomNumberGenerator`로 매번 새로 생성한다. 서버는 발급 시각의 허용 범위와 nonce의
중복 사용 여부를 확인해야 재전송 방지 효과가 완성된다. 이 서버 측 검증이 없다면 RSA 암호문을
복호화할 수 없더라도 캡처한 Authorization 헤더 자체를 다시 보내는 공격은 막을 수 없다.

관련 코드: `OvWorksLoginEnvelope.cs`의 `Wrap`, `NewNonce`

### 3. ID와 envelope가 적용된 PW를 각각 RSA 암호화

`OvWorksClientDashboardForm.BuildAuthorization`에서 실제 전송 직전에 다음 암호화를 수행한다.

```csharp
string encryptedUsername = RsaEncryptionLegacy.EncryptWithPemPublicKey(
    _publicKeyPath, _username);
string encryptedPassword = RsaEncryptionLegacy.EncryptWithPemPublicKey(
    _publicKeyPath, OvWorksLoginEnvelope.Wrap(_password));
```

`RsaEncryptionLegacy.EncryptWithPemPublicKey`의 세부 동작은 다음과 같다.

- PEM 공개키를 Bouncy Castle `PemReader`로 읽는다.
- OAEP digest와 MGF1 digest를 모두 SHA-256으로 지정한다.
- 입력 문자열을 UTF-8 바이트로 변환한다.
- RSA-OAEP-SHA256으로 암호화한다.
- 암호화 결과 바이트를 Base64 문자열로 반환한다.

관련 코드: `RsaEncryptionLegacy.cs`의 `EncryptWithPemPublicKey`

### 4. Basic Authorization 헤더 구성

암호화된 값은 다음 순서로 조립된다.

```text
basicPlaintext = <Base64(RSA(ID))>@<profile>:<Base64(RSA(envelope(PW)))>
authorization  = Basic <Base64(ASCII(basicPlaintext))>
```

즉, 네트워크 요청의 Authorization 헤더에 ID 또는 PW 원문이 직접 들어가지는 않는다. 프로필은
서버가 `ID@profile` 형식으로 해석해야 하므로 RSA 암호문 바깥에 있으며 애플리케이션 계층에서는
암호화되지 않는다. 프로필을 포함한 헤더 전체는 HTTPS/TLS로 보호된다.

관련 코드: `OvWorksClientDashboardForm.cs`의 `BuildAuthorization`

### 5. 최초 세션 인증 요청에만 헤더 첨부

`FreshCredentialsHandler.SendAsync`는 다음 조건을 모두 만족할 때만 Authorization 헤더를 만든다.

- oVirt REST API 경로에 대한 요청이다.
- `/logout` 또는 `/session` 요청이 아니다.
- 아직 세션이 성립하지 않았다.

세션이 성립된 뒤에는 `CookieContainer`에 저장된 세션 쿠키를 사용하며 ID/PW 암호문을 계속
보내지 않는다. 로그인 시도마다 `BuildAuthorization`이 새 비밀번호 envelope와 RSA-OAEP
암호문을 생성한다.

관련 코드:

- `OvWorksClientDashboardForm.cs`: `InitHttpClient`, `FreshCredentialsHandler.SendAsync`
- `OvWorksClientDashboardForm.cs`: `IsApiRequest`, `IsSessionRequest`, `SessionEstablished`

### 6. HTTPS와 서버 인증서 검증

API의 `BaseAddress`는 `https://`로 고정된다. 서버 인증서 검증 시 호스트 이름 불일치를
거부하고, 인증서 체인의 루트가 실행 파일과 함께 배포된 `ca.crt`와 같은지 thumbprint로
확인한다.

관련 코드: `OvWorksClientDashboardForm.cs`의 `InitHttpClient`

## 전송 값 요약

| 값 | 애플리케이션 계층 RSA | HTTPS/TLS | 비고 |
| --- | --- | --- | --- |
| ID | 적용 | 적용 | RSA-OAEP-SHA256 |
| PW | 적용 | 적용 | 시각/nonce envelope 후 RSA-OAEP-SHA256 |
| profile | 미적용 | 적용 | 서버의 `ID@profile` 파싱을 위해 암호문 밖에 위치 |
| 세션 쿠키 | 해당 없음 | 적용 | 로그인 성공 후 ID/PW 대신 사용 |

## 운영 및 서버 측 확인 사항

- 서버는 클라이언트의 `public_key.pem`에 대응하는 개인키로 ID/PW를 복호화해야 한다.
- 서버는 비밀번호 envelope의 발급 시각 만료와 nonce 1회 사용을 반드시 검증해야 한다.
- `public_key.pem`과 `ca.crt`는 설치 패키지 서명 또는 무결성 검증 범위에 포함해야 한다.
  공격자가 두 파일 중 하나를 바꿀 수 있으면 공격자의 키 또는 서버를 신뢰하게 될 수 있다.
- 원본 ID/PW는 전송 전에 프로세스 메모리에 .NET 문자열로 존재한다. 이 검토의 결론은 네트워크
  전송 보호에 관한 것이며, 메모리 덤프·로컬 관리자·악성 프로세스 위협까지 제거한다는 뜻은 아니다.
- `X-OVirt-Credentials-Encryption: RSA-OAEP-SHA256` 헤더는 암호화 방식을 서버에 알리는
  표식이며, 그 헤더 자체가 암호화를 수행하는 것은 아니다.

## 최종 판단

클라이언트 코드 기준으로 ID/PW 원문은 네트워크에 직접 송신되지 않는다. ID는 RSA-OAEP-SHA256,
PW는 재전송 방지 envelope를 적용한 뒤 RSA-OAEP-SHA256으로 암호화되고, 완성된 요청은 다시
HTTPS/TLS로 전송된다. 다만 전체 보안 성립 여부는 서버의 올바른 복호화 및 envelope 검증과
배포되는 공개키/CA 파일의 무결성에 의존한다.
