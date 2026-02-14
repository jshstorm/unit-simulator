#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "Simulation/SimulatorCallbacks.h"
#include "Simulation/FrameData.h"
#include "Behaviors/SquadBehavior.h"
#include "Behaviors/EnemyBehavior.h"
#include "Combat/CombatSystem.h"
#include "Combat/FrameEvents.h"
#include "Towers/TowerBehavior.h"
#include "GameState/GameSession.h"
#include "GameState/GameResult.h"
#include "GameState/WinConditionEvaluator.h"
#include "GameState/InitialSetup.h"
#include "Terrain/TerrainSystem.h"
#include "Units/Unit.h"
#include "Units/UnitRegistry.h"
#include "Commands/ISimulationCommand.h"
#include "Commands/SimulationCommands.h"

// Forward declarations
class FPathfindingGrid;
class FAStarPathfinder;
class FDynamicObstacleSystem;
class FPathSmoother;

/**
 * Command type enum for tagged union dispatch.
 */
enum class ESimCommandType : uint8
{
	Spawn,
	Move,
	Damage,
	Kill,
	Revive,
	SetHealth,
	Remove
};

/**
 * Concrete command wrapper implementing ISimulationCommand.
 * Uses tagged union pattern since USTRUCTs can't inherit from abstract classes.
 */
class UNITSIMCORE_API FSimCommandWrapper : public ISimulationCommand
{
public:
	ESimCommandType Type;

	FSpawnUnitCommand SpawnCmd;
	FMoveUnitCommand MoveCmd;
	FDamageUnitCommand DamageCmd;
	FKillUnitCommand KillCmd;
	FReviveUnitCommand ReviveCmd;
	FSetUnitHealthCommand SetHealthCmd;
	FRemoveUnitCommand RemoveCmd;

	virtual int32 GetFrameNumber() const override
	{
		switch (Type)
		{
		case ESimCommandType::Spawn:     return SpawnCmd.FrameNumber;
		case ESimCommandType::Move:      return MoveCmd.FrameNumber;
		case ESimCommandType::Damage:    return DamageCmd.FrameNumber;
		case ESimCommandType::Kill:      return KillCmd.FrameNumber;
		case ESimCommandType::Revive:    return ReviveCmd.FrameNumber;
		case ESimCommandType::SetHealth: return SetHealthCmd.FrameNumber;
		case ESimCommandType::Remove:    return RemoveCmd.FrameNumber;
		default:                         return 0;
		}
	}

	static TSharedPtr<FSimCommandWrapper> MakeSpawn(const FSpawnUnitCommand& Cmd)
	{
		auto W = MakeShared<FSimCommandWrapper>();
		W->Type = ESimCommandType::Spawn;
		W->SpawnCmd = Cmd;
		return W;
	}

	static TSharedPtr<FSimCommandWrapper> MakeMove(const FMoveUnitCommand& Cmd)
	{
		auto W = MakeShared<FSimCommandWrapper>();
		W->Type = ESimCommandType::Move;
		W->MoveCmd = Cmd;
		return W;
	}

	static TSharedPtr<FSimCommandWrapper> MakeDamage(const FDamageUnitCommand& Cmd)
	{
		auto W = MakeShared<FSimCommandWrapper>();
		W->Type = ESimCommandType::Damage;
		W->DamageCmd = Cmd;
		return W;
	}

	static TSharedPtr<FSimCommandWrapper> MakeKill(const FKillUnitCommand& Cmd)
	{
		auto W = MakeShared<FSimCommandWrapper>();
		W->Type = ESimCommandType::Kill;
		W->KillCmd = Cmd;
		return W;
	}

	static TSharedPtr<FSimCommandWrapper> MakeRevive(const FReviveUnitCommand& Cmd)
	{
		auto W = MakeShared<FSimCommandWrapper>();
		W->Type = ESimCommandType::Revive;
		W->ReviveCmd = Cmd;
		return W;
	}

	static TSharedPtr<FSimCommandWrapper> MakeSetHealth(const FSetUnitHealthCommand& Cmd)
	{
		auto W = MakeShared<FSimCommandWrapper>();
		W->Type = ESimCommandType::SetHealth;
		W->SetHealthCmd = Cmd;
		return W;
	}

	static TSharedPtr<FSimCommandWrapper> MakeRemove(const FRemoveUnitCommand& Cmd)
	{
		auto W = MakeShared<FSimCommandWrapper>();
		W->Type = ESimCommandType::Remove;
		W->RemoveCmd = Cmd;
		return W;
	}
};

/**
 * The core simulation engine.
 * Manages the simulation loop and state, providing a clean interface for
 * running simulations, capturing frame data, and integrating with external tools.
 *
 * Key features:
 * - Pure simulation logic with no rendering dependencies
 * - Command Queue for external control (spawning, state changes)
 * - Multicast delegate callbacks for external integrations
 * - 2-Phase Update pattern for deterministic behavior
 * - Supports state loading from saved frames
 *
 * Ported from SimulatorCore.cs (1,203 lines)
 */
class UNITSIMCORE_API FSimulatorCore
{
public:
	FSimulatorCore();
	~FSimulatorCore();

	// ════════════════════════════════════════════════════════════════════════
	// Initialization
	// ════════════════════════════════════════════════════════════════════════

	/** Initialize with default settings (Clash Royale standard) */
	void Initialize();

	/** Initialize with custom setup */
	void Initialize(const FInitialSetup& Setup);

	/** Reset simulation state and re-initialize */
	void Reset();

	// ════════════════════════════════════════════════════════════════════════
	// Simulation Running
	// ════════════════════════════════════════════════════════════════════════

	/**
	 * Run the complete simulation to completion.
	 * Ends when: max frames reached, all waves cleared, or game result determined.
	 */
	void Run();

	/**
	 * Execute a single simulation step (one frame).
	 * Uses 2-Phase Update pattern for deterministic behavior.
	 * @return Frame data snapshot for this frame.
	 */
	FFrameData Step();

	/** Stop a running simulation */
	void Stop();

	// ════════════════════════════════════════════════════════════════════════
	// Command Queue
	// ════════════════════════════════════════════════════════════════════════

	/** Enqueue a simulation command */
	void EnqueueCommand(TSharedPtr<ISimulationCommand> Command);

	// ════════════════════════════════════════════════════════════════════════
	// State Loading
	// ════════════════════════════════════════════════════════════════════════

	/** Load simulation state from frame data */
	void LoadState(const FFrameData& FrameData);

	/** Get current frame data snapshot */
	FFrameData GetCurrentFrameData() const;

	// ════════════════════════════════════════════════════════════════════════
	// Unit Injection / Removal
	// ════════════════════════════════════════════════════════════════════════

	/** Inject a new unit into the simulation */
	int32 InjectUnit(
		const FVector2D& Position,
		EUnitRole Role,
		EUnitFaction Faction,
		int32 HP = -1,
		float Speed = -1.f,
		float TurnSpeed = -1.f);

	/** Remove a unit by ID and faction */
	bool RemoveUnit(int32 UnitId, EUnitFaction Faction);

	/** Clear all attack slots on friendly units */
	void ClearFriendlyAttackSlots();

	// ════════════════════════════════════════════════════════════════════════
	// Public Accessors
	// ════════════════════════════════════════════════════════════════════════

	int32 GetCurrentFrame() const { return CurrentFrame; }
	bool GetIsInitialized() const { return bIsInitialized; }
	bool GetIsRunning() const { return bIsRunning; }
	const TArray<FUnit>& GetFriendlyUnits() const { return FriendlySquad; }
	const TArray<FUnit>& GetEnemyUnits() const { return EnemySquad; }
	TArray<FUnit>& GetFriendlyUnitsRef() { return FriendlySquad; }
	TArray<FUnit>& GetEnemyUnitsRef() { return EnemySquad; }
	const FVector2D& GetMainTarget() const { return MainTarget; }
	FGameSession& GetGameSession() { return GameSession; }
	const FGameSession& GetGameSession() const { return GameSession; }
	FTerrainSystem& GetTerrainSystem() { return TerrainSystem; }
	const FTerrainSystem& GetTerrainSystem() const { return TerrainSystem; }
	FAStarPathfinder* GetPathfinder() const { return Pathfinder.Get(); }
	FPathfindingGrid* GetPathfindingGrid() const { return PathfindingGrid.Get(); }
	FUnitRegistry& GetUnitRegistry() { return UnitRegistry; }

	int32 GetCurrentWave() const { return CurrentWave; }
	void SetCurrentWave(int32 Wave) { CurrentWave = Wave; }
	bool GetHasMoreWaves() const { return bHasMoreWaves; }
	void SetHasMoreWaves(bool bValue) { bHasMoreWaves = bValue; }
	bool AllEnemiesDead() const;

	/** Callback delegates container */
	FSimulatorCallbacks Callbacks;

private:
	// ════════════════════════════════════════════════════════════════════════
	// Simulation State
	// ════════════════════════════════════════════════════════════════════════

	int32 NextFriendlyId = 0;
	int32 NextEnemyId = 0;
	int32 CurrentFrame = 0;
	FVector2D MainTarget = FVector2D::ZeroVector;

	TArray<FUnit> FriendlySquad;
	TArray<FUnit> EnemySquad;

	FSquadBehavior SquadBehavior;
	FEnemyBehavior EnemyBehavior;
	FCombatSystem CombatSystem;
	FGameSession GameSession;
	FTowerBehavior TowerBehavior;
	FWinConditionEvaluator WinConditionEvaluator;
	FTerrainSystem TerrainSystem;
	FUnitRegistry UnitRegistry;

	// Pathfinding (heap-allocated for forward-declared types)
	TUniquePtr<FPathfindingGrid> PathfindingGrid;
	TUniquePtr<FAStarPathfinder> Pathfinder;
	TUniquePtr<FDynamicObstacleSystem> DynamicObstacleSystem;
	TUniquePtr<FPathSmoother> PathSmoother;

	// Command queue
	TQueue<TSharedPtr<ISimulationCommand>> CommandQueue;

	int32 CurrentWave = 0;
	bool bHasMoreWaves = true;
	bool bIsInitialized = false;
	bool bIsRunning = false;

	// ════════════════════════════════════════════════════════════════════════
	// Command Processing
	// ════════════════════════════════════════════════════════════════════════

	void ProcessCommands();
	void ExecuteCommand(const TSharedPtr<ISimulationCommand>& Cmd);

	// ════════════════════════════════════════════════════════════════════════
	// Phase 2: Apply Events
	// ════════════════════════════════════════════════════════════════════════

	void ApplyDamageEvents(const FFrameEvents& Events);
	void ApplyTowerDamageEvents(const FFrameEvents& Events);
	void ApplyDamageToTowers(const FFrameEvents& Events);
	void ProcessDeaths(FFrameEvents& Events);
	void ApplySpawnEvents(const FFrameEvents& Events);

	// ════════════════════════════════════════════════════════════════════════
	// Collision Resolution
	// ════════════════════════════════════════════════════════════════════════

	void ResolveCollisions();

	// ════════════════════════════════════════════════════════════════════════
	// Helpers
	// ════════════════════════════════════════════════════════════════════════

	void ConfigureStaticObstacles();
	void SpawnInitialUnits(const TArray<FUnitSpawnSetup>& UnitSetups);
	static FVector2D CalculateSpreadPosition(const FVector2D& Center, float Radius, int32 Index, int32 Total);
	void SpawnUnitFromSetup(const FName& UnitId, EUnitFaction Faction, const FVector2D& Position, int32 HPOverride);
	int32 InjectSpawnedUnit(const FUnitSpawnRequest& Request);

	int32 GetNextFriendlyId() { return ++NextFriendlyId; }
	int32 GetNextEnemyId() { return ++NextEnemyId; }

	/** Get living units combined from both squads */
	void GetAllLivingUnits(TArray<FUnit*>& OutUnits);

	/** Get opposing units for a given faction */
	TArray<FUnit>& GetOpposingUnits(EUnitFaction Faction);

	/** Reconstruct units from state data (for LoadState) */
	void ReconstructUnits(const TArray<FUnitStateData>& StateList, EUnitFaction ExpectedFaction, TArray<FUnit>& OutUnits);
};
