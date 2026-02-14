#include "Pathfinding/PathProgressMonitor.h"
#include "Units/Unit.h"
#include "GameConstants.h"

bool PathProgressMonitor::ShouldReplan(const FUnit& Unit, int32 CurrentFrame)
{
	// Cooldown check
	const int32 FramesSinceReplan = CurrentFrame - Unit.LastReplanFrame;
	if (FramesSinceReplan < UnitSimConstants::REPLAN_COOLDOWN_FRAMES)
	{
		return false;
	}

	// Trigger 1: Waypoint progress stall
	if (Unit.FramesSinceLastWaypointProgress >= UnitSimConstants::REPLAN_STALL_THRESHOLD)
	{
		return true;
	}

	// Trigger 2: Long avoidance state
	if (Unit.FramesSinceAvoidanceStart >= UnitSimConstants::REPLAN_AVOIDANCE_THRESHOLD)
	{
		return true;
	}

	// Trigger 3: Periodic replan (long-distance paths)
	if (FramesSinceReplan >= UnitSimConstants::REPLAN_PERIODIC_INTERVAL)
	{
		return true;
	}

	return false;
}

void PathProgressMonitor::UpdateProgress(FUnit& Unit, bool bIsAvoiding, bool bMadeProgress)
{
	// Waypoint progress tracking
	if (bMadeProgress)
	{
		Unit.FramesSinceLastWaypointProgress = 0;
	}
	else
	{
		++Unit.FramesSinceLastWaypointProgress;
	}

	// Avoidance tracking
	if (bIsAvoiding)
	{
		++Unit.FramesSinceAvoidanceStart;
	}
	else
	{
		Unit.FramesSinceAvoidanceStart = 0;
	}

	// Update previous position
	Unit.PreviousPosition = Unit.Position;
}

void PathProgressMonitor::OnReplan(FUnit& Unit, int32 CurrentFrame)
{
	Unit.LastReplanFrame = CurrentFrame;
	Unit.FramesSinceLastWaypointProgress = 0;
	Unit.FramesSinceAvoidanceStart = 0;
}

bool PathProgressMonitor::CheckProgress(const FUnit& Unit, const FVector2D& Waypoint)
{
	const float PreviousDistance = FVector2D::Distance(Unit.PreviousPosition, Waypoint);
	const float CurrentDistance = FVector2D::Distance(Unit.Position, Waypoint);
	const float DistanceMoved = FVector2D::Distance(Unit.PreviousPosition, Unit.Position);

	return DistanceMoved >= UnitSimConstants::WAYPOINT_PROGRESS_THRESHOLD * 0.5f
		&& CurrentDistance < PreviousDistance;
}
