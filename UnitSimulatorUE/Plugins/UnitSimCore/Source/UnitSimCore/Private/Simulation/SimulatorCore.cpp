#include "Simulation/SimulatorCore.h"
#include "Pathfinding/PathfindingGrid.h"
#include "Pathfinding/AStarPathfinder.h"
#include "Pathfinding/DynamicObstacleSystem.h"
#include "Pathfinding/PathSmoother.h"
#include "Terrain/TerrainObstacleProvider.h"
#include "Towers/TowerObstacleProvider.h"
#include "Combat/AvoidanceSystem.h"

// ============================================================================
// Constructor / Destructor
// ============================================================================

FSimulatorCore::FSimulatorCore()
{
	UnitRegistry = FUnitRegistry::CreateWithDefaults();
}

FSimulatorCore::~FSimulatorCore() = default;

// ============================================================================
// Initialization
// ============================================================================

void FSimulatorCore::Initialize()
{
	FInitialSetup DefaultSetup = FInitialSetup::CreateClashRoyaleStandard();
	Initialize(DefaultSetup);
}

void FSimulatorCore::Initialize(const FInitialSetup& Setup)
{
	UE_LOG(LogTemp, Log, TEXT("[SimulatorCore] Initialize() called"));

	// Set main target
	MainTarget = FVector2D(
		static_cast<double>(UnitSimConstants::SIMULATION_WIDTH) - 100.0,
		static_cast<double>(UnitSimConstants::SIMULATION_HEIGHT) / 2.0
	);

	// Initialize empty squads
	FriendlySquad.Empty();
	EnemySquad.Empty();

	// Spawn initial units
	SpawnInitialUnits(Setup.InitialUnits);
	UE_LOG(LogTemp, Log, TEXT("[SimulatorCore] Spawned %d friendly, %d enemy initial units"),
		FriendlySquad.Num(), EnemySquad.Num());

	// Initialize pathfinding
	PathfindingGrid = MakeUnique<FPathfindingGrid>(
		static_cast<float>(UnitSimConstants::SIMULATION_WIDTH),
		static_cast<float>(UnitSimConstants::SIMULATION_HEIGHT),
		UnitSimConstants::UNIT_RADIUS);
	Pathfinder = MakeUnique<FAStarPathfinder>(*PathfindingGrid);
	PathSmoother = MakeUnique<FPathSmoother>(*PathfindingGrid);
	DynamicObstacleSystem = MakeUnique<FDynamicObstacleSystem>(*PathfindingGrid);
	UE_LOG(LogTemp, Log, TEXT("[SimulatorCore] Pathfinding grid initialized"));

	// Initialize towers from setup
	GameSession.InitializeTowers(Setup.Towers);

	// Configure static obstacles
	ConfigureStaticObstacles();

	// Apply game time settings
	if (Setup.bHasGameTime)
	{
		GameSession.MaxGameTime = Setup.GameTime.MaxGameTime;
		UE_LOG(LogTemp, Log, TEXT("[SimulatorCore] Game time set to %.0fs"), Setup.GameTime.MaxGameTime);
	}

	bIsInitialized = true;
	CurrentFrame = 0;
	CurrentWave = 0;
	bHasMoreWaves = true;

	UE_LOG(LogTemp, Log, TEXT("[SimulatorCore] Initialization complete. Towers: %dF/%dE"),
		GameSession.FriendlyTowers.Num(), GameSession.EnemyTowers.Num());
}

void FSimulatorCore::Reset()
{
	UE_LOG(LogTemp, Log, TEXT("[SimulatorCore] Reset() called"));
	bIsRunning = false;
	bIsInitialized = false;
	CurrentFrame = 0;
	NextFriendlyId = 0;
	NextEnemyId = 0;
	FriendlySquad.Empty();
	EnemySquad.Empty();

	// Drain command queue
	TSharedPtr<ISimulationCommand> Cmd;
	while (CommandQueue.Dequeue(Cmd)) {}

	PathfindingGrid.Reset();
	Pathfinder.Reset();
	DynamicObstacleSystem.Reset();
	PathSmoother.Reset();

	Initialize();
	UE_LOG(LogTemp, Log, TEXT("[SimulatorCore] Reset complete"));
}

void FSimulatorCore::ConfigureStaticObstacles()
{
	if (!PathfindingGrid.IsValid()) return;

	// Terrain obstacles (river non-bridge areas)
	FTerrainObstacleProvider TerrainProvider;
	PathfindingGrid->ApplyObstacles(TerrainProvider);

	// Tower obstacles
	TArray<FTower> AllTowers;
	AllTowers.Append(GameSession.FriendlyTowers);
	AllTowers.Append(GameSession.EnemyTowers);
	FTowerObstacleProvider TowerProvider(AllTowers.GetData(), AllTowers.Num());
	PathfindingGrid->ApplyObstacles(TowerProvider);

	UE_LOG(LogTemp, Log, TEXT("[SimulatorCore] Static obstacles configured: terrain + %d towers"),
		AllTowers.Num());
}

// ============================================================================
// Simulation Running
// ============================================================================

void FSimulatorCore::Run()
{
	if (!bIsInitialized)
	{
		UE_LOG(LogTemp, Error, TEXT("[SimulatorCore] Must be initialized before running"));
		return;
	}

	bIsRunning = true;
	UE_LOG(LogTemp, Log, TEXT("Starting simulation..."));

	while (CurrentFrame < UnitSimConstants::MAX_FRAMES && bIsRunning)
	{
		FFrameData FrameResult = Step();

		if (FrameResult.bAllWavesCleared)
		{
			Callbacks.OnSimulationComplete.Broadcast(CurrentFrame, TEXT("AllWavesCleared"));
			UE_LOG(LogTemp, Log, TEXT("All enemy waves eliminated at frame %d."), CurrentFrame);
			break;
		}

		if (FrameResult.bMaxFramesReached)
		{
			Callbacks.OnSimulationComplete.Broadcast(CurrentFrame, TEXT("MaxFramesReached"));
			UE_LOG(LogTemp, Log, TEXT("Maximum frames reached at frame %d."), CurrentFrame);
			break;
		}

		if (GameSession.Result != EGameResult::InProgress)
		{
			const FString ResultStr = StaticEnum<EGameResult>()->GetNameStringByValue(
				static_cast<int64>(GameSession.Result));
			Callbacks.OnSimulationComplete.Broadcast(CurrentFrame, ResultStr);
			UE_LOG(LogTemp, Log, TEXT("Simulation ended with result %s at frame %d."),
				*ResultStr, CurrentFrame);
			break;
		}
	}

	bIsRunning = false;
}

FFrameData FSimulatorCore::Step()
{
	if (!bIsInitialized)
	{
		UE_LOG(LogTemp, Error, TEXT("[SimulatorCore] Must be initialized before stepping"));
		return FFrameData();
	}

	FFrameEvents Events;
	const float DeltaTime = UnitSimConstants::FRAME_TIME_SECONDS;

	// Process queued commands
	ProcessCommands();

	// Update dynamic obstacles periodically
	if (CurrentFrame % UnitSimConstants::DYNAMIC_OBSTACLE_UPDATE_INTERVAL == 0 && DynamicObstacleSystem.IsValid())
	{
		TArray<FUnit*> LivingUnits;
		GetAllLivingUnits(LivingUnits);
		// Build flat array for DynamicObstacleSystem
		TArray<FUnit> AllLiving;
		for (FUnit* U : LivingUnits)
		{
			AllLiving.Add(*U);
		}
		DynamicObstacleSystem->UpdateDynamicObstacles(AllLiving.GetData(), AllLiving.Num());
	}

	// ════════════════════════════════════════════════════════════════════════
	// Phase 1: Collect (no HP changes)
	// ════════════════════════════════════════════════════════════════════════
	EnemyBehavior.UpdateEnemySquad(*this, EnemySquad, FriendlySquad, GameSession.FriendlyTowers, Events);
	SquadBehavior.UpdateFriendlySquad(*this, FriendlySquad, EnemySquad, GameSession.EnemyTowers, MainTarget, Events);
	TowerBehavior.UpdateAllTowers(GameSession, FriendlySquad, EnemySquad, Events, DeltaTime);

	// ════════════════════════════════════════════════════════════════════════
	// Phase 1.5: Collision Resolution (Body Blocking)
	// ════════════════════════════════════════════════════════════════════════
	ResolveCollisions();

	// ════════════════════════════════════════════════════════════════════════
	// Phase 2: Apply events
	// ════════════════════════════════════════════════════════════════════════
	ApplyDamageEvents(Events);
	ApplyTowerDamageEvents(Events);
	ApplyDamageToTowers(Events);
	ProcessDeaths(Events);
	ApplySpawnEvents(Events);

	// Update game session
	GameSession.ElapsedTime += DeltaTime;
	GameSession.UpdateKingTowerActivation();
	GameSession.UpdateCrowns();
	WinConditionEvaluator.Evaluate(GameSession);

	// Generate frame data
	FFrameData FrameResult = FFrameData::FromSimulationState(
		CurrentFrame,
		FriendlySquad,
		EnemySquad,
		MainTarget,
		CurrentWave,
		bHasMoreWaves,
		&GameSession
	);

	// Notify callbacks
	Callbacks.OnFrameGenerated.Broadcast(FrameResult);

	// Advance frame
	CurrentFrame++;

	return FrameResult;
}

void FSimulatorCore::Stop()
{
	bIsRunning = false;
	UE_LOG(LogTemp, Log, TEXT("Simulation stopped at frame %d."), CurrentFrame);
}

// ============================================================================
// Command Queue
// ============================================================================

void FSimulatorCore::EnqueueCommand(TSharedPtr<ISimulationCommand> Command)
{
	CommandQueue.Enqueue(Command);
}

void FSimulatorCore::ProcessCommands()
{
	TSharedPtr<ISimulationCommand> Cmd;
	while (CommandQueue.Peek(Cmd))
	{
		if (Cmd->GetFrameNumber() <= CurrentFrame)
		{
			CommandQueue.Dequeue(Cmd);
			ExecuteCommand(Cmd);
		}
		else
		{
			break;
		}
	}
}

void FSimulatorCore::ExecuteCommand(const TSharedPtr<ISimulationCommand>& Cmd)
{
	const FSimCommandWrapper* Wrapper = static_cast<const FSimCommandWrapper*>(Cmd.Get());
	if (!Wrapper) return;

	switch (Wrapper->Type)
	{
	case ESimCommandType::Spawn:
	{
		const FSpawnUnitCommand& Spawn = Wrapper->SpawnCmd;
		InjectUnit(Spawn.Position, Spawn.Role, Spawn.Faction, Spawn.HP, Spawn.Speed, Spawn.TurnSpeed);
		break;
	}
	case ESimCommandType::Damage:
	{
		const FDamageUnitCommand& Damage = Wrapper->DamageCmd;
		TArray<FUnit>& Squad = (Damage.Faction == EUnitFaction::Friendly) ? FriendlySquad : EnemySquad;
		for (FUnit& U : Squad)
		{
			if (U.Id == Damage.UnitId)
			{
				U.TakeDamage(Damage.Damage);
				Callbacks.BroadcastStateChanged(FString::Printf(TEXT("Unit %d damaged by %d"), U.Id, Damage.Damage));
				break;
			}
		}
		break;
	}
	case ESimCommandType::Kill:
	{
		const FKillUnitCommand& Kill = Wrapper->KillCmd;
		TArray<FUnit>& Squad = (Kill.Faction == EUnitFaction::Friendly) ? FriendlySquad : EnemySquad;
		for (FUnit& U : Squad)
		{
			if (U.Id == Kill.UnitId)
			{
				U.HP = 0;
				U.bIsDead = true;
				U.Velocity = FVector2D::ZeroVector;
				Callbacks.BroadcastStateChanged(FString::Printf(TEXT("Unit %d killed"), U.Id));
				break;
			}
		}
		break;
	}
	case ESimCommandType::Remove:
	{
		const FRemoveUnitCommand& Remove = Wrapper->RemoveCmd;
		RemoveUnit(Remove.UnitId, Remove.Faction);
		break;
	}
	case ESimCommandType::Move:
	{
		const FMoveUnitCommand& Move = Wrapper->MoveCmd;
		TArray<FUnit>& Squad = (Move.Faction == EUnitFaction::Friendly) ? FriendlySquad : EnemySquad;
		for (FUnit& U : Squad)
		{
			if (U.Id == Move.UnitId)
			{
				U.CurrentDestination = Move.Destination;
				Callbacks.BroadcastStateChanged(FString::Printf(TEXT("Unit %d destination set"), U.Id));
				break;
			}
		}
		break;
	}
	case ESimCommandType::Revive:
	{
		const FReviveUnitCommand& Revive = Wrapper->ReviveCmd;
		TArray<FUnit>& Squad = (Revive.Faction == EUnitFaction::Friendly) ? FriendlySquad : EnemySquad;
		for (FUnit& U : Squad)
		{
			if (U.Id == Revive.UnitId)
			{
				U.HP = Revive.HP;
				U.bIsDead = false;
				Callbacks.BroadcastStateChanged(FString::Printf(TEXT("Unit %d revived with %d HP"), U.Id, Revive.HP));
				break;
			}
		}
		break;
	}
	case ESimCommandType::SetHealth:
	{
		const FSetUnitHealthCommand& SetHP = Wrapper->SetHealthCmd;
		TArray<FUnit>& Squad = (SetHP.Faction == EUnitFaction::Friendly) ? FriendlySquad : EnemySquad;
		for (FUnit& U : Squad)
		{
			if (U.Id == SetHP.UnitId)
			{
				U.HP = SetHP.HP;
				U.bIsDead = (SetHP.HP <= 0);
				if (U.bIsDead)
				{
					U.Velocity = FVector2D::ZeroVector;
				}
				Callbacks.BroadcastStateChanged(FString::Printf(TEXT("Unit %d HP set to %d"), U.Id, SetHP.HP));
				break;
			}
		}
		break;
	}
	}
}

// ============================================================================
// Phase 2: Apply Events
// ============================================================================

void FSimulatorCore::ApplyDamageEvents(const FFrameEvents& Events)
{
	for (const FSimDamageEvent& Dmg : Events.Damages)
	{
		// Determine target squad by source
		// TargetIndex is an index into the opposing squad
		// We need to figure out which squad it belongs to
		// In the C# version, damage events store direct references.
		// In UE5, TargetIndex is the index into one of the squads.
		// The convention is: if SourceIndex is in Friendlies, TargetIndex is in Enemies and vice-versa.
		// However, we also need to handle splash damage to same-squad.
		// For now, we try both squads.

		FUnit* Target = nullptr;
		if (Dmg.TargetIndex >= 0)
		{
			if (Dmg.TargetIndex < EnemySquad.Num())
			{
				Target = &EnemySquad[Dmg.TargetIndex];
			}
			if (Target == nullptr && Dmg.TargetIndex < FriendlySquad.Num())
			{
				Target = &FriendlySquad[Dmg.TargetIndex];
			}
		}

		if (Target && !Target->bIsDead)
		{
			Target->TakeDamage(Dmg.Amount);
		}
	}
}

void FSimulatorCore::ApplyTowerDamageEvents(const FFrameEvents& Events)
{
	for (const FTowerDamageEvent& Dmg : Events.TowerDamages)
	{
		FUnit* Target = nullptr;
		if (Dmg.TargetIndex >= 0)
		{
			if (Dmg.TargetIndex < FriendlySquad.Num())
			{
				Target = &FriendlySquad[Dmg.TargetIndex];
			}
			if (Target == nullptr && Dmg.TargetIndex < EnemySquad.Num())
			{
				Target = &EnemySquad[Dmg.TargetIndex];
			}
		}

		if (Target && !Target->bIsDead)
		{
			Target->TakeDamage(Dmg.Amount);
		}
	}
}

void FSimulatorCore::ApplyDamageToTowers(const FFrameEvents& Events)
{
	for (const FDamageToTowerEvent& Dmg : Events.DamageToTowers)
	{
		// Find tower by index across both arrays
		FTower* Target = nullptr;
		const int32 TowerIdx = Dmg.TargetTowerIndex;

		// Search friendly towers
		for (FTower& T : GameSession.FriendlyTowers)
		{
			if (T.Id == TowerIdx)
			{
				Target = &T;
				break;
			}
		}
		// Search enemy towers
		if (!Target)
		{
			for (FTower& T : GameSession.EnemyTowers)
			{
				if (T.Id == TowerIdx)
				{
					Target = &T;
					break;
				}
			}
		}

		if (Target && !Target->IsDestroyed())
		{
			Target->TakeDamage(Dmg.Amount);
		}
	}
}

void FSimulatorCore::ProcessDeaths(FFrameEvents& Events)
{
	TQueue<int32> FriendlyDeathQueue;
	TQueue<int32> EnemyDeathQueue;
	TSet<int32> ProcessedFriendly;
	TSet<int32> ProcessedEnemy;

	// Collect initial deaths: friendly
	for (int32 i = 0; i < FriendlySquad.Num(); i++)
	{
		if (!FriendlySquad[i].bIsDead && FriendlySquad[i].HP <= 0)
		{
			FriendlyDeathQueue.Enqueue(i);
		}
	}
	// Collect initial deaths: enemy
	for (int32 i = 0; i < EnemySquad.Num(); i++)
	{
		if (!EnemySquad[i].bIsDead && EnemySquad[i].HP <= 0)
		{
			EnemyDeathQueue.Enqueue(i);
		}
	}

	// Process friendly deaths
	int32 DeadIdx;
	while (FriendlyDeathQueue.Dequeue(DeadIdx))
	{
		if (ProcessedFriendly.Contains(DeadIdx)) continue;

		FUnit& Dead = FriendlySquad[DeadIdx];
		Dead.bIsDead = true;
		Dead.Velocity = FVector2D::ZeroVector;
		// Release slot on target
		if (Dead.TargetIndex >= 0 && Dead.TargetIndex < EnemySquad.Num())
		{
			EnemySquad[Dead.TargetIndex].ReleaseSlot(DeadIdx, Dead.TakenSlotIndex);
		}
		ProcessedFriendly.Add(DeadIdx);

		// Broadcast death event
		FUnitEventData EvtData;
		EvtData.EventType = EUnitEventType::Died;
		EvtData.UnitId = Dead.Id;
		EvtData.Faction = Dead.Faction;
		EvtData.FrameNumber = CurrentFrame;
		EvtData.Position = Dead.Position;
		EvtData.bHasPosition = true;
		Callbacks.BroadcastUnitEvent(EvtData);

		// Death spawn
		TArray<FUnitSpawnRequest> Spawns = CombatSystem.CreateDeathSpawnRequests(Dead);
		for (const FUnitSpawnRequest& S : Spawns)
		{
			Events.AddSpawn(S);
		}

		// Death damage
		TArray<int32> NewlyDead = CombatSystem.ApplyDeathDamage(Dead, EnemySquad);
		for (int32 KilledIdx : NewlyDead)
		{
			if (!ProcessedEnemy.Contains(KilledIdx))
			{
				EnemyDeathQueue.Enqueue(KilledIdx);
			}
		}
	}

	// Process enemy deaths
	while (EnemyDeathQueue.Dequeue(DeadIdx))
	{
		if (ProcessedEnemy.Contains(DeadIdx)) continue;

		FUnit& Dead = EnemySquad[DeadIdx];
		Dead.bIsDead = true;
		Dead.Velocity = FVector2D::ZeroVector;
		if (Dead.TargetIndex >= 0 && Dead.TargetIndex < FriendlySquad.Num())
		{
			FriendlySquad[Dead.TargetIndex].ReleaseSlot(DeadIdx, Dead.TakenSlotIndex);
		}
		ProcessedEnemy.Add(DeadIdx);

		FUnitEventData EvtData;
		EvtData.EventType = EUnitEventType::Died;
		EvtData.UnitId = Dead.Id;
		EvtData.Faction = Dead.Faction;
		EvtData.FrameNumber = CurrentFrame;
		EvtData.Position = Dead.Position;
		EvtData.bHasPosition = true;
		Callbacks.BroadcastUnitEvent(EvtData);

		TArray<FUnitSpawnRequest> Spawns = CombatSystem.CreateDeathSpawnRequests(Dead);
		for (const FUnitSpawnRequest& S : Spawns)
		{
			Events.AddSpawn(S);
		}

		TArray<int32> NewlyDead = CombatSystem.ApplyDeathDamage(Dead, FriendlySquad);
		for (int32 KilledIdx : NewlyDead)
		{
			if (!ProcessedFriendly.Contains(KilledIdx))
			{
				FriendlyDeathQueue.Enqueue(KilledIdx);
			}
		}
	}
}

void FSimulatorCore::ApplySpawnEvents(const FFrameEvents& Events)
{
	for (const FUnitSpawnRequest& Spawn : Events.Spawns)
	{
		InjectSpawnedUnit(Spawn);
	}
}

// ============================================================================
// Collision Resolution
// ============================================================================

void FSimulatorCore::ResolveCollisions()
{
	TArray<FUnit*> AllUnits;
	GetAllLivingUnits(AllUnits);
	if (AllUnits.Num() < 2) return;

	for (int32 Iteration = 0; Iteration < UnitSimConstants::COLLISION_RESOLUTION_ITERATIONS; Iteration++)
	{
		bool bAnyResolved = false;

		for (int32 i = 0; i < AllUnits.Num(); i++)
		{
			FUnit* UnitA = AllUnits[i];
			if (UnitA->bIsDead) continue;

			for (int32 j = i + 1; j < AllUnits.Num(); j++)
			{
				FUnit* UnitB = AllUnits[j];
				if (UnitB->bIsDead) continue;
				if (!UnitA->IsSameLayer(*UnitB)) continue;

				const double CombinedRadius = UnitA->Radius + UnitB->Radius;
				const FVector2D Delta = UnitB->Position - UnitA->Position;
				const double Distance = Delta.Size();

				if (Distance < CombinedRadius && Distance > 0.001)
				{
					const double Overlap = CombinedRadius - Distance;
					const FVector2D PushDir = AvoidanceSystem::SafeNormalize(Delta);
					const double PushAmount = Overlap * 0.5 * UnitSimConstants::COLLISION_PUSH_STRENGTH;

					UnitA->Position -= PushDir * PushAmount;
					UnitB->Position += PushDir * PushAmount;
					bAnyResolved = true;
				}
				else if (Distance <= 0.001)
				{
					const double PushAmount = CombinedRadius * 0.5 * UnitSimConstants::COLLISION_PUSH_STRENGTH;
					FVector2D RandomDir(
						static_cast<double>(UnitA->Id % 7 - 3) * 0.1 + 0.5,
						static_cast<double>(UnitB->Id % 7 - 3) * 0.1 + 0.5
					);
					RandomDir = AvoidanceSystem::SafeNormalize(RandomDir);
					UnitA->Position -= RandomDir * PushAmount;
					UnitB->Position += RandomDir * PushAmount;
					bAnyResolved = true;
				}
			}
		}

		if (!bAnyResolved) break;
	}
}

// ============================================================================
// Unit Injection / Removal
// ============================================================================

int32 FSimulatorCore::InjectUnit(
	const FVector2D& Position,
	EUnitRole Role,
	EUnitFaction Faction,
	int32 HP,
	float Speed,
	float TurnSpeed)
{
	const int32 Id = (Faction == EUnitFaction::Friendly) ? GetNextFriendlyId() : GetNextEnemyId();
	const int32 Health = (HP > 0) ? HP : ((Faction == EUnitFaction::Friendly) ? UnitSimConstants::FRIENDLY_HP : UnitSimConstants::ENEMY_HP);
	const float UnitSpeed = (Speed > 0.f) ? Speed : ((Faction == EUnitFaction::Friendly) ? 4.5f : 4.0f);
	const float UnitTurnSpeed = (TurnSpeed > 0.f) ? TurnSpeed : ((Faction == EUnitFaction::Friendly) ? 0.08f : 0.1f);

	FUnit Unit;
	const FName UnitIdName(*FString::Printf(TEXT("%s"), *StaticEnum<EUnitRole>()->GetNameStringByValue(static_cast<int64>(Role)).ToLower()));
	Unit.Initialize(Id, UnitIdName, Faction, Position, UnitSimConstants::UNIT_RADIUS,
		UnitSpeed, UnitTurnSpeed, Role, Health, UnitSimConstants::FRIENDLY_ATTACK_DAMAGE);

	TArray<FUnit>& Squad = (Faction == EUnitFaction::Friendly) ? FriendlySquad : EnemySquad;
	Squad.Add(Unit);

	Callbacks.BroadcastStateChanged(FString::Printf(TEXT("Unit %s injected at (%.0f, %.0f)"),
		*Unit.GetLabel(), Position.X, Position.Y));

	FUnitEventData EvtData;
	EvtData.EventType = EUnitEventType::Spawned;
	EvtData.UnitId = Id;
	EvtData.Faction = Faction;
	EvtData.FrameNumber = CurrentFrame;
	EvtData.Position = Position;
	EvtData.bHasPosition = true;
	Callbacks.BroadcastUnitEvent(EvtData);

	return Id;
}

int32 FSimulatorCore::InjectSpawnedUnit(const FUnitSpawnRequest& Request)
{
	const int32 Id = (Request.Faction == EUnitFaction::Friendly) ? GetNextFriendlyId() : GetNextEnemyId();

	// Try UnitRegistry lookup
	const FUnitDefinition* Def = UnitRegistry.GetDefinition(Request.UnitId);
	FUnit Unit;

	if (Def)
	{
		Unit.Initialize(
			Id,
			Def->UnitId,
			Request.Faction,
			Request.Position,
			Def->Radius,
			Def->MoveSpeed,
			Def->TurnSpeed,
			Def->Role,
			(Request.HP > 0) ? Request.HP : Def->MaxHP,
			Def->Damage,
			Def->Layer,
			Def->CanTarget,
			Def->TargetPriority
		);
	}
	else
	{
		// Default fallback
		const int32 Health = (Request.HP > 0) ? Request.HP
			: ((Request.Faction == EUnitFaction::Friendly) ? UnitSimConstants::FRIENDLY_HP : UnitSimConstants::ENEMY_HP);
		const float UnitSpeed = (Request.Faction == EUnitFaction::Friendly) ? 4.5f : 4.0f;
		const float UnitTurnSpeed = (Request.Faction == EUnitFaction::Friendly) ? 0.08f : 0.1f;

		const FName FallbackId = Request.UnitId.IsNone() ? FName(TEXT("unknown")) : Request.UnitId;
		Unit.Initialize(Id, FallbackId, Request.Faction, Request.Position,
			UnitSimConstants::UNIT_RADIUS, UnitSpeed, UnitTurnSpeed,
			EUnitRole::Melee, Health, UnitSimConstants::FRIENDLY_ATTACK_DAMAGE);

		if (!Request.UnitId.IsNone())
		{
			Callbacks.BroadcastStateChanged(FString::Printf(TEXT("Warning: Unknown unit type '%s', using defaults"),
				*Request.UnitId.ToString()));
		}
	}

	TArray<FUnit>& Squad = (Request.Faction == EUnitFaction::Friendly) ? FriendlySquad : EnemySquad;
	Squad.Add(Unit);

	Callbacks.BroadcastStateChanged(FString::Printf(TEXT("Unit %s spawned at (%.0f, %.0f)"),
		*Unit.GetLabel(), Request.Position.X, Request.Position.Y));

	FUnitEventData EvtData;
	EvtData.EventType = EUnitEventType::Spawned;
	EvtData.UnitId = Id;
	EvtData.Faction = Request.Faction;
	EvtData.FrameNumber = CurrentFrame;
	EvtData.Position = Request.Position;
	EvtData.bHasPosition = true;
	Callbacks.BroadcastUnitEvent(EvtData);

	return Id;
}

bool FSimulatorCore::RemoveUnit(int32 UnitId, EUnitFaction Faction)
{
	TArray<FUnit>& Squad = (Faction == EUnitFaction::Friendly) ? FriendlySquad : EnemySquad;

	for (int32 i = 0; i < Squad.Num(); i++)
	{
		if (Squad[i].Id == UnitId)
		{
			Callbacks.BroadcastStateChanged(FString::Printf(TEXT("Unit %s removed from simulation"),
				*Squad[i].GetLabel()));
			Squad.RemoveAt(i);
			return true;
		}
	}

	UE_LOG(LogTemp, Warning, TEXT("Unit %d (%s) not found for removal"),
		UnitId, Faction == EUnitFaction::Friendly ? TEXT("Friendly") : TEXT("Enemy"));
	return false;
}

void FSimulatorCore::ClearFriendlyAttackSlots()
{
	for (FUnit& F : FriendlySquad)
	{
		for (int32 s = 0; s < F.AttackSlots.Num(); s++)
		{
			F.AttackSlots[s] = -1;
		}
	}
}

// ============================================================================
// State Loading
// ============================================================================

void FSimulatorCore::LoadState(const FFrameData& FrameData)
{
	CurrentFrame = FrameData.FrameNumber;
	MainTarget = FrameData.MainTarget;
	CurrentWave = FrameData.CurrentWave;

	ReconstructUnits(FrameData.FriendlyUnits, EUnitFaction::Friendly, FriendlySquad);
	ReconstructUnits(FrameData.EnemyUnits, EUnitFaction::Enemy, EnemySquad);

	NextFriendlyId = 0;
	for (const FUnit& U : FriendlySquad)
	{
		NextFriendlyId = FMath::Max(NextFriendlyId, U.Id);
	}
	NextEnemyId = 0;
	for (const FUnit& U : EnemySquad)
	{
		NextEnemyId = FMath::Max(NextEnemyId, U.Id);
	}

	if (FrameData.FriendlyTowers.Num() > 0 || FrameData.EnemyTowers.Num() > 0)
	{
		// TODO: Implement GameSession.LoadFromState
		// For now, initialize default towers
		GameSession.InitializeDefaultTowers();
	}
	else
	{
		GameSession.InitializeDefaultTowers();
	}

	bIsInitialized = true;
	Callbacks.BroadcastStateChanged(FString::Printf(TEXT("State loaded from frame %d"), FrameData.FrameNumber));
	UE_LOG(LogTemp, Log, TEXT("Simulation state loaded from frame %d."), FrameData.FrameNumber);
}

FFrameData FSimulatorCore::GetCurrentFrameData() const
{
	if (!bIsInitialized)
	{
		UE_LOG(LogTemp, Error, TEXT("[SimulatorCore] Must be initialized first"));
		return FFrameData();
	}

	return FFrameData::FromSimulationState(
		CurrentFrame,
		FriendlySquad,
		EnemySquad,
		MainTarget,
		CurrentWave,
		bHasMoreWaves,
		&GameSession
	);
}

// ============================================================================
// Helpers
// ============================================================================

bool FSimulatorCore::AllEnemiesDead() const
{
	for (const FUnit& E : EnemySquad)
	{
		if (!E.bIsDead) return false;
	}
	return true;
}

void FSimulatorCore::GetAllLivingUnits(TArray<FUnit*>& OutUnits)
{
	OutUnits.Empty();
	for (FUnit& U : FriendlySquad)
	{
		if (!U.bIsDead) OutUnits.Add(&U);
	}
	for (FUnit& U : EnemySquad)
	{
		if (!U.bIsDead) OutUnits.Add(&U);
	}
}

TArray<FUnit>& FSimulatorCore::GetOpposingUnits(EUnitFaction Faction)
{
	return (Faction == EUnitFaction::Friendly) ? EnemySquad : FriendlySquad;
}

void FSimulatorCore::SpawnInitialUnits(const TArray<FUnitSpawnSetup>& UnitSetups)
{
	for (const FUnitSpawnSetup& Setup : UnitSetups)
	{
		for (int32 i = 0; i < Setup.Count; i++)
		{
			FVector2D Position;
			if (Setup.Count > 1)
			{
				Position = CalculateSpreadPosition(Setup.Position, Setup.SpawnRadius, i, Setup.Count);
			}
			else
			{
				Position = Setup.Position;
			}

			SpawnUnitFromSetup(Setup.UnitId, Setup.Faction, Position, Setup.HP);
		}
	}
}

FVector2D FSimulatorCore::CalculateSpreadPosition(const FVector2D& Center, float Radius, int32 Index, int32 Total)
{
	if (Total <= 1) return Center;

	const double Angle = 2.0 * UE_DOUBLE_PI * Index / Total;
	return FVector2D(
		Center.X + Radius * FMath::Cos(Angle),
		Center.Y + Radius * FMath::Sin(Angle)
	);
}

void FSimulatorCore::SpawnUnitFromSetup(const FName& UnitId, EUnitFaction Faction, const FVector2D& Position, int32 HPOverride)
{
	const int32 Id = (Faction == EUnitFaction::Friendly) ? GetNextFriendlyId() : GetNextEnemyId();
	const FUnitDefinition* Def = UnitRegistry.GetDefinition(UnitId);

	FUnit Unit;
	if (Def)
	{
		const int32 Health = (HPOverride > 0) ? HPOverride : Def->MaxHP;
		Unit.Initialize(Id, Def->UnitId, Faction, Position, Def->Radius,
			Def->MoveSpeed, Def->TurnSpeed, Def->Role, Health, Def->Damage,
			Def->Layer, Def->CanTarget, Def->TargetPriority);
	}
	else
	{
		const int32 Health = (HPOverride > 0) ? HPOverride : 100;
		Unit.Initialize(Id, UnitId, Faction, Position, UnitSimConstants::UNIT_RADIUS,
			4.0f, 0.1f, EUnitRole::Melee, Health, UnitSimConstants::FRIENDLY_ATTACK_DAMAGE);
	}

	TArray<FUnit>& Squad = (Faction == EUnitFaction::Friendly) ? FriendlySquad : EnemySquad;
	Squad.Add(Unit);
}

void FSimulatorCore::ReconstructUnits(const TArray<FUnitStateData>& StateList, EUnitFaction ExpectedFaction, TArray<FUnit>& OutUnits)
{
	OutUnits.Empty();

	for (const FUnitStateData& State : StateList)
	{
		EUnitRole Role = EUnitRole::Melee;
		if (State.Role == TEXT("Ranged")) Role = EUnitRole::Ranged;
		else if (State.Role == TEXT("Tank")) Role = EUnitRole::Tank;
		else if (State.Role == TEXT("MiniTank")) Role = EUnitRole::MiniTank;
		else if (State.Role == TEXT("GlassCannon")) Role = EUnitRole::GlassCannon;
		else if (State.Role == TEXT("Swarm")) Role = EUnitRole::Swarm;
		else if (State.Role == TEXT("Spawner")) Role = EUnitRole::Spawner;
		else if (State.Role == TEXT("Support")) Role = EUnitRole::Support;
		else if (State.Role == TEXT("Siege")) Role = EUnitRole::Siege;

		EUnitFaction Faction = ExpectedFaction;
		if (State.Faction == TEXT("Enemy")) Faction = EUnitFaction::Enemy;
		else if (State.Faction == TEXT("Friendly")) Faction = EUnitFaction::Friendly;

		ETargetPriority TargetPri = ETargetPriority::Nearest;
		if (State.TargetPriority == TEXT("Buildings")) TargetPri = ETargetPriority::Buildings;

		FUnit Unit;
		Unit.Initialize(
			State.Id,
			FName(*State.UnitId),
			Faction,
			State.Position,
			State.Radius,
			State.Speed,
			State.TurnSpeed,
			Role,
			State.HP,
			State.Damage,
			State.Layer,
			State.CanTarget,
			TargetPri
		);

		Unit.Velocity = State.Velocity;
		Unit.Forward = State.Forward;
		Unit.CurrentDestination = State.CurrentDestination;
		Unit.AttackCooldown = State.AttackCooldown;
		Unit.bIsDead = State.bIsDead;
		Unit.ShieldHP = FMath::Min(State.ShieldHP, State.MaxShieldHP);
		Unit.TakenSlotIndex = State.TakenSlotIndex;
		Unit.bHasAvoidanceTarget = State.bHasAvoidanceTarget;
		Unit.AvoidanceTarget = State.AvoidanceTarget;

		if (State.bHasChargeState)
		{
			Unit.ChargeState.bIsCharging = State.bIsCharging;
			Unit.ChargeState.bIsCharged = State.bIsCharged;
			Unit.ChargeState.RequiredDistance = State.RequiredChargeDistance;
			Unit.bHasChargeAbility = true;
		}

		OutUnits.Add(Unit);
	}
}
