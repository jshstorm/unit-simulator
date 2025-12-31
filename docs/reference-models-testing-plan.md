# Reference Models Testing Plan

## 문서 개요
- **작성일**: 2025-12-31
- **목적**: Reference Models 확장에 대한 테스트 작성 및 검증 계획
- **선행 작업**: reference-models-expansion-plan.md (Phase 1-6 완료)

---

## 1. 현재 상태 분석

### 1.1 기존 테스트 구조

#### 테스트 프레임워크
- **xUnit**: 테스트 러너
- **FluentAssertions**: Assertion 라이브러리
- **TargetFramework**: net9.0

#### 기존 테스트
- **위치**: `UnitSimulator.Core.Tests/References/ReferenceManagerTests.cs`
- **테스트 수**: 7개
- **커버리지**:
  - ✅ ReferenceManager.LoadAll() 기본 로드
  - ✅ Units, Skills JSON 파싱
  - ✅ 핸들러 누락 시 경고
  - ✅ UnitReference.CreateUnit() (Core에 존재)
  - ✅ SkillReference.ToAbilityData() (Core에 존재)
  - ✅ ReferenceTable.GetAll()

### 1.2 테스트 필요 항목

#### Phase 1-6에서 추가된 구조
1. **새로운 Enum 타입** (7개)
   - EntityType, AttackType, BuildingType, SpellType, TowerType, StatusEffectType, AbilityType
2. **확장된 Reference 클래스** (2개)
   - UnitReference (5개 새 필드)
   - SkillReference (5개 새 필드)
3. **새로운 Reference 클래스** (3개)
   - BuildingReference, SpellReference, TowerReference
4. **Validator** (5개)
   - UnitReferenceValidator (확장), SkillReferenceValidator (확장)
   - BuildingReferenceValidator, SpellReferenceValidator, TowerReferenceValidator
5. **JSON 데이터** (3개 신규, 1개 업데이트)
   - buildings.json, spells.json, towers.json, units.json (업데이트)

---

## 2. 테스트 전략

### 2.1 테스트 프로젝트 구조

```
ReferenceModels.Tests/                    (새 프로젝트)
├── ReferenceModels.Tests.csproj
├── Serialization/
│   ├── UnitReferenceSerializationTests.cs
│   ├── SkillReferenceSerializationTests.cs
│   ├── BuildingReferenceSerializationTests.cs
│   ├── SpellReferenceSerializationTests.cs
│   └── TowerReferenceSerializationTests.cs
├── Validation/
│   ├── UnitReferenceValidatorTests.cs
│   ├── SkillReferenceValidatorTests.cs
│   ├── BuildingReferenceValidatorTests.cs
│   ├── SpellReferenceValidatorTests.cs
│   └── TowerReferenceValidatorTests.cs
├── Infrastructure/
│   ├── ReferenceHandlersTests.cs
│   └── ReferenceManagerTests.cs
└── Integration/
    └── JsonLoadingTests.cs

UnitSimulator.Core.Tests/                 (기존 프로젝트)
└── References/
    └── ReferenceManagerTests.cs           (업데이트)
```

### 2.2 테스트 범위

#### 단위 테스트 (ReferenceModels.Tests)
- **Serialization**: JSON ↔ C# 객체 변환 검증
- **Validation**: Validator 로직 검증
- **Infrastructure**: ReferenceHandlers, ReferenceManager 로직 검증

#### 통합 테스트 (ReferenceModels.Tests/Integration)
- **실제 JSON 파일 로드**: data/references/*.json 파일 정상 로드 검증
- **참조 무결성**: SpawnUnitId 등이 실제 존재하는 유닛을 참조하는지 검증

---

## 3. 구현 계획

### Phase 1: 테스트 프로젝트 설정
**목표**: ReferenceModels.Tests 프로젝트 생성 및 기본 구조 구축

**예상 소요**: 30분
**난이도**: ⭐ (낮음)

#### 구체적 스텝

**Step 1.1: 테스트 프로젝트 생성**
- [ ] `dotnet new xunit -n ReferenceModels.Tests` 실행
- [ ] ReferenceModels.csproj 참조 추가
- [ ] FluentAssertions 패키지 추가
- [ ] .csproj 파일 정리 (Nullable, ImplicitUsings 설정)

**Step 1.2: 디렉토리 구조 생성**
- [ ] `Serialization/` 폴더 생성
- [ ] `Validation/` 폴더 생성
- [ ] `Infrastructure/` 폴더 생성
- [ ] `Integration/` 폴더 생성

**Step 1.3: 기본 헬퍼 클래스 작성**
- [ ] `TestHelpers/JsonTestHelper.cs` 작성 (JSON 문자열 생성 유틸)
- [ ] `TestHelpers/ReferenceTestFactory.cs` 작성 (테스트용 Reference 객체 생성)

**Step 1.4: 빌드 확인**
- [ ] `dotnet build ReferenceModels.Tests` 실행
- [ ] 모든 프로젝트가 정상적으로 빌드되는지 확인

---

### Phase 2: Serialization 테스트
**목표**: 모든 Reference 클래스의 JSON 직렬화/역직렬화 테스트

**예상 소요**: 2-3시간
**난이도**: ⭐⭐ (중간)

#### 구체적 스텝

**Step 2.1: UnitReferenceSerializationTests**
- [ ] 기본 필드 직렬화 테스트 (DisplayName, MaxHP, Damage 등)
- [ ] 새 필드 직렬화 테스트 (EntityType, AttackType, AttackSpeed, ShieldHP, SpawnCount)
- [ ] Enum 필드의 문자열 변환 테스트 (JsonStringEnumConverter)
- [ ] 기본값 동작 테스트 (새 필드 누락 시 기본값 적용)
- [ ] 왕복 테스트 (객체 → JSON → 객체 변환 후 동일성 검증)

**Step 2.2: SkillReferenceSerializationTests**
- [ ] 기존 필드 직렬화 테스트 (Type, ChargeAttack 필드 등)
- [ ] 새 필드 직렬화 테스트 (AppliedEffect, EffectDuration 등)
- [ ] StatusEffectType enum의 Flags 직렬화 테스트
- [ ] 왕복 테스트

**Step 2.3: BuildingReferenceSerializationTests**
- [ ] 모든 필드 직렬화 테스트
- [ ] BuildingType enum 직렬화 테스트
- [ ] Spawner 전용 필드 테스트
- [ ] Defensive 전용 필드 테스트
- [ ] 왕복 테스트

**Step 2.4: SpellReferenceSerializationTests**
- [ ] 모든 필드 직렬화 테스트
- [ ] SpellType enum 직렬화 테스트
- [ ] 왕복 테스트

**Step 2.5: TowerReferenceSerializationTests**
- [ ] 모든 필드 직렬화 테스트
- [ ] TowerType enum 직렬화 테스트
- [ ] 왕복 테스트

---

### Phase 3: Validation 테스트
**목표**: 모든 Validator의 검증 로직 테스트

**예상 소요**: 3-4시간
**난이도**: ⭐⭐⭐ (높음)

#### 구체적 스텝

**Step 3.1: UnitReferenceValidatorTests**
- [ ] 유효한 데이터 테스트 (`IsValid == true`)
- [ ] MaxHP ≤ 0 오류 테스트
- [ ] AttackSpeed ≤ 0 오류 테스트
- [ ] ShieldHP < 0 오류 테스트
- [ ] SpawnCount ≤ 0 오류 테스트
- [ ] DisplayName 비어있을 때 경고 테스트
- [ ] 여러 오류 동시 발생 테스트

**Step 3.2: SkillReferenceValidatorTests**
- [ ] ChargeAttack 타입 필수 필드 테스트
- [ ] SplashDamage 타입 필수 필드 테스트
- [ ] Shield 타입 필수 필드 테스트
- [ ] DeathSpawn 타입 필수 필드 테스트
- [ ] DeathDamage 타입 필수 필드 테스트
- [ ] 상태 효과 필드 검증 테스트
  - AppliedEffect 설정 시 EffectDuration > 0 검증
  - EffectRange ≥ 0 검증
- [ ] 알 수 없는 타입 경고 테스트

**Step 3.3: BuildingReferenceValidatorTests**
- [ ] Spawner 타입 필수 필드 테스트
  - SpawnUnitId 비어있지 않은지
  - SpawnCount > 0
  - SpawnInterval > 0
- [ ] Defensive 타입 필수 필드 테스트
  - Damage > 0
  - AttackSpeed > 0
  - AttackRange > 0
  - CanTarget != None
- [ ] Utility 타입 기본 필드 테스트
- [ ] 기본 필드 검증 (MaxHP > 0, Radius > 0, Lifetime ≥ 0)

**Step 3.4: SpellReferenceValidatorTests**
- [ ] Instant 타입 검증
  - Damage 또는 AppliedEffect 중 하나는 설정되어야 함
- [ ] AreaOverTime 타입 검증
  - Duration > 0
  - DamagePerTick ≥ 0
  - TickInterval > 0 (DamagePerTick > 0인 경우)
- [ ] Utility 타입 검증
  - AppliedEffect != None 경고
- [ ] Spawning 타입 검증
  - SpawnUnitId 비어있지 않은지
  - SpawnCount > 0

**Step 3.5: TowerReferenceValidatorTests**
- [ ] 모든 필수 필드 검증 (MaxHP, Damage, AttackSpeed, AttackRadius, Radius)
- [ ] CanTarget != None 검증
- [ ] Radius > AttackRadius 경고 테스트

---

### Phase 4: Infrastructure 테스트
**목표**: ReferenceHandlers, ReferenceManager 로직 테스트

**예상 소요**: 2-3시간
**난이도**: ⭐⭐ (중간)

#### 구체적 스텝

**Step 4.1: ReferenceHandlersTests**
- [ ] ParseUnits() 테스트
  - 유효한 JSON 파싱
  - 빈 JSON 파싱 (빈 Dictionary 반환)
  - 잘못된 JSON 형식 처리
- [ ] ParseSkills() 테스트
- [ ] ParseBuildings() 테스트
- [ ] ParseSpells() 테스트
- [ ] ParseTowers() 테스트
- [ ] JSON 옵션 테스트 (대소문자 무시, 주석 허용, 후행 쉼표 허용)

**Step 4.2: ReferenceManagerTests**
- [ ] CreateWithDefaultHandlers() 테스트
  - 모든 핸들러가 등록되었는지 확인 (units, skills, buildings, spells, towers)
- [ ] LoadAll() 테스트
  - 여러 JSON 파일 동시 로드
  - 존재하지 않는 디렉토리 처리
  - 핸들러 없는 파일 건너뛰기 경고
- [ ] 편의 접근자 테스트
  - Units, Skills, Buildings, Spells, Towers 프로퍼티
- [ ] GetTable<T>() 테스트
- [ ] HasTable() 테스트
- [ ] RegisterTable() 테스트 (테스트용)

---

### Phase 5: 통합 테스트
**목표**: 실제 JSON 파일 로드 및 참조 무결성 검증

**예상 소요**: 2-3시간
**난이도**: ⭐⭐ (중간)

#### 구체적 스텝

**Step 5.1: JsonLoadingTests 작성**
- [ ] 실제 data/references/ 디렉토리의 모든 JSON 파일 로드 테스트
  - units.json, skills.json, buildings.json, spells.json, towers.json
- [ ] 로드된 데이터 기본 검증
  - 각 테이블의 레코드 수 확인
  - 특정 유닛/스킬/건물/스펠/타워 존재 확인
- [ ] 참조 무결성 검증
  - 유닛의 Skills 목록에 있는 ID가 실제 Skills 테이블에 존재하는지
  - BuildingReference의 SpawnUnitId가 실제 Units에 존재하는지
  - SpellReference의 SpawnUnitId가 실제 Units에 존재하는지
  - SkillReference의 SpawnUnitId가 실제 Units에 존재하는지
- [ ] Validator 실행 테스트
  - 모든 유닛에 대해 UnitReferenceValidator 실행
  - 모든 스킬에 대해 SkillReferenceValidator 실행
  - 모든 건물에 대해 BuildingReferenceValidator 실행
  - 모든 스펠에 대해 SpellReferenceValidator 실행
  - 모든 타워에 대해 TowerReferenceValidator 실행
  - 검증 오류가 없는지 확인 (IsValid == true)

**Step 5.2: 샘플 데이터 검증**
- [ ] Knight 유닛 데이터 검증
  - EntityType == Troop
  - Role == MiniTank
  - AttackType == Melee
  - AttackSpeed == 1.1
- [ ] Prince 유닛 데이터 검증
  - Role == GlassCannon
  - AttackType == MeleeMedium
  - Skills에 "prince_charge" 포함
- [ ] Tombstone 건물 데이터 검증
  - Type == Spawner
  - SpawnUnitId == "skeleton"
- [ ] Fireball 스펠 데이터 검증
  - Type == Instant
  - Damage > 0
  - BuildingDamageMultiplier < 1.0

---

### Phase 6: Core.Tests 업데이트
**목표**: 기존 Core.Tests의 ReferenceManagerTests 업데이트

**예상 소요**: 1-2시간
**난이도**: ⭐ (낮음)

#### 구체적 스텝

**Step 6.1: 새로운 타입 테스트 추가**
- [ ] Buildings 로드 테스트 추가
- [ ] Spells 로드 테스트 추가
- [ ] Towers 로드 테스트 추가

**Step 6.2: 기존 테스트 업데이트**
- [ ] UnitReference 테스트에 새 필드 추가
  - EntityType, AttackType, AttackSpeed, ShieldHP, SpawnCount
- [ ] SkillReference 테스트에 새 필드 추가
  - AppliedEffect, EffectDuration, EffectMagnitude, EffectRange, AffectedTargets

**Step 6.3: 삭제 또는 수정이 필요한 테스트**
- [ ] `UnitReference_CreateUnit_*` 테스트 확인
  - ReferenceModels에는 CreateUnit() 메서드가 없음
  - Core에서 구현되었는지 확인
  - 필요시 테스트 수정 또는 삭제
- [ ] `SkillReference_ToAbilityData_*` 테스트 확인
  - 마찬가지로 ToAbilityData() 메서드 확인

---

## 4. 테스트 작성 가이드라인

### 4.1 테스트 네이밍 컨벤션

```csharp
// 패턴: MethodName_Scenario_ExpectedBehavior
[Fact]
public void Validate_ValidUnit_ShouldReturnSuccess()

[Fact]
public void Validate_NegativeHP_ShouldReturnError()

[Fact]
public void ParseUnits_EmptyJson_ShouldReturnEmptyTable()
```

### 4.2 Arrange-Act-Assert 패턴

```csharp
[Fact]
public void Validate_SpawnerWithoutUnitId_ShouldReturnError()
{
    // Arrange
    var validator = new BuildingReferenceValidator();
    var building = new BuildingReference
    {
        Type = BuildingType.Spawner,
        SpawnUnitId = "" // 잘못된 데이터
    };

    // Act
    var result = validator.Validate(building, "test_building");

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Contains("SpawnUnitId"));
}
```

### 4.3 FluentAssertions 사용

```csharp
// 권장
result.IsValid.Should().BeTrue();
result.Errors.Should().BeEmpty();
unit.MaxHP.Should().Be(100);
unit.Skills.Should().HaveCount(2);
unit.Skills.Should().Contain("skill_id");

// 피해야 할 것
Assert.True(result.IsValid);
Assert.Empty(result.Errors);
Assert.Equal(100, unit.MaxHP);
```

### 4.4 테스트 데이터 생성

```csharp
// TestHelpers/ReferenceTestFactory.cs
public static class ReferenceTestFactory
{
    public static UnitReference CreateValidUnit(string displayName = "Test Unit")
    {
        return new UnitReference
        {
            DisplayName = displayName,
            MaxHP = 100,
            Damage = 50,
            AttackSpeed = 1.0f,
            // ... 모든 필수 필드
        };
    }

    public static BuildingReference CreateValidSpawner(string spawnUnitId = "skeleton")
    {
        return new BuildingReference
        {
            DisplayName = "Test Spawner",
            Type = BuildingType.Spawner,
            MaxHP = 500,
            Radius = 2.0f,
            SpawnUnitId = spawnUnitId,
            SpawnCount = 1,
            SpawnInterval = 3.0f
        };
    }
}
```

---

## 5. 예상 산출물

### 5.1 새로 생성될 파일 (약 20개)

```
ReferenceModels.Tests/
├── ReferenceModels.Tests.csproj          [NEW]
├── Serialization/
│   ├── UnitReferenceSerializationTests.cs      [NEW]
│   ├── SkillReferenceSerializationTests.cs     [NEW]
│   ├── BuildingReferenceSerializationTests.cs  [NEW]
│   ├── SpellReferenceSerializationTests.cs     [NEW]
│   └── TowerReferenceSerializationTests.cs     [NEW]
├── Validation/
│   ├── UnitReferenceValidatorTests.cs          [NEW]
│   ├── SkillReferenceValidatorTests.cs         [NEW]
│   ├── BuildingReferenceValidatorTests.cs      [NEW]
│   ├── SpellReferenceValidatorTests.cs         [NEW]
│   └── TowerReferenceValidatorTests.cs         [NEW]
├── Infrastructure/
│   ├── ReferenceHandlersTests.cs               [NEW]
│   └── ReferenceManagerTests.cs                [NEW]
├── Integration/
│   └── JsonLoadingTests.cs                     [NEW]
└── TestHelpers/
    ├── JsonTestHelper.cs                       [NEW]
    └── ReferenceTestFactory.cs                 [NEW]
```

### 5.2 수정될 파일 (1개)

```
UnitSimulator.Core.Tests/
└── References/
    └── ReferenceManagerTests.cs                [MODIFIED]
```

---

## 6. 성공 기준

### 6.1 정량적 지표

- [ ] 모든 테스트가 통과 (0개 실패)
- [ ] 최소 60개 이상의 테스트 작성
- [ ] 모든 Reference 클래스의 주요 경로 커버
- [ ] 모든 Validator의 주요 검증 로직 커버

### 6.2 정성적 지표

- [ ] 실제 JSON 파일(data/references/)이 정상적으로 로드됨
- [ ] 모든 참조 무결성 검증 통과
- [ ] 모든 데이터가 Validator 검증 통과
- [ ] 테스트 코드가 읽기 쉽고 유지보수 가능
- [ ] 테스트 실패 시 명확한 오류 메시지 제공

---

## 7. 우선순위 및 의존성

### 의존성 그래프

```
Phase 1 (프로젝트 설정)
    ↓
Phase 2 (Serialization)────┐
    ↓                       ↓
Phase 3 (Validation)        Phase 4 (Infrastructure)
    ↓                       ↓
    └──── Phase 5 (Integration) ────┘
                ↓
        Phase 6 (Core.Tests 업데이트)
```

### 권장 순서

1. **Phase 1** - 테스트 인프라 구축
2. **Phase 2** - Serialization 테스트 (가장 기본)
3. **Phase 3** - Validation 테스트 (핵심 로직)
4. **Phase 4** - Infrastructure 테스트 (통합 준비)
5. **Phase 5** - Integration 테스트 (실제 데이터 검증)
6. **Phase 6** - Core.Tests 업데이트 (선택적)

---

## 8. 체크리스트 요약

### Phase 1: 테스트 프로젝트 설정
- [ ] 테스트 프로젝트 생성
- [ ] 디렉토리 구조 생성
- [ ] 헬퍼 클래스 작성
- [ ] 빌드 확인

### Phase 2: Serialization 테스트 (5개 파일)
- [ ] UnitReferenceSerializationTests.cs
- [ ] SkillReferenceSerializationTests.cs
- [ ] BuildingReferenceSerializationTests.cs
- [ ] SpellReferenceSerializationTests.cs
- [ ] TowerReferenceSerializationTests.cs

### Phase 3: Validation 테스트 (5개 파일)
- [ ] UnitReferenceValidatorTests.cs
- [ ] SkillReferenceValidatorTests.cs
- [ ] BuildingReferenceValidatorTests.cs
- [ ] SpellReferenceValidatorTests.cs
- [ ] TowerReferenceValidatorTests.cs

### Phase 4: Infrastructure 테스트 (2개 파일)
- [ ] ReferenceHandlersTests.cs
- [ ] ReferenceManagerTests.cs

### Phase 5: Integration 테스트 (1개 파일)
- [ ] JsonLoadingTests.cs

### Phase 6: Core.Tests 업데이트 (1개 파일)
- [ ] ReferenceManagerTests.cs 업데이트

---

## 9. 참고 사항

### 9.1 테스트 실행 명령어

```bash
# 전체 솔루션 테스트
dotnet test

# 특정 프로젝트만 테스트
dotnet test ReferenceModels.Tests

# 상세 출력
dotnet test --verbosity detailed

# 커버리지 수집
dotnet test --collect:"XPlat Code Coverage"
```

### 9.2 알려진 이슈 및 제약사항

1. **ReferenceModels vs Core 분리**
   - ReferenceModels는 순수 데이터 레이어 (CreateUnit, ToAbilityData 메서드 없음)
   - Core.Tests의 일부 테스트는 Core의 확장 메서드를 사용할 수 있음

2. **JSON 파일 경로**
   - 통합 테스트는 실제 `data/references/` 경로 의존
   - CI/CD 환경에서 경로 문제 발생 가능 → 상대 경로 주의

3. **Enum 직렬화**
   - JsonStringEnumConverter 사용 → 문자열로 직렬화
   - 대소문자 구분 안 함 (PropertyNameCaseInsensitive: true)

---

## 10. 향후 계획

테스트 완료 후 고려할 사항:

1. **코드 커버리지 목표**: 80% 이상
2. **성능 테스트**: 대량 데이터 로드 시 성능 측정
3. **벤치마크**: BenchmarkDotNet 사용
4. **Mutation 테스트**: Stryker.NET 도입 검토
5. **CI/CD 통합**: GitHub Actions에 테스트 자동 실행 추가
