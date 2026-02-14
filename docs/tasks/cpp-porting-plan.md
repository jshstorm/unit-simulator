# C++ 포팅 계획: UnitSimulator.Core → UE5 UnitSimCore 모듈

> 작성일: 2026-02-14
> 상태: 계획 수립
> 대상: UnitSimulator.Core (58 C# 파일, ~9,300줄)
> 목표: UE5 네이티브 C++ 게임플레이 모듈

---

## 1. 엔진 결정 사항

| 항목 | 결정 |
|------|------|
| 엔진 | Unreal Engine 5 |
| 코어 통합 방식 | UE 모듈 내 직접 구현 (정적 라이브러리 분리 안함) |
| 수학 라이브러리 | UE FMath 직접 사용 |
| 데이터 접근 | JSON 직접 로드 (이후 DataTable 자동 동기화 확장 가능) |
| 권위 모델 | 클라이언트 권위 (서버 불필요) |
| 멀티플레이어 | 미고려 (싱글플레이어 우선) |
| 결정론성 | 단일 디바이스 보장 (FMath 충분) |

---

## 2. 타입 매핑

| C# | C++ / UE5 | 비고 |
|----|-----------|------|
| `Vector2` (System.Numerics) | `FVector2D` | |
| `float` | `float` | FMath 사용 |
| `List<T>` | `TArray<T>` | |
| `Dictionary<K,V>` | `TMap<K,V>` | |
| `HashSet<T>` | `TSet<T>` | |
| `Queue<T>` | `TQueue<T>` | |
| `string` | `FString` / `FName` | ID는 FName |
| `int` | `int32` | |
| `record` / `class` | `USTRUCT(BlueprintType)` 또는 `UObject` | 값 타입은 USTRUCT |
| `enum` | `UENUM(BlueprintType)` | |
| `interface` | 순수 가상 클래스 / `UInterface` | |
| `Action<T>` / `delegate` | `DECLARE_DELEGATE` / `DECLARE_MULTICAST_DELEGATE` | |
| `Nullable<T>` | `TOptional<T>` | |
| `System.Text.Json` | `FJsonObject` / `FJsonSerializer` | |

---

## 3. 모듈 구조

### UnitSimCore 모듈 (Runtime)

```
Plugins/UnitSimCore/
├── UnitSimCore.Build.cs
├── Public/
│   ├── Simulation/
│   │   ├── SimulatorCore.h
│   │   ├── FrameData.h
│   │   └── GameConstants.h
│   ├── Units/
│   │   ├── Unit.h
│   │   ├── UnitDefinition.h
│   │   ├── UnitRegistry.h
│   │   └── ChargeState.h
│   ├── Abilities/
│   │   └── AbilityTypes.h
│   ├── Behaviors/
│   │   ├── SquadBehavior.h
│   │   ├── EnemyBehavior.h
│   │   └── AvoidanceSystem.h
│   ├── Combat/
│   │   ├── CombatSystem.h
│   │   └── FrameEvents.h
│   ├── Pathfinding/
│   │   ├── AStarPathfinder.h
│   │   ├── PathfindingGrid.h
│   │   ├── PathSmoother.h
│   │   ├── DynamicObstacleSystem.h
│   │   ├── PathNode.h
│   │   └── IObstacleProvider.h
│   ├── Towers/
│   │   ├── Tower.h
│   │   ├── TowerBehavior.h
│   │   └── TowerStats.h
│   ├── Targeting/
│   │   └── TowerTargetingRules.h
│   ├── Terrain/
│   │   ├── TerrainSystem.h
│   │   └── MapLayout.h
│   ├── GameState/
│   │   ├── GameSession.h
│   │   ├── WinConditionEvaluator.h
│   │   ├── GameResult.h
│   │   └── InitialSetup.h
│   ├── Commands/
│   │   ├── ISimulationCommand.h
│   │   └── SimulationCommands.h
│   ├── Skills/
│   │   ├── TowerSkillSystem.h
│   │   └── TowerSkill.h
│   └── Data/
│       └── JsonDataLoader.h
│
└── Private/
    ├── Simulation/
    │   ├── SimulatorCore.cpp
    │   └── FrameData.cpp
    ├── Units/
    │   ├── Unit.cpp
    │   ├── UnitDefinition.cpp
    │   └── UnitRegistry.cpp
    ├── Behaviors/
    │   ├── SquadBehavior.cpp
    │   ├── EnemyBehavior.cpp
    │   └── AvoidanceSystem.cpp
    ├── Combat/
    │   ├── CombatSystem.cpp
    │   └── FrameEvents.cpp
    ├── Pathfinding/
    │   ├── AStarPathfinder.cpp
    │   ├── PathfindingGrid.cpp
    │   ├── PathSmoother.cpp
    │   └── DynamicObstacleSystem.cpp
    ├── Towers/
    │   ├── Tower.cpp
    │   └── TowerBehavior.cpp
    ├── Targeting/
    │   └── TowerTargetingRules.cpp
    ├── Terrain/
    │   ├── TerrainSystem.cpp
    │   └── MapLayout.cpp
    ├── GameState/
    │   ├── GameSession.cpp
    │   └── WinConditionEvaluator.cpp
    ├── Commands/
    │   └── SimulationCommands.cpp
    ├── Skills/
    │   ├── TowerSkillSystem.cpp
    │   └── TowerSkill.cpp
    └── Data/
        └── JsonDataLoader.cpp
```

### UnitSimGame 모듈 (Runtime)

```
Source/UnitSimGame/
├── GameModes/
├── PlayerController/
└── UI/
```

---

## 4. 폐기 대상 (17파일, ~1,900줄)

| C# 파일 | 줄수 | 폐기 이유 | 대체 |
|----------|------|-----------|------|
| `Contracts/ISimulation.cs` | 36 | UE GameMode로 대체 | AGameModeBase |
| `Contracts/IDataProvider.cs` | 47 | JSON 직접 로드 | JsonDataLoader |
| `Contracts/IUnitController.cs` | 31 | UE PlayerController | APlayerController |
| `Contracts/ISimulationObserver.cs` | 27 | UE Delegate | DECLARE_MULTICAST_DELEGATE |
| `Contracts/ContractVersion.cs` | 13 | 불필요 | - |
| `SimulationFacade.cs` | 179 | 래핑 불필요 | SimulatorCore 직접 사용 |
| `SimulationObserverCallbacks.cs` | 74 | UE Delegate | - |
| `GuiIntegration.cs` | 470 | UE Editor 위젯 | UMG / Editor Utility |
| `ReferenceExtensions.cs` | 243 | ReferenceModels 의존 제거 | JsonDataLoader 내 통합 |
| `GlobalUsings.cs` | 4 | C# 전용 | - |
| `Data/JsonDataProvider.cs` | 249 | UE JSON 로드 재구현 | JsonDataLoader |
| `Data/DefaultDataProvider.cs` | 142 | 불필요 | - |
| `Data/DataProviderFactory.cs` | 71 | 불필요 | - |
| `Pathfinding/PathfindingTestRunner.cs` | 172 | UE Automation Test | FAutomationTestBase |
| `Pathfinding/PathfindingTestReport.cs` | 86 | UE 테스트 프레임워크 | - |
| `Pathfinding/PathfindingTestSettings.cs` | 17 | UE 테스트 프레임워크 | - |
| `MathUtils.cs` | 87 | FMath + FVector2D로 대체 | 인라인 또는 유틸 함수 |

---

## 5. 포팅 대상 (41파일, ~7,400줄)

### 5.1 Tier 1 — 기초 (의존성 없음, 최우선 포팅)

| C# 파일 | 줄수 | C++ 대상 | 설명 |
|----------|------|----------|------|
| `GameConstants.cs` | 102 | `GameConstants.h` | static 상수 → namespace 또는 UDeveloperSettings |
| `Abilities/AbilityTypes.cs` | 174 | `AbilityTypes.h` | UENUM + USTRUCT 정의 |
| `Abilities/ChargeState.cs` | 61 | `ChargeState.h` | USTRUCT 값 타입 |
| `Pathfinding/PathNode.cs` | 34 | `PathNode.h` | USTRUCT 값 타입 |
| `Pathfinding/IObstacleProvider.cs` | 21 | `IObstacleProvider.h` | 순수 가상 클래스 |
| `GameState/GameResult.cs` | 53 | `GameResult.h` | UENUM |
| `Contracts/SimulationStatus.cs` | 16 | `GameConstants.h` 내 포함 | UENUM |
| `Contracts/UnitStats.cs` | 81 | `UnitStats.h` (또는 Unit.h 내) | USTRUCT |
| `Contracts/WaveDefinition.cs` | 58 | `WaveDefinition.h` | USTRUCT |
| `Contracts/GameBalance.cs` | 129 | `GameConstants.h` 또는 별도 | USTRUCT, JSON 로드 가능 |
| `Contracts/GameConfig.cs` | 27 | `InitialSetup.h` 내 통합 | USTRUCT |
| `Commands/ISimulationCommand.cs` | 14 | `ISimulationCommand.h` | 순수 가상 클래스 |
| `Commands/SpawnUnitCommand.cs` | 74 | `SimulationCommands.h` | USTRUCT 커맨드들 |

**소계**: 13파일, ~844줄

### 5.2 Tier 2 — 엔티티 (Tier 1 의존)

| C# 파일 | 줄수 | C++ 대상 | 설명 |
|----------|------|----------|------|
| `Unit.cs` | 448 | `Unit.h/.cpp` | 핵심 유닛 상태 |
| `Units/UnitDefinition.cs` | 78 | `UnitDefinition.h/.cpp` | 유닛 생성 정의 |
| `Units/UnitRegistry.cs` | 215 | `UnitRegistry.h/.cpp` | 유닛 정의 레지스트리 |
| `Towers/Tower.cs` | 170 | `Tower.h/.cpp` | 타워 상태 |
| `Towers/TowerStats.cs` | 108 | `TowerStats.h` | 타워 스탯 USTRUCT |
| `Combat/FrameEvents.cs` | 213 | `FrameEvents.h/.cpp` | 이벤트 컨테이너 |
| `Terrain/MapLayout.cs` | 236 | `MapLayout.h/.cpp` | 맵 레이아웃 정의 |
| `GameState/InitialSetup.cs` | 169 | `InitialSetup.h` | 게임 초기 상태 USTRUCT |

**소계**: 8파일, ~1,637줄

### 5.3 Tier 3 — 시스템 (Tier 1+2 의존)

| C# 파일 | 줄수 | C++ 대상 | 설명 |
|----------|------|----------|------|
| `AvoidanceSystem.cs` | 191 | `AvoidanceSystem.h/.cpp` | 충돌 회피 (static) |
| `Combat/CombatSystem.cs` | 209 | `CombatSystem.h/.cpp` | 전투 계산 |
| `Pathfinding/PathfindingGrid.cs` | 151 | `PathfindingGrid.h/.cpp` | 그리드 맵 |
| `Pathfinding/AStarPathfinder.cs` | 131 | `AStarPathfinder.h/.cpp` | A* 알고리즘 |
| `Pathfinding/PathSmoother.cs` | 104 | `PathSmoother.h/.cpp` | 경로 스무딩 |
| `Pathfinding/DynamicObstacleSystem.cs` | 108 | `DynamicObstacleSystem.h/.cpp` | 동적 장애물 |
| `Pathfinding/PathProgressMonitor.cs` | 103 | `PathProgressMonitor.h/.cpp` | 경로 진행 추적 |
| `Terrain/TerrainSystem.cs` | 61 | `TerrainSystem.h/.cpp` | 지형 시스템 |
| `Terrain/TerrainObstacleProvider.cs` | 50 | `TerrainObstacleProvider.h/.cpp` | 지형 장애물 |
| `Towers/TowerObstacleProvider.cs` | 43 | `TowerObstacleProvider.h/.cpp` | 타워 장애물 |
| `Towers/TowerBehavior.cs` | 128 | `TowerBehavior.h/.cpp` | 타워 행동 |
| `Targeting/TowerTargetingRules.cs` | 49 | `TowerTargetingRules.h/.cpp` | 타워 타겟팅 |
| `Skills/TowerSkill.cs` | 232 | `TowerSkill.h/.cpp` | 타워 스킬 |
| `Skills/TowerSkillSystem.cs` | 260 | `TowerSkillSystem.h/.cpp` | 스킬 시스템 |
| `GameState/GameSession.cs` | 323 | `GameSession.h/.cpp` | 게임 상태 |
| `GameState/WinConditionEvaluator.cs` | 86 | `WinConditionEvaluator.h/.cpp` | 승리 조건 |

**소계**: 16파일, ~2,229줄

### 5.4 Tier 4 — 행동 AI (Tier 1+2+3 의존)

| C# 파일 | 줄수 | C++ 대상 | 설명 |
|----------|------|----------|------|
| `SquadBehavior.cs` | 387 | `SquadBehavior.h/.cpp` | 아군 유닛 집단 행동 |
| `EnemyBehavior.cs` | 316 | `EnemyBehavior.h/.cpp` | 적 유닛 행동 |
| `ISimulatorCallbacks.cs` | 176 | UE Delegate로 재구현 | 이벤트 시스템 |

**소계**: 3파일, ~879줄

### 5.5 Tier 5 — 엔진 코어 (전체 의존)

| C# 파일 | 줄수 | C++ 대상 | 설명 |
|----------|------|----------|------|
| `SimulatorCore.cs` | 1,203 | `SimulatorCore.h/.cpp` | 메인 시뮬레이션 루프 |
| `FrameData.cs` | 531 | `FrameData.h/.cpp` | 프레임 직렬화 |

**소계**: 2파일, ~1,734줄

### 5.6 Tier 6 — 데이터 (독립, 병렬 가능)

| C# 파일 | 줄수 | C++ 대상 | 설명 |
|----------|------|----------|------|
| (새로 구현) | ~300 | `JsonDataLoader.h/.cpp` | JSON → USTRUCT 로딩 |

---

## 6. 구현 순서

```
Phase A: UE5 프로젝트 + 모듈 스캐폴딩
Phase B: Tier 1 기초 타입 (UENUM, USTRUCT, 상수)
Phase C: Tier 2 엔티티 (Unit, Tower, FrameEvents)
Phase D: Tier 6 데이터 로더 (JSON → USTRUCT) ← Tier 2 완료 후 병렬 가능
Phase E: Tier 3 시스템 (Combat, Pathfinding, Terrain)
Phase F: Tier 4 행동 AI (Squad, Enemy)
Phase G: Tier 5 엔진 코어 (SimulatorCore, FrameData)
Phase H: UnitSimGame 연동 (GameMode, PlayerController)
Phase I: UE Automation Test
```

---

## 7. 검증 기준

각 Tier 완료 시:
- UE 에디터에서 컴파일 성공
- 해당 Tier의 Automation Test 통과
- C# 원본과 동일 입력 → 동일 출력 (비교 테스트)

최종 검증:
- 동일한 JSON 데이터로 C# 시뮬레이션과 C++ 시뮬레이션 결과 비교
- 100프레임 시뮬레이션 결과 일치

---

**마지막 업데이트**: 2026-02-14
