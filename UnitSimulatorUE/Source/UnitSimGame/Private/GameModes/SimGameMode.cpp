#include "GameModes/SimGameMode.h"
#include "Simulation/SimulatorCore.h"
#include "GameConstants.h"
#include "Units/UnitDefinition.h"

ASimGameMode::ASimGameMode()
{
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.TickGroup = TG_PrePhysics;
}

// ════════════════════════════════════════════════════════════════════════════
// AGameModeBase overrides
// ════════════════════════════════════════════════════════════════════════════

void ASimGameMode::InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage)
{
	Super::InitGame(MapName, Options, ErrorMessage);

	SimulatorCore = MakeUnique<FSimulatorCore>();
}

void ASimGameMode::BeginPlay()
{
	Super::BeginPlay();

	if (LoadGameData())
	{
		InitializeSimulator();
		BindSimulatorCallbacks();
		UE_LOG(LogTemp, Log, TEXT("SimGameMode: Simulation initialized successfully"));
	}
	else
	{
		UE_LOG(LogTemp, Error, TEXT("SimGameMode: Failed to load game data"));
	}
}

void ASimGameMode::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);

	if (!bIsSimulationRunning || bIsSimulationPaused || !SimulatorCore.IsValid())
	{
		return;
	}

	// Accumulator pattern: accumulate real time, step at fixed intervals
	TimeAccumulator += DeltaSeconds * SimulationSpeed;

	constexpr float FixedTimeStep = UnitSimConstants::FRAME_TIME_SECONDS;

	// Cap accumulated time to prevent spiral of death (max 10 steps per frame)
	constexpr float MaxAccumulatedTime = FixedTimeStep * 10.f;
	if (TimeAccumulator > MaxAccumulatedTime)
	{
		TimeAccumulator = MaxAccumulatedTime;
	}

	while (TimeAccumulator >= FixedTimeStep)
	{
		TimeAccumulator -= FixedTimeStep;
		SimulatorCore->Step();

		// Check if simulation ended after this step
		if (!SimulatorCore->GetIsRunning())
		{
			bIsSimulationRunning = false;
			TimeAccumulator = 0.f;
			break;
		}
	}
}

// ════════════════════════════════════════════════════════════════════════════
// Simulation Control
// ════════════════════════════════════════════════════════════════════════════

void ASimGameMode::StartSimulation()
{
	if (!SimulatorCore.IsValid())
	{
		UE_LOG(LogTemp, Warning, TEXT("SimGameMode: Cannot start - simulator not created"));
		return;
	}

	if (!SimulatorCore->GetIsInitialized())
	{
		InitializeSimulator();
	}

	bIsSimulationRunning = true;
	bIsSimulationPaused = false;
	TimeAccumulator = 0.f;

	UE_LOG(LogTemp, Log, TEXT("SimGameMode: Simulation started"));
}

void ASimGameMode::PauseSimulation()
{
	if (bIsSimulationRunning && !bIsSimulationPaused)
	{
		bIsSimulationPaused = true;
		UE_LOG(LogTemp, Log, TEXT("SimGameMode: Simulation paused at frame %d"), GetCurrentFrame());
	}
}

void ASimGameMode::ResumeSimulation()
{
	if (bIsSimulationRunning && bIsSimulationPaused)
	{
		bIsSimulationPaused = false;
		TimeAccumulator = 0.f;
		UE_LOG(LogTemp, Log, TEXT("SimGameMode: Simulation resumed at frame %d"), GetCurrentFrame());
	}
}

void ASimGameMode::ResetSimulation()
{
	if (!SimulatorCore.IsValid())
	{
		return;
	}

	bIsSimulationRunning = false;
	bIsSimulationPaused = false;
	TimeAccumulator = 0.f;

	SimulatorCore->Reset();
	InitializeSimulator();

	UE_LOG(LogTemp, Log, TEXT("SimGameMode: Simulation reset"));
}

void ASimGameMode::StepSimulation()
{
	if (!SimulatorCore.IsValid() || !SimulatorCore->GetIsInitialized())
	{
		UE_LOG(LogTemp, Warning, TEXT("SimGameMode: Cannot step - simulator not initialized"));
		return;
	}

	SimulatorCore->Step();
}

// ════════════════════════════════════════════════════════════════════════════
// Simulation Speed
// ════════════════════════════════════════════════════════════════════════════

void ASimGameMode::SetSimulationSpeed(float Speed)
{
	SimulationSpeed = FMath::Clamp(Speed, 0.1f, 10.f);
}

// ════════════════════════════════════════════════════════════════════════════
// State Queries
// ════════════════════════════════════════════════════════════════════════════

bool ASimGameMode::IsSimulationInitialized() const
{
	return SimulatorCore.IsValid() && SimulatorCore->GetIsInitialized();
}

int32 ASimGameMode::GetCurrentFrame() const
{
	return SimulatorCore.IsValid() ? SimulatorCore->GetCurrentFrame() : 0;
}

FFrameData ASimGameMode::GetCurrentFrameData() const
{
	return SimulatorCore.IsValid() ? SimulatorCore->GetCurrentFrameData() : FFrameData();
}

// ════════════════════════════════════════════════════════════════════════════
// Data Loading
// ════════════════════════════════════════════════════════════════════════════

bool ASimGameMode::LoadGameData()
{
	FString FullPath = FPaths::ProjectContentDir() / DataDirectoryPath;

	if (!FPaths::DirectoryExists(FullPath))
	{
		UE_LOG(LogTemp, Warning,
			TEXT("SimGameMode: Data directory not found at '%s', trying project root"),
			*FullPath);

		// Fallback: try project root / data / references
		FullPath = FPaths::ProjectDir() / TEXT("data") / TEXT("references");
	}

	if (!FPaths::DirectoryExists(FullPath))
	{
		UE_LOG(LogTemp, Error,
			TEXT("SimGameMode: No data directory found. Checked Content/%s and project/data/references"),
			*DataDirectoryPath);
		return false;
	}

	bDataLoaded = UJsonDataLoader::LoadAll(FullPath, GameData);

	if (bDataLoaded)
	{
		UE_LOG(LogTemp, Log,
			TEXT("SimGameMode: Loaded %d units, %d skills, %d towers, %d waves"),
			GameData.Units.Num(),
			GameData.Skills.Num(),
			GameData.Towers.Num(),
			GameData.Waves.Num());
	}

	return bDataLoaded;
}

// ════════════════════════════════════════════════════════════════════════════
// Simulator Initialization
// ════════════════════════════════════════════════════════════════════════════

void ASimGameMode::InitializeSimulator()
{
	if (!SimulatorCore.IsValid())
	{
		return;
	}

	// Convert FUnitStats to FUnitDefinition and register with the UnitRegistry
	for (const auto& Pair : GameData.Units)
	{
		const FUnitStats& Stats = Pair.Value;
		FUnitDefinition Def;
		Def.UnitId = Pair.Key;
		Def.DisplayName = Stats.DisplayName;
		Def.MaxHP = Stats.HP;
		Def.Damage = Stats.Damage;
		Def.AttackRange = Stats.AttackRange;
		Def.MoveSpeed = Stats.MoveSpeed;
		Def.TurnSpeed = Stats.TurnSpeed;
		Def.Radius = Stats.Radius;
		Def.Role = Stats.Role;
		Def.Layer = Stats.Layer;
		Def.CanTarget = Stats.CanTarget;
		Def.TargetPriority = Stats.TargetPriority;
		SimulatorCore->GetUnitRegistry().Register(Def);
	}

	// Initialize with standard Clash Royale setup
	FInitialSetup Setup = FInitialSetup::CreateClashRoyaleStandard();
	SimulatorCore->Initialize(Setup);
}

// ════════════════════════════════════════════════════════════════════════════
// Callback Binding
// ════════════════════════════════════════════════════════════════════════════

void ASimGameMode::BindSimulatorCallbacks()
{
	if (!SimulatorCore.IsValid())
	{
		return;
	}

	FSimulatorCallbacks& Callbacks = SimulatorCore->Callbacks;

	FrameGeneratedHandle = Callbacks.OnFrameGenerated.AddRaw(
		this, &ASimGameMode::HandleFrameGenerated);

	SimCompleteHandle = Callbacks.OnSimulationComplete.AddRaw(
		this, &ASimGameMode::HandleSimulationComplete);

	UnitEventHandle = Callbacks.OnUnitEvent.AddRaw(
		this, &ASimGameMode::HandleUnitEvent);
}

void ASimGameMode::HandleFrameGenerated(const FFrameData& FrameData)
{
	OnSimFrameCompleted.Broadcast(FrameData);
}

void ASimGameMode::HandleSimulationComplete(int32 FinalFrame, const FString& Reason)
{
	bIsSimulationRunning = false;
	bIsSimulationPaused = false;
	OnSimCompleted.Broadcast(FinalFrame, Reason);
}

void ASimGameMode::HandleUnitEvent(const FUnitEventData& EventData)
{
	OnSimUnitEvent.Broadcast(EventData);
}
