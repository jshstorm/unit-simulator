#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"

struct FUnit;

/**
 * Predictive collision avoidance system.
 * Static utility functions for steering units around each other.
 * Ported from AvoidanceSystem.cs (191 lines)
 */
namespace AvoidanceSystem
{
	/** Risk entry for avoidance calculation */
	struct FAvoidanceRisk
	{
		FVector2D RelPos;
		float Distance;
		float CombinedRadius;
		int32 ThreatIndex;
	};

	/**
	 * Compute predictive avoidance vector for a mover.
	 * @param Mover             The unit being steered
	 * @param MoverIndex        Index of the mover in the units array
	 * @param Others            All units array
	 * @param OtherCount        Number of units
	 * @param DesiredDirection   Desired movement direction
	 * @param OutAvoidanceTarget World-space avoidance waypoint
	 * @param bOutIsDetouring   Whether the unit is detouring
	 * @param OutThreatIndex    Index of primary avoidance threat (-1 = none)
	 * @return Weighted avoidance direction
	 */
	UNITSIMCORE_API FVector2D PredictiveAvoidanceVector(
		FUnit& Mover,
		int32 MoverIndex,
		const FUnit* Others,
		int32 OtherCount,
		const FVector2D& DesiredDirection,
		FVector2D& OutAvoidanceTarget,
		bool& bOutIsDetouring,
		int32& OutThreatIndex);

	/** Build segmented avoidance waypoint path */
	TArray<FVector2D> BuildSegmentedAvoidancePath(
		const FUnit& Mover,
		const FVector2D& BaseDir,
		const FAvoidanceRisk& PrimaryRisk);

	/** Check if a direction is clear of all risks */
	bool IsDirectionClear(
		const FVector2D& Direction,
		const TArray<FAvoidanceRisk>& Risks);

	/** Safe normalize: returns ZeroVector if input too small */
	FVector2D SafeNormalize(const FVector2D& V);

	/** Rotate a 2D vector by angle (radians) */
	FVector2D Rotate(const FVector2D& V, float Angle);

	/** Try get first collision time between two units */
	bool TryGetFirstCollision(
		const FUnit& A,
		const FUnit& B,
		float& OutT,
		float& OutDistance);
}
