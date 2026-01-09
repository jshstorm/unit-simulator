# WebSocket API: ActivateTowerSkill

**문서 버전**: 1.0  
**작성일**: 2026-01-09  
**상태**: 설계 완료

---

## 개요

| 항목 | 값 |
|------|-----|
| 요청 타입 | `ActivateTowerSkillRequest` |
| 응답 타입 | `ActivateTowerSkillResponse` |
| 설명 | 타워의 특수 스킬을 발동한다 |
| 관련 기능 | specs/features/tower-skill-system.md |

---

## C# DTO 정의

### 요청 (ActivateTowerSkillRequest)

```csharp
using System.Text.Json.Serialization;

namespace UnitSimulator.Server.Messages;

/// <summary>
/// 타워 스킬 발동 요청
/// </summary>
public record ActivateTowerSkillRequest
{
    /// <summary>
    /// 스킬을 발동할 타워 ID
    /// </summary>
    [JsonPropertyName("towerId")]
    public required string TowerId { get; init; }

    /// <summary>
    /// 발동할 스킬 ID
    /// </summary>
    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }

    /// <summary>
    /// 대상 위치 (범위 스킬용, 선택)
    /// </summary>
    [JsonPropertyName("targetPosition")]
    public Position? TargetPosition { get; init; }

    /// <summary>
    /// 대상 유닛 ID (단일 대상 스킬용, 선택)
    /// </summary>
    [JsonPropertyName("targetUnitId")]
    public string? TargetUnitId { get; init; }
}

/// <summary>
/// 2D 위치
/// </summary>
public record Position
{
    [JsonPropertyName("x")]
    public required float X { get; init; }

    [JsonPropertyName("y")]
    public required float Y { get; init; }
}
```

### 응답 (ActivateTowerSkillResponse)

```csharp
using System.Text.Json.Serialization;

namespace UnitSimulator.Server.Messages;

/// <summary>
/// 타워 스킬 발동 응답
/// </summary>
public record ActivateTowerSkillResponse
{
    /// <summary>
    /// 발동 성공 여부
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// 쿨다운 시간 (밀리초, 성공 시)
    /// </summary>
    [JsonPropertyName("cooldown")]
    public int? Cooldown { get; init; }

    /// <summary>
    /// 적용된 효과 목록 (성공 시)
    /// </summary>
    [JsonPropertyName("effects")]
    public List<SkillEffectResult>? Effects { get; init; }

    /// <summary>
    /// 에러 코드 (실패 시)
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// 에러 메시지 (실패 시)
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// 스킬 효과 결과
/// </summary>
public record SkillEffectResult
{
    /// <summary>
    /// 효과 타입
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// 대상 ID (유닛 또는 타워)
    /// </summary>
    [JsonPropertyName("targetId")]
    public required string TargetId { get; init; }

    /// <summary>
    /// 효과 값 (데미지량, 버프 수치 등)
    /// </summary>
    [JsonPropertyName("value")]
    public int? Value { get; init; }

    /// <summary>
    /// 지속 시간 (밀리초, 버프/디버프)
    /// </summary>
    [JsonPropertyName("duration")]
    public int? Duration { get; init; }
}
```

---

## JSON 예시

### 요청 - 단일 대상 스킬

```json
{
  "type": "ActivateTowerSkillRequest",
  "sessionId": "session-abc-123",
  "payload": {
    "towerId": "tower-1",
    "skillId": "skill-fireball",
    "targetUnitId": "unit-5"
  }
}
```

### 요청 - 범위 스킬

```json
{
  "type": "ActivateTowerSkillRequest",
  "sessionId": "session-abc-123",
  "payload": {
    "towerId": "tower-2",
    "skillId": "skill-meteor",
    "targetPosition": {
      "x": 150.5,
      "y": 200.0
    }
  }
}
```

### 응답 - 성공

```json
{
  "type": "ActivateTowerSkillResponse",
  "success": true,
  "cooldown": 5000,
  "effects": [
    {
      "type": "Damage",
      "targetId": "unit-5",
      "value": 150
    },
    {
      "type": "Damage",
      "targetId": "unit-6",
      "value": 100
    }
  ]
}
```

### 응답 - 실패 (쿨다운)

```json
{
  "type": "ActivateTowerSkillResponse",
  "success": false,
  "errorCode": "SKILL_ON_COOLDOWN",
  "error": "Skill is on cooldown. Remaining: 2500ms"
}
```

### 응답 - 실패 (타워 없음)

```json
{
  "type": "ActivateTowerSkillResponse",
  "success": false,
  "errorCode": "TOWER_NOT_FOUND",
  "error": "Tower 'tower-999' not found"
}
```

---

## 검증 규칙

| 필드 | 규칙 | 에러 코드 |
|------|------|-----------|
| `towerId` | 비어있지 않음 | `INVALID_TOWER_ID` |
| `towerId` | 세션에 존재하는 타워 | `TOWER_NOT_FOUND` |
| `skillId` | 비어있지 않음 | `INVALID_SKILL_ID` |
| `skillId` | 해당 타워가 보유한 스킬 | `SKILL_NOT_FOUND` |
| `skillId` | 쿨다운 상태 아님 | `SKILL_ON_COOLDOWN` |
| `targetPosition` | 범위 스킬인 경우 필수 | `TARGET_REQUIRED` |
| `targetPosition` | 맵 범위 내 | `INVALID_TARGET_POSITION` |
| `targetUnitId` | 단일 대상 스킬인 경우 필수 | `TARGET_REQUIRED` |
| `targetUnitId` | 존재하는 유닛 | `TARGET_NOT_FOUND` |

---

## 에러 케이스

| 에러 코드 | HTTP 유사 | 메시지 | 원인 |
|-----------|-----------|--------|------|
| `INVALID_TOWER_ID` | 400 | Tower ID is required | towerId 누락/빈값 |
| `TOWER_NOT_FOUND` | 404 | Tower '{id}' not found | 존재하지 않는 타워 |
| `INVALID_SKILL_ID` | 400 | Skill ID is required | skillId 누락/빈값 |
| `SKILL_NOT_FOUND` | 404 | Skill '{id}' not found on tower | 타워에 없는 스킬 |
| `SKILL_ON_COOLDOWN` | 409 | Skill is on cooldown | 쿨다운 중 |
| `TARGET_REQUIRED` | 400 | Target is required for this skill | 대상 정보 누락 |
| `INVALID_TARGET_POSITION` | 400 | Target position is out of bounds | 범위 밖 좌표 |
| `TARGET_NOT_FOUND` | 404 | Target unit '{id}' not found | 존재하지 않는 대상 |
| `INTERNAL_ERROR` | 500 | Internal server error | 서버 내부 오류 |

---

## 핸들러 구현 위치

| 파일 | 설명 |
|------|------|
| `UnitSimulator.Server/Messages/TowerSkillMessages.cs` | DTO 정의 |
| `UnitSimulator.Server/Handlers/TowerSkillHandler.cs` | 핸들러 구현 |
| `UnitSimulator.Core/Systems/TowerSkillSystem.cs` | 비즈니스 로직 |

---

## 관련 이벤트 (브로드캐스트)

스킬 발동 성공 시 세션 내 모든 클라이언트에게 브로드캐스트:

### TowerSkillActivatedEvent

```csharp
public record TowerSkillActivatedEvent
{
    [JsonPropertyName("type")]
    public string Type => "TowerSkillActivatedEvent";

    [JsonPropertyName("towerId")]
    public required string TowerId { get; init; }

    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }

    [JsonPropertyName("effects")]
    public required List<SkillEffectResult> Effects { get; init; }

    [JsonPropertyName("timestamp")]
    public required long Timestamp { get; init; }
}
```

```json
{
  "type": "TowerSkillActivatedEvent",
  "towerId": "tower-1",
  "skillId": "skill-fireball",
  "effects": [
    { "type": "Damage", "targetId": "unit-5", "value": 150 }
  ],
  "timestamp": 1736412345678
}
```

---

## 테스트 시나리오

### 정상 케이스
1. 유효한 타워와 스킬로 발동 → 성공 응답 + 효과 목록
2. 범위 스킬을 유효한 좌표로 발동 → 범위 내 유닛 피해
3. 버프 스킬 발동 → 대상에 버프 적용

### 에러 케이스
1. 존재하지 않는 타워 → `TOWER_NOT_FOUND`
2. 타워에 없는 스킬 → `SKILL_NOT_FOUND`
3. 쿨다운 중 재발동 → `SKILL_ON_COOLDOWN`
4. 범위 스킬에 좌표 없음 → `TARGET_REQUIRED`
5. 맵 범위 밖 좌표 → `INVALID_TARGET_POSITION`

---

## 다음 단계

1. **구현**: `scaffold-csharp` 스킬로 클래스 스캐폴딩
2. **테스트**: `generate-tests` 스킬로 xUnit 테스트 생성
3. **리뷰**: `run-review` 스킬로 코드 리뷰
