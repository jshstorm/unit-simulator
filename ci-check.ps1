<#
.SYNOPSIS
    CI 검증 스크립트: 의존성 복원 및 프로젝트 빌드를 확인합니다.
.DESCRIPTION
    이 스크립트는 .NET 설치 여부를 확인하고, 
    dotnet restore 및 dotnet build --no-restore 명령을 실행합니다.
#>

# 오류가 발생하면 즉시 스크립트를 중단합니다.
$ErrorActionPreference = "Stop"

# 시스템 PATH에서 dotnet 검색 (가장 일반적인 설치 위치)
$DotnetPath = $null
$DotnetCmd = Get-Command "dotnet" -ErrorAction SilentlyContinue
if ($DotnetCmd) {
    $DotnetPath = $DotnetCmd.Source
} else {
    # PATH에 없으면 사용자 프로필의 .dotnet 폴더에서 검색
    $UserDotnetPath = "$env:USERPROFILE\.dotnet\dotnet.exe"
    if (Test-Path $UserDotnetPath) {
        $DotnetPath = $UserDotnetPath
    }
}

if (-not $DotnetPath) {
    Write-Error "오류: dotnet을 찾을 수 없습니다. SDK가 설치되었는지 확인하세요."
    exit 1
}

Write-Host "--- 의존성 복원 시작 ---"
& $DotnetPath restore UnitSimulator.sln
if ($LASTEXITCODE -ne 0) {
    Write-Error "의존성 복원 실패"
    exit $LASTEXITCODE
}
Write-Host "--- 의존성 복원 완료 ---"

Write-Host ""

Write-Host "--- 프로젝트 빌드 시작 ---"
& $DotnetPath build UnitSimulator.sln --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "프로젝트 빌드 실패"
    exit $LASTEXITCODE
}
Write-Host "--- 프로젝트 빌드 완료 ---"

Write-Host ""
Write-Host "--- 테스트 실행 시작 ---"
& $DotnetPath test UnitSimulator.sln --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "테스트 실행 실패"
    exit $LASTEXITCODE
}
Write-Host "--- 테스트 실행 완료 ---"

Write-Host ""
Write-Host "CI 검증 성공: 프로젝트가 성공적으로 빌드 및 테스트되었습니다."
