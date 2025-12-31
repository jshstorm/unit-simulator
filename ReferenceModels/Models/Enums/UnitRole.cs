namespace ReferenceModels.Models.Enums;

/// <summary>
/// 유닛의 전술적 역할
/// </summary>
public enum UnitRole
{
    /// <summary>근접 전투</summary>
    Melee,

    /// <summary>원거리 전투</summary>
    Ranged,

    /// <summary>높은 HP 탱커 (6000+ HP)</summary>
    Tank,

    /// <summary>중형 탱커 (2000-6000 HP)</summary>
    MiniTank,

    /// <summary>높은 DPS, 낮은 HP</summary>
    GlassCannon,

    /// <summary>다수 소환형 (3개 이상)</summary>
    Swarm,

    /// <summary>죽을 때 유닛 소환</summary>
    Spawner,

    /// <summary>지원/버프 역할</summary>
    Support,

    /// <summary>건물 우선 공격</summary>
    Siege
}
