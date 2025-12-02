using System.Numerics;
using System.Linq;

namespace UnitSimulator;

public class SquadBehavior
{
    private Unit? _squadTarget = null;
    private Vector2 _rallyPoint = Vector2.Zero;

    private readonly List<Vector2> _formationOffsets = new()
    {
        new(0, 0), new(0, 90), new(-80, -45), new(-80, 135)
    };

    public void UpdateFriendlySquad(List<Unit> friendlies, List<Unit> enemies, Vector2 mainTarget)
    {
        var livingEnemies = enemies.Where(e => !e.IsDead).ToList();

        if (livingEnemies.Any())
        {
            UpdateSquadTargetAndRallyPoint(friendlies, livingEnemies);
            var engagedUnits = DetermineEngagedUnits(friendlies, livingEnemies);

            if (engagedUnits.Count > 0)
            {
                UpdateCombatBehavior(friendlies, livingEnemies, engagedUnits);
            }

            if (engagedUnits.Count < friendlies.Count)
            {
                UpdateFormation(friendlies, engagedUnits);
            }
        }
        else
        {
            ResetSquadState(friendlies);
            MoveToMainTarget(friendlies, mainTarget);
        }
    }

    private void UpdateSquadTargetAndRallyPoint(List<Unit> friendlies, List<Unit> livingEnemies)
    {
        if (_squadTarget == null)
        {
            var leader = friendlies.FirstOrDefault();
            if (leader != null)
            {
                _squadTarget = livingEnemies.OrderBy(e => Vector2.Distance(leader.Position, e.Position)).FirstOrDefault();
                if (_squadTarget != null)
                {
                    Vector2 directionToTarget = Vector2.Normalize(_squadTarget.Position - leader.Position);
                    _rallyPoint = _squadTarget.Position - directionToTarget * Constants.RALLY_DISTANCE;
                }
            }
        }

        if (_squadTarget != null && _squadTarget.IsDead)
        {
            _squadTarget = null;
        }
    }


    private void UpdateFormation(List<Unit> friendlies, HashSet<Unit>? engagedUnits = null)
    {
        var leader = friendlies.FirstOrDefault();
        if (leader == null) return;

        bool leaderEngaged = engagedUnits?.Contains(leader) == true;
        Vector2 leaderTargetPosition = leaderEngaged ? leader.Position : _rallyPoint;

        if (!leaderEngaged)
        {
            leader.HasAvoidanceTarget = false;
            leader.AvoidanceTarget = Vector2.Zero;
            leader.AvoidanceThreat = null;
            leader.CurrentDestination = leaderTargetPosition;

            Vector2 toMain = leaderTargetPosition - leader.Position;
            leader.Velocity = toMain.Length() < 5f ? Vector2.Zero : MathUtils.SafeNormalize(toMain) * leader.Speed;
            leader.Position += leader.Velocity;
            leader.UpdateRotation();
        }

        for (int i = 1; i < friendlies.Count; i++)
        {
            var follower = friendlies[i];
            if (engagedUnits?.Contains(follower) == true) continue;

            var rotation = Matrix3x2.CreateRotation(MathF.Atan2(leader.Forward.Y, leader.Forward.X));
            var rotatedOffset = Vector2.Transform(_formationOffsets[i], rotation);
            var formationTarget = leader.Position + rotatedOffset;

            Vector2 toFormation = formationTarget - follower.Position;
            float distanceToSlot = toFormation.Length();

            follower.HasAvoidanceTarget = false;
            follower.AvoidanceTarget = Vector2.Zero;
            follower.AvoidanceThreat = null;
            follower.ClearAvoidancePath();
            follower.CurrentDestination = formationTarget;

            follower.Velocity = distanceToSlot < 3f ? Vector2.Zero : MathUtils.SafeNormalize(toFormation) * follower.Speed;
            follower.Position += follower.Velocity;
            follower.UpdateRotation();
        }
    }

    private HashSet<Unit> DetermineEngagedUnits(List<Unit> friendlies, List<Unit> livingEnemies)
    {
        var engaged = new HashSet<Unit>();
        foreach (var friendly in friendlies)
        {
            if (IsUnitReadyToEngage(friendly, livingEnemies))
            {
                engaged.Add(friendly);
            }
        }
        return engaged;
    }

    private bool IsUnitReadyToEngage(Unit friendly, List<Unit> livingEnemies)
    {
        if (!livingEnemies.Any()) return false;
        if (friendly.Target != null && !friendly.Target.IsDead) return true;

        float triggerDistance = friendly.AttackRange * Constants.ENGAGEMENT_TRIGGER_DISTANCE_MULTIPLIER;
        foreach (var enemy in livingEnemies)
        {
            if (Vector2.Distance(friendly.Position, enemy.Position) <= triggerDistance)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateCombatBehavior(List<Unit> friendlies, List<Unit> livingEnemies, HashSet<Unit>? engagedUnits = null)
    {
        foreach (var friendly in friendlies)
        {
            if (engagedUnits != null && !engagedUnits.Contains(friendly)) continue;

            UpdateUnitTarget(friendly, livingEnemies);
            UpdateCombat(friendly, livingEnemies, friendlies);
            friendly.Position += friendly.Velocity;
            friendly.UpdateRotation();
        }
    }

    private void UpdateUnitTarget(Unit friendly, List<Unit> livingEnemies)
    {
        if (friendly.Target != null && friendly.Target.IsDead)
        {
            friendly.Target.ReleaseSlot(friendly);
            friendly.Target = null;
        }

        friendly.AttackCooldown = Math.Max(0, friendly.AttackCooldown - 1);
        var previousTarget = friendly.Target;
        friendly.Target = livingEnemies.OrderBy(e => Vector2.Distance(friendly.Position, e.Position)).FirstOrDefault();
        
        if (previousTarget != null && previousTarget != friendly.Target) 
            previousTarget.ReleaseSlot(friendly);
        
        if (friendly.Target != null) 
            friendly.Target.ClaimBestSlot(friendly);
    }

    private void UpdateCombat(Unit friendly, List<Unit> livingEnemies, List<Unit> friendlies)
    {
        if (friendly.Target == null)
        {
            friendly.CurrentDestination = friendly.Position;
            friendly.HasAvoidanceTarget = false;
            friendly.AvoidanceTarget = Vector2.Zero;
            friendly.AvoidanceThreat = null;
            friendly.ClearAvoidancePath();
            return;
        }

        int slotIndex = friendly.TakenSlotIndex;
        Vector2 attackPosition = slotIndex != -1
            ? friendly.Target.GetSlotPosition(slotIndex, friendly.Radius)
            : friendly.Target.Position;

        float distanceToAttack = Vector2.Distance(friendly.Position, attackPosition);
        float distanceToTargetCenter = Vector2.Distance(friendly.Position, friendly.Target.Position);
        bool inAttackRange = distanceToAttack <= friendly.AttackRange || distanceToTargetCenter <= friendly.AttackRange;
        friendly.CurrentDestination = attackPosition;
        
        if (inAttackRange)
        {
            friendly.Velocity = Vector2.Zero;
            friendly.HasAvoidanceTarget = false;
            friendly.AvoidanceTarget = Vector2.Zero;
            friendly.AvoidanceThreat = null;
            friendly.ClearAvoidancePath();
            friendly.CurrentDestination = friendly.Position;
            if (friendly.AttackCooldown <= 0)
            {
                friendly.Target.TakeDamage(Constants.FRIENDLY_ATTACK_DAMAGE);
                friendly.AttackCooldown = Constants.ATTACK_COOLDOWN;
                friendly.RecentAttacks.Add(new Tuple<Unit, int>(friendly.Target, 5));
            }
        }
        else
        {
            Vector2 desiredDirection = attackPosition - friendly.Position;
            Vector2 desiredForward = MathUtils.SafeNormalize(desiredDirection);
            Vector2 separationVector = MathUtils.CalculateSeparationVector(friendly, friendlies, Constants.FRIENDLY_SEPARATION_RADIUS);
            var avoidanceCandidates = livingEnemies.Cast<Unit>().Concat(friendlies.Where(u => u != friendly)).ToList();
            Vector2 avoidance = AvoidanceSystem.PredictiveAvoidanceVector(friendly, avoidanceCandidates, desiredForward, out var avoidTarget, out var isDetouring, out var avoidanceThreat);

            bool hasWaypoint = friendly.TryGetNextAvoidanceWaypoint(out var waypoint);
            Vector2 steeringTarget = hasWaypoint ? waypoint : attackPosition;
            bool hasDetour = hasWaypoint || isDetouring;

            if (!hasDetour)
            {
                friendly.ClearAvoidancePath();
            }

            friendly.HasAvoidanceTarget = hasDetour;
            friendly.AvoidanceTarget = hasWaypoint ? steeringTarget : (isDetouring ? avoidTarget : Vector2.Zero);
            friendly.AvoidanceThreat = hasDetour ? avoidanceThreat : null;
            friendly.CurrentDestination = steeringTarget;

            Vector2 steeringDir = MathUtils.SafeNormalize(steeringTarget - friendly.Position);
            Vector2 finalDir = MathUtils.SafeNormalize(steeringDir + separationVector + avoidance);
            friendly.Velocity = finalDir * friendly.Speed;
        }
    }

    private void ResetSquadState(List<Unit> friendlies)
    {
        _squadTarget = null;

        foreach (var f in friendlies)
        {
            if (f.Target != null) f.Target.ReleaseSlot(f);
            f.TakenSlotIndex = -1;
            f.Target = null;
            f.HasAvoidanceTarget = false;
            f.AvoidanceTarget = Vector2.Zero;
            f.AvoidanceThreat = null;
            f.ClearAvoidancePath();
            f.CurrentDestination = f.Position;
        }
    }

    private void MoveToMainTarget(List<Unit> friendlies, Vector2 mainTarget)
    {
        var leader = friendlies.FirstOrDefault();
        if (leader == null) return;

        Vector2 toMain = mainTarget - leader.Position;
        leader.HasAvoidanceTarget = false;
        leader.AvoidanceTarget = Vector2.Zero;
        leader.AvoidanceThreat = null;
        leader.ClearAvoidancePath();
        leader.CurrentDestination = toMain.Length() < 5f ? leader.Position : mainTarget;
        leader.Velocity = toMain.Length() < 5f ? Vector2.Zero : MathUtils.SafeNormalize(toMain) * leader.Speed;
        leader.Position += leader.Velocity;
        leader.UpdateRotation();

        for (int i = 1; i < friendlies.Count; i++)
        {
            var follower = friendlies[i];
            var rotation = Matrix3x2.CreateRotation(MathF.Atan2(leader.Forward.Y, leader.Forward.X));
            var rotatedOffset = Vector2.Transform(_formationOffsets[i], rotation);
            var formationTarget = leader.Position + rotatedOffset;

            Vector2 toFormation = formationTarget - follower.Position;
            float distanceToSlot = toFormation.Length();

            follower.HasAvoidanceTarget = false;
            follower.AvoidanceTarget = Vector2.Zero;
            follower.AvoidanceThreat = null;
            follower.CurrentDestination = formationTarget;

            follower.Velocity = distanceToSlot < 3f ? Vector2.Zero : MathUtils.SafeNormalize(toFormation) * follower.Speed;
            follower.Position += follower.Velocity;
            follower.UpdateRotation();
        }
    }
}
