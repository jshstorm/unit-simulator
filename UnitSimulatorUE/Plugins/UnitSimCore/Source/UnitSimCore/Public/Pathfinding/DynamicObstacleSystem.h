#pragma once

#include "CoreMinimal.h"

class FPathfindingGrid;
struct FUnit;

/**
 * Manages dynamic obstacles based on unit density per cell.
 * Tracks static vs dynamic blocked nodes to avoid corrupting static obstacles.
 * Ported from Pathfinding/DynamicObstacleSystem.cs (108 lines)
 */
class UNITSIMCORE_API FDynamicObstacleSystem
{
public:
	explicit FDynamicObstacleSystem(FPathfindingGrid& InGrid);

	/**
	 * Update dynamic obstacles based on ground unit density.
	 * Should be called once per frame.
	 * @param Units       Array of all units
	 * @param UnitCount   Number of units in the array
	 */
	void UpdateDynamicObstacles(const FUnit* Units, int32 UnitCount);

	/** Clear all dynamic blocks, restoring non-static nodes to walkable */
	void ClearDynamicBlocks();

	/** Number of currently blocked dynamic nodes */
	int32 GetDynamicBlockCount() const { return DynamicBlockedNodes.Num(); }

private:
	FPathfindingGrid& Grid;

	TSet<FIntPoint> DynamicBlockedNodes;
	TSet<FIntPoint> StaticBlockedNodes;
	bool bStaticBlocksRecorded = false;

	/** Record current unwalkable nodes as static (one-time) */
	void RecordStaticBlocks();
};
