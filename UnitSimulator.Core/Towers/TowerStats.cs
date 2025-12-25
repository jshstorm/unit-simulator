using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// 타워 기본 스탯 및 생성 팩토리.
/// Level 11 기준 스탯을 사용합니다.
/// </summary>
public static class TowerStats
{
    // ════════════════════════════════════════════════════════════════════════
    // Princess Tower Stats (Level 11)
    // ════════════════════════════════════════════════════════════════════════

    public const int PrincessMaxHP = 3052;
    public const int PrincessDamage = 109;
    public const float PrincessAttackSpeed = 1.25f;  // 0.8초당 1회 = 1.25/초
    public const float PrincessAttackRange = 350f;
    public const float PrincessRadius = 100f;

    // ════════════════════════════════════════════════════════════════════════
    // King Tower Stats (Level 11)
    // ════════════════════════════════════════════════════════════════════════

    public const int KingMaxHP = 4824;
    public const int KingDamage = 109;
    public const float KingAttackSpeed = 1.0f;  // 1.0초당 1회
    public const float KingAttackRange = 350f;
    public const float KingRadius = 150f;

    // ════════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Princess Tower를 생성합니다.
    /// </summary>
    /// <param name="id">타워 ID</param>
    /// <param name="faction">소속 진영</param>
    /// <param name="position">위치</param>
    /// <returns>생성된 Princess Tower</returns>
    public static Tower CreatePrincessTower(int id, UnitFaction faction, Vector2 position)
    {
        return new Tower
        {
            Id = id,
            Type = TowerType.Princess,
            Faction = faction,
            Position = position,
            Radius = PrincessRadius,
            AttackRange = PrincessAttackRange,
            MaxHP = PrincessMaxHP,
            CurrentHP = PrincessMaxHP,
            Damage = PrincessDamage,
            AttackSpeed = PrincessAttackSpeed,
            CanTarget = TargetType.GroundAndAir,
            IsActivated = true,  // Princess는 항상 활성화
            AttackCooldown = 0f
        };
    }

    /// <summary>
    /// King Tower를 생성합니다.
    /// </summary>
    /// <param name="id">타워 ID</param>
    /// <param name="faction">소속 진영</param>
    /// <param name="position">위치</param>
    /// <returns>생성된 King Tower</returns>
    public static Tower CreateKingTower(int id, UnitFaction faction, Vector2 position)
    {
        return new Tower
        {
            Id = id,
            Type = TowerType.King,
            Faction = faction,
            Position = position,
            Radius = KingRadius,
            AttackRange = KingAttackRange,
            MaxHP = KingMaxHP,
            CurrentHP = KingMaxHP,
            Damage = KingDamage,
            AttackSpeed = KingAttackSpeed,
            CanTarget = TargetType.GroundAndAir,
            IsActivated = false,  // King은 조건부 활성화
            AttackCooldown = 0f
        };
    }

    /// <summary>
    /// 지정된 HP로 Princess Tower를 생성합니다. (테스트용)
    /// </summary>
    public static Tower CreatePrincessTower(int id, UnitFaction faction, Vector2 position, int hp)
    {
        var tower = CreatePrincessTower(id, faction, position);
        tower.CurrentHP = hp;
        return tower;
    }

    /// <summary>
    /// 지정된 HP로 King Tower를 생성합니다. (테스트용)
    /// </summary>
    public static Tower CreateKingTower(int id, UnitFaction faction, Vector2 position, int hp)
    {
        var tower = CreateKingTower(id, faction, position);
        tower.CurrentHP = hp;
        return tower;
    }
}
