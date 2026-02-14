#pragma once

#include "CoreMinimal.h"
#include "Pathfinding/IObstacleProvider.h"

/**
 * Terrain-based static obstacle provider.
 * Returns river non-bridge areas as unwalkable rectangles.
 * Ported from Terrain/TerrainObstacleProvider.cs (50 lines)
 */
class UNITSIMCORE_API FTerrainObstacleProvider : public IObstacleProvider
{
public:
	virtual TArray<FObstacleRect> GetUnwalkableRects() const override;
	virtual TArray<FObstacleCircle> GetUnwalkableCircles() const override;
};
