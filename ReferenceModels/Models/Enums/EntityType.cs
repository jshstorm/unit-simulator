namespace ReferenceModels.Models.Enums;

/// <summary>
/// 엔티티의 기본 유형
/// </summary>
public enum EntityType
{
    /// <summary>유닛 (배치 가능한 전투 개체)</summary>
    Troop,

    /// <summary>건물 (고정된 구조물)</summary>
    Building,

    /// <summary>스펠 (일회성 효과)</summary>
    Spell,

    /// <summary>투사체 (발사되는 공격 개체)</summary>
    Projectile
}
