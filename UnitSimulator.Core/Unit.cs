using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ReferenceModels.Models.Enums;

namespace UnitSimulator;

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
    public Tower? TargetTower { get; set; }
    public int HP { get; set; }
    public UnitRole Role { get; }
    public float AttackRange { get; }
    public float AttackCooldown { get; set; }
    public bool IsDead { get; set; }
    public int Id { get; }
    public string UnitId { get; }
    public UnitFaction Faction { get; }
    public TargetPriority TargetPriority { get; set; } = TargetPriority.Nearest;
    public Vector2 CurrentDestination { get; set; } = Vector2.Zero;
    public Unit? AvoidanceThreat { get; set; }
    public string Label => $"{(Faction == UnitFaction.Friendly ? "F" : "E")}{Id}";
    public List<Tuple<Unit, int>> RecentAttacks { get; } = new();
    public Unit?[] AttackSlots { get; } = new Unit?[GameConstants.NUM_ATTACK_SLOTS];
    public int TakenSlotIndex { get; set; } = -1;
    public Vector2 AvoidanceTarget { get; set; } = Vector2.Zero;
    public bool HasAvoidanceTarget { get; set; }
    public int FramesSinceSlotEvaluation { get; set; }
    public int FramesSinceTargetEvaluation { get; set; }

    // Phase 2: Path Progress Tracking (Replan Triggers)
    /// <summary>
    /// 마지막 웨이포인트 진행 이후 경과 프레임 수
    /// </summary>
    public int FramesSinceLastWaypointProgress { get; set; }

    /// <summary>
    /// 회피 시작 이후 경과 프레임 수
    /// </summary>
    public int FramesSinceAvoidanceStart { get; set; }

    /// <summary>
    /// 마지막 경로 재계획 프레임
    /// </summary>
    public int LastReplanFrame { get; set; }

    /// <summary>
    /// 이전 프레임의 위치 (진행 추적용)
    /// </summary>
    public Vector2 PreviousPosition { get; set; }

    // Phase 1: Ground/Air Layer System
    /// <summary>
    /// 유닛의 이동 레이어 (Ground/Air)
    /// </summary>
    public MovementLayer Layer { get; }

    /// <summary>
    /// 유닛이 공격할 수 있는 대상 유형
    /// </summary>
    public TargetType CanTarget { get; }

    // Phase 2: Combat Mechanics
    /// <summary>
    /// 유닛이 보유한 능력 목록
    /// </summary>
    public List<AbilityData> Abilities { get; } = new();

    /// <summary>
    /// 최대 쉴드 HP (Shield 능력이 있는 경우)
    /// </summary>
    public int MaxShieldHP { get; private set; }

    /// <summary>
    /// 현재 쉴드 HP
    /// </summary>
    public int ShieldHP { get; set; }

    /// <summary>
    /// 기본 공격력 (Phase 2에서 추가)
    /// </summary>
    public int Damage { get; }

    /// <summary>
    /// 돌진 상태 (ChargeAttack 능력이 있는 경우)
    /// </summary>
    public ChargeState? ChargeState { get; private set; }

    private readonly List<Vector2> _avoidancePath = new();
    private int _avoidancePathIndex = 0;

    private readonly List<Vector2> _movementPath = new();
    private int _movementPathIndex = 0;

    public Unit(Vector2 position, float radius, float speed, float turnSpeed, UnitRole role, int hp, int id, UnitFaction faction,
        MovementLayer layer = MovementLayer.Ground, TargetType canTarget = TargetType.Ground,
        int damage = 1, List<AbilityData>? abilities = null, string unitId = "unknown",
        TargetPriority targetPriority = TargetPriority.Nearest)
    {
        Position = position;
        CurrentDestination = position;
        Radius = radius;
        Speed = speed;
        TurnSpeed = turnSpeed;
        Role = role;
        HP = hp;
        AttackRange = (role == UnitRole.Melee) ? radius * GameConstants.MELEE_RANGE_MULTIPLIER : radius * GameConstants.RANGED_RANGE_MULTIPLIER;
        AttackCooldown = 0;
        IsDead = false;
        Velocity = Vector2.Zero;
        UnitId = unitId;
        TargetPriority = targetPriority;
        Forward = Vector2.UnitX;
        Target = null;
        Id = id;
        Faction = faction;
        Layer = layer;
        CanTarget = canTarget;
        Damage = damage;

        // Phase 2: 능력 초기화
        if (abilities != null)
        {
            Abilities.AddRange(abilities);
            InitializeAbilities();
        }
    }

    /// <summary>
    /// 능력 데이터를 기반으로 유닛 상태 초기화
    /// </summary>
    private void InitializeAbilities()
    {
        foreach (var ability in Abilities)
        {
            switch (ability)
            {
                case ShieldData shield:
                    MaxShieldHP = shield.MaxShieldHP;
                    ShieldHP = shield.MaxShieldHP;
                    break;

                case ChargeAttackData charge:
                    ChargeState = new ChargeState();
                    break;
            }
        }
    }

    /// <summary>
    /// 돌진 상태 객체를 보장합니다.
    /// </summary>
    public ChargeState EnsureChargeState()
    {
        ChargeState ??= new ChargeState();
        return ChargeState;
    }

    /// <summary>
    /// 특정 타입의 능력을 가지고 있는지 확인
    /// </summary>
    public bool HasAbility(AbilityType type)
    {
        return Abilities.Any(a => a.Type == type);
    }

    /// <summary>
    /// 특정 타입의 능력 데이터 가져오기
    /// </summary>
    public T? GetAbility<T>() where T : AbilityData
    {
        return Abilities.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// 이 유닛이 지정된 대상을 공격할 수 있는지 확인합니다.
    /// </summary>
    public bool CanAttack(Unit target)
    {
        if (target == null || target.IsDead) return false;

        // 대상의 레이어에 따라 TargetType 확인
        TargetType targetLayer = target.Layer == MovementLayer.Air ? TargetType.Air : TargetType.Ground;
        return (CanTarget & targetLayer) != TargetType.None;
    }

    /// <summary>
    /// 이 유닛이 지정된 타워를 공격할 수 있는지 확인합니다.
    /// </summary>
    public bool CanAttackTower(Tower tower)
    {
        if (tower == null || tower.IsDestroyed) return false;

        if ((CanTarget & TargetType.Building) != TargetType.None)
        {
            return true;
        }

        return (CanTarget & TargetType.Ground) != TargetType.None;
    }

    /// <summary>
    /// 이 유닛이 지정된 대상과 같은 레이어에 있는지 확인합니다.
    /// (충돌 검사 등에 사용)
    /// </summary>
    public bool IsSameLayer(Unit other)
    {
        if (other == null) return false;
        return Layer == other.Layer;
    }

    public Vector2 GetSlotPosition(int slotIndex, float attackerRadius)
    {
        float angle = (2 * MathF.PI / GameConstants.NUM_ATTACK_SLOTS) * slotIndex;
        float distance = this.Radius + attackerRadius + 10f;
        return this.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    public int TryClaimSlot(Unit attacker)
    {
        for (int i = 0; i < GameConstants.NUM_ATTACK_SLOTS; i++)
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

        for (int i = 0; i < GameConstants.NUM_ATTACK_SLOTS; i++)
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
            if (Vector2.Distance(Position, target) <= GameConstants.AVOIDANCE_WAYPOINT_THRESHOLD)
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

    public void SetMovementPath(List<Vector2>? path)
    {
        _movementPath.Clear();
        if (path != null && path.Count > 0)
        {
            _movementPath.AddRange(path);
        }
        _movementPathIndex = 0;
    }

    public bool TryGetNextMovementWaypoint(out Vector2 waypoint)
    {
        if (_movementPathIndex < _movementPath.Count)
        {
            var target = _movementPath[_movementPathIndex];
            if (Vector2.Distance(Position, target) <= GameConstants.AVOIDANCE_WAYPOINT_THRESHOLD)
            {
                _movementPathIndex++;
                if (_movementPathIndex >= _movementPath.Count)
                {
                    waypoint = Vector2.Zero;
                    return false; // Path complete
                }
            }
            waypoint = _movementPath[_movementPathIndex];
            return true;
        }
        waypoint = Vector2.Zero;
        return false;
    }

    public void ClearMovementPath()
    {
        _movementPath.Clear();
        _movementPathIndex = 0;
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

    /// <summary>
    /// 피해를 받습니다. Shield가 있으면 Shield를 먼저 소모합니다.
    /// </summary>
    /// <param name="damage">받을 피해량</param>
    /// <returns>실제로 HP에 적용된 피해량 (Shield 소모 제외)</returns>
    public int TakeDamage(int damage = 1)
    {
        int remainingDamage = damage;
        int hpDamage = 0;

        // Phase 2: Shield를 먼저 소모
        if (ShieldHP > 0)
        {
            int shieldDamage = Math.Min(ShieldHP, remainingDamage);
            ShieldHP -= shieldDamage;
            remainingDamage -= shieldDamage;
        }

        // 남은 피해를 HP에 적용
        if (remainingDamage > 0)
        {
            hpDamage = Math.Min(HP, remainingDamage);
            HP = Math.Max(0, HP - remainingDamage);
        }

        if (HP <= 0 && !IsDead)
        {
            IsDead = true;
            Velocity = Vector2.Zero;
            Target?.ReleaseSlot(this);
        }

        return hpDamage;
    }

    /// <summary>
    /// 현재 유효 속도 (돌진 중이면 돌진 속도 적용)
    /// </summary>
    public float GetEffectiveSpeed()
    {
        if (ChargeState?.IsCharging == true)
        {
            var chargeData = GetAbility<ChargeAttackData>();
            if (chargeData != null)
            {
                return Speed * chargeData.SpeedMultiplier;
            }
        }
        return Speed;
    }

    /// <summary>
    /// 현재 공격 데미지 (돌진 완료 시 배율 적용)
    /// </summary>
    public int GetEffectiveDamage()
    {
        if (ChargeState?.IsCharged == true)
        {
            var chargeData = GetAbility<ChargeAttackData>();
            if (chargeData != null)
            {
                return (int)(Damage * chargeData.DamageMultiplier);
            }
        }
        return Damage;
    }

    /// <summary>
    /// 공격 수행 후 호출 (돌진 상태 소비 등)
    /// </summary>
    public void OnAttackPerformed()
    {
        ChargeState?.ConsumeCharge();
    }
}
