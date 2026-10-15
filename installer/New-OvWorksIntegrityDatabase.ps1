[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$AppDirectory,
    [Parameter(Mandatory = $true)][string]$PrivateKeyPath,
    [switch]$IncludeSecurityFiles
)

$ErrorActionPreference = 'Stop'
$files = @(
    'csharpOvWorksClient_1.0.0.exe',
    'csharpOvWorksClient_1.0.0.exe.config',
    'BouncyCastle.Crypto.dll',
    'integrity_signing_public_key.pem'
)
if ($IncludeSecurityFiles) { $files += @('ca.crt', 'public_key.pem') }

$manifestPath = Join-Path $env:TEMP ([Guid]::NewGuid().ToString('N') + '.manifest')
$signaturePath = $manifestPath + '.sig'
$derivedPublicKeyPath = $manifestPath + '.pub'
$databasePath = Join-Path $AppDirectory 'ovworks-integrity.db'
try {
    $deployedPublicKey = Join-Path $AppDirectory 'integrity_signing_public_key.pem'
    & openssl pkey -in $PrivateKeyPath -pubout -out $derivedPublicKeyPath
    if ($LASTEXITCODE -ne 0 -or
        (Get-FileHash $derivedPublicKeyPath -Algorithm SHA256).Hash -ne
        (Get-FileHash $deployedPublicKey -Algorithm SHA256).Hash) {
        throw 'The signing private key does not match the deployed integrity public key.'
    }
    $lines = foreach ($name in $files) {
        $path = Join-Path $AppDirectory $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing integrity target: $path" }
        '{0} *{1}' -f (Get-FileHash -LiteralPath $path -Algorithm SHA512).Hash.ToLowerInvariant(), $name
    }
    [IO.File]::WriteAllText($manifestPath, (($lines -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))

    & openssl dgst -sha256 -sign $PrivateKeyPath -out $signaturePath $manifestPath
    if ($LASTEXITCODE -ne 0) { throw 'OpenSSL integrity DB signing failed.' }

    $manifest = [IO.File]::ReadAllBytes($manifestPath)
    $signature = [IO.File]::ReadAllBytes($signaturePath)
    $stream = [IO.File]::Open($databasePath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $writer = [IO.BinaryWriter]::new($stream)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('OVWIDB02'))
        $writer.Write([int]$manifest.Length)
        $writer.Write($manifest)
        $writer.Write([int]$signature.Length)
        $writer.Write($signature)
        $writer.Flush()
    } finally { $stream.Dispose() }

    # Program Files의 기본 디렉터리 ACL과 함께 표준 사용자는 읽기만 가능하다.
    & icacls $databasePath /inheritance:r /grant:r '*S-1-5-18:(F)' '*S-1-5-32-544:(F)' '*S-1-5-32-545:(R)'
    if ($LASTEXITCODE -ne 0) { throw 'Failed to apply integrity DB ACL.' }
} finally {
    Remove-Item -LiteralPath $manifestPath, $signaturePath, $derivedPublicKeyPath -Force -ErrorAction SilentlyContinue
}
