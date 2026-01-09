# Skill: sync-docs

코드 변경사항을 감지하여 관련 문서를 자동으로 동기화한다.

---

## 메타데이터

```yaml
name: sync-docs
version: 1.0.0
agent: documenter
trigger: (커밋 후 자동), /sync-docs
```

---

## 입력

| 파라미터 | 필수 | 설명 |
|----------|------|------|
| changes | O | 변경된 파일 목록 또는 git diff |
| scope | X | 동기화 범위 (all/api/tests) |

---

## 출력

| 파일 | 조건 |
|------|------|
| `specs/apis/new_api_endpoint.md` | API 코드 변경 시 |
| `specs/apis/update_api_endpoint.md` | 기존 API 변경 시 |
| `specs/tests/test-*.md` | 테스트 코드 변경 시 |
| `specs/control/document.md` | 주요 의사결정 발생 시 |
| `CHANGELOG.md` | 버전 릴리스 시 |

---

## 실행 흐름

```
1. 변경 감지
   ├─ git diff 분석
   └─ 변경 파일 분류

2. 영향 범위 결정
   ├─ UnitSimulator.Core/* → Core 문서
   ├─ UnitSimulator.Server/Handlers/* → API 문서
   ├─ *Tests/* → 테스트 문서
   └─ ReferenceModels/* → 모델 문서

3. 문서별 동기화
   ├─ 기존 문서 로드
   ├─ 변경사항 반영
   └─ 갱신된 문서 저장

4. ADR 작성 판단
   ├─ 새로운 패턴 도입?
   ├─ 아키텍처 변경?
   └─ 주요 의사결정?

5. 변경 요약 생성
   └─ 동기화된 내용 보고

6. 검증
   └─ 문서-코드 일치 확인
```

---

## 프롬프트

```
## 역할
당신은 C#/.NET 기술 문서 작성자입니다.

## 입력
변경된 파일:
{{changes}}

## 작업
1. 변경된 파일이 어떤 문서에 영향을 주는지 파악하세요
2. 해당 문서를 코드와 일치하도록 갱신하세요
3. ADR 작성 필요 여부를 판단하세요
4. 변경 내용을 요약하세요

## 동기화 규칙
- Handler 변경 → API 스펙 문서 갱신
- System 변경 → document.md에 기록
- 테스트 추가/수정 → 테스트 문서 갱신
- ReferenceModels 변경 → 데이터 스키마 문서

## ADR 작성 조건
- 새로운 패턴/기술 도입
- 기존 구조 변경
- 성능/보안 관련 결정
- 의존성 추가/변경

## 출력
갱신된 문서 + 변경 요약
```

---

## 동기화 매핑

| 코드 변경 | 갱신 문서 | 갱신 내용 |
|-----------|-----------|-----------|
| `UnitSimulator.Server/Handlers/*.cs` | `specs/apis/*.md` | WebSocket API 변경 |
| `UnitSimulator.Core/Systems/*.cs` | `specs/control/document.md` | 시스템 로직 변경 |
| `UnitSimulator.Core/*.cs` | `specs/control/document.md` | 게임 엔티티 변경 |
| `*Tests/*.cs` | `specs/tests/test-*.md` | 테스트 케이스 추가 |
| `ReferenceModels/*.cs` | `docs/reference/models.md` | 데이터 스키마 변경 |
| `*.csproj` | `docs/reference/dependencies.md` | 의존성 변경 |

---

## 예시

### 입력
```
changes:
  - UnitSimulator.Server/Handlers/TowerSkillHandler.cs (modified)
  - UnitSimulator.Core/Systems/TowerSkillSystem.cs (modified)
  - UnitSimulator.Core.Tests/Systems/TowerSkillSystemTests.cs (added)
```

### 출력

**동기화 보고:**
```markdown
## 문서 동기화 결과

### 갱신된 문서
1. `specs/apis/new_api_endpoint.md`
   - TowerSkillHandler 변경사항 반영

2. `specs/tests/test-core.md`
   - 새 테스트 케이스 5개 추가

3. `specs/control/document.md`
   - TowerSkillSystem 로직 변경 기록

### 변경 요약
- 타워 스킬 발동 API 응답에 effects 필드 추가
- 쿨다운 로직 개선
- 관련 테스트 케이스 작성 완료
```

**specs/control/document.md 추가 내용:**
```markdown
## 2026-01-09: TowerSkillSystem 개선

### 변경 내용
- `ActivateSkillAsync` 메서드에 효과 적용 결과 반환 추가
- 쿨다운 체크 로직 리팩터링

### 이유
- UI에서 스킬 효과를 시각화하기 위해 효과 목록 필요
- 쿨다운 체크가 여러 곳에서 중복되어 통합

### 영향
- API 응답 구조 변경 (effects 필드 추가)
- 하위 호환성 유지 (기존 필드 유지)

### 관련 코드
- `UnitSimulator.Core/Systems/TowerSkillSystem.cs`
- `UnitSimulator.Server/Handlers/TowerSkillHandler.cs`
```

---

## ADR 템플릿

**docs/architecture/decisions/adr-XXX.md**
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

---

## 자동 감지 패턴

```yaml
# 파일 패턴 → 문서 매핑
patterns:
  - pattern: "UnitSimulator.Server/Handlers/**/*.cs"
    docs: ["specs/apis/new_api_endpoint.md"]

  - pattern: "UnitSimulator.Core/Systems/**/*.cs"
    docs: ["specs/control/document.md"]

  - pattern: "UnitSimulator.Core/*.cs"
    docs: ["specs/control/document.md"]

  - pattern: "*Tests/**/*.cs"
    docs: ["specs/tests/test-core.md", "specs/tests/test-server.md"]

  - pattern: "ReferenceModels/**/*.cs"
    docs: ["docs/reference/models.md"]

  - pattern: "*.csproj"
    docs: ["docs/reference/dependencies.md"]
```

---

## 에러 처리

| 에러 | 처리 |
|------|------|
| 문서 없음 | 새로 생성 |
| 충돌 감지 | 수동 확인 요청 |
| 동기화 실패 | 변경 내용만 보고 |

---

## 연결

- **이전 스킬**: 모든 구현/테스트 스킬
- **다음 스킬**: (없음 - 마무리 단계)
- **트리거**: 커밋 훅 또는 수동 실행
