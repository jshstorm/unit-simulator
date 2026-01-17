using System.Numerics;
using UnitSimulator.Core.Pathfinding;

namespace UnitSimulator.Core.Towers;

/// <summary>
/// 타워 기반 정적 장애물을 제공합니다.
/// 각 타워의 충돌 반경을 이동 불가 영역으로 표시합니다.
/// </summary>
public class TowerObstacleProvider : IObstacleProvider
{
    private readonly IEnumerable<Tower> _towers;

    /// <summary>
    /// TowerObstacleProvider를 생성합니다.
    /// </summary>
    /// <param name="towers">장애물로 등록할 타워 목록</param>
    public TowerObstacleProvider(IEnumerable<Tower> towers)
    {
        _towers = towers ?? Enumerable.Empty<Tower>();
    }

    /// <summary>
    /// 타워에는 사각형 장애물이 없습니다.
    /// </summary>
    public IEnumerable<(Vector2 min, Vector2 max)> GetUnwalkableRects()
    {
        yield break;
    }

    /// <summary>
    /// 모든 타워의 충돌 반경을 이동 불가 원형 영역으로 반환합니다.
    /// </summary>
    public IEnumerable<(Vector2 center, float radius)> GetUnwalkableCircles()
    {
        foreach (var tower in _towers)
        {
            // 파괴된 타워는 장애물에서 제외하지 않음 (잔해로 남아있다고 가정)
            float effectiveRadius = tower.Radius + GameConstants.TOWER_COLLISION_PADDING;
            yield return (tower.Position, effectiveRadius);
        }
    }
}
