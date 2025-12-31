namespace ReferenceModels.Models.Enums;

/// <summary>
/// 특수 능력 유형
/// </summary>
public enum AbilityType
{
    /// <summary>돌진 공격</summary>
    ChargeAttack,

    /// <summary>범위 피해</summary>
    SplashDamage,

    /// <summary>보호막</summary>
    Shield,

    /// <summary>죽을 때 유닛 소환</summary>
    DeathSpawn,

    /// <summary>죽을 때 피해</summary>
    DeathDamage,

    /// <summary>타겟팅 우선순위</summary>
    TargetPriority,

    /// <summary>상태 효과 부여</summary>
    ApplyStatusEffect,

    /// <summary>범위 버프</summary>
    AuraEffect,

    /// <summary>체력 회복</summary>
    Healing,

    /// <summary>텔레포트</summary>
    Teleport,

    /// <summary>무적 시간</summary>
    Invulnerability
}
