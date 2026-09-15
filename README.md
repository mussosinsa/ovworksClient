# ovworksClient

## 빌드할 때 실행 파일을 복사할 수 없는 경우

`MSB3027` 또는 `MSB3021` 오류와 함께 `bin\Debug\csharpOvWorksClient_1.0.0.exe`가
다른 프로세스에서 사용 중이라고 표시되면 이전에 실행한 클라이언트가 아직 종료되지 않은
상태입니다. Visual Studio에서 **디버깅 중지(Shift+F5)** 를 먼저 실행하십시오. 그래도
프로세스가 남아 있으면 오류 메시지에 표시된 PID를 사용하여 다음 명령으로 종료한 뒤 다시
빌드합니다.

```bat
taskkill /PID 17784 /F
```

PID를 모를 때는 실행 파일 이름으로 종료할 수 있습니다.

```bat
taskkill /IM csharpOvWorksClient_1.0.0.exe /F
```

프로세스를 종료한 후에도 오류가 계속되면 Visual Studio에서 **솔루션 정리**를 실행하고
`bin` 및 `obj` 디렉터리를 삭제한 뒤 다시 빌드하십시오. 실행 중인 EXE는 Windows가 잠그기
때문에 클라이언트를 실행한 상태에서 같은 출력 파일을 다시 빌드할 수 없습니다.
