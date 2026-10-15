[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# 무결성 DB는 사용자별 LocalAppData와 HKCU에 있으므로 반드시 앱을 사용한 사용자 컨텍스트에서
# 실행해야 한다. 감사 큐와 내려받은 보안 파일은 이 스크립트의 삭제 대상이 아니다.
$integrityDirectory = Join-Path $env:LOCALAPPDATA 'OVWorks\OVWorksClient\Integrity'
$registryPath = 'HKCU:\Software\OVWorks\OVWorksClient\Integrity'

if (Test-Path -LiteralPath $integrityDirectory) {
    Remove-Item -LiteralPath $integrityDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $registryPath) {
    Remove-Item -LiteralPath $registryPath -Recurse -Force
}

# 비어 있는 상위 디렉터리만 정리한다. PendingAudit 또는 Security가 있으면 그대로 보존한다.
$clientDataDirectory = Join-Path $env:LOCALAPPDATA 'OVWorks\OVWorksClient'
if ((Test-Path -LiteralPath $clientDataDirectory) -and
    -not (Get-ChildItem -LiteralPath $clientDataDirectory -Force | Select-Object -First 1)) {
    Remove-Item -LiteralPath $clientDataDirectory -Force
}
