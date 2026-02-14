#include "Misc/AutomationTest.h"
#include "Simulation/SimulatorCore.h"
#include "Simulation/FrameData.h"
#include "Commands/SimulationCommands.h"
#include "GameConstants.h"

// ============================================================================
// FSimulatorCore Initialization
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSimCoreInit,
	"UnitSimCore.SimulatorCore.Initialize.DefaultSetup",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FSimCoreInit::RunTest(const FString& Parameters)
{
	// Arrange & Act
	FSimulatorCore Sim;
	Sim.Initialize();

	// Assert
	TestTrue(TEXT("IsInitialized"), Sim.GetIsInitialized());
	TestFalse(TEXT("Not running before Run/Step"), Sim.GetIsRunning());
	TestEqual(TEXT("CurrentFrame 0"), Sim.GetCurrentFrame(), 0);
	TestEqual(TEXT("No friendly units initially"), Sim.GetFriendlyUnits().Num(), 0);
	TestEqual(TEXT("No enemy units initially"), Sim.GetEnemyUnits().Num(), 0);

	return true;
}

// ============================================================================
// Step() Returns FFrameData
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSimCoreStep,
	"UnitSimCore.SimulatorCore.Step.ReturnsFrameData",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FSimCoreStep::RunTest(const FString& Parameters)
{
	// Arrange
	FSimulatorCore Sim;
	Sim.Initialize();
	Sim.SetHasMoreWaves(false);

	// Act
	FFrameData Frame = Sim.Step();

	// Assert
	TestEqual(TEXT("Frame number 1"), Frame.FrameNumber, 1);
	TestEqual(TEXT("CurrentFrame advanced"), Sim.GetCurrentFrame(), 1);

	// Step again
	FFrameData Frame2 = Sim.Step();
	TestEqual(TEXT("Frame number 2"), Frame2.FrameNumber, 2);

	return true;
}

// ============================================================================
// SpawnUnitCommand -> Unit Creation
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSimCoreSpawnCommand,
	"UnitSimCore.SimulatorCore.Commands.SpawnUnit",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FSimCoreSpawnCommand::RunTest(const FString& Parameters)
{
	// Arrange
	FSimulatorCore Sim;
	Sim.Initialize();
	Sim.SetHasMoreWaves(false);

	FSpawnUnitCommand SpawnCmd;
	SpawnCmd.FrameNumber = 0;
	SpawnCmd.Position = FVector2D(1600.0, 500.0);
	SpawnCmd.Role = EUnitRole::Melee;
	SpawnCmd.Faction = EUnitFaction::Enemy;
	SpawnCmd.HP = 50;

	Sim.EnqueueCommand(FSimCommandWrapper::MakeSpawn(SpawnCmd));

	// Act
	FFrameData Frame = Sim.Step();

	// Assert
	TestTrue(TEXT("Enemy units spawned"), Sim.GetEnemyUnits().Num() > 0);

	return true;
}

// ============================================================================
// MoveUnitCommand -> Unit Movement
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSimCoreMoveCommand,
	"UnitSimCore.SimulatorCore.Commands.MoveUnit",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FSimCoreMoveCommand::RunTest(const FString& Parameters)
{
	// Arrange
	FSimulatorCore Sim;
	Sim.Initialize();
	Sim.SetHasMoreWaves(false);

	// Inject a unit first
	int32 UnitId = Sim.InjectUnit(FVector2D(1600.0, 1500.0), EUnitRole::Melee,
		EUnitFaction::Friendly, 100);
	TestTrue(TEXT("Unit injected"), UnitId > 0);

	FVector2D OrigPos = Sim.GetFriendlyUnits()[0].Position;

	// Enqueue move command
	FMoveUnitCommand MoveCmd;
	MoveCmd.FrameNumber = 0;
	MoveCmd.UnitId = UnitId;
	MoveCmd.Faction = EUnitFaction::Friendly;
	MoveCmd.Destination = FVector2D(1600.0, 3000.0);

	Sim.EnqueueCommand(FSimCommandWrapper::MakeMove(MoveCmd));

	// Step multiple frames to allow movement
	for (int32 i = 0; i < 10; ++i)
	{
		Sim.Step();
	}

	// Assert: unit should have moved toward destination
	FVector2D CurrentPos = Sim.GetFriendlyUnits()[0].Position;
	TestTrue(TEXT("Unit moved from original position"),
		FVector2D::Distance(CurrentPos, OrigPos) > 1.0);

	return true;
}

// ============================================================================
// 100-Frame Determinism Test
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSimCoreDeterminism,
	"UnitSimCore.SimulatorCore.Determinism.SameInputSameOutput",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FSimCoreDeterminism::RunTest(const FString& Parameters)
{
	// Arrange: Create two identical simulators
	auto CreateAndSetup = [](FSimulatorCore& Sim)
	{
		Sim.Initialize();
		Sim.SetHasMoreWaves(false);

		// Enqueue identical commands
		FSpawnUnitCommand Spawn1;
		Spawn1.FrameNumber = 0;
		Spawn1.Position = FVector2D(1600.0, 500.0);
		Spawn1.Role = EUnitRole::Melee;
		Spawn1.Faction = EUnitFaction::Enemy;
		Spawn1.HP = 10;
		Sim.EnqueueCommand(FSimCommandWrapper::MakeSpawn(Spawn1));

		FSpawnUnitCommand Spawn2;
		Spawn2.FrameNumber = 2;
		Spawn2.Position = FVector2D(1650.0, 520.0);
		Spawn2.Role = EUnitRole::Ranged;
		Spawn2.Faction = EUnitFaction::Enemy;
		Spawn2.HP = 8;
		Sim.EnqueueCommand(FSimCommandWrapper::MakeSpawn(Spawn2));

		FDamageUnitCommand Dmg;
		Dmg.FrameNumber = 5;
		Dmg.UnitId = 1;
		Dmg.Faction = EUnitFaction::Enemy;
		Dmg.Damage = 3;
		Sim.EnqueueCommand(FSimCommandWrapper::MakeDamage(Dmg));
	};

	FSimulatorCore Sim1;
	FSimulatorCore Sim2;
	CreateAndSetup(Sim1);
	CreateAndSetup(Sim2);

	// Act & Assert: Step both for 100 frames, compare JSON output
	bool bAllMatch = true;
	for (int32 i = 0; i < 100; ++i)
	{
		FFrameData Frame1 = Sim1.Step();
		FFrameData Frame2 = Sim2.Step();

		FString Json1 = Frame1.ToJson();
		FString Json2 = Frame2.ToJson();

		if (Json1 != Json2)
		{
			AddError(FString::Printf(TEXT("Frame %d mismatch"), i + 1));
			bAllMatch = false;
			break;
		}
	}

	TestTrue(TEXT("100 frames deterministic"), bAllMatch);

	return true;
}

// ============================================================================
// InjectUnit and RemoveUnit
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSimCoreInjectRemove,
	"UnitSimCore.SimulatorCore.Units.InjectAndRemove",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FSimCoreInjectRemove::RunTest(const FString& Parameters)
{
	// Arrange
	FSimulatorCore Sim;
	Sim.Initialize();
	Sim.SetHasMoreWaves(false);

	// Act: Inject
	int32 UnitId = Sim.InjectUnit(FVector2D(1600.0, 1500.0), EUnitRole::Melee,
		EUnitFaction::Friendly, 100);

	// Assert
	TestTrue(TEXT("Inject returned valid ID"), UnitId > 0);
	TestEqual(TEXT("Friendly count after inject"), Sim.GetFriendlyUnits().Num(), 1);

	// Act: Remove
	bool bRemoved = Sim.RemoveUnit(UnitId, EUnitFaction::Friendly);
	TestTrue(TEXT("Unit removed"), bRemoved);
	TestEqual(TEXT("Friendly count after remove"), Sim.GetFriendlyUnits().Num(), 0);

	// Remove non-existent
	bool bRemoved2 = Sim.RemoveUnit(999, EUnitFaction::Friendly);
	TestFalse(TEXT("Non-existent removal fails"), bRemoved2);

	return true;
}

// ============================================================================
// Custom InitialSetup
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSimCoreCustomSetup,
	"UnitSimCore.SimulatorCore.Initialize.CustomSetup",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FSimCoreCustomSetup::RunTest(const FString& Parameters)
{
	// Arrange
	FInitialSetup Setup = FInitialSetup::CreateClashRoyaleStandard();

	// Add initial units
	FUnitSpawnSetup UnitSetup;
	UnitSetup.UnitId = FName(TEXT("knight"));
	UnitSetup.Faction = EUnitFaction::Friendly;
	UnitSetup.Position = FVector2D(1600.0, 1500.0);
	UnitSetup.HP = 200;
	UnitSetup.Count = 1;
	Setup.InitialUnits.Add(UnitSetup);

	// Act
	FSimulatorCore Sim;
	Sim.Initialize(Setup);

	// Assert
	TestTrue(TEXT("IsInitialized"), Sim.GetIsInitialized());
	// Game session should have towers
	TestTrue(TEXT("Friendly towers exist"),
		Sim.GetGameSession().FriendlyTowers.Num() > 0);
	TestTrue(TEXT("Enemy towers exist"),
		Sim.GetGameSession().EnemyTowers.Num() > 0);

	return true;
}

// ============================================================================
// GetCurrentFrameData Snapshot
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FSimCoreFrameDataSnapshot,
	"UnitSimCore.SimulatorCore.FrameData.SnapshotMatchesStep",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FSimCoreFrameDataSnapshot::RunTest(const FString& Parameters)
{
	// Arrange
	FSimulatorCore Sim;
	Sim.Initialize();
	Sim.SetHasMoreWaves(false);

	// Act
	FFrameData StepFrame = Sim.Step();
	FFrameData Snapshot = Sim.GetCurrentFrameData();

	// Assert: frame numbers should match
	TestEqual(TEXT("FrameNumber match"), StepFrame.FrameNumber, Snapshot.FrameNumber);

	return true;
}
