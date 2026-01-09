# 에이전트 빠른 시작 가이드

Unit-Simulator 프로젝트에서 에이전트 기반 개발을 시작하는 5분 가이드입니다.

---

## 시작하기

### 1. 새 기능 개발 시작

```bash
/new-feature "타워가 특수 스킬을 발동할 수 있는 시스템"
```

이 명령은:
- ✅ `specs/control/plan.md`에 마일스톤 추가
- ✅ `specs/features/feature.md` 생성
- ✅ 영향받는 프로젝트 분석

---

### 2. API 설계

```bash
/new-api
```

이 명령은:
- ✅ `specs/apis/new_api_endpoint.md` 생성
- ✅ C# record DTO 정의
- ✅ JSON 요청/응답 예시
- ✅ 에러 케이스 정의

---

### 3. 구현

에이전트가 자동으로:
- `UnitSimulator.Core/Systems/` 비즈니스 로직
- `UnitSimulator.Server/Handlers/` WebSocket 핸들러
- `UnitSimulator.Server/Messages/` DTO 클래스

---

### 4. 테스트

```bash
/run-tests
```

이 명령은:
- ✅ `specs/tests/test-*.md` 생성
- ✅ xUnit 테스트 코드 생성
- ✅ `dotnet test` 실행

---

### 5. PR 준비

```bash
/pre-pr
```

이 명령은:
- ✅ 코드 리뷰 (`specs/reviews/code-review.md`)
- ✅ 기능 검증 (`specs/reviews/review.md`)
- ✅ PR 문서 (`specs/reviews/pull_ticket.md`)

---

## 명령어 요약

| 명령어 | 용도 | 결과물 |
|--------|------|--------|
| `/new-feature "설명"` | 기능 정의 | plan.md, feature.md |
| `/new-api` | API 설계 | new_api_endpoint.md |
| `/run-tests` | 테스트 | test-*.md, *.Tests.cs |
| `/pre-pr` | PR 준비 | code-review.md, pull_ticket.md |

---

## 워크플로우 다이어그램

```
┌─────────────────────────────────────────────────────────────┐
│                    /new-feature                              │
│                         │                                    │
│                         ▼                                    │
│              ┌──────────────────┐                           │
│              │    Planner       │                           │
│              │   (계획 수립)     │                           │
│              └────────┬─────────┘                           │
│                       │                                      │
│         specs/control/plan.md                               │
│         specs/features/feature.md                           │
│                       │                                      │
│                       ▼                                      │
│                    /new-api                                  │
│                       │                                      │
│                       ▼                                      │
│              ┌──────────────────┐                           │
│              │  API Designer    │                           │
│              │   (API 설계)      │                           │
│              └────────┬─────────┘                           │
│                       │                                      │
│         specs/apis/new_api_endpoint.md                      │
│                       │                                      │
│                       ▼                                      │
│              ┌──────────────────┐                           │
│              │  Implementer     │                           │
│              │   (코드 구현)     │                           │
│              └────────┬─────────┘                           │
│                       │                                      │
│         UnitSimulator.Core/                                 │
│         UnitSimulator.Server/                               │
│                       │                                      │
│                       ▼                                      │
│                   /run-tests                                 │
│                       │                                      │
│                       ▼                                      │
│              ┌──────────────────┐                           │
│              │    Tester        │                           │
│              │   (테스트 작성)   │                           │
│              └────────┬─────────┘                           │
│                       │                                      │
│         specs/tests/test-*.md                               │
│         *.Tests/*.cs                                        │
│                       │                                      │
│                       ▼                                      │
│                    /pre-pr                                   │
│                       │                                      │
│                       ▼                                      │
│              ┌──────────────────┐                           │
│              │   Reviewer       │                           │
│              │   (코드 리뷰)     │                           │
│              └────────┬─────────┘                           │
│                       │                                      │
│         specs/reviews/code-review.md                        │
│         specs/reviews/pull_ticket.md                        │
│                       │                                      │
│                       ▼                                      │
│                  [PR 생성]                                   │
└─────────────────────────────────────────────────────────────┘
```

---

## 예시: 타워 스킬 시스템

### Step 1: 기능 정의
```bash
/new-feature "타워가 특수 스킬을 발동할 수 있는 시스템"
```

**생성된 문서:**
- `specs/control/plan.md` - 마일스톤 및 리스크
- `specs/features/tower-skill-system.md` - 상세 요구사항

### Step 2: API 설계
```bash
/new-api
```

**생성된 문서:**
- `specs/apis/tower-skill-api.md` - WebSocket 프로토콜

**포함 내용:**
```csharp
public record ActivateTowerSkillRequest
{
    [JsonPropertyName("towerId")]
    public required string TowerId { get; init; }
    
    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }
}
```

### Step 3: 구현
에이전트가 자동 생성:
- `UnitSimulator.Core/Systems/TowerSkillSystem.cs`
- `UnitSimulator.Server/Handlers/TowerSkillHandler.cs`
- `UnitSimulator.Server/Messages/TowerSkillMessages.cs`

### Step 4: 테스트
```bash
/run-tests
```

**생성된 테스트:**
```csharp
[Fact]
public async Task ActivateSkill_ValidInput_ReturnsSuccess()
{
    // Arrange
    var request = new ActivateTowerSkillRequest
    {
        TowerId = "tower-1",
        SkillId = "skill-fireball"
    };
    
    // Act
    var result = await _sut.ActivateSkillAsync(
        request.TowerId, request.SkillId, null);
    
    // Assert
    Assert.True(result.Success);
}
```

### Step 5: PR 준비
```bash
/pre-pr
```

**생성된 리뷰:**
- 코드 품질 검증
- C#/.NET 규칙 확인
- PR 문서 자동 생성

---

## 자주 묻는 질문

### Q: 버그 수정은 어떻게 하나요?
```bash
/new-feature --type=bug "버그 설명"
```

### Q: 기존 기능을 수정하려면?
1. 해당 `specs/features/*.md` 파일 업데이트
2. `/run-tests`로 회귀 테스트
3. `/pre-pr`로 리뷰

### Q: 테스트만 추가하려면?
```bash
/run-tests TowerSkillSystem
```

### Q: 에이전트가 잘못된 코드를 생성하면?
1. 피드백 제공: "이 패턴 대신 이렇게 해줘"
2. 기존 코드 예시 제공
3. `.claude/skills/` 템플릿 수정

---

## 프로젝트별 경로

| 프로젝트 | 경로 | 용도 |
|----------|------|------|
| Core | `UnitSimulator.Core/` | 시뮬레이션 로직 |
| Server | `UnitSimulator.Server/` | WebSocket 서버 |
| Models | `ReferenceModels/` | 데이터 모델 |
| Tests | `*.Tests/` | xUnit 테스트 |
| UI | `sim-studio/` | React GUI |

---

## 다음 단계

자세한 내용은 다음 문서를 참고하세요:
- [에이전트 워크플로우 가이드](process/agentic-workflow.md)
- [AGENTS.md](../AGENTS.md) - 에이전트 운영 규칙
- [CLAUDE.md](../CLAUDE.md) - AI 행동 규칙
