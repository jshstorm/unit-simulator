#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "Abilities/AbilityTypes.h"
#include "UnitDefinition.generated.h"

/**
 * Defines the base stats of a unit type.
 * Used by DeathSpawn abilities and spawn systems to create units.
 * Ported from Units/UnitDefinition.cs (78 lines)
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FUnitDefinition
{
	GENERATED_BODY()

	/** Unit definition ID (e.g., "golemite", "skeleton") */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName UnitId;

	/** Display name */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString DisplayName;

	// === Base Stats ===

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxHP = 100;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Damage = 10;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackRange = 30.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float MoveSpeed = 4.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float TurnSpeed = 0.1f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Radius = 20.f;

	// === Unit Type ===

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitRole Role = EUnitRole::Melee;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EMovementLayer Layer = EMovementLayer::Ground;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETargetType CanTarget = ETargetType::Ground;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETargetPriority TargetPriority = ETargetPriority::Nearest;

	// === Abilities ===

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FAbilityData> Abilities;

	// Typed ability data (cached)
	bool bHasChargeAttack = false;
	FChargeAttackData ChargeAttackData;

	bool bHasSplashDamage = false;
	FSplashDamageData SplashDamageData;

	bool bHasShield = false;
	FShieldData ShieldData;

	bool bHasDeathSpawn = false;
	FDeathSpawnData DeathSpawnData;

	bool bHasDeathDamage = false;
	FDeathDamageData DeathDamageData;

	bool bHasStatusEffect = false;
	FStatusEffectAbilityData StatusEffectData;
};
