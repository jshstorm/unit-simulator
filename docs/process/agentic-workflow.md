# 에이전트 기반 개발 워크플로우

Unit-Simulator 프로젝트에서 에이전트 기반 개발 환경을 사용하는 가이드입니다.

---

## 개요

이 프로젝트는 **6개의 전문 에이전트**와 **4개의 명령어**, **6개의 스킬**을 사용하여 체계적인 개발 워크플로우를 지원합니다.

### 핵심 원칙
- **문서 우선**: 모든 작업은 `specs/` 디렉토리의 문서에서 시작
- **역할 분리**: 각 에이전트는 특정 역할에 집중
- **단계적 진행**: 계획 → 설계 → 구현 → 테스트 → 리뷰

---

## 에이전트 구성

### 1. Planner (계획자)
**역할**: 요구사항 분석 및 계획 수립

**입력**:
- 사용자 요구사항 (자연어)

**출력**:
- `specs/control/plan.md` (갱신)
- `specs/features/feature.md` (생성)

**트리거**:
```
/new-feature "기능 설명"
```

---

### 2. API Designer (API 설계자)
**역할**: WebSocket API 프로토콜 설계

**입력**:
- `specs/features/feature.md`

**출력**:
- `specs/apis/new_api_endpoint.md`
- C# DTO 정의

**트리거**:
```
/new-api
```

---

### 3. Implementer (구현자)
**역할**: C# 코드 구현

**입력**:
- `specs/apis/new_api_endpoint.md`
- 기존 코드 패턴

**출력**:
- `UnitSimulator.Core/Systems/*.cs`
- `UnitSimulator.Server/Handlers/*.cs`
- `UnitSimulator.Server/Messages/*.cs`

---

### 4. Tester (테스터)
**역할**: xUnit 테스트 생성 및 실행

**입력**:
- 구현된 C# 코드
- `specs/apis/new_api_endpoint.md`

**출력**:
- `specs/tests/test-*.md`
- `*.Tests/*.cs` 테스트 파일

**트리거**:
```
/run-tests
```

---

### 5. Reviewer (리뷰어)
**역할**: 코드 리뷰 및 PR 준비

**입력**:
- Git diff
- 테스트 결과
- 요구사항 문서

**출력**:
- `specs/reviews/code-review.md`
- `specs/reviews/review.md`
- `specs/reviews/pull_ticket.md`

**트리거**:
```
/pre-pr
```

---

### 6. Documenter (문서화 담당)
**역할**: 문서 분류 및 ADR 작성

**입력**:
- 변경 사항
- 기술적 결정

**출력**:
- `docs/architecture/adr/*.md`
- 기존 문서 업데이트

---

## 명령어 가이드

### /new-feature
새로운 기능 개발을 시작합니다.

```bash
# 기본 사용
/new-feature "타워가 특수 스킬을 발동할 수 있는 시스템"

# 대화형 모드
/new-feature

# 버그 수정
/new-feature --type=bug "로그인 실패 버그"

# 정비 작업
/new-feature --type=chore "의존성 업데이트"
```

**결과**:
- `specs/control/plan.md`에 마일스톤 추가
- `specs/features/feature.md` 생성
- 다음 단계 (`/new-api`) 안내

---

### /new-api
WebSocket API를 설계합니다.

```bash
/new-api
```

**전제 조건**:
- `specs/features/feature.md`가 존재해야 함

**결과**:
- `specs/apis/new_api_endpoint.md` 생성
- C# record DTO 정의 포함
- JSON 예시 포함

---

### /run-tests
xUnit 테스트를 생성하고 실행합니다.

```bash
# 전체 테스트
/run-tests

# 특정 파일만
/run-tests TowerSkillSystem

# 커버리지 포함
/run-tests --coverage
```

**결과**:
- `specs/tests/test-*.md` 생성
- 테스트 코드 파일 생성
- `dotnet test` 실행 결과

---

### /pre-pr
코드 리뷰 및 PR을 준비합니다.

```bash
/pre-pr
```

**결과**:
- `specs/reviews/code-review.md` - 코드 품질 리뷰
- `specs/reviews/review.md` - 기능 검증 결과
- `specs/reviews/pull_ticket.md` - PR 문서

---

## 전형적인 워크플로우

### 새 기능 개발

```
1. /new-feature "기능 설명"
   └── specs/control/plan.md (갱신)
   └── specs/features/feature.md (생성)
       
2. /new-api
   └── specs/apis/new_api_endpoint.md (생성)
       
3. [구현] - Implementer가 코드 작성
   └── UnitSimulator.Core/...
   └── UnitSimulator.Server/...
       
4. /run-tests
   └── specs/tests/test-*.md (생성)
   └── *.Tests/*.cs (생성)
   └── dotnet test 실행
       
5. /pre-pr
   └── specs/reviews/code-review.md
   └── specs/reviews/pull_ticket.md
       
6. [PR 생성 및 머지]
```

### 버그 수정

```
1. /new-feature --type=bug "버그 설명"
   └── specs/features/bug.md (생성)
       
2. [구현] - 버그 수정
       
3. /run-tests
   └── 회귀 테스트 포함
       
4. /pre-pr
```

---

## 스킬 목록

| 스킬 | 설명 | 관련 에이전트 |
|------|------|---------------|
| `generate-plan` | 계획/기능 문서 생성 | Planner |
| `generate-api-spec` | WebSocket API 스펙 생성 | API Designer |
| `scaffold-csharp` | C# 클래스 스캐폴딩 | Implementer |
| `generate-tests` | xUnit 테스트 생성 | Tester |
| `run-review` | 코드 리뷰 자동화 | Reviewer |
| `sync-docs` | 문서 동기화 및 ADR | Documenter |

---

## 디렉토리 구조

```
unit-simulator/
├── .claude/
│   ├── agents/          # 에이전트 정의
│   │   ├── planner.md
│   │   ├── api-designer.md
│   │   ├── implementer.md
│   │   ├── tester.md
│   │   ├── reviewer.md
│   │   └── documenter.md
│   ├── commands/        # 명령어 정의
│   │   ├── new-feature.md
│   │   ├── new-api.md
│   │   ├── run-tests.md
│   │   └── pre-pr.md
│   └── skills/          # 스킬 정의
│       ├── generate-plan/
│       ├── generate-api-spec/
│       ├── scaffold-csharp/
│       ├── generate-tests/
│       ├── run-review/
│       └── sync-docs/
├── specs/               # 작업 명세
│   ├── control/         # 계획 문서
│   ├── features/        # 기능 정의
│   ├── apis/            # API 스펙
│   ├── tests/           # 테스트 계획
│   └── reviews/         # 리뷰 결과
├── docs/                # 개발 가이드
└── AGENTS.md            # 에이전트 운영 규칙
```

---

## C#/.NET 규칙

에이전트가 생성하는 코드는 다음 규칙을 따릅니다:

### 네이밍
- 클래스/메서드/속성: `PascalCase`
- 로컬 변수/매개변수: `camelCase`
- private 필드: `_camelCase`
- 비동기 메서드: `Async` 접미사
- 테스트 클래스: `*Tests`

### 코딩 패턴
```csharp
// DTO는 record 타입
public record RequestDto
{
    [JsonPropertyName("fieldName")]
    public required string FieldName { get; init; }
}

// 핸들러는 IMessageHandler 구현
public class FeatureHandler : IMessageHandler<RequestDto>
{
    public async Task<ResponseDto> HandleAsync(
        RequestDto request,
        SimulationSession session,
        CancellationToken cancellationToken = default);
}

// 테스트는 xUnit
[Fact]
public async Task Method_Scenario_ExpectedResult()
{
    // Arrange
    // Act
    // Assert
}
```

---

## 문제 해결

### 명령어가 작동하지 않을 때
1. 현재 디렉토리 확인 (`unit-simulator/` 루트여야 함)
2. 필요한 입력 문서 존재 확인
3. `.claude/commands/` 파일 확인

### 에이전트가 잘못된 패턴을 사용할 때
1. 기존 코드 패턴을 명시적으로 언급
2. `.claude/skills/` 템플릿 수정
3. `CLAUDE.md`에 규칙 추가

### 테스트가 실패할 때
1. `dotnet build` 먼저 확인
2. 의존성 확인 (`dotnet restore`)
3. 테스트 코드와 실제 구현 일치 확인

---

## 참고 문서

- [AGENTS.md](../../AGENTS.md) - 에이전트 운영 규칙
- [CLAUDE.md](../../CLAUDE.md) - AI 행동 규칙
- [agent-integration-plan.md](../agent-integration-plan.md) - 통합 계획
