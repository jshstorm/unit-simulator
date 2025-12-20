# Google Sheets → XML 변환

Google Sheets 스프레드시트의 데이터를 XML 파일로 변환하려면 `sheet-to-xml` 명령어를 사용합니다.

## 사용법

```bash
dotnet run -- sheet-to-xml --sheet-id <SPREADSHEET_ID> --output <OUTPUT_DIR>
```

## 옵션

- `--sheet-id, -s <ID>`: Google Spreadsheet ID (필수, URL에서 확인 가능)
- `--output, -o <DIR>`: XML 파일을 저장할 디렉터리 (기본값: ./xml_output)
- `--credentials, -c <PATH>`: Google 서비스 계정 인증 JSON 파일 경로 (기본값: GOOGLE_APPLICATION_CREDENTIALS 환경 변수 또는 ./credentials.json)
- `--help, -h`: 도움말 표시

## 예시

```bash
# 기본 사용법
dotnet run -- sheet-to-xml -s 1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms

# 출력 디렉터리 및 인증 파일 지정
dotnet run -- sheet-to-xml -s 1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms -o ./my_xml -c ./my-credentials.json
```

## 사전 준비

1. [Google Cloud Console](https://console.cloud.google.com/)에서 프로젝트 생성
2. Google Sheets API 활성화
3. 서비스 계정 생성 및 JSON 키 파일 다운로드
4. 스프레드시트에 서비스 계정 이메일 공유 권한 부여

서비스 계정 인증 파일 템플릿은 `credentials.example.json`을 참조하세요.
