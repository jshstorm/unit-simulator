using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// 타워의 행동을 관리합니다.
/// 타겟팅, 공격, 쿨다운 처리 등을 담당합니다.
/// </summary>
public class TowerBehavior
{
    /// <summary>
    /// 모든 타워를 업데이트합니다.
    /// </summary>
    /// <param name="towers">업데이트할 타워 목록</param>
    /// <param name="enemies">적 유닛 목록</param>
    /// <param name="events">이벤트 수집 컨테이너</param>
    /// <param name="deltaTime">경과 시간 (초)</param>
    public void UpdateTowers(
        IEnumerable<Tower> towers,
        IEnumerable<Unit> enemies,
        FrameEvents events,
        float deltaTime)
    {
        var livingEnemies = enemies.Where(e => !e.IsDead).ToList();

        foreach (var tower in towers)
        {
            UpdateTower(tower, livingEnemies, events, deltaTime);
        }
    }

    /// <summary>
    /// 단일 타워를 업데이트합니다.
    /// </summary>
    private void UpdateTower(
        Tower tower,
        List<Unit> enemies,
        FrameEvents events,
        float deltaTime)
    {
        // 파괴된 타워는 처리하지 않음
        if (tower.IsDestroyed) return;

        // King Tower는 활성화되지 않으면 공격하지 않음
        if (tower.Type == TowerType.King && !tower.IsActivated) return;

        // 쿨다운 업데이트
        tower.UpdateCooldown(deltaTime);

        // 타겟 검증 및 재선정
        ValidateAndUpdateTarget(tower, enemies);

        // 공격 처리
        ProcessAttack(tower, events);
    }

    /// <summary>
    /// 타워의 타겟을 검증하고 필요시 재선정합니다.
    /// </summary>
    private void ValidateAndUpdateTarget(Tower tower, List<Unit> enemies)
    {
        // 현재 타겟이 유효한지 확인
        if (tower.CurrentTarget != null)
        {
            if (tower.CurrentTarget.IsDead || !tower.CanAttack(tower.CurrentTarget))
            {
                tower.CurrentTarget = null;
            }
        }

        // 타겟이 없으면 새로 선정
        if (tower.CurrentTarget == null)
        {
            tower.CurrentTarget = FindNearestTarget(tower, enemies);
        }
    }

    /// <summary>
    /// 타워 공격 범위 내에서 가장 가까운 적을 찾습니다.
    /// </summary>
    private Unit? FindNearestTarget(Tower tower, List<Unit> enemies)
    {
        return enemies
            .Where(e => tower.CanAttack(e))
            .OrderBy(e => Vector2.Distance(tower.Position, e.Position))
            .FirstOrDefault();
    }

    /// <summary>
    /// 타워 공격을 처리합니다.
    /// </summary>
    private void ProcessAttack(Tower tower, FrameEvents events)
    {
        // 공격 준비가 안 됐으면 스킵
        if (!tower.IsReadyToAttack) return;

        // 타겟이 없으면 스킵
        if (tower.CurrentTarget == null) return;

        // 공격 이벤트 추가
        events.AddTowerDamage(tower, tower.CurrentTarget, tower.Damage);

        // 쿨다운 리셋
        tower.OnAttackPerformed();
    }

    /// <summary>
    /// 두 진영의 모든 타워를 업데이트합니다.
    /// </summary>
    /// <param name="session">게임 세션</param>
    /// <param name="friendlyUnits">아군 유닛 목록</param>
    /// <param name="enemyUnits">적군 유닛 목록</param>
    /// <param name="events">이벤트 수집 컨테이너</param>
    /// <param name="deltaTime">경과 시간 (초)</param>
    public void UpdateAllTowers(
        GameSession session,
        List<Unit> friendlyUnits,
        List<Unit> enemyUnits,
        FrameEvents events,
        float deltaTime)
    {
        // Friendly 타워는 Enemy 유닛을 공격
        UpdateTowers(session.FriendlyTowers, enemyUnits, events, deltaTime);

        // Enemy 타워는 Friendly 유닛을 공격
        UpdateTowers(session.EnemyTowers, friendlyUnits, events, deltaTime);
    }
}
