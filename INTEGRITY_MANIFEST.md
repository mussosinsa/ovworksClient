# 서명된 무결성 DB 배포

`ovworks-integrity.db`는 설치 디렉터리에 배포되는 서명된 SHA-512 매니페스트입니다. 앱은 DB를
자체 생성하거나 현재 파일을 새 정상 기준으로 등록하지 않습니다. 공급자 개인키는 빌드 서버에만
보관하고 앱에는 `integrity_signing_public_key.pem` 공개키만 배포합니다.

## 설치 단계

릴리스 빌드는 `Resources/integrity_signing_public_key.pem`을 운영 공개키로 교체하고, 대응하는
개인키로 다음 스크립트를 실행해야 합니다.

```powershell
installer\New-OvWorksIntegrityDatabase.ps1 `
  -AppDirectory '<설치 준비 디렉터리>' `
  -PrivateKeyPath '<빌드 서버의 개인키 PEM>' `
  -IncludeSecurityFiles
```

스크립트는 앱 EXE, 설정, BouncyCastle DLL, 무결성 공개키와 선택적으로 `ca.crt`, `public_key.pem`의 SHA-512를
기록하고 RSA-SHA256으로 서명합니다. 생성된 DB에는 표준 사용자 읽기 권한만 부여하며 관리자와
SYSTEM만 수정할 수 있습니다. 관리자 권한 사용자는 Windows 보안 모델상 파일을 교체할 수 있으므로
앱은 매 실행 시 공급자 공개키로 디지털 서명을 다시 검증합니다.

## 검사 단계

1. 시작 시 정적 파일(EXE, config, DLL)만 검증합니다.
2. 로그인 시 보안 파일이 준비된 후 CA와 공개키를 별도로 검증합니다.
3. DB 또는 공개키가 없거나 서명/해시가 틀리면 실행 또는 로그인을 중단합니다.

## 데모 플래그

`App.config`의 `DemoSecurityBootstrap=true`는 시연 환경에서만 CA 및 공개키 다운로드를 허용합니다.
데모 모드에서는 다운로드한 PEM/X.509 형식을 검사합니다. 운영 기본값은 `false`이며 관리자가 검증한
보안 파일을 직접 등록하고 설치 단계의 서명 DB에 해당 해시를 포함해야 합니다.
