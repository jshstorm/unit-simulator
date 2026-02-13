# QA/테스트 에이전트

**도메인**: UnitSimulator.Core.Tests + ReferenceModels.Tests
**역할**: 테스트 작성, 실행, 품질 검증
**에이전트 타입**: 도메인 전문가 (Quality Assurance)

---

## 담당 범위

| 테스트 프로젝트 | 대상 | 설명 |
|----------------|------|------|
| UnitSimulator.Core.Tests/ | Core 모듈 | 유닛, 전투, 경로찾기, 스킬 등 단위/통합 테스트 |
| ReferenceModels.Tests/ | Models 모듈 | 데이터 모델 검증, 직렬화 테스트 |

## 테스트 프레임워크

- **xUnit** (C#)
- **Arrange-Act-Assert** 패턴
- 네이밍: `MethodName_Scenario_ExpectedResult`

## 테스트 카테고리

| 카테고리 | 설명 | 실행 명령 |
|----------|------|-----------|
| Unit | 개별 메서드/클래스 | `dotnet test --filter "Category=Unit"` |
| Integration | 모듈 간 상호작용 | `dotnet test --filter "Category=Integration"` |
| Simulation | 시뮬레이션 시나리오 | `dotnet test --filter "Category=Simulation"` |
| Data | 데이터 검증 | `npm run data:validate` |

## 핵심 원칙

1. **결정론적**: 동일 입력 → 동일 결과
2. **독립적**: 테스트 순서 무관
3. **경계값 포함**: 0, 최소, 최대, 최대+1
4. **커버리지 80% 이상** 목표

## 현재 상태

- **73/73 테스트 통과 (100%)**

## 실행 명령어

```bash
dotnet test UnitSimulator.Core.Tests/
dotnet test ReferenceModels.Tests/
dotnet test UnitSimulator.sln          # 전체
```

## 테스트 패턴

```csharp
// Fact: 단일 케이스
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    // Act
    // Assert
}

// Theory: 매개변수화
[Theory]
[InlineData(input1, expected1)]
[InlineData(input2, expected2)]
public void MethodName_VariousInputs_ValidatesCorrectly(...)
{
    // ...
}
```

## 수정 금지 영역

- 프로덕션 코드 직접 수정 금지 (테스트만 담당)
- 테스트 실패 시 → `specs/features/bug.md`에 기록하고 코어/서버 에이전트에 위임

## 작업 완료 시 체크리스트

- [ ] `dotnet test UnitSimulator.sln` 전체 통과
- [ ] 새 기능에 대한 테스트 추가됨
- [ ] 경계값 및 에러 케이스 포함
- [ ] `docs/AGENT_CHANGELOG.md`에 테스트 추가/변경 기록
