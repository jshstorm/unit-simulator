using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// 피해 유형을 정의합니다.
/// </summary>
public enum DamageType
{
    Normal,       // 일반 공격
    Splash,       // 스플래시 피해
    DeathDamage,  // 사망 시 폭발 피해
    Spell,        // 스펠 피해
    Tower         // 타워 공격
}

/// <summary>
/// 프레임 내 발생하는 피해 이벤트
/// </summary>
public class DamageEvent
{
    /// <summary>
    /// 피해 원인 유닛 (null일 수 있음 - 스펠 등)
    /// </summary>
    public Unit? Source { get; init; }

    /// <summary>
    /// 피해 대상 유닛
    /// </summary>
    public required Unit Target { get; init; }

    /// <summary>
    /// 피해량
    /// </summary>
    public int Amount { get; init; }

    /// <summary>
    /// 피해 유형
    /// </summary>
    public DamageType Type { get; init; } = DamageType.Normal;
}

/// <summary>
/// 타워가 유닛에게 가하는 피해 이벤트
/// </summary>
public class TowerDamageEvent
{
    /// <summary>
    /// 공격하는 타워
    /// </summary>
    public required Tower Source { get; init; }

    /// <summary>
    /// 피해 대상 유닛
    /// </summary>
    public required Unit Target { get; init; }

    /// <summary>
    /// 피해량
    /// </summary>
    public int Amount { get; init; }
}

/// <summary>
/// 유닛이 타워에게 가하는 피해 이벤트
/// </summary>
public class DamageToTowerEvent
{
    /// <summary>
    /// 공격하는 유닛
    /// </summary>
    public required Unit Source { get; init; }

    /// <summary>
    /// 피해 대상 타워
    /// </summary>
    public required Tower Target { get; init; }

    /// <summary>
    /// 피해량
    /// </summary>
    public int Amount { get; init; }
}

/// <summary>
/// 프레임 내 발생하는 모든 이벤트를 수집하는 컨테이너.
/// Phase 1(Collect)에서 이벤트를 수집하고, Phase 2(Apply)에서 일괄 적용합니다.
/// </summary>
public class FrameEvents
{
    // ════════════════════════════════════════════════════════════════════════
    // 유닛 간 피해
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 수집된 피해 이벤트 목록
    /// </summary>
    public List<DamageEvent> Damages { get; } = new();

    /// <summary>
    /// 수집된 스폰 요청 목록
    /// </summary>
    public List<UnitSpawnRequest> Spawns { get; } = new();

    // ════════════════════════════════════════════════════════════════════════
    // 타워 관련 피해
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 타워 → 유닛 피해 이벤트 목록
    /// </summary>
    public List<TowerDamageEvent> TowerDamages { get; } = new();

    /// <summary>
    /// 유닛 → 타워 피해 이벤트 목록
    /// </summary>
    public List<DamageToTowerEvent> DamageToTowers { get; } = new();

    /// <summary>
    /// 피해 이벤트를 추가합니다.
    /// </summary>
    public void AddDamage(Unit source, Unit target, int amount, DamageType type = DamageType.Normal)
    {
        Damages.Add(new DamageEvent
        {
            Source = source,
            Target = target,
            Amount = amount,
            Type = type
        });
    }

    /// <summary>
    /// 스폰 요청을 추가합니다.
    /// </summary>
    public void AddSpawn(UnitSpawnRequest spawn)
    {
        Spawns.Add(spawn);
    }

    /// <summary>
    /// 여러 스폰 요청을 추가합니다.
    /// </summary>
    public void AddSpawns(IEnumerable<UnitSpawnRequest> spawns)
    {
        Spawns.AddRange(spawns);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 타워 피해 이벤트 추가
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 타워 → 유닛 피해 이벤트를 추가합니다.
    /// </summary>
    public void AddTowerDamage(Tower source, Unit target, int amount)
    {
        TowerDamages.Add(new TowerDamageEvent
        {
            Source = source,
            Target = target,
            Amount = amount
        });
    }

    /// <summary>
    /// 유닛 → 타워 피해 이벤트를 추가합니다.
    /// </summary>
    public void AddDamageToTower(Unit source, Tower target, int amount)
    {
        DamageToTowers.Add(new DamageToTowerEvent
        {
            Source = source,
            Target = target,
            Amount = amount
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // 유틸리티
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 모든 이벤트를 초기화합니다.
    /// </summary>
    public void Clear()
    {
        Damages.Clear();
        Spawns.Clear();
        TowerDamages.Clear();
        DamageToTowers.Clear();
    }

    /// <summary>
    /// 수집된 피해 이벤트 수
    /// </summary>
    public int DamageCount => Damages.Count;

    /// <summary>
    /// 수집된 스폰 요청 수
    /// </summary>
    public int SpawnCount => Spawns.Count;

    /// <summary>
    /// 수집된 타워 피해 이벤트 수
    /// </summary>
    public int TowerDamageCount => TowerDamages.Count;

    /// <summary>
    /// 수집된 타워 대상 피해 이벤트 수
    /// </summary>
    public int DamageToTowerCount => DamageToTowers.Count;
}
