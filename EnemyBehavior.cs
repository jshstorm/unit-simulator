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

        if (enemy.Target == null || enemy.Target.IsDead)
        {
            if (enemy.Target != null) enemy.Target.ReleaseSlot(enemy);
            enemy.Target = livingFriendlies.OrderBy(f => Vector2.Distance(enemy.Position, f.Position)).FirstOrDefault();
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
        Vector2 friendlyAvoidance = AvoidanceSystem.PredictiveAvoidanceVector(enemy, livingFriendlies, desiredForward, out var avoidTarget, out var isDetouring, out var avoidanceThreat);

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
    }
}