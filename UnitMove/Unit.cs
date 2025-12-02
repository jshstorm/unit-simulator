using System.Collections.Generic;
using System.Numerics;

namespace UnitSimulator;

public enum UnitRole { Melee, Ranged }
public enum UnitFaction { Friendly, Enemy }

public class Unit
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Vector2 Forward { get; set; }
    public float Radius { get; }
    public float Speed { get; }
    public float TurnSpeed { get; }
    public Unit? Target { get; set; }
    public int HP { get; set; }
    public UnitRole Role { get; }
    public float AttackRange { get; }
    public float AttackCooldown { get; set; }
    public bool IsDead { get; set; }
    public int Id { get; }
    public UnitFaction Faction { get; }
    public Vector2 CurrentDestination { get; set; } = Vector2.Zero;
    public Unit? AvoidanceThreat { get; set; }
    public string Label => $"{(Faction == UnitFaction.Friendly ? "F" : "E")}{Id}";
    public List<Tuple<Unit, int>> RecentAttacks { get; } = new();
    public Unit?[] AttackSlots { get; } = new Unit?[Constants.NUM_ATTACK_SLOTS];
    public int TakenSlotIndex { get; set; } = -1;
    public Vector2 AvoidanceTarget { get; set; } = Vector2.Zero;
    public bool HasAvoidanceTarget { get; set; }
    public int FramesSinceSlotEvaluation { get; set; }
    public int FramesSinceTargetEvaluation { get; set; }

    private readonly List<Vector2> _avoidancePath = new();
    private int _avoidancePathIndex = 0;

    public Unit(Vector2 position, float radius, float speed, float turnSpeed, UnitRole role, int hp, int id, UnitFaction faction)
    {
        Position = position;
        CurrentDestination = position;
        Radius = radius;
        Speed = speed;
        TurnSpeed = turnSpeed;
        Role = role;
        HP = hp;
        AttackRange = (role == UnitRole.Melee) ? radius * Constants.MELEE_RANGE_MULTIPLIER : radius * Constants.RANGED_RANGE_MULTIPLIER;
        AttackCooldown = 0;
        IsDead = false;
        Velocity = Vector2.Zero;
        Forward = Vector2.UnitX;
        Target = null;
        Id = id;
        Faction = faction;
    }

    public Vector2 GetSlotPosition(int slotIndex, float attackerRadius)
    {
        float angle = (2 * MathF.PI / Constants.NUM_ATTACK_SLOTS) * slotIndex;
        float distance = this.Radius + attackerRadius + 10f;
        return this.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    public int TryClaimSlot(Unit attacker)
    {
        for (int i = 0; i < Constants.NUM_ATTACK_SLOTS; i++)
        {
            if (AttackSlots[i] == null)
            {
                AttackSlots[i] = attacker;
                attacker.TakenSlotIndex = i;
                return i;
            }
        }
        return -1;
    }

    public int ClaimBestSlot(Unit attacker)
    {
        int bestIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < Constants.NUM_ATTACK_SLOTS; i++)
        {
            var occupant = AttackSlots[i];
            if (occupant != null && occupant != attacker) continue;

            float distance = Vector2.Distance(attacker.Position, GetSlotPosition(i, attacker.Radius));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        if (bestIndex != -1)
        {
            if (attacker.TakenSlotIndex != -1 && attacker.TakenSlotIndex != bestIndex && 
                attacker.TakenSlotIndex < AttackSlots.Length && AttackSlots[attacker.TakenSlotIndex] == attacker)
            {
                AttackSlots[attacker.TakenSlotIndex] = null;
            }
            AttackSlots[bestIndex] = attacker;
            attacker.TakenSlotIndex = bestIndex;
        }
        else
        {
            ReleaseSlot(attacker);
        }

        return bestIndex;
    }

    public void ReleaseSlot(Unit attacker)
    {
        if (attacker.TakenSlotIndex != -1 && attacker.TakenSlotIndex < AttackSlots.Length)
        {
            if (AttackSlots[attacker.TakenSlotIndex] == attacker)
            {
                AttackSlots[attacker.TakenSlotIndex] = null;
            }
            attacker.TakenSlotIndex = -1;
        }
    }

    public void SetAvoidancePath(List<Vector2> waypoints)
    {
        _avoidancePath.Clear();
        if (waypoints.Count == 0)
        {
            _avoidancePathIndex = 0;
            return;
        }
        _avoidancePath.AddRange(waypoints);
        _avoidancePathIndex = 0;
    }

    public bool TryGetNextAvoidanceWaypoint(out Vector2 waypoint)
    {
        while (_avoidancePathIndex < _avoidancePath.Count)
        {
            var target = _avoidancePath[_avoidancePathIndex];
            if (Vector2.Distance(Position, target) <= Constants.AVOIDANCE_WAYPOINT_THRESHOLD)
            {
                _avoidancePathIndex++;
                continue;
            }
            waypoint = target;
            return true;
        }
        waypoint = Vector2.Zero;
        return false;
    }

    public void ClearAvoidancePath()
    {
        _avoidancePath.Clear();
        _avoidancePathIndex = 0;
    }

    public void UpdateRotation()
    {
        if (Velocity.LengthSquared() < 0.001f) return;
        float targetAngle = MathF.Atan2(Velocity.Y, Velocity.X);
        float currentAngle = MathF.Atan2(Forward.Y, Forward.X);
        float angleDiff = targetAngle - currentAngle;
        while (angleDiff > MathF.PI) angleDiff -= 2 * MathF.PI;
        while (angleDiff < -MathF.PI) angleDiff += 2 * MathF.PI;
        float rotation = Math.Clamp(angleDiff, -TurnSpeed, TurnSpeed);
        Forward = Vector2.Transform(Forward, Matrix3x2.CreateRotation(rotation));
    }

    public void TakeDamage(int damage = 1)
    {
        HP = Math.Max(0, HP - damage);
        if (HP <= 0 && !IsDead)
        {
            IsDead = true;
            Velocity = Vector2.Zero;
            Target?.ReleaseSlot(this);
        }
    }
}
