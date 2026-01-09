# Skill: generate-api-spec

기능 정의에서 WebSocket API 스펙과 C# DTO를 자동 생성한다.

---

## 메타데이터

```yaml
name: generate-api-spec
version: 1.0.0
agent: api-designer
trigger: /new-api
```

---

## 입력

| 파라미터 | 필수 | 설명 |
|----------|------|------|
| feature_path | O | feature.md 경로 |
| message_type | X | 메시지 타입명 (자동 추출 가능) |

---

## 출력

| 파일 | 설명 |
|------|------|
| `specs/apis/new_api_endpoint.md` | WebSocket API 스펙 문서 |

---

## 실행 흐름

```
1. feature.md 로드
   └─ 요구사항에서 필요한 동작 추출

2. 메시지 타입 식별
   ├─ 동작 추출 (Activate, Get, Update 등)
   └─ 엔티티 추출 (Tower, Unit, Skill 등)

3. 메시지 페어 설계
   ├─ {Action}{Entity}Request
   └─ {Action}{Entity}Response

4. C# DTO 정의
   ├─ record 타입 사용
   ├─ JsonPropertyName 속성
   ├─ required 키워드 (필수 필드)
   └─ nullable 타입 (선택 필드)

5. 문서 생성
   └─ new_api_endpoint.md 작성

6. 검증
   └─ C# 컴파일 가능 여부 확인
```

---

## 프롬프트

```
## 역할
당신은 C#/.NET WebSocket API 설계 전문가입니다.

## 입력
기능 정의: {{feature.md 내용}}

## 작업
1. 필요한 WebSocket 메시지 타입을 도출하세요
2. 각 메시지의 C# record DTO를 정의하세요
3. JSON 직렬화 예시를 작성하세요
4. 검증 규칙과 에러 케이스를 정의하세요

## 설계 원칙
- 메시지 명명: {Action}{Entity}Request, {Action}{Entity}Response
- C# record 타입 사용
- System.Text.Json JsonPropertyName 속성
- required 키워드로 필수 필드 표시
- nullable 참조 타입으로 선택 필드 표시

## 출력 형식
new_api_endpoint.md 템플릿에 맞게 작성
```

---

## C# DTO 템플릿

### 요청 record
```csharp
/// <summary>
/// [설명]
/// </summary>
public record {Action}{Entity}Request
{
    /// <summary>
    /// [필드 설명]
    /// </summary>
    [JsonPropertyName("fieldName")]
    public required string FieldName { get; init; }

    /// <summary>
    /// [선택 필드 설명]
    /// </summary>
    [JsonPropertyName("optionalField")]
    public int? OptionalField { get; init; }
}
```

### 응답 record
```csharp
/// <summary>
/// [설명]
/// </summary>
public record {Action}{Entity}Response
{
    /// <summary>
    /// 요청 성공 여부
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// 결과 데이터 (성공 시)
    /// </summary>
    [JsonPropertyName("data")]
    public {Entity}Data? Data { get; init; }

    /// <summary>
    /// 에러 메시지 (실패 시)
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
```

---

## 예시

### 입력
```
feature_path: specs/features/feature.md
```

**feature.md 내용:**
```markdown
# 기능: 타워 스킬 시스템
타워가 특수 스킬을 발동할 수 있다.
```

### 출력

**specs/apis/new_api_endpoint.md**
```markdown
# WebSocket API: ActivateTowerSkill

## 개요
- **요청 타입**: `ActivateTowerSkillRequest`
- **응답 타입**: `ActivateTowerSkillResponse`
- **설명**: 타워의 특수 스킬을 발동한다

## C# DTO 정의

### 요청
```csharp
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
    /// 대상 위치 (선택, 범위 스킬용)
    /// </summary>
    [JsonPropertyName("targetPosition")]
    public Position? TargetPosition { get; init; }
}
```

### 응답
```csharp
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
    /// 쿨다운 시간 (밀리초)
    /// </summary>
    [JsonPropertyName("cooldown")]
    public int? Cooldown { get; init; }

    /// <summary>
    /// 적용된 효과 목록
    /// </summary>
    [JsonPropertyName("effects")]
    public List<SkillEffectResult>? Effects { get; init; }

    /// <summary>
    /// 에러 메시지 (실패 시)
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
```

## JSON 예시

### 요청
```json
{
  "type": "ActivateTowerSkillRequest",
  "sessionId": "session-abc-123",
  "payload": {
    "towerId": "tower-1",
    "skillId": "skill-fireball",
    "targetPosition": { "x": 100, "y": 200 }
  }
}
```

### 응답 (성공)
```json
{
  "type": "ActivateTowerSkillResponse",
  "success": true,
  "cooldown": 5000,
  "effects": [
    { "type": "Damage", "target": "unit-5", "value": 100 }
  ]
}
```

### 응답 (실패)
```json
{
  "type": "ActivateTowerSkillResponse",
  "success": false,
  "error": "Skill is on cooldown"
}
```

## 검증 규칙
| 필드 | 규칙 |
|------|------|
| towerId | 비어있지 않음, 존재하는 타워 |
| skillId | 비어있지 않음, 해당 타워가 보유한 스킬 |
| targetPosition | 범위 스킬인 경우 필수 |

## 에러 케이스
| 에러 코드 | 메시지 | 조건 |
|-----------|--------|------|
| TOWER_NOT_FOUND | Tower not found | towerId 불일치 |
| SKILL_NOT_FOUND | Skill not found | skillId 불일치 |
| SKILL_ON_COOLDOWN | Skill is on cooldown | 쿨다운 중 |
| INVALID_TARGET | Invalid target position | 범위 밖 |

## 핸들러 위치
- 파일: `UnitSimulator.Server/Handlers/TowerSkillHandler.cs`
- 메서드: `HandleActivateTowerSkillAsync`
```

---

## 에러 처리

| 에러 | 처리 |
|------|------|
| feature.md 없음 | 에러 메시지 반환 |
| 동작 불명확 | 사용자에게 질문 |

---

## 연결

- **이전 스킬**: `generate-plan`
- **다음 스킬**: `scaffold-csharp`
