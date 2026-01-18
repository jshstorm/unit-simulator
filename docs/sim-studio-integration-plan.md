# sim-studio Integration Plan

Core 기능과 sim-studio UI 연동을 위한 구현 계획 문서.

---

## 목차

1. [현재 상태 분석](#1-현재-상태-분석)
2. [구현 계획](#2-구현-계획)
3. [Phase 상세](#3-phase-상세)
4. [우선순위 및 일정](#4-우선순위-및-일정)
5. [기술적 고려사항](#5-기술적-고려사항)

---

## 1. 현재 상태 분석

### 1.1 Core에서 구현된 주요 기능

| 영역 | 파일 | 설명 |
|------|------|------|
| **지형 시스템** | `Terrain/TerrainSystem.cs`, `GameState/MapLayout.cs` | 클래시로얄 스타일 맵 (3200x5100), 강/다리 |
| **타워 시스템** | `Towers/Tower.cs`, `TowerBehavior.cs`, `TowerStats.cs` | King/Princess 타워, 공격, HP |
| **전투 시스템** | `Combat/CombatSystem.cs`, `FrameEvents.cs` | 피해 처리, 이벤트 수집 |
| **어빌리티** | `Abilities/AbilityTypes.cs`, `ChargeState.cs` | ChargeAttack, Shield, DeathSpawn 등 |
| **게임 상태** | `GameState/GameSession.cs`, `WinConditionEvaluator.cs` | 크라운, 오버타임, 승리조건 |
| **경로 탐색** | `Pathfinding/AStarPathfinder.cs`, `DynamicObstacleSystem.cs` | A* 알고리즘, 동적 장애물 |
| **유닛 확장** | `Unit.cs` | Layer, CanTarget, Shield, Abilities |

### 1.2 sim-studio 현재 연동 상태

| 영역 | Core 구현 | sim-studio 연동 | 상태 |
|------|-----------|-----------------|------|
| 타워 렌더링 | ✅ Tower, TowerStateData | ✅ 기본 렌더링 | **완료** |
| 지형 (강/다리) | ✅ MapLayout, TerrainSystem | ❌ 미구현 | **필요** |
| 게임 상태 | ✅ GameSession, WinCondition | ⚠️ 일부만 | **확장 필요** |
| 유닛 확장 | ✅ Shield, Abilities, ChargeState | ⚠️ types만 정의됨 | **UI 필요** |
| 전투 이벤트 | ✅ FrameEvents, DamageType | ❌ 미구현 | **필요** |
| 타워 스킬 | ✅ TowerSkillSystem | ❌ 미구현 | **필요** |

### 1.3 FrameData 전송 현황

**현재 전송되는 데이터** (`FrameData.cs`):

```typescript
interface FrameData {
  // 기본 정보
  frameNumber: number;
  currentWave: number;
  livingFriendlyCount: number;
  livingEnemyCount: number;
  mainTarget: SerializableVector2;

  // 유닛/타워 상태
  friendlyUnits: UnitStateData[];
  enemyUnits: UnitStateData[];
  friendlyTowers?: TowerStateData[];
  enemyTowers?: TowerStateData[];

  // 게임 상태 (UI에 미표시)
  elapsedTime?: number;
  friendlyCrowns?: number;
  enemyCrowns?: number;
  gameResult?: string;
  winConditionType?: string | null;
  isOvertime?: boolean;

  // 종료 조건
  allWavesCleared: boolean;
  maxFramesReached: boolean;
}
```

**UnitStateData 확장 필드** (Core에서 전송, UI 미표시):

```typescript
interface UnitStateData {
  // 기존 필드...

  // 추가된 필드 (UI 연동 필요)
  layer: MovementLayer;        // Ground | Air
  canTarget: TargetType;       // Ground | Air | All
  damage: number;
  shieldHP: number;
  maxShieldHP: number;
  hasChargeState: boolean;
  isCharging: boolean;
  isCharged: boolean;
  requiredChargeDistance: number;
  abilities: AbilityType[];
}
```

---

## 2. 구현 계획

### Phase A: 지형 시각화

맵 레이아웃을 시각화하여 전투 흐름을 이해하기 쉽게 함.

**작업 항목**:
- [ ] A.1 강(River) 영역 렌더링 (파란색 반투명)
- [ ] A.2 다리(Bridge) 위치 표시
- [ ] A.3 스폰 존 가이드라인 (디버그 모드 토글)
- [ ] A.4 Friendly/Enemy 진영 구분선

**참조 데이터** (`MapLayout.cs`):
```
River: Y 2400 ~ 2700
Left Bridge: X 400 ~ 800
Right Bridge: X 2400 ~ 2800
Friendly Zone: Y 0 ~ 2400
Enemy Zone: Y 2700 ~ 5100
```

---

### Phase B: 게임 상태 UI 확장

FrameData에 이미 전송되지만 UI에 표시되지 않는 정보들.

**작업 항목**:
- [ ] B.1 크라운 표시 (friendlyCrowns, enemyCrowns)
- [ ] B.2 게임 결과 배너 (gameResult: Win/Lose/Draw/InProgress)
- [ ] B.3 오버타임 표시 (isOvertime)
- [ ] B.4 경과 시간 표시 (elapsedTime)
- [ ] B.5 승리 조건 타입 표시 (winConditionType)

**UI 위치 제안**:
```
┌──────────────────────────────────────────┐
│  [Crown] 0 - 0 [Crown]   ⏱️ 00:00       │  ← 상단 게임 상태 바
│  [OVERTIME]  Result: InProgress          │
├──────────────────────────────────────────┤
│                                          │
│              Canvas                      │
│                                          │
└──────────────────────────────────────────┘
```

---

### Phase C: 유닛 정보 확장

Core에서 전송하는 확장된 유닛 데이터 시각화.

**작업 항목**:
- [ ] C.1 `types.ts` 업데이트 (Layer, CanTarget, Shield, Abilities 등)
- [ ] C.2 Shield HP 바 (메인 HP 위에 별도 표시, 보라색)
- [ ] C.3 Layer 표시 (Air 유닛에 날개 아이콘 또는 그림자 효과)
- [ ] C.4 ChargeState 시각화 (돌진 게이지 또는 이펙트)
- [ ] C.5 Abilities 툴팁 (마우스 오버 시 아이콘 표시)
- [ ] C.6 UnitStateViewer 패널 확장

**Shield HP 바 렌더링**:
```
┌────────────────┐  ← Shield HP (보라색)
├────────────────┤  ← Main HP (녹색/노랑/빨강)
│     Unit       │
└────────────────┘
```

---

### Phase D: 이벤트 시각화

전투 피해 및 효과를 시각적으로 표현.

**작업 항목**:
- [ ] D.1 피해 숫자 팝업 (데미지 플로팅 텍스트)
- [ ] D.2 스플래시 피해 범위 표시 (원형 이펙트)
- [ ] D.3 사망 효과 시각화 (DeathSpawn 위치, DeathDamage 반경)
- [ ] D.4 타워 공격 라인 표시 (타워 → 유닛)
- [ ] D.5 `unit_event` 메시지 처리 확장

**필요한 WebSocket 이벤트**:
```typescript
interface DamageEventMessage {
  type: 'damage_event';
  data: {
    sourceId: number | null;
    targetId: number;
    amount: number;
    damageType: 'Normal' | 'Splash' | 'DeathDamage' | 'Spell' | 'Tower';
    position: SerializableVector2;
  };
}
```

> **Note**: Server에서 이벤트 브로드캐스트 추가 필요 여부 검토

---

### Phase E: 타워 상호작용 및 스킬

타워 선택 및 스킬 발동 UI.

**작업 항목**:
- [ ] E.1 타워 클릭 선택 기능
- [ ] E.2 선택된 타워 정보 패널 (HP, 공격력, 범위 등)
- [ ] E.3 타워 스킬 발동 버튼
- [ ] E.4 TowerSkillMessages WebSocket 연동

**Server 참조 파일**:
- `UnitSimulator.Server/Handlers/TowerSkillHandler.cs`
- `UnitSimulator.Server/Messages/TowerSkillMessages.cs`

---

## 3. Phase 상세

### 3.1 Phase A: 지형 시각화

**변경 파일**:
- `sim-studio/src/components/SimulationCanvas.tsx`

**구현 상세**:

```typescript
// MapLayout 상수 (Core와 동기화)
const MAP_LAYOUT = {
  RIVER_Y_MIN: 2400,
  RIVER_Y_MAX: 2700,
  LEFT_BRIDGE_X_MIN: 400,
  LEFT_BRIDGE_X_MAX: 800,
  RIGHT_BRIDGE_X_MIN: 2400,
  RIGHT_BRIDGE_X_MAX: 2800,
};

// 강 렌더링
const drawRiver = (ctx: CanvasRenderingContext2D) => {
  ctx.fillStyle = 'rgba(59, 130, 246, 0.3)'; // 파란색 반투명
  ctx.fillRect(0, flipY(MAP_LAYOUT.RIVER_Y_MAX), WORLD_WIDTH,
               MAP_LAYOUT.RIVER_Y_MAX - MAP_LAYOUT.RIVER_Y_MIN);

  // 다리 영역 (갈색)
  ctx.fillStyle = 'rgba(139, 69, 19, 0.5)';
  // Left bridge
  ctx.fillRect(MAP_LAYOUT.LEFT_BRIDGE_X_MIN, flipY(MAP_LAYOUT.RIVER_Y_MAX),
               MAP_LAYOUT.LEFT_BRIDGE_X_MAX - MAP_LAYOUT.LEFT_BRIDGE_X_MIN,
               MAP_LAYOUT.RIVER_Y_MAX - MAP_LAYOUT.RIVER_Y_MIN);
  // Right bridge
  ctx.fillRect(MAP_LAYOUT.RIGHT_BRIDGE_X_MIN, flipY(MAP_LAYOUT.RIVER_Y_MAX),
               MAP_LAYOUT.RIGHT_BRIDGE_X_MAX - MAP_LAYOUT.RIGHT_BRIDGE_X_MIN,
               MAP_LAYOUT.RIVER_Y_MAX - MAP_LAYOUT.RIVER_Y_MIN);
};
```

---

### 3.2 Phase B: 게임 상태 UI

**변경 파일**:
- `sim-studio/src/components/GameStatusBar.tsx` (신규)
- `sim-studio/src/App.tsx`

**컴포넌트 구조**:

```tsx
interface GameStatusBarProps {
  frameData: FrameData | null;
}

function GameStatusBar({ frameData }: GameStatusBarProps) {
  if (!frameData) return null;

  return (
    <div className="game-status-bar">
      <div className="crowns">
        <span className="crown friendly">👑 {frameData.friendlyCrowns ?? 0}</span>
        <span className="separator">-</span>
        <span className="crown enemy">👑 {frameData.enemyCrowns ?? 0}</span>
      </div>

      {frameData.isOvertime && (
        <span className="overtime-badge">OVERTIME</span>
      )}

      <div className="timer">
        ⏱️ {formatTime(frameData.elapsedTime ?? 0)}
      </div>

      {frameData.gameResult && frameData.gameResult !== 'InProgress' && (
        <div className={`result-banner ${frameData.gameResult.toLowerCase()}`}>
          {frameData.gameResult}
        </div>
      )}
    </div>
  );
}
```

---

### 3.3 Phase C: 유닛 정보 확장

**변경 파일**:
- `sim-studio/src/types.ts`
- `sim-studio/src/components/SimulationCanvas.tsx`
- `sim-studio/src/components/UnitStateViewer.tsx`

**types.ts 추가**:

```typescript
export type MovementLayer = 'Ground' | 'Air';
export type TargetType = 'Ground' | 'Air' | 'All';

export type AbilityType =
  | 'ChargeAttack'
  | 'SplashDamage'
  | 'ChainDamage'
  | 'PiercingAttack'
  | 'Shield'
  | 'DeathSpawn'
  | 'DeathDamage'
  | 'StatusEffect';

export interface UnitStateData {
  // 기존 필드...

  // 추가 필드
  layer: MovementLayer;
  canTarget: TargetType;
  damage: number;
  shieldHP: number;
  maxShieldHP: number;
  hasChargeState: boolean;
  isCharging: boolean;
  isCharged: boolean;
  requiredChargeDistance: number;
  abilities: AbilityType[];
}
```

---

## 4. 우선순위 및 일정

### 구현 우선순위

```
높음 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 낮음

1. [Phase B] 게임 상태 UI      - 데이터 이미 있음, UI만 추가
2. [Phase A] 지형 시각화       - 게임 이해도 향상
3. [Phase C] 유닛 정보 확장    - 디버깅/분석에 필수
4. [Phase D] 이벤트 시각화     - Server 수정 필요할 수 있음
5. [Phase E] 타워 상호작용     - Server 핸들러 존재, 연동만
```

### 의존성 그래프

```
Phase B (게임 상태) ─────┐
                        ├──► Phase D (이벤트)
Phase A (지형) ─────────┤
                        │
Phase C (유닛 확장) ────┴──► Phase E (타워 스킬)
```

---

## 5. 기술적 고려사항

### 5.1 Server 수정 필요 항목

| Phase | 항목 | 설명 |
|-------|------|------|
| D | 이벤트 브로드캐스트 | DamageEvent, SpawnEvent WebSocket 전송 |
| E | 타워 스킬 연동 | TowerSkillHandler 테스트 필요 |

### 5.2 성능 고려사항

- **Canvas 최적화**: 유닛/타워 수 증가 시 렌더링 성능
- **이벤트 버퍼링**: 피해 이벤트 다수 발생 시 일괄 처리
- **WebSocket 트래픽**: 이벤트 전송 빈도 조절

### 5.3 M2.4 연동

DataEditor 컴포넌트와 ReferenceModels 시스템 통합 검토:
- units.json 수정 → 실시간 반영
- 스키마 검증 UI 통합

---

## 변경 이력

| 날짜 | 버전 | 변경 내용 |
|------|------|-----------|
| 2026-01-18 | 1.0 | 초안 작성 |
