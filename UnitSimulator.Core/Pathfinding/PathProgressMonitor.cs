using System.Numerics;

namespace UnitSimulator.Core.Pathfinding;

/// <summary>
/// 유닛의 경로 진행 상태를 모니터링하고 재경로 필요 여부를 판단합니다.
/// </summary>
public static class PathProgressMonitor
{
    /// <summary>
    /// 유닛이 경로를 재계획해야 하는지 확인합니다.
    /// </summary>
    /// <param name="unit">검사할 유닛</param>
    /// <param name="currentFrame">현재 프레임</param>
    /// <returns>재계획이 필요하면 true</returns>
    public static bool ShouldReplan(Unit unit, int currentFrame)
    {
        // 쿨다운 중에는 재계획하지 않음
        int framesSinceReplan = currentFrame - unit.LastReplanFrame;
        if (framesSinceReplan < GameConstants.REPLAN_COOLDOWN_FRAMES)
            return false;

        // Trigger 1: 웨이포인트 진행 정체 (stall detection)
        if (unit.FramesSinceLastWaypointProgress >= GameConstants.REPLAN_STALL_THRESHOLD)
            return true;

        // Trigger 2: 장기 회피 상태
        if (unit.FramesSinceAvoidanceStart >= GameConstants.REPLAN_AVOIDANCE_THRESHOLD)
            return true;

        // Trigger 3: 주기적 재계획 (장거리 경로)
        if (framesSinceReplan >= GameConstants.REPLAN_PERIODIC_INTERVAL)
            return true;

        return false;
    }

    /// <summary>
    /// 유닛의 경로 진행 상태를 업데이트합니다.
    /// 매 프레임 MoveUnit 호출 시 사용합니다.
    /// </summary>
    /// <param name="unit">업데이트할 유닛</param>
    /// <param name="isAvoiding">현재 회피 중인지 여부</param>
    /// <param name="madeProgress">웨이포인트 방향으로 진행했는지 여부</param>
    public static void UpdateProgress(Unit unit, bool isAvoiding, bool madeProgress)
    {
        // 웨이포인트 진행 추적
        if (madeProgress)
        {
            unit.FramesSinceLastWaypointProgress = 0;
        }
        else
        {
            unit.FramesSinceLastWaypointProgress++;
        }

        // 회피 상태 추적
        if (isAvoiding)
        {
            unit.FramesSinceAvoidanceStart++;
        }
        else
        {
            unit.FramesSinceAvoidanceStart = 0;
        }

        // 이전 위치 업데이트
        unit.PreviousPosition = unit.Position;
    }

    /// <summary>
    /// 경로가 재계획되었을 때 호출합니다.
    /// 관련 카운터를 리셋합니다.
    /// </summary>
    /// <param name="unit">재계획된 유닛</param>
    /// <param name="currentFrame">현재 프레임</param>
    public static void OnReplan(Unit unit, int currentFrame)
    {
        unit.LastReplanFrame = currentFrame;
        unit.FramesSinceLastWaypointProgress = 0;
        unit.FramesSinceAvoidanceStart = 0;
    }

    /// <summary>
    /// 유닛이 웨이포인트 방향으로 진행했는지 확인합니다.
    /// </summary>
    /// <param name="unit">검사할 유닛</param>
    /// <param name="waypoint">현재 목표 웨이포인트</param>
    /// <returns>진행했으면 true</returns>
    public static bool CheckProgress(Unit unit, Vector2 waypoint)
    {
        // 이전 위치와 현재 위치의 차이가 임계값 이상이고
        // 웨이포인트 방향으로 가까워졌는지 확인
        float previousDistance = Vector2.Distance(unit.PreviousPosition, waypoint);
        float currentDistance = Vector2.Distance(unit.Position, waypoint);

        float distanceMoved = Vector2.Distance(unit.PreviousPosition, unit.Position);

        // 최소 이동량 이상 움직였고, 웨이포인트에 가까워졌으면 진행으로 판단
        return distanceMoved >= GameConstants.WAYPOINT_PROGRESS_THRESHOLD * 0.5f
            && currentDistance < previousDistance;
    }
}
