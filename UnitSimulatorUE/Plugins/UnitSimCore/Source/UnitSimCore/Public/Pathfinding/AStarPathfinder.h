#pragma once

#include "CoreMinimal.h"

class FPathfindingGrid;
struct FPathNode;

/**
 * A* pathfinder with diagonal movement.
 * Diagonal cost = 14, straight cost = 10.
 * Prevents corner cutting through unwalkable tiles.
 * Ported from Pathfinding/AStarPathfinder.cs (131 lines)
 */
class UNITSIMCORE_API FAStarPathfinder
{
public:
	explicit FAStarPathfinder(FPathfindingGrid& InGrid);

	/**
	 * Find a path from start to end world positions.
	 * Returns true if path found, OutPath filled with world-space waypoints.
	 */
	bool FindPath(const FVector2D& StartWorldPos, const FVector2D& EndWorldPos, TArray<FVector2D>& OutPath);

private:
	FPathfindingGrid& Grid;

	/** Retrace path from end to start using CameFromNodeIndex chain */
	void RetracePath(int32 StartNodeIndex, int32 EndNodeIndex, TArray<FVector2D>& OutPath);

	/** Calculate distance cost between two nodes (10/14 diagonal) */
	static int32 CalculateDistanceCost(const FPathNode& A, const FPathNode& B);

	/** Get valid neighbor nodes (including diagonals with corner-cut prevention) */
	void GetNeighbors(const FPathNode& Node, TArray<int32>& OutNeighborIndices);

	/** Convert grid coords to flat index */
	int32 ToFlatIndex(int32 X, int32 Y) const;
};
