# Dev Tool

> 이 디렉터리는 레거시/참조용으로 유지됩니다. 신규 개발 도구는 `tools/unit-dev-tool`을 참조하세요.

## 목표

- ReferenceModels에 정의된 모델 클래스를 Google Sheets와 바인딩하여 XML 직렬화된 게임 데이터를 생성
- 생성된 XML 데이터는 unit-simulator의 data-driven 소스로 사용
- TUI 기반의 개발 툴을 제공하여 개발 인프라 기능을 한 곳에서 실행
- Docker를 통해 Linux 기반의 동일한 실행 환경 제공

## 아키텍처

### 계층 분리

1. **Google Sheets 연동 계층**: Sheets API 접근 및 데이터 로드
2. **모델 매핑 계층**: ReferenceModels ↔ Sheet 매핑
3. **직렬화/출력 계층**: XML, bytes, 기타 포맷 변환

### 신규 프로젝트 구조 (`tools/unit-dev-tool`)

```
tools/unit-dev-tool/
├── UnitDevTool.csproj
├── Program.cs                 # 엔트리 포인트 (Spectre.Console TUI)
├── Commands/                  # 고수준 명령 ("데이터 시트 빌드" 등)
├── Sheets/                    # Google Sheets 연동
├── Mapping/                   # ReferenceModels ↔ Sheet 매핑
├── Serialization/             # XML 변환기
└── Config/                    # 설정 파일
```

## 핵심 플로우

1. Google Sheets 인증/접근 (서비스 계정 JSON 또는 OAuth)
2. 설정 파일에서 대상 스프레드시트 ID, 시트 이름, 매핑 정보 로드
3. 각 시트 데이터를 `SheetData`로 로드 → ReferenceModels 인스턴스로 변환
4. XML 직렬화하여 `unit-simulator`가 로드하는 경로에 저장
5. 필요시 bytes/json/filelist 등 부가 아티팩트 생성

## Data Sheet Download 흐름

### 설정 파일

`tools/unit-dev-tool/appsettings.json`:
- `GoogleSheets.CredentialsPath`: 서비스 계정 JSON 키 경로
- `GoogleSheets.Documents[]`: `{ Name, SpreadsheetId }`

### 시트-모델 매핑 규칙

- 각 Google 문서의 개별 시트는 ReferenceModels의 특정 모델 타입과 1:1로 이름 기반 매핑
- 여러 문서에 동일한 이름의 시트가 있으면 하나의 레코드로 취급 (병합 규칙 별도 정의)

### 실행 흐름

1. appsettings.json 로드 → `CredentialsPath`, `Documents` 목록 읽기
2. 선택된 문서의 `SpreadsheetId`로 GoogleSheetsService에서 데이터 로드
3. XmlConverter로 각 시트를 XML 파일로 저장
4. (향후) 시트 이름 ↔ ReferenceModels 타입 매핑으로 병합 로직 적용
5. (향후) 저장된 XML을 unit-simulator의 data-driven 소스로 사용

## Docker 기반 실행 환경

- **베이스 이미지**: .NET SDK + 런타임, Google API 클라이언트 의존성 포함
- **컨테이너 구성**: `unit-dev-tool` 바이너리, ReferenceModels DLL, 설정/크리덴셜 파일
- **실행**: `docker run ... unit-dev-tool` 명령으로 동일 환경에서 TUI 실행

## 재사용 대상 (dev-tool에서 발췌)

- Google Sheets 접근 코드 구조 (서비스 계층, 인증 방식)
- 시트 → 중간 모델(`SheetData`) 변환 로직
- XML/bytes/JSON 저장 유틸
- TUI 구성 패턴 (메뉴, 진행률 표시, 로그 출력)

## 향후 과제

- ReferenceModels와 시트 매핑 방식 결정 (JSON 설정 / C# Attribute / 하이브리드)
- unit-simulator에서 XML 데이터 로드하는 런타임 코드 인터페이스 정의
- Docker 이미지 구성 (빌드/런타임 분리, Google API 인증 방식)
