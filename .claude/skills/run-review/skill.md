# Skill: run-review

코드 변경사항을 자동 리뷰하고 PR 문서를 생성한다.

---

## 메타데이터

```yaml
name: run-review
version: 1.0.0
agent: reviewer
trigger: /pre-pr
```

---

## 입력

| 파라미터 | 필수 | 설명 |
|----------|------|------|
| branch | X | 리뷰 대상 브랜치 (기본: 현재 브랜치) |
| base | X | 비교 기준 브랜치 (기본: main) |
| feature_path | X | feature.md 경로 (자동 탐지) |

---

## 출력

| 파일 | 설명 |
|------|------|
| `specs/reviews/code-review.md` | 코드 품질 리뷰 결과 |
| `specs/reviews/review.md` | 기능/요구사항 검증 결과 |
| `specs/reviews/pull_ticket.md` | PR 요약 및 체크리스트 |

---

## 실행 흐름

```
1. 변경사항 분석
   ├─ git diff {base}..{branch} 실행
   ├─ 변경된 파일 목록 추출
   └─ 프로젝트별 분류 (Core/Server/Models)

2. 관련 문서 로드
   ├─ specs/features/feature.md
   ├─ specs/apis/new_api_endpoint.md
   └─ specs/tests/test-*.md

3. C#/.NET 규칙 검증
   ├─ 네이밍 규칙 확인
   ├─ nullable 참조 타입 확인
   ├─ async/await 패턴 확인
   └─ 예외 처리 확인

4. 요구사항 매핑
   ├─ feature.md의 완료 조건 추출
   ├─ 구현 여부 확인
   └─ 테스트 커버리지 확인

5. 이슈 식별
   ├─ 심각도 분류 (CRITICAL/MAJOR/MINOR/SUGGESTION)
   └─ 구체적 라인 및 수정 제안

6. 문서 생성
   ├─ code-review.md 생성
   ├─ review.md 생성
   └─ pull_ticket.md 생성

7. 빌드/테스트 결과 통합
   ├─ dotnet build 결과
   └─ dotnet test 결과
```

---

## 프롬프트

```
## 역할
당신은 시니어 C#/.NET 코드 리뷰어입니다.

## 입력
- Git diff: {{변경된 코드}}
- 변경 파일: {{파일 목록}}
- 요구사항: {{feature.md}}
- API 스펙: {{new_api_endpoint.md}}
- 테스트 결과: {{test-*.md}}

## 작업
1. 코드 변경을 꼼꼼히 검토하세요
2. C#/.NET 규칙 준수 여부를 확인하세요
3. 요구사항 충족 여부를 검증하세요
4. 발견된 이슈를 심각도별로 분류하세요
5. 리뷰 문서 3종을 생성하세요

## C#/.NET 리뷰 체크리스트
- [ ] nullable 참조 타입 (#nullable enable)
- [ ] async/await 올바른 사용
- [ ] ConfigureAwait(false) (라이브러리 코드)
- [ ] IDisposable 패턴
- [ ] 예외 처리 적절성
- [ ] LINQ 효율성
- [ ] 네이밍 규칙 (PascalCase/camelCase/_camelCase)
- [ ] XML 문서 주석

## 심각도 기준
- CRITICAL: 보안 취약점, null 참조 예외, 데이터 손실 위험
- MAJOR: 버그, 성능 문제, 비동기 패턴 오류
- MINOR: 스타일, 네이밍, 경미한 코드 스멜
- SUGGESTION: 더 나은 패턴 제안

## 출력 형식
세 개의 문서를 templates에 맞게 작성하세요:
1. code-review.md - 코드 품질 리뷰
2. review.md - 기능 검증 리뷰
3. pull_ticket.md - PR 문서
```

---

## 체크리스트 (checklists/)

### security.md
```markdown
# 보안 체크리스트

- [ ] 입력 검증이 모든 엔드포인트에 있는가?
- [ ] SQL Injection 방지 (파라미터화된 쿼리)?
- [ ] 민감 정보가 로그에 노출되지 않는가?
- [ ] 인증/인가 체크가 올바른가?
- [ ] 암호화가 필요한 데이터가 암호화되었는가?
```

### performance.md
```markdown
# 성능 체크리스트

- [ ] 불필요한 객체 할당이 없는가?
- [ ] LINQ가 효율적으로 사용되었는가? (ToList() 남용 없음)
- [ ] async/await 오버헤드가 적절한가?
- [ ] 적절한 자료구조를 사용했는가? (Dictionary vs List)
- [ ] 캐싱이 필요한 곳에 캐싱이 있는가?
- [ ] N+1 쿼리 문제가 없는가?
```

### csharp-style.md
```markdown
# C# 스타일 체크리스트

## 네이밍
- [ ] 클래스/메서드/속성: PascalCase
- [ ] 로컬 변수/매개변수: camelCase
- [ ] private 필드: _camelCase
- [ ] 상수: PascalCase 또는 UPPER_CASE
- [ ] 인터페이스: I 접두사

## 타입
- [ ] nullable 참조 타입 활성화 (#nullable enable)
- [ ] required 키워드 적절한 사용
- [ ] record vs class 적절한 선택

## 비동기
- [ ] async 메서드명: Async 접미사
- [ ] await 누락 없음
- [ ] ConfigureAwait(false) (라이브러리 코드)
- [ ] 불필요한 async 없음

## 예외
- [ ] catch(Exception) 최소화
- [ ] 빈 catch 블록 없음
- [ ] 적절한 예외 타입 사용
```

---

## 예시

### 입력

```
branch: feature/tower-skill
base: main
```

### 실행

```bash
# 변경사항 추출
git diff main..feature/tower-skill

# 빌드 검증
dotnet build

# 테스트 실행
dotnet test
```

### 출력

**specs/reviews/code-review.md**
```markdown
# 코드 리뷰

## 개요
- **리뷰 대상**: 타워 스킬 시스템 구현
- **리뷰 일시**: 2026-01-09
- **전체 판정**: CHANGES_REQUESTED

## 변경 파일
| 파일 | 변경 | 상태 |
|------|------|------|
| UnitSimulator.Core/Systems/TowerSkillSystem.cs | +85/-0 | 이슈 있음 |
| UnitSimulator.Server/Handlers/TowerSkillHandler.cs | +62/-0 | OK |
| UnitSimulator.Server/Messages/TowerSkillMessages.cs | +45/-0 | OK |

## 발견된 이슈

### MAJOR
#### [CR-001] 잠재적 null 참조
- **파일**: UnitSimulator.Core/Systems/TowerSkillSystem.cs:34
- **내용**: `GetTower()`가 null을 반환할 수 있으나 체크 없이 사용
- **코드**:
  ```csharp
  var tower = _simulator.GetTower(towerId);
  var skill = tower.GetSkill(skillId);  // null 참조 위험
  ```
- **제안**:
  ```csharp
  var tower = _simulator.GetTower(towerId)
      ?? throw new TowerNotFoundException(towerId);
  ```

### MINOR
#### [CR-002] XML 문서 누락
- **파일**: UnitSimulator.Core/Systems/TowerSkillSystem.cs:20
- **내용**: public 메서드에 XML 문서 주석 없음
- **제안**: `<summary>`, `<param>`, `<returns>` 추가

## C#/.NET 규칙 검증
- [x] nullable 참조 타입 활성화
- [x] async/await 올바른 사용
- [ ] IDisposable 패턴 - 해당 없음
- [x] 네이밍 규칙 (PascalCase/camelCase)
- [ ] XML 문서 주석 - 일부 누락

## 잘된 점
- record 타입을 적절히 사용함
- 예외 처리가 세분화됨
- 로깅이 적절히 추가됨

## 결론
MAJOR 이슈 1건 수정 후 승인 가능
```

**specs/reviews/pull_ticket.md**
```markdown
# Pull Request

## 제목
feat: 타워 스킬 시스템 추가

## 변경 요약
타워가 특수 스킬을 발동할 수 있는 시스템을 구현합니다.
스킬 발동, 쿨다운 관리, 효과 적용을 처리합니다.

## 변경 사항
- TowerSkillSystem: 스킬 발동 로직 구현
- TowerSkillHandler: WebSocket 메시지 핸들러 추가
- TowerSkillMessages: 요청/응답 DTO 정의

## 관련 문서
- specs/features/feature.md
- specs/apis/new_api_endpoint.md
- specs/tests/test-core.md

## 테스트 결과
```bash
dotnet test
```
- UnitSimulator.Core.Tests: PASS (12/12)
- UnitSimulator.Server.Tests: PASS (5/5)

## 체크리스트
- [x] 코드가 스펙과 일치함
- [x] 모든 테스트 통과함
- [x] nullable 참조 타입 올바름
- [x] 빌드 경고 없음
- [ ] 문서가 갱신됨
- [x] 보안 이슈 없음
- [x] 성능 이슈 없음

## 영향받는 프로젝트
- [x] UnitSimulator.Core
- [x] UnitSimulator.Server
- [ ] ReferenceModels
- [ ] sim-studio

## 브레이킹 체인지
- [x] 없음

## 롤백 계획
해당 커밋 revert로 롤백 가능
```

---

## 에러 처리

| 에러 | 처리 |
|------|------|
| diff 없음 | "변경사항 없음" 리포트 |
| feature.md 없음 | 경고 출력, 코드 리뷰만 진행 |
| 빌드 실패 | CRITICAL 이슈로 기록 |
| 테스트 실패 | MAJOR 이슈로 기록 |

---

## 통합

### 빌드/테스트 자동 실행

```bash
# 리뷰 전 자동 실행
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

### Git 명령

```bash
# 변경된 파일 목록
git diff --name-only {base}..{branch}

# 전체 diff
git diff {base}..{branch}

# 커밋 목록
git log --oneline {base}..{branch}
```

---

## 연결

- **이전 스킬**: `generate-tests`
- **다음 단계**: PR 생성 및 머지
