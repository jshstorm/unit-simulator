# Unit Simulator 행동 규칙

이 문서는 시뮬레이션에서 유닛이 따르는 규칙과 알고리즘을 구조적으로 정리합니다.

## 1. 시뮬레이션 루프
- `SimulatorCore.Step()`에서 프레임별로 커맨드 처리 → 유닛/타워 업데이트 → 이벤트 적용 → `FrameData` 생성 → 콜백 통지 순서로 진행합니다.
- **종료 조건**:
  - King Tower가 파괴됨 (즉시 승패 결정)
  - 정규 시간(3분) 종료 시 크라운 차이로 승패 결정
  - 연장전(5분) 종료 시 타워 HP 비율로 승패 결정
  - 최대 프레임 수(`GameConstants.MAX_FRAMES`) 도달

## 2. 유닛 기본 스펙
- 속성: 위치/속도/전방 벡터, 반지름(`GameConstants.UNIT_RADIUS`), 이동 속도, 회전 속도, 체력, 역할(근접/원거리), 진영(아군/적)
- 체력: 아군 기본 HP `GameConstants.FRIENDLY_HP`가 적 HP `GameConstants.ENEMY_HP`의 약 10배 (현재 100 vs 10)
- 공격 사거리: 근접 `radius * 3`, 원거리 `radius * 6`
- 공격 슬롯: 목표 주변에 8개, 가장 가까운 슬롯을 점유해 겹침 방지

## 3. 이동 시스템(전역 경로 + 로컬 회피)

### 3.1 경로 탐색 (Pathfinding)
정적 장애물(벽/건물)을 회피하며 목적지까지의 전역 경로를 계산합니다.

- **A* 알고리즘**: 시뮬레이션 맵을 일정한 크기의 노드 그리드로 구성하고 최단 경로를 계산합니다.
- **구성 요소**:
  - `PathfindingGrid`: walkable/blocked 상태를 관리
  - `AStarPathfinder`: 출발/도착 노드 사이 경로(노드 리스트) 반환
- **동작 방식**:
  1. 유닛 이동 명령 시 `AStarPathfinder`에 출발지/목적지를 전달
  2. 웨이포인트 경로 반환
  3. 유닛은 웨이포인트를 순차적으로 추종
  4. 이동 중 충돌 회피는 `AvoidanceSystem`에 위임

- **장애물 표현**:
  - 좌표 기반 `(x, y)` 노드 단위 설정
  - 월드 좌표 기반 `Vector2` 위치 설정
  - 사각형 범위 `min/max` 월드 좌표 일괄 설정

- **코너 끼임 방지**:
  - 대각 이동은 인접 직교 타일 중 하나라도 blocked이면 허용하지 않음

- **시작/종료 노드 검증**:
  - 시작 또는 도착 노드가 blocked면 경로를 반환하지 않음

- **경로 반환 규칙**:
  - 반환 경로는 웨이포인트 리스트이며 시작 노드는 포함하지 않음

- **실행 검증 시나리오 (M1.5)**:
  - 목적: A* 경로가 장애물 회피/코너 컷 금지 규칙을 유지하는지 실행 수준에서 확인
  - 실행 방법(고정 설정):
    - `PathfindingTestSettings`: `Seed=1234`, `ObstacleDensity=0.15`, `ScenarioCount=25`
    - `MapWidth/MapHeight/NodeSize`는 기본값(`GameConstants.SIMULATION_*`, `GameConstants.UNIT_RADIUS`) 사용
    - 실행: `dotnet run --project tools/unit-dev-tool` → 메뉴 `Pathfinding Report`
  - 실행 코드(예시):
    ```csharp
    var settings = new PathfindingTestSettings
    {
        Seed = 1234,
        ObstacleDensity = 0.15f,
        ScenarioCount = 25
    };

    var runner = new PathfindingTestRunner();
    var report = runner.Run(settings);
    report.SaveToJson("output/pathfinding-report.json");
    ```
  - 성공 기준(초기 기준선 수립 목적):
    - `output/pathfinding-report.json` 생성
    - `report.Summary.SuccessRate` 및 `AveragePathLength`가 기록됨
    - 변경 전후 비교 시 성공률 급락 또는 평균 경로 길이 급증 시 원인 분석

- **RTS 확장 고려사항**:
  - 유닛 반경 마스킹, 동적 장애물 재탐색, 경로 스무딩
  - 지형 비용 가중치, 군집 이동(코리도어/플로우 필드)
  - 대량 유닛 경로 요청의 프레임 분산 처리

- **구현 계획(상세)**:
  1. **Grid 확장**: `BaseCost` 추가, `InflateObstacles(radius)` 제공
  2. **Pathfinder 개선**: 지형 비용 포함, `MaxIterations`/타임슬라이스 옵션
  3. **경로 스무딩**: LOS 기반 웨이포인트 축소, 스위치 제공
  4. **동적 장애물**: blocked 감지 시 재탐색, 쿨다운 적용
  5. **군집 이동**: 분대 목적지 공유 + 개인 오프셋
  6. **테스트**: 마스킹/스무딩/비용/재탐색 케이스

### 3.2 충돌 예측 및 회피 (AvoidanceSystem)
동적 장애물(유닛)을 회피하기 위한 로컬 스티어링 시스템입니다.

- **정확한 충돌 예측**: 두 이동 원의 첫 충돌 시각을 2차 방정식으로 계산
- **위험 판단**: lookahead 범위 내 충돌 가능성이 있으면 회피 후보로 선정
- **회피 방향 선택**: 진행 방향 기준 좌/우 회전을 스캔해 안전 방향 선택
- **세그먼트 우회 경로**: 우회 시작 → 측면 편향 → 병렬 이동 → 복귀 순으로 경유지 생성

### 3.3 경로 탐색 vs 회피 규칙
- **전역/로컬 역할 분리**: A*는 전역 경로, Avoidance는 로컬 회피 전담
- **스티어링 합성**:
  - 최종 방향 = `Normalize(steeringDir + separationVector + avoidanceVector)` (가중치 합성 없음)
  - 최종 속도는 `unit.GetEffectiveSpeed()`를 사용
- **재경로 조건(현재 구현)**:
  - 목적지가 `GameConstants.DESTINATION_THRESHOLD` 이상 바뀌면 경로 재계산

## 4. 아군 분대 로직 (`SquadBehavior`)
- **랠리 & 대형**: 최초 적 탐지 시 랠리 포인트를 잡고 리더 이동, 나머지는 회전 오프셋으로 대형 유지
- **목표 지정**: 가장 가까운 적 타겟 + 슬롯 점유
- **교전 이동**: 슬롯 위치로 이동, 사거리 내면 정지 후 공격
- **개별 교전 전환**: 적이 공격 사거리 `1.5×` 내로 진입하면 즉시 교전
- **회피/분리**: 분리 벡터 + 예측 회피 벡터 합산
- **속도 규칙**: 기본 속도 유지, 임계 거리 내에서만 정지
- **비전투 이동**: 적이 없으면 주 목표로 이동하며 대형 유지

## 5. 적 분대 로직 (`EnemyBehavior`)
- **타겟팅**: 거리 + 몰림 가중치 기반 점수 최소 타겟 선택
- **재평가**: 주기 또는 명확히 더 좋은 타겟이 있으면 교체
- **이동/회피**: 슬롯/우회 위치로 이동, 분리/회피는 동일 시스템 사용
- **공격**: 사거리 내에서 `ENEMY_ATTACK_DAMAGE`, 쿨다운 `ATTACK_COOLDOWN`
- **슬롯 갱신 규칙**: 거리/주기 조건에 따라 슬롯 재평가

## 6. 맵 및 타워 시스템

### 6.1 맵 레이아웃
```
┌─────────────────────────────────────────────────────────────┐
│                        ENEMY SIDE                            │
│     [Princess L]          [King]          [Princess R]       │
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ BRIDGE ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │
│                          RIVER                               │
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ BRIDGE ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │
│     [Princess L]          [King]          [Princess R]       │
│                       FRIENDLY SIDE                          │
└─────────────────────────────────────────────────────────────┘
```
- 맵 크기: 3200 x 5100
- 각 진영: Princess Tower 2개 + King Tower 1개

### 6.2 타워 규칙
- **Princess Tower**: 사이드 위치, 즉시 공격 가능한 대상
- **King Tower**: 중앙 위치, Princess Tower 파괴 또는 직접 피해 시 활성화
- 타워는 사거리 내 적 유닛을 자동 공격

### 6.3 승패 조건
| 조건 | 결과 |
|------|------|
| King Tower 파괴 | 즉시 승리/패배 |
| 정규 시간(3분) 종료 | 크라운 수로 결정 |
| 연장전(5분) 종료 | 타워 HP 비율로 결정 |
| 동률 | 무승부 |

### 6.4 크라운 시스템
- Princess Tower 파괴: 1 크라운
- King Tower 파괴: 3 크라운 (게임 종료)

### 6.5 강 & 다리
- 강(River)은 Ground 유닛의 이동을 차단
- 다리(Bridge)는 Ground 유닛이 강을 건널 수 있는 유일한 경로
- Air 유닛은 강을 무시하고 이동

> **상세 스펙**: `unit-system-spec.md` Section 12, 13 참조

---

## 7. 유닛 스폰 및 배치

### 7.1 기본 규칙
- 각 진영은 자신의 영역에만 유닛 배치 가능
- Friendly: Y < 2400 (강 이전)
- Enemy: Y > 2700 (강 이후)

### 7.2 스폰 위치 정책
유닛 생성 시 진영별로 지정된 스폰 영역을 사용합니다.

#### Friendly 유닛 스폰 영역
- **기준점**: King Tower 전방 (Y = 1500, King Tower Y + 800)
- **스폰 영역**: X: 800~2400, Y: 1400~1700
- **기본 스폰 위치**: (1600, 1500) - 맵 중앙, King Tower 전방

```
         [Princess L]                    [Princess R]
              ↑                               ↑
    ┌─────────────────────────────────────────────┐
    │           FRIENDLY SPAWN ZONE               │
    │         X: 800~2400, Y: 1400~1700           │
    └─────────────────────────────────────────────┘
                        ↑
                   [King Tower]
```

#### Enemy 유닛 스폰 영역
- **기준점**: King Tower 전방 (Y = 3600, King Tower Y - 800)
- **스폰 영역**: X: 800~2400, Y: 3400~3700
- **기본 스폰 위치**: (1600, 3600) - 맵 중앙, King Tower 전방

```
                   [King Tower]
                        ↓
    ┌─────────────────────────────────────────────┐
    │            ENEMY SPAWN ZONE                 │
    │         X: 800~2400, Y: 3400~3700           │
    └─────────────────────────────────────────────┘
              ↓                               ↓
         [Princess L]                    [Princess R]
```

### 7.3 겹침 방지
- 스폰 시 기존 유닛과의 최소 거리 확보 (유닛 반경 × 2.5)
- 겹침 발생 시 스폰 영역 내 무작위 위치로 재시도 (최대 10회)
- 모든 시도 실패 시 기본 스폰 위치 사용

### 7.4 수동 스폰 vs 자동 스폰
- 수동 스폰 (GUI):
  - `SimulationSession.TryResolveManualSpawnPosition`에서 맵 범위/강 영역/겹침 검사 후 위치 보정
- 자동 스폰 (웨이브/DeathSpawn/SpawnUnitCommand):
  - 소스에서 제공한 위치를 그대로 사용 (별도 맵 범위/겹침 검사 없음)

### 7.5 타워 위치 참조
| 타워 | Friendly | Enemy |
|------|----------|-------|
| King Tower | (1600, 700) | (1600, 4400) |
| Princess Left | (600, 1200) | (600, 3900) |
| Princess Right | (2600, 1200) | (2600, 3900) |

---

## 8. 레거시: 웨이브 관리 (`WaveManager`)
> **Note**: 타워 기반 시스템 도입 후 웨이브 시스템은 선택적으로 사용됩니다.

- 사전 정의 좌표로 적 웨이브 생성
- 전 웨이브 처치 시 다음 웨이브 스폰
- 최대 웨이브 수 `GameConstants.MAX_WAVES`

## 9. 렌더링 (`Renderer`)
- 유닛, 슬롯, 전방 벡터, 최근 공격선, 회피 목표, **타워 상태**를 프레임 이미지로 저장

## 10. 알려진 문제 및 개선점
- **장애물 데이터 미연동**: 런타임에서는 PathfindingGrid에 장애물이 채워지지 않아 복잡 지형 회피가 제한됨
- **성능**: 유닛 수 증가 시 O(N^2) 계산 부하
- **단순한 대형 유지**: 좁은 길에서 대형 붕괴 가능
