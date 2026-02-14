#pragma once

#include "CoreMinimal.h"
#include "Pathfinding/IObstacleProvider.h"

struct FTower;

/**
 * Tower-based static obstacle provider.
 * Returns tower collision circles as unwalkable areas.
 * Ported from Towers/TowerObstacleProvider.cs (43 lines)
 */
class UNITSIMCORE_API FTowerObstacleProvider : public IObstacleProvider
{
public:
	/**
	 * @param InTowers  Pointer to tower array
	 * @param InCount   Number of towers
	 */
	FTowerObstacleProvider(const FTower* InTowers, int32 InCount);

	virtual TArray<FObstacleRect> GetUnwalkableRects() const override;
	virtual TArray<FObstacleCircle> GetUnwalkableCircles() const override;

private:
	const FTower* Towers;
	int32 TowerCount;
};
