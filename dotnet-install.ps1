#!/usr/bin/env pwsh
# .NET SDK/Runtime 설치 스크립트 (Windows PowerShell용)
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license.
#
# 이 스크립트는 Windows 플랫폼에서 .NET SDK 및 런타임을 설치합니다.
# 공식 Microsoft dotnet-install.ps1 스크립트를 다운로드하여 실행합니다.

[CmdletBinding()]
param (
    # 채널 (예: LTS, STS, 8.0, 9.0)
    [string]$Channel = "LTS",

    # 특정 버전 (예: latest, 8.0.100)
    [string]$Version = "Latest",

    # 설치 디렉터리 (기본값: %USERPROFILE%\.dotnet)
    [string]$InstallDir = "",

    # 아키텍처 (예: x64, x86, arm64)
    [string]$Architecture = "",

    # 런타임만 설치 (dotnet, aspnetcore)
    [string]$Runtime = "",

    # 품질 (daily, preview, GA)
    [string]$Quality = "",

    # Dry run 모드 (설치하지 않고 다운로드 링크만 표시)
    [switch]$DryRun,

    # PATH에 추가하지 않음
    [switch]$NoPath,

    # 상세 로그 출력
    [switch]$Verbose,

    # 도움말 표시
    [switch]$Help
)

# 오류가 발생하면 즉시 스크립트를 중단합니다.
$ErrorActionPreference = "Stop"

# 색상이 있는 메시지 출력 함수들
function Write-Info {
    param([string]$Message)
    Write-Host "dotnet-install: $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "dotnet-install: $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "dotnet-install: 경고: $Message" -ForegroundColor Yellow
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "dotnet-install: 오류: $Message" -ForegroundColor Red
}

# 도움말 표시
function Show-Help {
    Write-Host ".NET SDK/Runtime 설치 스크립트 (Windows PowerShell용)"
    Write-Host ""
    Write-Host "사용법:"
    Write-Host "    .\dotnet-install.ps1 [옵션]"
    Write-Host ""
    Write-Host "옵션:"
    Write-Host "    -Channel <CHANNEL>      다운로드할 채널 (기본값: LTS)"
    Write-Host "                            가능한 값: LTS, STS, 8.0, 9.0 등"
    Write-Host ""
    Write-Host "    -Version <VERSION>      특정 버전 설치 (기본값: Latest)"
    Write-Host "                            가능한 값: Latest, 8.0.100 등"
    Write-Host ""
    Write-Host "    -InstallDir <DIR>       설치 디렉터리 (기본값: %USERPROFILE%\.dotnet)"
    Write-Host ""
    Write-Host "    -Architecture <ARCH>    설치할 아키텍처"
    Write-Host "                            가능한 값: x64, x86, arm64"
    Write-Host ""
    Write-Host "    -Runtime <RUNTIME>      런타임만 설치 (SDK 제외)"
    Write-Host "                            가능한 값: dotnet, aspnetcore"
    Write-Host ""
    Write-Host "    -Quality <QUALITY>      품질 수준 (기본값: GA)"
    Write-Host "                            가능한 값: daily, preview, GA"
    Write-Host ""
    Write-Host "    -DryRun                 설치하지 않고 다운로드 링크만 표시"
    Write-Host ""
    Write-Host "    -NoPath                 현재 프로세스 PATH에 추가하지 않음"
    Write-Host ""
    Write-Host "    -Verbose                상세 로그 출력"
    Write-Host ""
    Write-Host "    -Help                   이 도움말 표시"
    Write-Host ""
    Write-Host "예제:"
    Write-Host "    # 최신 LTS 버전 설치"
    Write-Host "    .\dotnet-install.ps1"
    Write-Host ""
    Write-Host "    # 특정 버전 설치"
    Write-Host "    .\dotnet-install.ps1 -Version 8.0.100"
    Write-Host ""
    Write-Host "    # 특정 채널의 런타임만 설치"
    Write-Host "    .\dotnet-install.ps1 -Channel 8.0 -Runtime dotnet"
    Write-Host ""
    Write-Host "설치 위치:"
    Write-Host "    다음 순서로 설치 위치가 결정됩니다:"
    Write-Host "    1. -InstallDir 옵션"
    Write-Host "    2. DOTNET_INSTALL_DIR 환경 변수"
    Write-Host "    3. %USERPROFILE%\.dotnet"
}

# 도움말 요청 시 도움말 표시 후 종료
if ($Help) {
    Show-Help
    exit 0
}

Write-Info "Windows용 .NET 설치 스크립트를 시작합니다..."

# 설치 디렉터리 결정
if ([string]::IsNullOrEmpty($InstallDir)) {
    if ($env:DOTNET_INSTALL_DIR) {
        $InstallDir = $env:DOTNET_INSTALL_DIR
    } else {
        $InstallDir = Join-Path $env:USERPROFILE ".dotnet"
    }
}

Write-Info "설치 디렉터리: $InstallDir"

# 공식 Microsoft dotnet-install.ps1 스크립트 다운로드 URL
$OfficialScriptUrl = "https://dot.net/v1/dotnet-install.ps1"

# 임시 디렉터리에 공식 스크립트 다운로드
$TempDir = [System.IO.Path]::GetTempPath()
$TempScriptPath = Join-Path $TempDir "dotnet-install-official.ps1"

Write-Info "공식 Microsoft 설치 스크립트를 다운로드합니다..."
Write-Info "URL: $OfficialScriptUrl"

try {
    # TLS 1.2 사용 (보안 연결을 위해 필수)
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    # 스크립트 다운로드 (Invoke-WebRequest 사용)
    Invoke-WebRequest -Uri $OfficialScriptUrl -OutFile $TempScriptPath -UseBasicParsing
    Write-Success "스크립트 다운로드 완료"
}
catch {
    Write-ErrorMessage "공식 설치 스크립트 다운로드 실패: $_"
    exit 1
}

# 스크립트 실행을 위한 인자 구성
$ScriptArgs = @()

# Channel 인자
if (-not [string]::IsNullOrEmpty($Channel)) {
    $ScriptArgs += "-Channel"
    $ScriptArgs += $Channel
}

# Version 인자
if (-not [string]::IsNullOrEmpty($Version)) {
    $ScriptArgs += "-Version"
    $ScriptArgs += $Version
}

# InstallDir 인자
$ScriptArgs += "-InstallDir"
$ScriptArgs += $InstallDir

# Architecture 인자
if (-not [string]::IsNullOrEmpty($Architecture)) {
    $ScriptArgs += "-Architecture"
    $ScriptArgs += $Architecture
}

# Runtime 인자
if (-not [string]::IsNullOrEmpty($Runtime)) {
    $ScriptArgs += "-Runtime"
    $ScriptArgs += $Runtime
}

# Quality 인자
if (-not [string]::IsNullOrEmpty($Quality)) {
    $ScriptArgs += "-Quality"
    $ScriptArgs += $Quality
}

# DryRun 플래그
if ($DryRun) {
    $ScriptArgs += "-DryRun"
}

# NoPath 플래그
if ($NoPath) {
    $ScriptArgs += "-NoPath"
}

# Verbose 플래그
if ($Verbose) {
    $ScriptArgs += "-Verbose"
}

Write-Info "설치 시작..."
if ($Verbose) {
    Write-Info "실행 인자: $($ScriptArgs -join ' ')"
}

try {
    # 공식 스크립트 실행
    & $TempScriptPath @ScriptArgs

    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMessage "설치 실패 (종료 코드: $LASTEXITCODE)"
        exit $LASTEXITCODE
    }
}
catch {
    Write-ErrorMessage "설치 중 오류 발생: $_"
    exit 1
}
finally {
    # 임시 스크립트 파일 정리 (정리 실패는 무시)
    if (Test-Path $TempScriptPath) {
        Remove-Item $TempScriptPath -Force -ErrorAction Ignore
    }
}

# 설치 완료 후 경로 안내
$DotnetExePath = Join-Path $InstallDir "dotnet.exe"
if (Test-Path $DotnetExePath) {
    Write-Success "설치 완료!"
    Write-Info "dotnet 실행 파일 경로: $DotnetExePath"

    if (-not $NoPath) {
        # 현재 세션의 PATH에 추가 (경로 분리하여 정확히 확인)
        $PathEntries = $env:PATH -split [IO.Path]::PathSeparator
        if ($PathEntries -notcontains $InstallDir) {
            $env:PATH = "$InstallDir$([IO.Path]::PathSeparator)$env:PATH"
            Write-Info "현재 세션의 PATH에 추가됨: $InstallDir"
        }
    }

    Write-Host ""
    Write-Info "참고: 새 터미널 세션에서 dotnet을 사용하려면 PATH 환경 변수에 다음을 추가하세요:"
    Write-Host "    $InstallDir" -ForegroundColor Yellow
} else {
    if (-not $DryRun) {
        Write-Warning "dotnet.exe를 찾을 수 없습니다. 설치가 완료되지 않았을 수 있습니다."
    }
}

Write-Host ""
Write-Info "참고: 이 스크립트는 지속적 통합(CI) 시나리오를 위해 설계되었습니다."
Write-Info "개발 환경 설정이나 앱 실행을 위해서는 공식 설치 프로그램을 사용하세요."
Write-Info "공식 설치 프로그램: https://dotnet.microsoft.com/download"
