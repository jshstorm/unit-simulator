# CLAUDE.md 작성 완료 요약

## 검토 완료

**날짜**: 2026-01-06
**작업**: CLAUDE.md 응답 패턴 및 프롬프트 템플릿 작성
**검토 항목**: 3번 - CLAUDE.md 응답 패턴 추가

---

## 주요 작성 내용

### 1. C#/.NET 전용 기본 원칙 추가

agentic 원본의 3가지 원칙에 **"C#/.NET 우선"** 원칙 추가:

```markdown
### 1.4 C#/.NET 우선
- 모든 코드는 C# 9.0+ 규칙 준수
- .NET 9.0 프레임워크 활용
- async/await 패턴 적극 사용
- nullable 참조 타입 활성화
```

---

### 2. 응답 패턴 확장 (C# 프로젝트 구조 반영)

#### 작업 시작 시 패턴

**추가된 섹션**:
- **영향받는 프로젝트**: 4개 C# 프로젝트 체크리스트
  - UnitSimulator.Core
  - UnitSimulator.Server
  - ReferenceModels
  - sim-studio (React)
- **사용 스킬**: 스킬명 및 목적 명시

**예시**:
```markdown
## 영향받는 프로젝트
- [x] UnitSimulator.Core (TowerUpgradeSystem.cs)
- [x] UnitSimulator.Server (WebSocket 핸들러)
- [x] ReferenceModels (TowerUpgradeReference.cs)
- [x] sim-studio (업그레이드 UI)
```

#### 작업 완료 시 패턴

**추가된 섹션**:
- **검증 결과**: 빌드/테스트 체크리스트
  - `dotnet build` 성공
  - `dotnet test` 통과
  - 문서 완전성 확인

**C# 파일 구조 반영**:
```markdown
### 소스 코드
- UnitSimulator.Core/TowerUpgradeSystem.cs - 핵심 로직
- UnitSimulator.Server/UpgradeHandler.cs - WebSocket 핸들러

### 테스트 코드
- UnitSimulator.Core.Tests/TowerUpgradeSystemTests.cs - 단위 테스트
```

#### 에러 발생 시 패턴

**C# 컴파일 에러 고려**:
```markdown
### 에러 내용
```
error CS0246: The type or namespace name 'TowerReference' could not be found
```

**재현 조건**에 .NET 환경 정보 포함:
- OS
- .NET SDK 버전
- 프로젝트
```

---

### 3. 6개 에이전트 프롬프트 패턴 (Documenter 추가)

| 에이전트 | 주요 추가 내용 |
|----------|----------------|
| **Planner** | C# 네임스페이스 구조 고려, async/await 필요성 판단, ReferenceModels 데이터 스키마 변경 여부 |
| **API Designer** | WebSocket 메시지 형식, C# DTO (record 타입), System.Text.Json 직렬화, JsonPropertyName 속성 |
| **Implementer** | C# 코딩 규칙 (명명/비동기/Null 안전성/에러 처리/XML 주석) 상세 예시 |
| **Tester** | xUnit 테스트 패턴 (Fact/Theory/비동기/픽스처/Trait) 상세 예시 |
| **Reviewer** | C#/.NET 리뷰 체크리스트 (명명/async/null/리소스/LINQ) 상세 예시 |
| **Documenter** | 문서 분류 규칙, ADR 형식, CHANGELOG 형식 (Keep a Changelog) |

---

### 4. C# 코드 예시 (총 20개 이상)

#### Planner 에이전트
```csharp
public class TowerUpgradeSystem
{
    public async Task<TowerUpgradeResult> UpgradeTowerAsync(
        string towerId,
        int targetLevel);
}
```

#### API Designer 에이전트
**WebSocket DTO**:
```csharp
public record UpgradeTowerRequest
{
    [JsonPropertyName("towerId")]
    public required string TowerId { get; init; }

    [JsonPropertyName("targetLevel")]
    [Range(1, 10)]
    public required int TargetLevel { get; init; }
}
```

**WebSocket 핸들러**:
```csharp
public class TowerUpgradeHandler : IMessageHandler
{
    public async Task<object> HandleAsync(
        UpgradeTowerRequest request,
        SimulationSession session)
    {
        // 구현
    }
}
```

#### Implementer 에이전트

**비동기 패턴**:
```csharp
// ✅ Good
public async Task<Result> ProcessAsync()
{
    var data = await _repository.GetDataAsync();
    return await TransformAsync(data);
}
```

**Null 안전성**:
```csharp
#nullable enable

public class Tower
{
    public required string Id { get; init; }
    public string? Description { get; init; }

    // null 검사
    if (tower?.Upgrade is not null)
    {
        // 안전한 접근
    }
}
```

**에러 처리**:
```csharp
public async Task<Result> UpgradeAsync(string id)
{
    try
    {
        return await _service.UpgradeAsync(id);
    }
    catch (ValidationException ex)
    {
        _logger.LogError(ex, "Validation failed for tower {Id}", id);
        throw;
    }
}
```

**XML 문서 주석**:
```csharp
/// <summary>
/// 타워를 지정된 레벨로 업그레이드합니다.
/// </summary>
/// <param name="towerId">업그레이드할 타워의 ID</param>
/// <param name="targetLevel">목표 레벨 (1-10)</param>
/// <returns>업그레이드 결과</returns>
/// <exception cref="InvalidOperationException">
/// 타워를 찾을 수 없거나 레벨이 유효하지 않은 경우
/// </exception>
public async Task<UpgradeResult> UpgradeTowerAsync(
    string towerId,
    int targetLevel)
```

#### Tester 에이전트

**기본 xUnit 테스트**:
```csharp
public class TowerUpgradeSystemTests
{
    [Fact]
    public async Task UpgradeTower_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var system = new TowerUpgradeSystem();
        var towerId = "tower-123";
        var targetLevel = 5;

        // Act
        var result = await system.UpgradeTowerAsync(towerId, targetLevel);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(targetLevel, result.NewLevel);
    }
}
```

**Theory (매개변수화)**:
```csharp
[Theory]
[InlineData(1, true)]
[InlineData(5, true)]
[InlineData(10, true)]
[InlineData(0, false)]   // 경계값: 최소 미만
[InlineData(11, false)]  // 경계값: 최대 초과
public async Task UpgradeTower_VariousLevels_ValidatesCorrectly(
    int targetLevel,
    bool expectedValid)
```

**비동기 테스트**:
```csharp
[Fact]
public async Task UpgradeTower_ConcurrentRequests_HandlesCorrectly()
{
    var tasks = Enumerable.Range(1, 10)
        .Select(i => _system.UpgradeTowerAsync($"tower-{i}", 5));

    var results = await Task.WhenAll(tasks);

    Assert.All(results, r => Assert.True(r.Success));
}
```

**테스트 픽스처**:
```csharp
public class TowerUpgradeSystemTests : IClassFixture<TowerTestFixture>
{
    private readonly TowerTestFixture _fixture;

    public TowerUpgradeSystemTests(TowerTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

**Trait (카테고리)**:
```csharp
[Trait("Category", "Unit")]
public class UnitTests { }

// 실행: dotnet test --filter "Category=Unit"
```

#### Reviewer 에이전트

**명명 규칙**:
```csharp
// ✅ Good
public class TowerUpgradeSystem { }
private readonly ILogger _logger;

// ❌ Bad
public class towerUpgradeSystem { }  // PascalCase 위반
private readonly ILogger logger;  // 언더스코어 누락
```

**Async/Await**:
```csharp
// ✅ Good
return await _repository.GetAsync();

// ❌ Bad
return _repository.GetAsync().Result;  // 데드락 위험
```

**Null 안전성**:
```csharp
// ✅ Good
return tower?.Description;

// ❌ Bad
return tower.Description;  // NullReferenceException 위험
```

**리소스 관리**:
```csharp
// ✅ Good
await using var stream = File.OpenRead(path);
using var client = new HttpClient();
```

**LINQ**:
```csharp
// ✅ Good
var activeTowers = towers
    .Where(t => t.IsActive)
    .OrderBy(t => t.Level)
    .ToList();

// ❌ Bad (성능 이슈)
var activeTowers = towers
    .ToList()
    .Where(t => t.IsActive)
    .ToList()
    .OrderBy(t => t.Level)
    .ToList();
```

#### Documenter 에이전트

**ADR 형식**:
```markdown
## ADR-XXX: [결정 제목]

**날짜**: 2026-01-06
**상태**: 승인됨
**결정자**: [에이전트]

### 컨텍스트
[배경]

### 결정
[무엇을]

### 이유
[왜]

### 결과
[영향]
```

**CHANGELOG 형식**:
```markdown
## [Unreleased]

### Added
- 타워 업그레이드 시스템 (#123)

### Changed
- TowerReference에 UpgradeStats 추가

### Fixed
- 타워 스탯 계산 오류 (#124)
```

---

### 5. 품질 체크리스트 (C#/.NET 전용)

#### C# 코드 작성 시 (5개 카테고리)

**기본**:
- [ ] 스펙 문서와 일치
- [ ] 명명 규칙 준수
- [ ] 에러 처리
- [ ] 테스트 작성
- [ ] 기존 패턴 일관성

**비동기**:
- [ ] I/O는 async/await
- [ ] 메서드명에 Async 접미사
- [ ] ConfigureAwait(false)
- [ ] Task.Run 불필요하게 사용 안 함

**Null 안전성**:
- [ ] nullable 경고 0개
- [ ] 매개변수 null 검사
- [ ] null-conditional 연산자 활용

**성능**:
- [ ] 불필요한 할당 최소화
- [ ] LINQ 적절히 사용
- [ ] StringBuilder 사용

**보안**:
- [ ] 외부 입력 검증
- [ ] SQL 인젝션 방지
- [ ] 민감 정보 로깅 안 함

---

### 6. 금지 사항 (C#/.NET 특화 추가)

**기존 금지 사항** (agentic 원본):
- 사용자 동의 없이 파일 삭제
- 스펙 없이 기능 구현
- 테스트 없이 완료 선언
- 에러 무시
- 하드코딩된 비밀 정보

**C#/.NET 추가 금지 사항**:
- [ ] `#nullable disable` 사용
- [ ] async 메서드에서 `.Result` 또는 `.Wait()` 사용
- [ ] IDisposable 미구현
- [ ] `var` 남용
- [ ] `dynamic` 무분별 사용
- [ ] `goto` 사용
- [ ] 예외를 제어 흐름에 사용

---

### 7. 프로젝트 구조 이해 추가

unit-simulator의 4개 주요 프로젝트 구조 설명:

```
UnitSimulator.Core/          # 순수 시뮬레이션 로직
UnitSimulator.Server/        # WebSocket 서버
ReferenceModels/             # 데이터 기반 모델
sim-studio/                  # React UI
```

각 프로젝트의 역할 및 주요 파일 명시

---

### 8. 에이전트 간 핸드오프 규칙

6개 에이전트의 전체 핸드오프 체인 문서화:

```
Planner → API Designer
  전달: specs/features/feature.md

API Designer → Implementer
  전달: specs/apis/new_api_endpoint.md

Implementer → Tester
  전달: 구현된 코드 + 명세

Tester → Reviewer
  전달: 코드 + 테스트 + 결과

Reviewer → Documenter
  전달: 리뷰 결과 + Git diff
```

---

## Agentic 원본과의 차이점 요약

| 항목 | Agentic (TypeScript) | Unit-Simulator (C#) |
|------|---------------------|---------------------|
| **에이전트 수** | 5개 (Documenter 없음) | 6개 (Documenter 추가) |
| **프롬프트 언어** | TypeScript, Express.js | C#, .NET 9.0, WebSocket |
| **테스트 프레임워크** | Jest | xUnit |
| **API 패러다임** | REST API | WebSocket 프로토콜 |
| **코드 예시** | TypeScript 예시 | C# 예시 (20개 이상) |
| **품질 체크리스트** | 일반적 | C#/.NET 특화 |
| **금지 사항** | 7개 | 13개 (C# 특화 6개 추가) |
| **응답 패턴** | 기본 | C# 프로젝트 구조 반영 |

---

## 생성된 파일

### 1. CLAUDE.md (루트)
- **크기**: ~17K (약 600줄)
- **내용**:
  - 4개 기본 원칙 (C#/.NET 우선 추가)
  - 3개 응답 패턴 (C# 특화)
  - 6개 에이전트 프롬프트 패턴
  - 20개 이상 C# 코드 예시
  - 컨텍스트 관리
  - 파일 작업 규칙
  - 금지 사항 (13개)
  - 품질 체크리스트 (5개 카테고리)
  - C# 코드 스타일 가이드

---

## 주요 성과

### 1. 완전한 C#/.NET 적응
- TypeScript → C# 모든 예시 변환
- Express.js → WebSocket 패턴
- Jest → xUnit 패턴
- REST → WebSocket 메시지 형식

### 2. 에이전트별 명확한 가이드
- 각 에이전트가 수행할 작업 명확히 정의
- 입력/출력 명시
- 실제 사용 가능한 C# 코드 예시

### 3. 실용적인 체크리스트
- 비동기 패턴
- Null 안전성
- 성능
- 보안

### 4. Documenter 에이전트 추가
- ADR 형식
- CHANGELOG 형식
- 문서 분류 규칙

---

## 사용 예시

### Planner 에이전트 사용 시

**사용자 요구사항**:
"타워 업그레이드 시스템 추가"

**Planner가 CLAUDE.md 참조**:
1. 3.1 Planner 프롬프트 패턴 읽기
2. 규칙 10개 확인:
   - C# 네임스페이스 구조 고려
   - async/await 필요성 판단
   - ReferenceModels 변경 여부
   - WebSocket 프로토콜 변경 여부
3. 코드 예시 참조:
   ```csharp
   public class TowerUpgradeSystem
   {
       public async Task<TowerUpgradeResult> UpgradeTowerAsync(...);
   }
   ```
4. 응답 패턴 2.1 사용하여 응답 생성

### Implementer 에이전트 사용 시

**API 명세 입력**:
`specs/apis/new_api_endpoint.md`

**Implementer가 CLAUDE.md 참조**:
1. 3.3 Implementer 프롬프트 패턴
2. C#/.NET 코딩 규칙 섹션 읽기:
   - 명명 규칙
   - 비동기 패턴
   - Null 안전성
   - 에러 처리
   - XML 주석
3. 각 패턴의 ✅ Good / ❌ Bad 예시 참조
4. 품질 체크리스트 7.1 확인

### Tester 에이전트 사용 시

**구현 코드 입력**:
`TowerUpgradeSystem.cs`

**Tester가 CLAUDE.md 참조**:
1. 3.4 Tester 프롬프트 패턴
2. xUnit 테스트 패턴 5개 학습:
   - Fact
   - Theory
   - 비동기
   - 픽스처
   - Trait
3. 각 패턴의 실제 코드 예시 참조
4. 품질 체크리스트 7.3 확인

---

## 검증 방법

### 1. 에이전트가 사용 가능한가?
- [ ] 각 에이전트 프롬프트 패턴이 명확한가?
- [ ] C# 코드 예시가 실제로 컴파일 되는가?
- [ ] xUnit 예시가 실제로 실행 가능한가?

### 2. 응답 패턴이 유용한가?
- [ ] 작업 시작 시 패턴으로 진행 계획 명확한가?
- [ ] 작업 완료 시 패턴으로 결과 확인 가능한가?
- [ ] 에러 발생 시 패턴으로 원인 파악 가능한가?

### 3. 품질 체크리스트가 실용적인가?
- [ ] C# 코드 작성 시 체크리스트로 품질 확인 가능한가?
- [ ] 금지 사항이 일반적인 실수를 커버하는가?

---

## 다음 단계

### 즉시 조치 (완료)
- [x] CLAUDE.md 작성 (루트 디렉토리)
- [x] 6개 에이전트 프롬프트 패턴 정의
- [x] C# 코드 예시 20개 이상 추가
- [x] 응답 패턴 C# 특화

### 다음 검토 (4번)
- [ ] WebSocket 템플릿 추가
  - 기존 unit-simulator WebSocket 메시지 분석
  - API Designer용 템플릿 생성
  - 예시 JSON 페이로드 제공

### Phase 1 킥오프 준비
- [ ] CLAUDE.md를 AGENTS.md와 함께 검토
- [ ] Phase 2에서 에이전트 정의 시 CLAUDE.md 참조
- [ ] Phase 4에서 스킬 구현 시 CLAUDE.md의 코드 예시 활용

---

## 참조 문서
- 생성된 CLAUDE.md: `CLAUDE.md` (루트)
- Agentic 원본: `/Users/storm/Documents/github/agentic/CLAUDE.md`
- 마이그레이션 계획: `docs/agentic-migration-plan-ko.md`

---

**검토 상태**: ✅ 완료
**생성 파일**: CLAUDE.md (루트)
**다음 검토**: 4번 - WebSocket 템플릿 추가
