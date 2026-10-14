# 암호화 무결성 DB 배포 형식

클라이언트의 무결성 DB는 `%LOCALAPPDATA%\OVWorks\OVWorksClient\Integrity`에 제품 세부버전별
`.db` 파일로 저장되며 평문 파일이 아니라 인증 암호화된 DB입니다. DB를 만들기
전의 SHA-512 매니페스트에서 각 줄은 다음 형식을 사용합니다.

```text
<128자리 SHA-512 16진수> *<파일 이름>
```

빈 줄과 `#`으로 시작하는 주석은 무시됩니다. 경로가 아닌 파일 이름만 허용됩니다.

다음 항목은 반드시 포함해야 합니다.

- `csharpOvWorksClient_1.0.0.exe`
- `csharpOvWorksClient_1.0.0.exe.config`
- `BouncyCastle.Crypto.dll`
- `ca.crt`
- `public_key.pem`

`OvWorksIntegrityDatabase.EncryptManifest`는 무작위 salt와 IV, PBKDF2-HMAC-SHA1 100,000회로
분리한 AES-256 암호화 키와 HMAC-SHA256 인증 키를 사용합니다. DB를 읽을 때는 HMAC을 먼저
검증한 후 복호화하므로 암호문이 변경되면 파일 검증 단계로 넘어가지 않습니다.

릴리스 빌드 파이프라인은 소스에 있는 기본 `ApplicationSecret`을 조직의 비밀값으로 치환해야
합니다. 클라이언트는 필요한 보안 파일이 준비된 최초 실행에서 현재 제품 버전의 DB를 생성하고,
이후 실행부터는 DB를 복호화하여 모든 대상 파일의 SHA-512 값을 비교합니다. 제품 업데이트는
세부버전이 포함된 별도의 DB를 생성하므로 이전 버전의 기준값과 충돌하지 않습니다.
