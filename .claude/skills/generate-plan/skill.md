# Skill: generate-plan

요구사항을 분석하여 plan.md와 작업 문서(feature/bug/chore)를 자동 생성한다.

---

## 메타데이터

```yaml
name: generate-plan
version: 1.0.0
agent: planner
trigger: /new-feature, /new-bug, /new-chore
```

---

## 입력

| 파라미터 | 필수 | 설명 |
|----------|------|------|
| requirement | O | 사용자 요구사항 (자연어) |
| type | X | 작업 유형 (feature/bug/chore), 자동 감지 |

---

## 출력

| 파일 | 설명 |
|------|------|
| `specs/control/plan.md` | 프로젝트 계획 (갱신) |
| `specs/features/{type}.md` | 작업 정의서 |

---

## 실행 흐름

```
1. 입력 파싱
   └─ requirement에서 핵심 요구사항 추출

2. 작업 유형 결정
   ├─ "추가", "구현", "만들어" → feature
   ├─ "버그", "오류", "안됨" → bug
   └─ "정리", "리팩터", "업데이트" → chore

3. 기존 plan.md 로드
   └─ 없으면 새로 생성

4. 프로젝트 영향 분석
   ├─ UnitSimulator.Core 변경 필요 여부
   ├─ UnitSimulator.Server 변경 필요 여부
   ├─ ReferenceModels 변경 필요 여부
   └─ sim-studio 변경 필요 여부

5. 작업 분해
   ├─ 요구사항을 세부 작업으로 분해
   ├─ 각 작업에 완료 조건 정의
   └─ 의존성 및 순서 결정

6. 문서 생성
   ├─ plan.md에 마일스톤 추가
   └─ {type}.md 생성

7. 검증
   └─ 필수 섹션 존재 확인
```

---

## 프롬프트

```
## 역할
당신은 C#/.NET 프로젝트 계획 전문가입니다.

## 입력
요구사항: {{requirement}}

## 프로젝트 구조
- UnitSimulator.Core: 순수 시뮬레이션 로직 (게임 엔티티, 전투, 경로찾기)
- UnitSimulator.Server: WebSocket 실시간 서버 (메시지 핸들러)
- ReferenceModels: 데이터 모델 및 참조 데이터
- sim-studio: React/TypeScript GUI

## 작업
1. 요구사항을 분석하여 작업 유형을 결정하세요 (feature/bug/chore)
2. 영향받는 프로젝트를 식별하세요
3. 요구사항을 실행 가능한 세부 작업으로 분해하세요
4. 각 작업에 테스트 가능한 완료 조건을 정의하세요
5. 리스크와 의존성을 식별하세요

## 출력 형식

### plan.md 추가 내용
```markdown
## [작업명]
- 목적: [왜 이 작업을 하는가]
- 범위: [포함/제외 사항]
- 영향 프로젝트: [Core/Server/ReferenceModels/sim-studio]
- 마일스톤:
  1. [ ] [단계1]
  2. [ ] [단계2]
- 리스크: [식별된 리스크]
- 검증: [완료 확인 방법]
```

### {type}.md 내용
[해당 타입의 템플릿에 맞게 작성]
```

---

## 예시

### 입력
```
requirement: "타워가 특수 스킬을 발동할 수 있는 시스템"
```

### 출력

**specs/control/plan.md** (추가)
```markdown
## 타워 스킬 시스템

- 목적: 타워가 자동 공격 외에 특수 스킬을 발동할 수 있게 함
- 범위:
  - 포함: 스킬 발동, 쿨다운, 효과 적용
  - 제외: 스킬 업그레이드, 스킬 연출
- 영향 프로젝트:
  - UnitSimulator.Core: TowerSkillSystem 추가
  - UnitSimulator.Server: TowerSkillHandler 추가
  - ReferenceModels: TowerSkillReference 추가
- 마일스톤:
  1. [ ] WebSocket API 설계
  2. [ ] Core 로직 구현
  3. [ ] Server 핸들러 구현
  4. [ ] xUnit 테스트
- 리스크: 기존 전투 시스템과 상호작용 복잡성
- 검증: 스킬 발동 → 효과 적용 → 쿨다운 확인
```

**specs/features/feature.md**
```markdown
# 기능: 타워 스킬 시스템

## 요구사항
타워가 특수 스킬을 발동할 수 있다. 각 타워는 고유 스킬을 가지며, 스킬은 쿨다운 후 재사용 가능하다.

## 완료 조건
- [ ] C# 클래스 구현됨 (TowerSkillSystem)
- [ ] xUnit 테스트 통과함
- [ ] WebSocket 프로토콜 정의됨 (ActivateTowerSkillRequest/Response)
- [ ] ReferenceModels에 스킬 데이터 추가됨

## 영향받는 프로젝트
- [ ] UnitSimulator.Core: TowerSkillSystem, SkillEffect 클래스
- [ ] UnitSimulator.Server: TowerSkillHandler
- [ ] ReferenceModels: TowerSkillReference, SkillData
- [ ] sim-studio: 스킬 발동 UI (Phase 2)

## C# 클래스 설계
### Core
- `TowerSkillSystem.cs`: 스킬 발동 로직, 쿨다운 관리
- `SkillEffect.cs`: 스킬 효과 정의

### Server
- `TowerSkillHandler.cs`: WebSocket 메시지 핸들러

### Models
- `TowerSkillReference.cs`: 스킬 참조 데이터

## 테스트 계획
- 단위 테스트: TowerSkillSystem.ActivateSkillAsync()
- 통합 테스트: WebSocket 요청/응답
- 시나리오 테스트: 스킬 발동 → 효과 적용 → 쿨다운 확인
```

---

## 에러 처리

| 에러 | 처리 |
|------|------|
| 요구사항 불명확 | 사용자에게 명확화 요청 |
| 기존 plan.md 충돌 | 기존 내용 유지하고 추가 |

---

## 연결

- **이전 스킬**: (없음 - 시작점)
- **다음 스킬**: `generate-api-spec` (WebSocket API 필요 시)
