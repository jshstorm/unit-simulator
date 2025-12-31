using ReferenceModels.Models;
using ReferenceModels.Models.Enums;

namespace ReferenceModels.Tests.TestHelpers;

/// <summary>
/// 테스트용 Reference 객체 생성 팩토리
/// </summary>
public static class ReferenceTestFactory
{
    public static UnitReference CreateValidUnit(string displayName = "Test Unit")
    {
        return new UnitReference
        {
            DisplayName = displayName,
            MaxHP = 100,
            Damage = 50,
            MoveSpeed = 5.0f,
            TurnSpeed = 0.1f,
            AttackRange = 60f,
            Radius = 20f,
            Role = UnitRole.Melee,
            Layer = MovementLayer.Ground,
            CanTarget = TargetType.Ground,
            TargetPriority = TargetPriority.Nearest,
            EntityType = EntityType.Troop,
            AttackType = AttackType.Melee,
            AttackSpeed = 1.0f,
            ShieldHP = 0,
            SpawnCount = 1,
            Skills = new List<string>()
        };
    }

    public static SkillReference CreateValidSkill(string type = "SplashDamage")
    {
        return new SkillReference
        {
            Type = type,
            Radius = 60f,
            DamageFalloff = 0f
        };
    }

    public static BuildingReference CreateValidSpawner(string spawnUnitId = "skeleton")
    {
        return new BuildingReference
        {
            DisplayName = "Test Spawner",
            Type = BuildingType.Spawner,
            MaxHP = 500,
            Radius = 2.0f,
            Lifetime = 40f,
            SpawnUnitId = spawnUnitId,
            SpawnCount = 1,
            SpawnInterval = 3.0f,
            FirstSpawnDelay = 1.0f,
            Skills = new List<string>()
        };
    }

    public static BuildingReference CreateValidDefensive()
    {
        return new BuildingReference
        {
            DisplayName = "Test Cannon",
            Type = BuildingType.Defensive,
            MaxHP = 1000,
            Radius = 2.0f,
            Lifetime = 40f,
            AttackRange = 5.5f,
            Damage = 200,
            AttackSpeed = 1.0f,
            CanTarget = TargetType.Ground,
            Skills = new List<string>()
        };
    }

    public static SpellReference CreateValidInstantSpell()
    {
        return new SpellReference
        {
            DisplayName = "Test Fireball",
            Type = SpellType.Instant,
            Radius = 2.5f,
            Damage = 500,
            CastDelay = 1.0f,
            AffectedTargets = TargetType.Ground | TargetType.Air
        };
    }

    public static SpellReference CreateValidAreaSpell()
    {
        return new SpellReference
        {
            DisplayName = "Test Poison",
            Type = SpellType.AreaOverTime,
            Radius = 3.5f,
            Duration = 8.0f,
            DamagePerTick = 95,
            TickInterval = 1.0f,
            AffectedTargets = TargetType.Ground | TargetType.Air
        };
    }

    public static TowerReference CreateValidTower(TowerType type = TowerType.Princess)
    {
        return new TowerReference
        {
            DisplayName = "Test Tower",
            Type = type,
            MaxHP = 2500,
            Damage = 90,
            AttackSpeed = 0.8f,
            AttackRadius = 7.5f,
            Radius = 2.5f,
            CanTarget = TargetType.Ground | TargetType.Air
        };
    }
}
