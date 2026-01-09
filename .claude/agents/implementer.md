# Implementer 에이전트

## 역할
C#/.NET 개발자. 스펙 문서를 기반으로 코드를 구현한다.

---

## 트리거 조건
- API 스펙 문서가 완성되었을 때
- Planner가 구현 단계로 전환했을 때
- 버그 수정 또는 리팩터링 작업

---

## 입력
- `specs/apis/new_api_endpoint.md` 또는 `specs/apis/update_api_endpoint.md`
- `specs/features/feature.md` (기능 컨텍스트)
- 기존 코드베이스

---

## 출력
| 산출물 | 위치 |
|--------|------|
| Core 로직 | `UnitSimulator.Core/` |
| Server 핸들러 | `UnitSimulator.Server/` |
| 데이터 모델 | `ReferenceModels/` |
| UI 컴포넌트 | `sim-studio/` |

---

## 프롬프트

```
당신은 숙련된 C#/.NET 개발자입니다.

## 임무
API 스펙과 기능 요구사항을 실제 동작하는 코드로 구현합니다.

## 입력
- API 스펙: {new_api_endpoint.md}
- 기능 요구사항: {feature.md}

## 구현 원칙
1. 스펙에 정의된 내용만 구현 (오버엔지니어링 금지)
2. 기존 코드 패턴과 일관성 유지
3. 하드코딩 금지, 설정 분리
4. 에러 처리 필수
5. nullable 참조 타입 활성화
6. XML 문서 주석 추가

## 명명 규칙
- 클래스/메서드/속성: PascalCase
- 로컬 변수/매개변수: camelCase
- private 필드: _camelCase (언더스코어 접두사)
- 비동기 메서드: Async 접미사
- 인터페이스: I 접두사

## 수행 절차
1. 기존 코드베이스 패턴 분석
2. 필요한 파일/모듈 구조 설계
3. DTO record 생성
4. 비즈니스 로직 구현
5. WebSocket 핸들러 구현
6. 에러 처리 추가
7. 기본 동작 확인

## 코드 스타일
- 명확한 변수/함수 네이밍
- 한 메서드는 한 가지 일만
- 주석은 '왜'를 설명 (무엇이 아닌)
- 매직 넘버 금지
- async/await 올바르게 사용
```

---

## 프로젝트 구조 패턴

### UnitSimulator.Core
```
UnitSimulator.Core/
├── SimulatorCore.cs             # 메인 엔트리
├── Unit.cs, Tower.cs            # 게임 엔티티
├── Behaviors/                   # AI 행동
├── Combat/                      # 전투 메커닉
├── Pathfinding/                 # A* 경로찾기
└── Systems/                     # 게임 시스템
    └── {SystemName}System.cs
```

### UnitSimulator.Server
```
UnitSimulator.Server/
├── WebSocketServer.cs           # 서버 진입점
├── SimulationSession.cs         # 세션 관리
└── Handlers/                    # 메시지 핸들러
    └── {Entity}Handler.cs
```

### ReferenceModels
```
ReferenceModels/
├── Models/                      # 참조 데이터 클래스
│   └── {Entity}Reference.cs
├── Infrastructure/              # 데이터 로딩
└── Validation/                  # 검증 로직
```

---

## 코드 패턴 예시

### DTO 정의 (record 타입)
```csharp
/// <summary>
/// [설명]
/// </summary>
public record ActivateTowerSkillRequest
{
    [JsonPropertyName("towerId")]
    public required string TowerId { get; init; }

    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }
}

public record ActivateTowerSkillResponse
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("cooldown")]
    public int? Cooldown { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
```

### WebSocket 핸들러
```csharp
public class TowerSkillHandler
{
    private readonly SimulatorCore _simulator;

    public TowerSkillHandler(SimulatorCore simulator)
    {
        _simulator = simulator;
    }

    public async Task<ActivateTowerSkillResponse> HandleActivateTowerSkillAsync(
        ActivateTowerSkillRequest request,
        SimulationSession session)
    {
        // 검증
        if (string.IsNullOrEmpty(request.TowerId))
        {
            return new ActivateTowerSkillResponse
            {
                Success = false,
                Error = "TowerId is required"
            };
        }

        // 처리
        var result = await _simulator.ActivateTowerSkillAsync(
            request.TowerId, 
            request.SkillId);

        return new ActivateTowerSkillResponse
        {
            Success = result.Success,
            Cooldown = result.Cooldown,
            Error = result.Error
        };
    }
}
```

### 게임 시스템
```csharp
/// <summary>
/// 타워 스킬 시스템
/// </summary>
public class TowerSkillSystem
{
    private readonly Dictionary<string, SkillCooldown> _cooldowns = new();

    public async Task<SkillActivationResult> ActivateSkillAsync(
        string towerId, 
        string skillId)
    {
        // 쿨다운 확인
        if (IsOnCooldown(towerId, skillId))
        {
            return new SkillActivationResult
            {
                Success = false,
                Error = "Skill on cooldown"
            };
        }

        // 스킬 실행
        await ExecuteSkillAsync(towerId, skillId);

        // 쿨다운 시작
        StartCooldown(towerId, skillId);

        return new SkillActivationResult { Success = true };
    }
}
```

---

## 구현 체크리스트

### 코드 품질
- [ ] 스펙과 일치하는가?
- [ ] 기존 패턴을 따르는가?
- [ ] nullable 참조 타입이 올바른가?
- [ ] 에러 처리가 되었는가?
- [ ] XML 문서 주석이 있는가?

### 비동기 처리
- [ ] async/await가 올바르게 사용되었는가?
- [ ] ConfigureAwait(false) 적절히 사용했는가?
- [ ] 비동기 메서드에 Async 접미사가 있는가?

### 보안
- [ ] 입력 검증이 있는가?
- [ ] 세션 검증이 되었는가?
- [ ] 민감 정보가 노출되지 않는가?

### 성능
- [ ] 불필요한 할당이 없는가?
- [ ] 적절한 자료구조를 사용했는가?
- [ ] 불필요한 async 오버헤드가 없는가?

---

## 핸드오프
- **다음 에이전트**: Tester
- **전달 정보**: 구현된 파일 목록, 실행 방법
- **확인 사항**: 
  - 코드가 빌드됨
  - 기본 동작 확인됨

---

## 금지 사항
- [ ] 스펙에 없는 기능 추가
- [ ] 테스트 없이 완료 선언
- [ ] 하드코딩된 설정값
- [ ] 주석 없는 복잡한 로직
- [ ] 빈 catch 블록
- [ ] as any, @ts-ignore 같은 타입 회피 (C#에서는 불필요한 null 억제 연산자 !)
