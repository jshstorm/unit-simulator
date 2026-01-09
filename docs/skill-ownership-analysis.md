# 스킬 소유권 재배치 분석

## 현황 분석

### Agentic 원본 스킬 소유권

| 스킬 | 소유 에이전트 | 트리거 | 입력 | 출력 |
|------|--------------|--------|------|------|
| `generate-plan` | Planner | `/new-feature`, `/new-bug`, `/new-chore` | 요구사항 | plan.md, feature.md/bug.md/chore.md |
| `generate-api-spec` | API Designer | `/new-api` | feature.md | new_api_endpoint.md |
| `scaffold-endpoint` | Implementer | (자동, API 스펙 완료 후) | new_api_endpoint.md | 소스 코드 스캐폴드 |
| `generate-tests` | Tester | `/run-tests` | 소스 코드, API 스펙 | test-be.md, test-fe.md, 테스트 코드 |
| `sync-docs` | Documenter | (커밋 후 자동), `/sync-docs` | Git diff | 갱신된 문서 |

**누락된 스킬**: `run-review` (Reviewer용)

---

### 현재 마이그레이션 계획서의 스킬 배정

#### Agent 1: Planner
- **허용 스킬**: `generate-plan`
- **상태**: ✅ 올바름

#### Agent 2: API Designer
- **허용 스킬**: `generate-api-spec`, `scaffold-endpoint`
- **문제**: ⚠️ `scaffold-endpoint`는 Implementer 소유
- **수정 필요**: `scaffold-endpoint` 제거

#### Agent 3: Implementer
- **허용 스킬**: `scaffold-endpoint`, `implement-feature`
- **문제**: ⚠️ `implement-feature`는 실제 스킬이 아님 (임의로 추가한 이름)
- **수정 필요**: `implement-feature` 제거 또는 실제 스킬로 정의

#### Agent 4: Tester
- **허용 스킬**: `generate-tests`, `run-tests`
- **문제**: ⚠️ `run-tests`는 명령어 이름이지 스킬이 아님
- **수정 필요**: `run-tests` 제거 (또는 별도 스킬로 정의)

#### Agent 5: Reviewer
- **허용 스킬**: `run-review`
- **문제**: ⚠️ agentic 원본에 없는 스킬 (계획서에만 존재)
- **수정 필요**: 실제 스킬로 정의 필요

#### Agent 6: Documenter
- **허용 스킬**: `sync-docs`, `classify-docs`, `validate-docs`, `generate-changelog`
- **문제**:
  - ⚠️ `sync-docs` - OK (agentic 원본에 존재)
  - ⚠️ `classify-docs` - 신규 스킬 (정의 필요)
  - ⚠️ `validate-docs` - 신규 스킬 (정의 필요)
  - ⚠️ `generate-changelog` - 신규 스킬 (정의 필요)

---

## 문제점 요약

### 1. 스킬 vs 명령어 혼동
- **문제**: `run-tests`는 명령어인데 스킬처럼 사용됨
- **원인**: 명령어와 스킬의 차이를 명확히 구분하지 못함
- **해결**:
  - 명령어: 사용자가 호출 (`/run-tests`)
  - 스킬: 에이전트가 실행하는 실제 작업 (`generate-tests`)

### 2. 존재하지 않는 스킬 참조
- `implement-feature` - 실제로 구현되지 않음
- `run-review` - agentic 원본에 없음 (하지만 필요함)
- `classify-docs`, `validate-docs`, `generate-changelog` - 신규 제안이지만 미정의

### 3. 잘못된 스킬 소유권
- `scaffold-endpoint`는 Implementer 소유인데 API Designer에도 배정됨

---

## 수정 방안

### 방안 1: Agentic 원본 충실 따르기 (최소 변경)

#### 장점
- agentic 검증된 구조 그대로 사용
- 혼란 최소화

#### 단점
- unit-simulator 특수 요구사항 반영 못 함
- Reviewer용 스킬 없음 → 별도 추가 필요

#### 스킬 소유권 (5개 스킬)

| 에이전트 | 허용 스킬 | 비고 |
|----------|-----------|------|
| Planner | `generate-plan` | |
| API Designer | `generate-api-spec` | `scaffold-endpoint` 제거 |
| Implementer | `scaffold-endpoint` | `implement-feature` 제거 |
| Tester | `generate-tests` | `run-tests` 제거 |
| Reviewer | `run-review` | 신규 추가 필요 |
| Documenter | `sync-docs` | `classify-docs`, `validate-docs`, `generate-changelog` 제거 |

---

### 방안 2: Unit-Simulator 맞춤 확장 (적극 변경)

#### 장점
- unit-simulator 특수 요구사항 반영
- 문서 관리 기능 강화

#### 단점
- agentic 원본에서 이탈
- 더 많은 스킬 구현 필요

#### 스킬 소유권 (8-10개 스킬)

| 에이전트 | 허용 스킬 | 비고 |
|----------|-----------|------|
| Planner | `generate-plan` | |
| API Designer | `generate-api-spec` | |
| Implementer | `scaffold-endpoint` | |
| Tester | `generate-tests`, `execute-tests` | `execute-tests` 신규 (테스트 실행) |
| Reviewer | `run-review`, `analyze-coverage` | 둘 다 신규 |
| Documenter | `sync-docs`, `classify-docs`, `validate-docs`, `generate-changelog` | 3개 신규 |

---

### 방안 3: 하이브리드 (권장) ⭐

#### 개념
- **Phase 1-3**: agentic 원본 충실 (5개 필수 스킬만)
- **Phase 4**: 필요시 확장 스킬 추가 (3-5개)
- **Phase 5-6**: 검증 후 추가 스킬 결정

#### 장점
- 검증된 기반으로 시작
- 점진적 확장 가능
- 리스크 최소화

#### 단점
- 초기에 기능 제한적

#### Phase별 스킬 추가 계획

**Phase 1-3: 필수 스킬 (5개)**
| 에이전트 | 허용 스킬 |
|----------|-----------|
| Planner | `generate-plan` |
| API Designer | `generate-api-spec` |
| Implementer | `scaffold-endpoint` |
| Tester | `generate-tests` |
| Reviewer | `run-review` (신규 구현) |
| Documenter | `sync-docs` |

**Phase 4: 권장 스킬 추가 (2-3개)**
- `classify-docs` (Documenter) - 문서 자동 분류
- `validate-docs` (Documenter) - 문서 품질 검증
- `execute-tests` (Tester) - 테스트 실행 및 결과 파싱

**Phase 5-6: 선택 스킬 (필요시)**
- `generate-changelog` (Documenter) - CHANGELOG 자동 생성
- `analyze-coverage` (Reviewer) - 테스트 커버리지 분석
- `validate-refs` (특수) - ReferenceModels 데이터 검증

---

## 스킬 vs 명령어 vs 에이전트 관계도

```
[사용자]
   ↓ 명령어 입력
[명령어] (/new-feature, /new-api, /run-tests, /pre-pr, /sync-docs)
   ↓ 트리거
[에이전트] (Planner, API Designer, Implementer, Tester, Reviewer, Documenter)
   ↓ 실행
[스킬] (generate-plan, generate-api-spec, scaffold-endpoint, generate-tests, run-review, sync-docs)
   ↓ 생성
[출력] (문서, 코드)
```

### 명확한 구분

| 개념 | 역할 | 예시 | 누가 사용 |
|------|------|------|-----------|
| **명령어** | 사용자 인터페이스 | `/new-feature` | 사용자 |
| **에이전트** | 역할 및 책임 | Planner | Claude Code |
| **스킬** | 구체적 작업 수행 | `generate-plan` | 에이전트 |
| **출력** | 결과물 | plan.md | 시스템 |

---

## 권장 사항 (하이브리드 방안)

### 즉시 수정 (Phase 1-2)

#### 1. Agent 2: API Designer
**현재**:
```yaml
허용 스킬: generate-api-spec, scaffold-endpoint
```

**수정**:
```yaml
허용 스킬: generate-api-spec
```

**이유**: `scaffold-endpoint`는 Implementer의 책임

---

#### 2. Agent 3: Implementer
**현재**:
```yaml
허용 스킬: scaffold-endpoint, implement-feature
```

**수정**:
```yaml
허용 스킬: scaffold-endpoint
```

**이유**: `implement-feature`는 실제 스킬이 아님. Implementer는 수동 구현도 수행하므로 스킬 없이도 작업 가능.

---

#### 3. Agent 4: Tester
**현재**:
```yaml
허용 스킬: generate-tests, run-tests
```

**수정 (옵션 A - 최소)**:
```yaml
허용 스킬: generate-tests
```

**수정 (옵션 B - 확장)**:
```yaml
허용 스킬: generate-tests, execute-tests
```

**이유**:
- `run-tests`는 명령어이지 스킬이 아님
- 테스트 실행 자동화가 필요하면 `execute-tests` 스킬 신규 추가

**권장**: 옵션 A (최소) - Phase 4에서 필요시 `execute-tests` 추가

---

#### 4. Agent 5: Reviewer
**현재**:
```yaml
허용 스킬: run-review
```

**수정**: 변경 없음 (신규 스킬 구현 필요)

**상태**: ✅ OK (Phase 4에서 구현)

---

#### 5. Agent 6: Documenter
**현재**:
```yaml
허용 스킬: sync-docs, classify-docs, validate-docs, generate-changelog
```

**수정 (옵션 A - 최소)**:
```yaml
허용 스킬: sync-docs
```

**수정 (옵션 B - 확장)**:
```yaml
허용 스킬: sync-docs, classify-docs, validate-docs
```

**이유**:
- `sync-docs`만 agentic 원본에 존재
- 나머지는 신규 스킬 (Phase 4 이후 추가)

**권장**: 옵션 A (최소) - Phase 4에서 필요시 추가

---

### Phase별 구현 순서

#### Phase 2: 에이전트 정의
- 6개 에이전트 정의 파일 작성
- **필수 스킬만 명시** (5개)

#### Phase 3: 명령어 정의
- 5개 명령어 정의
- 명령어 → 에이전트 → 스킬 매핑 명확화

#### Phase 4: 스킬 구현
**우선순위 1 (필수 - Week 4)**:
1. `generate-plan` - Planner
2. `generate-api-spec` - API Designer
3. `generate-tests` - Tester
4. `run-review` - Reviewer (신규)
5. `sync-docs` - Documenter

**우선순위 2 (권장 - Week 5)**:
6. `scaffold-endpoint` - Implementer
7. `classify-docs` - Documenter (신규)

**우선순위 3 (선택 - Phase 6)**:
8. `validate-docs` - Documenter (신규)
9. `execute-tests` - Tester (신규)

#### Phase 5: 통합 테스트
- 5개 필수 스킬로 전체 워크플로우 검증
- 부족한 기능 식별

#### Phase 6: 확장 및 개선
- Phase 5 결과 기반으로 추가 스킬 구현
- 문서화 및 최적화

---

## 스킬 체이닝 수정

### Agentic 원본
```
generate-plan → generate-api-spec → scaffold-endpoint → generate-tests → (없음)
```

### Unit-Simulator (수정 후)
```
generate-plan → generate-api-spec → scaffold-endpoint → generate-tests → run-review → sync-docs
```

### 체이닝 세부사항

| 단계 | 스킬 | 입력 | 출력 | 다음 단계 |
|------|------|------|------|-----------|
| 1 | `generate-plan` | 요구사항 | plan.md, feature.md | 2 |
| 2 | `generate-api-spec` | feature.md | new_api_endpoint.md | 3 |
| 3 | `scaffold-endpoint` | new_api_endpoint.md | 소스 코드 스캐폴드 | (수동 구현) |
| 4 | (수동 구현) | 스캐폴드 | 완성된 코드 | 5 |
| 5 | `generate-tests` | 코드, API 스펙 | 테스트 코드, test-*.md | 6 |
| 6 | `run-review` | 코드, 테스트 | code-review.md, pull_ticket.md | 7 |
| 7 | `sync-docs` | Git diff | 갱신된 문서, ADR | 완료 |

---

## 명령어 → 에이전트 → 스킬 매핑 (최종)

| 명령어 | 에이전트 | 스킬 | 출력 |
|--------|----------|------|------|
| `/new-feature` | Planner | `generate-plan` | plan.md, feature.md |
| `/new-api` | API Designer | `generate-api-spec` | new_api_endpoint.md |
| (수동) | Implementer | `scaffold-endpoint` | 코드 스캐폴드 |
| `/run-tests` | Tester | `generate-tests` | 테스트 코드, test-*.md |
| `/pre-pr` | Reviewer | `run-review` | code-review.md, pull_ticket.md |
| `/sync-docs` | Documenter | `sync-docs` | 갱신된 문서, ADR |

---

## 다음 단계

### 즉시 조치
1. [ ] 마이그레이션 계획서의 에이전트 정의 수정
   - API Designer: `scaffold-endpoint` 제거
   - Implementer: `implement-feature` 제거
   - Tester: `run-tests` 제거
   - Documenter: `classify-docs`, `validate-docs`, `generate-changelog` 제거

2. [ ] Phase 4 스킬 구현 순서 명확화
   - 필수 5개 (Week 4)
   - 권장 2개 (Week 5)
   - 선택 2개 (Phase 6)

3. [ ] AGENTS.md 스킬 사용 규칙 섹션 작성
   - 명령어 vs 스킬 구분
   - 스킬 체이닝 규칙
   - 스킬 호출 조건

---

**작성일**: 2026-01-06
**상태**: 분석 완료 - 수정 대기
**다음 단계**: 마이그레이션 계획서 업데이트
