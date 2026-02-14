#include "Pathfinding/AStarPathfinder.h"
#include "Pathfinding/PathfindingGrid.h"
#include "Pathfinding/PathNode.h"

FAStarPathfinder::FAStarPathfinder(FPathfindingGrid& InGrid)
	: Grid(InGrid)
{
}

bool FAStarPathfinder::FindPath(const FVector2D& StartWorldPos, const FVector2D& EndWorldPos, TArray<FVector2D>& OutPath)
{
	OutPath.Empty();

	FPathNode* StartNode = Grid.NodeFromWorldPoint(StartWorldPos);
	FPathNode* EndNode = Grid.NodeFromWorldPoint(EndWorldPos);

	if (StartNode == nullptr || EndNode == nullptr ||
		!StartNode->bIsWalkable || !EndNode->bIsWalkable)
	{
		return false;
	}

	const int32 StartIndex = ToFlatIndex(StartNode->X, StartNode->Y);
	const int32 EndIndex = ToFlatIndex(EndNode->X, EndNode->Y);

	TArray<int32> OpenList;
	TSet<int32> ClosedSet;

	Grid.ResetAllNodes();

	StartNode->GCost = 0;
	StartNode->HCost = CalculateDistanceCost(*StartNode, *EndNode);
	OpenList.Add(StartIndex);

	TArray<int32> NeighborIndices;

	while (OpenList.Num() > 0)
	{
		// Find node with lowest FCost in open list
		int32 BestOpenIdx = 0;
		const FPathNode* BestNode = Grid.GetNode(
			OpenList[0] % Grid.GetWidth(),
			OpenList[0] / Grid.GetWidth());

		for (int32 i = 1; i < OpenList.Num(); ++i)
		{
			const int32 Idx = OpenList[i];
			const FPathNode* CandidateNode = Grid.GetNode(
				Idx % Grid.GetWidth(),
				Idx / Grid.GetWidth());

			if (CandidateNode->GetFCost() < BestNode->GetFCost() ||
				(CandidateNode->GetFCost() == BestNode->GetFCost() &&
				 CandidateNode->HCost < BestNode->HCost))
			{
				BestNode = CandidateNode;
				BestOpenIdx = i;
			}
		}

		const int32 CurrentIndex = OpenList[BestOpenIdx];

		if (CurrentIndex == EndIndex)
		{
			RetracePath(StartIndex, EndIndex, OutPath);
			return true;
		}

		OpenList.RemoveAt(BestOpenIdx);
		ClosedSet.Add(CurrentIndex);

		const FPathNode* CurrentNode = Grid.GetNode(
			CurrentIndex % Grid.GetWidth(),
			CurrentIndex / Grid.GetWidth());

		NeighborIndices.Reset();
		GetNeighbors(*CurrentNode, NeighborIndices);

		for (const int32 NeighborIndex : NeighborIndices)
		{
			FPathNode* NeighborNode = Grid.GetNode(
				NeighborIndex % Grid.GetWidth(),
				NeighborIndex / Grid.GetWidth());

			if (!NeighborNode->bIsWalkable || ClosedSet.Contains(NeighborIndex))
			{
				continue;
			}

			const int32 TentativeGCost = CurrentNode->GCost + CalculateDistanceCost(*CurrentNode, *NeighborNode);
			if (TentativeGCost < NeighborNode->GCost)
			{
				NeighborNode->CameFromNodeIndex = CurrentIndex;
				NeighborNode->GCost = TentativeGCost;
				NeighborNode->HCost = CalculateDistanceCost(*NeighborNode, *EndNode);

				if (!OpenList.Contains(NeighborIndex))
				{
					OpenList.Add(NeighborIndex);
				}
			}
		}
	}

	return false;
}

void FAStarPathfinder::RetracePath(int32 StartNodeIndex, int32 EndNodeIndex, TArray<FVector2D>& OutPath)
{
	TArray<int32> PathIndices;
	int32 CurrentIndex = EndNodeIndex;

	while (CurrentIndex != -1 && CurrentIndex != StartNodeIndex)
	{
		PathIndices.Add(CurrentIndex);
		const FPathNode* Node = Grid.GetNode(
			CurrentIndex % Grid.GetWidth(),
			CurrentIndex / Grid.GetWidth());
		CurrentIndex = Node->CameFromNodeIndex;
	}

	// Reverse to get start-to-end order
	OutPath.Reserve(PathIndices.Num());
	for (int32 i = PathIndices.Num() - 1; i >= 0; --i)
	{
		const FPathNode* Node = Grid.GetNode(
			PathIndices[i] % Grid.GetWidth(),
			PathIndices[i] / Grid.GetWidth());
		OutPath.Add(Node->WorldPosition);
	}
}

int32 FAStarPathfinder::CalculateDistanceCost(const FPathNode& A, const FPathNode& B)
{
	const int32 XDistance = FMath::Abs(A.X - B.X);
	const int32 YDistance = FMath::Abs(A.Y - B.Y);
	const int32 Remaining = FMath::Abs(XDistance - YDistance);
	return 14 * FMath::Min(XDistance, YDistance) + 10 * Remaining;
}

void FAStarPathfinder::GetNeighbors(const FPathNode& Node, TArray<int32>& OutNeighborIndices)
{
	for (int32 DX = -1; DX <= 1; ++DX)
	{
		for (int32 DY = -1; DY <= 1; ++DY)
		{
			if (DX == 0 && DY == 0) continue;

			const int32 NX = Node.X + DX;
			const int32 NY = Node.Y + DY;
			const FPathNode* Neighbor = Grid.GetNode(NX, NY);
			if (Neighbor == nullptr) continue;

			// Prevent corner cutting through unwalkable tiles
			if (DX != 0 && DY != 0)
			{
				const FPathNode* Horizontal = Grid.GetNode(Node.X + DX, Node.Y);
				const FPathNode* Vertical = Grid.GetNode(Node.X, Node.Y + DY);
				if (Horizontal != nullptr && Vertical != nullptr &&
					(!Horizontal->bIsWalkable || !Vertical->bIsWalkable))
				{
					continue;
				}
			}

			OutNeighborIndices.Add(ToFlatIndex(NX, NY));
		}
	}
}

int32 FAStarPathfinder::ToFlatIndex(int32 X, int32 Y) const
{
	return X + Y * Grid.GetWidth();
}
