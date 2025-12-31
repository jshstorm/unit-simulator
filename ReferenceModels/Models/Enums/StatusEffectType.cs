namespace UnitSimulator.ReferenceModels.Models.Enums;

/// <summary>
/// 상태 효과 유형
/// </summary>
[Flags]
public enum StatusEffectType
{
    None = 0,

    /// <summary>기절 - 이동 및 공격 불가</summary>
    Stunned = 1 << 0,

    /// <summary>빙결 - 이동 및 공격 불가 (얼음 효과)</summary>
    Frozen = 1 << 1,

    /// <summary>둔화 - 이동 속도 감소</summary>
    Slowed = 1 << 2,

    /// <summary>속박 - 이동 불가, 공격은 가능</summary>
    Rooted = 1 << 3,

    /// <summary>중독 - 지속 피해</summary>
    Poisoned = 1 << 4,

    /// <summary>화상 - 지속 피해 (높은 DPS)</summary>
    Burning = 1 << 5,

    /// <summary>격분 - 공격 속도 및 이동 속도 증가</summary>
    Raged = 1 << 6,

    /// <summary>치유 - 지속 회복</summary>
    Healing = 1 << 7,

    /// <summary>보호막 - 추가 HP</summary>
    Shielded = 1 << 8,

    /// <summary>투명 - 타게팅 불가</summary>
    Invisible = 1 << 9,

    /// <summary>표식 - 받는 피해 증가</summary>
    Marked = 1 << 10,

    /// <summary>무적 - 피해 받지 않음</summary>
    Invulnerable = 1 << 11
}
