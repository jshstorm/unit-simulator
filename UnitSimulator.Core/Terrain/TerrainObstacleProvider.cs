using System.Numerics;
using UnitSimulator.Core.Pathfinding;

namespace UnitSimulator.Core.Terrain;

/// <summary>
/// 지형 기반 정적 장애물을 제공합니다.
/// 강의 비-다리 영역을 이동 불가로 표시합니다.
/// </summary>
public class TerrainObstacleProvider : IObstacleProvider
{
    /// <summary>
    /// 강의 비-다리 영역을 이동 불가 사각형으로 반환합니다.
    /// 다리 영역은 제외됩니다.
    /// </summary>
    public IEnumerable<(Vector2 min, Vector2 max)> GetUnwalkableRects()
    {
        float margin = GameConstants.RIVER_OBSTACLE_MARGIN;

        // 강 Y 범위: 2400 ~ 2700
        // 왼쪽 다리: X 400 ~ 800
        // 오른쪽 다리: X 2400 ~ 2800

        // 1. 왼쪽 강 영역 (X: 0 ~ 400)
        yield return (
            new Vector2(0, MapLayout.RiverYMin + margin),
            new Vector2(MapLayout.LeftBridgeXMin - margin, MapLayout.RiverYMax - margin)
        );

        // 2. 중앙 강 영역 (X: 800 ~ 2400)
        yield return (
            new Vector2(MapLayout.LeftBridgeXMax + margin, MapLayout.RiverYMin + margin),
            new Vector2(MapLayout.RightBridgeXMin - margin, MapLayout.RiverYMax - margin)
        );

        // 3. 오른쪽 강 영역 (X: 2800 ~ 3200)
        yield return (
            new Vector2(MapLayout.RightBridgeXMax + margin, MapLayout.RiverYMin + margin),
            new Vector2(MapLayout.MapWidth, MapLayout.RiverYMax - margin)
        );
    }

    /// <summary>
    /// 지형에는 원형 장애물이 없습니다.
    /// </summary>
    public IEnumerable<(Vector2 center, float radius)> GetUnwalkableCircles()
    {
        yield break;
    }
}
