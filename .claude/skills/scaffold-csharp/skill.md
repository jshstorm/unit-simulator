# Skill: scaffold-csharp

API 명세에서 C# 클래스 스캐폴딩을 자동 생성한다.

---

## 메타데이터

```yaml
name: scaffold-csharp
version: 1.0.0
agent: implementer
trigger: API 명세 완료 후 자동 호출
```

---

## 입력

| 파라미터 | 필수 | 설명 |
|----------|------|------|
| api_spec_path | O | new_api_endpoint.md 경로 |
| target_project | X | 대상 프로젝트 (Core/Server/Models) |

---

## 출력

| 파일 | 설명 |
|------|------|
| `UnitSimulator.Core/Systems/{Feature}System.cs` | 핵심 로직 클래스 |
| `UnitSimulator.Server/Handlers/{Feature}Handler.cs` | WebSocket 핸들러 |
| `UnitSimulator.Server/Messages/{Feature}Messages.cs` | DTO 정의 |
| `ReferenceModels/Models/{Feature}Reference.cs` | 데이터 모델 (필요 시) |

---

## 실행 흐름

```
1. API 명세 로드
   └─ new_api_endpoint.md에서 DTO 정의 추출

2. 프로젝트 구조 분석
   ├─ 기존 네임스페이스 확인
   ├─ 기존 핸들러 패턴 확인
   └─ 의존성 파악

3. DTO 파일 생성
   └─ Messages/{Feature}Messages.cs

4. 핸들러 파일 생성
   └─ Handlers/{Feature}Handler.cs

5. 시스템 클래스 생성 (Core)
   └─ Systems/{Feature}System.cs

6. 레퍼런스 모델 생성 (필요 시)
   └─ Models/{Feature}Reference.cs

7. DI 등록 코드 제안
   └─ Program.cs 또는 ServiceExtensions.cs 수정 사항
```

---

## 프롬프트

```
## 역할
당신은 C#/.NET WebSocket 서버 개발 전문가입니다.

## 입력
API 명세: {{new_api_endpoint.md 내용}}
기존 코드 구조: {{프로젝트 분석 결과}}

## 작업
1. API 명세에서 DTO 정의를 추출하세요
2. 기존 코드 패턴을 분석하세요
3. 일관된 스타일로 새 클래스를 스캐폴딩하세요
4. DI 등록 코드를 제안하세요

## 코딩 규칙
- 클래스/메서드/속성: PascalCase
- 로컬 변수/매개변수: camelCase
- private 필드: _camelCase
- 비동기 메서드: Async 접미사
- 인터페이스: I 접두사

## 출력
각 파일의 전체 C# 코드
```

---

## C# 템플릿

### Messages/{Feature}Messages.cs

```csharp
using System.Text.Json.Serialization;

namespace UnitSimulator.Server.Messages;

/// <summary>
/// {기능} 요청 메시지
/// </summary>
public record {Action}{Entity}Request
{
    [JsonPropertyName("fieldName")]
    public required string FieldName { get; init; }
}

/// <summary>
/// {기능} 응답 메시지
/// </summary>
public record {Action}{Entity}Response
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
```

### Handlers/{Feature}Handler.cs

```csharp
using Microsoft.Extensions.Logging;
using UnitSimulator.Core.Systems;
using UnitSimulator.Server.Messages;

namespace UnitSimulator.Server.Handlers;

/// <summary>
/// {기능} WebSocket 핸들러
/// </summary>
public class {Feature}Handler : IMessageHandler<{Action}{Entity}Request>
{
    private readonly {Feature}System _system;
    private readonly ILogger<{Feature}Handler> _logger;

    public {Feature}Handler(
        {Feature}System system,
        ILogger<{Feature}Handler> logger)
    {
        _system = system;
        _logger = logger;
    }

    public async Task<{Action}{Entity}Response> HandleAsync(
        {Action}{Entity}Request request,
        SimulationSession session,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "{Handler} processing request for session {SessionId}",
                nameof({Feature}Handler),
                session.Id);

            var result = await _system.{Action}Async(request, cancellationToken);

            return new {Action}{Entity}Response
            {
                Success = true,
                // Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Request}", typeof({Action}{Entity}Request).Name);

            return new {Action}{Entity}Response
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}
```

### Systems/{Feature}System.cs

```csharp
using Microsoft.Extensions.Logging;

namespace UnitSimulator.Core.Systems;

/// <summary>
/// {기능} 시스템
/// </summary>
public class {Feature}System
{
    private readonly ILogger<{Feature}System> _logger;

    public {Feature}System(ILogger<{Feature}System> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// {동작} 수행
    /// </summary>
    public async Task<{Result}> {Action}Async(
        {Action}{Entity}Request request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing {Action} for {Entity}", "{Action}", "{Entity}");

        // TODO: 구현

        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
```

---

## 예시

### 입력

```
api_spec_path: specs/apis/new_api_endpoint.md
```

**new_api_endpoint.md 요약:**
- 요청: `ActivateTowerSkillRequest`
- 응답: `ActivateTowerSkillResponse`
- 기능: 타워 스킬 발동

### 출력

**UnitSimulator.Server/Messages/TowerSkillMessages.cs**
```csharp
using System.Text.Json.Serialization;

namespace UnitSimulator.Server.Messages;

public record ActivateTowerSkillRequest
{
    [JsonPropertyName("towerId")]
    public required string TowerId { get; init; }

    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }

    [JsonPropertyName("targetPosition")]
    public Position? TargetPosition { get; init; }
}

public record ActivateTowerSkillResponse
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("cooldown")]
    public int? Cooldown { get; init; }

    [JsonPropertyName("effects")]
    public List<SkillEffectResult>? Effects { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
```

**UnitSimulator.Server/Handlers/TowerSkillHandler.cs**
```csharp
using Microsoft.Extensions.Logging;
using UnitSimulator.Core.Systems;
using UnitSimulator.Server.Messages;

namespace UnitSimulator.Server.Handlers;

public class TowerSkillHandler : IMessageHandler<ActivateTowerSkillRequest>
{
    private readonly TowerSkillSystem _system;
    private readonly ILogger<TowerSkillHandler> _logger;

    public TowerSkillHandler(
        TowerSkillSystem system,
        ILogger<TowerSkillHandler> logger)
    {
        _system = system;
        _logger = logger;
    }

    public async Task<ActivateTowerSkillResponse> HandleAsync(
        ActivateTowerSkillRequest request,
        SimulationSession session,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _system.ActivateSkillAsync(
                request.TowerId,
                request.SkillId,
                request.TargetPosition,
                cancellationToken);

            return new ActivateTowerSkillResponse
            {
                Success = true,
                Cooldown = result.Cooldown,
                Effects = result.Effects
            };
        }
        catch (TowerNotFoundException ex)
        {
            return new ActivateTowerSkillResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
        catch (SkillOnCooldownException ex)
        {
            return new ActivateTowerSkillResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}
```

**UnitSimulator.Core/Systems/TowerSkillSystem.cs**
```csharp
using Microsoft.Extensions.Logging;

namespace UnitSimulator.Core.Systems;

public class TowerSkillSystem
{
    private readonly ILogger<TowerSkillSystem> _logger;
    private readonly SimulatorCore _simulator;

    public TowerSkillSystem(
        ILogger<TowerSkillSystem> logger,
        SimulatorCore simulator)
    {
        _logger = logger;
        _simulator = simulator;
    }

    public async Task<SkillActivationResult> ActivateSkillAsync(
        string towerId,
        string skillId,
        Position? targetPosition,
        CancellationToken cancellationToken = default)
    {
        var tower = _simulator.GetTower(towerId)
            ?? throw new TowerNotFoundException(towerId);

        var skill = tower.GetSkill(skillId)
            ?? throw new SkillNotFoundException(skillId);

        if (skill.IsOnCooldown)
        {
            throw new SkillOnCooldownException(skillId, skill.RemainingCooldown);
        }

        _logger.LogInformation(
            "Tower {TowerId} activating skill {SkillId}",
            towerId, skillId);

        var effects = await skill.ExecuteAsync(targetPosition, cancellationToken);

        return new SkillActivationResult
        {
            Success = true,
            Cooldown = skill.CooldownMs,
            Effects = effects
        };
    }
}
```

---

## DI 등록 제안

**Program.cs 또는 ServiceExtensions.cs에 추가:**

```csharp
// Systems
services.AddScoped<TowerSkillSystem>();

// Handlers
services.AddScoped<IMessageHandler<ActivateTowerSkillRequest>, TowerSkillHandler>();
```

---

## 에러 처리

| 에러 | 처리 |
|------|------|
| API 명세 없음 | 에러 메시지 반환, generate-api-spec 실행 제안 |
| 기존 파일 충돌 | 덮어쓰기 전 확인 요청 |
| 네임스페이스 불일치 | 기존 패턴에 맞게 자동 조정 |

---

## 연결

- **이전 스킬**: `generate-api-spec`
- **다음 스킬**: `generate-tests`
