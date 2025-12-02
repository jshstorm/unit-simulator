using System.Linq;
using System.Numerics;

namespace UnitSimulator;

public class EnemyBehavior
{
    public void UpdateEnemySquad(List<Unit> enemies, List<Unit> friendlies)
    {
        var livingFriendlies = friendlies.Where(f => !f.IsDead).ToList();
        if (!livingFriendlies.Any()) return;

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
            UpdateEnemyMovement(enemy, enemies, livingFriendlies);
            
            enemy.Position += enemy.Velocity;
            enemy.UpdateRotation();
        }
    }

    private void UpdateEnemyTarget(Unit enemy, List<Unit> livingFriendlies)
    {
        var previousTarget = enemy.Target;
        enemy.FramesSinceTargetEvaluation++;

        bool needsTarget = enemy.Target == null || enemy.Target.IsDead;

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
                bool intervalElapsed = enemy.FramesSinceTargetEvaluation >= Constants.TARGET_REEVALUATE_INTERVAL_FRAMES;
                bool clearlyBetter = bestScore + Constants.TARGET_SWITCH_MARGIN < currentScore;

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

    private void UpdateEnemyMovement(Unit enemy, List<Unit> enemies, List<Unit> livingFriendlies)
    {
        if (enemy.Target == null)
        {
            enemy.CurrentDestination = enemy.Position;
            enemy.HasAvoidanceTarget = false;
            enemy.AvoidanceTarget = Vector2.Zero;
            enemy.AvoidanceThreat = null;
            enemy.ClearAvoidancePath();
            enemy.FramesSinceSlotEvaluation = 0;
            return;
        }

        enemy.FramesSinceSlotEvaluation++;
        var target = enemy.Target!;
        int slotIndex = enemy.TakenSlotIndex;
        Vector2 targetPosition;

        bool needsSlotRefresh = slotIndex == -1;
        if (!needsSlotRefresh)
        {
            var desiredSlotPos = target.GetSlotPosition(slotIndex, enemy.Radius);
            float slotOffset = Vector2.Distance(desiredSlotPos, enemy.Position);
            bool offsetTooLarge = slotOffset > Constants.SLOT_REEVALUATE_DISTANCE;
            bool intervalElapsed = enemy.FramesSinceSlotEvaluation >= Constants.SLOT_REEVALUATE_INTERVAL_FRAMES;
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
            if (Vector2.Distance(enemy.Position, targetPosition) < enemy.Radius)
            {
                enemy.Velocity = Vector2.Zero;
                enemy.CurrentDestination = enemy.Position;
                enemy.HasAvoidanceTarget = false;
                enemy.AvoidanceTarget = Vector2.Zero;
                enemy.AvoidanceThreat = null;
                enemy.ClearAvoidancePath();
                return;
            }
        }
        else
        {
            Vector2 toTarget = target.Position - enemy.Position;
            Vector2 perpendicular = new(-toTarget.Y, toTarget.X);
            targetPosition = target.Position + MathUtils.SafeNormalize(perpendicular) * 200f;
        }

        Vector2 desiredDirection = targetPosition - enemy.Position;
        Vector2 desiredForward = MathUtils.SafeNormalize(desiredDirection);
        Vector2 separationVector = MathUtils.CalculateSeparationVector(enemy, enemies, Constants.SEPARATION_RADIUS);
        var avoidanceCandidates = livingFriendlies.Cast<Unit>().Concat(enemies.Where(e => e != enemy && !e.IsDead)).ToList();
        Vector2 friendlyAvoidance = AvoidanceSystem.PredictiveAvoidanceVector(enemy, avoidanceCandidates, desiredForward, out var avoidTarget, out var isDetouring, out var avoidanceThreat);

        bool hasWaypoint = enemy.TryGetNextAvoidanceWaypoint(out var waypoint);
        Vector2 steeringTarget = hasWaypoint ? waypoint : targetPosition;
        bool hasDetour = hasWaypoint || isDetouring;

        if (!hasDetour)
        {
            enemy.ClearAvoidancePath();
        }

        enemy.HasAvoidanceTarget = hasDetour;
        enemy.AvoidanceTarget = hasWaypoint ? steeringTarget : (isDetouring ? avoidTarget : Vector2.Zero);
        enemy.AvoidanceThreat = hasDetour ? avoidanceThreat : null;
        enemy.CurrentDestination = steeringTarget;

        Vector2 steeringDir = MathUtils.SafeNormalize(steeringTarget - enemy.Position);
        Vector2 finalDir = MathUtils.SafeNormalize(steeringDir + separationVector + friendlyAvoidance);
        enemy.Velocity = finalDir * enemy.Speed;

        TryAttack(enemy, target);
    }

    private void TryAttack(Unit attacker, Unit target)
    {
        if (target.IsDead) return;
        float distanceToTarget = Vector2.Distance(attacker.Position, target.Position);
        if (distanceToTarget <= attacker.AttackRange)
        {
            attacker.Velocity = Vector2.Zero;
            if (attacker.AttackCooldown <= 0)
            {
                target.TakeDamage(Constants.ENEMY_ATTACK_DAMAGE);
                attacker.AttackCooldown = Constants.ATTACK_COOLDOWN;
            }
        }
    }

    private Unit? SelectBestTarget(Unit enemy, List<Unit> candidates)
    {
        Unit? best = null;
        float bestScore = float.MaxValue;
        foreach (var candidate in candidates)
        {
            if (candidate.IsDead) continue;
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
        float crowdPenalty = occupied * Constants.TARGET_CROWD_PENALTY_PER_ATTACKER;
        return distance + crowdPenalty;
    }
}
