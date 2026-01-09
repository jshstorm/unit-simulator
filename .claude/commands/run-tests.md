# Command: /run-tests

xUnit 테스트 케이스를 생성하고 실행한다.

---

## 사용법

```
/run-tests                    # 전체 테스트
/run-tests --target=core      # Core 프로젝트만
/run-tests --target=server    # Server 프로젝트만
/run-tests --generate         # 테스트 코드 생성만
/run-tests --run              # 기존 테스트 실행만
```

---

## 예시

```
/run-tests                          # 테스트 생성 + 실행
/run-tests --target=core            # Core 프로젝트 테스트만
/run-tests --generate               # 테스트 코드만 생성
/run-tests --coverage               # 커버리지 리포트 포함
```

---

## 실행 흐름

```
1. 대상 분석
   ├─ API 스펙 로드 (specs/apis/new_api_endpoint.md)
   └─ 소스 코드 분석 (UnitSimulator.*/)

2. Tester 에이전트 활성화
   └─ .claude/agents/tester.md 참조

3. generate-tests 스킬 실행
   ├─ 테스트 케이스 도출
   ├─ xUnit 테스트 코드 생성
   └─ 테스트 문서 작성

4. 테스트 실행
   ├─ Core: dotnet test UnitSimulator.Core.Tests/
   └─ Server: dotnet test UnitSimulator.Server.Tests/

5. 결과 보고
   ├─ 통과/실패 요약
   ├─ 커버리지 (옵션)
   └─ 실패 시 reproduce.md 생성 안내
```

---

## 생성되는 문서/코드

### specs/tests/test-core.md
```markdown
# Core 프로젝트 테스트

## 테스트 케이스
| ID | 케이스 | 예상 결과 | 상태 |
|----|--------|-----------|------|
| CORE-001 | ... | ... | PASS/FAIL |

## 실행 결과
- 통과: X개
- 실패: Y개
- 커버리지: Z%
```

### specs/tests/test-server.md
```markdown
# Server 프로젝트 테스트

## 테스트 케이스
[테스트 목록]

## 실행 결과
[결과 요약]
```

### xUnit 테스트 코드
```
UnitSimulator.Core.Tests/
└── Systems/
    └── {SystemName}Tests.cs

UnitSimulator.Server.Tests/
└── Handlers/
    └── {Handler}Tests.cs
```

---

## 연결 명령어

| 순서 | 명령어 | 설명 |
|------|--------|------|
| 이전 | `/new-api --scaffold` | 코드 구현 |
| 현재 | `/run-tests` | 테스트 |
| 다음 | `/pre-pr` | PR 준비 |

---

## 옵션

| 옵션 | 설명 | 기본값 |
|------|------|--------|
| --target | 테스트 대상 (core/server/all) | all |
| --generate | 테스트 코드 생성만 | false |
| --run | 기존 테스트 실행만 | false |
| --coverage | 커버리지 리포트 | false |

---

## 테스트 유형

| 유형 | 설명 | 프레임워크 |
|------|------|------------|
| 단위 테스트 | 메서드/클래스 단위 | xUnit |
| 통합 테스트 | WebSocket 핸들러 | xUnit + 모킹 |
| 시나리오 테스트 | 시뮬레이션 시나리오 | xUnit |

---

## xUnit 패턴

### Fact 테스트
```csharp
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
```

### Theory 테스트
```csharp
[Theory]
[InlineData("", false)]
[InlineData("valid-id", true)]
public void ValidateTowerId_VariousInputs_ReturnsExpected(string input, bool expected)
{
    var result = validator.IsValid(input);
    Assert.Equal(expected, result);
}
```

---

## 실행 명령어

```bash
# 전체 테스트
dotnet test

# Core만
dotnet test UnitSimulator.Core.Tests/

# Server만
dotnet test UnitSimulator.Server.Tests/

# 커버리지 포함
dotnet test --collect:"XPlat Code Coverage"
```

---

## 실패 시 처리

테스트 실패 시:
1. 실패 내용을 `specs/tests/test-*.md`에 기록
2. 버그로 판단되면 `specs/features/reproduce.md` 생성 제안
3. `/new-feature --type=bug` 안내

---

## 체크리스트

명령어 실행 후 확인:
- [ ] 테스트 문서가 갱신되었는가?
- [ ] 모든 핸들러가 테스트되었는가?
- [ ] 에러 케이스가 포함되었는가?
- [ ] 경계값 테스트가 있는가?
- [ ] 테스트가 독립적으로 실행 가능한가?
- [ ] 테스트 이름이 명확한가? (MethodName_Scenario_ExpectedResult)
- [ ] 실패한 테스트의 원인이 문서화되었는가?
- [ ] `dotnet test` 통과하는가?
