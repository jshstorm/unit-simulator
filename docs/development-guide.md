# 개발 인프라 및 GUI 연동 가이드

이 문서는 Unit Simulator 코어와 웹 기반 Sim Studio(편집 기능 포함)를 유지·확장할 때의 구조, 규칙, 그리고 실행 방법을 정리합니다. 기존 영문 내용을 한국어로 옮기고, 최근 실시간 플레이/일시정지(Play/Pause) 흐름을 반영했습니다.

## 개요

- **코어/렌더링 분리**: `SimulatorCore`는 시뮬레이션 계산과 상태 관리만 담당하고, PNG 렌더링은 선택적(`RenderingEnabled`)입니다.
- **프레임 데이터 우선**: 각 스텝은 JSON 형태의 `FrameData`를 생성하며, GUI·디버깅·재생에 활용합니다.
- **확장성**: 콜백 인터페이스(`ISimulatorCallbacks`)를 통해 외부 툴이나 GUI가 이벤트를 구독하고 상태를 주입할 수 있습니다.

## 아키텍처 원칙

### 관심사 분리

- **시뮬레이션 로직 (코어)**: `SimulatorCore`가 유닛 행동, 웨이브, 상태를 모두 관리합니다.
- **프레임 데이터 생성**: 렌더링과 무관하게 JSON 상태(`FrameData`)를 생성합니다.
- **렌더링**: `Renderer`가 이미지(PNG)를 생성하며, 헤드리스 모드에서는 건너뜁니다.

### 핵심 컴포넌트

- **SimulatorCore**
  - 상태 유지(유닛, 웨이브 등)
  - 매 스텝 프레임 데이터 생성
  - 콜백 지원으로 외부 연동
  - JSON 상태 로드/주입, 런타임 수정 지원
- **FrameData**
  - 프레임 번호, 웨이브 정보, 유닛 상태 전체를 담은 직렬화 가능한 구조체
- **콜백 인터페이스 (`ISimulatorCallbacks`)**
  - `OnFrameGenerated`, `OnSimulationComplete`, `OnStateChanged`, `OnUnitEvent`

### 프레임 데이터 vs 렌더링

- **프레임 데이터(JSON)**: 가볍고 빠르며 디버깅, 상태 보존/재생, 외부 시각화, 회귀 테스트에 사용
- **렌더링(PNG)**: 시각화/데모용, 리소스 소모가 크므로 필요 시에만 활성화

## WebSocket 서버 & GUI 연동

- `Program --server`가 `WebSocketServer`를 띄우고, 렌더링은 기본 비활성화(`RenderingEnabled = false`)됩니다.
- 엔드포인트:
  - WebSocket: `ws://localhost:{port}/ws`
  - 헬스 체크: `http://localhost:{port}/health`
- **재생 루프**: `start` 명령 시 약 30fps 간격으로 `SimulatorCore.Step()`을 반복하며, 매 프레임을 GUI에 브로드캐스트합니다.
- **일시정지/정지**: `stop` 명령은 재생 루프를 중단하고 코어를 멈춥니다. `reset`은 중단 후 초기화합니다.
- **단일 스텝/역스텝/시킹**: `step`은 재생 중 눌러도 일시정지 후 1프레임 진행합니다. `step_back`은 프레임 히스토리(최대 약 5,000프레임)를 사용해 한 프레임 이전으로 이동합니다. `seek`은 프레임 번호로 점프하며, 히스토리에 없으면 목표까지 앞으로 계산합니다.
- **브로드캐스트 정책**: 모든 프레임을 전송하여 GUI가 실시간으로 갱신됩니다(`OnFrameGenerated`에서 즉시 송신).

### 명령 프로토콜(요약)

- `start`: 재생 시작(30fps 루프)
- `stop`: 재생/시뮬레이션 중단
- `step`: 1프레임 진행(재생 중 눌러도 일시정지 후 1프레임 진행)
- `step_back`: 1프레임 이전 상태로 이동(히스토리 기반)
- `reset`: 초기화 후 첫 프레임 상태로 복귀
- `seek`: 특정 프레임 번호로 점프(히스토리 내 즉시 로드, 부족 시 앞으로 계산)
- `move`/`set_health`/`kill`/`revive`: 특정 유닛 제어·수정
- **캔버스 상호작용**: 드래그 패닝, 휠 줌 인/아웃, 클릭 이동/선택은 패닝/줌을 반영한 월드 좌표 기준으로 처리.

## Sim Studio(웹, Vite + React)

- 기본 WebSocket 주소: `ws://localhost:5000/ws` (`sim-studio/src/App.tsx`에서 변경 가능)
- **컨트롤**: Play/Pause, Step(재생 중 눌러도 일시정지 후 1프레임 진행), Step Back(한 프레임 이전으로), Seek(프레임 번호로 점프), Reset. 비디오 플레이어처럼 동작합니다.
- **캔버스 내 네비게이션**: 드래그로 패닝, 마우스 휠로 줌 인/아웃(MIN_ZOOM~MAX_ZOOM). 패닝/줌은 클릭 이동/유닛 선택 등 모든 상호작용에 반영되며, 그리드는 뷰포트 범위에 맞춰 동적으로 이어서 그려집니다.
- **실행**:
  1. `dotnet run --project UnitMove -- --server --port 5000`
  2. `cd sim-studio && npm install && npm run dev`
  3. 브라우저 `http://localhost:5173` 접속 → 상단 상태가 Connected면 성공

## 상태 주입/재생 예시

```csharp
// JSON에서 상태 복원
var frameData = FrameData.LoadFromJsonFile("output/debug/frame_0100.json");
var simulator = new SimulatorCore();
simulator.LoadState(frameData);
simulator.Run(callbacks);

// 런타임 상태 수정
simulator.ModifyUnit(unitId, faction, unit => {
    unit.HP = 50;
    unit.CurrentDestination = new Vector2(300, 200);
});
```

## 개발 가이드라인

1. **코어 로직 추가**: `SimulatorCore`/AI/웨이브 등 도메인 클래스에 구현.
2. **프레임 데이터 확장**: 새 상태가 필요하면 `FrameData`/직렬화 추가.
3. **콜백 확장**: 외부 연동이 필요하면 `ISimulatorCallbacks`에 이벤트 추가.
4. **문서 갱신**: 새로운 연동점이나 흐름을 이 문서에 반영.

## 길찾기 랜덤 테스트 도구

유닛 길찾기(A*) 동작을 검증하기 위해 랜덤 장애물 맵과 시작/목표 지점을 생성하는 테스트 러너가 추가되었습니다.

- 설정: `UnitSimulator.Core/Pathfinding/PathfindingTestSettings.cs`
- 실행 로직: `UnitSimulator.Core/Pathfinding/PathfindingTestRunner.cs`
- 결과/리포트: `UnitSimulator.Core/Pathfinding/PathfindingTestReport.cs`

### 사용 예시

```csharp
var settings = new PathfindingTestSettings
{
    Seed = 1234,
    ObstacleDensity = 0.2f,
    ScenarioCount = 50
};

var runner = new PathfindingTestRunner();
var report = runner.Run(settings);
report.SaveToJson("output/pathfinding-report.json");
```

### 결과 확인 포인트

- `report.Results`: 시나리오별 경로 성공 여부, 길이, 노드 수 등 개별 지표
- `report.Summary`: 성공률 및 평균 통계
- `report.Obstacles`: 생성된 장애물 사각형 목록 (시각화 연동용)

### 성능/테스트

- 렌더링은 필요 시에만 켜고, 회귀는 JSON 프레임으로 비교.
- 콜백 오버헤드를 줄이기 위해 필요한 이벤트만 송신.

## 품질 보증 (Quality Assurance)

이 프로젝트는 GitHub Actions를 사용한 CI 파이프라인으로 빌드 무결성을 검증합니다. 로컬에서 확인하려면 아래 스크립트를 사용하세요.

### Linux/macOS

```bash
./ci-check.sh
```

### Windows (PowerShell)

```powershell
.\ci-check.ps1
```

## .NET SDK 설치

CI 환경이나 자동 설치가 필요하면 아래 스크립트를 사용할 수 있습니다.

### Linux/macOS

```bash
./dotnet-install.sh --channel LTS
```

### Windows (PowerShell)

```powershell
.\dotnet-install.ps1 -Channel LTS
```

**주요 옵션:**
- `-Channel`: 설치할 .NET 채널 (LTS, STS, 8.0, 9.0 등)
- `-Version`: 설치할 특정 버전 (latest, 8.0.100 등)
- `-InstallDir`: 설치 디렉터리
- `-Architecture`: 아키텍처 (x64, x86, arm64)
- `-Help`: 전체 도움말 표시

> **참고**: 일반 개발 환경에는 [.NET 공식 웹사이트](https://dotnet.microsoft.com/download) 설치 프로그램 사용을 권장합니다.

## 파일 구조(요약)

```
UnitMove/
├── SimulatorCore.cs        # 핵심 시뮬레이션 엔진
├── FrameData.cs            # 프레임 데이터/직렬화
├── ISimulatorCallbacks.cs  # 콜백 인터페이스
├── WebSocketServer.cs      # WebSocket 서버 (재생 루프 포함)
├── Renderer.cs             # PNG 렌더링 (옵션)
├── EnemyBehavior.cs, SquadBehavior.cs, AvoidanceSystem.cs 등
└── Constants.cs            # 설정 상수
sim-studio/                 # Vite + React GUI (Play/Pause/Step/Reset 컨트롤)
```

## 버전 히스토리(요약)

- 초안: 코어/렌더링 분리, 프레임 데이터/콜백, GUI 연동 플레이스홀더.
- 최신: WebSocket 서버에 실시간 재생 루프 도입, 프레임 전체 브로드캐스트, GUI Play/Pause/Step/Reset UX 추가.
- 최신(업데이트1): Step 중 자동 일시정지, Step Back, Seek(프레임 점프) 지원, 프레임 히스토리 최대 약 5,000프레임 유지.
- 최신(업데이트2): 캔버스 패닝(드래그), 줌 인/아웃(휠) 지원. 클릭 이동/선택은 현재 뷰(패닝/줌) 기준 월드 좌표로 처리.
