using System.Linq;
using System.Numerics;
using UnitSimulator.Core.Pathfinding;

namespace UnitSimulator;

public class EnemyBehavior
{
    // Phase 2: 전투 시스템
    private readonly CombatSystem _combatSystem = new();

    public void UpdateEnemySquad(
        SimulatorCore sim,
        List<Unit> enemies,
        List<Unit> friendlies,
        List<Tower> friendlyTowers,
        FrameEvents events)
    {
        var livingFriendlies = friendlies.Where(f => !f.IsDead).ToList();
        var livingTowers = friendlyTowers.Where(t => !t.IsDestroyed).ToList();
        if (!livingFriendlies.Any() && livingTowers.Count == 0)
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

            UpdateEnemyTarget(enemy, livingFriendlies, livingTowers);
            UpdateEnemyMovement(sim, enemy, enemies, livingFriendlies, livingTowers, events);

            enemy.Position += enemy.Velocity;
            enemy.UpdateRotation();
        }
    }

    private void UpdateEnemyTarget(Unit enemy, List<Unit> livingFriendlies, List<Tower> friendlyTowers)
    {
        var previousTarget = enemy.Target;
        enemy.FramesSinceTargetEvaluation++;
        if (enemy.TargetTower != null && enemy.TargetTower.IsDestroyed)
        {
            enemy.TargetTower = null;
        }

        var selection = TowerTargetingRules.SelectTarget(enemy, livingFriendlies, friendlyTowers);
        if (selection.towerTarget != null)
        {
            if (enemy.Target != null) enemy.Target.ReleaseSlot(enemy);
            enemy.Target = null;
            enemy.TakenSlotIndex = -1;
            enemy.TargetTower = selection.towerTarget;
            enemy.FramesSinceTargetEvaluation = 0;
            return;
        }

        if (enemy.TargetTower != null)
        {
            enemy.TargetTower = null;
        }

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

    private void UpdateEnemyMovement(
        SimulatorCore sim,
        Unit enemy,
        List<Unit> enemies,
        List<Unit> livingFriendlies,
        List<Tower> friendlyTowers,
        FrameEvents events)
    {
        if (enemy.TargetTower != null)
        {
            UpdateTowerCombat(sim, enemy, enemy.TargetTower, enemies, livingFriendlies, events);
            return;
        }

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
            TryAttack(enemy, target, livingFriendlies, events);
        }
        else
        {
            MoveUnit(sim, enemy, targetPosition, enemies, livingFriendlies);
        }
    }

    private void UpdateTowerCombat(
        SimulatorCore sim,
        Unit enemy,
        Tower targetTower,
        List<Unit> enemies,
        List<Unit> livingFriendlies,
        FrameEvents events)
    {
        float distanceToTarget = Vector2.Distance(enemy.Position, targetTower.Position);
        if (distanceToTarget <= enemy.AttackRange)
        {
            enemy.Velocity = Vector2.Zero;
            enemy.ClearMovementPath();
            enemy.ClearAvoidancePath();
            if (enemy.AttackCooldown <= 0)
            {
                events.AddDamageToTower(enemy, targetTower, enemy.GetEffectiveDamage());
                enemy.AttackCooldown = GameConstants.ATTACK_COOLDOWN;
            }
        }
        else
        {
            MoveUnit(sim, enemy, targetTower.Position, enemies, livingFriendlies);
        }
    }
    
    private void MoveUnit(SimulatorCore sim, Unit unit, Vector2 destination, List<Unit> enemies, List<Unit> friendlies)
    {
        var adjustedDestination = sim.TerrainSystem.GetAdjustedDestination(unit, destination);

        // 경로 재계획 필요 여부 확인 (목적지 변경 또는 재계획 트리거)
        bool destinationChanged = Vector2.Distance(unit.CurrentDestination, adjustedDestination) > GameConstants.DESTINATION_THRESHOLD;
        bool shouldReplan = PathProgressMonitor.ShouldReplan(unit, sim.CurrentFrame);
        bool needsNewPath = destinationChanged || shouldReplan;

        if (needsNewPath)
        {
            var path = sim.Pathfinder?.FindPath(unit.Position, adjustedDestination);
            unit.SetMovementPath(path);
            unit.CurrentDestination = adjustedDestination;
            PathProgressMonitor.OnReplan(unit, sim.CurrentFrame);
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

            // 경로 진행 상태 업데이트
            bool madeProgress = PathProgressMonitor.CheckProgress(unit, waypoint);
            PathProgressMonitor.UpdateProgress(unit, hasDetour, madeProgress);
        }
        else
        {
            unit.Velocity = Vector2.Zero;
            // 정지 상태에서도 진행 추적 리셋
            PathProgressMonitor.UpdateProgress(unit, false, true);
        }

        unit.Position += unit.Velocity;
        unit.UpdateRotation();
    }

    private void TryAttack(Unit attacker, Unit target, List<Unit> allFriendlies, FrameEvents events)
    {
        if (target.IsDead) return;
        float distanceToTarget = Vector2.Distance(attacker.Position, target.Position);
        if (distanceToTarget <= attacker.AttackRange)
        {
            attacker.Velocity = Vector2.Zero;
            if (attacker.AttackCooldown <= 0)
            {
                // 2-Phase Update: 이벤트만 수집, 실제 데미지는 Phase 2에서 적용
                _combatSystem.CollectAttackEvents(attacker, target, allFriendlies, events);
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
