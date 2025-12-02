# Dev Tool Design

## 목표
- ReferenceModels에 정의된 모델 클래스를 Google Sheets와 바인딩하여 XML 직렬화된 게임 데이터를 생성한다.
- 생성된 XML 데이터는 unit-simulator의 data-driven 소스로 사용한다.
- TUI 기반(dev-tool 스타일)의 개발 툴을 제공하여, 개발 인프라 기능을 한 곳에서 실행할 수 있게 한다.
- 개발자의 로컬 OS/환경과 무관하게, Docker를 통해 Linux 기반의 동일한 실행 환경을 제공한다.

## 요구사항 개요
- TUI 메뉴 구조를 통해 주요 기능(데이터 시트 빌드, 테스트 실행, 배포 유틸 등)을 선택/실행할 수 있다.
- ReferenceModels의 모델 타입과 Google Sheets 컬럼/시트 구조를 매핑할 수 있는 설정/메타데이터를 정의한다.
- Google Sheets로부터 데이터를 가져와 ReferenceModels 인스턴스로 변환한 뒤, XML 직렬화하여 출력 디렉터리에 저장한다.
- dev-tool의 기존 Google Sheets 연동/빌드 로직에서 필요한 부분만 발췌하여 사용하고, 불필요한 기능은 제거 또는 비활성화한다.
- Docker를 통해 동일한 실행 이미지를 제공하며, dev-tool 바이너리와 필요한 스크립트/설정을 포함한다.

## 아키텍처 방향
- 단일 dev-tool 프로젝트 안에서 필요한 부분만 정리해서 사용하는 방안과, 별도의 디렉터리/신규 프로젝트를 만들어 필수 기능만 옮기는 방안을 비교/검토한다.
- 현재 선택: `tools/unit-dev-tool`에 신규 TUI 툴을 만들고, 기존 dev-tool은 참조용으로 유지한다.
- Google Sheets 연동 계층, 모델 매핑 계층(ReferenceModels ↔ Sheet), 직렬화/출력 계층(XML, bytes, 기타 포맷)을 명확히 분리한다.
- 메뉴/CLI 계층은 위의 기능들을 조합하여 "데이터 시트 빌드" 등의 고수준 작업을 제공한다.

### Data Sheet Download 흐름(초기 구현)
- 설정 파일: `tools/unit-dev-tool/appsettings.json`에 Google Sheets 크레덴셜 경로와 대상 문서 목록을 정의한다.
  - 예시 구조:
    - `GoogleSheets.CredentialsPath`: 서비스 계정 JSON 키 경로
    - `GoogleSheets.Documents[]`: `{ Name, SpreadsheetId }`
- 시트-모델 매핑 규칙:
  - 각 Google 문서의 개별 시트는 ReferenceModels의 특정 모델 타입과 1:1로 이름 기반 매핑된다.
  - 여러 문서에 동일한 이름의 시트가 있으면, 이들은 "하나의 레코드"로 취급되며, 매핑/머지 규칙(예: 우선순위, 병합 전략)은 별도 규칙으로 정의한다.
- 메뉴: UnitDevTool TUI에서 "Data Sheet Download" 메뉴를 선택하면, appsettings.json에 정의된 문서 목록 중 하나를 선택하도록 한다.
- 실행 흐름:
  1) appsettings.json을 로드하여 `CredentialsPath`, `Documents` 목록을 읽는다.
  2) 선택된 문서의 `SpreadsheetId`를 사용해 GoogleSheetsService로 전체 시트 데이터를 가져온다.
  3) XmlConverter를 이용해 각 시트를 XML 파일로 저장한다.
  4) (향후) 시트 이름 ↔ ReferenceModels 타입 이름 매핑을 통해, 여러 문서에 같은 시트 이름이 있을 경우 하나의 논리 레코드로 취급하는 병합 로직을 적용한다.
  5) (향후) 저장된 XML 파일을 ReferenceModels 기반 런타임 데이터 로더가 unit-simulator의 data-driven 소스로 사용한다.
- 구현 위치:
  - Google Sheets/Xml 변환 로직: `GoogleSheets/GoogleSheetsService.cs`, `GoogleSheets/XmlConverter.cs`를 재사용.
  - TUI 및 환경 설정 처리: `tools/unit-dev-tool/Program.cs`, `tools/unit-dev-tool/appsettings.json`.

## 향후 논의 필요 사항
- dev-tool를 그대로 정리/리팩터링할지, 신규 경량 프로젝트를 만들지 결정.
- ReferenceModels와 시트 구조 매핑을 어떤 형식(JSON, attribute, 별도 설정 파일 등)으로 표현할지.
- unit-simulator에서 XML 데이터를 로드하는 런타임 코드의 인터페이스 및 경로 구조.
- Docker 이미지 구성(베이스 이미지, 빌드/런타임 분리 여부, Google API 인증 방식 등).
