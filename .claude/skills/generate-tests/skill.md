# Skill: generate-tests

구현된 C# 코드와 API 스펙을 기반으로 xUnit 테스트 케이스를 자동 생성한다.

---

## 메타데이터

```yaml
name: generate-tests
version: 1.0.0
agent: tester
trigger: /run-tests
```

---

## 입력

| 파라미터 | 필수 | 설명 |
|----------|------|------|
| target | O | 테스트 대상 (core/server/all) |
| api_spec_path | X | API 스펙 경로 |
| source_path | X | 소스 코드 경로 |

---

## 출력

| 파일 | 설명 |
|------|------|
| `specs/tests/test-core.md` | Core 프로젝트 테스트 문서 |
| `specs/tests/test-server.md` | Server 프로젝트 테스트 문서 |
| `UnitSimulator.Core.Tests/*.cs` | Core xUnit 테스트 코드 |
| `UnitSimulator.Server.Tests/*.cs` | Server xUnit 테스트 코드 |

---

## 실행 흐름

```
1. 대상 분석
   ├─ API 스펙에서 테스트 케이스 도출
   └─ 소스 코드에서 테스트 포인트 식별

2. 테스트 케이스 설계
   ├─ 정상 케이스 (Happy Path)
   ├─ 에러 케이스 (Error Cases)
   ├─ 경계값 (Boundary)
   └─ 엣지 케이스 (Edge Cases)

3. Core 테스트 생성 (target: core/all)
   ├─ 시스템 단위 테스트
   ├─ 로직 테스트
   └─ 테스트 문서 작성

4. Server 테스트 생성 (target: server/all)
   ├─ 핸들러 테스트
   ├─ 메시지 직렬화 테스트
   └─ 테스트 문서 작성

5. 테스트 실행
   └─ dotnet test 실행

6. 결과 기록
   └─ 결과를 문서에 기록
```

---

## 프롬프트

```
## 역할
당신은 C#/.NET QA 엔지니어입니다.

## 입력
- API 스펙: {{new_api_endpoint.md}}
- 소스 코드: {{source files}}

## 작업
1. 각 메서드에 대한 테스트 케이스를 도출하세요
2. 정상 케이스와 에러 케이스를 모두 포함하세요
3. 경계값 테스트를 추가하세요
4. xUnit 테스트 코드를 작성하세요

## 테스트 원칙
- Arrange-Act-Assert 패턴
- 각 테스트는 독립적
- 테스트 이름: MethodName_Scenario_ExpectedResult
- [Fact] 단일 케이스, [Theory] 매개변수화

## 출력
xUnit 테스트 코드 + 테스트 문서
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
        var towerId = "tower-1";
        var skillId = "skill-fireball";
        
        // Act
        var result = await system.ActivateSkillAsync(towerId, skillId);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Cooldown);
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
        Assert.Equal("TowerId is required", result.Error);
    }

    [Fact]
    public async Task ActivateSkill_SkillOnCooldown_ReturnsError()
    {
        // Arrange
        var system = new TowerSkillSystem();
        await system.ActivateSkillAsync("tower-1", "skill-fireball");
        
        // Act
        var result = await system.ActivateSkillAsync("tower-1", "skill-fireball");
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("Skill is on cooldown", result.Error);
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
    [InlineData("tower-1", true)]
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
    var exception = Assert.Throws<ArgumentNullException>(
        () => new TowerSkillSystem(null!));
    
    Assert.Equal("simulator", exception.ParamName);
}
```

### 비동기 테스트
```csharp
[Fact]
public async Task HandleAsync_ValidRequest_ReturnsResponse()
{
    // Arrange
    var handler = new TowerSkillHandler(mockSimulator);
    var request = new ActivateTowerSkillRequest
    {
        TowerId = "tower-1",
        SkillId = "skill-fireball"
    };
    
    // Act
    var response = await handler.HandleActivateTowerSkillAsync(request, mockSession);
    
    // Assert
    Assert.True(response.Success);
}
```

---

## 예시

### 입력
```
target: core
api_spec_path: specs/apis/new_api_endpoint.md
```

### 출력

**specs/tests/test-core.md**
```markdown
# Core 프로젝트 테스트: TowerSkillSystem

## 테스트 범위
- 대상: TowerSkillSystem.ActivateSkillAsync
- 제외: UI 관련, Server 핸들러

## 테스트 케이스

### TowerSkillSystem.ActivateSkillAsync

| ID | 케이스 | 입력 | 예상 | 상태 |
|----|--------|------|------|------|
| CORE-001 | 정상 발동 | 유효한 towerId, skillId | Success=true | - |
| CORE-002 | towerId 빈 값 | "", "skill-1" | Success=false, Error | - |
| CORE-003 | skillId 빈 값 | "tower-1", "" | Success=false, Error | - |
| CORE-004 | 쿨다운 중 | 연속 발동 | Success=false, Error | - |
| CORE-005 | 존재하지 않는 스킬 | "tower-1", "invalid" | Success=false, Error | - |

## 실행 방법
```bash
dotnet test UnitSimulator.Core.Tests/
```

## 커버리지 목표
- 라인: 80% 이상
- 브랜치: 70% 이상
```

**UnitSimulator.Core.Tests/Systems/TowerSkillSystemTests.cs**
```csharp
using System.Threading.Tasks;
using UnitSimulator.Core.Systems;
using Xunit;

namespace UnitSimulator.Core.Tests.Systems;

public class TowerSkillSystemTests
{
    private readonly TowerSkillSystem _sut;

    public TowerSkillSystemTests()
    {
        _sut = new TowerSkillSystem();
    }

    [Fact]
    public async Task ActivateSkillAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var towerId = "tower-1";
        var skillId = "skill-fireball";

        // Act
        var result = await _sut.ActivateSkillAsync(towerId, skillId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Cooldown);
    }

    [Theory]
    [InlineData("", "skill-1", "TowerId is required")]
    [InlineData(null, "skill-1", "TowerId is required")]
    [InlineData("tower-1", "", "SkillId is required")]
    [InlineData("tower-1", null, "SkillId is required")]
    public async Task ActivateSkillAsync_InvalidInput_ReturnsError(
        string? towerId, 
        string? skillId, 
        string expectedError)
    {
        // Act
        var result = await _sut.ActivateSkillAsync(towerId!, skillId!);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public async Task ActivateSkillAsync_SkillOnCooldown_ReturnsError()
    {
        // Arrange
        await _sut.ActivateSkillAsync("tower-1", "skill-fireball");

        // Act
        var result = await _sut.ActivateSkillAsync("tower-1", "skill-fireball");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Skill is on cooldown", result.Error);
    }
}
```

---

## 에러 처리

| 에러 | 처리 |
|------|------|
| 스펙 없음 | 소스 코드만으로 테스트 생성 |
| 소스 없음 | 스펙 기반 테스트만 생성 |
| 테스트 실패 | 실패 내용 문서화 |

---

## 연결

- **이전 스킬**: `scaffold-csharp`
- **다음 스킬**: `run-review` (또는 Reviewer에게 핸드오프)
