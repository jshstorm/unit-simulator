# 클래시 로얄 유닛 움직임 구현 계획

이 문서는 클래시 로얄 스타일의 유닛 움직임을 구현하기 위한 단계별 개발 계획을 기술합니다.
각 페이즈는 독립적으로 구현, 테스트, 커밋되는 것을 원칙으로 합니다.

---

## Phase 1: 기본 토대 구축 - 정적 장애물 회피 (A\* 경로탐색 활성화)

**목표:** 유닛이 타워, 강과 같은 정적 장애물을 인지하고 A\* 알고리즘을 통해 우회하도록 합니다.

### **1.1. `PathfindingGrid`에 정적 장애물 정보 등록**

-   **수정 파일:** `UnitSimulator.Core/SimulatorCore.cs`
-   **수정 메소드:** `Initialize()`
-   **구현 상세:**
    1.  `_pathfindingGrid = new PathfindingGrid(...)` 코드 직후에 로직을 추가합니다.
    2.  `_gameSession.FriendlyTowers`와 `_gameSession.EnemyTowers` 목록을 순회합니다.
    3.  각 타워의 `Position`과 `Radius`를 가져옵니다.
    4.  타워의 영향권에 포함되는 모든 그리드 노드를 `_pathfindingGrid.SetObstacle(tower.Position, tower.Radius)`와 같은 헬퍼 메소드를 만들어 비활성화(`walkable = false`)합니다.
    5.  `GameConstants`에 정의된 맵의 강(River) 영역 좌표를 기반으로, 다리를 제외한 모든 강 영역의 그리드 노드를 비활성화합니다.

### **1.2. 유닛 행동 로직에 A\* 경로탐색 결과 연동**

-   **수정 파일 1:** `UnitSimulator.Core/Unit.cs`
    -   **구현 상세:** 유닛의 경로 추적을 위한 상태 변수를 추가합니다.
      ```csharp
      public List<Vector2> PathWaypoints { get; set; } = new List<Vector2>();
      public int CurrentWaypointIndex { get; set; } = -1;
      ```

-   **수정 파일 2:** `UnitSimulator.Core/SquadBehavior.cs`, `UnitSimulator.Core/EnemyBehavior.cs`
    -   **수정 메소드:** 유닛의 목적지가 설정되거나 갱신되는 로직 (예: `UpdateFriendlySquad` 내부의 타겟 할당 부분)
    -   **구현 상세:**
        1.  유닛이 새로운 목표를 할당받아 목적지가 정해지면, 기존처럼 `unit.CurrentDestination`을 직접 설정하지 않습니다.
        2.  대신 `simulator.Pathfinder.FindPath(unit.Position, targetPosition)`를 호출하여 `List<Vector2>` 형태의 경로(경유지)를 받아옵니다.
        3.  경로가 성공적으로 반환되면(`path != null && path.Count > 0`), 유닛의 `PathWaypoints`에 저장하고 `CurrentWaypointIndex`를 0으로 설정합니다.
        4.  유닛의 이동 로직을 수정합니다. `CurrentWaypointIndex`가 유효하면, `PathWaypoints[CurrentWaypointIndex]`를 현재 프레임의 목적지로 사용합니다.
        5.  현재 경유지에 충분히 가까워지면 `CurrentWaypointIndex`를 1 증가시켜 다음 경유지를 따라가도록 합니다. 모든 경유지를 통과하면 `CurrentWaypointIndex`를 -1로 리셋합니다.

### **1.3. 검증 계획**

-   **방법:** `UnitSimulator.Server/Program.cs` 또는 테스트용 프로젝트에서 특정 시나리오를 코드로 구성하여 실행하고 로그를 확인합니다.
-   **시나리오:**
    1.  `SimulatorCore` 초기화 시, 맵 중앙(예: 1600, 2550)에 반지름 200짜리 가상 장애물을 `_pathfindingGrid`에 수동으로 추가합니다.
    2.  아군 유닛 하나를 (1600, 1500)에 스폰하고, 목적지를 (1600, 3600)으로 설정합니다.
    3.  시뮬레이션을 500프레임 정도 실행하며, 매 프레임 유닛의 `Position.X`와 `Position.Y` 좌표를 콘솔에 출력합니다.
-   **성공 기준:** 유닛의 X 좌표가 1600에서 시작하여, 장애물을 피하기 위해 좌측 또는 우측으로 벗어났다가 다시 1600에 가까워지는 로그가 확인되어야 합니다.

---

## Phase 2: 동적 상호작용 - 유닛 간 충돌 회피

**목표:** 스티어링 로직을 도입하여 유닛들이 서로 부딪히지 않고 자연스럽게 비켜가도록 합니다.

### **2.1. `AvoidanceSystem` 활성화 및 스티어링 로직 통합**

-   **수정 파일 1:** `UnitSimulator.Core/SimulatorCore.cs`
    -   **구현 상세:**
        1.  `private readonly AvoidanceSystem _avoidanceSystem = new();` 필드를 추가합니다.
        2.  `Step()` 메소드 내 `_squadBehavior.Update...` 호출 직전에, 모든 유닛 목록을 `_avoidanceSystem`에 전달하여 회피 벡터를 계산하는 로직을 추가합니다. `_avoidanceSystem.Update(allUnits)`
-   **수정 파일 2:** `UnitSimulator.Core/Unit.cs`
    -   **구현 상세:** 회피 계산 결과를 저장할 필드를 추가합니다.
      ```csharp
      public Vector2 AvoidanceVector { get; set; } = Vector2.Zero;
      ```
-   **수정 파일 3:** `UnitSimulator.Core/SquadBehavior.cs` (및 `EnemyBehavior.cs`)
    -   **구현 상세:** 유닛의 최종 속도를 결정하는 로직을 '스티어링 모델'로 변경합니다.
        1.  **(기존) 경로 추종 벡터:** `PathWaypoints`의 현재 경유지를 향하는 방향 벡터.
        2.  **(신규) 회피 벡터:** `unit.AvoidanceVector` 값.
        3.  **(신규) 분리 벡터:** 주변의 '아군' 유닛을 너무 가까우면 밀어내는 힘. `SquadBehavior` 내에서 근접한 아군을 찾아 반대 방향 벡터를 계산합니다.
        4.  `steering = (pathVector * 1.0f) + (avoidanceVector * 1.5f) + (separationVector * 0.8f)` 와 같이 각 벡터에 가중치를 부여하여 최종 스티어링 방향을 계산합니다.
        5.  이 `steering` 방향으로 최종 `Velocity`를 설정합니다.

### **2.2. 검증 계획**

-   **시나리오:**
    1.  장애물이 없는 빈 맵에서 두 유닛을 500 유닛 거리에서 서로 마주보게 소환하고, 서로의 위치를 목적지로 설정합니다.
    2.  시뮬레이션을 실행하며 두 유닛의 X, Y 좌표를 로그로 남깁니다.
-   **성공 기준:** 두 유닛의 경로가 중앙에서 충돌하지 않고, 부드러운 곡선을 그리며 서로를 비껴가는 로그가 확인되어야 합니다.

---

## Phase 3: 그룹 움직임 고도화 - 물리적 충돌 처리

**목표:** 유닛들이 서로를 통과하지 못하고, 겹쳤을 때 물리적으로 밀어내도록 하여 '몸으로 막는(Body Blocking)' 현상을 구현합니다.

### **3.1. 충돌 해소(Collision Resolution) 로직 추가**

-   **수정 파일:** `UnitSimulator.Core/SimulatorCore.cs`
-   **수정 메소드:** `Step()`
-   **구현 상세:**
    1.  모든 유닛의 위치 업데이트(`unit.Position += unit.Velocity * deltaTime`)가 완료된 직후에 새로운 단계를 추가합니다.
    2.  `for` 루프를 두 번 중첩하여 모든 유닛 쌍(`unitA`, `unitB`)에 대해 거리를 검사합니다.
    3.  두 유닛의 거리 `dist`가 `unitA.Radius + unitB.Radius`보다 작다면(겹침 발생),
    4.  겹친 깊이 `overlap = (unitA.Radius + unitB.Radius) - dist`를 계산합니다.
    5.  `unitA`와 `unitB`를 서로 밀어내는 방향 벡터 `pushVector`를 계산합니다.
    6.  각 유닛을 `pushVector * overlap * 0.5f` 만큼 즉시 이동시켜 겹침을 해소합니다. (0.5f는 힘을 둘로 나누어 적용하기 위함)
    7.  성능을 위해 이 검사를 여러 번(e.g., 2-3회) 반복하여 프레임 내에서 안정적으로 겹침을 해결할 수 있습니다.

### **3.2. 검증 계획**

-   **시나리오:**
    1.  (1600, 2000) 위치에 반지름이 100인 대형 유닛(자이언트)을 소환합니다.
    2.  (1600, 1800) 위치에 반지름이 20인 소형 유닛(고블린)을 소환합니다.
    3.  두 유닛 모두 (1600, 3000)을 향해 이동하도록 명령합니다.
-   **성공 기준:** 고블린이 자이언트를 통과하지 못하고 뒤에 머무르거나, 자이언트의 가장자리를 따라 비집고 앞으로 나아가려는 움직임을 보여야 합니다. 두 유닛의 원이 겹치지 않고 유지되는지 로그를 통해 확인합니다.
