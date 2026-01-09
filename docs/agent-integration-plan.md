# Agent 및 Skills 통합 계획

**프로젝트**: Unit-Simulator  
**목적**: agentic 프로젝트의 에이전트 개발 환경 설정을 unit-simulator에 도입  
**작성일**: 2026-01-09  
**상태**: 초안

---

## 1. 개요

### 1.1 목표
agentic 프로젝트에서 검증된 에이전트 기반 개발 인프라를 unit-simulator에 적용하여:
- 체계적인 문서 기반 개발 워크플로우 구축
- 에이전트 역할 분리를 통한 작업 품질 향상
- 재사용 가능한 스킬 패키지로 반복 작업 자동화
- C#/.NET 프로젝트에 특화된 에이전트 환경 구성

### 1.2 범위

**포함**:
- [ ] `.claude/` 디렉토리 구조 생성 (agents, commands, skills)
- [ ] `AGENTS.md` 작성 (unit-simulator 컨텍스트 반영)
- [ ] 핵심 에이전트 정의 (Planner, API Designer, Implementer, Tester, Reviewer, Documenter)
- [ ] 기본 스킬 구현 (generate-plan, generate-api-spec, generate-tests 등)
- [ ] C#/.NET 특화 프롬프트 및 템플릿 작성
- [ ] 기존 `CLAUDE.md` 개선 및 통합

**제외**:
- [ ] 외부 MCP 서버 연결 (추후 단계)
- [ ] 자동화 스크립트 실행 (수동 검증 후 점진 도입)
- [ ] GUI 기반 에이전트 대시보드

---

## 2. 현황 분석

### 2.1 agentic 프로젝트 구조

```
agentic/
├── AGENTS.md                    # 에이전트 운영 규칙 (Single Source of Truth)
├── CLAUDE.md                    # 행동 규칙 및 프롬프트 패턴
├── agentic_workflow.md          # 전체 워크플로우 정의
├── mcp.json                     # MCP 서버 연결 (선택)
├── specs/                       # 작업 명세 문서
│   ├── plan.md
│   ├── feature.md
│   ├── new_api_endpoint.md
│   ├── test-be.md
│   ├── code-review.md
│   └── pull_ticket.md
└── .claude/
    ├── agents/                  # 에이전트 정의
    │   ├── planner.md
    │   ├── api-designer.md
    │   ├── implementer.md
    │   ├── tester.md
    │   ├── reviewer.md
    │   └── documenter.md
    ├── commands/                # 명령어 단축
    │   ├── new-feature.md
    │   ├── new-api.md
    │   └── run-tests.md
    └── skills/                  # 재사용 가능한 스킬
        ├── generate-plan/
        ├── generate-api-spec/
        ├── scaffold-endpoint/
        ├── generate-tests/
        └── sync-docs/
```

**핵심 특징**:
- 문서 우선주의: 모든 작업은 specs/에서 시작
- 에이전트 역할 분리: 6개 전문 에이전트
- 스킬 캡슐화: 반복 작업의 자동화
- 명령어 매핑: `/new-feature`, `/new-api` 등

### 2.2 unit-simulator 프로젝트 구조

```
unit-simulator/
├── CLAUDE.md                    # 기존 행동 규칙 (C#/.NET 중심)
├── README.md
├── specs/                       # 명세 문서 (게임 시스템 중심)
│   ├── apis/
│   ├── control/
│   ├── features/
│   ├── game-systems/
│   ├── reviews/
│   ├── server/
│   └── tests/
├── docs/                        # 개발 가이드 및 참조 문서
│   ├── architecture/
│   ├── process/
│   ├── reference/
│   ├── testing/
│   ├── tasks/
│   ├── development-guide.md
│   └── sim-studio.md
├── UnitSimulator.Core/          # C# 코어 프로젝트
├── UnitSimulator.Server/        # WebSocket 서버
├── ReferenceModels/             # 데이터 모델
└── sim-studio/                  # React/TypeScript UI
```

**특징**:
- C#/.NET 9.0 기반 멀티 프로젝트 솔루션
- WebSocket 실시간 통신 프로토콜
- JSON 기반 ReferenceModels 데이터 시스템
- xUnit 테스트 프레임워크
- React + TypeScript GUI (sim-studio)

---

## 3. 통합 설계

### 3.1 디렉토리 구조 (최종안)

```
unit-simulator/
├── AGENTS.md                    # [신규] 에이전트 운영 규칙
├── CLAUDE.md                    # [개선] 기존 문서 + 에이전트 통합
├── specs/                       # [유지] 기존 구조 유지
│   ├── control/                 # 제어 문서
│   │   ├── plan.md
│   │   └── document.md          # ADR 포함
│   ├── features/
│   │   ├── feature.md
│   │   ├── bug.md
│   │   └── chore.md
│   ├── apis/
│   │   ├── new_api_endpoint.md
│   │   └── update_api_endpoint.md
│   ├── tests/
│   │   ├── test-core.md
│   │   ├── test-server.md
│   │   └── test-integration.md
│   ├── reviews/
│   │   ├── code-review.md
│   │   ├── review.md
│   │   └── pull_ticket.md
│   ├── game-systems/            # [유지] 기존 게임 명세
│   └── server/                  # [유지] 서버 명세
├── docs/                        # [유지] 참조 문서
└── .claude/                     # [신규] 에이전트 레이어
    ├── agents/
    │   ├── planner.md
    │   ├── api-designer.md
    │   ├── implementer.md
    │   ├── tester.md
    │   ├── reviewer.md
    │   └── documenter.md
    ├── commands/
    │   ├── new-feature.md
    │   ├── new-api.md
    │   ├── run-tests.md
    │   └── pre-pr.md
    └── skills/
        ├── generate-plan/
        │   ├── skill.md
        │   └── template-plan.md
        ├── generate-api-spec/
        │   ├── skill.md
        │   └── template-websocket-api.md
        ├── generate-tests/
        │   ├── skill.md
        │   └── template-xunit.md
        ├── scaffold-csharp/
        │   ├── skill.md
        │   └── templates/
        └── sync-docs/
            └── skill.md
```

### 3.2 에이전트 역할 정의 (Unit-Simulator 특화)

| 에이전트 | 역할 | 입력 | 출력 | C#/.NET 특화 사항 |
|----------|------|------|------|-------------------|
| **Planner** | 요구사항 분석 및 계획 수립 | 자연어 요구사항 | plan.md, feature.md | - 프로젝트 간 의존성 분석 (Core/Server/Models)<br>- async/await 필요성 판단<br>- ReferenceModels 스키마 변경 여부 |
| **API Designer** | WebSocket 프로토콜 설계 | feature.md | new_api_endpoint.md | - C# record DTO 설계<br>- System.Text.Json 직렬화<br>- WebSocket 메시지 타입 정의<br>- 요청/응답 페어링 |
| **Implementer** | C# 코드 구현 | API 명세, feature.md | *.cs 파일 | - .NET 9.0 네이밍 규칙<br>- nullable 참조 타입<br>- 의존성 주입 패턴<br>- XML 문서 주석 |
| **Tester** | xUnit 테스트 작성 및 실행 | 구현 코드 | *Tests.cs, test-*.md | - xUnit Fact/Theory<br>- 비동기 테스트 (async Task)<br>- 테스트 픽스처<br>- 커버리지 80% 목표 |
| **Reviewer** | 코드 리뷰 및 PR 문서 | Git diff, 관련 문서 | code-review.md, pull_ticket.md | - C# 코딩 규칙 검증<br>- async/await 올바른 사용<br>- IDisposable 구현 확인<br>- 브레이킹 체인지 식별 |
| **Documenter** | 문서 분류 및 동기화 | 코드 변경사항 | document.md, CHANGELOG.md | - ADR 작성 (아키텍처 결정)<br>- API 변경 추적<br>- 문서 분류 자동화 |

### 3.3 워크플로우 (기본 흐름)

```
[사용자 요구사항]
        ↓
    Planner (/new-feature)
        ↓
    specs/control/plan.md
    specs/features/feature.md
        ↓
    API Designer (/new-api)
        ↓
    specs/apis/new_api_endpoint.md
    (C# DTO 정의 포함)
        ↓
    Implementer (스킬: scaffold-csharp)
        ↓
    UnitSimulator.Core/*.cs
    UnitSimulator.Server/*.cs
    ReferenceModels/*.cs
        ↓
    Tester (/run-tests)
        ↓
    *Tests.cs
    specs/tests/test-*.md
        ↓
    Reviewer (/pre-pr)
        ↓
    specs/reviews/code-review.md
    specs/reviews/pull_ticket.md
        ↓
    Documenter (자동)
        ↓
    specs/control/document.md (ADR)
    CHANGELOG.md
```

### 3.4 스킬 정의 (우선순위)

#### Phase 1: 필수 스킬 (즉시 구현)

1. **generate-plan** (Planner)
   - 입력: 자연어 요구사항
   - 출력: plan.md, feature.md (또는 bug.md, chore.md)
   - 템플릿: C#/.NET 프로젝트 구조 반영
   
2. **generate-api-spec** (API Designer)
   - 입력: feature.md
   - 출력: new_api_endpoint.md
   - 템플릿: WebSocket 메시지 + C# DTO 정의

3. **generate-tests** (Tester)
   - 입력: 구현된 C# 코드
   - 출력: xUnit 테스트 파일, test-*.md
   - 템플릿: Fact, Theory, 비동기 테스트

4. **sync-docs** (Documenter)
   - 입력: Git diff
   - 출력: 갱신된 문서
   - 로직: 파일 분류, 링크 검증, ADR 생성

#### Phase 2: 확장 스킬 (점진 도입)

5. **scaffold-csharp** (Implementer)
   - 입력: API 명세
   - 출력: C# 클래스 스캐폴딩
   - 템플릿: 네임스페이스, 클래스, 인터페이스

6. **run-review** (Reviewer)
   - 입력: Git diff, 테스트 결과
   - 출력: code-review.md, pull_ticket.md
   - 체크리스트: C# 규칙, 성능, 보안

---

## 4. 구현 계획

### 4.1 Phase 1: 기반 구조 생성 (1일)

**목표**: 에이전트 레이어 기본 틀 구축

**작업**:
1. `.claude/` 디렉토리 구조 생성
2. `AGENTS.md` 초안 작성
   - 6개 에이전트 역할 정의
   - 협업 규칙 및 핸드오프 프로토콜
   - 스킬 호출 규칙
   - 제약사항 (금지/필수 행위)
3. 기존 `CLAUDE.md` 통합
   - 에이전트별 프롬프트 패턴 추가
   - C#/.NET 코딩 규칙 유지
   - 컨텍스트 관리 섹션 추가

**산출물**:
- `/unit-simulator/AGENTS.md`
- `/unit-simulator/CLAUDE.md` (갱신)
- `/unit-simulator/.claude/agents/` (6개 파일)
- `/unit-simulator/.claude/commands/` (4개 파일)

**검증**:
- [ ] AGENTS.md에 모든 에이전트 역할 정의됨
- [ ] CLAUDE.md가 C#/.NET 규칙 유지함
- [ ] 에이전트 파일이 입력/출력/프롬프트 포함함

### 4.2 Phase 2: 핵심 스킬 구현 (2일)

**목표**: 자주 사용되는 스킬 4개 구현

**작업**:
1. **generate-plan** 스킬
   - skill.md 작성 (메타데이터, 입력/출력, 실행 흐름)
   - template-plan.md 생성
   - template-feature.md 생성
   - C#/.NET 프로젝트 구조 반영 (Core/Server/Models)

2. **generate-api-spec** 스킬
   - skill.md 작성
   - template-websocket-api.md 생성
   - C# DTO record 예시 포함
   - System.Text.Json 속성 예시

3. **generate-tests** 스킬
   - skill.md 작성
   - template-xunit.md 생성
   - Fact, Theory, 비동기 테스트 템플릿
   - Arrange-Act-Assert 패턴

4. **sync-docs** 스킬
   - skill.md 작성
   - 문서 분류 로직 정의
   - ADR 템플릿 포함

**산출물**:
- `/unit-simulator/.claude/skills/generate-plan/` (2개 파일)
- `/unit-simulator/.claude/skills/generate-api-spec/` (2개 파일)
- `/unit-simulator/.claude/skills/generate-tests/` (2개 파일)
- `/unit-simulator/.claude/skills/sync-docs/` (1개 파일)

**검증**:
- [ ] 각 스킬에 실행 흐름 정의됨
- [ ] 템플릿이 C#/.NET 규칙 준수함
- [ ] 프롬프트가 명확하고 재현 가능함

### 4.3 Phase 3: 파일럿 테스트 (1일)

**목표**: 실제 기능 개발로 에이전트 시스템 검증

**시나리오**: "타워 스킬 시스템 추가"

**작업**:
1. Planner 에이전트로 계획 수립
   - 명령: `/new-feature "타워가 특수 스킬을 발동할 수 있는 시스템"`
   - 출력: specs/control/plan.md, specs/features/feature.md

2. API Designer 에이전트로 프로토콜 설계
   - 명령: `/new-api` (feature.md 기반)
   - 출력: specs/apis/new_api_endpoint.md (ActivateTowerSkillRequest/Response)

3. Implementer 에이전트로 코드 생성
   - 입력: API 명세
   - 출력: TowerSkillSystem.cs (Core), TowerSkillHandler.cs (Server)

4. Tester 에이전트로 테스트 작성
   - 명령: `/run-tests`
   - 출력: TowerSkillSystemTests.cs, test-core.md

5. Reviewer 에이전트로 리뷰
   - 명령: `/pre-pr`
   - 출력: code-review.md, pull_ticket.md

**검증**:
- [ ] 각 단계에서 올바른 문서/코드 생성됨
- [ ] C#/.NET 규칙 준수함
- [ ] 핸드오프가 자연스럽게 진행됨
- [ ] 생성된 코드가 빌드/테스트 통과함

**개선**:
- 파일럿에서 발견된 문제점 수정
- 템플릿 및 프롬프트 개선
- 에이전트 간 핸드오프 최적화

### 4.4 Phase 4: 문서화 및 교육 (1일)

**목표**: 팀원이 에이전트 시스템을 쉽게 사용할 수 있도록 문서화

**작업**:
1. 사용자 가이드 작성
   - docs/process/agentic-workflow.md 갱신
   - 각 명령어 사용 예시
   - 트러블슈팅 섹션

2. 빠른 시작 가이드
   - README.md에 에이전트 섹션 추가
   - 5분 안에 첫 기능 개발 가능하도록

3. 에이전트 개선 절차
   - 에이전트/스킬 수정 방법
   - 새로운 에이전트 추가 방법
   - 버전 관리 정책

**산출물**:
- `/unit-simulator/docs/process/agentic-workflow.md` (갱신)
- `/unit-simulator/docs/agent-quick-start.md` (신규)
- `/unit-simulator/README.md` (에이전트 섹션 추가)

**검증**:
- [ ] 신규 팀원이 가이드만으로 에이전트 사용 가능함
- [ ] 모든 명령어에 예시 포함됨
- [ ] 트러블슈팅 섹션이 충분함

---

## 5. C#/.NET 특화 고려사항

### 5.1 프로젝트 구조 이해

에이전트가 다음 프로젝트 구조를 이해하도록 프롬프트에 반영:

```
UnitSimulator.Core/              # 순수 시뮬레이션 로직
├── SimulatorCore.cs             # 메인 엔트리
├── Unit.cs, Tower.cs            # 게임 엔티티
├── Behaviors/                   # AI 행동
├── Combat/                      # 전투 메커닉
├── Pathfinding/                 # A* 경로찾기
└── Systems/                     # 게임 시스템

UnitSimulator.Server/            # WebSocket 서버
├── WebSocketServer.cs           # 서버 진입점
├── SimulationSession.cs         # 세션 관리
└── Handlers/                    # 메시지 핸들러

ReferenceModels/                 # 데이터 모델
├── Models/                      # 참조 데이터 클래스
├── Infrastructure/              # 데이터 로딩
└── Validation/                  # 검증 로직
```

### 5.2 명명 규칙 (프롬프트에 강제)

- 클래스/메서드/속성: `PascalCase`
- 로컬 변수/매개변수: `camelCase`
- private 필드: `_camelCase` (언더스코어 접두사)
- 비동기 메서드: `Async` 접미사 (`GetDataAsync`)
- 테스트 클래스: `*Tests` 접미사
- 테스트 메서드: `MethodName_Scenario_ExpectedResult`

### 5.3 코딩 패턴 (스킬 템플릿에 포함)

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

### 5.4 문서 템플릿 (C#/.NET 컨텍스트)

#### feature.md 템플릿
```markdown
# 기능: [기능명]

## 요구사항
[사용자 스토리]

## 완료 조건
- [ ] C# 클래스 구현됨
- [ ] xUnit 테스트 통과함
- [ ] WebSocket 프로토콜 정의됨
- [ ] ReferenceModels 데이터 스키마 갱신됨 (필요시)

## 영향받는 프로젝트
- [ ] UnitSimulator.Core: [변경 내용]
- [ ] UnitSimulator.Server: [변경 내용]
- [ ] ReferenceModels: [변경 내용]
- [ ] sim-studio: [변경 내용]

## C# 클래스 설계
### Core
- `TowerSkillSystem.cs`: 스킬 발동 로직

### Server
- `TowerSkillHandler.cs`: WebSocket 메시지 핸들러

### Models
- `TowerSkillReference.cs`: 스킬 데이터 참조

## 테스트 계획
- 단위 테스트: TowerSkillSystem.ActivateSkillAsync()
- 통합 테스트: WebSocket 엔드포인트
- 시나리오 테스트: 스킬 발동 → 효과 적용 → UI 갱신
```

#### new_api_endpoint.md 템플릿
```markdown
# WebSocket API: [메시지 타입]

## 메시지 정의

### 요청
**타입**: `ActivateTowerSkillRequest`

```csharp
public record ActivateTowerSkillRequest
{
    [JsonPropertyName("towerId")]
    public required string TowerId { get; init; }

    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }
}
```

### 응답
**타입**: `ActivateTowerSkillResponse`

```csharp
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

## JSON 예시

### 요청
```json
{
  "type": "ActivateTowerSkillRequest",
  "sessionId": "abc-123",
  "payload": {
    "towerId": "tower-1",
    "skillId": "skill-fireball"
  }
}
```

### 응답 (성공)
```json
{
  "type": "ActivateTowerSkillResponse",
  "success": true,
  "cooldown": 5000
}
```

### 응답 (실패)
```json
{
  "type": "ActivateTowerSkillResponse",
  "success": false,
  "error": "Skill on cooldown"
}
```

## 검증 규칙
- `towerId`: 비어있지 않음
- `skillId`: 비어있지 않음, ReferenceModels에 존재함
```

---

## 6. 리스크 및 대응

| 리스크 | 영향 | 확률 | 대응 |
|--------|------|------|------|
| 에이전트 프롬프트가 C# 코드를 잘못 생성 | 높음 | 중간 | - 파일럿 테스트로 검증<br>- 템플릿 상세화<br>- 코드 리뷰 필수화 |
| 기존 개발 워크플로우와 충돌 | 중간 | 낮음 | - 점진적 도입<br>- 선택적 사용 허용<br>- 기존 방식과 병행 |
| 에이전트 간 핸드오프가 불명확 | 중간 | 중간 | - AGENTS.md에 명확한 규칙<br>- 핸드오프 프로토콜 문서화 |
| 스킬 유지보수 비용 증가 | 낮음 | 높음 | - 스킬은 최소한으로 유지<br>- 공통 패턴만 스킬화 |
| 팀원의 학습 곡선 | 중간 | 중간 | - 상세한 문서 제공<br>- 빠른 시작 가이드<br>- 페어 프로그래밍 |

---

## 7. 성공 기준

### 7.1 Phase 1 성공 기준
- [ ] `.claude/` 디렉토리 구조 생성됨
- [ ] `AGENTS.md`에 모든 에이전트 역할 정의됨
- [ ] 기존 `CLAUDE.md`와 통합되어 일관성 유지됨

### 7.2 Phase 2 성공 기준
- [ ] 4개 핵심 스킬 구현됨
- [ ] 각 스킬에 명확한 실행 흐름 정의됨
- [ ] 템플릿이 C#/.NET 규칙 준수함

### 7.3 Phase 3 성공 기준
- [ ] 파일럿 기능이 에이전트만으로 개발됨
- [ ] 생성된 코드가 빌드 및 테스트 통과함
- [ ] 각 에이전트 단계가 자연스럽게 연결됨

### 7.4 Phase 4 성공 기준
- [ ] 신규 팀원이 가이드만으로 에이전트 사용 가능함
- [ ] 모든 명령어에 예시 포함됨
- [ ] 트러블슈팅 문서가 충분함

### 7.5 전체 성공 기준 (최종)
- [ ] 에이전트 시스템으로 최소 3개 기능 개발 완료
- [ ] 팀원 만족도 조사 결과 긍정적
- [ ] 개발 속도 및 품질 향상 확인됨

---

## 8. 타임라인

| Phase | 작업 | 기간 | 담당 | 완료 조건 |
|-------|------|------|------|-----------|
| Phase 1 | 기반 구조 생성 | 1일 | 개발자 | `.claude/` 디렉토리, AGENTS.md 완성 |
| Phase 2 | 핵심 스킬 구현 | 2일 | 개발자 | 4개 스킬 완성 |
| Phase 3 | 파일럿 테스트 | 1일 | 개발자 + 검토자 | 타워 스킬 시스템 완성 |
| Phase 4 | 문서화 및 교육 | 1일 | 개발자 | 가이드 문서 완성 |
| **합계** | | **5일** | | |

---

## 9. 다음 단계

### 9.1 즉시 실행
1. **이 문서 검토 및 승인**
   - 관계자 리뷰
   - 피드백 반영
   - 최종 승인

2. **Phase 1 착수**
   - `.claude/` 디렉토리 생성
   - `AGENTS.md` 초안 작성

### 9.2 Phase 1 완료 후
1. Phase 1 결과물 검토
2. Phase 2 착수
3. 스킬 템플릿 작성

### 9.3 전체 완료 후
1. 팀 교육 세션
2. 실제 프로젝트에 적용
3. 피드백 수집 및 개선

---

## 10. 참조

### 10.1 관련 문서
- `unit-simulator/CLAUDE.md`: 기존 행동 규칙
- `unit-simulator/docs/development-guide.md`: 개발 가이드
- `agentic/AGENTS.md`: agentic 프로젝트 에이전트 규칙
- `agentic/agentic_workflow.md`: 워크플로우 정의

### 10.2 외부 참조
- [Claude CLI Documentation](https://docs.anthropic.com/claude/docs)
- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [xUnit Documentation](https://xunit.net/)

---

## 부록 A: 명령어 매핑

| 명령어 | 실행 에이전트 | 실행 스킬 | 출력 |
|--------|---------------|-----------|------|
| `/new-feature [요구사항]` | Planner | generate-plan | plan.md, feature.md |
| `/new-bug [설명]` | Planner | generate-plan | plan.md, bug.md |
| `/new-chore [작업]` | Planner | generate-plan | plan.md, chore.md |
| `/new-api` | API Designer | generate-api-spec | new_api_endpoint.md |
| `/run-tests` | Tester | generate-tests | *Tests.cs, test-*.md |
| `/pre-pr` | Reviewer | run-review | code-review.md, pull_ticket.md |

---

## 부록 B: 스킬 우선순위 매트릭스

| 스킬 | 사용 빈도 | 구현 난이도 | 영향도 | 우선순위 |
|------|-----------|-------------|--------|----------|
| generate-plan | 매우 높음 | 중간 | 높음 | **P1** |
| generate-api-spec | 높음 | 중간 | 높음 | **P1** |
| generate-tests | 높음 | 낮음 | 중간 | **P1** |
| sync-docs | 중간 | 낮음 | 중간 | **P1** |
| scaffold-csharp | 중간 | 높음 | 중간 | P2 |
| run-review | 낮음 | 중간 | 낮음 | P2 |

---

**문서 버전**: 1.0  
**작성자**: Claude (Agent Integration Planner)  
**최종 수정일**: 2026-01-09  
**다음 검토**: Phase 1 완료 후
