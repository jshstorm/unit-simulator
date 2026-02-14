#include "Pathfinding/PathSmoother.h"
#include "Pathfinding/PathfindingGrid.h"
#include "Pathfinding/PathNode.h"
#include "GameConstants.h"

FPathSmoother::FPathSmoother(FPathfindingGrid& InGrid)
	: Grid(InGrid)
{
}

void FPathSmoother::SmoothPath(TArray<FVector2D>& Path, bool bEnabled)
{
	if (!bEnabled || Path.Num() <= 2)
	{
		return;
	}

	TArray<FVector2D> Smoothed;
	Smoothed.Add(Path[0]);
	int32 Current = 0;

	while (Current < Path.Num() - 1)
	{
		int32 FarthestVisible = Current + 1;
		const int32 MaxSkip = FMath::Min(
			Current + UnitSimConstants::PATH_SMOOTHING_MAX_SKIP,
			Path.Num() - 1);

		for (int32 i = MaxSkip; i > Current + 1; --i)
		{
			if (HasLineOfSight(Path[Current], Path[i]))
			{
				FarthestVisible = i;
				break;
			}
		}

		Smoothed.Add(Path[FarthestVisible]);
		Current = FarthestVisible;
	}

	Path = MoveTemp(Smoothed);
}

bool FPathSmoother::HasLineOfSight(const FVector2D& From, const FVector2D& To) const
{
	const FPathNode* FromNode = Grid.NodeFromWorldPoint(From);
	const FPathNode* ToNode = Grid.NodeFromWorldPoint(To);

	if (FromNode == nullptr || ToNode == nullptr)
	{
		return false;
	}

	return BresenhamLineWalkable(FromNode->X, FromNode->Y, ToNode->X, ToNode->Y);
}

bool FPathSmoother::BresenhamLineWalkable(int32 X0, int32 Y0, int32 X1, int32 Y1) const
{
	const int32 DX = FMath::Abs(X1 - X0);
	const int32 DY = FMath::Abs(Y1 - Y0);
	const int32 SX = X0 < X1 ? 1 : -1;
	const int32 SY = Y0 < Y1 ? 1 : -1;
	int32 Err = DX - DY;

	while (true)
	{
		const FPathNode* Node = Grid.GetNode(X0, Y0);
		if (Node == nullptr || !Node->bIsWalkable)
		{
			return false;
		}

		if (X0 == X1 && Y0 == Y1)
		{
			break;
		}

		const int32 E2 = 2 * Err;
		if (E2 > -DY)
		{
			Err -= DY;
			X0 += SX;
		}
		if (E2 < DX)
		{
			Err += DX;
			Y0 += SY;
		}
	}

	return true;
}
