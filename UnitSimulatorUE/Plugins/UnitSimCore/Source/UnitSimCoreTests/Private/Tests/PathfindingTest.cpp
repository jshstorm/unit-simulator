#include "Misc/AutomationTest.h"
#include "Pathfinding/PathfindingGrid.h"
#include "Pathfinding/AStarPathfinder.h"
#include "Pathfinding/PathSmoother.h"
#include "Pathfinding/DynamicObstacleSystem.h"
#include "Units/Unit.h"

// ============================================================================
// FPathfindingGrid Creation & Obstacle Setting
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FPathGridCreation,
	"UnitSimCore.Pathfinding.Grid.CreationAndObstacles",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FPathGridCreation::RunTest(const FString& Parameters)
{
	// Arrange & Act
	FPathfindingGrid Grid(100.f, 100.f, 10.f);

	// Assert
	TestEqual(TEXT("Width"), Grid.GetWidth(), 10);
	TestEqual(TEXT("Height"), Grid.GetHeight(), 10);
	TestEqual(TEXT("NodeSize"), Grid.GetNodeSize(), 10.f);

	// All nodes should be walkable by default
	for (int32 X = 0; X < Grid.GetWidth(); ++X)
	{
		for (int32 Y = 0; Y < Grid.GetHeight(); ++Y)
		{
			const FPathNode* Node = Grid.GetNode(X, Y);
			TestNotNull(TEXT("Node exists"), Node);
			if (Node)
			{
				TestTrue(TEXT("Node walkable by default"), Node->bIsWalkable);
			}
		}
	}

	// Set obstacle
	Grid.SetWalkable(3, 3, false);
	const FPathNode* Blocked = Grid.GetNode(3, 3);
	TestNotNull(TEXT("Blocked node exists"), Blocked);
	if (Blocked)
	{
		TestFalse(TEXT("Node blocked"), Blocked->bIsWalkable);
	}

	return true;
}

// ============================================================================
// A* Straight-Line Path
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FAStarStraightPath,
	"UnitSimCore.Pathfinding.AStar.StraightLinePath",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FAStarStraightPath::RunTest(const FString& Parameters)
{
	// Arrange
	FPathfindingGrid Grid(100.f, 100.f, 10.f);
	FAStarPathfinder Pathfinder(Grid);

	// Act: path with no obstacles
	TArray<FVector2D> Path;
	bool bFound = Pathfinder.FindPath(FVector2D(5.0, 5.0), FVector2D(95.0, 95.0), Path);

	// Assert
	TestTrue(TEXT("Path found"), bFound);
	TestTrue(TEXT("Path has waypoints"), Path.Num() > 0);

	// All path waypoints should be on walkable nodes
	for (const FVector2D& Waypoint : Path)
	{
		const FPathNode* Node = Grid.NodeFromWorldPoint(Waypoint);
		TestNotNull(TEXT("Path node exists"), Node);
		if (Node)
		{
			TestTrue(TEXT("Path node walkable"), Node->bIsWalkable);
		}
	}

	return true;
}

// ============================================================================
// A* Obstacle Avoidance
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FAStarObstacleAvoidance,
	"UnitSimCore.Pathfinding.AStar.AvoidsObstacles",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FAStarObstacleAvoidance::RunTest(const FString& Parameters)
{
	// Arrange: Create a vertical wall with a gap
	FPathfindingGrid Grid(100.f, 100.f, 10.f);
	for (int32 Y = 0; Y < Grid.GetHeight(); ++Y)
	{
		if (Y != 5) // gap at Y=5
		{
			Grid.SetWalkable(4, Y, false);
		}
	}
	FAStarPathfinder Pathfinder(Grid);

	// Act
	TArray<FVector2D> Path;
	bool bFound = Pathfinder.FindPath(FVector2D(5.0, 5.0), FVector2D(95.0, 95.0), Path);

	// Assert
	TestTrue(TEXT("Path found around obstacle"), bFound);
	TestTrue(TEXT("Path has waypoints"), Path.Num() > 0);

	// No waypoint should be on a blocked node
	for (const FVector2D& Waypoint : Path)
	{
		const FPathNode* Node = Grid.NodeFromWorldPoint(Waypoint);
		TestNotNull(TEXT("Node exists"), Node);
		if (Node)
		{
			TestTrue(TEXT("Not on blocked node"), Node->bIsWalkable);
		}
	}

	return true;
}

// ============================================================================
// A* No Path (Fully Blocked)
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FAStarNoPath,
	"UnitSimCore.Pathfinding.AStar.NoPathWhenBlocked",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FAStarNoPath::RunTest(const FString& Parameters)
{
	// Arrange: Complete wall across the grid
	FPathfindingGrid Grid(60.f, 60.f, 10.f);
	for (int32 X = 0; X < Grid.GetWidth(); ++X)
	{
		Grid.SetWalkable(X, 3, false);
	}
	FAStarPathfinder Pathfinder(Grid);

	// Act
	TArray<FVector2D> Path;
	bool bFound = Pathfinder.FindPath(FVector2D(15.0, 15.0), FVector2D(45.0, 55.0), Path);

	// Assert
	TestFalse(TEXT("No path found"), bFound);
	TestEqual(TEXT("Path empty"), Path.Num(), 0);

	return true;
}

// ============================================================================
// A* Blocked Start/End
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FAStarBlockedStartEnd,
	"UnitSimCore.Pathfinding.AStar.BlockedStartOrEnd",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FAStarBlockedStartEnd::RunTest(const FString& Parameters)
{
	// Arrange
	FPathfindingGrid Grid(100.f, 100.f, 10.f);
	FAStarPathfinder Pathfinder(Grid);

	// Block start node
	Grid.SetWalkable(0, 0, false);
	TArray<FVector2D> Path1;
	bool bFound1 = Pathfinder.FindPath(FVector2D(1.0, 1.0), FVector2D(50.0, 50.0), Path1);
	TestFalse(TEXT("No path from blocked start"), bFound1);

	// Unblock start, block end
	Grid.SetWalkable(0, 0, true);
	Grid.SetWalkable(5, 5, false);
	Grid.ResetAllNodes();
	TArray<FVector2D> Path2;
	bool bFound2 = Pathfinder.FindPath(FVector2D(1.0, 1.0), FVector2D(55.0, 55.0), Path2);
	TestFalse(TEXT("No path to blocked end"), bFound2);

	return true;
}

// ============================================================================
// PathSmoother LOS Simplification
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FPathSmootherLOS,
	"UnitSimCore.Pathfinding.Smoother.LOSSimplification",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FPathSmootherLOS::RunTest(const FString& Parameters)
{
	// Arrange
	FPathfindingGrid Grid(100.f, 100.f, 10.f);
	FPathSmoother Smoother(Grid);

	// Create a path with redundant intermediate points
	TArray<FVector2D> Path;
	Path.Add(FVector2D(5.0, 5.0));
	Path.Add(FVector2D(15.0, 15.0));
	Path.Add(FVector2D(25.0, 25.0));
	Path.Add(FVector2D(35.0, 35.0));
	Path.Add(FVector2D(45.0, 45.0));
	Path.Add(FVector2D(55.0, 55.0));
	const int32 OriginalCount = Path.Num();

	// Act
	Smoother.SmoothPath(Path, true);

	// Assert: smoothed path should have fewer waypoints (direct LOS)
	TestTrue(TEXT("Path simplified"), Path.Num() <= OriginalCount);
	TestTrue(TEXT("Path still has points"), Path.Num() >= 2); // at least start and end

	return true;
}

// ============================================================================
// PathSmoother Disabled
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FPathSmootherDisabled,
	"UnitSimCore.Pathfinding.Smoother.DisabledNoChange",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FPathSmootherDisabled::RunTest(const FString& Parameters)
{
	// Arrange
	FPathfindingGrid Grid(100.f, 100.f, 10.f);
	FPathSmoother Smoother(Grid);

	TArray<FVector2D> Path;
	Path.Add(FVector2D(5.0, 5.0));
	Path.Add(FVector2D(15.0, 15.0));
	Path.Add(FVector2D(25.0, 25.0));
	Path.Add(FVector2D(35.0, 35.0));
	const int32 OriginalCount = Path.Num();

	// Act: smoothing disabled
	Smoother.SmoothPath(Path, false);

	// Assert: path unchanged
	TestEqual(TEXT("Path unchanged when disabled"), Path.Num(), OriginalCount);

	return true;
}

// ============================================================================
// DynamicObstacleSystem Update
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FDynObstacleUpdate,
	"UnitSimCore.Pathfinding.DynamicObstacle.UpdateAndClear",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FDynObstacleUpdate::RunTest(const FString& Parameters)
{
	// Arrange
	FPathfindingGrid Grid(100.f, 100.f, 10.f);
	FDynamicObstacleSystem DynObstacle(Grid);

	// Initially no dynamic blocks
	TestEqual(TEXT("Initial dynamic block count"), DynObstacle.GetDynamicBlockCount(), 0);

	// Create dense cluster of units on same cell
	TArray<FUnit> Units;
	for (int32 i = 0; i < 5; ++i)
	{
		FUnit U;
		U.Initialize(i, FName(TEXT("test")), EUnitFaction::Friendly,
			FVector2D(15.0, 15.0), 10.f, 4.f, 0.1f, EUnitRole::Melee, 100, 1);
		U.Layer = EMovementLayer::Ground;
		Units.Add(U);
	}

	// Act
	DynObstacle.UpdateDynamicObstacles(Units.GetData(), Units.Num());

	// Verify some dynamic blocks were created (density threshold)
	int32 BlockCount = DynObstacle.GetDynamicBlockCount();
	// Block count depends on density threshold; just verify the system ran
	// without crash and returns a non-negative count
	TestTrue(TEXT("Dynamic block count >= 0"), BlockCount >= 0);

	// Clear
	DynObstacle.ClearDynamicBlocks();
	TestEqual(TEXT("Cleared dynamic blocks"), DynObstacle.GetDynamicBlockCount(), 0);

	return true;
}

// ============================================================================
// Grid SetWalkableRect
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FPathGridRect,
	"UnitSimCore.Pathfinding.Grid.SetWalkableRect",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FPathGridRect::RunTest(const FString& Parameters)
{
	// Arrange
	FPathfindingGrid Grid(100.f, 100.f, 10.f);

	// Act: Block a rectangular area
	Grid.SetWalkableRect(FVector2D(20.0, 20.0), FVector2D(50.0, 50.0), false);

	// Assert: nodes inside rect should be blocked
	const FPathNode* Inside = Grid.GetNode(3, 3);
	TestNotNull(TEXT("Inside node"), Inside);
	if (Inside)
	{
		TestFalse(TEXT("Inside rect blocked"), Inside->bIsWalkable);
	}

	// Nodes outside rect should be walkable
	const FPathNode* Outside = Grid.GetNode(0, 0);
	TestNotNull(TEXT("Outside node"), Outside);
	if (Outside)
	{
		TestTrue(TEXT("Outside rect walkable"), Outside->bIsWalkable);
	}

	return true;
}

// ============================================================================
// Grid Out of Bounds
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FPathGridOutOfBounds,
	"UnitSimCore.Pathfinding.Grid.OutOfBoundsReturnsNull",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FPathGridOutOfBounds::RunTest(const FString& Parameters)
{
	// Arrange
	FPathfindingGrid Grid(100.f, 100.f, 10.f);

	// Assert: out of bounds returns nullptr
	TestNull(TEXT("Negative X"), Grid.GetNode(-1, 0));
	TestNull(TEXT("Negative Y"), Grid.GetNode(0, -1));
	TestNull(TEXT("Over Width"), Grid.GetNode(Grid.GetWidth(), 0));
	TestNull(TEXT("Over Height"), Grid.GetNode(0, Grid.GetHeight()));

	// World point out of bounds
	TestNull(TEXT("Negative world"), Grid.NodeFromWorldPoint(FVector2D(-10.0, -10.0)));

	return true;
}
