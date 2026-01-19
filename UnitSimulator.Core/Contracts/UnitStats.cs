using ReferenceModels.Models.Enums;

namespace UnitSimulator.Core.Contracts;

/// <summary>
/// 유닛의 런타임 스탯 데이터.
/// ReferenceModels의 UnitReference에서 변환되어 SimulatorCore에서 사용됩니다.
/// </summary>
public sealed class UnitStats
{
    /// <summary>표시 이름</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>최대 HP</summary>
    public int HP { get; init; }

    /// <summary>기본 공격력</summary>
    public int Damage { get; init; }

    /// <summary>이동 속도</summary>
    public float MoveSpeed { get; init; }

    /// <summary>회전 속도 (라디안/프레임)</summary>
    public float TurnSpeed { get; init; }

    /// <summary>공격 사거리</summary>
    public float AttackRange { get; init; }

    /// <summary>충돌 반경</summary>
    public float Radius { get; init; }

    /// <summary>초당 공격 횟수</summary>
    public float AttackSpeed { get; init; }

    /// <summary>유닛 역할</summary>
    public UnitRole Role { get; init; }

    /// <summary>이동 레이어 (Ground/Air)</summary>
    public MovementLayer Layer { get; init; }

    /// <summary>공격 가능 대상</summary>
    public TargetType CanTarget { get; init; }

    /// <summary>타겟 우선순위</summary>
    public TargetPriority TargetPriority { get; init; }

    /// <summary>공격 방식</summary>
    public AttackType AttackType { get; init; }

    /// <summary>기본 쉴드 HP</summary>
    public int ShieldHP { get; init; }

    /// <summary>배치 시 생성 수량 (Swarm 유닛용)</summary>
    public int SpawnCount { get; init; } = 1;

    /// <summary>보유 스킬 ID 목록</summary>
    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 기본값으로 초기화된 UnitStats 생성
    /// </summary>
    public static UnitStats Default => new()
    {
        DisplayName = "Unknown",
        HP = 100,
        Damage = 10,
        MoveSpeed = 4.0f,
        TurnSpeed = 0.1f,
        AttackRange = 30f,
        Radius = 20f,
        AttackSpeed = 1.0f,
        Role = UnitRole.Melee,
        Layer = MovementLayer.Ground,
        CanTarget = TargetType.Ground,
        TargetPriority = TargetPriority.Nearest,
        AttackType = AttackType.Melee,
        ShieldHP = 0,
        SpawnCount = 1,
        Skills = Array.Empty<string>()
    };
}
