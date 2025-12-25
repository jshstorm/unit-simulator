using System.Collections.Generic;

namespace UnitSimulator;

/// <summary>
/// 유닛 타입의 기본 스탯을 정의하는 클래스.
/// DeathSpawn 등에서 SpawnUnitId로 참조되어 실제 유닛 생성 시 사용됩니다.
/// </summary>
public class UnitDefinition
{
    /// <summary>유닛 정의 ID (예: "golemite", "skeleton")</summary>
    public required string UnitId { get; init; }

    /// <summary>표시 이름</summary>
    public string DisplayName { get; init; } = "";

    // === 기본 스탯 ===

    /// <summary>최대 HP</summary>
    public int MaxHP { get; init; } = 100;

    /// <summary>공격력</summary>
    public int Damage { get; init; } = 10;

    /// <summary>공격 사거리</summary>
    public float AttackRange { get; init; } = 30f;

    /// <summary>이동 속도</summary>
    public float MoveSpeed { get; init; } = 4.0f;

    /// <summary>회전 속도</summary>
    public float TurnSpeed { get; init; } = 0.1f;

    /// <summary>충돌 반경</summary>
    public float Radius { get; init; } = 20f;

    // === 유닛 유형 ===

    /// <summary>역할 (Melee, Ranged 등)</summary>
    public UnitRole Role { get; init; } = UnitRole.Melee;

    /// <summary>이동 레이어 (Ground, Air)</summary>
    public MovementLayer Layer { get; init; } = MovementLayer.Ground;

    /// <summary>공격 가능 대상</summary>
    public TargetType CanTarget { get; init; } = TargetType.Ground;

    // === 특수 능력 ===

    /// <summary>유닛이 보유한 능력 목록</summary>
    public List<AbilityData> Abilities { get; init; } = new();

    /// <summary>
    /// 이 정의를 기반으로 Unit 인스턴스를 생성합니다.
    /// </summary>
    public Unit CreateUnit(int id, UnitFaction faction, System.Numerics.Vector2 position)
    {
        return new Unit(
            position: position,
            radius: Radius,
            speed: MoveSpeed,
            turnSpeed: TurnSpeed,
            role: Role,
            hp: MaxHP,
            id: id,
            faction: faction,
            layer: Layer,
            canTarget: CanTarget,
            damage: Damage,
            abilities: Abilities,
            unitId: UnitId
        );
    }
}
