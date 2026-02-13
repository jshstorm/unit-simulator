# Agent Team Framework - Unit Simulator

**프로젝트**: Unit-Simulator
**엔진**: C#/.NET 9.0 + WebSocket + React/TypeScript
**장르**: RTS 유닛 시뮬레이션
**목표**: 제품 출시 가능한 게임 시뮬레이션 엔진 및 UI
**작성일**: 2026-02-13
**상태**: 초안 (Draft)

---

## 1. 핵심 전제

### 1.1 Claude 에이전트가 할 수 있는 것

| 영역 | 가능한 작업 |
|------|-------------|
| **C# 코드** | Core 시뮬레이션 로직, WebSocket 핸들러, 데이터 모델, 유닛 테스트 |
| **TypeScript/React** | sim-studio UI 컴포넌트, WebSocket 클라이언트, 상태 관리 |
| **데이터 설계** | JSON Schema 정의, 게임 밸런스 데이터, 유닛/타워/스킬 참조 데이터 |
| **빌드/자동화** | dotnet build/test, npm scripts, CI/CD 파이프라인, 데이터 검증 |
| **문서화** | GDD, 아키텍처 결정 기록(ADR), API 명세, 테스트 계획 |
| **Git 워크플로우** | 브랜치 관리, PR 작성, 코드 리뷰, CHANGELOG 관리 |

### 1.2 제한적인 것 (인간 협업 필요)

| 영역 | 제한 사항 | 대응 |
|------|-----------|------|
| **비주얼 에셋** | 스프라이트, 애니메이션, 이펙트 제작 | 인간 아티스트 / 외부 도구 |
| **실시간 플레이테스트** | 게임 플레이 체감 품질 판단 | 인간 QA / 플레이테스터 |
| **게임 밸런스 감각** | 수치 미세 조정의 '재미' 판단 | 인간 게임 디자이너 |
| **UX 감성 판단** | UI/UX의 직관성, 미적 완성도 | 인간 UX 디자이너 |

**원칙**: 코드/로직/자동화는 에이전트가, 창의적 판단과 체감 품질은 인간이 담당

---

## 2. 에이전트 팀 아키텍처

### 2.1 기존 구조 vs 새로운 구조

**기존 (순차 핸드오프)**:
```
Planner → API Designer → Implementer → Tester → Reviewer → Documenter
(직렬, 한 번에 하나의 에이전트만 활성)
```

**새로운 (도메인 기반 팀)**:
```
                    ┌─────────────────────┐
                    │   오케스트레이터      │ ← 전체 상태 추적, 의존성 해결
                    │   (Root CLAUDE.md)   │
                    └──────────┬──────────┘
                               │ 작업 분배 & 우선순위
          ┌────────────────────┼────────────────────┐
          │                    │                     │
   ┌──────▼──────┐     ┌──────▼──────┐      ┌──────▼──────┐
   │  시뮬레이션   │     │   서버/통신   │      │  데이터/모델  │
   │  코어 에이전트 │     │  에이전트     │      │  에이전트     │
   │ (Core)       │     │ (Server)     │      │ (Models)    │
   └──────┬──────┘     └──────┬──────┘      └──────┬──────┘
          │                    │                     │
          │    인터페이스 계약    │    데이터 바인딩      │
          ├────────────────────┤                     │
          │                    │                     │
   ┌──────▼──────┐     ┌──────▼──────┐      ┌──────▼──────┐
   │  UI/클라이언트 │     │  QA/테스트   │      │  DevOps     │
   │  에이전트     │     │  에이전트     │      │  에이전트     │
   │ (sim-studio) │     │ (Tests)     │      │ (CI/CD)     │
   └─────────────┘     └─────────────┘      └─────────────┘
```

### 2.2 핵심 차이점

| 관점 | 기존 (역할 기반) | 새로운 (도메인 기반) |
|------|-----------------|---------------------|
| **에이전트 단위** | 역할 (Planner, Tester 등) | 도메인 (Core, Server, UI 등) |
| **동시 작업** | 불가 (순차 실행) | 가능 (병렬 터미널) |
| **컨텍스트** | 전체 프로젝트 | 도메인별 깊은 컨텍스트 |
| **협업 방식** | 문서 핸드오프 | 인터페이스 계약 + 변경 로그 |
| **확장성** | 에이전트 추가 어려움 | 도메인 추가 용이 |

---

## 3. 프로젝트 디렉토리 구조

```
unit-simulator/
├── CLAUDE.md                              ← 🎯 오케스트레이터 (전체 조율)
│
├── UnitSimulator.Core/                    ← 🎮 시뮬레이션 코어 에이전트
│   ├── CLAUDE.md                          ← 도메인 컨텍스트
│   ├── Units/                             ← 유닛 시스템
│   ├── Towers/                            ← 타워 시스템
│   ├── Combat/                            ← 전투 시스템
│   ├── Pathfinding/                       ← 경로찾기
│   ├── Skills/                            ← 스킬 시스템
│   ├── GameState/                         ← 게임 상태 관리
│   └── Contracts/                         ← 인터페이스 계약 (공유)
│
├── UnitSimulator.Server/                  ← 🌐 서버/통신 에이전트
│   ├── CLAUDE.md                          ← 도메인 컨텍스트
│   ├── Handlers/                          ← WebSocket 메시지 핸들러
│   ├── Messages/                          ← 메시지 타입 정의
│   └── Sessions/                          ← 세션 관리
│
├── ReferenceModels/                       ← 📊 데이터/모델 에이전트
│   ├── CLAUDE.md                          ← 도메인 컨텍스트
│   ├── Models/                            ← 참조 데이터 클래스
│   ├── Infrastructure/                    ← 데이터 로딩
│   └── Validation/                        ← 검증 로직
│
├── sim-studio/                            ← 🖥️ UI/클라이언트 에이전트
│   ├── CLAUDE.md                          ← 도메인 컨텍스트
│   └── src/
│       ├── components/                    ← React 컴포넌트
│       ├── hooks/                         ← 커스텀 훅
│       ├── services/                      ← WebSocket 서비스
│       └── utils/                         ← 유틸리티
│
├── data/                                  ← 📊 데이터/모델 에이전트 (공유)
│   ├── CLAUDE.md                          ← 데이터 관리 컨텍스트
│   ├── schemas/                           ← JSON Schema
│   ├── references/                        ← 게임 데이터
│   └── processed/                         ← 빌드된 데이터
│
├── UnitSimulator.Core.Tests/              ← 🧪 QA 에이전트
│   └── CLAUDE.md                          ← 테스트 컨텍스트
├── ReferenceModels.Tests/                 ← 🧪 QA 에이전트
│
├── .github/workflows/                     ← 🔧 DevOps 에이전트
│   └── CLAUDE.md                          ← CI/CD 컨텍스트
│
├── scripts/                               ← 🔧 DevOps 에이전트
│   └── CLAUDE.md                          ← 자동화 스크립트 컨텍스트
│
├── docs/                                  ← 📝 오케스트레이터 관할
│   ├── agent-team-framework.md            ← 이 문서
│   ├── INDEX.md                           ← 문서 인덱스
│   └── development-milestone.md           ← 마일스톤
│
├── specs/                                 ← 📋 에이전트 간 공유 명세
│   ├── control/                           ← 계획/조율 문서
│   ├── features/                          ← 기능 명세 (공유)
│   ├── apis/                              ← API 명세 (Server ↔ UI 계약)
│   ├── tests/                             ← 테스트 명세
│   └── reviews/                           ← 리뷰 문서
│
├── AGENTS.md                              ← 에이전트 운영 규칙 (갱신 필요)
└── .claude/                               ← 에이전트/스킬/명령어 정의
    ├── agents/                            ← 도메인 에이전트 정의
    ├── skills/                            ← 재사용 가능한 스킬
    └── commands/                          ← 명령어 단축
```

---

## 4. 오케스트레이터 설계

### 4.1 Root CLAUDE.md 역할

오케스트레이터는 프로젝트 루트 `CLAUDE.md`가 담당하며, 다음을 관리합니다:

```markdown
## 오케스트레이터 책임

1. **프로젝트 상태 추적**
   - 현재 개발 단계 (Phase)
   - 각 도메인 에이전트의 진행 상황
   - 블로킹 이슈 및 의존성

2. **작업 분배 및 우선순위**
   - 새로운 기능 요청 → 영향받는 도메인 식별
   - 도메인 간 의존성 순서 결정
   - 병렬 작업 가능 여부 판단

3. **인터페이스 계약 관리**
   - Contracts/ 디렉토리의 공유 인터페이스 변경 조율
   - specs/apis/ 의 WebSocket 프로토콜 변경 승인
   - data/schemas/ 의 스키마 변경 영향 분석

4. **품질 게이트**
   - 빌드 통과 확인 (dotnet build)
   - 전체 테스트 통과 (dotnet test)
   - 데이터 검증 통과 (npm run data:validate)

5. **아키텍처 결정 기록 (ADR)**
   - 도메인 간 영향을 미치는 결정 문서화
   - specs/control/document.md에 ADR 기록
```

### 4.2 오케스트레이터 작동 방식

```
사용자 요구사항: "유닛에 버프/디버프 상태 효과 시스템 추가"
                    │
                    ▼
            ┌───────────────────┐
            │  오케스트레이터      │
            │  영향 분석:         │
            │  ✅ Core (상태 효과) │
            │  ✅ Models (데이터)  │
            │  ✅ Server (API)    │
            │  ✅ UI (시각화)     │
            │  ✅ Tests (검증)    │
            └───────┬───────────┘
                    │
        ┌───────────┼───────────┐
        │           │           │
        ▼           ▼           ▼
   Phase 1      Phase 2      Phase 3
   (병렬)       (병렬)       (병렬)
   ┌─────┐    ┌─────┐    ┌──────┐
   │Models│    │Core │    │Server│
   │데이터 │    │로직  │    │API   │
   │스키마 │    │구현  │    │핸들러 │
   └──┬──┘    └──┬──┘    └──┬───┘
      │          │          │
      └──────────┼──────────┘
                 │
           Phase 4 (병렬)
           ┌─────┬──────┐
           │     │      │
           ▼     ▼      ▼
         UI   Tests  Docs
         시각화 검증   문서화
```

---

## 5. 도메인 에이전트 상세 설계

### 5.1 🎮 시뮬레이션 코어 에이전트

**파일**: `UnitSimulator.Core/CLAUDE.md`

```markdown
# 시뮬레이션 코어 에이전트

## 담당 범위
- 유닛 상태 및 행동 (Unit.cs, SquadBehavior.cs, EnemyBehavior.cs)
- 전투 시스템 (Combat/, 데미지 계산, 타겟팅)
- 타워 시스템 (Towers/, 타워 스킬, 업그레이드)
- 경로찾기 (Pathfinding/, A* 알고리즘)
- 스킬 시스템 (Skills/, 상태 효과)
- 게임 상태 관리 (GameState/, 시뮬레이션 루프)
- 충돌/회피 시스템 (AvoidanceSystem.cs)

## 핵심 원칙
- **결정론적**: 동일 입력 → 동일 결과 (리플레이/동기화 필수)
- **엔진 무관**: 렌더링/입력/사운드 의존성 없음
- **데이터 주도**: 코드 변경 없이 밸런스 조정 가능 (JSON)
- **커맨드 패턴**: 모든 액션은 직렬화 가능한 커맨드

## 코딩 규칙
- C# 9.0+, nullable 참조 타입 활성화
- PascalCase (클래스/메서드), camelCase (변수), _camelCase (private 필드)
- async/await 패턴 (I/O 작업)
- 인터페이스 기반 추상화 (Contracts/ 참조)

## 의존성
- ReferenceModels: 유닛/타워/스킬 데이터 참조
- Contracts/: 다른 모듈과 공유하는 인터페이스

## 수정 금지 영역 (다른 에이전트 소유)
- UnitSimulator.Server/* (서버 에이전트)
- sim-studio/* (UI 에이전트)
- data/schemas/* (데이터 에이전트)

## 현재 작업
[오케스트레이터가 갱신]
```

### 5.2 🌐 서버/통신 에이전트

**파일**: `UnitSimulator.Server/CLAUDE.md`

```markdown
# 서버/통신 에이전트

## 담당 범위
- WebSocket 서버 (WebSocketServer.cs)
- 세션 관리 (SimulationSession.cs, SessionManager.cs)
- 메시지 핸들러 (Handlers/)
- 메시지 타입 정의 (Messages/)
- 웨이브 관리 (WaveManager.cs)
- 렌더링 (Renderer.cs, ImageSharp)
- 세션 로깅 (SessionLogger.cs)

## 핵심 원칙
- 모든 핸들러는 async/await 필수
- WebSocket 메시지는 JSON 직렬화 (System.Text.Json)
- 세션 격리 보장 (멀티 세션 지원)
- 요청/응답 페어링 명확

## WebSocket 메시지 형식
{
  "type": "MessageType",
  "sessionId": "uuid",
  "payload": { }
}

## 의존성
- UnitSimulator.Core: 시뮬레이션 로직 호출
- ReferenceModels: 데이터 모델 참조
- specs/apis/: API 명세 (UI 에이전트와 계약)

## 인터페이스 계약
- sim-studio와의 WebSocket 프로토콜은 specs/apis/*.md에 정의
- 프로토콜 변경 시 오케스트레이터 승인 필요
```

### 5.3 📊 데이터/모델 에이전트

**파일**: `ReferenceModels/CLAUDE.md` + `data/CLAUDE.md`

```markdown
# 데이터/모델 에이전트

## 담당 범위
### ReferenceModels (C#)
- 참조 데이터 클래스 (Models/)
- 데이터 로딩 인프라 (Infrastructure/)
- 데이터 검증 (Validation/)

### data/ (JSON)
- JSON Schema 정의 (schemas/)
- 게임 참조 데이터 (references/)
- 데이터 변환 파이프라인 (scripts/)

## 핵심 원칙
- 불변 데이터 (record 또는 init-only 속성)
- 스키마 변경 시 반드시 검증: npm run data:validate
- 빌드 파이프라인: npm run data:build (normalize + validate + diff)
- 스키마와 C# 모델의 동기화 유지

## 데이터 파이프라인
data/references/*.json (원본)
        ↓ scripts/normalize.js
data/processed/*.json (정규화)
        ↓ ajv validate (스키마 검증)
data/validation/report.md

## 의존성
- Core 에이전트가 참조 (읽기 전용)
- Server 에이전트가 참조 (읽기 전용)
- 스키마 변경 시 Core/Server 에이전트에 영향 알림 필요
```

### 5.4 🖥️ UI/클라이언트 에이전트

**파일**: `sim-studio/CLAUDE.md`

```markdown
# UI/클라이언트 에이전트

## 담당 범위
- React 컴포넌트 (src/components/)
- WebSocket 클라이언트 서비스 (src/services/)
- 커스텀 훅 (src/hooks/)
- 유틸리티 (src/utils/)
- 게임 에셋 표시 (public/assets/)
- Vite 빌드 설정

## 기술 스택
- React 18+ / TypeScript (Strict mode)
- Vite 빌드
- WebSocket 실시간 통신
- Canvas 기반 시뮬레이션 렌더링

## 핵심 원칙
- TypeScript strict mode (as any, @ts-ignore 금지)
- WebSocket 메시지 타입은 specs/apis/*.md와 동기화
- 컴포넌트는 단일 책임 원칙
- 상태 관리는 React hooks 기반

## 의존성
- Server 에이전트와 WebSocket 프로토콜 공유 (specs/apis/)
- 게임 데이터 표시를 위해 data/schemas/ 참조

## 수정 금지 영역
- UnitSimulator.Core/* (코어 에이전트)
- UnitSimulator.Server/* (서버 에이전트)
- data/references/* (데이터 에이전트)
```

### 5.5 🧪 QA/테스트 에이전트

**파일**: `UnitSimulator.Core.Tests/CLAUDE.md`

```markdown
# QA/테스트 에이전트

## 담당 범위
- Core 단위 테스트 (UnitSimulator.Core.Tests/)
- Models 테스트 (ReferenceModels.Tests/)
- 통합 테스트 (WebSocket 시나리오)
- 성능 프로파일링 기본 체크
- 데이터 검증 테스트

## 테스트 프레임워크
- xUnit (C#)
- Arrange-Act-Assert 패턴
- 테스트 네이밍: MethodName_Scenario_ExpectedResult

## 테스트 카테고리
| 카테고리 | 설명 | 실행 |
|----------|------|------|
| Unit | 개별 메서드/클래스 | dotnet test --filter "Category=Unit" |
| Integration | 모듈 간 상호작용 | dotnet test --filter "Category=Integration" |
| Simulation | 시뮬레이션 시나리오 | dotnet test --filter "Category=Simulation" |
| Data | 데이터 검증 | npm run data:validate |

## 현재 테스트 상태
- 73/73 passing (100%)

## 핵심 원칙
- 모든 Core 공개 API에 단위 테스트
- 결정론적 테스트 (동일 입력 → 동일 결과)
- 경계값 테스트 포함
- 커버리지 80% 이상 목표
```

### 5.6 🔧 DevOps 에이전트

**파일**: `scripts/CLAUDE.md` + `.github/workflows/CLAUDE.md`

```markdown
# DevOps/자동화 에이전트

## 담당 범위
- CI/CD 파이프라인 (.github/workflows/)
- 데이터 빌드 스크립트 (scripts/)
- dotnet 빌드/테스트 자동화
- 패키징 및 배포 스크립트

## CI 파이프라인
- dotnet-ci.yml: Build → Test (모든 PR)
- validate-data.yml: 데이터 스키마 검증

## 빌드 명령어
| 명령 | 목적 |
|------|------|
| dotnet build UnitSimulator.sln | 전체 빌드 |
| dotnet test UnitSimulator.sln | 전체 테스트 |
| npm run data:build | 데이터 빌드 (normalize + validate + diff) |
| npm run data:validate | 데이터 스키마 검증만 |
| npm run build --prefix sim-studio | UI 프로덕션 빌드 |

## 핵심 원칙
- 모든 PR에 CI 통과 필수
- 데이터 변경 시 validate-data 파이프라인 실행
- 빌드 실패 시 즉시 알림 및 수정
```

---

## 6. 인터페이스 계약 (Interface Contract)

에이전트 간 충돌을 방지하기 위한 모듈 경계 계약입니다.

### 6.1 계약 위치

```
specs/
├── apis/                    ← Server ↔ UI 계약 (WebSocket 프로토콜)
│   ├── tower-skill-api.md
│   └── [새 API].md
│
├── contracts/               ← 🆕 Core ↔ Server ↔ Models 계약
│   ├── simulation-interface.md   ← Core가 노출하는 API
│   ├── data-access-interface.md  ← Models가 노출하는 API
│   └── message-types.md         ← 공유 메시지 타입 정의
│
└── schemas/                 ← data/schemas/ 와 동기화 (읽기 참조)
```

### 6.2 계약 변경 프로토콜

```
1. 변경 제안자가 specs/contracts/ 또는 specs/apis/ 에 변경안 작성
2. 오케스트레이터가 영향받는 에이전트 식별
3. 영향받는 모든 에이전트가 호환성 확인
4. 합의 후 변경 적용 (동시에 모든 모듈 업데이트)
```

### 6.3 주요 인터페이스

```csharp
// UnitSimulator.Core/Contracts/ — 모든 에이전트가 참조하는 공유 인터페이스
// 이 디렉토리의 파일은 단독 에이전트가 수정하지 않고
// 오케스트레이터 레벨에서 변경 관리

/// <summary>
/// 시뮬레이션 코어가 외부에 노출하는 API
/// Server 에이전트가 이 인터페이스를 통해 Core에 접근
/// </summary>
public interface ISimulationEngine
{
    Task<SimulationResult> RunFrameAsync(SimulationInput input);
    Task<UnitState[]> GetUnitStatesAsync(string sessionId);
    Task<TowerState[]> GetTowerStatesAsync(string sessionId);
    Task<SkillActivationResult> ActivateSkillAsync(string entityId, string skillId);
}

/// <summary>
/// 데이터 접근 계약
/// Core와 Server가 이 인터페이스를 통해 참조 데이터에 접근
/// </summary>
public interface IGameDataProvider
{
    UnitReference GetUnit(string unitId);
    TowerReference GetTower(string towerId);
    SkillReference GetSkill(string skillId);
    WaveDefinition GetWave(int waveNumber);
    GameBalance GetBalance();
}
```

---

## 7. 에이전트 간 커뮤니케이션 패턴

### 7.1 변경 로그 기반 동기화

각 에이전트가 작업 완료 시 변경 로그에 기록 → 다른 에이전트가 최신 상태 파악

```markdown
# docs/AGENT_CHANGELOG.md (새로 도입)

## [2026-02-13]

### Core 에이전트
- Unit.cs에 StatusEffect 상태 추가
- Combat/DamageCalculator.cs에 버프/디버프 적용 로직 추가
- ⚠️ ISimulationEngine 인터페이스 변경 → Server 에이전트 대응 필요

### Data 에이전트
- data/schemas/unit-stats.schema.json에 statusEffects 필드 추가
- data/references/skills.json에 버프/디버프 타입 추가
- ✅ npm run data:validate 통과

### Server 에이전트
- Handlers/StatusEffectHandler.cs 추가
- Messages/StatusEffectMessages.cs 추가
- ✅ ISimulationEngine 변경 대응 완료
```

### 7.2 의존성 알림 (Dependency Alert)

```
Core 에이전트가 인터페이스를 변경할 때:
1. specs/contracts/simulation-interface.md 갱신
2. AGENT_CHANGELOG.md에 ⚠️ 경고 기록
3. 영향받는 에이전트: Server (핸들러), Tests (테스트)

Data 에이전트가 스키마를 변경할 때:
1. data/schemas/*.schema.json 갱신
2. npm run data:validate 실행
3. 영향받는 에이전트: Core (모델), Server (직렬화)
```

### 7.3 실제 병렬 작업 예시

```bash
# 터미널 1: 코어 에이전트
cd unit-simulator/UnitSimulator.Core
claude "유닛에 상태 효과(버프/디버프) 시스템을 추가해줘.
       Skills/StatusEffectSystem.cs로 구현.
       결정론적이어야 하고, 기존 Combat 시스템과 통합."

# 터미널 2: 데이터 에이전트
cd unit-simulator/data
claude "상태 효과를 위한 JSON Schema를 추가해줘.
       data/schemas/에 status-effect.schema.json 생성.
       기존 skill-reference.schema.json에 effectType 필드 추가."

# 터미널 3: QA 에이전트
cd unit-simulator/UnitSimulator.Core.Tests
claude "상태 효과 시스템의 단위 테스트를 작성해줘.
       버프 적용/해제, 디버프 적용/해제, 중첩, 시간 만료 케이스."

# 터미널 4: 오케스트레이터
cd unit-simulator
claude "현재 상태 효과 기능의 각 도메인 진행 상황을 점검하고
       인터페이스 계약 충돌 여부를 분석해줘."
```

---

## 8. 개발 단계별 파이프라인

### Phase 1: Pre-Production (기반 구축)

| 에이전트 | 작업 | 산출물 |
|----------|------|--------|
| 오케스트레이터 | GDD 작성, 아키텍처 결정, ADR 문서화 | docs/GDD.md, specs/control/ |
| 코어 | 프로젝트 구조 생성, 기본 SimulatorCore 세팅 | Core 골격 코드 |
| 데이터 | JSON Schema 정의, 초기 게임 데이터 | schemas/, references/ |
| DevOps | Git 레포 구성, CI 파이프라인 설정 | .github/workflows/ |

**✅ 현재 unit-simulator 상태**: Phase 1 완료

### Phase 2: Core Systems (핵심 시스템)

| 에이전트 | 작업 | 산출물 |
|----------|------|--------|
| 코어 | 유닛/타워/전투/경로찾기 구현 | Core/*.cs |
| 서버 | WebSocket 서버, 세션 관리 | Server/*.cs |
| 데이터 | 유닛/스킬/타워 데이터, 검증 파이프라인 | data/*, Models/*.cs |
| QA | Smoke 테스트, 단위 테스트 프레임워크 | Tests/*.cs |

**✅ 현재 unit-simulator 상태**: Phase 2 대부분 완료 (2.3 런타임 로더 진행 중)

### Phase 3: Production (기능 완성)

| 에이전트 | 작업 | 산출물 |
|----------|------|--------|
| 코어 | 스킬 시스템, 상태 효과, 고급 AI | Core 기능 완성 |
| 서버 | 전체 API 구현, 멀티 세션 | Server 기능 완성 |
| UI | sim-studio 기능 완성, 시뮬레이션 시각화 | sim-studio 완성 |
| 데이터 | 밸런스 데이터, 웨이브 정의 | 게임 데이터 완성 |
| QA | 지속적 테스트, 성능 모니터링 | 테스트 커버리지 80%+ |
| DevOps | 나이틀리 빌드, 자동 배포 | CI/CD 완성 |

### Phase 4: Polish & Release (완성 및 출시)

| 에이전트 | 작업 | 산출물 |
|----------|------|--------|
| QA | 집중 버그 사냥, 성능 최적화 검증 | 버그 리포트, 성능 리포트 |
| DevOps | 프로덕션 빌드, 배포 파이프라인 | 릴리즈 빌드 |
| 오케스트레이터 | 릴리즈 체크리스트, 최종 조율 | 릴리즈 문서 |
| UI | 최종 UI 폴리시, 접근성 | UI 완성 |

---

## 9. 기존 에이전트 시스템과의 통합

### 9.1 역할 에이전트 → 도메인 에이전트 매핑

기존 역할 기반 에이전트는 **스킬**로 전환되어 도메인 에이전트 내에서 호출됩니다:

| 기존 역할 에이전트 | 전환 후 | 사용 위치 |
|-------------------|---------|-----------|
| Planner | `스킬: generate-plan` | 오케스트레이터에서 호출 |
| API Designer | `스킬: generate-api-spec` | 서버 에이전트에서 호출 |
| Implementer | 각 도메인 에이전트에 흡수 | Core/Server/UI 각자 구현 |
| Tester | `스킬: generate-tests` | QA 에이전트에서 호출 |
| Reviewer | `스킬: run-review` | 오케스트레이터에서 호출 |
| Documenter | `스킬: sync-docs` | 오케스트레이터에서 호출 |

### 9.2 명령어 호환

기존 명령어는 유지하되, 도메인 에이전트를 통해 실행:

```
/new-feature "상태 효과 시스템"
  → 오케스트레이터: 영향 분석 → 도메인별 작업 분배

/new-api
  → 서버 에이전트: specs/apis/ 에 WebSocket API 명세 생성

/run-tests
  → QA 에이전트: 전체 테스트 실행 + 리포트

/pre-pr
  → 오케스트레이터: 전체 빌드/테스트 + 코드 리뷰 + PR 문서
```

---

## 10. 실전 팁

### 10.1 컨텍스트 관리가 핵심

각 도메인 에이전트의 `CLAUDE.md`에 해당 도메인의:
- **아키텍처 결정**: 왜 이렇게 설계했는지
- **현재 상태**: 무엇이 완료되고 무엇이 남았는지
- **코딩 컨벤션**: 이 도메인의 패턴과 규칙
- **수정 금지 영역**: 다른 에이전트 소유 코드

을 명확히 유지해야 합니다. 컨텍스트가 없으면 에이전트가 이전 결정과 모순되는 코드를 생성합니다.

### 10.2 변경 로그 자동화

각 에이전트가 작업 완료 시 `docs/AGENT_CHANGELOG.md`에 기록하도록 CLAUDE.md에 명시하면, 다른 에이전트가 최신 변경사항을 파악할 수 있습니다.

### 10.3 인간의 역할

| 인간 역할 | 에이전트 역할 |
|-----------|-------------|
| 게임 디자인 방향 결정 | 디자인 문서 기반 구현 |
| 아트 디렉션 | 코드 레벨 에셋 관리 |
| 플레이테스트 피드백 | 피드백 기반 코드 수정 |
| 최종 릴리즈 승인 | 릴리즈 체크리스트 준비 |
| UX 감성 판단 | UI 컴포넌트 구현 |

### 10.4 주의사항

- **인터페이스 먼저, 구현 나중**: 도메인 간 계약을 먼저 합의한 후 각자 구현
- **작은 단위로 커밋**: 각 에이전트는 작은 단위로 자주 커밋하여 충돌 최소화
- **테스트 먼저 통과**: 어떤 에이전트든 작업 완료 전 `dotnet test` 통과 필수
- **스키마 변경은 신중히**: data/schemas/ 변경은 모든 모듈에 영향, 오케스트레이터 승인 필수

---

## 11. 구현 로드맵

### Step 1: 도메인 CLAUDE.md 생성 (즉시)
- [ ] `UnitSimulator.Core/CLAUDE.md`
- [ ] `UnitSimulator.Server/CLAUDE.md`
- [ ] `ReferenceModels/CLAUDE.md`
- [ ] `sim-studio/CLAUDE.md`
- [ ] `data/CLAUDE.md`
- [ ] `UnitSimulator.Core.Tests/CLAUDE.md`
- [ ] `scripts/CLAUDE.md`

### Step 2: 인터페이스 계약 정의 (1일)
- [ ] `specs/contracts/` 디렉토리 생성
- [ ] `simulation-interface.md` 작성
- [ ] `data-access-interface.md` 작성
- [ ] `message-types.md` 작성

### Step 3: 오케스트레이터 CLAUDE.md 갱신 (1일)
- [ ] Root CLAUDE.md에 오케스트레이터 섹션 추가
- [ ] AGENTS.md를 도메인 기반으로 갱신
- [ ] `docs/AGENT_CHANGELOG.md` 생성

### Step 4: 파일럿 테스트 (2일)
- [ ] 새 기능을 도메인 팀 체제로 개발
- [ ] 병렬 작업 검증
- [ ] 인터페이스 계약 프로토콜 검증

### Step 5: 피드백 및 개선 (지속)
- [ ] 에이전트 CLAUDE.md 튜닝
- [ ] 스킬/명령어 최적화
- [ ] 워크플로우 개선

---

## 참조

- [AGENTS.md](../AGENTS.md) - 에이전트 운영 규칙 (갱신 예정)
- [CLAUDE.md](../CLAUDE.md) - 행동 규칙 및 프롬프트 패턴
- [docs/INDEX.md](INDEX.md) - 문서 인덱스
- [docs/development-milestone.md](development-milestone.md) - 마일스톤
- [docs/process/agentic-workflow.md](process/agentic-workflow.md) - 기존 워크플로우

---

**문서 버전**: 0.1 (초안)
**작성자**: Claude Agent Team
**최종 수정일**: 2026-02-13
**상태**: 초안 - 리뷰 대기
**다음 검토**: Step 1 완료 후
