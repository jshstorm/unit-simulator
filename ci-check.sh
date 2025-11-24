#!/bin/bash
# CI 검증 스크립트: 의존성 복원 및 프로젝트 빌드를 확인합니다.

set -e # 오류가 발생하면 즉시 스크립트를 중단합니다.

# 설치된 dotnet 실행 파일 경로 설정
DOTNET_PATH="$HOME/.dotnet/dotnet"

if [ ! -f "$DOTNET_PATH" ]; then
    echo "오류: dotnet을 찾을 수 없습니다. SDK가 설치되었는지 확인하세요."
    exit 1
fi

echo "--- 의존성 복원 시작 ---"
$DOTNET_PATH restore
echo "--- 의존성 복원 완료 ---"

echo ""

echo "--- 프로젝트 빌드 시작 ---"
$DOTNET_PATH build --no-restore
echo "--- 프로젝트 빌드 완료 ---"

echo ""
echo "CI 검증 성공: 프로젝트가 성공적으로 빌드되었습니다."
