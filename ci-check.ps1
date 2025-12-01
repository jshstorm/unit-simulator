#!/usr/bin/env pwsh
# CI 검증 스크립트: 의존성 복원 및 프로젝트 빌드를 확인합니다.
# Windows PowerShell용 스크립트

# 오류가 발생하면 즉시 스크립트를 중단합니다.
$ErrorActionPreference = "Stop"

# 설치된 dotnet 실행 파일 경로 설정
# Windows에서는 일반적으로 %USERPROFILE%\.dotnet\dotnet.exe 또는 시스템 PATH에 있음
$DotnetPath = $null

# 1. 먼저 시스템 PATH에서 dotnet을 찾습니다
$DotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if ($DotnetCommand) {
    $DotnetPath = $DotnetCommand.Source
}

# 2. PATH에 없으면 사용자 프로필의 .dotnet 폴더에서 찾습니다
if (-not $DotnetPath) {
    $UserDotnetPath = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
    if (Test-Path $UserDotnetPath) {
        $DotnetPath = $UserDotnetPath
    }
}

# 3. DOTNET_ROOT 환경 변수가 설정되어 있으면 해당 경로에서 찾습니다
if (-not $DotnetPath -and $env:DOTNET_ROOT) {
    $EnvDotnetPath = Join-Path $env:DOTNET_ROOT "dotnet.exe"
    if (Test-Path $EnvDotnetPath) {
        $DotnetPath = $EnvDotnetPath
    }
}

# dotnet을 찾지 못한 경우 오류 출력 및 종료
if (-not $DotnetPath) {
    Write-Error "오류: dotnet을 찾을 수 없습니다. SDK가 설치되었는지 확인하세요."
    exit 1
}

Write-Host "dotnet 경로: $DotnetPath" -ForegroundColor Cyan

Write-Host ""
Write-Host "--- 의존성 복원 시작 ---" -ForegroundColor Green
& $DotnetPath restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "의존성 복원 실패"
    exit $LASTEXITCODE
}
Write-Host "--- 의존성 복원 완료 ---" -ForegroundColor Green

Write-Host ""

Write-Host "--- 프로젝트 빌드 시작 ---" -ForegroundColor Green
& $DotnetPath build --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "프로젝트 빌드 실패"
    exit $LASTEXITCODE
}
Write-Host "--- 프로젝트 빌드 완료 ---" -ForegroundColor Green

Write-Host ""
Write-Host "CI 검증 성공: 프로젝트가 성공적으로 빌드되었습니다." -ForegroundColor Green
