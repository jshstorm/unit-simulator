#include "Debug/SimDebugDrawer.h"
#include "Simulation/SimulatorCore.h"
#include "Simulation/FrameData.h"
#include "Units/Unit.h"
#include "Towers/Tower.h"
#include "GameState/GameSession.h"
#include "Pathfinding/PathfindingGrid.h"
#include "Pathfinding/PathNode.h"
#include "DrawDebugHelpers.h"

// ════════════════════════════════════════════════════════════════════════════
// Draw All
// ════════════════════════════════════════════════════════════════════════════

void USimDebugDrawer::DrawAll(const UWorld* World, const FSimulatorCore* Simulator)
{
	if (!bEnabled || !World || !Simulator)
	{
		return;
	}

	if (bDrawUnits)
	{
		DrawDebugUnits(World, Simulator);
	}
	if (bDrawPaths)
	{
		DrawDebugPaths(World, Simulator);
	}
	if (bDrawTowers)
	{
		DrawDebugTowers(World, Simulator);
	}
	if (bDrawGrid)
	{
		DrawDebugGrid(World, Simulator);
	}
}

// ════════════════════════════════════════════════════════════════════════════
// Units
// ════════════════════════════════════════════════════════════════════════════

void USimDebugDrawer::DrawDebugUnits(const UWorld* World, const FSimulatorCore* Simulator)
{
	if (!World || !Simulator)
	{
		return;
	}

	auto DrawUnitArray = [this, World](const TArray<FUnit>& Units, const FColor& AliveColor, const FColor& DeadColor)
	{
		for (const FUnit& Unit : Units)
		{
			FColor Color = Unit.bIsDead ? DeadColor : AliveColor;
			FVector Center = SimToWorld(Unit.Position);

			// Draw circle for unit position
			DrawDebugCircle(
				World, Center, Unit.Radius, 16,
				Color, false, -1.f, 0, LineThickness,
				FVector(1, 0, 0), FVector(0, 1, 0), false);

			if (!Unit.bIsDead)
			{
				// Draw forward direction
				FVector ForwardEnd = Center + FVector(Unit.Forward.X, Unit.Forward.Y, 0.f) * Unit.Radius;
				DrawDebugLine(World, Center, ForwardEnd, FColor::White, false, -1.f, 0, LineThickness * 0.5f);

				// Draw HP text
				FString HPText = FString::Printf(TEXT("HP:%d"), Unit.HP);
				DrawDebugString(World, Center + FVector(0, 0, 30.f), HPText, nullptr, Color, -1.f, true, TextScale);

				// Draw unit label
				FString Label = Unit.GetLabel();
				DrawDebugString(World, Center + FVector(0, 0, 45.f), Label, nullptr, Color, -1.f, true, TextScale * 0.8f);

				// Draw target line if targeting something
				if (Unit.TargetIndex >= 0)
				{
					// Indicate targeting with a thin line (target position lookup omitted for simplicity)
					DrawDebugPoint(World, Center + FVector(0, 0, 20.f), 5.f, FColor::Red, false, -1.f);
				}
			}
		}
	};

	DrawUnitArray(Simulator->GetFriendlyUnits(), FColor::Blue, FColor(50, 50, 100));
	DrawUnitArray(Simulator->GetEnemyUnits(), FColor::Red, FColor(100, 50, 50));
}

// ════════════════════════════════════════════════════════════════════════════
// Paths
// ════════════════════════════════════════════════════════════════════════════

void USimDebugDrawer::DrawDebugPaths(const UWorld* World, const FSimulatorCore* Simulator)
{
	if (!World || !Simulator)
	{
		return;
	}

	auto DrawPaths = [this, World](const TArray<FUnit>& Units, const FColor& PathColor)
	{
		for (const FUnit& Unit : Units)
		{
			if (Unit.bIsDead)
			{
				continue;
			}

			// Draw movement path
			if (Unit.MovementPath.Num() > 1)
			{
				for (int32 i = Unit.MovementPathIndex; i < Unit.MovementPath.Num() - 1; ++i)
				{
					FVector Start = SimToWorld(Unit.MovementPath[i]);
					FVector End = SimToWorld(Unit.MovementPath[i + 1]);
					DrawDebugLine(World, Start, End, PathColor, false, -1.f, 0, LineThickness);
				}
			}

			// Draw avoidance path (yellow)
			if (Unit.AvoidancePath.Num() > 1)
			{
				for (int32 i = Unit.AvoidancePathIndex; i < Unit.AvoidancePath.Num() - 1; ++i)
				{
					FVector Start = SimToWorld(Unit.AvoidancePath[i]);
					FVector End = SimToWorld(Unit.AvoidancePath[i + 1]);
					DrawDebugLine(World, Start, End, FColor::Yellow, false, -1.f, 0, LineThickness * 0.5f);
				}
			}

			// Draw line from unit to current destination
			FVector UnitPos = SimToWorld(Unit.Position);
			FVector DestPos = SimToWorld(Unit.CurrentDestination);
			DrawDebugLine(World, UnitPos, DestPos, FColor(PathColor.R, PathColor.G, PathColor.B, 80),
				false, -1.f, 0, LineThickness * 0.3f);
		}
	};

	DrawPaths(Simulator->GetFriendlyUnits(), FColor::Cyan);
	DrawPaths(Simulator->GetEnemyUnits(), FColor::Orange);
}

// ════════════════════════════════════════════════════════════════════════════
// Towers
// ════════════════════════════════════════════════════════════════════════════

void USimDebugDrawer::DrawDebugTowers(const UWorld* World, const FSimulatorCore* Simulator)
{
	if (!World || !Simulator)
	{
		return;
	}

	const FGameSession& Session = Simulator->GetGameSession();

	auto DrawTowerArray = [this, World](const TArray<FTower>& Towers, const FColor& Color)
	{
		for (const FTower& Tower : Towers)
		{
			FVector Center = SimToWorld(Tower.Position);

			// Tower body circle
			DrawDebugCircle(
				World, Center, Tower.Radius, 24,
				Color, false, -1.f, 0, LineThickness * 1.5f,
				FVector(1, 0, 0), FVector(0, 1, 0), false);

			// Attack range circle (thin, dashed effect via segments)
			DrawDebugCircle(
				World, Center, Tower.AttackRange, 48,
				FColor(Color.R, Color.G, Color.B, 100), false, -1.f, 0, LineThickness * 0.5f,
				FVector(1, 0, 0), FVector(0, 1, 0), false);

			// Tower info text
			FString TowerText = FString::Printf(TEXT("HP:%d/%d"), Tower.CurrentHP, Tower.MaxHP);
			DrawDebugString(World, Center + FVector(0, 0, 50.f), TowerText, nullptr, Color, -1.f, true, TextScale);

			FString TypeText = Tower.Type == ETowerType::King ? TEXT("KING") : TEXT("PRINCESS");
			DrawDebugString(World, Center + FVector(0, 0, 65.f), TypeText, nullptr, Color, -1.f, true, TextScale * 0.8f);
		}
	};

	DrawTowerArray(Session.FriendlyTowers, FColor::Blue);
	DrawTowerArray(Session.EnemyTowers, FColor::Red);
}

// ════════════════════════════════════════════════════════════════════════════
// Grid
// ════════════════════════════════════════════════════════════════════════════

void USimDebugDrawer::DrawDebugGrid(const UWorld* World, const FSimulatorCore* Simulator)
{
	if (!World || !Simulator)
	{
		return;
	}

	FPathfindingGrid* Grid = Simulator->GetPathfindingGrid();
	if (!Grid)
	{
		return;
	}

	const float NodeSize = Grid->GetNodeSize();
	const int32 GridWidth = Grid->GetWidth();
	const int32 GridHeight = Grid->GetHeight();

	// Only draw non-walkable cells to reduce draw call count
	for (int32 Y = 0; Y < GridHeight; ++Y)
	{
		for (int32 X = 0; X < GridWidth; ++X)
		{
			const FPathNode* Node = Grid->GetNode(X, Y);
			if (!Node || Node->bIsWalkable)
			{
				continue;
			}

			// Draw obstacle cell as a red box
			FVector Center(
				X * NodeSize + NodeSize * 0.5f,
				Y * NodeSize + NodeSize * 0.5f,
				DrawHeight);
			FVector Extent(NodeSize * 0.4f, NodeSize * 0.4f, 1.f);

			DrawDebugBox(World, Center, Extent, FColor(200, 50, 50, 150), false, -1.f, 0, LineThickness * 0.3f);
		}
	}
}

// ════════════════════════════════════════════════════════════════════════════
// Helpers
// ════════════════════════════════════════════════════════════════════════════

FVector USimDebugDrawer::SimToWorld(const FVector2D& SimPos) const
{
	return FVector(SimPos.X, SimPos.Y, DrawHeight);
}
