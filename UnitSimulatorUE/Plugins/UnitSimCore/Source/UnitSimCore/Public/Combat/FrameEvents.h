#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "FrameEvents.generated.h"

/** Damage type enum */
UENUM(BlueprintType)
enum class EDamageType : uint8
{
	Normal,
	Splash,
	DeathDamage,
	Spell,
	Tower
};

/** Unit-to-unit damage event */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FSimDamageEvent
{
	GENERATED_BODY()

	/** Source unit index (-1 for spells, etc.) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SourceIndex = -1;

	/** Target unit index */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TargetIndex = -1;

	/** Damage amount */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Amount = 0;

	/** Damage type */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EDamageType Type = EDamageType::Normal;
};

/** Tower-to-unit damage event */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FTowerDamageEvent
{
	GENERATED_BODY()

	/** Source tower index */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SourceTowerIndex = -1;

	/** Target unit index */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TargetIndex = -1;

	/** Damage amount */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Amount = 0;
};

/** Unit-to-tower damage event */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FDamageToTowerEvent
{
	GENERATED_BODY()

	/** Source unit index */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SourceIndex = -1;

	/** Target tower index */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TargetTowerIndex = -1;

	/** Damage amount */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Amount = 0;
};

/** Unit spawn request (produced by death spawn abilities) */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FUnitSpawnRequest
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName UnitId;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Position = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 HP = 0;
};

/**
 * Container for all events collected during a simulation frame.
 * Events are collected in Phase 1 (Collect) and applied in Phase 2 (Apply).
 * Ported from Combat/FrameEvents.cs (213 lines)
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FFrameEvents
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FSimDamageEvent> Damages;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FUnitSpawnRequest> Spawns;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FTowerDamageEvent> TowerDamages;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FDamageToTowerEvent> DamageToTowers;

	void AddDamage(int32 SourceIndex, int32 TargetIndex, int32 Amount, EDamageType Type = EDamageType::Normal);
	void AddSpawn(const FUnitSpawnRequest& Spawn);
	void AddTowerDamage(int32 SourceTowerIndex, int32 TargetIndex, int32 Amount);
	void AddDamageToTower(int32 SourceIndex, int32 TargetTowerIndex, int32 Amount);
	void Clear();

	int32 GetDamageCount() const { return Damages.Num(); }
	int32 GetSpawnCount() const { return Spawns.Num(); }
	int32 GetTowerDamageCount() const { return TowerDamages.Num(); }
	int32 GetDamageToTowerCount() const { return DamageToTowers.Num(); }
};
