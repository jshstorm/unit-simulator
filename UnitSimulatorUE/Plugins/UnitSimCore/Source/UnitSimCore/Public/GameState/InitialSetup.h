#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "Terrain/MapLayout.h"
#include "InitialSetup.generated.h"

/**
 * Individual tower initial setup.
 * Ported from Contracts/InitialSetup.cs
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FTowerSetup
{
	GENERATED_BODY()

	/** Tower type (King / Princess) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETowerType Type = ETowerType::Princess;

	/** Faction */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	/** Tower position (optional; ZeroVector = use default) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Position = FVector2D::ZeroVector;

	/** Whether position is explicitly set */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bHasPosition = false;

	/** Initial HP (-1 = use max HP) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 InitialHP = -1;

	/** King tower activation state (-1 = default, 0 = false, 1 = true) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 IsActivated = -1;
};

/**
 * Initial unit spawn setup (for test/tutorial).
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FUnitSpawnSetup
{
	GENERATED_BODY()

	/** Unit reference ID */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName UnitId;

	/** Faction */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	/** Spawn position */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Position = FVector2D::ZeroVector;

	/** HP override (-1 = use reference data) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 HP = -1;

	/** Spawn count */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Count = 1;

	/** Spawn scatter radius (when Count > 1) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float SpawnRadius = 30.f;
};

/**
 * Game time settings.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FGameTimeSetup
{
	GENERATED_BODY()

	/** Regular time in seconds (default 180s = 3 min) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float RegularTime = 180.f;

	/** Max game time in seconds (default 300s = 5 min) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float MaxGameTime = 300.f;
};

/**
 * Initial setup for a simulation.
 * Defines tower placement, initial units, and game time settings.
 * Ported from Contracts/InitialSetup.cs (169 lines)
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FInitialSetup
{
	GENERATED_BODY()

	/** Tower initial setups (both factions) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FTowerSetup> Towers;

	/** Initial unit spawn requests (for test/tutorial) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FUnitSpawnSetup> InitialUnits;

	/** Game time setup */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FGameTimeSetup GameTime;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bHasGameTime = true;

	/** Create Clash Royale standard layout (6 towers, no initial units) */
	static FInitialSetup CreateClashRoyaleStandard();
};

/**
 * Tower setup defaults.
 */
namespace TowerSetupDefaults
{
	/** Clash Royale standard 6-tower layout */
	UNITSIMCORE_API TArray<FTowerSetup> ClashRoyaleStandard();
}
