namespace ReferenceModels.Models.Enums;

/// <summary>
/// 공격 방식 및 사거리 유형
/// </summary>
public enum AttackType
{
    /// <summary>근접 공격 - 초단거리 (1 타일 이하)</summary>
    MeleeShort,

    /// <summary>근접 공격 - 단거리 (1.5 타일)</summary>
    Melee,

    /// <summary>근접 공격 - 중거리 (2-3 타일)</summary>
    MeleeMedium,

    /// <summary>근접 공격 - 장거리 (4-5 타일)</summary>
    MeleeLong,

    /// <summary>원거리 공격 (5 타일 이상)</summary>
    Ranged,

    /// <summary>공격하지 않음</summary>
    None
}
