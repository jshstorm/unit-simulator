#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "SimulatorCallbacks.generated.h"

// Forward declarations
struct FFrameData;

/**
 * Unit event types for simulation callbacks.
 * Ported from ISimulatorCallbacks.cs UnitEventType
 */
UENUM(BlueprintType)
enum class EUnitEventType : uint8
{
	Spawned,
	Died,
	Attack,
	Damaged,
	TargetAcquired,
	TargetLost,
	MovementStarted,
	MovementStopped,
	EnteredCombat,
	ExitedCombat
};

/**
 * Data structure representing a unit event in the simulation.
 * Ported from ISimulatorCallbacks.cs UnitEventData
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FUnitEventData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitEventType EventType = EUnitEventType::Spawned;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 UnitId = -1;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FrameNumber = 0;

	/** Optional: Target unit ID (-1 = none) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TargetUnitId = -1;

	/** Optional: Additional value (e.g., damage amount, -1 = none) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Value = -1;

	/** Optional: Position where event occurred */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Position = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bHasPosition = false;
};

/**
 * Multicast delegate declarations for simulator callbacks.
 * Replaces C# ISimulatorCallbacks interface with UE5 delegate pattern.
 *
 * Usage:
 *   SimCallbacks.OnFrameGenerated.AddRaw(this, &MyClass::HandleFrame);
 *   SimCallbacks.OnUnitEvent.Broadcast(EventData);
 */

DECLARE_MULTICAST_DELEGATE_OneParam(FOnFrameGenerated, const FFrameData& /* FrameData */);
DECLARE_MULTICAST_DELEGATE_TwoParams(FOnSimulationComplete, int32 /* FinalFrameNumber */, const FString& /* Reason */);
DECLARE_MULTICAST_DELEGATE_OneParam(FOnStateChanged, const FString& /* ChangeDescription */);
DECLARE_MULTICAST_DELEGATE_OneParam(FOnUnitEvent, const FUnitEventData& /* EventData */);

/**
 * Container for all simulator callback delegates.
 * Equivalent to ISimulatorCallbacks interface in C#.
 * Ported from ISimulatorCallbacks.cs (176 lines)
 */
struct UNITSIMCORE_API FSimulatorCallbacks
{
	FOnFrameGenerated OnFrameGenerated;
	FOnSimulationComplete OnSimulationComplete;
	FOnStateChanged OnStateChanged;
	FOnUnitEvent OnUnitEvent;

	/** Broadcast a unit event */
	void BroadcastUnitEvent(const FUnitEventData& EventData)
	{
		OnUnitEvent.Broadcast(EventData);
	}

	/** Broadcast state changed */
	void BroadcastStateChanged(const FString& Description)
	{
		OnStateChanged.Broadcast(Description);
	}
};
