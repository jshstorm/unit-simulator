using System.Collections.Generic;
using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// ReferenceModels의 레퍼런스 타입에 대한 확장 메서드
/// </summary>
public static class ReferenceExtensions
{
    /// <summary>
    /// UnitReference를 기반으로 Unit 인스턴스를 생성합니다.
    /// </summary>
    /// <remarks>
    /// 단일 유닛을 생성합니다. Swarm 유닛(SpawnCount > 1)의 경우 CreateUnits()를 사용하세요.
    /// </remarks>
    public static Unit CreateUnit(
        this UnitReference unitRef,
        string unitId,
        int id,
        UnitFaction faction,
        Vector2 position,
        ReferenceManager? referenceManager = null)
    {
        var abilities = ConvertSkills(unitRef, referenceManager);

        // ShieldHP가 설정되어 있고 Shield 능력이 없으면 자동으로 추가
        if (unitRef.ShieldHP > 0 && !abilities.Any(a => a is ShieldData))
        {
            abilities.Add(new ShieldData
            {
                MaxShieldHP = unitRef.ShieldHP,
                BlocksStun = false,
                BlocksKnockback = false
            });
        }

        var unit = new Unit(
            position: position,
            radius: unitRef.Radius,
            speed: unitRef.MoveSpeed,
            turnSpeed: unitRef.TurnSpeed,
            role: unitRef.Role,
            hp: unitRef.MaxHP,
            id: id,
            faction: faction,
            layer: unitRef.Layer,
            canTarget: unitRef.CanTarget,
            damage: unitRef.Damage,
            abilities: abilities,
            unitId: unitId,
            targetPriority: unitRef.TargetPriority
        );
        return unit;
    }

    /// <summary>
    /// UnitReference를 기반으로 여러 Unit 인스턴스를 생성합니다 (Swarm 유닛용).
    /// SpawnCount만큼 유닛을 생성하여 SpawnRadius 내에 분산 배치합니다.
    /// </summary>
    public static List<Unit> CreateUnits(
        this UnitReference unitRef,
        string unitId,
        int startId,
        UnitFaction faction,
        Vector2 centerPosition,
        float spawnRadius = 30f,
        ReferenceManager? referenceManager = null)
    {
        var units = new List<Unit>();
        var count = Math.Max(1, unitRef.SpawnCount);

        for (int i = 0; i < count; i++)
        {
            // 원형으로 분산 배치
            var angle = i * (2 * MathF.PI / count);
            var offset = new Vector2(
                MathF.Cos(angle) * spawnRadius,
                MathF.Sin(angle) * spawnRadius
            );
            var position = centerPosition + offset;

            var unit = unitRef.CreateUnit(
                unitId,
                startId + i,
                faction,
                position,
                referenceManager
            );

            units.Add(unit);
        }

        return units;
    }

    /// <summary>
    /// SkillReference를 AbilityData로 변환합니다.
    /// </summary>
    public static AbilityData? ToAbilityData(this SkillReference skillRef)
    {
        return skillRef.Type.ToLowerInvariant() switch
        {
            "chargeattack" => new ChargeAttackData
            {
                TriggerDistance = skillRef.TriggerDistance,
                RequiredChargeDistance = skillRef.RequiredChargeDistance,
                DamageMultiplier = skillRef.DamageMultiplier,
                SpeedMultiplier = skillRef.SpeedMultiplier
            },
            "splashdamage" => new SplashDamageData
            {
                Radius = skillRef.Radius,
                DamageFalloff = skillRef.DamageFalloff
            },
            "shield" => new ShieldData
            {
                MaxShieldHP = skillRef.MaxShieldHP,
                BlocksStun = skillRef.BlocksStun,
                BlocksKnockback = skillRef.BlocksKnockback
            },
            "deathspawn" => new DeathSpawnData
            {
                SpawnUnitId = skillRef.SpawnUnitId,
                SpawnCount = skillRef.SpawnCount,
                SpawnRadius = skillRef.SpawnRadius,
                SpawnUnitHP = skillRef.SpawnUnitHP
            },
            "deathdamage" => new DeathDamageData
            {
                Damage = skillRef.Damage,
                Radius = skillRef.Radius,
                KnockbackDistance = skillRef.KnockbackDistance
            },
            "statuseffect" => new StatusEffectAbilityData
            {
                AppliedEffect = skillRef.AppliedEffect,
                EffectDuration = skillRef.EffectDuration,
                EffectMagnitude = skillRef.EffectMagnitude,
                EffectRange = skillRef.EffectRange,
                AffectedTargets = skillRef.AffectedTargets
            },
            _ => null
        };
    }

    private static List<AbilityData> ConvertSkills(UnitReference unitRef, ReferenceManager? referenceManager)
    {
        var result = new List<AbilityData>();
        if (referenceManager?.Skills == null || unitRef.Skills.Count == 0)
        {
            return result;
        }

        foreach (var skillId in unitRef.Skills)
        {
            if (!referenceManager.Skills.TryGet(skillId, out var skillRef) || skillRef == null)
            {
                continue;
            }

            var ability = skillRef.ToAbilityData();
            if (ability != null)
            {
                result.Add(ability);
            }
        }
        return result;
    }

    /// <summary>
    /// TowerReference를 기반으로 Tower 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="towerRef">타워 레퍼런스 데이터</param>
    /// <param name="id">타워 ID</param>
    /// <param name="faction">소속 진영</param>
    /// <param name="position">타워 위치</param>
    /// <param name="initialHP">초기 HP (null이면 MaxHP 사용)</param>
    /// <param name="isActivated">활성화 여부 (null이면 타입에 따라 자동 설정)</param>
    /// <returns>생성된 Tower 인스턴스</returns>
    public static Tower CreateTower(
        this TowerReference towerRef,
        int id,
        UnitFaction faction,
        Vector2 position,
        int? initialHP = null,
        bool? isActivated = null)
    {
        return new Tower
        {
            Id = id,
            Type = towerRef.Type,
            Faction = faction,
            Position = position,
            Radius = towerRef.Radius,
            AttackRange = towerRef.AttackRadius,
            MaxHP = towerRef.MaxHP,
            CurrentHP = initialHP ?? towerRef.MaxHP,
            Damage = towerRef.Damage,
            AttackSpeed = towerRef.AttackSpeed,
            CanTarget = towerRef.CanTarget,
            IsActivated = isActivated ?? (towerRef.Type == TowerType.Princess),
            AttackCooldown = 0f
        };
    }
}
