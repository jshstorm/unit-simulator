namespace ReferenceModels.Models.Enums;

/// <summary>
/// 건물 유형
/// </summary>
public enum BuildingType
{
    /// <summary>방어용 건물 (타워, 대포 등)</summary>
    Defensive,

    /// <summary>유닛 소환 건물 (묘비, 오두막 등)</summary>
    Spawner,

    /// <summary>유틸리티 건물 (엘릭서 펌프 등)</summary>
    Utility
}
