# 인터페이스 계약: 데이터 접근

**소유자**: 오케스트레이터 (변경 시 승인 필요)
**구현**: 데이터/모델 에이전트
**사용**: Core 에이전트, Server 에이전트

---

## 목적

ReferenceModels 모듈이 외부에 노출하는 데이터 접근 API를 정의합니다.
Core와 Server 에이전트는 이 인터페이스를 통해서만 참조 데이터에 접근합니다.

## 인터페이스 정의

### IGameDataProvider

참조 데이터에 대한 읽기 전용 접근:

```csharp
/// <summary>
/// 게임 참조 데이터 접근 인터페이스
/// Core/Server가 이 인터페이스를 통해 게임 데이터에 접근
/// </summary>
public interface IGameDataProvider
{
    /// <summary>유닛 참조 데이터 조회</summary>
    UnitReference? GetUnit(string unitId);

    /// <summary>타워 참조 데이터 조회</summary>
    TowerReference? GetTower(string towerId);

    /// <summary>스킬 참조 데이터 조회</summary>
    SkillReference? GetSkill(string skillId);

    /// <summary>웨이브 정의 조회</summary>
    WaveDefinition? GetWave(int waveNumber);

    /// <summary>밸런스 상수 조회</summary>
    GameBalance GetBalance();

    /// <summary>모든 유닛 목록</summary>
    IReadOnlyList<UnitReference> GetAllUnits();

    /// <summary>모든 타워 목록</summary>
    IReadOnlyList<TowerReference> GetAllTowers();

    /// <summary>모든 스킬 목록</summary>
    IReadOnlyList<SkillReference> GetAllSkills();
}
```

## JSON Schema ↔ C# 모델 매핑

| JSON Schema | C# 모델 | JSON 파일 |
|-------------|---------|-----------|
| unit-stats.schema.json | UnitReference | units.json |
| skill-reference.schema.json | SkillReference | skills.json |
| tower-reference.schema.json | TowerReference | towers.json |
| wave-definition.schema.json | WaveDefinition | waves.json |
| game-balance.schema.json | GameBalance | balance.json |

## 변경 프로토콜

1. 스키마 변경 제안 → 이 문서 + `data/schemas/` 갱신
2. 오케스트레이터 승인
3. 데이터 에이전트: 스키마 + C# 모델 갱신 + `npm run data:validate`
4. Core 에이전트: 데이터 사용부 갱신
5. Server 에이전트: 직렬화/역직렬화 갱신
6. `docs/AGENT_CHANGELOG.md`에 ⚠️ 기록

## 변경 이력

| 날짜 | 변경 | 영향 |
|------|------|------|
| 2026-02-13 | 초안 작성 | - |
