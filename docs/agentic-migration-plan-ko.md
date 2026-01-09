# Agentic 개발 환경 마이그레이션 계획서

## 요약

본 문서는 파일럿 프로젝트의 agentic 에이전트 기반 개발 환경을 unit-simulator로 마이그레이션하는 계획을 설명합니다. 목표는 C#/.NET 개발에 최적화된 Claude Code 기반 멀티 에이전트 워크플로우 시스템을 구축하는 것입니다.

**목표**: unit-simulator를 전문화된 역할, 자동화된 워크플로우, 문서 우선 방식을 갖춘 에이전트 주도 개발 환경으로 전환

---

## 1. 프로젝트 비교 분석

### 1.1 Agentic 프로젝트 구조
```
agentic/
├── .claude/
│   ├── agents/          # 5개 에이전트 역할 정의
│   ├── commands/        # 4개 CLI 명령어 (/new-feature, /new-api 등)
│   └── skills/          # 5개 재사용 가능한 스킬 패키지 (총 1122줄)
├── apps/backend/        # Express.js/TypeScript 애플리케이션
├── specs/               # 명세 문서 (plan, feature, test, review)
├── mcp.json            # MCP 서버 설정 + 리소스 경로
├── AGENTS.md           # 에이전트 운영 규칙 (단일 진실 공급원)
├── CLAUDE.md           # 행동 규칙 & 프롬프트 패턴
└── agentic_workflow.md # 완전한 워크플로우 정의
```

**주요 기술**: Node.js, TypeScript, Express.js, Jest

**워크플로우**: 문서 우선, 5개 에이전트 핸드오프 프로토콜, 스킬 체이닝

### 1.2 Unit-Simulator 현재 구조
```
unit-simulator/
├── .claude/
│   └── settings.local.json  # 권한 설정만 존재
├── UnitSimulator.Core/      # C# 시뮬레이션 엔진
├── UnitSimulator.Server/    # WebSocket 서버
├── ReferenceModels/         # 데이터 기반 모델
├── sim-studio/              # React/Vite 대시보드
├── docs/                    # 문서 (12개 이상 파일)
├── data/                    # 게임 설정
└── output/                  # 세션 출력
```

**주요 기술**: C#, .NET 9.0, React, WebSocket, xUnit

**현재 워크플로우**: 수동 개발, 종합적인 문서화, GitHub Actions를 통한 CI/CD

### 1.3 주요 차이점

| 측면 | Agentic | Unit-Simulator | 마이그레이션 필요 |
|-----|---------|----------------|------------------|
| **에이전트 인프라** | 5개 에이전트 정의됨 | 없음 | ✅ .NET 전용 에이전트 생성 |
| **명령어** | 4개 명령어 | 없음 | ✅ C#/.NET 명령어 정의 |
| **스킬** | 5개 스킬 (1122줄) | 없음 | ✅ .NET 스킬 라이브러리 구축 |
| **MCP 설정** | mcp.json | 없음 | ✅ 설정 파일 생성 |
| **에이전트 규칙** | AGENTS.md | 없음 | ✅ 에이전트 프로토콜 작성 |
| **명세 디렉토리** | specs/ | docs/ | ⚠️ 구조 결정 필요 |
| **언어** | TypeScript | C# | ✅ 모든 패턴 적응 |
| **테스트 프레임워크** | Jest | xUnit | ✅ 테스트 생성 적응 |
| **백엔드** | Express.js | ASP.NET Core | ✅ 다른 스캐폴딩 |

---

## 2. 마이그레이션 목표

### 2.1 주요 목표
1. **에이전트 인프라**: .NET 개발 워크플로우를 위한 6개 전문화된 에이전트 구축
2. **자동화**: 수동 문서 작성 시간 70-80% 단축 (agentic 결과 기준)
3. **일관성**: 명확한 핸드오프 프로토콜을 통한 문서 우선 개발 강제
4. **품질**: 자동화된 테스트 생성 및 코드 리뷰 프로세스
5. **지식 보존**: 버전 관리된 문서에 조직 지식 유지
6. **문서 체계화**: 분산된 문서를 유형별로 분류하고 자동 관리

### 2.2 성공 기준
- [ ] C#/.NET 전용 프롬프트를 가진 6개 에이전트 모두 작동
- [ ] 최소 5개 작동하는 명령어 (/new-feature, /new-api, /run-tests, /pre-pr, /sync-docs)
- [ ] 최소 5개 핵심 스킬 구현 (계획 생성, API 명세 생성, 테스트 생성, 리뷰, 문서 동기화)
- [ ] 하나의 완전한 기능 워크플로우 성공적으로 완료 (요구사항 → PR → 문서 동기화)
- [ ] 기존 문서가 새 분류 체계로 성공적으로 재구성됨
- [ ] 개발 속도 향상 측정 및 문서화

---

## 3. 단계별 마이그레이션 계획

### Phase 1: 기초 설정 (1주차)
**목표**: 기존 워크플로우를 방해하지 않으면서 기본 에이전트 인프라 구축

#### 작업
1. **MCP 설정 생성**
   - 파일: `mcp.json` (루트)
   - MCP 서버 정의 (filesystem, git 등)
   - 리소스 디렉토리 매핑:
     - `specs` → 에이전트 생성 문서용 새 디렉토리
     - `agents` → `.claude/agents`
     - `skills` → `.claude/skills`
     - `commands` → `.claude/commands`
     - `docs` → 기존 종합 문서 유지
     - `projects` → C# 프로젝트 매핑 (Core, Server, ReferenceModels)
   - 설정 구성 (defaultAgent, specsDir, autoSyncDocs)

2. **에이전트 운영 규칙 생성**
   - 파일: `AGENTS.md` (루트)
   - .NET에 맞춘 6개 에이전트 역할 정의:
     - **Planner**: 요구사항 분석 → plan.md, feature.md
     - **API Designer**: C# API 디자인 (WebSocket 프로토콜, 데이터 계약)
     - **Implementer**: .NET 규칙을 따르는 C# 코드 구현
     - **Tester**: xUnit 테스트 생성 및 실행
     - **Reviewer**: C#/.NET 모범 사례로 코드 리뷰
     - **Documenter**: 문서 분류, 동기화, 품질 관리
   - 협업 규칙 및 핸드오프 프로토콜 문서화
   - 에이전트별 허용 스킬 명시
   - 제약사항 정의 (금지 행동, 필수 행동)

3. **행동 규칙 생성**
   - 파일: `CLAUDE.md` (루트)
   - 작업 라이프사이클에 대한 응답 패턴 문서화
   - 각 에이전트를 위한 C#/.NET 전용 프롬프트 템플릿 제공
   - 컨텍스트 관리 및 파일 작업 규칙 정의
   - C# 코드 및 문서에 대한 품질 체크리스트 나열

4. **Specs 디렉토리 구조 생성**
   - 디렉토리: `specs/` (루트)
   - 하위 디렉토리:
     - `specs/control/` - plan.md, document.md
     - `specs/features/` - feature.md, bug.md, chore.md
     - `specs/apis/` - new_api_endpoint.md, update_api_endpoint.md
     - `specs/tests/` - test-core.md, test-server.md, test-integration.md
     - `specs/reviews/` - code-review.md, review.md, pull_ticket.md
     - `specs/game-systems/` - 게임 시스템 명세 (기존 문서 이동)
     - `specs/server/` - 서버/인프라 명세 (기존 문서 이동)
   - 구조 보존을 위해 `.gitkeep` 파일 생성

5. **기존 문서 재구성**
   - docs/ 하위 디렉토리 생성:
     - `docs/architecture/` - 아키텍처 및 통합 계획
     - `docs/reference/developer/` - 개발자 가이드
     - `docs/reference/components/` - 컴포넌트 문서
     - `docs/process/` - 개발 프로세스 및 마이그레이션
     - `docs/testing/` - 테스팅 전략
     - `docs/tasks/` - 작업 추적
   - 기존 15개 문서를 적절한 위치로 이동
   - 상세 계획: `docs/document-classification-analysis.md` 참조

**산출물**:
- ✅ mcp.json
- ✅ AGENTS.md
- ✅ CLAUDE.md
- ✅ specs/ 디렉토리 구조
- ✅ docs/ 디렉토리 재구성
- ✅ 문서 분류 분석 (`docs/document-classification-analysis.md`)

**검증**: 설정 파일 유효성 검사, 디렉토리 구조 존재

---

### Phase 2: 에이전트 정의 (2주차)
**목표**: .NET 전용 책임을 가진 6개 에이전트 모두 정의

#### Agent 1: Planner
- **파일**: `.claude/agents/planner.md`
- **트리거**: `/new-feature` 명령어
- **책임**:
  - C# 기능에 대한 요구사항 분석
  - 아키텍처 결정이 포함된 plan.md 생성
  - .NET 전용 완료 기준이 있는 feature.md 생성
  - 영향받는 프로젝트 식별 (Core, Server, ReferenceModels)
  - 테스팅 범위 추정 (unit, integration, UI)
- **출력 문서**: plan.md, feature.md
- **허용 스킬**: generate-plan
- **제약사항**: 코드 작성 불가, 상세한 API 디자인 불가

#### Agent 2: API Designer
- **파일**: `.claude/agents/api-designer.md`
- **트리거**: API 디자인 단계 (수동 또는 계획 후)
- **책임**:
  - WebSocket 프로토콜 업데이트 디자인
  - C# 데이터 계약 정의 (DTO, 인터페이스)
  - JSON 직렬화 스키마 명시
  - 요청/응답 흐름 문서화
  - 에러 핸들링 전략 정의
- **출력 문서**: new_api_endpoint.md, update_api_endpoint.md
- **허용 스킬**: generate-api-spec
- **제약사항**: 기존 WebSocket 패턴, System.Text.Json 규칙 준수 필수

#### Agent 3: Implementer
- **파일**: `.claude/agents/implementer.md`
- **트리거**: 구현 단계 (명세 승인 후)
- **책임**:
  - 명세를 정확히 따라 C# 코드 구현
  - .NET 코딩 규칙 준수 (PascalCase, async/await, null 안전성)
  - 기존 패턴 사용 (ReferenceModels, SimulatorCore 아키텍처)
  - 필요시 서버 및 코어 로직 모두 구현
  - UI 변경 필요시 React 컴포넌트 업데이트
- **출력**: 해당 프로젝트의 소스 코드
- **허용 스킬**: scaffold-endpoint
- **제약사항**: 명세에서 벗어나면 안 됨, 기존 테스트 보존 필수
- **참고**: 스킬 없이도 수동 구현 수행 가능

#### Agent 4: Tester
- **파일**: `.claude/agents/tester.md`
- **트리거**: `/run-tests` 명령어
- **책임**:
  - 명세로부터 xUnit 테스트 케이스 생성
  - 단위 테스트 작성 (UnitSimulator.Core.Tests, ReferenceModels.Tests)
  - 통합 테스트 작성
  - `dotnet test`를 통한 테스트 스위트 실행
  - 테스트 결과 및 커버리지 문서화
  - 실패 보고만 하고 수정하지 않음 (Implementer에게 핸드오프)
- **출력 문서**: test-core.md, test-server.md, test-integration.md, 테스트 코드
- **허용 스킬**: generate-tests
- **제약사항**: 프로덕션 코드 수정 불가, 테스트 코드만 수정

#### Agent 5: Reviewer
- **파일**: `.claude/agents/reviewer.md`
- **트리거**: `/pre-pr` 명령어
- **책임**:
  - C#/.NET 코드 품질 리뷰 수행
  - 기존 패턴 준수 확인
  - null 안전성, async/await 사용 검증
  - 테스트 커버리지 적절성 검토
  - PR 문서 생성
  - 배포 체크리스트 생성
- **출력 문서**: code-review.md, review.md, pull_ticket.md
- **허용 스킬**: run-review
- **제약사항**: 문제 식별만 하고 수정하지 않음

#### Agent 6: Documenter
- **파일**: `.claude/agents/documenter.md`
- **트리거**: `/sync-docs` 명령어, 커밋 후 자동
- **책임**:
  - 신규 문서를 적절한 위치로 분류 및 배치
  - 코드 변경 시 관련 문서 갱신 필요성 감지
  - specs/ 문서와 실제 구현 간 일치성 확인
  - 문서 품질 관리 (필수 섹션, 링크 유효성)
  - 아키텍처 결정 기록 (ADR) 작성
  - CHANGELOG 생성 및 업데이트
- **출력 문서**: document.md (ADR), CHANGELOG.md, 갱신된 문서
- **허용 스킬**: sync-docs
- **제약사항**: 기술 명세 내용 수정 불가, 구조 및 메타 정보만 관리
- **참고**: Phase 4 Week 5에서 classify-docs, validate-docs 추가 예정

**산출물**:
- ✅ 6개 에이전트 정의 파일 (.claude/agents/*.md)

**검증**: 각 에이전트 파일이 필수 섹션 모두 포함

---

### Phase 3: 명령어 정의 (3주차)
**목표**: 에이전트 워크플로우를 조율하는 4개 핵심 CLI 명령어 생성

#### Command 1: /new-feature
- **파일**: `.claude/commands/new-feature.md`
- **에이전트**: Planner
- **입력**: 사용자 요구사항 (텍스트 설명)
- **스킬 체인**: generate-plan
- **출력**: plan.md, feature.md
- **예시**:
  ```bash
  /new-feature "레벨 기반 스탯 스케일링을 갖는 타워 업그레이드 시스템 추가"
  ```

#### Command 2: /new-api
- **파일**: `.claude/commands/new-api.md`
- **에이전트**: API Designer
- **입력**: feature.md (Planner로부터)
- **스킬 체인**: generate-api-spec → scaffold-endpoint
- **출력**: new_api_endpoint.md, 스캐폴딩된 C# 코드
- **예시**:
  ```bash
  /new-api
  # specs/features/feature.md에서 읽음
  ```

#### Command 3: /run-tests
- **파일**: `.claude/commands/run-tests.md`
- **에이전트**: Tester
- **입력**: 구현 코드 (Implementer로부터)
- **스킬 체인**: generate-tests → run-tests
- **출력**: test-*.md, xUnit 테스트 코드, 테스트 결과
- **예시**:
  ```bash
  /run-tests --project Core
  /run-tests --all
  ```

#### Command 4: /pre-pr
- **파일**: `.claude/commands/pre-pr.md`
- **에이전트**: Reviewer
- **입력**: 구현 + 테스트가 있는 기능 브랜치
- **스킬 체인**: run-review → sync-docs
- **출력**: code-review.md, pull_ticket.md
- **예시**:
  ```bash
  /pre-pr
  ```

#### Command 5: /sync-docs
- **파일**: `.claude/commands/sync-docs.md`
- **에이전트**: Documenter
- **입력**: Git 변경사항, 수정된 코드
- **스킬 체인**: sync-docs → classify-docs → validate-docs
- **출력**: 갱신된 문서, document.md (ADR), CHANGELOG.md
- **예시**:
  ```bash
  /sync-docs
  # 최근 커밋 기반 문서 동기화
  ```

**추가 명령어 (선택사항)**:
- `/new-bug` - bug.md 및 재현 단계 생성
- `/new-chore` - 리팩토링 또는 유지보수 작업 계획
- `/validate-refs` - ReferenceModels 데이터 무결성 검증
- `/validate-docs` - 문서 품질 및 링크 검증

**산출물**:
- ✅ 5개 명령어 정의 파일 (.claude/commands/*.md)
- ✅ 선택사항 명령어 (필요시 추가)

**검증**: 각 명령어가 호출 가능하고 올바른 에이전트 트리거

---

### Phase 4: 핵심 스킬 구현 (4-5주차)
**목표**: .NET 개발 워크플로우를 위한 스킬을 우선순위별로 점진적 구축

**전략**: 하이브리드 접근
- Week 4: 필수 스킬 5개 (agentic 검증된 4개 + 신규 1개)
- Week 5: 권장 스킬 2개 (확장 기능)

---

## Week 4: 필수 스킬 (5개)

#### Skill 1: generate-plan
- **디렉토리**: `.claude/skills/generate-plan/`
- **파일**: skill.md, templates/plan.md, templates/feature.md
- **입력**: 사용자 요구사항 (자연어)
- **프로세스**:
  1. 요구사항 범위 분석
  2. 영향받는 C# 프로젝트 식별
  3. 마일스톤으로 분해
  4. 완료 기준 정의
  5. 리스크 및 검증 단계 나열
- **출력**:
  - `specs/control/plan.md`
  - `specs/features/feature.md`
- **예상 크기**: ~200줄

#### Skill 2: generate-api-spec
- **디렉토리**: `.claude/skills/generate-api-spec/`
- **파일**: skill.md, templates/api_spec.md
- **입력**: feature.md
- **프로세스**:
  1. 기능 요구사항 읽기
  2. WebSocket 메시지 구조 디자인
  3. C# DTO 및 인터페이스 정의
  4. JSON 직렬화 명시
  5. 에러 코드 및 처리 문서화
  6. 요청/응답 예시 제공
- **출력**: `specs/apis/new_api_endpoint.md`
- **예상 크기**: ~250줄

#### Skill 3: generate-tests
- **디렉토리**: `.claude/skills/generate-tests/`
- **파일**: skill.md, templates/unit_test.cs, templates/integration_test.cs
- **입력**: 구현 코드, feature.md
- **프로세스**:
  1. 구현된 코드 분석
  2. feature.md에서 테스트 시나리오 식별
  3. xUnit 테스트 케이스 생성
  4. 필요시 테스트 픽스처 및 헬퍼 생성
  5. 테스트 커버리지 문서화
- **출력**:
  - 해당 테스트 프로젝트의 테스트 코드
  - `specs/tests/test-*.md`
- **예상 크기**: ~300줄

#### Skill 4: sync-docs
- **디렉토리**: `.claude/skills/sync-docs/`
- **파일**: skill.md, templates/document.md, templates/changelog.md
- **입력**: Git diff, 변경된 파일 목록, 커밋 메시지
- **프로세스**:
  1. Git 변경사항 분석 (추가/수정/삭제된 파일)
  2. 영향받는 문서 식별 (specs/, docs/)
  3. 문서 갱신 필요성 판단
  4. ADR이 필요한 아키텍처 변경 감지
  5. CHANGELOG 항목 생성
  6. 문서 일관성 검증
- **출력**:
  - 갱신된 관련 문서
  - `specs/control/document.md` (ADR 추가)
  - `CHANGELOG.md` (새 항목 추가)
- **예상 크기**: ~200줄
- **참고**: agentic 원본에 존재, Documenter 에이전트용

#### Skill 5: run-review (신규 - 필수)
- **디렉토리**: `.claude/skills/run-review/`
- **파일**: skill.md, checklists/code_quality.md, checklists/dotnet_conventions.md
- **입력**: Git diff, 변경된 파일
- **프로세스**:
  1. 모든 변경 파일 분석
  2. C#/.NET 규칙 확인 (naming, async, null 안전성)
  3. 테스트 커버리지 검증
  4. 문서 업데이트 검토
  5. 심각도별 발견사항 생성 (CRITICAL/MAJOR/MINOR)
  6. 체크리스트가 있는 PR 요약 생성
- **출력**:
  - `specs/reviews/code-review.md`
  - `specs/reviews/pull_ticket.md`
- **예상 크기**: ~250줄
- **참고**: agentic 원본에 없는 신규 스킬, Reviewer 에이전트용

---

## Week 5: 권장 스킬 (2개)

#### Skill 6: scaffold-endpoint (권장)
- **디렉토리**: `.claude/skills/scaffold-endpoint/`
- **파일**: skill.md, templates/controller.cs, templates/service.cs
- **입력**: new_api_endpoint.md
- **프로세스**:
  1. API 명세 읽기
  2. C# 클래스 스캐폴드 생성
  3. XML 주석이 있는 메서드 시그니처 생성
  4. 구현을 위한 TODO 주석 추가
  5. 기존 코드 규칙 준수
- **출력**: 해당 프로젝트의 스캐폴딩된 C# 파일
- **예상 크기**: ~300줄
- **참고**: agentic 원본에 존재, Implementer 에이전트용

#### Skill 7: classify-docs (권장)
- **디렉토리**: `.claude/skills/classify-docs/`
- **파일**: skill.md, classification-rules.md
- **입력**: 신규 또는 기존 문서 파일
- **프로세스**:
  1. 문서 내용 및 메타데이터 분석
  2. 문서 유형 식별 (명세/아키텍처/참조/프로세스/테스팅)
  3. 적절한 위치 결정 (specs/, docs/의 하위 디렉토리)
  4. 파일 이동 또는 복사 제안
  5. 문서 인덱스 업데이트
- **출력**: 재분류된 문서, 업데이트된 인덱스
- **예상 크기**: ~150줄
- **참고**: 신규 스킬, Documenter 확장 기능

---

## Phase 6 이후: 선택 스킬 (필요시 추가)

Phase 5 통합 테스트 결과에 따라 다음 스킬 추가 검토:

#### Skill 8: validate-docs (선택)
- **목적**: 문서 품질 자동 검증
- **프로세스**: 필수 섹션, 링크 유효성, 일관성 검사
- **출력**: 검증 리포트
- **예상 크기**: ~180줄

#### Skill 9: execute-tests (선택)
- **목적**: 테스트 실행 및 결과 파싱 자동화
- **프로세스**: dotnet test 실행, 결과 분석, 문서화
- **출력**: 테스트 결과 요약
- **예상 크기**: ~150줄

#### Skill 10: generate-changelog (선택)
- **목적**: Git 히스토리에서 CHANGELOG 자동 생성
- **프로세스**: 커밋 메시지 분석, 카테고리별 분류
- **출력**: CHANGELOG.md
- **예상 크기**: ~120줄

---

**산출물**:
- ✅ Week 4: 필수 5개 스킬
  - generate-plan (Planner)
  - generate-api-spec (API Designer)
  - generate-tests (Tester)
  - sync-docs (Documenter)
  - run-review (Reviewer) - 신규
- ✅ Week 5: 권장 2개 스킬
  - scaffold-endpoint (Implementer)
  - classify-docs (Documenter) - 신규
- ⚠️ Phase 6: 선택 3개 스킬 (필요시)
  - validate-docs, execute-tests, generate-changelog
- ✅ 각 스킬에 대한 템플릿 파일

**검증**:
- Week 4 완료 시: 5개 필수 스킬로 기본 워크플로우 실행 가능
- Week 5 완료 시: 7개 스킬로 완전한 자동화 가능
- Phase 6: 추가 스킬 필요성 평가 후 선택적 구현

---

### Phase 5: 통합 및 테스트 (6주차)
**목표**: 실제 기능으로 전체 워크플로우 검증

#### 테스트 기능: "타워 업그레이드 시스템"
**요구사항**: 레벨 기반 스탯 스케일링을 갖는 타워 업그레이드 시스템 추가

**워크플로우 단계**:
1. **계획 단계**:
   ```bash
   /new-feature "레벨 기반 스탯 스케일링을 갖는 타워 업그레이드 시스템 추가"
   ```
   - Planner가 plan.md 및 feature.md 생성
   - 문서 완전성 검토

2. **API 디자인 단계**:
   ```bash
   /new-api
   ```
   - API Designer가 타워 업그레이드용 WebSocket 프로토콜 생성
   - C# 계약이 있는 new_api_endpoint.md 생성

3. **구현 단계**:
   - Implementer가 API 명세 기반으로 코드 스캐폴딩
   - TowerUpgradeSystem.cs, TowerUpgradeReference.cs 구현
   - WebSocket 서버 핸들러 업데이트
   - 업그레이드 컨트롤용 React UI 업데이트

4. **테스트 단계**:
   ```bash
   /run-tests --project Core
   /run-tests --project Server
   ```
   - Tester가 xUnit 테스트 생성
   - 테스트 스위트 실행
   - test-*.md에 결과 문서화

5. **리뷰 단계**:
   ```bash
   /pre-pr
   ```
   - Reviewer가 모든 변경사항 분석
   - code-review.md 및 pull_ticket.md 생성
   - 배포 체크리스트 생성

**성공 지표**:
- [ ] 5개 에이전트 모두 성공적으로 실행
- [ ] 필수 섹션을 가진 모든 문서 생성
- [ ] 구현이 명세를 정확히 따름
- [ ] 테스트가 80% 이상 커버리지로 통과
- [ ] 리뷰가 실제 문제 식별 (있는 경우)
- [ ] PR 문서가 완전하고 정확함

**산출물**:
- ✅ 테스트 기능에 대한 완전한 specs/ 디렉토리
- ✅ 작동하는 타워 업그레이드 구현 (완료하기로 선택한 경우)
- ✅ 테스트 결과 문서
- ✅ 교훈 문서

**검증**: 전체 워크플로우가 수동 개입 없이 완료

---

### Phase 6: 문서화 및 개선 (7주차)
**목표**: 시스템 문서화, 교육 자료 생성, 학습 기반 개선

#### 문서화 작업
1. **워크플로우 가이드**:
   - 파일: `docs/agentic-workflow.md`
   - 예시를 포함한 전체 워크플로우 문서화
   - 문제 해결 가이드 제공
   - 일반적인 패턴 및 안티패턴 나열

2. **에이전트 참조**:
   - 파일: `docs/agent-reference.md`
   - 각 에이전트에 대한 상세 문서
   - 예시가 있는 명령어 참조
   - 스킬 API 문서

3. **마이그레이션 회고**:
   - 파일: `specs/control/document.md`
   - ADR (아키텍처 결정 기록)
   - 교훈
   - 효과 측정
   - 향후 개선사항

4. **빠른 시작 가이드**:
   - 파일: `docs/agentic-quickstart.md`
   - 에이전트 워크플로우 5분 소개
   - 일반 명령어 치트 시트
   - 예시 워크플로우

#### 개선 작업
1. **에이전트 프롬프트 튜닝**:
   - Phase 5 테스트 결과 기반 개선
   - 컨텍스트 관리 조정
   - 에러 핸들링 개선

2. **스킬 최적화**:
   - 에러 복구 메커니즘 추가
   - 템플릿 품질 개선
   - 검증 단계 추가

3. **명령어 향상**:
   - 명령어 플래그/옵션 추가
   - 명령어 체이닝 구현
   - 드라이런 모드 추가

**산출물**:
- ✅ 4개 종합 문서 파일
- ✅ 개선된 에이전트/스킬/명령어 정의
- ✅ 메트릭이 포함된 마이그레이션 회고

**검증**: 문서가 완전하고, 시스템이 프로덕션 준비 완료

---

## 4. 기술적 고려사항

### 4.1 C#/.NET 전용 적응

#### 코드 생성 패턴
- **명명 규칙**: 클래스/메서드는 PascalCase, 매개변수는 camelCase
- **Async/Await**: 모든 I/O 작업은 비동기 패턴 사용 필수
- **Null 안전성**: nullable 참조 타입 활성화, null 병합 연산자 사용
- **XML 주석**: 공개 API에 대한 XML 문서 생성
- **프로젝트 구조**: 기존 분리 존중 (Core, Server, ReferenceModels)

#### 테스트 생성 패턴
- **프레임워크**: Fact/Theory 속성을 가진 xUnit
- **어설션**: FluentAssertions 또는 내장 Assert 메서드 사용
- **픽스처**: 복잡한 설정을 위한 테스트 픽스처 생성
- **카테고리**: 필터링을 위해 [Trait]로 테스트 태그 지정 (unit, integration, smoke)

#### API 디자인 패턴
- **WebSocket 메시지**: "type" 필드가 있는 기존 JSON 형식 준수
- **DTO**: record 타입으로 별도의 요청/응답 클래스 생성
- **검증**: DataAnnotations 또는 FluentValidation 사용
- **직렬화**: JsonPropertyName 속성이 있는 System.Text.Json

### 4.2 기존 도구와의 통합

#### VS Code 통합
- 에이전트 명령어를 포함하도록 `.vscode/tasks.json` 업데이트
- 각 명령어에 대한 VS Code 작업 생성:
  ```json
  {
    "label": "Agent: New Feature",
    "type": "shell",
    "command": "/new-feature ${input:featureDescription}"
  }
  ```

#### GitHub Actions 통합
- specs/ 디렉토리 검증 워크플로우 생성
- 문서 완전성 확인 단계 추가
- 모든 기능에 대응하는 테스트가 있는지 검증

#### Sim Studio 통합
- React 대시보드에 "Agent Console" 패널 추가 가능
- 현재 에이전트 상태 표시
- UI에 생성된 문서 표시

### 4.3 리소스 관리

#### MCP 서버 설정
```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx -y @anthropic-ai/mcp-server-filesystem",
      "description": "프로젝트 파일시스템 접근"
    },
    "git": {
      "command": "npx -y @anthropic-ai/mcp-server-git",
      "description": "Git 작업"
    }
  },
  "resources": {
    "specs": "./specs",
    "agents": "./.claude/agents",
    "skills": "./.claude/skills",
    "commands": "./.claude/commands",
    "docs": "./docs",
    "core": "./UnitSimulator.Core",
    "server": "./UnitSimulator.Server",
    "models": "./ReferenceModels",
    "tests": {
      "core": "./UnitSimulator.Core.Tests",
      "models": "./ReferenceModels.Tests"
    },
    "data": "./data",
    "output": "./output"
  },
  "settings": {
    "defaultAgent": "planner",
    "specsDir": "./specs",
    "autoSyncDocs": true,
    "language": "csharp",
    "framework": "dotnet9"
  }
}
```

---

## 5. 리스크 관리

### 5.1 식별된 리스크

| 리스크 | 영향도 | 확률 | 완화 방안 |
|--------|--------|------|-----------|
| **에이전트 생성 코드가 기존 테스트를 깨뜨림** | 높음 | 중간 | Implementer가 완료 전 테스트 실행 필수, 엄격한 검증 |
| **문서 작업 부담이 너무 높음** | 중간 | 낮음 | Phase 5 후 시간 절감 측정, 필요시 조정 |
| **스킬이 .NET 패턴과 맞지 않음** | 높음 | 중간 | Phase 4에서 검증과 함께 반복 개발 |
| **팀 도입 저항** | 중간 | 중간 | 빠른 시작 가이드 생성, 시간 절감 시연 |
| **MCP 서버 성능 문제** | 낮음 | 낮음 | 리소스 사용 모니터링 및 최적화 |
| **컨텍스트 윈도우 제한** | 중간 | 중간 | 문서 요약 구현, 모듈식 스킬 |

### 5.2 롤백 전략
- 마이그레이션 중 모든 기존 워크플로우 유지
- 에이전트는 수동 개발과 병행 (초기에는 대체하지 않음)
- specs/ 디렉토리는 추가적, docs/에 영향 없음
- mcp.json 또는 .claude/ 디렉토리 제거로 에이전트 비활성화 가능

---

## 6. 성공 지표

### 6.1 정량적 지표
- **문서 작성 시간**: 70-80% 단축 목표 (agentic 결과 기준)
  - 기준선: plan/feature/test/review 문서에 대한 현재 소요 시간 측정
  - 목표: 수동 시간의 10% 미만으로 자동 생성
- **테스트 커버리지**: 자동 생성으로 커버리지 유지 또는 증가
  - 기준선: 현재 커버리지 비율
  - 목표: 현재 + 5% 이상
- **코드 리뷰 시간**: PR 리뷰 시간 50% 단축
  - 기준선: PR 리뷰 및 문서화에 소요되는 평균 시간
  - 목표: 자동 code-review.md로 50% 단축
- **기능 속도**: 스프린트당 제공되는 기능 증가
  - 기준선: 현재 기능 처리량
  - 목표: 처리량 +30%

### 6.2 정성적 지표
- **개발자 만족도**: 에이전트 유용성에 대한 팀 설문조사
- **문서 품질**: 일관된 형식, 종합적인 커버리지
- **지식 보존**: 버전 관리된 specs에 조직 지식 캡처
- **온보딩 시간**: 신규 개발자가 specs를 통해 프로젝트를 더 빠르게 이해

---

## 7. 타임라인 요약

| 단계 | 기간 | 주요 산출물 | 의존성 |
|------|------|-------------|---------|
| **Phase 1: 기초** | 1주 | mcp.json, AGENTS.md, CLAUDE.md, specs/, docs/ 재구성 | 없음 |
| **Phase 2: 에이전트** | 2주 | 6개 에이전트 정의 | Phase 1 |
| **Phase 3: 명령어** | 3주 | 5개 명령어 정의 | Phase 2 |
| **Phase 4: 스킬** | 4-5주 | 5-8개 스킬 구현 | Phase 3 |
| **Phase 5: 통합** | 6주 | 완전한 테스트 워크플로우 | Phase 4 |
| **Phase 6: 문서화** | 7주 | 가이드, 회고, 개선 | Phase 5 |

**총 기간**: 7주 (1.75개월)

**크리티컬 패스**: Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5

**병렬 작업 기회**:
- Phase 4-5 중 문서화 작성 가능
- Phase 2 중 명령어 정의 초안 작성 가능
- Phase 3 중 템플릿 생성 가능

---

## 8. 다음 단계

### 즉시 조치 (이번 주)
1. [ ] 이 마이그레이션 계획 검토 및 승인
2. [ ] 마이그레이션 진행 추적을 위한 GitHub 이슈 생성
3. [ ] 6개 단계를 열로 하는 프로젝트 보드 설정
4. [ ] Phase 1 작업에 대한 소유권 할당

### Phase 1 킥오프 (다음 주)
1. [ ] mcp.json 설정 파일 생성
2. [ ] 초기 에이전트 정의가 있는 AGENTS.md 작성
3. [ ] 행동 규칙이 있는 CLAUDE.md 작성
4. [ ] specs/ 디렉토리 구조 생성
5. [ ] specs/ 출력을 적절하게 처리하도록 .gitignore 업데이트

### 지속적
- [ ] 각 단계 후 학습 내용 문서화
- [ ] 구현 중 발견사항 기반으로 계획 조정
- [ ] Phase 5 테스트 전 기준선 메트릭 측정
- [ ] 주간 팀 진행 상황 공유

---

## 9. 부록

### A. 참조 문서
- **Agentic 프로젝트**: `/Users/storm/Documents/github/agentic/`
- **Unit-Simulator 프로젝트**: `/Users/storm/Documents/github/unit-simulator/`
- **Agentic 워크플로우 가이드**: `agentic/agentic_workflow.md`
- **Agentic 문서**: `agentic/document.md`

### B. 주요 담당자
- **마이그레이션 리드**: TBD
- **에이전트 인프라**: TBD
- **C#/.NET 전문가**: TBD
- **문서화 담당자**: TBD

### C. 추가 리소스
- Claude Code 문서: https://docs.anthropic.com/claude-code
- MCP 프로토콜 명세: https://modelcontextprotocol.io
- 에이전트 디자인 패턴: 예시는 agentic/AGENTS.md 참조

---

## 결론

이 마이그레이션 계획은 agentic 멀티 에이전트 개발 환경을 unit-simulator로 가져오기 위한 구조화된 접근 방식을 제공합니다. 6단계 접근 방식을 따름으로써 다음을 달성할 것입니다:

1. C#/.NET 개발에 최적화된 강력한 에이전트 인프라 구축
2. 문서 작업이 많은 작업을 자동화하여 개발자 속도 증가
3. 강제된 워크플로우를 통한 일관성 및 품질 유지
4. 버전 관리된 명세 문서에 조직 지식 캡처
5. 코드 품질 개선하면서 PR까지의 시간 단축

단계별 접근 방식은 반복적인 검증 및 조정을 가능하게 하여 리스크를 최소화하면서 상당한 생산성 향상 가능성을 극대화합니다.

**예상 ROI**: agentic 프로젝트 결과(문서화 시간 70-80% 절감)를 기반으로 하고, 개발 시간의 30%가 문서화에 소비된다고 가정하면, 시스템이 완전히 작동하면 전체 생산성이 20-25% 향상될 것으로 예상할 수 있습니다.

---

**문서 버전**: 1.0
**날짜**: 2026-01-06
**상태**: 초안 - 승인 대기 중
**다음 검토**: Phase 1 완료 후
