using System.Linq;
using System.Numerics;

namespace UnitSimulator;

public class EnemyBehavior
{
    // Phase 2: 전투 시스템
    private readonly CombatSystem _combatSystem = new();

    public void UpdateEnemySquad(SimulatorCore sim, List<Unit> enemies, List<Unit> friendlies)
    {
        var livingFriendlies = friendlies.Where(f => !f.IsDead).ToList();
        if (!livingFriendlies.Any())
        {
            foreach (var enemy in enemies)
            {
                enemy.Velocity = Vector2.Zero;
                enemy.ClearMovementPath();
            }
            return;
        }

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead) continue;

            enemy.AttackCooldown = Math.Max(0, enemy.AttackCooldown - 1);

            if (enemy.HP <= 0)
            {
                enemy.IsDead = true;
                enemy.Velocity = Vector2.Zero;
                if (enemy.Target != null) enemy.Target.ReleaseSlot(enemy);
                continue;
            }

            UpdateEnemyTarget(enemy, livingFriendlies);
            UpdateEnemyMovement(sim, enemy, enemies, livingFriendlies);

            enemy.Position += enemy.Velocity;
            enemy.UpdateRotation();
        }
    }

    private void UpdateEnemyTarget(Unit enemy, List<Unit> livingFriendlies)
    {
        var previousTarget = enemy.Target;
        enemy.FramesSinceTargetEvaluation++;

        // Phase 1: 현재 타겟이 죽었거나 공격 불가능하면 새 타겟 필요
        bool needsTarget = enemy.Target == null || enemy.Target.IsDead || !enemy.CanAttack(enemy.Target);

        if (needsTarget)
        {
            if (enemy.Target != null) enemy.Target.ReleaseSlot(enemy);
            enemy.Target = SelectBestTarget(enemy, livingFriendlies);
            enemy.FramesSinceTargetEvaluation = 0;
        }
        else
        {
            var best = SelectBestTarget(enemy, livingFriendlies);
            if (best != null && best != enemy.Target)
            {
                var currentTarget = enemy.Target;
                float currentScore = currentTarget != null ? EvaluateTargetScore(enemy, currentTarget) : float.MaxValue;
                float bestScore = EvaluateTargetScore(enemy, best);
                bool intervalElapsed = enemy.FramesSinceTargetEvaluation >= GameConstants.TARGET_REEVALUATE_INTERVAL_FRAMES;
                bool clearlyBetter = bestScore + GameConstants.TARGET_SWITCH_MARGIN < currentScore;

                if (intervalElapsed || clearlyBetter)
                {
                    if (currentTarget != null) currentTarget.ReleaseSlot(enemy);
                    enemy.Target = best;
                    enemy.FramesSinceTargetEvaluation = 0;
                }
            }
        }

        if (enemy.Target != null && enemy.Target != previousTarget)
        {
            enemy.Target.ClaimBestSlot(enemy);
            enemy.FramesSinceSlotEvaluation = 0;
        }
        else if (enemy.Target == null)
        {
            enemy.FramesSinceSlotEvaluation = 0;
        }
    }

    private void UpdateEnemyMovement(SimulatorCore sim, Unit enemy, List<Unit> enemies, List<Unit> livingFriendlies)
    {
        if (enemy.Target == null)
        {
            enemy.ClearMovementPath();
            enemy.CurrentDestination = enemy.Position;
            enemy.Velocity = Vector2.Zero;
            enemy.ChargeState?.Reset();
            return;
        }

        // Phase 2: 돌진 상태 업데이트
        _combatSystem.UpdateChargeState(enemy, enemy.Target);

        enemy.FramesSinceSlotEvaluation++;
        var target = enemy.Target!;
        int slotIndex = enemy.TakenSlotIndex;
        Vector2 targetPosition;

        bool needsSlotRefresh = slotIndex == -1;
        if (!needsSlotRefresh)
        {
            var desiredSlotPos = target.GetSlotPosition(slotIndex, enemy.Radius);
            float slotOffset = Vector2.Distance(desiredSlotPos, enemy.Position);
            bool offsetTooLarge = slotOffset > GameConstants.SLOT_REEVALUATE_DISTANCE;
            bool intervalElapsed = enemy.FramesSinceSlotEvaluation >= GameConstants.SLOT_REEVALUATE_INTERVAL_FRAMES;
            needsSlotRefresh = offsetTooLarge || intervalElapsed;
        }

        if (needsSlotRefresh)
        {
            target.ClaimBestSlot(enemy);
            enemy.FramesSinceSlotEvaluation = 0;
            slotIndex = enemy.TakenSlotIndex;
        }

        if (slotIndex != -1)
        {
            targetPosition = target.GetSlotPosition(slotIndex, enemy.Radius);
        }
        else
        {
            Vector2 toTarget = target.Position - enemy.Position;
            Vector2 perpendicular = new(-toTarget.Y, toTarget.X);
            targetPosition = target.Position + MathUtils.SafeNormalize(perpendicular) * 200f;
        }

        float distanceToTargetCenter = Vector2.Distance(enemy.Position, target.Position);
        if (distanceToTargetCenter <= enemy.AttackRange)
        {
            enemy.Velocity = Vector2.Zero;
            enemy.ClearMovementPath();
            TryAttack(sim, enemy, target, livingFriendlies);
        }
        else
        {
            MoveUnit(sim, enemy, targetPosition, enemies, livingFriendlies);
        }
    }
    
    private void MoveUnit(SimulatorCore sim, Unit unit, Vector2 destination, List<Unit> enemies, List<Unit> friendlies)
    {
        bool needsNewPath = Vector2.Distance(unit.CurrentDestination, destination) > GameConstants.DESTINATION_THRESHOLD;
        if (needsNewPath)
        {
            var path = sim.Pathfinder?.FindPath(unit.Position, destination);
            unit.SetMovementPath(path);
            unit.CurrentDestination = destination;
        }

        if (unit.TryGetNextMovementWaypoint(out var waypoint))
        {
            Vector2 desiredDirection = waypoint - unit.Position;
            Vector2 desiredForward = MathUtils.SafeNormalize(desiredDirection);
            Vector2 separationVector = MathUtils.CalculateSeparationVector(unit, enemies, GameConstants.SEPARATION_RADIUS);
            
            var avoidanceCandidates = friendlies.Cast<Unit>().Concat(enemies.Where(e => e != unit && !e.IsDead)).ToList();
            Vector2 avoidance = AvoidanceSystem.PredictiveAvoidanceVector(unit, avoidanceCandidates, desiredForward, out var avoidTarget, out var isDetouring, out var avoidanceThreat);

            bool hasWaypoint = unit.TryGetNextAvoidanceWaypoint(out var avoidanceWaypoint);
            Vector2 steeringTarget = hasWaypoint ? avoidanceWaypoint : waypoint;
            bool hasDetour = hasWaypoint || isDetouring;

            if (!hasDetour)
            {
                unit.ClearAvoidancePath();
            }

            unit.HasAvoidanceTarget = hasDetour;
            unit.AvoidanceTarget = hasWaypoint ? steeringTarget : (isDetouring ? avoidTarget : Vector2.Zero);
            unit.AvoidanceThreat = hasDetour ? avoidanceThreat : null;

            Vector2 steeringDir = MathUtils.SafeNormalize(steeringTarget - unit.Position);
            Vector2 finalDir = MathUtils.SafeNormalize(steeringDir + separationVector + avoidance);
            // Phase 2: 유효 속도 사용 (돌진 중이면 돌진 속도 적용)
            unit.Velocity = finalDir * unit.GetEffectiveSpeed();
        }
        else
        {
            unit.Velocity = Vector2.Zero;
        }
    }

    private void TryAttack(SimulatorCore sim, Unit attacker, Unit target, List<Unit> allFriendlies)
    {
        if (target.IsDead) return;
        float distanceToTarget = Vector2.Distance(attacker.Position, target.Position);
        if (distanceToTarget <= attacker.AttackRange)
        {
            attacker.Velocity = Vector2.Zero;
            if (attacker.AttackCooldown <= 0)
            {
                // Phase 2: CombatSystem을 통한 공격 처리 (SplashDamage, ChargeAttack 적용)
                int damage = attacker.Damage > 0 ? attacker.GetEffectiveDamage() : GameConstants.ENEMY_ATTACK_DAMAGE;
                var result = _combatSystem.PerformAttack(attacker, target, allFriendlies);
                sim.ProcessAttackResult(attacker.Faction, result);
                attacker.AttackCooldown = GameConstants.ATTACK_COOLDOWN;
            }
        }
    }

    private Unit? SelectBestTarget(Unit enemy, List<Unit> candidates)
    {
        Unit? best = null;
        float bestScore = float.MaxValue;
        foreach (var candidate in candidates)
        {
            // Phase 1: 죽었거나 공격 불가능한 대상 제외
            if (candidate.IsDead || !enemy.CanAttack(candidate)) continue;
            float score = EvaluateTargetScore(enemy, candidate);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best;
    }

    private float EvaluateTargetScore(Unit enemy, Unit candidate)
    {
        float distance = Vector2.Distance(enemy.Position, candidate.Position);
        int occupied = candidate.AttackSlots.Count(s => s != null);
        float crowdPenalty = occupied * GameConstants.TARGET_CROWD_PENALTY_PER_ATTACKER;
        return distance + crowdPenalty;
    }
}
