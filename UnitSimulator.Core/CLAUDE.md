# 시뮬레이션 코어 에이전트

**도메인**: UnitSimulator.Core
**역할**: 순수 시뮬레이션 로직 담당
**에이전트 타입**: 도메인 전문가 (Game Systems)

---

## 담당 범위

| 시스템 | 디렉토리 | 설명 |
|--------|----------|------|
| 유닛 | Units/ | 유닛 상태, 이동, 행동 |
| 타워 | Towers/ | 타워 공격, 스킬, 업그레이드 |
| 전투 | Combat/ | 데미지 계산, 타겟팅 |
| 경로찾기 | Pathfinding/ | A* 알고리즘, 맵 그리드 |
| 스킬 | Skills/ | 스킬 발동, 쿨다운, 효과 |
| 게임 상태 | GameState/ | 시뮬레이션 루프, 프레임 관리 |
| 행동 AI | Behaviors/ | Squad/Enemy 행동 패턴 |
| 충돌 회피 | AvoidanceSystem.cs | 유닛 간 충돌 해소 |
| 인터페이스 | Contracts/ | 모듈 간 공유 인터페이스 |
| 데이터 접근 | Data/ | 데이터 접근 추상화 레이어 |

## 핵심 원칙

1. **결정론적 (Deterministic)**: 동일 입력 → 동일 결과. 리플레이와 동기화에 필수
2. **엔진 무관 (Engine Agnostic)**: 렌더링, 입력, 사운드에 대한 의존성 없음
3. **데이터 주도 (Data-Driven)**: 코드 변경 없이 밸런스 조정 가능 (JSON 참조 데이터)
4. **커맨드 패턴**: 모든 액션은 직렬화 가능한 커맨드 (Commands/)
5. **테스트 가능**: 의존성 주입, 인터페이스 추상화

## 코딩 규칙

- C# 9.0+, .NET 9.0, nullable 참조 타입 활성화
- PascalCase (클래스/메서드/속성), camelCase (변수), _camelCase (private 필드)
- 비동기 메서드: `Async` 접미사
- XML 문서 주석: 모든 공개 API
- 매직 넘버 금지 → `GameConstants.cs` 또는 ReferenceModels 데이터 사용

## 의존성

```
UnitSimulator.Core
    └── ReferenceModels (읽기 전용 참조)
```

- **ReferenceModels**: 유닛/타워/스킬 참조 데이터 접근 (IGameDataProvider 통해)
- **Contracts/**: Server 에이전트와 공유하는 인터페이스 정의

## 수정 금지 영역

- `UnitSimulator.Server/*` → 서버 에이전트 소유
- `sim-studio/*` → UI 에이전트 소유
- `data/schemas/*` → 데이터 에이전트 소유
- `data/references/*` → 데이터 에이전트 소유

## 인터페이스 계약

Core가 외부에 노출하는 API는 `Contracts/` 디렉토리에 정의:
- 변경 시 오케스트레이터(Root CLAUDE.md) 승인 필요
- 영향받는 에이전트: Server, Tests

## 작업 완료 시 체크리스트

- [ ] `dotnet build UnitSimulator.Core` 통과
- [ ] `dotnet test UnitSimulator.Core.Tests` 통과
- [ ] 기존 테스트 깨뜨리지 않음
- [ ] Contracts/ 인터페이스 변경 시 `docs/AGENT_CHANGELOG.md`에 기록
- [ ] 새로운 공개 API에 XML 문서 주석 추가
