#include "Pathfinding/PathfindingGrid.h"

FPathfindingGrid::FPathfindingGrid(float MapWidth, float MapHeight, float InNodeSize)
	: NodeSize(InNodeSize)
{
	Width = static_cast<int32>(MapWidth / NodeSize);
	Height = static_cast<int32>(MapHeight / NodeSize);
	Grid.SetNum(Width * Height);

	for (int32 X = 0; X < Width; ++X)
	{
		for (int32 Y = 0; Y < Height; ++Y)
		{
			const FVector2D WorldPos(
				X * NodeSize + NodeSize / 2.f,
				Y * NodeSize + NodeSize / 2.f);
			Grid[FlatIndex(X, Y)] = FPathNode(X, Y, WorldPos);
		}
	}
}

FPathNode* FPathfindingGrid::GetNode(int32 X, int32 Y)
{
	if (X >= 0 && X < Width && Y >= 0 && Y < Height)
	{
		return &Grid[FlatIndex(X, Y)];
	}
	return nullptr;
}

const FPathNode* FPathfindingGrid::GetNode(int32 X, int32 Y) const
{
	if (X >= 0 && X < Width && Y >= 0 && Y < Height)
	{
		return &Grid[FlatIndex(X, Y)];
	}
	return nullptr;
}

FPathNode* FPathfindingGrid::NodeFromWorldPoint(const FVector2D& WorldPosition)
{
	const int32 X = static_cast<int32>(WorldPosition.X / NodeSize);
	const int32 Y = static_cast<int32>(WorldPosition.Y / NodeSize);
	return GetNode(X, Y);
}

const FPathNode* FPathfindingGrid::NodeFromWorldPoint(const FVector2D& WorldPosition) const
{
	const int32 X = static_cast<int32>(WorldPosition.X / NodeSize);
	const int32 Y = static_cast<int32>(WorldPosition.Y / NodeSize);
	return GetNode(X, Y);
}

bool FPathfindingGrid::SetWalkable(int32 X, int32 Y, bool bIsWalkable)
{
	FPathNode* Node = GetNode(X, Y);
	if (Node == nullptr)
	{
		return false;
	}
	Node->bIsWalkable = bIsWalkable;
	return true;
}

bool FPathfindingGrid::SetWalkableWorld(const FVector2D& WorldPosition, bool bIsWalkable)
{
	FPathNode* Node = NodeFromWorldPoint(WorldPosition);
	if (Node == nullptr)
	{
		return false;
	}
	Node->bIsWalkable = bIsWalkable;
	return true;
}

void FPathfindingGrid::SetWalkableRect(const FVector2D& Min, const FVector2D& Max, bool bIsWalkable)
{
	const int32 MinX = FMath::Clamp(static_cast<int32>(Min.X / NodeSize), 0, Width - 1);
	const int32 MinY = FMath::Clamp(static_cast<int32>(Min.Y / NodeSize), 0, Height - 1);
	const int32 MaxX = FMath::Clamp(static_cast<int32>(Max.X / NodeSize), 0, Width - 1);
	const int32 MaxY = FMath::Clamp(static_cast<int32>(Max.Y / NodeSize), 0, Height - 1);

	for (int32 X = MinX; X <= MaxX; ++X)
	{
		for (int32 Y = MinY; Y <= MaxY; ++Y)
		{
			Grid[FlatIndex(X, Y)].bIsWalkable = bIsWalkable;
		}
	}
}

void FPathfindingGrid::SetWalkableCircle(const FVector2D& Center, float Radius, bool bIsWalkable)
{
	const int32 MinX = FMath::Clamp(static_cast<int32>((Center.X - Radius) / NodeSize), 0, Width - 1);
	const int32 MinY = FMath::Clamp(static_cast<int32>((Center.Y - Radius) / NodeSize), 0, Height - 1);
	const int32 MaxX = FMath::Clamp(static_cast<int32>((Center.X + Radius) / NodeSize), 0, Width - 1);
	const int32 MaxY = FMath::Clamp(static_cast<int32>((Center.Y + Radius) / NodeSize), 0, Height - 1);

	const float RadiusSq = Radius * Radius;

	for (int32 X = MinX; X <= MaxX; ++X)
	{
		for (int32 Y = MinY; Y <= MaxY; ++Y)
		{
			FPathNode& Node = Grid[FlatIndex(X, Y)];
			const float DX = Node.WorldPosition.X - Center.X;
			const float DY = Node.WorldPosition.Y - Center.Y;
			const float DistSq = DX * DX + DY * DY;

			if (DistSq <= RadiusSq)
			{
				Node.bIsWalkable = bIsWalkable;
			}
		}
	}
}

void FPathfindingGrid::ApplyObstacles(const IObstacleProvider& Provider)
{
	// Rectangular obstacles
	TArray<FObstacleRect> Rects = Provider.GetUnwalkableRects();
	for (const FObstacleRect& Rect : Rects)
	{
		SetWalkableRect(Rect.Min, Rect.Max, false);
	}

	// Circular obstacles
	TArray<FObstacleCircle> Circles = Provider.GetUnwalkableCircles();
	for (const FObstacleCircle& Circle : Circles)
	{
		SetWalkableCircle(Circle.Center, Circle.Radius, false);
	}
}

void FPathfindingGrid::ResetAllNodes()
{
	for (FPathNode& Node : Grid)
	{
		Node.ResetCosts();
	}
}
