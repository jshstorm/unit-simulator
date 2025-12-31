namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 스펠 유형
/// </summary>
public enum SpellType
{
    /// <summary>즉시 효과 (파이어볼, 잽 등)</summary>
    Instant,

    /// <summary>지속 범위 효과 (포이즌, 프리즈 등)</summary>
    AreaOverTime,

    /// <summary>유틸리티 (레이지, 힐 등)</summary>
    Utility,

    /// <summary>유닛 소환 (그래브야드 등)</summary>
    Spawning
}
