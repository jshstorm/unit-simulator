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

    // Phase 2: 전투 시스템
    private readonly CombatSystem _combatSystem = new();

    public void UpdateFriendlySquad(SimulatorCore sim, List<Unit> friendlies, List<Unit> enemies, Vector2 mainTarget)
    {
        var livingEnemies = enemies.Where(e => !e.IsDead).ToList();

        if (livingEnemies.Any())
        {
            UpdateSquadTargetAndRallyPoint(friendlies, livingEnemies);
            var engagedUnits = DetermineEngagedUnits(friendlies, livingEnemies);

            if (engagedUnits.Count > 0)
            {
                UpdateCombatBehavior(sim, friendlies, livingEnemies, engagedUnits);
            }

            if (engagedUnits.Count < friendlies.Count)
            {
                UpdateFormation(sim, friendlies, engagedUnits);
            }
        }
        else
        {
            ResetSquadState(friendlies);
            MoveToMainTarget(sim, friendlies, mainTarget);
        }
    }

    private void UpdateSquadTargetAndRallyPoint(List<Unit> friendlies, List<Unit> livingEnemies)
    {
        if (_squadTarget == null)
        {
            var leader = friendlies.FirstOrDefault();
            if (leader != null)
            {
                // Phase 1: 공격 가능한 적만 타겟팅
                _squadTarget = livingEnemies
                    .Where(e => leader.CanAttack(e))
                    .OrderBy(e => Vector2.Distance(leader.Position, e.Position))
                    .FirstOrDefault();
                if (_squadTarget != null)
                {
                    Vector2 directionToTarget = Vector2.Normalize(_squadTarget.Position - leader.Position);
                    _rallyPoint = _squadTarget.Position - directionToTarget * GameConstants.RALLY_DISTANCE;
                }
            }
        }

        if (_squadTarget != null && _squadTarget.IsDead)
        {
            _squadTarget = null;
        }
    }


    private void UpdateFormation(SimulatorCore sim, List<Unit> friendlies, HashSet<Unit>? engagedUnits = null)
    {
        var leader = friendlies.FirstOrDefault();
        if (leader == null) return;

        bool leaderEngaged = engagedUnits?.Contains(leader) == true;
        Vector2 leaderTargetPosition = leaderEngaged ? leader.Position : _rallyPoint;

        if (!leaderEngaged)
        {
            MoveUnit(sim, leader, leaderTargetPosition, friendlies, null);
        }

        for (int i = 1; i < friendlies.Count; i++)
        {
            var follower = friendlies[i];
            if (engagedUnits?.Contains(follower) == true) continue;

            var rotation = Matrix3x2.CreateRotation(MathF.Atan2(leader.Forward.Y, leader.Forward.X));
            var rotatedOffset = Vector2.Transform(_formationOffsets[i], rotation);
            var formationTarget = leader.Position + rotatedOffset;

            MoveUnit(sim, follower, formationTarget, friendlies, null);
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
        if (friendly.Target != null && !friendly.Target.IsDead && friendly.CanAttack(friendly.Target)) return true;

        float triggerDistance = friendly.AttackRange * GameConstants.ENGAGEMENT_TRIGGER_DISTANCE_MULTIPLIER;
        foreach (var enemy in livingEnemies)
        {
            // Phase 1: 공격 가능한 적만 교전 트리거
            if (friendly.CanAttack(enemy) && Vector2.Distance(friendly.Position, enemy.Position) <= triggerDistance)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateCombatBehavior(SimulatorCore sim, List<Unit> friendlies, List<Unit> livingEnemies, HashSet<Unit>? engagedUnits = null)
    {
        foreach (var friendly in friendlies)
        {
            if (engagedUnits != null && !engagedUnits.Contains(friendly)) continue;

            UpdateUnitTarget(friendly, livingEnemies);
            UpdateCombat(sim, friendly, livingEnemies, friendlies);
            friendly.Position += friendly.Velocity;
            friendly.UpdateRotation();
        }
    }

    private void UpdateUnitTarget(Unit friendly, List<Unit> livingEnemies)
    {
        if (friendly.Target != null && (friendly.Target.IsDead || !friendly.CanAttack(friendly.Target)))
        {
            friendly.Target.ReleaseSlot(friendly);
            friendly.Target = null;
        }

        friendly.AttackCooldown = Math.Max(0, friendly.AttackCooldown - 1);
        var previousTarget = friendly.Target;

        // Phase 1: 공격 가능한 적 중 가장 가까운 적 선택
        friendly.Target = livingEnemies
            .Where(e => friendly.CanAttack(e))
            .OrderBy(e => Vector2.Distance(friendly.Position, e.Position))
            .FirstOrDefault();

        if (previousTarget != null && previousTarget != friendly.Target)
            previousTarget.ReleaseSlot(friendly);

        if (friendly.Target != null)
            friendly.Target.ClaimBestSlot(friendly);
    }

    private void UpdateCombat(SimulatorCore sim, Unit friendly, List<Unit> livingEnemies, List<Unit> friendlies)
    {
        if (friendly.Target == null)
        {
            friendly.ClearMovementPath();
            friendly.CurrentDestination = friendly.Position;
            friendly.Velocity = Vector2.Zero;
            friendly.ChargeState?.Reset();
            return;
        }

        // Phase 2: 돌진 상태 업데이트
        _combatSystem.UpdateChargeState(friendly, friendly.Target);

        int slotIndex = friendly.TakenSlotIndex;
        Vector2 attackPosition = slotIndex != -1
            ? friendly.Target.GetSlotPosition(slotIndex, friendly.Radius)
            : friendly.Target.Position;

        float distanceToTargetCenter = Vector2.Distance(friendly.Position, friendly.Target.Position);
        bool inAttackRange = distanceToTargetCenter <= friendly.AttackRange;

        if (inAttackRange)
        {
            friendly.Velocity = Vector2.Zero;
            friendly.ClearMovementPath();
            friendly.ClearAvoidancePath();
            friendly.CurrentDestination = friendly.Position;
            if (friendly.AttackCooldown <= 0)
            {
                // Phase 2: CombatSystem을 통한 공격 처리 (SplashDamage, ChargeAttack 적용)
                int damage = friendly.Damage > 0 ? friendly.GetEffectiveDamage() : GameConstants.FRIENDLY_ATTACK_DAMAGE;
                var result = _combatSystem.PerformAttack(friendly, friendly.Target, livingEnemies);
                sim.ProcessAttackResult(friendly.Faction, result);
                friendly.AttackCooldown = GameConstants.ATTACK_COOLDOWN;
                friendly.RecentAttacks.Add(new Tuple<Unit, int>(friendly.Target, 5));
            }
        }
        else
        {
            MoveUnit(sim, friendly, attackPosition, friendlies, livingEnemies);
        }
    }

    private void MoveUnit(SimulatorCore sim, Unit unit, Vector2 destination, List<Unit> friendlies, List<Unit>? enemies)
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
            Vector2 separationVector = MathUtils.CalculateSeparationVector(unit, friendlies, GameConstants.FRIENDLY_SEPARATION_RADIUS);
            
            var avoidanceCandidates = enemies != null 
                ? enemies.Cast<Unit>().Concat(friendlies.Where(u => u != unit)).ToList()
                : friendlies.Where(u => u != unit).ToList();

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
        
        unit.Position += unit.Velocity;
        unit.UpdateRotation();
    }

    private void ResetSquadState(List<Unit> friendlies)
    {
        _squadTarget = null;

        foreach (var f in friendlies)
        {
            if (f.Target != null) f.Target.ReleaseSlot(f);
            f.TakenSlotIndex = -1;
            f.Target = null;
            f.ClearMovementPath();
            f.ClearAvoidancePath();
            f.CurrentDestination = f.Position;
        }
    }

    private void MoveToMainTarget(SimulatorCore sim, List<Unit> friendlies, Vector2 mainTarget)
    {
        var leader = friendlies.FirstOrDefault();
        if (leader == null) return;

        MoveUnit(sim, leader, mainTarget, friendlies, null);

        for (int i = 1; i < friendlies.Count; i++)
        {
            var follower = friendlies[i];
            var rotation = Matrix3x2.CreateRotation(MathF.Atan2(leader.Forward.Y, leader.Forward.X));
            var rotatedOffset = Vector2.Transform(_formationOffsets[i], rotation);
            var formationTarget = leader.Position + rotatedOffset;

            MoveUnit(sim, follower, formationTarget, friendlies, null);
        }
    }
}
