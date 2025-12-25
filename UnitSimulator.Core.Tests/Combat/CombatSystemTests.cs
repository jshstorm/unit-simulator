using System.Numerics;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace UnitSimulator.Core.Tests.Combat;

public class CombatSystemTests
{
    private readonly CombatSystem _combat = new();

    [Fact]
    public void PerformAttack_ShouldTriggerDeathSpawnAndDeathDamage()
    {
        // Arrange: attacker with splash and charge (damage 10 base)
        var attacker = new Unit(
            position: Vector2.Zero,
            radius: 10f,
            speed: 5f,
            turnSpeed: 0.1f,
            role: UnitRole.Melee,
            hp: 100,
            id: 1,
            faction: UnitFaction.Friendly,
            layer: MovementLayer.Ground,
            canTarget: TargetType.GroundAndAir,
            damage: 10,
            abilities: new List<AbilityData> { new SplashDamageData { Radius = 50f }, new ChargeAttackData(), new ShieldData() }
        );

        // Target with death effects
        var target = new Unit(
            position: new Vector2(10, 0),
            radius: 10f,
            speed: 0f,
            turnSpeed: 0f,
            role: UnitRole.Melee,
            hp: 5,
            id: 2,
            faction: UnitFaction.Enemy,
            layer: MovementLayer.Ground,
            canTarget: TargetType.GroundAndAir,
            damage: 0,
            abilities: new List<AbilityData>
            {
                new DeathSpawnData { SpawnUnitId = "minion", SpawnCount = 2, SpawnRadius = 5f },
                new DeathDamageData { Damage = 3, Radius = 30f }
            }
        );

        // Another enemy within death explosion radius
        var nearbyEnemy = new Unit(
            position: new Vector2(20, 0),
            radius: 10f,
            speed: 0f,
            turnSpeed: 0f,
            role: UnitRole.Melee,
            hp: 3,
            id: 3,
            faction: UnitFaction.Enemy,
            layer: MovementLayer.Ground,
            canTarget: TargetType.GroundAndAir,
            damage: 0
        );

        var enemies = new List<Unit> { target, nearbyEnemy };

        // Act
        var result = _combat.PerformAttack(attacker, target, enemies);

        // Assert: main target dead, death explosion killed the nearby enemy, spawn requests issued
        result.KilledUnits.Should().Contain(target);
        result.KilledUnits.Should().Contain(nearbyEnemy);
        result.SpawnRequests.Should().HaveCount(2);
        result.SpawnRequests.Should().OnlyContain(r => r.UnitId == "minion" && r.Faction == UnitFaction.Enemy);
    }

    [Fact]
    public void ChargeAttack_WithSplash_ShouldBypassShieldAndKillNearby()
    {
        // Arrange
        var chargeAbility = new ChargeAttackData { DamageMultiplier = 2.0f };
        var splashAbility = new SplashDamageData { Radius = 40f };
        var shieldAbility = new ShieldData { MaxShieldHP = 5 };

        var attacker = new Unit(
            position: Vector2.Zero,
            radius: 10f,
            speed: 5f,
            turnSpeed: 0.1f,
            role: UnitRole.Melee,
            hp: 100,
            id: 1,
            faction: UnitFaction.Friendly,
            layer: MovementLayer.Ground,
            canTarget: TargetType.Ground,
            damage: 10,
            abilities: new List<AbilityData> { chargeAbility, splashAbility }
        );
        // 강제로 차지 완료 상태 설정
        attacker.EnsureChargeState();
        attacker.ChargeState!.IsCharged = true;

        var primary = new Unit(
            position: new Vector2(10, 0),
            radius: 10f,
            speed: 0f,
            turnSpeed: 0f,
            role: UnitRole.Melee,
            hp: 10,
            id: 2,
            faction: UnitFaction.Enemy,
            layer: MovementLayer.Ground,
            canTarget: TargetType.Ground,
            damage: 0,
            abilities: new List<AbilityData> { shieldAbility }
        );

        var secondary = new Unit(
            position: new Vector2(20, 0),
            radius: 10f,
            speed: 0f,
            turnSpeed: 0f,
            role: UnitRole.Melee,
            hp: 8,
            id: 3,
            faction: UnitFaction.Enemy,
            layer: MovementLayer.Ground,
            canTarget: TargetType.Ground,
            damage: 0
        );

        var enemies = new List<Unit> { primary, secondary };

        // Act
        var result = _combat.PerformAttack(attacker, primary, enemies);

        // Assert: 2배 데미지(20) → 쉴드 5 흡수, HP 10 초과로 주 대상 사망, 스플래시로 근처 적도 사망
        primary.ShieldHP.Should().Be(0);
        primary.IsDead.Should().BeTrue();
        secondary.IsDead.Should().BeTrue();
        result.KilledUnits.Should().Contain(primary);
        result.KilledUnits.Should().Contain(secondary);
    }
}
