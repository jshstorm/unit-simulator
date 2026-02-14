#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "UnitStats.generated.h"

/**
 * Runtime unit stats data.
 * Converted from ReferenceModels UnitReference for use in SimulatorCore.
 * Ported from Contracts/UnitStats.cs
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FUnitStats
{
	GENERATED_BODY()

	/** Display name */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString DisplayName = TEXT("Unknown");

	/** Maximum HP */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 HP = 100;

	/** Base attack damage */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Damage = 10;

	/** Movement speed */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float MoveSpeed = 4.0f;

	/** Turn speed (radians/frame) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float TurnSpeed = 0.1f;

	/** Attack range */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackRange = 30.f;

	/** Collision radius */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Radius = 20.f;

	/** Attacks per second */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackSpeed = 1.0f;

	/** Unit role */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitRole Role = EUnitRole::Melee;

	/** Movement layer (Ground/Air) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EMovementLayer Layer = EMovementLayer::Ground;

	/** Targetable types */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETargetType CanTarget = ETargetType::Ground;

	/** Target priority */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETargetPriority TargetPriority = ETargetPriority::Nearest;

	/** Attack type */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EAttackType AttackType = EAttackType::Melee;

	/** Base shield HP */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 ShieldHP = 0;

	/** Number of units spawned on deploy (for Swarm units) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SpawnCount = 1;

	/** Skill IDs */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FName> Skills;

	/** Create default UnitStats */
	static FUnitStats Default()
	{
		return FUnitStats();
	}
};
