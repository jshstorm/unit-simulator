#include "Pathfinding/DynamicObstacleSystem.h"
#include "Pathfinding/PathfindingGrid.h"
#include "Pathfinding/PathNode.h"
#include "Units/Unit.h"
#include "GameConstants.h"

FDynamicObstacleSystem::FDynamicObstacleSystem(FPathfindingGrid& InGrid)
	: Grid(InGrid)
{
}

void FDynamicObstacleSystem::UpdateDynamicObstacles(const FUnit* Units, int32 UnitCount)
{
	// Record static blocks once
	if (!bStaticBlocksRecorded)
	{
		RecordStaticBlocks();
		bStaticBlocksRecorded = true;
	}

	// Clear previous dynamic blocks
	ClearDynamicBlocks();

	// Count ground units per cell
	TMap<FIntPoint, int32> CellCounts;

	for (int32 i = 0; i < UnitCount; ++i)
	{
		const FUnit& Unit = Units[i];
		if (Unit.bIsDead || Unit.Layer != EMovementLayer::Ground)
		{
			continue;
		}

		const FPathNode* Node = Grid.NodeFromWorldPoint(Unit.Position);
		if (Node != nullptr)
		{
			FIntPoint Key(Node->X, Node->Y);
			int32& Count = CellCounts.FindOrAdd(Key, 0);
			++Count;
		}
	}

	// Mark dense cells as dynamic obstacles
	for (const auto& Pair : CellCounts)
	{
		if (Pair.Value >= UnitSimConstants::DYNAMIC_OBSTACLE_DENSITY_THRESHOLD)
		{
			if (!StaticBlockedNodes.Contains(Pair.Key))
			{
				Grid.SetWalkable(Pair.Key.X, Pair.Key.Y, false);
				DynamicBlockedNodes.Add(Pair.Key);
			}
		}
	}
}

void FDynamicObstacleSystem::ClearDynamicBlocks()
{
	for (const FIntPoint& Cell : DynamicBlockedNodes)
	{
		if (!StaticBlockedNodes.Contains(Cell))
		{
			Grid.SetWalkable(Cell.X, Cell.Y, true);
		}
	}
	DynamicBlockedNodes.Empty();
}

void FDynamicObstacleSystem::RecordStaticBlocks()
{
	for (int32 X = 0; X < Grid.GetWidth(); ++X)
	{
		for (int32 Y = 0; Y < Grid.GetHeight(); ++Y)
		{
			const FPathNode* Node = Grid.GetNode(X, Y);
			if (Node != nullptr && !Node->bIsWalkable)
			{
				StaticBlockedNodes.Add(FIntPoint(X, Y));
			}
		}
	}
}
