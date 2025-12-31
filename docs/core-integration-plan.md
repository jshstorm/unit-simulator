# Core Integration Plan

## 문서 개요
- **작성일**: 2025-12-31
- **목적**: 확장된 Reference Models를 SimulatorCore에 통합
- **선행 작업**: reference-models-expansion-plan.md (Phase 1-6), reference-models-testing-plan.md (Phase 1-5)

---

## 1. 현재 상태 분석

### 1.1 기존 Core 구조

#### ReferenceExtensions.cs
- **UnitReference.CreateUnit()**: UnitReference → Unit 변환
  - 현재 지원 필드: DisplayName, MaxHP, Speed, Damage, AttackRadius, Radius, Role, Layer, CanTarget, TargetPriority, Skills
  - **미지원 필드**: EntityType, AttackType, AttackSpeed, ShieldHP, SpawnCount

- **SkillReference.ToAbilityData()**: SkillReference → AbilityData 변환
  - 현재 지원: ChargeAttack, SplashDamage, Shield, DeathSpawn, DeathDamage
  - **미지원**: 상태 효과 필드 (AppliedEffect, EffectDuration, EffectMagnitude, EffectRange, AffectedTargets)

#### SimulatorCore.cs
- **ReferenceManager 사용**: JSON 파일에서 로드된 읽기 전용 데이터
- **UnitRegistry**: Obsolete로 표시 (ReferenceManager로 대체)
- **Tower 시스템**: 존재하지만 TowerReference 미사용

#### InitialSetup.cs
- **TowerSetup**: 타워 초기 설정 (Type, Faction, Position, InitialHP)
- **현재**: 하드코딩된 타워 스탯 사용
- **목표**: TowerReference 기반으로 타워 생성

### 1.2 통합 필요 항목

1. **UnitReference 새 필드 반영**
   - EntityType, AttackType, AttackSpeed → Unit 생성 시 활용
   - ShieldHP → Unit 초기 쉴드 설정
   - SpawnCount → 배치 시 여러 유닛 생성 (Swarm)

2. **SkillReference 상태 효과 지원**
   - AppliedEffect, EffectDuration, EffectMagnitude → 상태 효과 Ability 생성
   - EffectRange, AffectedTargets → 범위 효과 Ability

3. **TowerReference 통합**
   - TowerReference → Tower 인스턴스 생성
   - InitialSetup에서 TowerReference 활용

4. **Building, Spell 준비**
   - BuildingReference, SpellReference를 위한 확장 메서드 준비
   - 실제 Building, Spell 엔티티는 미래 작업

---

## 2. 통합 전략

### 2.1 설계 원칙

1. **하위 호환성 유지**: 기존 코드가 계속 작동해야 함
2. **점진적 통합**: 기능별로 나누어 단계적으로 통합
3. **테스트 우선**: 각 단계마다 테스트 작성 및 검증
4. **문서화**: 변경사항 명확히 문서화

### 2.2 통합 범위

#### 즉시 통합 (이번 계획)
- ✅ UnitReference 새 필드 → Unit 생성
- ✅ SkillReference 상태 효과 → AbilityData
- ✅ TowerReference → Tower 생성

#### 향후 통합 (미래 작업)
- ⏳ BuildingReference → Building 엔티티
- ⏳ SpellReference → Spell 엔티티
- ⏳ 상태 효과 시스템 구현

---

## 3. 구현 계획

### Phase 1: UnitReference 확장 메서드 업데이트
**목표**: CreateUnit()에 새 필드 반영

**예상 소요**: 1-2시간
**난이도**: ⭐⭐ (중간)

#### 구체적 스텝

**Step 1.1: Unit 클래스 확인**
- [ ] Unit 생성자 시그니처 확인
- [ ] AttackSpeed, ShieldHP 필드 존재 확인
- [ ] SpawnCount는 CreateUnit() 로직에서 처리 필요 (여러 유닛 생성)

**Step 1.2: ReferenceExtensions.CreateUnit() 수정**
- [ ] AttackSpeed 필드 추가 (Unit 생성자 파라미터로 전달)
- [ ] ShieldHP 필드 추가 (Unit 생성 후 설정 또는 생성자 파라미터)
- [ ] 주석 업데이트

**Step 1.3: SpawnCount 지원 추가**
- [ ] `CreateUnits()` 새 메서드 생성 (복수 유닛 생성)
  - SpawnCount > 1이면 여러 유닛 생성
  - SpawnRadius 내에 분산 배치
- [ ] 기존 `CreateUnit()`은 단일 유닛 생성으로 유지 (하위 호환성)

**Step 1.4: 테스트 작성**
- [ ] CreateUnit() 새 필드 테스트
- [ ] CreateUnits() 복수 유닛 테스트

---

### Phase 2: SkillReference 상태 효과 지원
**목표**: ToAbilityData()에 상태 효과 변환 추가

**예상 소요**: 2-3시간
**난이도**: ⭐⭐⭐ (높음)

#### 구체적 스텝

**Step 2.1: StatusEffectAbilityData 클래스 설계**
- [ ] 상태 효과를 부여하는 Ability 클래스 설계
  ```csharp
  public class StatusEffectAbilityData : AbilityData
  {
      public StatusEffectType AppliedEffect { get; init; }
      public float EffectDuration { get; init; }
      public float EffectMagnitude { get; init; }
      public float EffectRange { get; init; }
      public TargetType AffectedTargets { get; init; }
  }
  ```
- [ ] AbilityType enum에 StatusEffect 추가 (필요시)

**Step 2.2: ToAbilityData() 확장**
- [ ] 기존 스킬 타입에 상태 효과 필드 추가
  - ChargeAttack + AppliedEffect → 돌진 시 상태 효과 부여
  - SplashDamage + AppliedEffect → 범위 피해 + 상태 효과
- [ ] 상태 효과 전용 스킬 타입 추가 (필요시)

**Step 2.3: 제약사항 처리**
- [ ] 현재 AbilityData는 Core에서 정의됨 (ReferenceModels가 아님)
- [ ] StatusEffectType, TargetType은 ReferenceModels.Models.Enums에 정의됨
- [ ] Core에서 ReferenceModels.Models.Enums를 참조해야 함 (이미 참조 중)

**Step 2.4: 테스트 작성**
- [ ] 상태 효과 Ability 변환 테스트

---

### Phase 3: TowerReference 통합
**목표**: TowerReference를 사용하여 Tower 인스턴스 생성

**예상 소요**: 2-3시간
**난이도**: ⭐⭐ (중간)

#### 구체적 스텝

**Step 3.1: Tower 클래스 확인**
- [ ] Tower 생성자 시그니처 확인
- [ ] 현재 Tower가 어떻게 생성되는지 확인 (하드코딩 여부)

**Step 3.2: TowerReference.CreateTower() 확장 메서드 작성**
```csharp
public static Tower CreateTower(
    this TowerReference towerRef,
    int id,
    UnitFaction faction,
    Vector2? position = null)
{
    return new Tower(
        position: position ?? GetDefaultPosition(towerRef.Type, faction),
        radius: towerRef.Radius,
        hp: towerRef.MaxHP,
        id: id,
        faction: faction,
        damage: towerRef.Damage,
        attackSpeed: towerRef.AttackSpeed,
        attackRadius: towerRef.AttackRadius,
        canTarget: towerRef.CanTarget,
        type: towerRef.Type
    );
}
```

**Step 3.3: InitialSetup 업데이트**
- [ ] TowerSetup에 TowerReferenceId 필드 추가 (선택적)
  - null이면 기본 타워 스탯 사용 (하위 호환성)
  - 설정되면 ReferenceManager에서 조회
- [ ] SimulatorCore.Initialize()에서 TowerReference 기반 타워 생성

**Step 3.4: 기본 TowerReference 설정**
- [ ] towers.json의 princess_tower, king_tower 사용
- [ ] TowerSetupDefaults에서 TowerReferenceId 설정

**Step 3.5: 테스트 작성**
- [ ] CreateTower() 테스트
- [ ] InitialSetup 기반 타워 생성 테스트

---

### Phase 4: Building, Spell 확장 메서드 준비
**목표**: 미래 Building, Spell 엔티티를 위한 확장 메서드 스켈레톤

**예상 소요**: 1시간
**난이도**: ⭐ (낮음)

#### 구체적 스텝

**Step 4.1: BuildingReference.CreateBuilding() 스켈레톤**
```csharp
// TODO: Building 엔티티 구현 시 활성화
// public static Building CreateBuilding(
//     this BuildingReference buildingRef,
//     int id,
//     UnitFaction faction,
//     Vector2 position)
// {
//     // 구현 대기
// }
```

**Step 4.2: SpellReference.CreateSpell() 스켈레톤**
```csharp
// TODO: Spell 엔티티 구현 시 활성화
// public static Spell CreateSpell(
//     this SpellReference spellRef,
//     UnitFaction faction,
//     Vector2 position)
// {
//     // 구현 대기
// }
```

**Step 4.3: 문서화**
- [ ] 향후 Building, Spell 통합 가이드 작성

---

### Phase 5: Core.Tests 업데이트
**목표**: 기존 Core.Tests 업데이트 및 새 테스트 추가

**예상 소요**: 2-3시간
**난이도**: ⭐⭐ (중간)

#### 구체적 스텝

**Step 5.1: ReferenceManagerTests 업데이트**
- [ ] UnitReference_CreateUnit_* 테스트에 새 필드 추가
  - AttackSpeed, ShieldHP 검증
- [ ] CreateUnits() 테스트 추가 (SpawnCount)
- [ ] SkillReference_ToAbilityData_* 테스트에 상태 효과 추가

**Step 5.2: 새 테스트 파일 작성**
- [ ] TowerReferenceExtensionsTests.cs
  - CreateTower() 테스트
  - TowerReference 필드 매핑 테스트

**Step 5.3: 통합 테스트**
- [ ] ReferenceManager + CreateUnit() 통합 테스트
- [ ] ReferenceManager + CreateTower() 통합 테스트

---

## 4. 파일 구조 변경

### 4.1 수정될 파일

```
UnitSimulator.Core/
├── ReferenceExtensions.cs                    [MODIFIED]
│   ├── CreateUnit() 수정
│   ├── CreateUnits() 추가
│   ├── ToAbilityData() 확장
│   ├── CreateTower() 추가
│   └── CreateBuilding(), CreateSpell() 스켈레톤
├── AbilityData.cs (또는 관련 파일)           [MODIFIED]
│   └── StatusEffectAbilityData 추가 (필요시)
└── Contracts/
    └── InitialSetup.cs                       [MODIFIED]
        └── TowerSetup에 TowerReferenceId 추가

UnitSimulator.Core.Tests/
└── References/
    ├── ReferenceManagerTests.cs              [MODIFIED]
    └── TowerReferenceExtensionsTests.cs      [NEW]
```

### 4.2 신규 파일 (선택적)

```
UnitSimulator.Core/
└── StatusEffectAbilityData.cs                [NEW - 필요시]
```

---

## 5. 주요 설계 결정

### 5.1 Unit 생성 시 SpawnCount 처리

**옵션 1**: CreateUnit()에서 직접 처리
```csharp
public static List<Unit> CreateUnit(...) // 복수 반환
{
    var units = new List<Unit>();
    for (int i = 0; i < unitRef.SpawnCount; i++)
    {
        // 위치 분산...
    }
}
```
- 장점: 단일 메서드
- 단점: 기존 CreateUnit() 시그니처 변경 (하위 호환 깨짐)

**옵션 2**: CreateUnits() 새 메서드 (✅ 채택)
```csharp
public static Unit CreateUnit(...)  // 단일 유닛
public static List<Unit> CreateUnits(...) // 복수 유닛 (SpawnCount 고려)
```
- 장점: 하위 호환성 유지
- 단점: 메서드 2개

### 5.2 상태 효과 Ability 처리

**옵션 1**: 기존 Ability에 상태 효과 필드 추가
```csharp
public class SplashDamageData : AbilityData
{
    public float Radius { get; init; }
    public float DamageFalloff { get; init; }
    public StatusEffectType AppliedEffect { get; init; } // 추가
    public float EffectDuration { get; init; } // 추가
}
```
- 장점: 기존 스킬 타입 재사용
- 단점: Ability 클래스 비대화

**옵션 2**: 독립적인 StatusEffectAbilityData (✅ 채택)
```csharp
public class StatusEffectAbilityData : AbilityData
{
    public StatusEffectType AppliedEffect { get; init; }
    public float EffectDuration { get; init; }
    ...
}
```
- 장점: 관심사 분리, 명확한 역할
- 단점: Ability 클래스 증가

**결정**: 옵션 2 채택 후, 필요시 기존 Ability에도 상태 효과 필드 추가 (복합 효과)

### 5.3 TowerReference 활용

**현재 접근 방식**:
1. towers.json에서 TowerReference 로드
2. InitialSetup에서 TowerReferenceId 참조 (선택적)
3. TowerReference.CreateTower()로 Tower 인스턴스 생성
4. 기존 하드코딩 방식도 유지 (하위 호환)

---

## 6. 우선순위 및 의존성

### 의존성 그래프

```
Phase 1 (UnitReference)
    ↓
Phase 2 (SkillReference) ────┐
    ↓                         ↓
Phase 3 (TowerReference)      Phase 4 (Building/Spell 준비)
    ↓                         ↓
    └──── Phase 5 (Core.Tests) ────┘
```

### 권장 순서

1. **Phase 1** - UnitReference (기본)
2. **Phase 3** - TowerReference (독립적, 비교적 단순)
3. **Phase 2** - SkillReference (복잡, 상태 효과 시스템 필요)
4. **Phase 4** - Building/Spell 준비 (미래 작업)
5. **Phase 5** - Core.Tests (검증)

---

## 7. 체크리스트 요약

### Phase 1: UnitReference 확장
- [ ] Unit 생성자 확인
- [ ] CreateUnit() 수정 (AttackSpeed, ShieldHP)
- [ ] CreateUnits() 추가 (SpawnCount)
- [ ] 테스트 작성

### Phase 2: SkillReference 상태 효과
- [ ] StatusEffectAbilityData 설계
- [ ] ToAbilityData() 확장
- [ ] 테스트 작성

### Phase 3: TowerReference 통합
- [ ] Tower 클래스 확인
- [ ] CreateTower() 확장 메서드
- [ ] InitialSetup 업데이트
- [ ] 테스트 작성

### Phase 4: Building/Spell 준비
- [ ] 확장 메서드 스켈레톤 작성
- [ ] 문서화

### Phase 5: Core.Tests 업데이트
- [ ] ReferenceManagerTests 업데이트
- [ ] TowerReferenceExtensionsTests 작성
- [ ] 통합 테스트

---

## 8. 성공 기준

### 8.1 정량적 지표

- [ ] 모든 기존 테스트 통과 (회귀 없음)
- [ ] 최소 10개 이상의 새 테스트 추가
- [ ] CreateUnit()에서 새 필드 100% 반영
- [ ] TowerReference 기반 타워 생성 성공

### 8.2 정성적 지표

- [ ] 하위 호환성 유지 (기존 코드 동작)
- [ ] 새 Reference 필드가 Core에서 활용됨
- [ ] 코드 가독성 및 유지보수성 향상
- [ ] 명확한 문서화

---

## 9. 향후 계획

통합 완료 후 고려할 사항:

1. **상태 효과 시스템 구현**
   - StatusEffect 클래스
   - Unit에 ActiveEffects 리스트
   - 상태 효과 적용/해제 로직

2. **Building 엔티티 구현**
   - Building 클래스
   - Spawner, Defensive 타입별 로직
   - BuildingReference 완전 통합

3. **Spell 엔티티 구현**
   - Spell 클래스
   - Instant, AreaOverTime, Utility, Spawning 타입별 로직
   - SpellReference 완전 통합

4. **성능 최적화**
   - CreateUnits() 벌크 생성 최적화
   - 상태 효과 업데이트 최적화
