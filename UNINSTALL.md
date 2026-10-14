# 앱 삭제 시 무결성 DB 정리

무결성 DB와 설치 표식은 사용자별 데이터이므로 `Program Files`의 실행 파일을 삭제하는 것만으로는
제거되지 않습니다. 설치 관리자의 제거 단계에서 `installer/Remove-OvWorksIntegrityData.ps1`을
**앱을 사용한 사용자 컨텍스트**로 실행해야 합니다.

```powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass `
  -File "<설치 패키지의 작업 경로>\Remove-OvWorksIntegrityData.ps1"
```

스크립트는 다음 항목을 제거합니다.

- `%LOCALAPPDATA%\OVWorks\OVWorksClient\Integrity` 아래의 모든 버전별 암호화 DB
- `HKCU\Software\OVWorks\OVWorksClient\Integrity` 레지스트리 키와 버전별 설치 표식

서버 장애 알림이 남아 있는 `PendingAudit`과 다시 다운로드하기 어려울 수 있는 `Security` 폴더는
의도적으로 유지합니다. 완전한 사용자 데이터 삭제가 필요한 설치 정책이라면 해당 두 폴더는 별도의
명시적 사용자 동의를 받은 제거 옵션으로 처리해야 합니다.

한 PC에서 여러 Windows 사용자가 앱을 사용했다면 관리자 권한으로 한 번 실행하는 것으로는 각
사용자의 `HKCU`와 `LOCALAPPDATA`가 정리되지 않습니다. 설치 관리자는 앱을 사용한 각 사용자
컨텍스트에서 이 정리 작업을 예약하거나 실행해야 합니다.
