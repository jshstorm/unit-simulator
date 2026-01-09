# Command: /new-feature

새로운 기능 개발을 시작한다. 요구사항을 분석하여 계획 문서와 기능 정의서를 자동 생성한다.

---

## 사용법

```
/new-feature "기능 설명"
/new-feature           # 대화형 모드
```

---

## 예시

```
/new-feature "타워가 특수 스킬을 발동할 수 있는 시스템"
/new-feature "유닛 사망 시 이벤트 콜백 추가"
/new-feature "시뮬레이션 상태 스냅샷 저장/복원 기능"
```

---

## 실행 흐름

```
1. 요구사항 확인
   ├─ 인자로 전달된 경우 → 바로 진행
   └─ 인자 없는 경우 → 대화형으로 수집

2. Planner 에이전트 활성화
   └─ .claude/agents/planner.md 참조

3. generate-plan 스킬 실행
   ├─ 입력: 요구사항
   └─ 출력: specs/control/plan.md, specs/features/feature.md

4. 영향 분석
   ├─ UnitSimulator.Core 변경 필요 여부
   ├─ UnitSimulator.Server 변경 필요 여부
   ├─ ReferenceModels 변경 필요 여부
   └─ sim-studio 변경 필요 여부

5. 결과 보고
   ├─ 생성된 문서 요약
   └─ 다음 단계 안내 (/new-api)
```

---

## 생성되는 문서

### specs/control/plan.md (갱신)
```markdown
## [기능명]
- 목적: [왜 이 기능이 필요한가]
- 범위: [포함/제외]
- 마일스톤: [단계별 체크리스트]
- 리스크: [예상 리스크]
- 검증: [완료 조건]
```

### specs/features/feature.md (생성)
```markdown
# 기능: [기능명]

## 요구사항
[상세 요구사항]

## 완료 조건
- [ ] C# 클래스 구현됨
- [ ] xUnit 테스트 통과함
- [ ] WebSocket 프로토콜 정의됨

## 영향받는 프로젝트
- [ ] UnitSimulator.Core: [변경 내용]
- [ ] UnitSimulator.Server: [변경 내용]
- [ ] ReferenceModels: [변경 내용]
- [ ] sim-studio: [변경 내용]

## C# 클래스 설계
### Core
- [클래스명]: [설명]

### Server
- [핸들러명]: [설명]

## 테스트 계획
[xUnit 테스트 전략]
```

---

## 연결 명령어

| 순서 | 명령어 | 설명 |
|------|--------|------|
| 현재 | `/new-feature` | 기능 정의 |
| 다음 | `/new-api` | WebSocket API 설계 (API 필요 시) |
| 이후 | `/run-tests` | xUnit 테스트 생성/실행 |
| 마지막 | `/pre-pr` | PR 준비 |

---

## 옵션

| 옵션 | 설명 | 예시 |
|------|------|------|
| --type=bug | 버그 수정 모드 | `/new-feature --type=bug "로그인 실패"` |
| --type=chore | 정비 작업 모드 | `/new-feature --type=chore "의존성 업데이트"` |
| --skip-plan | plan.md 갱신 생략 | `/new-feature --skip-plan "..."` |

---

## 프로젝트별 영향 분석

| 프로젝트 | 확인 항목 |
|----------|-----------|
| UnitSimulator.Core | 게임 로직, 시스템, 엔티티 |
| UnitSimulator.Server | WebSocket 핸들러, 세션 관리 |
| ReferenceModels | 데이터 스키마, 참조 데이터 |
| sim-studio | React UI 컴포넌트 |

---

## 체크리스트

명령어 실행 후 확인:
- [ ] specs/control/plan.md에 마일스톤이 추가되었는가?
- [ ] specs/features/feature.md가 생성되었는가?
- [ ] 요구사항이 명확하게 기술되었는가?
- [ ] 완료 조건이 테스트 가능한가?
- [ ] 영향받는 프로젝트가 식별되었는가?
- [ ] C# 클래스 설계가 포함되었는가?
- [ ] 다음 단계가 안내되었는가?
