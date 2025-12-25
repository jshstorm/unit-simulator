using System.Numerics;
using System.Linq;
using System.Collections.Generic;

namespace UnitSimulator;

/// <summary>
/// Phase 2: 전투 관련 로직을 처리하는 시스템
/// SplashDamage, ChargeAttack, DeathSpawn 등의 능력을 처리
/// </summary>
public class CombatSystem
{
    /// <summary>
    /// 공격을 수행하고 능력을 적용합니다.
    /// </summary>
    /// <param name="attacker">공격자</param>
    /// <param name="target">주 타겟</param>
    /// <param name="allEnemies">스플래시 대상이 될 수 있는 모든 적</param>
    /// <returns>공격 결과 (사망 유닛, 스폰 요청)</returns>
    public AttackResult PerformAttack(Unit attacker, Unit target, List<Unit> allEnemies)
    {
        var result = new AttackResult();

        if (target == null || target.IsDead) return result;

        int damage = attacker.GetEffectiveDamage();

        // 주 타겟에 피해
        bool wasAlive = !target.IsDead;
        target.TakeDamage(damage);
        if (wasAlive && target.IsDead)
        {
            HandleDeath(target, attacker, allEnemies, result);
        }

        // SplashDamage 처리
        var splashData = attacker.GetAbility<SplashDamageData>();
        if (splashData != null)
        {
            ApplySplashDamage(attacker, target, damage, splashData, allEnemies, result);
        }

        // 공격 후 처리 (돌진 상태 소비 등)
        attacker.OnAttackPerformed();

        return result;
    }

    /// <summary>
    /// 스플래시 데미지를 적용합니다.
    /// </summary>
    private void ApplySplashDamage(Unit attacker, Unit mainTarget, int baseDamage, SplashDamageData splashData, List<Unit> allEnemies, AttackResult result)
    {
        foreach (var enemy in allEnemies)
        {
            if (enemy == mainTarget || enemy.IsDead) continue;
            if (!attacker.CanAttack(enemy)) continue;

            float distance = Vector2.Distance(mainTarget.Position, enemy.Position);
            if (distance > splashData.Radius) continue;

            // 거리에 따른 피해 감소 계산
            int splashDamage = baseDamage;
            if (splashData.DamageFalloff > 0)
            {
                float falloffFactor = 1f - (distance / splashData.Radius) * splashData.DamageFalloff;
                splashDamage = (int)(baseDamage * Math.Max(0, falloffFactor));
            }

            if (splashDamage > 0)
            {
                bool wasAlive = !enemy.IsDead;
                enemy.TakeDamage(splashDamage);
                if (wasAlive && enemy.IsDead)
                {
                    HandleDeath(enemy, attacker, allEnemies, result);
                }
            }
        }
    }

    /// <summary>
    /// 유닛의 돌진 상태를 업데이트합니다.
    /// </summary>
    public void UpdateChargeState(Unit unit, Unit? target)
    {
        if (unit.ChargeState == null) return;

        var chargeData = unit.GetAbility<ChargeAttackData>();
        if (chargeData == null) return;

        // 타겟이 없거나 죽었으면 돌진 리셋
        if (target == null || target.IsDead)
        {
            unit.ChargeState.Reset();
            return;
        }

        float distanceToTarget = Vector2.Distance(unit.Position, target.Position);

        // 돌진 시작 조건: 타겟과의 거리가 트리거 거리 이상
        if (!unit.ChargeState.IsCharging && distanceToTarget >= chargeData.TriggerDistance)
        {
            unit.ChargeState.StartCharge(unit.Position, chargeData.RequiredChargeDistance);
        }

        // 돌진 중이면 거리 업데이트
        if (unit.ChargeState.IsCharging)
        {
            unit.ChargeState.UpdateChargeDistance(unit.Position);

            // 공격 범위 내에 들어오면 돌진 완료 상태 유지 (공격 시 소비됨)
            if (distanceToTarget <= unit.AttackRange)
            {
                // 이미 IsCharged가 설정되어 있으면 다음 공격에서 배율 적용
            }
        }
    }

    /// <summary>
    /// 유닛 사망 시 DeathSpawn 및 DeathDamage 처리
    /// </summary>
    /// <param name="deadUnit">사망한 유닛</param>
    /// <param name="allEnemies">DeathDamage 대상이 될 수 있는 적 유닛들</param>
    /// <returns>생성된 유닛 목록 (호출자가 시뮬레이터에 추가해야 함)</returns>
    public (List<UnitSpawnRequest> spawns, List<Unit> killedByDeathDamage) ProcessDeath(Unit deadUnit, List<Unit> allEnemies)
    {
        var spawns = new List<UnitSpawnRequest>();
        var killedUnits = new List<Unit>();

        // DeathSpawn 처리
        var deathSpawn = deadUnit.GetAbility<DeathSpawnData>();
        if (deathSpawn != null && deathSpawn.SpawnCount > 0)
        {
            for (int i = 0; i < deathSpawn.SpawnCount; i++)
            {
                // 원형으로 배치
                float angle = (2 * MathF.PI / deathSpawn.SpawnCount) * i;
                Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * deathSpawn.SpawnRadius;
                Vector2 spawnPos = deadUnit.Position + offset;

                spawns.Add(new UnitSpawnRequest
                {
                    UnitId = deathSpawn.SpawnUnitId,
                    Position = spawnPos,
                    Faction = deadUnit.Faction,
                    HP = deathSpawn.SpawnUnitHP
                });
            }
        }

        // DeathDamage 처리
        var deathDamage = deadUnit.GetAbility<DeathDamageData>();
        if (deathDamage != null && deathDamage.Damage > 0)
        {
            foreach (var enemy in allEnemies)
            {
                if (enemy.IsDead) continue;

                float distance = Vector2.Distance(deadUnit.Position, enemy.Position);
                if (distance <= deathDamage.Radius)
                {
                    bool wasAlive = !enemy.IsDead;
                    enemy.TakeDamage(deathDamage.Damage);

                    // 넉백 적용
                    if (deathDamage.KnockbackDistance > 0 && !enemy.IsDead)
                    {
                        Vector2 knockbackDir = Vector2.Normalize(enemy.Position - deadUnit.Position);
                        enemy.Position += knockbackDir * deathDamage.KnockbackDistance;
                    }

                    if (wasAlive && enemy.IsDead)
                    {
                        killedUnits.Add(enemy);
                    }
                }
            }
        }

        return (spawns, killedUnits);
    }

    private void HandleDeath(Unit dead, Unit attacker, List<Unit> opposingUnits, AttackResult result)
    {
        result.KilledUnits.Add(dead);

        // DeathSpawn / DeathDamage 처리
        var (spawns, killedByDeathDamage) = ProcessDeath(dead, opposingUnits);
        if (spawns.Any())
        {
            result.SpawnRequests.AddRange(spawns);
        }

        foreach (var killed in killedByDeathDamage)
        {
            if (result.KilledUnits.Contains(killed)) continue;
            result.KilledUnits.Add(killed);
        }
    }
}

/// <summary>
/// 유닛 생성 요청 데이터
/// </summary>
public class UnitSpawnRequest
{
    public string UnitId { get; init; } = "";
    public Vector2 Position { get; init; }
    public UnitFaction Faction { get; init; }
    public int HP { get; init; }
}

/// <summary>
/// 공격 결과 데이터
/// </summary>
public class AttackResult
{
    public List<Unit> KilledUnits { get; } = new();
    public List<UnitSpawnRequest> SpawnRequests { get; } = new();
}
