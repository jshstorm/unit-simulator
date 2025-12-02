# Unit Dev Tool Plan

## 방향성
- 기존 `dev-tool` 디렉터리는 레거시/참조용으로 유지한다.
- `tools/unit-dev-tool` (가칭) 같은 별도 디렉터리에 신규 TUI 기반 개발 툴 프로젝트를 만든다.
- 신규 툴은 ReferenceModels ↔ Google Sheets 바인딩 및 XML 직렬화된 게임 데이터 생성에 필요한 최소 기능만 포함한다.

## 1. 신규 프로젝트 구조 (안)
- `tools/unit-dev-tool/`
  - `UnitDevTool.csproj`
  - `Program.cs` (엔트리 포인트, Spectre.Console 기반 TUI/메뉴 구성)
  - `Commands/` ("데이터 시트 빌드" 등 고수준 명령)
  - `Sheets/` (Google Sheets 연동, `GoogleSheetsService`, `SheetData` 등)
  - `Mapping/` (ReferenceModels ↔ Sheet 매핑 정의/로직)
  - `Serialization/` (`XmlConverter` 및 기타 포맷 변환기)
  - `Config/` (시트 ID, 매핑 설정, 출력 경로 등을 담는 설정 파일)

## 2. 핵심 플로우
1) Google Sheets 인증/접근: 서비스 계정 JSON 또는 OAuth를 사용해 Sheets API에 접근.
2) 설정 파일에서 대상 스프레드시트 ID와 시트 이름, ReferenceModels 타입 및 컬럼 매핑 정보를 읽는다.
3) 각 시트 데이터를 `SheetData`로 로드한 뒤, 매핑 정보를 이용해 ReferenceModels 인스턴스 리스트로 변환한다.
4) 변환된 모델들을 XML로 직렬화하여 `unit-simulator`가 로드하는 경로(예: `Data/Generated` 등)에 저장한다.
5) 필요시 bytes/json/filelist 등 부가 아티팩트도 생성한다.

## 3. dev-tool로부터 발췌할 요소
- Google Sheets 접근 코드 구조 (서비스 계층, 인증 방식)
- 시트 → 중간 모델(`SheetData` 유사) 변환 로직
- XML/bytes/JSON로 저장하는 유틸(필요한 최소 집합만)
- TUI 구성 패턴(메뉴, 진행률 표시, 로그 출력 등)

## 4. Docker 기반 실행 환경
- 베이스 이미지: .NET SDK + 런타임, Google API 클라이언트에 필요한 의존성 포함.
- 컨테이너 내에 `unit-dev-tool` 바이너리, ReferenceModels DLL, 설정/크리덴셜 파일을 배치.
- 개발자는 로컬 OS에 관계없이 `docker run ... unit-dev-tool` 명령으로 동일한 환경에서 TUI를 실행.

## 5. 다음 단계
- `tools/unit-dev-tool` 디렉토리 생성 및 최소 csproj/Program.cs 스켈레톤 추가.
- dev-tool에서 재사용할 코드(예: XmlConverter, GoogleSheetsService 패턴)를 확인하고, 의존성 없이 옮길 수 있도록 분리 설계.
- ReferenceModels와 시트 매핑 방식을 (JSON 설정 / C# Attribute / 하이브리드) 중 하나로 선택.
