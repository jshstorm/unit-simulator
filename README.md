[![.NET CI](https://github.com/clover-storm/unit-simulator/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/clover-storm/unit-simulator/actions/workflows/dotnet-ci.yml)

# UnitMove 시뮬레이션

이 프로젝트는 유닛들의 전투 상황을 시뮬레이션하는 프로그램입니다. 두 개의 분대가 서로 전투를 벌이는 동안의 움직임, 위치 선정, 그리고 전투 로직을 시각화하여 일련의 이미지 프레이으로 생성합니다. 이 시뮬레이션은 충돌 회피, 목표물을 둘러싸기 위한 공격 슬롯 관리, 대형 유지 등과 같은 고급 개념을 구현한 예제입니다.

## 주요 기능

*   **예측 기반 충돌 회피**: 유닛들은 다른 유닛과의 미래 충돌을 예측하고 이를 피하기 위해 경로를 동적으로 수정합니다.
*   **공격 슬롯 시스템**: 근접 유닛이 목표물을 효과적으로 둘러싸고 공격할 수 있도록 대상 주변의 공격 위치를 점유하고 관리합니다. 이를 통해 유닛들이 뭉치지 않고 효율적인 공격 대형을 형성할 수 있습니다.
*   **분대 행동 로직**:
    *   **전투 시**: 각 유닛은 가장 가까운 적을 목표로 삼고, 최적의 공격 위치로 이동하여 교전합니다.
    *   **비전투 시**: 적이 없을 경우, 분대는 리더를 중심으로 지정된 대형을 유지하며 목표 지점으로 이동합니다.
*   **다중 웨이브**: 시뮬레이션은 여러 웨이브의 적들을 순차적으로 생성하여, 모든 웨이브를 성공적으로 막아내는 것을 목표로 합니다.
*   **시각화**: 전체 시뮬레이션 과정은 이미지 프레임으로 렌더링되어 `output` 폴더에 저장됩니다. 이를 통해 각 유닛의 움직임과 전투 상황을 시각적으로 분석할 수 있습니다.
*   **Google Sheets to XML 변환**: Google Sheets 스프레드시트의 데이터를 XML 파일로 변환하는 기능을 제공합니다. CLI를 통해 간편하게 사용할 수 있습니다.
*   **세션 디버깅 로깅**: WebSocket 서버 세션의 모든 이벤트, 명령, 상태 변경을 자동으로 기록하여 JSON 형식으로 저장합니다. 디버깅 및 분석에 유용합니다. 자세한 내용은 [세션 로깅 문서](docs/session-logging.md)를 참조하세요.

## 시작하기

### 전제 조건

이 프로젝트를 실행하기 위해서는 .NET SDK가 설치되어 있어야 합니다. .NET 공식 웹사이트에서 다운로드하여 설치할 수 있습니다.

### 설치

프로젝트의 의존성을 설치하기 위해, 프로젝트의 루트 디렉터리에서 다음 명령어를 실행하세요:

```bash
dotnet restore
```

## 사용법

시뮬레이션을 실행하려면, 프로젝트의 루트 디렉터리에서 다음 명령어를 실행하세요:

```bash
dotnet run
```

실행이 완료되면, `output` 디렉터리에 시뮬레이션의 각 프레임이 `frame_xxxx.png` 형식의 이미지 파일로 저장됩니다.

### 웹 대시보드(GUI) 실행

실시간 WebSocket 서버와 웹 기반 GUI를 함께 실행하여 대시보드를 띄울 수 있습니다.

**전제 조건**
- .NET SDK 설치
- Node.js/npm 설치

**실행 순서**
1) WebSocket 서버 실행:
```bash
dotnet run --project UnitMove -- --server --port 5000
```

2) GUI 실행:
```bash
cd sim-studio
npm install
npm run dev
```

3) 브라우저 접속:
- `http://localhost:5173` (상단 상태가 Connected이면 성공)

**참고**
- 기본 WebSocket 주소: `ws://localhost:5000/ws`
- 필요 시 `sim-studio/src/App.tsx`에서 변경 가능합니다.

### 동영상 생성

프레임 이미지를 동영상으로 변환하는 방법은 문서를 참고하세요:
- `docs/video-export.md`

### Google Sheets to XML 변환

Google Sheets 스프레드시트의 데이터를 XML 파일로 변환하는 방법은 문서를 참고하세요:
- `docs/sheet-to-xml.md`

## 의존성

이 프로젝트는 다음의 .NET 라이브러리를 사용합니다:

*   **SixLabors.ImageSharp**: 이미지 생성 및 처리를 위한 라이브러리입니다.
*   **SixLabors.ImageSharp.Drawing**: 이미지에 도형과 텍스트를 그리기 위한 라이브러리입니다.
*   **Google.Apis.Sheets.v4**: Google Sheets API 접근을 위한 라이브러리입니다.

이 라이브러리들은 `dotnet restore` 명령어를 통해 자동으로 설치됩니다.

## 품질 보증 (Quality Assurance)

CI 및 로컬 빌드 확인 방법은 개발 가이드를 참고하세요:
- `docs/development-guide.md`

## 문서

| 문서 | 설명 |
|------|------|
| [시뮬레이션 스펙](docs/simulation-spec.md) | 유닛 행동 규칙, 전투 로직, 회피 시스템 등 도메인 스펙 |
| [개발 가이드](docs/development-guide.md) | 개발 인프라, WebSocket 서버, GUI 연동 가이드 |
| [Sim Studio](docs/sim-studio.md) | 웹 기반 시뮬레이션 스튜디오 |
| [Sheet to XML](docs/sheet-to-xml.md) | Google Sheets → XML 변환 사용법 |
| [동영상 생성](docs/video-export.md) | 프레임 이미지 → 동영상 변환 |
| [세션 로깅](docs/session-logging.md) | WebSocket 세션 디버깅 로깅 기능 |
| [ReferenceModels](ReferenceModels/README.md) | Google Sheets ↔ C# 모델 매핑 규칙 |
| [Dev Tool](dev-tool/README.md) | 개발 도구 설계 및 계획 |
