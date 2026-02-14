#pragma once

#include "CoreMinimal.h"

struct FUnit;

/**
 * Monitors unit path progress and determines when replanning is needed.
 * Static utility functions (no state).
 * Ported from Pathfinding/PathProgressMonitor.cs (103 lines)
 */
namespace PathProgressMonitor
{
	/**
	 * Check if a unit should replan its path.
	 * Triggers: stall, long avoidance, periodic interval.
	 */
	UNITSIMCORE_API bool ShouldReplan(const FUnit& Unit, int32 CurrentFrame);

	/**
	 * Update unit path progress tracking. Called each frame.
	 * @param Unit         The unit to update
	 * @param bIsAvoiding  Whether the unit is currently in avoidance
	 * @param bMadeProgress Whether the unit made waypoint progress this frame
	 */
	UNITSIMCORE_API void UpdateProgress(FUnit& Unit, bool bIsAvoiding, bool bMadeProgress);

	/**
	 * Called when a path replan occurs. Resets progress counters.
	 */
	UNITSIMCORE_API void OnReplan(FUnit& Unit, int32 CurrentFrame);

	/**
	 * Check if the unit made progress toward a waypoint.
	 * Compares previous/current position distances to the waypoint.
	 */
	UNITSIMCORE_API bool CheckProgress(const FUnit& Unit, const FVector2D& Waypoint);
}
