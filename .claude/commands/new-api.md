# Command: /new-api

WebSocket API 엔드포인트를 설계하고 C# DTO를 정의한다.

---

## 사용법

```
/new-api                      # feature.md 기반 자동 생성
/new-api --message=TowerSkill # 메시지 타입 지정
/new-api --scaffold           # 핸들러 코드까지 생성
```

---

## 예시

```
/new-api                              # specs/features/feature.md 읽고 API 설계
/new-api --message=ActivateTowerSkill # 특정 메시지 타입 설계
/new-api --scaffold                   # API 설계 + 핸들러 스캐폴드 생성
/new-api --update                     # 기존 API 수정 모드
```

---

## 실행 흐름

```
1. 기능 정의 로드
   └─ specs/features/feature.md 읽기

2. API Designer 에이전트 활성화
   └─ .claude/agents/api-designer.md 참조

3. generate-api-spec 스킬 실행
   ├─ 입력: feature.md
   └─ 출력: specs/apis/new_api_endpoint.md

4. (--scaffold 옵션 시) scaffold-csharp 스킬 실행
   ├─ 입력: new_api_endpoint.md
   └─ 출력: 
      ├─ UnitSimulator.Server/Handlers/{Entity}Handler.cs
      └─ DTO record 클래스

5. 결과 보고
   ├─ 생성된 메시지 타입 목록
   ├─ 생성된 파일 목록 (scaffold 시)
   └─ 다음 단계 안내
```

---

## 생성되는 문서/코드

### specs/apis/new_api_endpoint.md
```markdown
# WebSocket API: [메시지 타입]

## 메시지 정의

### 요청
```csharp
public record {Action}{Entity}Request
{
    [JsonPropertyName("field")]
    public required string Field { get; init; }
}
```

### 응답
```csharp
public record {Action}{Entity}Response
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }
}
```

## JSON 예시
[요청/응답 JSON]

## 검증 규칙
[필드별 검증]

## 에러 케이스
[에러 응답 정의]
```

### (--scaffold 시) 코드 파일
```
UnitSimulator.Server/
└── Handlers/
    └── {Entity}Handler.cs

UnitSimulator.Core/
└── Messages/
    └── {Action}{Entity}Request.cs
    └── {Action}{Entity}Response.cs
```

---

## 연결 명령어

| 순서 | 명령어 | 설명 |
|------|--------|------|
| 이전 | `/new-feature` | 기능 정의 |
| 현재 | `/new-api` | WebSocket API 설계 |
| 다음 | `/run-tests` | 테스트 생성 |

---

## 옵션

| 옵션 | 설명 | 기본값 |
|------|------|--------|
| --message | 메시지 타입명 지정 | 자동 추출 |
| --scaffold | 핸들러 코드 생성 | false |
| --update | 기존 API 수정 모드 | false |

---

## C# 코드 패턴

### DTO (record 타입)
```csharp
public record ActivateTowerSkillRequest
{
    [JsonPropertyName("towerId")]
    public required string TowerId { get; init; }

    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }
}

public record ActivateTowerSkillResponse
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("cooldown")]
    public int? Cooldown { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
```

### 핸들러 스캐폴드
```csharp
public class TowerSkillHandler
{
    private readonly SimulatorCore _simulator;

    public TowerSkillHandler(SimulatorCore simulator)
    {
        _simulator = simulator;
    }

    public async Task<ActivateTowerSkillResponse> HandleActivateTowerSkillAsync(
        ActivateTowerSkillRequest request,
        SimulationSession session)
    {
        // TODO: 구현
        throw new NotImplementedException();
    }
}
```

---

## 체크리스트

명령어 실행 후 확인:
- [ ] API 스펙 문서가 생성되었는가?
- [ ] C# record DTO가 정의되었는가?
- [ ] JsonPropertyName 속성이 모든 필드에 있는가?
- [ ] nullable 타입이 적절히 사용되었는가?
- [ ] 검증 규칙이 명시되었는가?
- [ ] 에러 케이스가 정의되었는가?
- [ ] (scaffold 시) 핸들러 코드가 생성되었는가?
- [ ] (scaffold 시) 기존 코드 패턴과 일관성이 있는가?
