#pragma once

#include "CoreMinimal.h"
#include "GameBalance.generated.h"

/**
 * Game balance and simulation settings data.
 * Can override GameConstants at runtime.
 * Ported from Contracts/GameBalance.cs
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FGameBalance
{
	GENERATED_BODY()

	/** Data version */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Version = 1;

	// ═══════════════════════════════════════════════════════════════════
	// Simulation Space
	// ═══════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SimulationWidth = 3200;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SimulationHeight = 5100;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxFrames = 3000;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float FrameTimeSeconds = 1.f / 30.f;

	// ═══════════════════════════════════════════════════════════════════
	// Unit Settings
	// ═══════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float UnitRadius = 20.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float CollisionRadiusScale = 2.f / 3.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 NumAttackSlots = 8;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float SlotReevaluateDistance = 40.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SlotReevaluateIntervalFrames = 60;

	// ═══════════════════════════════════════════════════════════════════
	// Combat Settings
	// ═══════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackCooldown = 30.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MeleeRangeMultiplier = 3;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 RangedRangeMultiplier = 6;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float EngagementTriggerDistanceMultiplier = 1.5f;

	// ═══════════════════════════════════════════════════════════════════
	// Squad Behavior
	// ═══════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float RallyDistance = 300.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float FormationThreshold = 20.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float SeparationRadius = 120.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float FriendlySeparationRadius = 80.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float DestinationThreshold = 10.f;

	// ═══════════════════════════════════════════════════════════════════
	// Wave Settings
	// ═══════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxWaves = 3;

	// ═══════════════════════════════════════════════════════════════════
	// Targeting Settings
	// ═══════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TargetReevaluateIntervalFrames = 45;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float TargetSwitchMargin = 15.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float TargetCrowdPenaltyPerAttacker = 25.f;

	// ═══════════════════════════════════════════════════════════════════
	// Avoidance Settings
	// ═══════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AvoidanceAngleStep = UE_PI / 8.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxAvoidanceIterations = 8;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AvoidanceMaxLookahead = 3.5f;

	// ═══════════════════════════════════════════════════════════════════
	// Collision Resolution
	// ═══════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 CollisionResolutionIterations = 3;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float CollisionPushStrength = 0.8f;

	/** Default game balance settings */
	static FGameBalance Default()
	{
		return FGameBalance();
	}
};
