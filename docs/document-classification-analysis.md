# Unit-Simulator 문서 분류 및 재구성 계획

## 현재 문서 현황 (15개 파일, 총 279K)

### 📋 현재 문서 목록

| 파일명 | 크기 | 현재 유형 | 제안 분류 |
|--------|------|-----------|-----------|
| agentic-comparison-summary-ko.md | 15K | 마이그레이션 | 🔧 프로세스/마이그레이션 |
| agentic-migration-plan-ko.md | 26K | 마이그레이션 | 🔧 프로세스/마이그레이션 |
| core-integration-plan.md | 13K | 통합 계획 | 📐 아키텍처/통합 |
| development-guide.md | 8.4K | 개발 가이드 | 📚 참조/개발자 가이드 |
| development-milestone.md | 50K | 로드맵 | 🔧 프로세스/로드맵 |
| initial-setup-spec.md | 3.9K | 기술 명세 | 📋 명세/게임시스템 |
| multi-session-spec.md | 21K | 기술 명세 | 📋 명세/서버 |
| reference-models-expansion-plan.md | 31K | 확장 계획 | 📐 아키텍처/ReferenceModels |
| reference-models-testing-plan.md | 18K | 테스팅 계획 | 🧪 테스팅/전략 |
| session-logging.md | 5.1K | 기능 문서 | 📚 참조/디버깅 |
| sim-studio.md | 19K | 컴포넌트 문서 | 📚 참조/UI |
| simulation-spec.md | 12K | 기술 명세 | 📋 명세/게임시스템 |
| todo_reference-models.md | 4.2K | 작업 추적 | ✅ 작업추적/TODO |
| TOWER_SYSTEM_CONTEXT.md | 4.0K | 컴포넌트 문서 | 📋 명세/게임시스템 |
| unit-system-spec.md | 49K | 기술 명세 | 📋 명세/게임시스템 |

---

## 제안하는 문서 분류 체계

### 1. 📋 명세 문서 (Specifications)
**목적**: 시스템 동작, 요구사항, 기술 세부사항 정의

#### 1.1 게임 시스템 명세
```
specs/game-systems/
├── simulation-spec.md           (12K) - 유닛 행동, 전투 로직, 경로찾기
├── unit-system-spec.md          (49K) - 상세 유닛 메커닉, 특수 능력
├── tower-system-spec.md         (4.0K) - 타워 메커닉 (TOWER_SYSTEM_CONTEXT.md 리네임)
└── initial-setup-spec.md        (3.9K) - 게임 초기화 명세
```

#### 1.2 서버/인프라 명세
```
specs/server/
├── multi-session-spec.md        (21K) - 멀티 사용자 세션 관리
└── websocket-protocol-spec.md   (신규) - WebSocket 메시지 프로토콜 정의
```

#### 1.3 에이전트 생성 명세 (새로 추가될 영역)
```
specs/features/          # Planner 에이전트가 생성
├── feature.md
├── bug.md
└── chore.md

specs/apis/              # API Designer 에이전트가 생성
├── new_api_endpoint.md
└── update_api_endpoint.md

specs/tests/             # Tester 에이전트가 생성
├── test-core.md
├── test-server.md
└── test-integration.md

specs/reviews/           # Reviewer 에이전트가 생성
├── code-review.md
├── review.md
└── pull_ticket.md

specs/control/           # Planner + Documenter 에이전트가 관리
├── plan.md
└── document.md
```

---

### 2. 📐 아키텍처 문서 (Architecture)
**목적**: 시스템 설계, 통합 계획, 구조적 의사결정

```
docs/architecture/
├── core-integration-plan.md               (13K) - ReferenceModels 통합
├── reference-models-expansion-plan.md     (31K) - ReferenceModels 확장
└── data-driven-architecture.md            (신규) - 데이터 기반 아키텍처 개요
```

---

### 3. 📚 참조 문서 (Reference)
**목적**: 개발자가 참조하는 가이드, 사용법, API 문서

#### 3.1 개발자 가이드
```
docs/reference/developer/
├── development-guide.md         (8.4K) - 아키텍처, WebSocket, GUI 통합
├── debugging-guide.md           (5.1K) - 디버깅 (session-logging.md 리네임)
└── coding-conventions.md        (신규) - C#/.NET 코딩 규칙
```

#### 3.2 컴포넌트 문서
```
docs/reference/components/
├── sim-studio.md                (19K) - React 대시보드
├── simulator-core.md            (신규) - Core 엔진 API
├── reference-models.md          (신규) - ReferenceModels 사용법
└── websocket-server.md          (신규) - Server API
```

---

### 4. 🔧 프로세스 문서 (Process)
**목적**: 개발 프로세스, 워크플로우, 마이그레이션 계획

```
docs/process/
├── development-milestone.md              (50K) - 5단계 로드맵
├── agentic-migration-plan-ko.md          (26K) - 에이전트 환경 마이그레이션
├── agentic-comparison-summary-ko.md      (15K) - 빠른 비교 참조
└── agentic-workflow.md                   (신규) - 에이전트 워크플로우 가이드
```

---

### 5. 🧪 테스팅 문서 (Testing)
**목적**: 테스트 전략, 테스트 계획, 품질 보증

```
docs/testing/
├── reference-models-testing-plan.md     (18K) - ReferenceModels 테스팅 전략
├── testing-strategy.md                  (신규) - 전체 테스팅 전략
└── test-coverage-report.md              (신규) - 커버리지 리포트 (자동 생성)
```

---

### 6. ✅ 작업 추적 (Task Tracking)
**목적**: TODO, 진행 중인 작업, 완료된 작업

```
docs/tasks/
├── todo_reference-models.md             (4.2K) - ReferenceModels 체크리스트
└── active-tasks.md                      (신규) - 현재 진행 중인 작업
```

---

## Documenter 에이전트 역할 정의

### 책임 범위

#### 1. 문서 분류 및 정리
- 새로 생성된 문서를 적절한 위치로 이동
- 문서 유형에 따라 올바른 디렉토리에 배치
- 중복 또는 구식 문서 식별

#### 2. 문서 동기화
- 코드 변경 시 관련 문서 업데이트 필요성 감지
- specs/ 문서와 실제 구현 간 일치성 확인
- API 변경 시 관련 명세 문서 갱신 알림

#### 3. 문서 품질 관리
- 필수 섹션 누락 여부 확인
- 링크 유효성 검증
- 문서 간 일관성 확인

#### 4. 메타 문서 관리
- `document.md` - 아키텍처 결정 기록 (ADR)
- `CHANGELOG.md` - 변경 이력
- `README.md` - 프로젝트 개요 (루트)

### 소유 문서

```
Documenter 에이전트가 관리하는 문서:
├── specs/control/document.md              # ADR 및 회고
├── docs/process/agentic-workflow.md       # 워크플로우 가이드
├── docs/reference/index.md                # 문서 인덱스
├── docs/testing/test-coverage-report.md   # 자동 생성 리포트
└── CHANGELOG.md                           # 변경 이력
```

### 트리거 조건

1. **커밋 후**: 변경된 코드 분석 → 관련 문서 갱신 필요성 확인
2. **문서 생성 시**: 적절한 위치로 분류 및 이동
3. **주기적 검토**: 문서 간 일관성 및 링크 유효성 검증
4. **마일스톤 완료 시**: ADR 작성 및 회고 문서화

### 허용 스킬

- `sync-docs`: 코드 변경사항 → 문서 갱신
- `classify-docs`: 신규 문서 → 적절한 위치로 분류
- `validate-docs`: 문서 품질 및 일관성 검증
- `generate-changelog`: Git 히스토리 → CHANGELOG 생성

---

## 마이그레이션 전략

### Phase 1: 디렉토리 구조 생성
```bash
mkdir -p docs/architecture
mkdir -p docs/reference/developer
mkdir -p docs/reference/components
mkdir -p docs/process
mkdir -p docs/testing
mkdir -p docs/tasks
mkdir -p specs/game-systems
mkdir -p specs/server
mkdir -p specs/features
mkdir -p specs/apis
mkdir -p specs/tests
mkdir -p specs/reviews
mkdir -p specs/control
```

### Phase 2: 기존 문서 재배치

#### 명세 문서 이동
```bash
mv docs/simulation-spec.md specs/game-systems/
mv docs/unit-system-spec.md specs/game-systems/
mv docs/TOWER_SYSTEM_CONTEXT.md specs/game-systems/tower-system-spec.md
mv docs/initial-setup-spec.md specs/game-systems/
mv docs/multi-session-spec.md specs/server/
```

#### 아키텍처 문서 이동
```bash
mv docs/core-integration-plan.md docs/architecture/
mv docs/reference-models-expansion-plan.md docs/architecture/
```

#### 참조 문서 이동
```bash
mv docs/development-guide.md docs/reference/developer/
mv docs/session-logging.md docs/reference/developer/debugging-guide.md
mv docs/sim-studio.md docs/reference/components/
```

#### 프로세스 문서 이동
```bash
mv docs/development-milestone.md docs/process/
mv docs/agentic-migration-plan-ko.md docs/process/
mv docs/agentic-comparison-summary-ko.md docs/process/
```

#### 테스팅 문서 이동
```bash
mv docs/reference-models-testing-plan.md docs/testing/
```

#### 작업 추적 문서 이동
```bash
mv docs/todo_reference-models.md docs/tasks/
```

### Phase 3: 인덱스 및 메타 문서 생성

#### 문서 인덱스 생성
```markdown
# docs/reference/index.md

## Unit-Simulator 문서 인덱스

### 빠른 시작
- [개발 가이드](developer/development-guide.md)
- [아키텍처 개요](../architecture/core-integration-plan.md)

### 명세 문서
- [게임 시스템](../../specs/game-systems/)
- [서버/인프라](../../specs/server/)

### 컴포넌트 참조
- [Simulator Core](components/simulator-core.md)
- [WebSocket Server](components/websocket-server.md)
- [Sim Studio UI](components/sim-studio.md)

...
```

#### ADR 템플릿 생성
```markdown
# specs/control/document.md

## 아키텍처 결정 기록 (ADR)

### ADR-001: ReferenceModels 도입
- 날짜: 2025-XX-XX
- 상태: 승인됨
- 컨텍스트: ...
- 결정: ...
- 결과: ...

...
```

### Phase 4: 검증

```bash
# 모든 문서 링크 확인
find docs specs -name "*.md" -exec grep -l "\[.*\](.*\.md)" {} \;

# 누락된 필수 섹션 확인
# (Documenter 에이전트가 자동화)
```

---

## 6개 에이전트 업데이트된 역할

### 전체 에이전트 목록

| # | 에이전트 | 주요 책임 | 관리 영역 | 트리거 |
|---|----------|-----------|-----------|--------|
| 1 | **Planner** | 요구사항 분석 및 계획 | `specs/features/`, `specs/control/plan.md` | `/new-feature` |
| 2 | **API Designer** | WebSocket 프로토콜 설계 | `specs/apis/` | `/new-api` |
| 3 | **Implementer** | C# 코드 구현 | 소스 코드 (Core, Server, Models) | 구현 단계 |
| 4 | **Tester** | xUnit 테스트 생성/실행 | `specs/tests/`, 테스트 코드 | `/run-tests` |
| 5 | **Reviewer** | 코드 리뷰 및 PR 문서 | `specs/reviews/` | `/pre-pr` |
| 6 | **Documenter** | 문서 분류/동기화/품질 관리 | `docs/`, `specs/control/document.md`, `CHANGELOG.md` | 커밋 후, `/sync-docs` |

---

## 다음 단계

1. [ ] 디렉토리 구조 생성
2. [ ] 기존 문서 재배치 (위 명령어 실행)
3. [ ] Documenter 에이전트 정의 파일 작성 (`.claude/agents/documenter.md`)
4. [ ] `sync-docs` 스킬 구현
5. [ ] `classify-docs` 스킬 구현
6. [ ] 문서 인덱스 생성
7. [ ] AGENTS.md 및 CLAUDE.md 업데이트 (6개 에이전트 반영)
8. [ ] 마이그레이션 계획서 업데이트

---

**작성일**: 2026-01-06
**상태**: 제안 - 검토 중
**다음 검토**: Documenter 에이전트 상세 설계 후
