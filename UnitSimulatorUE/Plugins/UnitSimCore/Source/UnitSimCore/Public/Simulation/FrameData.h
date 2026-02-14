#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "GameState/GameResult.h"
#include "FrameData.generated.h"

// Forward declarations
struct FUnit;
struct FTower;
struct FSimGameSession;

/**
 * Serialized state of a single unit within a frame snapshot.
 * Ported from FrameData.cs UnitStateData
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FUnitStateData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Id = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Label;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString UnitId;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString TargetPriority;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Role;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Faction;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsDead = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 HP = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Radius = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Speed = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float TurnSpeed = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackRange = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackCooldown = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EMovementLayer Layer = EMovementLayer::Ground;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETargetType CanTarget = ETargetType::Ground;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Damage = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 ShieldHP = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxShieldHP = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bHasChargeState = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsCharging = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsCharged = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float RequiredChargeDistance = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<EAbilityType> Abilities;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Position = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Velocity = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Forward = FVector2D(1.0, 0.0);

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D CurrentDestination = FVector2D::ZeroVector;

	/** Target unit ID (-1 = none) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TargetId = -1;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TakenSlotIndex = -1;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bHasAvoidanceTarget = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D AvoidanceTarget = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsMoving = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bInAttackRange = false;

	/** Create from a live FUnit */
	static FUnitStateData FromUnit(const FUnit& Unit, const TArray<FUnit>& AllEnemies);
};

/**
 * Serialized state of a tower within a frame snapshot.
 * Ported from FrameData.cs TowerStateData
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FTowerStateData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Id = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Type;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Faction;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Position = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Radius = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackRange = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxHP = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 CurrentHP = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsActivated = true;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackCooldown = 0.f;

	/** Create from a live FTower */
	static FTowerStateData FromTower(const FTower& Tower);
};

/**
 * Complete simulation frame snapshot.
 * Captures all state needed to render, save/load, or resume simulation.
 * Ported from FrameData.cs (531 lines)
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FFrameData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FrameNumber = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 CurrentWave = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 LivingFriendlyCount = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 LivingEnemyCount = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D MainTarget = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FUnitStateData> FriendlyUnits;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FUnitStateData> EnemyUnits;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FTowerStateData> FriendlyTowers;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FTowerStateData> EnemyTowers;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float ElapsedTime = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FriendlyCrowns = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 EnemyCrowns = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EGameResult GameResult = EGameResult::InProgress;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EWinCondition WinConditionType = EWinCondition::None;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsOvertime = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bAllWavesCleared = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bMaxFramesReached = false;

	/**
	 * Create a frame snapshot from live simulation state.
	 */
	static FFrameData FromSimulationState(
		int32 FrameNumber,
		const TArray<FUnit>& Friendlies,
		const TArray<FUnit>& Enemies,
		const FVector2D& MainTarget,
		int32 CurrentWave,
		bool bHasMoreWaves,
		const FSimGameSession* Session = nullptr);

	/** Serialize to JSON string */
	FString ToJson() const;

	/** Deserialize from JSON string */
	static bool FromJson(const FString& JsonString, FFrameData& OutFrameData);
};
