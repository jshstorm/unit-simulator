#pragma once

#include "CoreMinimal.h"

class FPathfindingGrid;

/**
 * Smooths A* paths by removing unnecessary waypoints
 * using Bresenham line-of-sight checks.
 * Ported from Pathfinding/PathSmoother.cs (104 lines)
 */
class UNITSIMCORE_API FPathSmoother
{
public:
	explicit FPathSmoother(FPathfindingGrid& InGrid);

	/**
	 * Smooth a path by skipping intermediate waypoints where
	 * line-of-sight exists. Modifies the array in-place.
	 * @param Path     Path to smooth (modified in-place)
	 * @param bEnabled Whether smoothing is enabled
	 */
	void SmoothPath(TArray<FVector2D>& Path, bool bEnabled = true);

private:
	FPathfindingGrid& Grid;

	/** Check line-of-sight between two world positions */
	bool HasLineOfSight(const FVector2D& From, const FVector2D& To) const;

	/** Bresenham line walk: returns true if all nodes on line are walkable */
	bool BresenhamLineWalkable(int32 X0, int32 Y0, int32 X1, int32 Y1) const;
};
