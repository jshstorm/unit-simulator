namespace ReferenceModels.Models.Enums;

/// <summary>
/// 유닛이 공격할 수 있는 대상 유형을 정의합니다.
/// Flags 속성으로 복수 선택 가능합니다.
/// </summary>
[Flags]
public enum TargetType
{
    None     = 0,
    Ground   = 1 << 0,  // 지상 유닛 공격 가능
    Air      = 1 << 1,  // 공중 유닛 공격 가능
    Building = 1 << 2,  // 건물 공격 가능

    GroundAndAir = Ground | Air,
    All = Ground | Air | Building
}
