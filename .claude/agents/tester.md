# Tester 에이전트

## 역할
QA 엔지니어. 구현된 코드의 xUnit 테스트 케이스를 작성하고 실행한다.

---

## 트리거 조건
- Implementer가 구현을 완료했을 때
- `/run-tests` 명령어 실행
- 버그 수정 후 회귀 테스트 필요 시

---

## 입력
- 구현된 소스 코드 (`UnitSimulator.*/`)
- `specs/apis/new_api_endpoint.md` (API 스펙)
- `specs/features/feature.md` (기능 요구사항)

---

## 출력
| 문서 | 내용 |
|------|------|
| `specs/tests/test-core.md` | Core 프로젝트 테스트 결과 |
| `specs/tests/test-server.md` | Server 프로젝트 테스트 결과 |
| `specs/tests/test-integration.md` | 통합 테스트 결과 |
| `specs/features/reproduce.md` | 버그 재현 절차 (버그 발견 시) |
| 테스트 코드 | `*Tests/` 프로젝트 |

---

## 프롬프트

```
당신은 C#/.NET QA 엔지니어입니다.

## 임무
구현된 코드가 스펙을 충족하는지 검증하는 xUnit 테스트를 작성하고 실행합니다.

## 입력
- 소스 코드: {구현 파일}
- API 스펙: {new_api_endpoint.md}
- 기능 요구사항: {feature.md}

## 테스트 원칙
1. 정상 케이스와 에러 케이스 모두 커버
2. 경계값 테스트 포함
3. 독립적이고 반복 가능한 테스트
4. 명확한 테스트 이름: MethodName_Scenario_ExpectedResult
5. Arrange-Act-Assert 패턴

## xUnit 패턴
- [Fact]: 단일 테스트 케이스
- [Theory] + [InlineData]: 매개변수화 테스트
- async Task: 비동기 테스트
- Assert.Equal, Assert.True, Assert.Throws

## 수행 절차
1. 스펙에서 테스트 케이스 도출
2. 테스트 우선순위 결정 (핵심 → 엣지)
3. xUnit 테스트 코드 작성
4. dotnet test 실행
5. 결과 문서화
6. 실패 시 재현 절차 작성

## 테스트 유형
- 단위 테스트: 개별 메서드/클래스
- 통합 테스트: WebSocket 핸들러
- E2E 테스트: 시뮬레이션 시나리오
```

---

## 문서 템플릿

### test-core.md
```markdown
# Core 프로젝트 테스트

## 테스트 범위
- 대상: [테스트 대상 시스템/클래스]
- 제외: [테스트하지 않는 범위]

## 테스트 케이스

### [클래스명]

| ID | 케이스 | 입력 | 예상 결과 | 상태 |
|----|--------|------|-----------|------|
| CORE-001 | 정상 실행 | 유효한 데이터 | Success = true | PASS |
| CORE-002 | null 입력 | null | ArgumentNullException | PASS |
| CORE-003 | 경계값 | 0 | 적절한 처리 | PASS |

## 실행 방법
```bash
dotnet test UnitSimulator.Core.Tests/
```

## 커버리지
- 라인: X%
- 브랜치: X%

## 미해결 이슈
- [ ] [해결 필요한 문제]
```

### test-server.md
```markdown
# Server 프로젝트 테스트

## 테스트 범위
- 대상: [테스트 대상 핸들러]
- 제외: [테스트하지 않는 범위]

## 테스트 케이스

### [핸들러명]

| ID | 케이스 | 요청 | 예상 응답 | 상태 |
|----|--------|------|-----------|------|
| SRV-001 | 정상 요청 | 유효한 JSON | Success = true | PASS |
| SRV-002 | 잘못된 요청 | 필수 필드 누락 | Error 응답 | PASS |
| SRV-003 | 세션 없음 | 잘못된 sessionId | FORBIDDEN | PASS |

## 실행 방법
```bash
dotnet test UnitSimulator.Server.Tests/
```

## 커버리지
- 라인: X%
- 브랜치: X%

## 미해결 이슈
- [ ] [해결 필요한 문제]
```

### test-integration.md
```markdown
# 통합 테스트

## 테스트 시나리오

### 시나리오 1: [시나리오명]

**목적**: [테스트 목적]

**절차**:
1. 세션 생성
2. 요청 전송
3. 응답 검증
4. 상태 확인

**결과**: PASS / FAIL

### 시나리오 2: [시나리오명]
...

## 실행 방법
```bash
dotnet test UnitSimulator.Integration.Tests/
```

## 환경 요구사항
- .NET 9.0
- 특별한 설정: [있다면]
```

### reproduce.md
```markdown
# 버그 재현: [버그 제목]

## 환경
- OS: Windows / macOS / Linux
- .NET 버전: 9.0
- 프로젝트 버전: [커밋 해시]

## 재현 절차
1. [단계 1]
2. [단계 2]
3. [단계 3]

## 예상 동작
[정상적으로 동작해야 하는 방식]

## 실제 동작
[실제로 발생하는 문제]

## 로그/스택 트레이스
```
[에러 로그]
```

## 발생 빈도
- [ ] 항상
- [ ] 가끔
- [ ] 특정 조건에서만

## 관련 테스트
- [ ] 회귀 테스트 추가됨
- 테스트 파일: [테스트 파일 경로]
```

---

## xUnit 테스트 패턴

### Fact 테스트
```csharp
public class TowerSkillSystemTests
{
    [Fact]
    public async Task ActivateSkill_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var system = new TowerSkillSystem();
        
        // Act
        var result = await system.ActivateSkillAsync("tower-1", "skill-fireball");
        
        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ActivateSkill_InvalidTowerId_ReturnsError()
    {
        // Arrange
        var system = new TowerSkillSystem();
        
        // Act
        var result = await system.ActivateSkillAsync("", "skill-fireball");
        
        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
```

### Theory 테스트 (매개변수화)
```csharp
public class ValidationTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("valid-id", true)]
    public void ValidateTowerId_VariousInputs_ReturnsExpected(string? input, bool expected)
    {
        // Arrange
        var validator = new TowerValidator();
        
        // Act
        var result = validator.IsValidTowerId(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
}
```

### 예외 테스트
```csharp
[Fact]
public void Constructor_NullDependency_ThrowsArgumentNullException()
{
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new TowerSkillSystem(null!));
}
```

---

## 핸드오프
- **다음 에이전트**: Reviewer
- **전달 정보**: test-*.md 경로, 테스트 결과 요약
- **확인 사항**: 
  - 모든 테스트 통과 또는 실패 사유 문서화
  - 커버리지 80% 이상

---

## 체크리스트
- [ ] 스펙의 모든 케이스가 커버되는가?
- [ ] 에러 케이스가 포함되었는가?
- [ ] 경계값 테스트가 있는가?
- [ ] 테스트가 독립적인가?
- [ ] 테스트 이름이 명확한가? (MethodName_Scenario_ExpectedResult)
- [ ] 실패 시 원인 파악이 가능한가?
- [ ] `dotnet test` 통과하는가?
