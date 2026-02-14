#pragma once

#include "CoreMinimal.h"
#include "Commands/ISimulationCommand.h"
#include "GameConstants.h"
#include "SimulationCommands.generated.h"

/**
 * Command to spawn a new unit in the simulation.
 * Ported from Commands/SpawnUnitCommand.cs
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FSpawnUnitCommand
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FrameNumber = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Position = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitRole Role = EUnitRole::Melee;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	/** Optional HP override (-1 = use default) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 HP = -1;

	/** Optional speed override (-1 = use default) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Speed = -1.f;

	/** Optional turn speed override (-1 = use default) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float TurnSpeed = -1.f;
};

/**
 * Command to move a unit to a new position.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FMoveUnitCommand
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FrameNumber = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 UnitId = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Destination = FVector2D::ZeroVector;
};

/**
 * Command to deal damage to a unit.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FDamageUnitCommand
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FrameNumber = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 UnitId = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Damage = 0;
};

/**
 * Command to kill a unit immediately.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FKillUnitCommand
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FrameNumber = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 UnitId = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;
};

/**
 * Command to revive a dead unit.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FReviveUnitCommand
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FrameNumber = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 UnitId = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 HP = 0;
};

/**
 * Command to set a unit's health directly.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FSetUnitHealthCommand
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FrameNumber = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 UnitId = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 HP = 0;
};

/**
 * Command to remove a unit from the simulation entirely.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FRemoveUnitCommand
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FrameNumber = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 UnitId = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;
};
