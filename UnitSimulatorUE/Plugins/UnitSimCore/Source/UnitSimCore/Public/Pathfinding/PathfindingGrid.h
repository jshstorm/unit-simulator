#pragma once

#include "CoreMinimal.h"
#include "Pathfinding/PathNode.h"
#include "Pathfinding/IObstacleProvider.h"

/**
 * 2D pathfinding grid for A* navigation.
 * Manages nodes, walkability, obstacle application.
 * Ported from Pathfinding/PathfindingGrid.cs (151 lines)
 */
class UNITSIMCORE_API FPathfindingGrid
{
public:
	FPathfindingGrid() = default;
	FPathfindingGrid(float MapWidth, float MapHeight, float InNodeSize);

	int32 GetWidth() const { return Width; }
	int32 GetHeight() const { return Height; }
	float GetNodeSize() const { return NodeSize; }

	/** Get node by grid coords. Returns nullptr if out of bounds. */
	FPathNode* GetNode(int32 X, int32 Y);
	const FPathNode* GetNode(int32 X, int32 Y) const;

	/** Get node from world position. Returns nullptr if out of bounds. */
	FPathNode* NodeFromWorldPoint(const FVector2D& WorldPosition);
	const FPathNode* NodeFromWorldPoint(const FVector2D& WorldPosition) const;

	/** Set walkability of a single node by grid coords */
	bool SetWalkable(int32 X, int32 Y, bool bIsWalkable);

	/** Set walkability of a single node by world position */
	bool SetWalkableWorld(const FVector2D& WorldPosition, bool bIsWalkable);

	/** Set walkability of a rectangular area (world coords) */
	void SetWalkableRect(const FVector2D& Min, const FVector2D& Max, bool bIsWalkable);

	/** Set walkability of a circular area (world coords) */
	void SetWalkableCircle(const FVector2D& Center, float Radius, bool bIsWalkable);

	/** Apply obstacle provider to grid */
	void ApplyObstacles(const IObstacleProvider& Provider);

	/** Reset all node costs (for A* reuse) */
	void ResetAllNodes();

private:
	int32 Width = 0;
	int32 Height = 0;
	float NodeSize = 0.f;

	/** Flat 1D array storing grid[x + y * Width] */
	TArray<FPathNode> Grid;

	FORCEINLINE int32 FlatIndex(int32 X, int32 Y) const { return X + Y * Width; }
};
