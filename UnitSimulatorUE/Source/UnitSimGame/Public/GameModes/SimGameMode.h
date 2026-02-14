#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "Simulation/FrameData.h"
#include "Simulation/SimulatorCallbacks.h"
#include "Data/JsonDataLoader.h"
#include "Simulation/SimulatorCore.h"
#include "SimGameMode.generated.h"

/**
 * Game mode that owns and drives the UnitSimCore simulation.
 *
 * Responsibilities:
 * - Owns the FSimulatorCore instance via TUniquePtr
 * - Loads JSON game data on BeginPlay
 * - Drives simulation with fixed timestep (1/30s) using accumulator pattern
 * - Exposes BlueprintCallable controls: Start, Pause, Resume, Reset
 * - Broadcasts simulation events via multicast delegates
 */
UCLASS()
class UNITSIMGAME_API ASimGameMode : public AGameModeBase
{
	GENERATED_BODY()

public:
	ASimGameMode();
	virtual ~ASimGameMode();

	// ════════════════════════════════════════════════════════════════════════
	// AGameModeBase overrides
	// ════════════════════════════════════════════════════════════════════════

	virtual void InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage) override;
	virtual void BeginPlay() override;
	virtual void Tick(float DeltaSeconds) override;

	// ════════════════════════════════════════════════════════════════════════
	// Simulation Control (BlueprintCallable)
	// ════════════════════════════════════════════════════════════════════════

	/** Start or resume the simulation */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Simulation")
	void StartSimulation();

	/** Pause the simulation (accumulator stops) */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Simulation")
	void PauseSimulation();

	/** Resume a paused simulation */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Simulation")
	void ResumeSimulation();

	/** Reset the simulation to initial state */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Simulation")
	void ResetSimulation();

	/** Execute a single simulation step (useful when paused) */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Simulation")
	void StepSimulation();

	// ════════════════════════════════════════════════════════════════════════
	// Simulation Speed
	// ════════════════════════════════════════════════════════════════════════

	/** Set simulation speed multiplier (1.0 = normal, 2.0 = double speed) */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Simulation")
	void SetSimulationSpeed(float Speed);

	/** Get current simulation speed multiplier */
	UFUNCTION(BlueprintPure, Category = "UnitSim|Simulation")
	float GetSimulationSpeed() const { return SimulationSpeed; }

	// ════════════════════════════════════════════════════════════════════════
	// State Queries (BlueprintPure)
	// ════════════════════════════════════════════════════════════════════════

	UFUNCTION(BlueprintPure, Category = "UnitSim|Simulation")
	bool IsSimulationRunning() const { return bIsSimulationRunning; }

	UFUNCTION(BlueprintPure, Category = "UnitSim|Simulation")
	bool IsSimulationPaused() const { return bIsSimulationPaused; }

	UFUNCTION(BlueprintPure, Category = "UnitSim|Simulation")
	bool IsSimulationInitialized() const;

	UFUNCTION(BlueprintPure, Category = "UnitSim|Simulation")
	int32 GetCurrentFrame() const;

	UFUNCTION(BlueprintPure, Category = "UnitSim|Simulation")
	FFrameData GetCurrentFrameData() const;

	// ════════════════════════════════════════════════════════════════════════
	// Data Access
	// ════════════════════════════════════════════════════════════════════════

	/** Get loaded game data (units, skills, towers, etc.) */
	UFUNCTION(BlueprintPure, Category = "UnitSim|Data")
	const FGameData& GetGameData() const { return GameData; }

	/** Get direct access to the simulator core (C++ only) */
	FSimulatorCore* GetSimulatorCore() const { return SimulatorCore.Get(); }

	// ════════════════════════════════════════════════════════════════════════
	// Events (Blueprint-assignable delegates)
	// ════════════════════════════════════════════════════════════════════════

	/** Fired each time a simulation frame completes */
	DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnSimFrameCompleted, const FFrameData&, FrameData);

	UPROPERTY(BlueprintAssignable, Category = "UnitSim|Events")
	FOnSimFrameCompleted OnSimFrameCompleted;

	/** Fired when the simulation completes (win/loss/draw) */
	DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnSimCompleted, int32, FinalFrame, const FString&, Reason);

	UPROPERTY(BlueprintAssignable, Category = "UnitSim|Events")
	FOnSimCompleted OnSimCompleted;

	/** Fired when a unit event occurs (spawn, death, attack, etc.) */
	DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnSimUnitEvent, const FUnitEventData&, EventData);

	UPROPERTY(BlueprintAssignable, Category = "UnitSim|Events")
	FOnSimUnitEvent OnSimUnitEvent;

protected:
	// ════════════════════════════════════════════════════════════════════════
	// Configuration
	// ════════════════════════════════════════════════════════════════════════

	/** Relative path under project Content directory to JSON data folder */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|Config")
	FString DataDirectoryPath = TEXT("Data/references");

	// ════════════════════════════════════════════════════════════════════════
	// Internal
	// ════════════════════════════════════════════════════════════════════════

	/** Load JSON game data from disk */
	bool LoadGameData();

	/** Initialize FSimulatorCore with loaded data */
	void InitializeSimulator();

	/** Bind FSimulatorCallbacks delegates to our Blueprint-exposed delegates */
	void BindSimulatorCallbacks();

private:
	/** The core simulation engine (pure C++, no UObject) */
	TUniquePtr<FSimulatorCore> SimulatorCore;

	/** Loaded game reference data */
	FGameData GameData;

	/** Fixed timestep accumulator */
	float TimeAccumulator = 0.f;

	/** Simulation speed multiplier */
	float SimulationSpeed = 1.f;

	/** Whether the simulation is actively stepping */
	bool bIsSimulationRunning = false;

	/** Whether the simulation is paused (accumulator frozen) */
	bool bIsSimulationPaused = false;

	/** Whether game data was loaded successfully */
	bool bDataLoaded = false;

	/** Handles for FSimulatorCallbacks delegate bindings */
	FDelegateHandle FrameGeneratedHandle;
	FDelegateHandle SimCompleteHandle;
	FDelegateHandle UnitEventHandle;

	// ════════════════════════════════════════════════════════════════════════
	// Callback handlers (bridge from FSimulatorCallbacks to dynamic delegates)
	// ════════════════════════════════════════════════════════════════════════

	void HandleFrameGenerated(const FFrameData& FrameData);
	void HandleSimulationComplete(int32 FinalFrame, const FString& Reason);
	void HandleUnitEvent(const FUnitEventData& EventData);
};
