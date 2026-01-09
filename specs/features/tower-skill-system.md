# 기능: 타워 스킬 시스템

**문서 버전**: 1.0  
**작성일**: 2026-01-09  
**상태**: 설계 완료

---

## 개요

타워가 자동 공격 외에 특수 스킬을 발동할 수 있는 시스템. 각 타워 타입은 고유한 스킬을 보유하며, 스킬은 쿨다운 후 재사용 가능.

---

## 요구사항

### 핵심 요구사항
1. 타워는 1개 이상의 스킬을 보유할 수 있다
2. 스킬은 WebSocket 메시지로 발동된다
3. 스킬 발동 시 쿨다운이 시작된다
4. 쿨다운 중에는 같은 스킬을 발동할 수 없다
5. 스킬은 다양한 효과를 가질 수 있다 (데미지, 범위 데미지, 버프/디버프)

### 스킬 타입
| 스킬 타입 | 설명 | 예시 |
|-----------|------|------|
| TargetedDamage | 단일 대상 데미지 | 파이어볼 |
| AreaOfEffect | 범위 데미지 | 메테오 |
| Buff | 아군 강화 | 공격력 증가 |
| Debuff | 적군 약화 | 이동속도 감소 |
| Utility | 특수 효과 | 시간 정지 |

---

## 완료 조건

- [x] WebSocket 프로토콜 정의됨 (ActivateTowerSkillRequest/Response)
- [ ] C# 클래스 구현됨 (TowerSkillSystem)
- [ ] 스킬 효과 시스템 구현됨 (SkillEffect)
- [ ] xUnit 테스트 통과함 (최소 10개 케이스)
- [ ] ReferenceModels에 스킬 데이터 추가됨
- [ ] 기존 전투 시스템과 통합됨

---

## 영향받는 프로젝트

### UnitSimulator.Core
**변경 사항**:
- `Systems/TowerSkillSystem.cs` 추가
- `Systems/SkillEffectProcessor.cs` 추가
- `Tower.cs`에 스킬 관련 메서드/속성 추가
- `Interfaces/ISkillEffect.cs` 추가

**의존성**:
- SimulatorCore (타워/유닛 접근)
- CombatSystem (데미지 계산)

### UnitSimulator.Server
**변경 사항**:
- `Handlers/TowerSkillHandler.cs` 추가
- `Messages/TowerSkillMessages.cs` 추가
- `WebSocketServer.cs` 핸들러 등록

**의존성**:
- TowerSkillSystem (Core)
- SimulationSession (세션 관리)

### ReferenceModels
**변경 사항**:
- `Models/TowerSkillReference.cs` 추가
- `Models/SkillEffectData.cs` 추가
- 스킬 데이터 JSON 파일

**의존성**:
- 기존 TowerReference와 연결

### sim-studio (Phase 2 - 별도 작업)
**예정 변경**:
- 스킬 발동 버튼 UI
- 쿨다운 표시
- 스킬 효과 시각화

---

## C# 클래스 설계

### Core 계층

#### TowerSkillSystem.cs
```csharp
namespace UnitSimulator.Core.Systems;

/// <summary>
/// 타워 스킬 발동 및 쿨다운 관리
/// </summary>
public class TowerSkillSystem
{
    private readonly SimulatorCore _simulator;
    private readonly SkillEffectProcessor _effectProcessor;
    private readonly ILogger<TowerSkillSystem> _logger;

    /// <summary>
    /// 스킬 발동
    /// </summary>
    public async Task<SkillActivationResult> ActivateSkillAsync(
        string towerId,
        string skillId,
        Position? targetPosition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 스킬 쿨다운 확인
    /// </summary>
    public bool IsSkillOnCooldown(string towerId, string skillId);

    /// <summary>
    /// 스킬 쿨다운 남은 시간
    /// </summary>
    public int GetRemainingCooldown(string towerId, string skillId);
}
```

#### SkillEffectProcessor.cs
```csharp
namespace UnitSimulator.Core.Systems;

/// <summary>
/// 스킬 효과 처리
/// </summary>
public class SkillEffectProcessor
{
    /// <summary>
    /// 효과 적용
    /// </summary>
    public async Task<List<SkillEffectResult>> ApplyEffectsAsync(
        TowerSkill skill,
        Tower source,
        Position? targetPosition,
        CancellationToken cancellationToken = default);
}
```

#### ISkillEffect.cs
```csharp
namespace UnitSimulator.Core.Interfaces;

/// <summary>
/// 스킬 효과 인터페이스
/// </summary>
public interface ISkillEffect
{
    SkillEffectType Type { get; }
    Task<SkillEffectResult> ExecuteAsync(SkillEffectContext context);
}
```

### Server 계층

#### TowerSkillHandler.cs
```csharp
namespace UnitSimulator.Server.Handlers;

/// <summary>
/// 타워 스킬 WebSocket 핸들러
/// </summary>
public class TowerSkillHandler : IMessageHandler<ActivateTowerSkillRequest>
{
    public async Task<ActivateTowerSkillResponse> HandleAsync(
        ActivateTowerSkillRequest request,
        SimulationSession session,
        CancellationToken cancellationToken = default);
}
```

### Models 계층

#### TowerSkillReference.cs
```csharp
namespace ReferenceModels.Models;

/// <summary>
/// 타워 스킬 참조 데이터
/// </summary>
public record TowerSkillReference
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string TowerId { get; init; }
    public required SkillEffectType EffectType { get; init; }
    public required int CooldownMs { get; init; }
    public int? Range { get; init; }
    public int? Damage { get; init; }
    public int? Duration { get; init; }
}
```

---

## 테스트 계획

### 단위 테스트 (UnitSimulator.Core.Tests)

#### TowerSkillSystemTests.cs
| 테스트 케이스 | 설명 |
|--------------|------|
| `ActivateSkill_ValidInput_ReturnsSuccess` | 정상 발동 |
| `ActivateSkill_TowerNotFound_ThrowsException` | 타워 없음 |
| `ActivateSkill_SkillNotFound_ThrowsException` | 스킬 없음 |
| `ActivateSkill_OnCooldown_ThrowsException` | 쿨다운 중 |
| `ActivateSkill_InvalidTarget_ThrowsException` | 잘못된 대상 |
| `IsSkillOnCooldown_AfterActivation_ReturnsTrue` | 쿨다운 상태 확인 |
| `GetRemainingCooldown_DecreasesOverTime` | 쿨다운 감소 확인 |

#### SkillEffectProcessorTests.cs
| 테스트 케이스 | 설명 |
|--------------|------|
| `ApplyEffects_DamageSkill_DealsDamage` | 데미지 적용 |
| `ApplyEffects_AreaSkill_AffectsMultipleTargets` | 범위 효과 |
| `ApplyEffects_BuffSkill_AppliesBuff` | 버프 적용 |

### 통합 테스트 (UnitSimulator.Server.Tests)

#### TowerSkillHandlerTests.cs
| 테스트 케이스 | 설명 |
|--------------|------|
| `Handle_ValidRequest_ReturnsSuccessResponse` | 정상 응답 |
| `Handle_InvalidTower_ReturnsErrorResponse` | 에러 응답 |
| `Handle_Cooldown_ReturnsErrorResponse` | 쿨다운 에러 |

---

## 시퀀스 다이어그램

```
Client           Server              TowerSkillHandler      TowerSkillSystem       Tower
   |                |                        |                      |                 |
   |--ActivateTowerSkillRequest-->           |                      |                 |
   |                |----HandleAsync-------->|                      |                 |
   |                |                        |--ActivateSkillAsync->|                 |
   |                |                        |                      |--GetSkill------>|
   |                |                        |                      |<--TowerSkill----|
   |                |                        |                      |--IsOnCooldown-->|
   |                |                        |                      |<----false-------|
   |                |                        |                      |--ApplyEffects-->|
   |                |                        |                      |--StartCooldown->|
   |                |                        |<--SkillActivationResult               |
   |                |<--ActivateTowerSkillResponse                  |                 |
   |<--------------------|                   |                      |                 |
```

---

## 다음 단계

1. **API 설계**: `/new-api` 명령으로 WebSocket 프로토콜 상세화 → ✅ 완료
2. **Core 구현**: TowerSkillSystem, SkillEffectProcessor 구현
3. **Server 구현**: TowerSkillHandler 구현
4. **테스트 작성**: `/run-tests` 명령으로 xUnit 테스트 생성
5. **리뷰**: `/pre-pr` 명령으로 코드 리뷰 및 PR 준비
