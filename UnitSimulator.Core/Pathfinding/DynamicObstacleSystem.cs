using System.Numerics;

namespace UnitSimulator.Core.Pathfinding;

/// <summary>
/// 동적 장애물(유닛 밀집 영역)을 관리합니다.
/// 밀집도 기반으로 일시적으로 노드를 이동 불가로 표시합니다.
/// </summary>
public class DynamicObstacleSystem
{
    private readonly PathfindingGrid _grid;
    private readonly HashSet<(int x, int y)> _dynamicBlockedNodes = new();
    private readonly HashSet<(int x, int y)> _staticBlockedNodes = new();
    private bool _staticBlocksRecorded = false;

    public DynamicObstacleSystem(PathfindingGrid grid)
    {
        _grid = grid;
    }

    /// <summary>
    /// 유닛 밀집도를 기반으로 동적 장애물을 업데이트합니다.
    /// 프레임당 한 번 호출되어야 합니다.
    /// </summary>
    /// <param name="units">현재 살아있는 모든 유닛</param>
    public void UpdateDynamicObstacles(IEnumerable<Unit> units)
    {
        // 정적 장애물 위치 기록 (최초 1회)
        if (!_staticBlocksRecorded)
        {
            RecordStaticBlocks();
            _staticBlocksRecorded = true;
        }

        // 이전 동적 장애물 제거
        ClearDynamicBlocks();

        // Ground 유닛만 고려 (Air 유닛은 지상 경로에 영향 없음)
        var groundUnits = units.Where(u => !u.IsDead && u.Layer == MovementLayer.Ground);

        // 셀별 유닛 수 집계
        var cellCounts = new Dictionary<(int, int), int>();
        foreach (var unit in groundUnits)
        {
            var node = _grid.NodeFromWorldPoint(unit.Position);
            if (node != null)
            {
                var key = (node.X, node.Y);
                cellCounts[key] = cellCounts.GetValueOrDefault(key) + 1;
            }
        }

        // 밀집 셀을 임시 장애물로 표시
        foreach (var (cell, count) in cellCounts)
        {
            if (count >= GameConstants.DYNAMIC_OBSTACLE_DENSITY_THRESHOLD)
            {
                // 정적 장애물이 아닌 경우에만 동적 장애물로 표시
                if (!_staticBlockedNodes.Contains(cell))
                {
                    _grid.SetWalkable(cell.Item1, cell.Item2, false);
                    _dynamicBlockedNodes.Add(cell);
                }
            }
        }
    }

    /// <summary>
    /// 정적 장애물 위치를 기록합니다.
    /// 동적 장애물 해제 시 정적 장애물을 복원하지 않도록 합니다.
    /// </summary>
    private void RecordStaticBlocks()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                var node = _grid.GetNode(x, y);
                if (node != null && !node.IsWalkable)
                {
                    _staticBlockedNodes.Add((x, y));
                }
            }
        }
    }

    /// <summary>
    /// 동적 장애물을 제거합니다.
    /// 정적 장애물은 유지됩니다.
    /// </summary>
    public void ClearDynamicBlocks()
    {
        foreach (var (x, y) in _dynamicBlockedNodes)
        {
            // 정적 장애물이 아닌 경우에만 복원
            if (!_staticBlockedNodes.Contains((x, y)))
            {
                _grid.SetWalkable(x, y, true);
            }
        }
        _dynamicBlockedNodes.Clear();
    }

    /// <summary>
    /// 현재 동적 장애물로 표시된 노드 수를 반환합니다.
    /// </summary>
    public int DynamicBlockCount => _dynamicBlockedNodes.Count;
}
