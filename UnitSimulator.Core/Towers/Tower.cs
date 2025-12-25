using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// 게임 내 타워를 표현합니다.
/// 각 진영은 Princess Tower 2개와 King Tower 1개를 보유합니다.
/// </summary>
public class Tower
{
    // ════════════════════════════════════════════════════════════════════════
    // 식별 및 기본 정보
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 타워 고유 ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 타워 유형 (Princess / King)
    /// </summary>
    public TowerType Type { get; init; }

    /// <summary>
    /// 소속 진영
    /// </summary>
    public UnitFaction Faction { get; init; }

    // ════════════════════════════════════════════════════════════════════════
    // 위치 및 크기
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 타워 위치 (중심점)
    /// </summary>
    public Vector2 Position { get; init; }

    /// <summary>
    /// 타워 충돌/타겟팅 반경
    /// </summary>
    public float Radius { get; init; }

    /// <summary>
    /// 공격 사거리
    /// </summary>
    public float AttackRange { get; init; }

    // ════════════════════════════════════════════════════════════════════════
    // 스탯
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 최대 HP
    /// </summary>
    public int MaxHP { get; init; }

    /// <summary>
    /// 현재 HP
    /// </summary>
    public int CurrentHP { get; set; }

    /// <summary>
    /// 공격력
    /// </summary>
    public int Damage { get; init; }

    /// <summary>
    /// 공격 속도 (초당 공격 횟수)
    /// </summary>
    public float AttackSpeed { get; init; }

    /// <summary>
    /// 공격 가능 대상 타입 (Ground, Air, GroundAndAir)
    /// </summary>
    public TargetType CanTarget { get; init; }

    // ════════════════════════════════════════════════════════════════════════
    // 상태
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 타워가 파괴되었는지 여부
    /// </summary>
    public bool IsDestroyed => CurrentHP <= 0;

    /// <summary>
    /// King Tower 활성화 여부 (Princess Tower는 항상 true)
    /// King Tower는 Princess 파괴 또는 직접 피해 시 활성화됨
    /// </summary>
    public bool IsActivated { get; set; }

    /// <summary>
    /// 현재 공격 쿨다운 (초)
    /// </summary>
    public float AttackCooldown { get; set; }

    /// <summary>
    /// 현재 타겟 유닛
    /// </summary>
    public Unit? CurrentTarget { get; set; }

    // ════════════════════════════════════════════════════════════════════════
    // 메서드
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 타워에 피해를 입힙니다.
    /// </summary>
    /// <param name="amount">피해량</param>
    public void TakeDamage(int amount)
    {
        if (IsDestroyed) return;

        CurrentHP -= amount;
        if (CurrentHP < 0) CurrentHP = 0;

        // King Tower가 피해를 받으면 활성화
        if (Type == TowerType.King && !IsActivated)
        {
            IsActivated = true;
        }
    }

    /// <summary>
    /// 타워가 유닛을 공격할 수 있는지 확인합니다.
    /// </summary>
    public bool CanAttack(Unit target)
    {
        if (target == null || target.IsDead) return false;
        if (IsDestroyed) return false;
        if (Type == TowerType.King && !IsActivated) return false;

        // 레이어 체크
        TargetType targetLayer = target.Layer == MovementLayer.Air
            ? TargetType.Air
            : TargetType.Ground;

        if ((CanTarget & targetLayer) == TargetType.None) return false;

        // 사거리 체크
        float distance = Vector2.Distance(Position, target.Position);
        return distance <= AttackRange;
    }

    /// <summary>
    /// 타워가 공격을 수행할 준비가 되었는지 확인합니다.
    /// </summary>
    public bool IsReadyToAttack => AttackCooldown <= 0 && !IsDestroyed;

    /// <summary>
    /// 공격 수행 후 쿨다운을 리셋합니다.
    /// </summary>
    public void OnAttackPerformed()
    {
        AttackCooldown = 1f / AttackSpeed;
    }

    /// <summary>
    /// 쿨다운을 업데이트합니다.
    /// </summary>
    /// <param name="deltaTime">경과 시간 (초)</param>
    public void UpdateCooldown(float deltaTime)
    {
        if (AttackCooldown > 0)
        {
            AttackCooldown -= deltaTime;
        }
    }
}
