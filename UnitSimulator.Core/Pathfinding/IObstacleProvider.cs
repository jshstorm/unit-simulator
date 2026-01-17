using System.Numerics;

namespace UnitSimulator.Core.Pathfinding;

/// <summary>
/// 정적 장애물 정보를 PathfindingGrid에 제공하는 인터페이스.
/// </summary>
public interface IObstacleProvider
{
    /// <summary>
    /// 이동 불가 사각형 영역 목록을 반환합니다.
    /// </summary>
    /// <returns>(min, max) 좌표 쌍의 열거형</returns>
    IEnumerable<(Vector2 min, Vector2 max)> GetUnwalkableRects();

    /// <summary>
    /// 이동 불가 원형 영역 목록을 반환합니다.
    /// </summary>
    /// <returns>(center, radius) 쌍의 열거형</returns>
    IEnumerable<(Vector2 center, float radius)> GetUnwalkableCircles();
}
