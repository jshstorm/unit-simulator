# Command: /pre-pr

코드 리뷰를 수행하고 PR 문서를 준비한다.

---

## 사용법

```
/pre-pr                       # 전체 리뷰 + PR 문서 생성
/pre-pr --review-only         # 코드 리뷰만
/pre-pr --pr-only             # PR 문서만 생성
```

---

## 예시

```
/pre-pr                              # 리뷰 + PR 준비
/pre-pr --review-only                # 코드 리뷰만 실행
/pre-pr --skip-tests                 # 테스트 확인 생략
```

---

## 실행 흐름

```
1. 변경사항 수집
   ├─ Git diff 분석
   ├─ 관련 스펙 문서 로드
   └─ 테스트 결과 확인

2. Reviewer 에이전트 활성화
   └─ .claude/agents/reviewer.md 참조

3. 코드 리뷰 실행
   ├─ C#/.NET 규칙 검증
   ├─ 보안/성능 검토
   └─ 요구사항 충족 확인

4. PR 문서 생성
   ├─ specs/reviews/code-review.md
   ├─ specs/reviews/review.md
   └─ specs/reviews/pull_ticket.md

5. 결과 보고
   ├─ 리뷰 이슈 요약
   ├─ PR 준비 상태
   └─ 다음 단계 안내
```

---

## 생성되는 문서

### specs/reviews/code-review.md
```markdown
# 코드 리뷰

## 개요
- 리뷰 대상: [변경 범위]
- 전체 판정: APPROVED | CHANGES_REQUESTED

## 변경 파일
| 파일 | 변경 | 상태 |
|------|------|------|
| UnitSimulator.Core/... | +50/-10 | OK |

## 발견된 이슈

### CRITICAL
[없음 또는 이슈 목록]

### MAJOR
[이슈 목록]

### MINOR
[이슈 목록]

## C#/.NET 규칙 검증
- [ ] nullable 참조 타입
- [ ] async/await 패턴
- [ ] 네이밍 규칙
```

### specs/reviews/pull_ticket.md
```markdown
# Pull Request

## 제목
[feat/fix/chore]: [설명]

## 변경 요약
[변경 내용]

## 테스트 결과
- UnitSimulator.Core.Tests: PASS
- UnitSimulator.Server.Tests: PASS

## 체크리스트
- [ ] 코드가 스펙과 일치함
- [ ] 모든 테스트 통과함
- [ ] 빌드 경고 없음
```

---

## 연결 명령어

| 순서 | 명령어 | 설명 |
|------|--------|------|
| 이전 | `/run-tests` | 테스트 |
| 현재 | `/pre-pr` | PR 준비 |
| 다음 | PR 생성 | GitHub PR |

---

## 옵션

| 옵션 | 설명 | 기본값 |
|------|------|--------|
| --review-only | 코드 리뷰만 실행 | false |
| --pr-only | PR 문서만 생성 | false |
| --skip-tests | 테스트 확인 생략 | false |

---

## C#/.NET 리뷰 체크리스트

### 필수 확인
- [ ] nullable 참조 타입 올바른가?
- [ ] async/await 패턴이 올바른가?
- [ ] ConfigureAwait(false) 필요한 곳에 있는가?
- [ ] IDisposable 구현이 올바른가?
- [ ] 예외 처리가 적절한가?

### 성능
- [ ] 불필요한 할당이 없는가?
- [ ] LINQ가 효율적으로 사용되었는가?
- [ ] 불필요한 async 오버헤드가 없는가?

### 보안
- [ ] 입력 검증이 있는가?
- [ ] 민감 정보가 노출되지 않는가?

---

## 심각도 분류

| 심각도 | 설명 | 조치 |
|--------|------|------|
| CRITICAL | 보안, 데이터 손실 | 즉시 수정 필수 |
| MAJOR | 버그, 성능 문제 | 머지 전 수정 |
| MINOR | 스타일, 네이밍 | 권장 수정 |
| SUGGESTION | 개선 제안 | 선택적 |

---

## 빌드 확인

```bash
# 빌드
dotnet build

# 테스트
dotnet test

# 경고 확인
dotnet build -warnaserror
```

---

## PR 생성 후

PR 생성 명령어 예시:
```bash
gh pr create --title "[feat]: 타워 스킬 시스템 추가" \
  --body "$(cat specs/reviews/pull_ticket.md)"
```

---

## 체크리스트

명령어 실행 후 확인:
- [ ] 코드 리뷰가 완료되었는가?
- [ ] CRITICAL/MAJOR 이슈가 해결되었는가?
- [ ] PR 문서가 생성되었는가?
- [ ] 테스트가 모두 통과하는가?
- [ ] 빌드에 경고가 없는가?
- [ ] 변경 파일 목록이 정확한가?
- [ ] 영향받는 프로젝트가 명시되었는가?
