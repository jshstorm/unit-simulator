<#
.SYNOPSIS
    .NET SDK 설치 스크립트 (Windows용)
.DESCRIPTION
    이 스크립트는 Windows 환경에서 .NET SDK를 설치합니다.
    Microsoft의 공식 dotnet-install.ps1 스크립트를 다운로드하여 실행합니다.
.PARAMETER Channel
    설치할 .NET 채널을 지정합니다 (예: LTS, STS, 8.0, 9.0).
    기본값: LTS
.PARAMETER Version
    설치할 특정 버전을 지정합니다 (예: latest, 8.0.100).
    기본값: latest
.PARAMETER InstallDir
    .NET SDK를 설치할 디렉터리를 지정합니다.
    기본값: $env:USERPROFILE\.dotnet
.PARAMETER Architecture
    설치할 아키텍처를 지정합니다 (예: x64, x86, arm64).
    기본값: 시스템 아키텍처에 따라 자동 감지
.PARAMETER Runtime
    SDK 대신 런타임만 설치합니다 (dotnet, aspnetcore).
.PARAMETER DryRun
    실제 설치 없이 다운로드 링크만 표시합니다.
.PARAMETER NoPath
    PATH 환경 변수를 수정하지 않습니다.
.PARAMETER Verbose
    자세한 진단 정보를 표시합니다.
.PARAMETER Help
    도움말을 표시합니다.
.EXAMPLE
    .\dotnet-install.ps1
    LTS 채널의 최신 .NET SDK를 설치합니다.
.EXAMPLE
    .\dotnet-install.ps1 -Channel 8.0 -Version latest
    .NET 8.0의 최신 SDK를 설치합니다.
.EXAMPLE
    .\dotnet-install.ps1 -InstallDir "C:\dotnet" -Architecture x64
    지정된 디렉터리에 x64 아키텍처의 .NET SDK를 설치합니다.
#>

[CmdletBinding()]
param(
    [string]$Channel = "LTS",
    [string]$Version = "latest",
    [string]$InstallDir = "",
    [string]$Architecture = "",
    [string]$Runtime = "",
    [string]$Quality = "",
    [switch]$DryRun,
    [switch]$NoPath,
    [switch]$Help
)

# 도움말 표시
if ($Help) {
    Write-Host ".NET SDK 설치 스크립트 (Windows용)"
    Write-Host ""
    Write-Host "사용법:"
    Write-Host "  .\dotnet-install.ps1 [-Channel <CHANNEL>] [-Version <VERSION>] [-InstallDir <DIR>]"
    Write-Host "  .\dotnet-install.ps1 -Help"
    Write-Host ""
    Write-Host "옵션:"
    Write-Host "  -Channel <CHANNEL>      설치할 .NET 채널 (LTS, STS, 8.0, 9.0 등). 기본값: LTS"
    Write-Host "  -Version <VERSION>      설치할 특정 버전 (latest, 8.0.100 등). 기본값: latest"
    Write-Host "  -InstallDir <DIR>       설치 디렉터리. 기본값: `$env:USERPROFILE\.dotnet"
    Write-Host "  -Architecture <ARCH>    아키텍처 (x64, x86, arm64). 기본값: 자동 감지"
    Write-Host "  -Runtime <RUNTIME>      런타임만 설치 (dotnet, aspnetcore)"
    Write-Host "  -Quality <QUALITY>      품질 수준 (daily, preview, GA)"
    Write-Host "  -DryRun                 실제 설치 없이 다운로드 링크만 표시"
    Write-Host "  -NoPath                 PATH 환경 변수를 수정하지 않음"
    Write-Host "  -Help                   이 도움말 표시"
    Write-Host ""
    Write-Host "예제:"
    Write-Host "  .\dotnet-install.ps1"
    Write-Host "  .\dotnet-install.ps1 -Channel 8.0 -Version latest"
    Write-Host "  .\dotnet-install.ps1 -InstallDir 'C:\dotnet' -Architecture x64"
    exit 0
}

# 오류가 발생하면 즉시 스크립트를 중단합니다.
$ErrorActionPreference = "Stop"

# 임시 디렉터리 설정
$TempDir = [System.IO.Path]::GetTempPath()
$ScriptPath = Join-Path $TempDir "dotnet-install-official.ps1"

# Microsoft 공식 설치 스크립트 URL
$OfficialScriptUrl = "https://dot.net/v1/dotnet-install.ps1"

Write-Host "dotnet-install: .NET SDK 설치를 시작합니다..." -ForegroundColor Cyan

# 공식 설치 스크립트 다운로드
Write-Host "dotnet-install: Microsoft 공식 설치 스크립트를 다운로드합니다..." -ForegroundColor Cyan
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $OfficialScriptUrl -OutFile $ScriptPath -UseBasicParsing
} catch {
    Write-Error "dotnet-install: 오류: 설치 스크립트를 다운로드할 수 없습니다. 네트워크 연결을 확인하세요."
    exit 1
}

# 설치 인자 구성
$InstallArgs = @()

if (-not [string]::IsNullOrEmpty($Channel)) {
    $InstallArgs += "-Channel", $Channel
}

if (-not [string]::IsNullOrEmpty($Version)) {
    $InstallArgs += "-Version", $Version
}

if (-not [string]::IsNullOrEmpty($InstallDir)) {
    $InstallArgs += "-InstallDir", $InstallDir
}

if (-not [string]::IsNullOrEmpty($Architecture)) {
    $InstallArgs += "-Architecture", $Architecture
}

if (-not [string]::IsNullOrEmpty($Runtime)) {
    $InstallArgs += "-Runtime", $Runtime
}

if (-not [string]::IsNullOrEmpty($Quality)) {
    $InstallArgs += "-Quality", $Quality
}

if ($DryRun) {
    $InstallArgs += "-DryRun"
}

if ($NoPath) {
    $InstallArgs += "-NoPath"
}

Write-Host "dotnet-install: 설치 스크립트를 실행합니다..." -ForegroundColor Cyan

# 공식 스크립트 실행
try {
    & $ScriptPath @InstallArgs
    $ExitCode = $LASTEXITCODE
} catch {
    Write-Error "dotnet-install: 오류: 설치 중 오류가 발생했습니다. $_"
    exit 1
}

# 임시 파일 정리
if (Test-Path $ScriptPath) {
    Remove-Item $ScriptPath -Force -ErrorAction SilentlyContinue
}

if ($ExitCode -ne 0) {
    Write-Error "dotnet-install: 오류: 설치가 실패했습니다."
    exit $ExitCode
}

Write-Host ""
Write-Host "dotnet-install: 설치가 완료되었습니다." -ForegroundColor Green

# 설치 경로 안내
$FinalInstallDir = if ($InstallDir) { $InstallDir } else { "$env:USERPROFILE\.dotnet" }
Write-Host "dotnet-install: 설치 위치: $FinalInstallDir" -ForegroundColor Cyan

if (-not $NoPath -and -not $DryRun) {
    Write-Host "dotnet-install: 참고: PATH 환경 변수에 설치 경로를 추가하려면 다음 명령을 실행하세요:" -ForegroundColor Yellow
    Write-Host "  `$env:PATH = `"$FinalInstallDir;`$env:PATH`"" -ForegroundColor Yellow
}

Write-Host "dotnet-install: 참고: 이 스크립트는 CI(Continuous Integration) 시나리오를 위한 것입니다." -ForegroundColor Cyan
Write-Host "dotnet-install: 개발 환경 설정이나 앱 실행을 위해서는 공식 설치 프로그램을 사용하세요: https://dotnet.microsoft.com/download" -ForegroundColor Cyan
