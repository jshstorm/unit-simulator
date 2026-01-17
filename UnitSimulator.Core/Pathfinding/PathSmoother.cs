using System.Numerics;

namespace UnitSimulator.Core.Pathfinding;

/// <summary>
/// A* 경로를 스무딩하여 불필요한 웨이포인트를 제거합니다.
/// Line-of-Sight 검사를 통해 직선 이동 가능한 구간을 찾습니다.
/// </summary>
public class PathSmoother
{
    private readonly PathfindingGrid _grid;

    public PathSmoother(PathfindingGrid grid)
    {
        _grid = grid;
    }

    /// <summary>
    /// 경로를 스무딩하여 불필요한 중간 웨이포인트를 제거합니다.
    /// </summary>
    /// <param name="originalPath">원본 A* 경로</param>
    /// <param name="enabled">스무딩 활성화 여부 (기본값: GameConstants.PATH_SMOOTHING_ENABLED)</param>
    /// <returns>스무딩된 경로 (null이면 원본 반환)</returns>
    public List<Vector2>? SmoothPath(List<Vector2>? originalPath, bool enabled = GameConstants.PATH_SMOOTHING_ENABLED)
    {
        if (!enabled || originalPath == null || originalPath.Count <= 2)
            return originalPath;

        var smoothed = new List<Vector2> { originalPath[0] };
        int current = 0;

        while (current < originalPath.Count - 1)
        {
            // 가능한 멀리 있는 웨이포인트까지 직선 이동 가능한지 확인
            int farthestVisible = current + 1;
            int maxSkip = Math.Min(current + GameConstants.PATH_SMOOTHING_MAX_SKIP, originalPath.Count - 1);

            for (int i = maxSkip; i > current + 1; i--)
            {
                if (HasLineOfSight(originalPath[current], originalPath[i]))
                {
                    farthestVisible = i;
                    break;
                }
            }

            smoothed.Add(originalPath[farthestVisible]);
            current = farthestVisible;
        }

        return smoothed;
    }

    /// <summary>
    /// 두 점 사이에 장애물 없이 직선 이동이 가능한지 확인합니다.
    /// Bresenham 라인 알고리즘을 사용하여 경로상의 모든 노드를 검사합니다.
    /// </summary>
    private bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        var fromNode = _grid.NodeFromWorldPoint(from);
        var toNode = _grid.NodeFromWorldPoint(to);

        if (fromNode == null || toNode == null)
            return false;

        return BresenhamLineWalkable(fromNode.X, fromNode.Y, toNode.X, toNode.Y);
    }

    /// <summary>
    /// Bresenham 라인 알고리즘으로 두 노드 사이의 모든 노드가 이동 가능한지 확인합니다.
    /// </summary>
    private bool BresenhamLineWalkable(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            var node = _grid.GetNode(x0, y0);
            if (node == null || !node.IsWalkable)
                return false;

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return true;
    }
}
