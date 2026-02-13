# 인터페이스 계약: 시뮬레이션 엔진

**소유자**: 오케스트레이터 (변경 시 승인 필요)
**구현**: Core 에이전트
**사용**: Server 에이전트, Tests 에이전트

---

## 목적

Core 모듈이 외부에 노출하는 시뮬레이션 API를 정의합니다.
Server 에이전트는 이 인터페이스를 통해서만 Core에 접근합니다.

## 인터페이스 정의

### ISimulationEngine

Core가 노출하는 메인 시뮬레이션 API:

```csharp
/// <summary>
/// 시뮬레이션 엔진의 외부 API
/// Server 에이전트가 이 인터페이스를 통해 Core에 접근
/// </summary>
public interface ISimulationEngine
{
    /// <summary>시뮬레이션 프레임 실행</summary>
    Task<SimulationResult> RunFrameAsync(SimulationInput input);

    /// <summary>유닛 상태 조회</summary>
    Task<UnitState[]> GetUnitStatesAsync(string sessionId);

    /// <summary>타워 상태 조회</summary>
    Task<TowerState[]> GetTowerStatesAsync(string sessionId);

    /// <summary>스킬 발동</summary>
    Task<SkillActivationResult> ActivateSkillAsync(string entityId, string skillId);

    /// <summary>커맨드 큐에 커맨드 추가</summary>
    void EnqueueCommand(ICommand command);
}
```

## 변경 프로토콜

1. 변경 제안 → 이 문서 갱신
2. 오케스트레이터 승인
3. Core 에이전트: 인터페이스 구현 갱신
4. Server 에이전트: 핸들러 호출부 갱신
5. Tests 에이전트: 테스트 갱신
6. `docs/AGENT_CHANGELOG.md`에 ⚠️ 기록

## 변경 이력

| 날짜 | 변경 | 영향 |
|------|------|------|
| 2026-02-13 | 초안 작성 | - |
