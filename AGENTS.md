# AGENTS.md - 에이전트 운영 규칙

이 문서는 unit-simulator 프로젝트의 모든 에이전트가 따르는 단일 진실 원천(Single Source of Truth)입니다.

---

## 1. 에이전트 목록 및 역할

| 에이전트 | 파일 | 역할 | 트리거 |
|----------|------|------|--------|
| Planner | `.claude/agents/planner.md` | 요구사항 분석 → plan.md, feature.md 생성 | `/new-feature`, `/new-bug`, `/new-chore` |
| API Designer | `.claude/agents/api-designer.md` | 기능 정의 → WebSocket API 스펙 생성 | `/new-api` |
| Implementer | `.claude/agents/implementer.md` | 스펙 기반 C# 코드 구현 | 구현 단계 |
| Tester | `.claude/agents/tester.md` | xUnit 테스트 생성 및 실행 | `/run-tests` |
| Reviewer | `.claude/agents/reviewer.md` | C#/.NET 코드 리뷰 및 PR 문서 작성 | `/pre-pr` |
| Documenter | `.claude/agents/documenter.md` | 문서 분류, 동기화, ADR 작성 | `/sync-docs` |

---

## 2. 협업 규칙

### 2.1 작업 흐름

```
[요구사항] → Planner → API Designer → Implementer → Tester → Reviewer
                ↓                                        ↓
            plan.md                               code-review.md
            feature.md                            pull_ticket.md
                                                       ↓
                                                  Documenter
                                                       ↓
                                                  document.md (ADR)
                                                  CHANGELOG.md
```

### 2.2 문서 소유권

각 에이전트는 자신의 담당 문서만 생성/수정합니다. 다른 에이전트의 문서를 수정해야 할 경우, 해당 에이전트에게 위임합니다.

| 에이전트 | 소유 문서 |
|----------|-----------|
| Planner | `specs/control/plan.md`, `specs/features/feature.md`, `specs/features/bug.md`, `specs/features/chore.md` |
| API Designer | `specs/apis/new_api_endpoint.md`, `specs/apis/update_api_endpoint.md` |
| Implementer | 소스 코드 (`UnitSimulator.Core/`, `UnitSimulator.Server/`, `ReferenceModels/`) |
| Tester | `specs/tests/test-core.md`, `specs/tests/test-server.md`, `*Tests.cs` 파일 |
| Reviewer | `specs/reviews/code-review.md`, `specs/reviews/review.md`, `specs/reviews/pull_ticket.md` |
| Documenter | `specs/control/document.md`, `CHANGELOG.md`, 문서 분류 및 링크 관리 |

### 2.3 핸드오프 프로토콜

1. 현재 에이전트는 작업 완료 시 담당 문서를 갱신합니다
2. 다음 에이전트에게 필요한 컨텍스트를 문서에 명시합니다
3. 다음 에이전트는 이전 문서를 읽고 작업을 시작합니다

**핸드오프 체크리스트**:
- [ ] 출력 문서가 필수 섹션을 모두 포함하는가?
- [ ] 다음 에이전트가 이해할 수 있는 명확한 언어인가?
- [ ] 테스트 가능한 완료 조건이 정의되었는가?

---

## 3. 제약사항

### 3.1 금지 행위

- [ ] 사용자 확인 없이 외부 API 호출
- [ ] 민감 정보(API 키, 비밀번호)를 문서에 기록
- [ ] `specs/` 외부에 스펙 문서 생성
- [ ] 테스트 없이 구현 완료 선언
- [ ] 기존 테스트를 삭제하거나 무시
- [ ] `as any`, `@ts-ignore` 등 타입 안전성 우회 (TypeScript)
- [ ] C# nullable 경고 무시 (`#pragma warning disable`)

### 3.2 필수 행위

- [x] 모든 작업은 `specs/control/plan.md`에 기록된 범위 내에서 수행
- [x] 변경사항은 반드시 관련 문서에 반영
- [x] 에러 발생 시 `specs/features/bug.md`에 재현 절차 기록
- [x] 코드 변경 전 테스트 케이스 확인
- [x] C# 코드는 XML 문서 주석 포함
- [x] async/await 패턴 일관성 유지
- [x] 빌드/테스트 통과 확인 후 완료 선언

---

## 4. 스킬 사용 규칙

### 4.1 스킬 호출 조건

| 스킬 | 호출 조건 | 호출 주체 |
|------|-----------|-----------|
| `generate-plan` | 새 기능/버그/정비 작업 시작 | Planner |
| `generate-api-spec` | WebSocket API 엔드포인트 필요 시 | API Designer |
| `generate-tests` | 구현 완료 후 | Tester |
| `sync-docs` | 커밋 완료 후 | Documenter |

### 4.2 스킬 체이닝

스킬은 순차적으로 실행되며, 이전 스킬의 출력이 다음 스킬의 입력이 됩니다.

```
generate-plan → generate-api-spec → [구현] → generate-tests → [리뷰] → sync-docs
```

---

## 5. 명령어 매핑

| 명령어 | 실행 에이전트 | 실행 스킬 | 출력 |
|--------|---------------|-----------|------|
| `/new-feature [요구사항]` | Planner | `generate-plan` | plan.md, feature.md |
| `/new-bug [설명]` | Planner | `generate-plan` | plan.md, bug.md |
| `/new-chore [작업]` | Planner | `generate-plan` | plan.md, chore.md |
| `/new-api` | API Designer | `generate-api-spec` | new_api_endpoint.md |
| `/run-tests [--project]` | Tester | `generate-tests` | *Tests.cs, test-*.md |
| `/pre-pr` | Reviewer | `run-review` | code-review.md, pull_ticket.md |
| `/sync-docs` | Documenter | `sync-docs` | document.md, CHANGELOG.md |

---

## 6. 에러 처리

### 6.1 에러 발생 시

1. 현재 작업을 중단하고 에러를 기록
2. `specs/features/bug.md`에 재현 절차 작성
3. 사용자에게 에러 보고 및 다음 단계 확인

### 6.2 복구 절차

1. 에러 원인 분석 (`bug.md` 작성)
2. 수정 후 테스트 (`specs/tests/test-*.md` 갱신)
3. 정상 확인 후 워크플로우 재개

### 6.3 빌드/테스트 실패 시

```bash
# 빌드 실패
dotnet build UnitSimulator.sln

# 테스트 실패
dotnet test UnitSimulator.Core.Tests
dotnet test ReferenceModels.Tests
```

실패 시:
1. 에러 메시지 분석
2. 영향받는 파일 식별
3. 최소 수정으로 해결
4. 재빌드/재테스트

---

## 7. C#/.NET 특화 규칙

### 7.1 프로젝트 구조 이해

```
UnitSimulator.Core/          # 순수 시뮬레이션 로직 (의존성 최소)
UnitSimulator.Server/        # WebSocket 서버, 세션 관리
ReferenceModels/             # 데이터 모델, JSON 스키마
UnitSimulator.Core.Tests/    # Core 테스트
ReferenceModels.Tests/       # Models 테스트
sim-studio/                  # React/TypeScript UI
```

### 7.2 명명 규칙

| 구분 | 규칙 | 예시 |
|------|------|------|
| 클래스/메서드/속성 | PascalCase | `TowerUpgradeSystem`, `CalculateDamage()` |
| 로컬 변수/매개변수 | camelCase | `currentTarget`, `damageAmount` |
| private 필드 | _camelCase | `_simulatorCore`, `_sessionManager` |
| 상수 | PascalCase | `MaxUnitCount`, `DefaultTimeout` |
| 비동기 메서드 | Async 접미사 | `LoadDataAsync()`, `ProcessFrameAsync()` |
| 테스트 메서드 | Method_Scenario_Expected | `CalculateDamage_CriticalHit_ReturnsDoubled` |

### 7.3 코딩 패턴

**DTO 정의 (record 타입)**:
```csharp
public record ActivateTowerSkillRequest
{
    [JsonPropertyName("towerId")]
    public required string TowerId { get; init; }

    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }
}
```

**WebSocket 핸들러**:
```csharp
public class TowerSkillHandler : IMessageHandler
{
    public async Task<object> HandleAsync(
        ActivateTowerSkillRequest request,
        SimulationSession session)
    {
        // 구현
    }
}
```

**xUnit 테스트**:
```csharp
public class TowerSkillSystemTests
{
    [Fact]
    public async Task ActivateSkill_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var system = new TowerSkillSystem();
        
        // Act
        var result = await system.ActivateSkillAsync("tower-1", "skill-fireball");
        
        // Assert
        Assert.True(result.Success);
    }
}
```

---

## 8. 버전 및 변경 이력

| 버전 | 날짜 | 변경 내용 |
|------|------|-----------|
| 1.0 | 2026-01-09 | 초기 버전 작성 (agentic 기반 적응) |

---

## 9. 참조

- `CLAUDE.md`: 행동 규칙 및 프롬프트 패턴
- `docs/agent-integration-plan.md`: 에이전트 통합 상세 계획
- `docs/agentic-migration-plan-ko.md`: 마이그레이션 로드맵
- `.claude/agents/`: 개별 에이전트 상세 정의
- `.claude/skills/`: 스킬 패키지
