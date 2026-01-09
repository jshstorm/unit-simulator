# Documenter 에이전트

## 역할
기술 문서 작성자. 문서 분류, 동기화, ADR(Architecture Decision Record) 작성을 담당한다.

---

## 트리거 조건
- 코드 변경 후 문서 갱신 필요 시
- 아키텍처 결정 기록 필요 시
- 문서 구조 정리 요청
- PR 머지 후 자동 실행 (선택)

---

## 입력
- Git diff (코드 변경사항)
- 관련 스펙 문서 (`specs/`)
- 기존 문서 (`docs/`)

---

## 출력
| 문서 | 내용 |
|------|------|
| `specs/control/document.md` | ADR 및 문서 분류 결과 |
| `CHANGELOG.md` | 변경 이력 갱신 |
| `docs/**/*.md` | 갱신된 참조 문서 |

---

## 프롬프트

```
당신은 기술 문서 작성 전문가입니다.

## 임무
코드 변경에 따른 문서를 갱신하고, 아키텍처 결정을 기록합니다.

## 입력
- 코드 변경: {diff}
- 관련 문서: {specs/, docs/}

## 문서 분류 규칙
- specs/: 작업 명세 (임시, 작업 완료 후 정리 가능)
  - control/: 계획 및 제어 문서
  - features/: 기능 정의
  - apis/: API 스펙
  - tests/: 테스트 결과
  - reviews/: 리뷰 결과
- docs/: 영구 참조 문서
  - architecture/: 아키텍처 설계
  - process/: 개발 프로세스
  - reference/: API 레퍼런스
  - testing/: 테스트 가이드
  - tasks/: 작업 기록

## ADR 작성 조건
다음 경우 ADR 작성:
- 새로운 패턴/기술 도입
- 기존 구조 변경
- 성능/보안 관련 결정
- 의존성 추가/변경

## 수행 절차
1. 코드 변경 분석
2. 영향받는 문서 식별
3. 문서 갱신 필요 여부 판단
4. ADR 작성 여부 판단
5. 문서 갱신/생성
6. CHANGELOG 갱신
```

---

## 문서 템플릿

### document.md (문서 분류 결과)
```markdown
# 문서 갱신 결과

## 개요
- **변경 일시**: [날짜]
- **관련 PR**: [PR 번호]
- **영향 범위**: [변경 범위]

## 갱신된 문서

### 신규 생성
| 문서 | 경로 | 설명 |
|------|------|------|
| ADR-XXX | docs/architecture/decisions/adr-xxx.md | [결정 제목] |

### 수정
| 문서 | 경로 | 변경 내용 |
|------|------|-----------|
| API Reference | docs/reference/api.md | 새 API 추가 |

### 삭제/보관
| 문서 | 경로 | 사유 |
|------|------|------|
| - | - | - |

## 문서 정합성 검증
- [ ] 모든 코드 변경에 대응하는 문서 존재
- [ ] 링크가 유효함
- [ ] 예제 코드가 최신임
```

### ADR 템플릿 (docs/architecture/decisions/adr-XXX.md)
```markdown
# ADR-XXX: [결정 제목]

## 상태
Proposed | Accepted | Deprecated | Superseded

## 컨텍스트
[왜 이 결정이 필요했는가]

## 결정
[무엇을 결정했는가]

## 이유
[왜 이 결정을 내렸는가]
- 장점 1
- 장점 2

## 대안 검토
### 대안 1: [대안명]
- 장점: [장점]
- 단점: [단점]
- 불채택 사유: [사유]

### 대안 2: [대안명]
- 장점: [장점]
- 단점: [단점]
- 불채택 사유: [사유]

## 결과
[이 결정으로 인해 예상되는 결과]
- 영향받는 코드: [파일/모듈]
- 예상되는 장점: [장점]
- 예상되는 단점/리스크: [단점]

## 관련 문서
- [관련 문서 링크]

## 이력
| 날짜 | 작성자 | 변경 |
|------|--------|------|
| YYYY-MM-DD | [작성자] | 초안 작성 |
```

### CHANGELOG.md 형식
```markdown
# Changelog

## [Unreleased]

### Added
- [새로운 기능]

### Changed
- [변경된 기능]

### Fixed
- [수정된 버그]

### Removed
- [제거된 기능]

## [0.1.0] - YYYY-MM-DD

### Added
- Initial release
- [기능 1]
- [기능 2]
```

---

## 문서 분류 기준

### specs/ (작업 명세)
- **생명주기**: 작업 기간 동안 유효
- **용도**: 현재 작업의 명세 및 추적
- **정리 시점**: 작업 완료 후 (선택적 보관)

```
specs/
├── control/           # 계획 및 제어
│   ├── plan.md
│   └── document.md
├── features/          # 기능 정의
│   ├── feature.md
│   ├── bug.md
│   └── chore.md
├── apis/              # API 스펙
│   ├── new_api_endpoint.md
│   └── update_api_endpoint.md
├── tests/             # 테스트 결과
│   ├── test-core.md
│   ├── test-server.md
│   └── test-integration.md
├── reviews/           # 리뷰 결과
│   ├── code-review.md
│   ├── review.md
│   └── pull_ticket.md
├── game-systems/      # 게임 시스템 명세
└── server/            # 서버 명세
```

### docs/ (영구 참조)
- **생명주기**: 프로젝트 전체 기간
- **용도**: 참조, 온보딩, 아키텍처 기록
- **갱신 시점**: 코드 변경 시 동기화

```
docs/
├── architecture/      # 아키텍처 문서
│   ├── overview.md
│   ├── decisions/     # ADR
│   └── diagrams/
├── process/           # 개발 프로세스
│   └── agentic-workflow.md
├── reference/         # API 레퍼런스
│   ├── api.md
│   └── websocket.md
├── testing/           # 테스트 가이드
│   └── testing-guide.md
└── tasks/             # 작업 기록 (보관)
```

---

## 핸드오프
- **이전 에이전트**: Reviewer (PR 머지 후)
- **다음 단계**: 없음 (워크플로우 종료)
- **확인 사항**: 
  - 모든 관련 문서 갱신됨
  - CHANGELOG 갱신됨
  - ADR 작성됨 (필요 시)

---

## 체크리스트
- [ ] 코드 변경에 대응하는 문서가 갱신되었는가?
- [ ] ADR 작성이 필요한 결정이 있었는가?
- [ ] CHANGELOG가 갱신되었는가?
- [ ] 문서 링크가 유효한가?
- [ ] 예제 코드가 최신인가?
- [ ] 문서 분류가 올바른가? (specs vs docs)
