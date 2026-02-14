#pragma once

#include "CoreMinimal.h"

/**
 * Obstacle rectangle defined by min/max corners.
 */
struct FObstacleRect
{
	FVector2D Min;
	FVector2D Max;
};

/**
 * Obstacle circle defined by center and radius.
 */
struct FObstacleCircle
{
	FVector2D Center;
	float Radius;
};

/**
 * Interface providing static obstacle information to the pathfinding grid.
 * Ported from Pathfinding/IObstacleProvider.cs
 */
class UNITSIMCORE_API IObstacleProvider
{
public:
	virtual ~IObstacleProvider() = default;

	/** Returns unwalkable rectangular areas as (min, max) pairs */
	virtual TArray<FObstacleRect> GetUnwalkableRects() const = 0;

	/** Returns unwalkable circular areas as (center, radius) pairs */
	virtual TArray<FObstacleCircle> GetUnwalkableCircles() const = 0;
};
