# 테스트 계획: 타워 스킬 시스템

**문서 버전**: 1.0  
**작성일**: 2026-01-09  
**상태**: 대기 (구현 후 실행)

---

## 개요

| 항목 | 값 |
|------|-----|
| 대상 기능 | 타워 스킬 시스템 |
| 테스트 프레임워크 | xUnit |
| 관련 문서 | specs/features/tower-skill-system.md |
| API 스펙 | specs/apis/tower-skill-api.md |

---

## 테스트 파일 구조

```
UnitSimulator.Core.Tests/
└── Systems/
    ├── TowerSkillSystemTests.cs
    └── SkillEffectProcessorTests.cs

UnitSimulator.Server.Tests/
└── Handlers/
    └── TowerSkillHandlerTests.cs
```

---

## 단위 테스트: TowerSkillSystemTests.cs

### 테스트 클래스 구조

```csharp
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using UnitSimulator.Core.Systems;

namespace UnitSimulator.Core.Tests.Systems;

public class TowerSkillSystemTests
{
    private readonly Mock<SimulatorCore> _simulatorMock;
    private readonly Mock<SkillEffectProcessor> _effectProcessorMock;
    private readonly Mock<ILogger<TowerSkillSystem>> _loggerMock;
    private readonly TowerSkillSystem _sut;

    public TowerSkillSystemTests()
    {
        _simulatorMock = new Mock<SimulatorCore>();
        _effectProcessorMock = new Mock<SkillEffectProcessor>();
        _loggerMock = new Mock<ILogger<TowerSkillSystem>>();
        _sut = new TowerSkillSystem(
            _simulatorMock.Object,
            _effectProcessorMock.Object,
            _loggerMock.Object);
    }
}
```

### 테스트 케이스

| # | 메서드 | 시나리오 | 예상 결과 |
|---|--------|----------|-----------|
| 1 | `ActivateSkill_ValidInput_ReturnsSuccess` | 유효한 타워/스킬 | Success = true |
| 2 | `ActivateSkill_TowerNotFound_ThrowsTowerNotFoundException` | 존재하지 않는 타워 | TowerNotFoundException |
| 3 | `ActivateSkill_SkillNotFound_ThrowsSkillNotFoundException` | 타워에 없는 스킬 | SkillNotFoundException |
| 4 | `ActivateSkill_SkillOnCooldown_ThrowsSkillOnCooldownException` | 쿨다운 중 | SkillOnCooldownException |
| 5 | `ActivateSkill_TargetedSkillWithoutTarget_ThrowsInvalidTargetException` | 대상 누락 | InvalidTargetException |
| 6 | `ActivateSkill_AreaSkillWithValidPosition_AffectsMultipleUnits` | 범위 스킬 | 다중 효과 |
| 7 | `ActivateSkill_StartsCooldown` | 발동 후 쿨다운 | IsOnCooldown = true |
| 8 | `IsSkillOnCooldown_BeforeActivation_ReturnsFalse` | 첫 발동 전 | false |
| 9 | `IsSkillOnCooldown_AfterActivation_ReturnsTrue` | 발동 직후 | true |
| 10 | `GetRemainingCooldown_DecreasesOverTime` | 시간 경과 | 감소 확인 |

### 테스트 구현 예시

```csharp
[Fact]
public async Task ActivateSkill_ValidInput_ReturnsSuccess()
{
    // Arrange
    var towerId = "tower-1";
    var skillId = "skill-fireball";
    var tower = CreateTestTower(towerId, skillId);
    
    _simulatorMock
        .Setup(s => s.GetTower(towerId))
        .Returns(tower);
    
    _effectProcessorMock
        .Setup(p => p.ApplyEffectsAsync(
            It.IsAny<TowerSkill>(),
            It.IsAny<Tower>(),
            It.IsAny<Position?>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<SkillEffectResult>
        {
            new() { Type = "Damage", TargetId = "unit-1", Value = 100 }
        });

    // Act
    var result = await _sut.ActivateSkillAsync(
        towerId, skillId, null, CancellationToken.None);

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.Effects);
    Assert.Single(result.Effects);
    Assert.Equal(100, result.Effects[0].Value);
}

[Fact]
public async Task ActivateSkill_TowerNotFound_ThrowsTowerNotFoundException()
{
    // Arrange
    var towerId = "nonexistent-tower";
    var skillId = "skill-fireball";
    
    _simulatorMock
        .Setup(s => s.GetTower(towerId))
        .Returns((Tower?)null);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<TowerNotFoundException>(
        () => _sut.ActivateSkillAsync(towerId, skillId, null));
    
    Assert.Equal(towerId, exception.TowerId);
}

[Fact]
public async Task ActivateSkill_SkillOnCooldown_ThrowsSkillOnCooldownException()
{
    // Arrange
    var towerId = "tower-1";
    var skillId = "skill-fireball";
    var tower = CreateTestTower(towerId, skillId);
    tower.Skills[skillId].SetOnCooldown(5000);
    
    _simulatorMock
        .Setup(s => s.GetTower(towerId))
        .Returns(tower);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<SkillOnCooldownException>(
        () => _sut.ActivateSkillAsync(towerId, skillId, null));
    
    Assert.Equal(skillId, exception.SkillId);
    Assert.True(exception.RemainingMs > 0);
}

[Theory]
[InlineData(0)]
[InlineData(1000)]
[InlineData(2500)]
public async Task GetRemainingCooldown_AfterTimeElapsed_ReturnsCorrectValue(
    int elapsedMs)
{
    // Arrange
    var towerId = "tower-1";
    var skillId = "skill-fireball";
    var cooldownMs = 5000;
    var tower = CreateTestTower(towerId, skillId, cooldownMs);
    
    _simulatorMock
        .Setup(s => s.GetTower(towerId))
        .Returns(tower);
    
    // 스킬 발동
    await _sut.ActivateSkillAsync(towerId, skillId, null);
    
    // 시간 경과 시뮬레이션
    tower.Skills[skillId].AdvanceTime(elapsedMs);

    // Act
    var remaining = _sut.GetRemainingCooldown(towerId, skillId);

    // Assert
    Assert.Equal(cooldownMs - elapsedMs, remaining);
}
```

---

## 단위 테스트: SkillEffectProcessorTests.cs

### 테스트 케이스

| # | 메서드 | 시나리오 | 예상 결과 |
|---|--------|----------|-----------|
| 1 | `ApplyEffects_DamageSkill_DealsDamage` | 데미지 스킬 | 대상에 데미지 |
| 2 | `ApplyEffects_AreaSkill_AffectsUnitsInRange` | 범위 스킬 | 범위 내 모든 유닛 |
| 3 | `ApplyEffects_BuffSkill_AppliesBuff` | 버프 스킬 | 대상에 버프 |
| 4 | `ApplyEffects_DebuffSkill_AppliesDebuff` | 디버프 스킬 | 대상에 디버프 |
| 5 | `ApplyEffects_NoTargetsInRange_ReturnsEmpty` | 범위 내 대상 없음 | 빈 리스트 |

### 테스트 구현 예시

```csharp
[Fact]
public async Task ApplyEffects_AreaSkill_AffectsUnitsInRange()
{
    // Arrange
    var skill = CreateAreaSkill(range: 100, damage: 50);
    var tower = CreateTestTower();
    var position = new Position { X = 100, Y = 100 };
    
    var unitsInRange = new List<Unit>
    {
        CreateTestUnit("unit-1", new Position { X = 120, Y = 100 }), // 거리 20
        CreateTestUnit("unit-2", new Position { X = 150, Y = 100 }), // 거리 50
        CreateTestUnit("unit-3", new Position { X = 250, Y = 100 }), // 거리 150 (범위 밖)
    };
    
    _unitFinderMock
        .Setup(f => f.FindUnitsInRange(position, 100))
        .Returns(unitsInRange.Take(2).ToList());

    // Act
    var results = await _sut.ApplyEffectsAsync(
        skill, tower, position, CancellationToken.None);

    // Assert
    Assert.Equal(2, results.Count);
    Assert.All(results, r => Assert.Equal("Damage", r.Type));
    Assert.All(results, r => Assert.Equal(50, r.Value));
    Assert.Contains(results, r => r.TargetId == "unit-1");
    Assert.Contains(results, r => r.TargetId == "unit-2");
    Assert.DoesNotContain(results, r => r.TargetId == "unit-3");
}
```

---

## 통합 테스트: TowerSkillHandlerTests.cs

### 테스트 케이스

| # | 메서드 | 시나리오 | 예상 결과 |
|---|--------|----------|-----------|
| 1 | `Handle_ValidRequest_ReturnsSuccessResponse` | 정상 요청 | Success = true |
| 2 | `Handle_TowerNotFound_ReturnsErrorResponse` | 타워 없음 | ErrorCode = TOWER_NOT_FOUND |
| 3 | `Handle_SkillNotFound_ReturnsErrorResponse` | 스킬 없음 | ErrorCode = SKILL_NOT_FOUND |
| 4 | `Handle_Cooldown_ReturnsErrorResponse` | 쿨다운 중 | ErrorCode = SKILL_ON_COOLDOWN |
| 5 | `Handle_InvalidTarget_ReturnsErrorResponse` | 대상 오류 | ErrorCode = TARGET_REQUIRED |

### 테스트 구현 예시

```csharp
[Fact]
public async Task Handle_ValidRequest_ReturnsSuccessResponse()
{
    // Arrange
    var request = new ActivateTowerSkillRequest
    {
        TowerId = "tower-1",
        SkillId = "skill-fireball"
    };
    
    var session = CreateTestSession();
    
    _skillSystemMock
        .Setup(s => s.ActivateSkillAsync(
            request.TowerId,
            request.SkillId,
            request.TargetPosition,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new SkillActivationResult
        {
            Success = true,
            Cooldown = 5000,
            Effects = new List<SkillEffectResult>()
        });

    // Act
    var response = await _handler.HandleAsync(
        request, session, CancellationToken.None);

    // Assert
    Assert.True(response.Success);
    Assert.Equal(5000, response.Cooldown);
    Assert.Null(response.ErrorCode);
    Assert.Null(response.Error);
}

[Fact]
public async Task Handle_TowerNotFound_ReturnsErrorResponse()
{
    // Arrange
    var request = new ActivateTowerSkillRequest
    {
        TowerId = "nonexistent",
        SkillId = "skill-fireball"
    };
    
    var session = CreateTestSession();
    
    _skillSystemMock
        .Setup(s => s.ActivateSkillAsync(
            request.TowerId,
            request.SkillId,
            It.IsAny<Position?>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(new TowerNotFoundException(request.TowerId));

    // Act
    var response = await _handler.HandleAsync(
        request, session, CancellationToken.None);

    // Assert
    Assert.False(response.Success);
    Assert.Equal("TOWER_NOT_FOUND", response.ErrorCode);
    Assert.Contains("nonexistent", response.Error);
}
```

---

## 테스트 실행 명령

```bash
# 전체 테스트 실행
dotnet test

# 특정 프로젝트만
dotnet test UnitSimulator.Core.Tests/
dotnet test UnitSimulator.Server.Tests/

# 특정 클래스만
dotnet test --filter "FullyQualifiedName~TowerSkillSystemTests"

# 커버리지 포함
dotnet test --collect:"XPlat Code Coverage"
```

---

## 테스트 커버리지 목표

| 프로젝트 | 클래스 | 목표 커버리지 |
|----------|--------|---------------|
| Core | TowerSkillSystem | ≥ 90% |
| Core | SkillEffectProcessor | ≥ 85% |
| Server | TowerSkillHandler | ≥ 90% |

---

## 다음 단계

1. **구현 완료 후**: 위 테스트 코드 파일 생성
2. **테스트 실행**: `dotnet test` 통과 확인
3. **커버리지 확인**: 목표 달성 확인
4. **리뷰**: `/pre-pr` 명령으로 코드 리뷰 진행
