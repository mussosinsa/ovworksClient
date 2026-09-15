# remote-viewer 화면 캡처 및 키로깅 방지 검토

## 결론

보호 대상을 가상 콘솔 화면으로만 한정하려면 **remote-viewer 프로세스 자체에서** 보호 기능을
구현해야 한다. OV-Works Client의 로그인 창이나 대시보드 창에 보호 속성을 적용하면 가상 콘솔은
보호되지 않고 엉뚱한 창만 캡처에서 제외된다. 따라서 이전에 두 WinForms 창에 적용했던
`ScreenCaptureProtection` 호출은 제거했다.

## 왜 클라이언트에서 remote-viewer 창을 보호할 수 없는가

OV-Works Client는 `Process.Start`로 별도 프로세스인 `remote-viewer.exe`를 실행한다.
`SetWindowDisplayAffinity`는 호출 프로세스가 소유한 최상위 창에만 적용할 수 있으므로,
OV-Works Client가 `remote-viewer`의 HWND를 찾아 호출하는 방식은 지원되지 않으며 실패한다.

프로세스 감시, 창 제목 검색, `MainWindowHandle` 대기 같은 코드를 추가해도 이 소유권 제약은
바뀌지 않는다. DLL injection이나 전역 hook으로 제약을 우회하는 방식은 보안 제품 동작과
유사하고 안정성·서명·업데이트 문제를 만들기 때문에 사용하지 않아야 한다.

## 캡처 방지를 remote-viewer에 적용하는 방법

사내에서 빌드하는 `remote-viewer`/virt-viewer 소스에 다음 동작을 추가해야 한다.

1. SPICE 또는 VNC 콘솔을 표시하는 Windows 최상위 창의 HWND가 생성된 직후 실행한다.
2. `SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE)`를 호출한다.
3. 구형 Windows에서 `ERROR_INVALID_PARAMETER`가 반환되는 경우에만
   `SetWindowDisplayAffinity(hwnd, WDA_MONITOR)`로 대체한다.
4. 콘솔 창이 재생성되거나 전체 화면 창으로 전환될 때 새 HWND에도 다시 적용한다.
5. API 호출 실패를 보안 로그에 남기고, 정책에 따라 콘솔 표시를 중단하는 fail-closed 모드를
   제공한다.

개념적인 Windows 구현은 다음과 같다. 이 코드는 OV-Works Client가 아니라 **remote-viewer의
창 생성 코드 안에** 들어가야 한다.

```c
#define WDA_MONITOR             0x00000001
#define WDA_EXCLUDEFROMCAPTURE  0x00000011

if (!SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE)) {
    DWORD error = GetLastError();
    if (error != ERROR_INVALID_PARAMETER ||
        !SetWindowDisplayAffinity(hwnd, WDA_MONITOR)) {
        /* 보호 적용 실패: 기록 후 정책에 따라 창을 닫는다. */
    }
}
```

이 설정은 Windows의 보호 속성을 준수하는 캡처 API에 대한 심층 방어이다. 외부 카메라,
관리자·커널 권한 프로그램 또는 보호되는 캡처 경로를 사용하지 않는 프로그램까지 막는 DRM은
아니다.

## 키로깅 방지를 가상 콘솔 사용 중에만 적용하는 방법

저수준 키보드 hook으로 키 값을 바꾸거나 다음 hook 호출을 막는 것은 keylogger 방지가 아니다.
이미 먼저 설치된 hook, 관리자 권한 프로세스, raw input 및 커널 드라이버는 입력을 계속 읽을 수
있고, 정상 단축키·한글 IME·접근성 기능을 손상시킬 수 있다. 그러므로 삭제한
`OvWorksKeyboardHook`을 다시 활성화해서는 안 된다.

가능한 통제는 remote-viewer 실행 수명에 맞춘 **운영 정책**이다.

- remote-viewer 실행 전에 EDR 상태, 실행 파일 서명 및 무결성을 확인한다.
- AppLocker 또는 WDAC 정책으로 승인되지 않은 입력 hook/캡처 프로그램 실행을 제한한다.
- remote-viewer 실행 계정을 최소 권한으로 운영하고 관리자 프로세스와 분리한다.
- 콘솔 프로세스가 종료되면 임시 `.vv` 파일과 단기 콘솔 티켓을 즉시 폐기한다.
- 강한 보호가 필요하면 일반 사용자 데스크톱이 아니라 Windows 보안 데스크톱, 전용 키오스크
  세션 또는 격리된 관리 단말에서 remote-viewer를 실행한다.

이 정책들은 콘솔이 실행되는 동안에만 활성화하도록 EDR/WDAC 또는 전용 런처 정책과 연동할 수
있다. 하지만 OV-Works Client 내부의 키보드 hook만으로 외부 keylogger를 차단하는 기능은
구현할 수 없다.

## 적용 범위

| 화면/프로세스 | 캡처 방지 적용 위치 | 키로깅 대응 |
| --- | --- | --- |
| OV-Works 로그인 창 | 적용하지 않음 | OS/EDR 정책 |
| OV-Works 대시보드 | 적용하지 않음 | OS/EDR 정책 |
| remote-viewer 가상 콘솔 | remote-viewer 자체 창 생성 코드 | 콘솔 실행 기간의 EDR/WDAC·격리 정책 |

현재 저장소에서 가능한 수정은 잘못된 보호 범위를 제거하고, 외부 프로세스에 보호 API를 호출하지
않도록 명확히 하는 것이다. 실제 가상 콘솔 캡처 방지는 배포 중인 virt-viewer 소스 또는 해당
제품 공급사에서 구현해야 한다.
