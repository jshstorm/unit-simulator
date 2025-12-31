# Reference Models Expansion Plan

## 문서 개요
- **작성일**: 2025-12-31
- **목적**: unit-system-spec.md의 요구사항을 Reference Models에 반영하기 위한 확장 계획
- **범위**: ReferenceModels 프로젝트의 스키마 확장 및 데이터 구조 설계

---

## 1. 현재 상태 분석

### 1.1 이미 구현된 구조

#### Reference Classes
- **UnitReference**: 기본 유닛 속성
  - `DisplayName`, `MaxHP`, `Speed`, `Damage`, `AttackRadius`, `Radius`
  - `Layer`, `TargetPriority`, `CanTarget`, `Role`
  - `Skills` (스킬 ID 목록)

- **SkillReference**: 스킬 데이터
  - `DisplayName`
  - ChargeAttack: `ChargeSpeed`, `ChargeDamageMultiplier`
  - SplashDamage: `SplashRadius`, `SplashPercent`
  - Shield: `ShieldHP`, `ShieldDuration`
  - DeathSpawn: `SpawnUnitId`, `SpawnCount`
  - DeathDamage: `DeathDamageRadius`, `DeathDamage`

#### Enum Types
- **UnitRole**: `Melee`, `Ranged` (2개만 존재)
- **MovementLayer**: `Ground`, `Air`
- **TargetType**: 공격 가능 대상 플래그 (`Ground`, `Air`, `Buildings`)
- **TargetPriority**: `Nearest`, `Buildings`

#### Infrastructure
- **ReferenceManager**: JSON 파싱 및 레퍼런스 데이터 관리
- **ReferenceHandlers**: 타입별 파싱 핸들러
- **Validators**: 각 레퍼런스 데이터의 유효성 검증

### 1.2 스펙 대비 누락 사항

#### 누락된 Enum 타입
1. **EntityType** (섹션 2.1): Troop, Building, Spell, Projectile
2. **AttackType** (섹션 3.2): Melee*, Ranged, None
3. **BuildingType** (섹션 6.1): Defensive, Spawner, Utility
4. **SpellType** (섹션 7.1): Instant, AreaOverTime, Utility, Spawning
5. **TowerType** (섹션 12.2): Princess, King
6. **StatusEffectType** (섹션 5.1): Stunned, Frozen, Slowed 등 12종
7. **AbilityType**: ChargeAttack, SplashDamage, Shield 등
8. **UnitRole 확장**: Tank, MiniTank, GlassCannon, Swarm, Spawner, Support, Siege

#### 누락된 Reference 클래스
1. **BuildingReference** (섹션 6.2)
2. **SpellReference** (섹션 7.2)
3. **TowerReference** (섹션 12.2)

#### 누락된 UnitReference 필드
- `EntityType`: 엔티티 기본 유형
- `AttackType`: 공격 방식
- `AttackSpeed`: 초당 공격 횟수
- `ShieldHP`: 기본 쉴드 HP
- `SpawnCount`: 배치 시 생성 수량

#### 누락된 SkillReference 필드
- 상태 효과: `AppliedEffect`, `EffectDuration`, `EffectMagnitude`
- 범위 효과: `EffectRange`, `AffectedTargets`

---

## 2. 추가할 스키마 설계

### 2.1 새로운 Enum 타입

#### EntityType.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 엔티티의 기본 유형
/// </summary>
public enum EntityType
{
    /// <summary>유닛 (배치 가능한 전투 개체)</summary>
    Troop,

    /// <summary>건물 (고정된 구조물)</summary>
    Building,

    /// <summary>스펠 (일회성 효과)</summary>
    Spell,

    /// <summary>투사체 (발사되는 공격 개체)</summary>
    Projectile
}
```

#### AttackType.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 공격 방식 및 사거리 유형
/// </summary>
public enum AttackType
{
    /// <summary>근접 공격 - 초단거리 (1 타일 이하)</summary>
    MeleeShort,

    /// <summary>근접 공격 - 단거리 (1.5 타일)</summary>
    Melee,

    /// <summary>근접 공격 - 중거리 (2-3 타일)</summary>
    MeleeMedium,

    /// <summary>근접 공격 - 장거리 (4-5 타일)</summary>
    MeleeLong,

    /// <summary>원거리 공격 (5 타일 이상)</summary>
    Ranged,

    /// <summary>공격하지 않음</summary>
    None
}
```

#### UnitRole 확장
```csharp
namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 유닛의 전술적 역할
/// </summary>
public enum UnitRole
{
    /// <summary>근접 전투</summary>
    Melee,

    /// <summary>원거리 전투</summary>
    Ranged,

    /// <summary>높은 HP 탱커 (6000+ HP)</summary>
    Tank,

    /// <summary>중형 탱커 (2000-6000 HP)</summary>
    MiniTank,

    /// <summary>높은 DPS, 낮은 HP</summary>
    GlassCannon,

    /// <summary>다수 소환형 (3개 이상)</summary>
    Swarm,

    /// <summary>죽을 때 유닛 소환</summary>
    Spawner,

    /// <summary>지원/버프 역할</summary>
    Support,

    /// <summary>건물 우선 공격</summary>
    Siege
}
```

#### BuildingType.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 건물 유형
/// </summary>
public enum BuildingType
{
    /// <summary>방어용 건물 (타워, 대포 등)</summary>
    Defensive,

    /// <summary>유닛 소환 건물 (묘비, 오두막 등)</summary>
    Spawner,

    /// <summary>유틸리티 건물 (엘릭서 펌프 등)</summary>
    Utility
}
```

#### SpellType.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 스펠 유형
/// </summary>
public enum SpellType
{
    /// <summary>즉시 효과 (파이어볼, 잽 등)</summary>
    Instant,

    /// <summary>지속 범위 효과 (포이즌, 프리즈 등)</summary>
    AreaOverTime,

    /// <summary>유틸리티 (레이지, 힐 등)</summary>
    Utility,

    /// <summary>유닛 소환 (그래브야드 등)</summary>
    Spawning
}
```

#### TowerType.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 타워 유형
/// </summary>
public enum TowerType
{
    /// <summary>프린세스 타워 (양 옆)</summary>
    Princess,

    /// <summary>킹 타워 (중앙)</summary>
    King
}
```

#### StatusEffectType.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 상태 효과 유형
/// </summary>
[Flags]
public enum StatusEffectType
{
    None = 0,

    /// <summary>기절 - 이동 및 공격 불가</summary>
    Stunned = 1 << 0,

    /// <summary>빙결 - 이동 및 공격 불가 (얼음 효과)</summary>
    Frozen = 1 << 1,

    /// <summary>둔화 - 이동 속도 감소</summary>
    Slowed = 1 << 2,

    /// <summary>속박 - 이동 불가, 공격은 가능</summary>
    Rooted = 1 << 3,

    /// <summary>중독 - 지속 피해</summary>
    Poisoned = 1 << 4,

    /// <summary>화상 - 지속 피해 (높은 DPS)</summary>
    Burning = 1 << 5,

    /// <summary>격분 - 공격 속도 및 이동 속도 증가</summary>
    Raged = 1 << 6,

    /// <summary>치유 - 지속 회복</summary>
    Healing = 1 << 7,

    /// <summary>보호막 - 추가 HP</summary>
    Shielded = 1 << 8,

    /// <summary>투명 - 타게팅 불가</summary>
    Invisible = 1 << 9,

    /// <summary>표식 - 받는 피해 증가</summary>
    Marked = 1 << 10,

    /// <summary>무적 - 피해 받지 않음</summary>
    Invulnerable = 1 << 11
}
```

#### AbilityType.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 특수 능력 유형
/// </summary>
public enum AbilityType
{
    /// <summary>돌진 공격</summary>
    ChargeAttack,

    /// <summary>범위 피해</summary>
    SplashDamage,

    /// <summary>보호막</summary>
    Shield,

    /// <summary>죽을 때 유닛 소환</summary>
    DeathSpawn,

    /// <summary>죽을 때 피해</summary>
    DeathDamage,

    /// <summary>타겟팅 우선순위</summary>
    TargetPriority,

    /// <summary>상태 효과 부여</summary>
    ApplyStatusEffect,

    /// <summary>범위 버프</summary>
    AuraEffect,

    /// <summary>체력 회복</summary>
    Healing,

    /// <summary>텔레포트</summary>
    Teleport,

    /// <summary>무적 시간</summary>
    Invulnerability
}
```

### 2.2 기존 Reference 클래스 확장

#### UnitReference 확장
```csharp
namespace UnitSimulator.ReferenceModels.Models;

public class UnitReference
{
    // === 기존 필드들 ===
    public required string DisplayName { get; init; }
    public int MaxHP { get; init; }
    public float Speed { get; init; }
    public int Damage { get; init; }
    public float AttackRadius { get; init; }
    public float Radius { get; init; }
    public MovementLayer Layer { get; init; }
    public TargetPriority TargetPriority { get; init; }
    public TargetType CanTarget { get; init; }
    public UnitRole Role { get; init; }
    public List<string> Skills { get; init; } = new();

    // === 새로운 필드들 ===

    /// <summary>엔티티 유형 (기본값: Troop)</summary>
    public EntityType EntityType { get; init; } = EntityType.Troop;

    /// <summary>공격 방식 (기본값: Melee)</summary>
    public AttackType AttackType { get; init; } = AttackType.Melee;

    /// <summary>초당 공격 횟수 (기본값: 1.0)</summary>
    public float AttackSpeed { get; init; } = 1.0f;

    /// <summary>기본 쉬드 HP (기본값: 0)</summary>
    public int ShieldHP { get; init; } = 0;

    /// <summary>배치 시 생성되는 수량 (기본값: 1, Swarm용)</summary>
    public int SpawnCount { get; init; } = 1;
}
```

#### SkillReference 확장
```csharp
namespace UnitSimulator.ReferenceModels.Models;

public class SkillReference
{
    // === 기존 필드들 ===
    public required string DisplayName { get; init; }

    // ChargeAttack
    public float ChargeSpeed { get; init; }
    public float ChargeDamageMultiplier { get; init; }

    // SplashDamage
    public float SplashRadius { get; init; }
    public float SplashPercent { get; init; }

    // Shield
    public int ShieldHP { get; init; }
    public float ShieldDuration { get; init; }

    // DeathSpawn
    public string? SpawnUnitId { get; init; }
    public int SpawnCount { get; init; }

    // DeathDamage
    public float DeathDamageRadius { get; init; }
    public int DeathDamage { get; init; }

    // === 새로운 필드들 (상태 효과) ===

    /// <summary>부여할 상태 효과 (기본값: None)</summary>
    public StatusEffectType AppliedEffect { get; init; } = StatusEffectType.None;

    /// <summary>상태 효과 지속 시간 (초, 기본값: 0)</summary>
    public float EffectDuration { get; init; } = 0f;

    /// <summary>효과 크기 (슬로우: 속도 감소율, 레이지: 속도 증가율 등)</summary>
    public float EffectMagnitude { get; init; } = 0f;

    /// <summary>효과 범위 (기본값: 0, 범위 효과용)</summary>
    public float EffectRange { get; init; } = 0f;

    /// <summary>영향받는 대상 (기본값: None)</summary>
    public TargetType AffectedTargets { get; init; } = TargetType.None;
}
```

### 2.3 새로운 Reference 클래스

#### BuildingReference.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models;

/// <summary>
/// 건물 레퍼런스 데이터
/// </summary>
public class BuildingReference
{
    public required string DisplayName { get; init; }

    /// <summary>건물 유형</summary>
    public BuildingType Type { get; init; }

    /// <summary>최대 HP</summary>
    public int MaxHP { get; init; }

    /// <summary>충돌 반경</summary>
    public float Radius { get; init; }

    /// <summary>생명 시간 (초, 0 = 무한)</summary>
    public float Lifetime { get; init; } = 0f;

    // === Spawner 전용 필드 ===

    /// <summary>소환할 유닛 ID</summary>
    public string? SpawnUnitId { get; init; }

    /// <summary>소환 수량</summary>
    public int SpawnCount { get; init; } = 0;

    /// <summary>소환 간격 (초)</summary>
    public float SpawnInterval { get; init; } = 0f;

    /// <summary>첫 소환 딜레이 (초)</summary>
    public float FirstSpawnDelay { get; init; } = 0f;

    // === Defensive 전용 필드 ===

    /// <summary>공격 사거리</summary>
    public float AttackRange { get; init; } = 0f;

    /// <summary>공격력</summary>
    public int Damage { get; init; } = 0;

    /// <summary>초당 공격 횟수</summary>
    public float AttackSpeed { get; init; } = 0f;

    /// <summary>공격 가능 대상</summary>
    public TargetType CanTarget { get; init; } = TargetType.None;

    // === 공통 ===

    /// <summary>스킬 ID 목록</summary>
    public List<string> Skills { get; init; } = new();
}
```

#### SpellReference.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models;

/// <summary>
/// 스펠 레퍼런스 데이터
/// </summary>
public class SpellReference
{
    public required string DisplayName { get; init; }

    /// <summary>스펠 유형</summary>
    public SpellType Type { get; init; }

    /// <summary>효과 반경</summary>
    public float Radius { get; init; }

    /// <summary>지속 시간 (초, Instant는 0)</summary>
    public float Duration { get; init; } = 0f;

    /// <summary>시전 딜레이 (초)</summary>
    public float CastDelay { get; init; } = 0f;

    // === 피해 관련 ===

    /// <summary>즉시 피해 (Instant용)</summary>
    public int Damage { get; init; } = 0;

    /// <summary>틱당 피해 (AreaOverTime용)</summary>
    public int DamagePerTick { get; init; } = 0;

    /// <summary>틱 간격 (초)</summary>
    public float TickInterval { get; init; } = 0f;

    /// <summary>건물 피해 배율</summary>
    public float BuildingDamageMultiplier { get; init; } = 1.0f;

    // === 효과 관련 ===

    /// <summary>영향받는 대상</summary>
    public TargetType AffectedTargets { get; init; } = TargetType.None;

    /// <summary>부여할 상태 효과</summary>
    public StatusEffectType AppliedEffect { get; init; } = StatusEffectType.None;

    /// <summary>효과 크기 (슬로우: 속도 감소율 등)</summary>
    public float EffectMagnitude { get; init; } = 0f;

    // === 소환 관련 ===

    /// <summary>소환할 유닛 ID</summary>
    public string? SpawnUnitId { get; init; }

    /// <summary>소환 수량</summary>
    public int SpawnCount { get; init; } = 0;

    /// <summary>소환 간격 (초)</summary>
    public float SpawnInterval { get; init; } = 0f;
}
```

#### TowerReference.cs
```csharp
namespace UnitSimulator.ReferenceModels.Models;

/// <summary>
/// 타워 레퍼런스 데이터
/// </summary>
public class TowerReference
{
    public required string DisplayName { get; init; }

    /// <summary>타워 유형</summary>
    public TowerType Type { get; init; }

    /// <summary>최대 HP</summary>
    public int MaxHP { get; init; }

    /// <summary>공격력</summary>
    public int Damage { get; init; }

    /// <summary>초당 공격 횟수</summary>
    public float AttackSpeed { get; init; }

    /// <summary>공격 사거리</summary>
    public float AttackRadius { get; init; }

    /// <summary>충돌 반경</summary>
    public float Radius { get; init; }

    /// <summary>공격 가능 대상</summary>
    public TargetType CanTarget { get; init; }
}
```

---

## 3. 구현 전략 및 단계

### Phase 1: Enum 타입 추가
**목표**: 스펙에서 요구하는 모든 enum 타입을 ReferenceModels에 추가

**예상 소요**: 1-2시간
**난이도**: ⭐ (낮음)

#### 구체적 스텝

**Step 1.1: EntityType 추가**
- [ ] `ReferenceModels/Models/Enums/EntityType.cs` 생성
- [ ] Troop, Building, Spell, Projectile enum 정의
- [ ] XML 문서화 주석 추가

**Step 1.2: AttackType 추가**
- [ ] `ReferenceModels/Models/Enums/AttackType.cs` 생성
- [ ] MeleeShort, Melee, MeleeMedium, MeleeLong, Ranged, None enum 정의
- [ ] XML 문서화 주석 추가

**Step 1.3: UnitRole 확장**
- [ ] `ReferenceModels/Models/Enums/UnitRole.cs` 열기
- [ ] Tank, MiniTank, GlassCannon, Swarm, Spawner, Support, Siege 추가
- [ ] 각 역할에 대한 설명 주석 추가

**Step 1.4: BuildingType 추가**
- [ ] `ReferenceModels/Models/Enums/BuildingType.cs` 생성
- [ ] Defensive, Spawner, Utility enum 정의
- [ ] XML 문서화 주석 추가

**Step 1.5: SpellType 추가**
- [ ] `ReferenceModels/Models/Enums/SpellType.cs` 생성
- [ ] Instant, AreaOverTime, Utility, Spawning enum 정의
- [ ] XML 문서화 주석 추가

**Step 1.6: TowerType 추가**
- [ ] `ReferenceModels/Models/Enums/TowerType.cs` 생성
- [ ] Princess, King enum 정의
- [ ] XML 문서화 주석 추가

**Step 1.7: StatusEffectType 추가**
- [ ] `ReferenceModels/Models/Enums/StatusEffectType.cs` 생성
- [ ] `[Flags]` 속성 추가
- [ ] 12개 상태 효과 플래그 정의 (Stunned, Frozen, Slowed 등)
- [ ] 각 효과에 대한 설명 주석 추가

**Step 1.8: AbilityType 추가**
- [ ] `ReferenceModels/Models/Enums/AbilityType.cs` 생성
- [ ] 주요 능력 enum 정의 (ChargeAttack, SplashDamage 등)
- [ ] XML 문서화 주석 추가

**Step 1.9: 빌드 확인**
- [ ] `dotnet build ReferenceModels` 실행
- [ ] 컴파일 오류 해결

---

### Phase 2: UnitReference 확장
**목표**: 기존 UnitReference에 스펙 요구사항 반영

**예상 소요**: 1시간
**난이도**: ⭐ (낮음)

#### 구체적 스텝

**Step 2.1: UnitReference 필드 추가**
- [ ] `ReferenceModels/Models/UnitReference.cs` 열기
- [ ] `EntityType EntityType` 필드 추가 (기본값: `EntityType.Troop`)
- [ ] `AttackType AttackType` 필드 추가 (기본값: `AttackType.Melee`)
- [ ] `float AttackSpeed` 필드 추가 (기본값: `1.0f`)
- [ ] `int ShieldHP` 필드 추가 (기본값: `0`)
- [ ] `int SpawnCount` 필드 추가 (기본값: `1`)
- [ ] 각 필드에 XML 문서화 주석 추가

**Step 2.2: 하위 호환성 확인**
- [ ] 기존 JSON 파일이 새 필드 없이도 로드되는지 확인
- [ ] 모든 새 필드가 기본값을 가지는지 검증

**Step 2.3: 빌드 확인**
- [ ] `dotnet build ReferenceModels` 실행
- [ ] 컴파일 오류 해결

---

### Phase 3: 새로운 Reference 클래스 추가
**목표**: Building, Spell, Tower 레퍼런스 추가

**예상 소요**: 3-4시간
**난이도**: ⭐⭐ (중간)

#### 구체적 스텝

**Step 3.1: BuildingReference 생성**
- [ ] `ReferenceModels/Models/BuildingReference.cs` 생성
- [ ] 클래스 정의 및 모든 필드 추가
  - DisplayName, Type, MaxHP, Radius, Lifetime
  - Spawner 필드: SpawnUnitId, SpawnCount, SpawnInterval, FirstSpawnDelay
  - Defensive 필드: AttackRange, Damage, AttackSpeed, CanTarget
  - Skills 목록
- [ ] XML 문서화 주석 추가

**Step 3.2: SpellReference 생성**
- [ ] `ReferenceModels/Models/SpellReference.cs` 생성
- [ ] 클래스 정의 및 모든 필드 추가
  - DisplayName, Type, Radius, Duration, CastDelay
  - 피해: Damage, DamagePerTick, TickInterval, BuildingDamageMultiplier
  - 효과: AffectedTargets, AppliedEffect, EffectMagnitude
  - 소환: SpawnUnitId, SpawnCount, SpawnInterval
- [ ] XML 문서화 주석 추가

**Step 3.3: TowerReference 생성**
- [ ] `ReferenceModels/Models/TowerReference.cs` 생성
- [ ] 클래스 정의 및 모든 필드 추가
  - DisplayName, Type, MaxHP, Damage, AttackSpeed
  - AttackRadius, Radius, CanTarget
- [ ] XML 문서화 주석 추가

**Step 3.4: ReferenceHandlers 확장**
- [ ] `ReferenceModels/Infrastructure/ReferenceHandlers.cs` 열기
- [ ] `ParseBuildings(JsonElement buildingsElement)` 메서드 추가
  - JsonElement를 순회하며 BuildingReference 리스트 생성
  - `Dictionary<string, BuildingReference>` 반환
- [ ] `ParseSpells(JsonElement spellsElement)` 메서드 추가
  - JsonElement를 순회하며 SpellReference 리스트 생성
  - `Dictionary<string, SpellReference>` 반환
- [ ] `ParseTowers(JsonElement towersElement)` 메서드 추가
  - JsonElement를 순회하며 TowerReference 리스트 생성
  - `Dictionary<string, TowerReference>` 반환

**Step 3.5: ReferenceManager 확장**
- [ ] `ReferenceModels/Infrastructure/ReferenceManager.cs` 열기
- [ ] `IReadOnlyDictionary<string, BuildingReference> Buildings` 프로퍼티 추가
- [ ] `IReadOnlyDictionary<string, SpellReference> Spells` 프로퍼티 추가
- [ ] `IReadOnlyDictionary<string, TowerReference> Towers` 프로퍼티 추가
- [ ] `LoadReferences()` 메서드에서 새 핸들러 호출 추가
  - buildings.json 파싱 → Buildings 할당
  - spells.json 파싱 → Spells 할당
  - towers.json 파싱 → Towers 할당

**Step 3.6: 빌드 확인**
- [ ] `dotnet build ReferenceModels` 실행
- [ ] 컴파일 오류 해결

---

### Phase 4: SkillReference 확장
**목표**: 상태 효과 및 범위 효과 데이터 추가

**예상 소요**: 1시간
**난이도**: ⭐ (낮음)

#### 구체적 스텝

**Step 4.1: SkillReference 필드 추가**
- [ ] `ReferenceModels/Models/SkillReference.cs` 열기
- [ ] `StatusEffectType AppliedEffect` 필드 추가 (기본값: `StatusEffectType.None`)
- [ ] `float EffectDuration` 필드 추가 (기본값: `0f`)
- [ ] `float EffectMagnitude` 필드 추가 (기본값: `0f`)
- [ ] `float EffectRange` 필드 추가 (기본값: `0f`)
- [ ] `TargetType AffectedTargets` 필드 추가 (기본값: `TargetType.None`)
- [ ] 각 필드에 XML 문서화 주석 추가

**Step 4.2: 하위 호환성 확인**
- [ ] 기존 JSON 파일이 새 필드 없이도 로드되는지 확인
- [ ] 모든 새 필드가 기본값을 가지는지 검증

**Step 4.3: 빌드 확인**
- [ ] `dotnet build ReferenceModels` 실행
- [ ] 컴파일 오류 해결

---

### Phase 5: Validator 추가 및 확장
**목표**: 새로운 레퍼런스 데이터의 유효성 검증

**예상 소요**: 3-4시간
**난이도**: ⭐⭐ (중간)

#### 구체적 스텝

**Step 5.1: BuildingReferenceValidator 생성**
- [ ] `ReferenceModels/Validation/BuildingReferenceValidator.cs` 생성
- [ ] 기본 검증 로직 추가
  - DisplayName 비어있지 않은지
  - MaxHP > 0
  - Radius > 0
- [ ] Spawner 타입 전용 검증
  - `Type == Spawner`일 때 `SpawnUnitId` 필수
  - `SpawnCount > 0`
  - `SpawnInterval > 0`
- [ ] Defensive 타입 전용 검증
  - `Type == Defensive`일 때 `Damage > 0`
  - `AttackSpeed > 0`
  - `AttackRange > 0`
  - `CanTarget != None`
- [ ] 참조 무결성 검증
  - `SpawnUnitId`가 실제 Units에 존재하는지 확인

**Step 5.2: SpellReferenceValidator 생성**
- [ ] `ReferenceModels/Validation/SpellReferenceValidator.cs` 생성
- [ ] 기본 검증 로직 추가
  - DisplayName 비어있지 않은지
  - Radius > 0
- [ ] Instant 타입 검증
  - `Type == Instant`일 때 `Damage > 0` (피해 스펠인 경우)
- [ ] AreaOverTime 타입 검증
  - `Duration > 0`
  - `TickInterval > 0`
- [ ] Spawning 타입 검증
  - `SpawnUnitId` 필수
  - `SpawnCount > 0`
- [ ] 참조 무결성 검증
  - `SpawnUnitId`가 실제 Units에 존재하는지 확인

**Step 5.3: TowerReferenceValidator 생성**
- [ ] `ReferenceModels/Validation/TowerReferenceValidator.cs` 생성
- [ ] 기본 검증 로직 추가
  - DisplayName 비어있지 않은지
  - MaxHP > 0
  - Damage > 0
  - AttackSpeed > 0
  - AttackRadius > 0
  - Radius > 0
  - CanTarget != None

**Step 5.4: UnitReferenceValidator 확장**
- [ ] `ReferenceModels/Validation/UnitReferenceValidator.cs` 열기
- [ ] 새 필드 검증 추가
  - `AttackSpeed > 0` (공격하는 유닛인 경우)
  - `ShieldHP >= 0`
  - `SpawnCount > 0`

**Step 5.5: SkillReferenceValidator 확장**
- [ ] `ReferenceModels/Validation/SkillReferenceValidator.cs` 열기
- [ ] 상태 효과 필드 검증 추가
  - `AppliedEffect != None`일 때 `EffectDuration > 0` 검증
  - `EffectRange > 0` (범위 효과인 경우)

**Step 5.6: 검증 통합**
- [ ] `ReferenceManager` 또는 별도 `ValidationRunner` 클래스에서 모든 Validator 실행
- [ ] 검증 실패 시 명확한 오류 메시지 반환

**Step 5.7: 빌드 및 테스트**
- [ ] `dotnet build ReferenceModels` 실행
- [ ] 간단한 검증 테스트 작성 (선택)

---

### Phase 6: 샘플 JSON 데이터 생성
**목표**: 스펙의 예시를 기반으로 실제 JSON 파일 생성

**예상 소요**: 2-3시간
**난이도**: ⭐ (낮음)

#### 구체적 스텝

**Step 6.1: units.json 업데이트**
- [ ] 기존 `reference_data/units.json` 열기 (없으면 생성)
- [ ] Knight 데이터 추가 (섹션 10.1 참조)
  ```json
  {
    "knight": {
      "DisplayName": "Knight",
      "EntityType": "Troop",
      "Role": "MiniTank",
      "AttackType": "Melee",
      "MaxHP": 2400,
      "Damage": 200,
      "AttackSpeed": 1.1,
      "Speed": 1.0,
      "AttackRadius": 1.5,
      "Radius": 0.8,
      "Layer": "Ground",
      "CanTarget": "Ground",
      "TargetPriority": "Nearest",
      "Skills": []
    }
  }
  ```
- [ ] Baby Dragon 데이터 추가 (섹션 10.2)
- [ ] Prince 데이터 추가 (섹션 10.3)
- [ ] Golem 데이터 추가 (섹션 10.4)

**Step 6.2: skills.json 업데이트**
- [ ] 기존 `reference_data/skills.json` 열기 (없으면 생성)
- [ ] baby_dragon_splash 추가 (섹션 10.6)
  ```json
  {
    "baby_dragon_splash": {
      "DisplayName": "Splash Damage",
      "SplashRadius": 1.5,
      "SplashPercent": 0.3
    }
  }
  ```
- [ ] prince_charge 추가
- [ ] golem_death_spawn 추가

**Step 6.3: buildings.json 생성**
- [ ] `reference_data/buildings.json` 생성
- [ ] Tombstone 데이터 추가
  ```json
  {
    "tombstone": {
      "DisplayName": "Tombstone",
      "Type": "Spawner",
      "MaxHP": 800,
      "Radius": 2.0,
      "Lifetime": 40.0,
      "SpawnUnitId": "skeleton",
      "SpawnCount": 4,
      "SpawnInterval": 2.9,
      "FirstSpawnDelay": 3.0,
      "Skills": []
    }
  }
  ```
- [ ] Cannon 데이터 추가
- [ ] Tesla 데이터 추가

**Step 6.4: spells.json 생성**
- [ ] `reference_data/spells.json` 생성
- [ ] Fireball 데이터 추가
  ```json
  {
    "fireball": {
      "DisplayName": "Fireball",
      "Type": "Instant",
      "Radius": 2.5,
      "Damage": 572,
      "BuildingDamageMultiplier": 0.4,
      "AffectedTargets": "Ground | Air | Buildings",
      "CastDelay": 1.0
    }
  }
  ```
- [ ] Zap 데이터 추가
- [ ] Poison 데이터 추가
- [ ] Rage 데이터 추가

**Step 6.5: towers.json 생성**
- [ ] `reference_data/towers.json` 생성
- [ ] Princess Tower 데이터 추가
  ```json
  {
    "princess_tower": {
      "DisplayName": "Princess Tower",
      "Type": "Princess",
      "MaxHP": 2534,
      "Damage": 90,
      "AttackSpeed": 0.8,
      "AttackRadius": 7.5,
      "Radius": 2.5,
      "CanTarget": "Ground | Air"
    }
  }
  ```
- [ ] King Tower 데이터 추가

**Step 6.6: JSON 파싱 테스트**
- [ ] 프로그램 실행하여 모든 JSON 파일이 정상적으로 로드되는지 확인
- [ ] Validator가 통과하는지 확인
- [ ] 누락된 데이터나 오타 수정

---

## 4. 우선순위 및 의존성

### 의존성 그래프
```
Phase 1 (Enum 추가)
    ↓
Phase 2 (UnitReference 확장) ────┐
    ↓                            ↓
Phase 4 (SkillReference 확장)    Phase 3 (새 Reference 클래스)
    ↓                            ↓
    └──────── Phase 5 (Validator) ───────┘
                    ↓
            Phase 6 (샘플 JSON)
```

### 권장 순서
1. **Phase 1** - 모든 Phase의 기반이 되는 Enum 타입 먼저 추가
2. **Phase 2** - 가장 핵심적인 UnitReference 확장
3. **Phase 4** - UnitReference와 밀접한 SkillReference 확장
4. **Phase 3** - 독립적인 Building, Spell, Tower 클래스 추가
5. **Phase 5** - 데이터 품질 보증을 위한 Validator
6. **Phase 6** - 실제 데이터로 검증 및 테스트

---

## 5. 설계 원칙 및 고려사항

### 5.1 하위 호환성
- 모든 새 필드는 **기본값** 제공
- 기존 JSON 파일은 새 필드 없이도 정상 로드
- 선택적 필드는 **nullable 타입** 사용 (`string?`, `int?`)

### 5.2 확장성
- Enum 타입은 추후 확장 가능하도록 설계
- StatusEffect, Ability는 Flags 패턴 사용 → 복합 효과 표현 가능
- JSON 스키마는 평탄(flat)하게 유지 → nested object 최소화

### 5.3 검증 전략
- **타입별 필수 필드 검증**
  - BuildingType.Spawner → SpawnUnitId 필수
  - BuildingType.Defensive → Damage, AttackSpeed 필수
- **참조 무결성 검증**
  - SpawnUnitId가 실제 Units에 존재하는지
  - Skills 목록의 ID가 실제 Skills에 존재하는지
- **값 범위 검증**
  - HP > 0, Speed > 0, AttackSpeed > 0 등

### 5.4 데이터 표현 방식 (Flat Structure)
- **채택**: 모든 스킬/스펠/건물 타입의 필드를 하나의 클래스에 평탄하게 배치
- **장점**:
  - JSON 직렬화/역직렬화 단순
  - 타입별 핸들러 불필요
  - 데이터 읽기 편리
- **단점**:
  - 사용하지 않는 필드가 많음 (Spawner 건물은 Defensive 필드 사용 안 함)
- **이유**: 현재 `SkillReference`가 이미 이 방식 사용 중 → 일관성 유지

---

## 6. 예상 산출물

### 6.1 새로 생성될 파일 (18개)
```
ReferenceModels/
├── Models/
│   ├── Enums/
│   │   ├── EntityType.cs              [NEW]
│   │   ├── AttackType.cs              [NEW]
│   │   ├── BuildingType.cs            [NEW]
│   │   ├── SpellType.cs               [NEW]
│   │   ├── TowerType.cs               [NEW]
│   │   ├── StatusEffectType.cs        [NEW]
│   │   └── AbilityType.cs             [NEW]
│   ├── BuildingReference.cs           [NEW]
│   ├── SpellReference.cs              [NEW]
│   └── TowerReference.cs              [NEW]
└── Validation/
    ├── BuildingReferenceValidator.cs  [NEW]
    ├── SpellReferenceValidator.cs     [NEW]
    └── TowerReferenceValidator.cs     [NEW]

reference_data/
├── buildings.json                     [NEW]
├── spells.json                        [NEW]
└── towers.json                        [NEW]
```

### 6.2 수정될 파일 (6개)
```
ReferenceModels/
├── Models/
│   ├── Enums/
│   │   └── UnitRole.cs                [MODIFIED]
│   ├── UnitReference.cs               [MODIFIED]
│   └── SkillReference.cs              [MODIFIED]
├── Validation/
│   ├── UnitReferenceValidator.cs      [MODIFIED]
│   └── SkillReferenceValidator.cs     [MODIFIED]
└── Infrastructure/
    ├── ReferenceManager.cs            [MODIFIED]
    └── ReferenceHandlers.cs           [MODIFIED]

reference_data/
├── units.json                         [MODIFIED]
└── skills.json                        [MODIFIED]
```

---

## 7. 향후 확장 계획 (참고)

이 계획 이후 고려할 사항들:

1. **레벨별 스탯 차이**
   - 현재: 단일 레벨 데이터만 지원
   - 향후: 레벨별 HP, Damage 증가치 표현

2. **복합 스킬 조합**
   - 현재: 스킬 ID 목록만 참조
   - 향후: 스킬 간 상호작용, 조건부 발동 등

3. **동적 효과 시스템**
   - 현재: 정적 StatusEffect 플래그
   - 향후: 효과 스택, 갱신, 제거 로직

4. **AI 패턴 데이터**
   - 현재: TargetPriority만 표현
   - 향후: 복잡한 AI 행동 트리 데이터

---

## 8. 체크리스트 요약

### Phase 1: Enum 추가 (8개 파일)
- [ ] EntityType.cs
- [ ] AttackType.cs
- [ ] UnitRole.cs 확장
- [ ] BuildingType.cs
- [ ] SpellType.cs
- [ ] TowerType.cs
- [ ] StatusEffectType.cs
- [ ] AbilityType.cs

### Phase 2: UnitReference 확장 (1개 파일)
- [ ] UnitReference.cs

### Phase 3: 새 Reference 클래스 (4개 파일)
- [ ] BuildingReference.cs
- [ ] SpellReference.cs
- [ ] TowerReference.cs
- [ ] ReferenceHandlers.cs 확장

### Phase 4: SkillReference 확장 (1개 파일)
- [ ] SkillReference.cs

### Phase 5: Validator (5개 파일)
- [ ] BuildingReferenceValidator.cs
- [ ] SpellReferenceValidator.cs
- [ ] TowerReferenceValidator.cs
- [ ] UnitReferenceValidator.cs 확장
- [ ] SkillReferenceValidator.cs 확장

### Phase 6: 샘플 JSON (5개 파일)
- [ ] units.json 업데이트
- [ ] skills.json 업데이트
- [ ] buildings.json 생성
- [ ] spells.json 생성
- [ ] towers.json 생성

---

## 9. 참고 자료
- **스펙 문서**: `docs/unit-system-spec.md`
- **기존 구조**: `ReferenceModels/Models/`, `ReferenceModels/Infrastructure/`
- **JSON 데이터**: `reference_data/units.json`, `reference_data/skills.json`
