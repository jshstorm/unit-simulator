#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "AbilityTypes.generated.h"

/**
 * Base ability data struct.
 * All specific ability data types inherit from this.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FAbilityData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EAbilityType Type = EAbilityType::ChargeAttack;
};

/**
 * ChargeAttack ability data.
 * Applies damage multiplier after moving a certain distance.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FChargeAttackData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EAbilityType Type = EAbilityType::ChargeAttack;

	/** Minimum distance to trigger charge */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float TriggerDistance = 150.f;

	/** Required travel distance to complete charge */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float RequiredChargeDistance = 100.f;

	/** Damage multiplier on charged attack */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float DamageMultiplier = 2.0f;

	/** Speed multiplier during charge */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float SpeedMultiplier = 2.0f;
};

/**
 * SplashDamage ability data.
 * Deals damage to nearby enemies on attack.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FSplashDamageData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EAbilityType Type = EAbilityType::SplashDamage;

	/** Splash radius */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Radius = 60.f;

	/** Damage falloff with distance (0 = no falloff, 1 = full falloff) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float DamageFalloff = 0.f;
};

/**
 * Shield ability data.
 * Provides a separate shield HP pool before main HP.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FShieldData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EAbilityType Type = EAbilityType::Shield;

	/** Maximum shield HP */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxShieldHP = 200;

	/** Whether shield blocks stun */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool BlocksStun = false;

	/** Whether shield blocks knockback */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool BlocksKnockback = false;
};

/**
 * DeathSpawn ability data.
 * Spawns units when this unit dies.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FDeathSpawnData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EAbilityType Type = EAbilityType::DeathSpawn;

	/** Unit definition ID to spawn */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName SpawnUnitId;

	/** Number of units to spawn */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SpawnCount = 2;

	/** Spawn scatter radius */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float SpawnRadius = 30.f;

	/** HP of spawned units (0 = use default) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SpawnUnitHP = 0;
};

/**
 * DeathDamage ability data.
 * Deals explosion damage when this unit dies.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FDeathDamageData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EAbilityType Type = EAbilityType::DeathDamage;

	/** Explosion damage amount */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Damage = 100;

	/** Explosion radius */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Radius = 60.f;

	/** Knockback distance (0 = no knockback) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float KnockbackDistance = 0.f;
};

/**
 * StatusEffect ability data.
 * Applies status effects to targets.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FStatusEffectAbilityData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EAbilityType Type = EAbilityType::StatusEffect;

	/** Status effect type to apply (bitmask flags) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Meta = (Bitmask, BitmaskEnum = "/Script/UnitSimCore.EStatusEffectType"))
	int32 AppliedEffect = 0;

	/** Effect duration in seconds */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float EffectDuration = 0.f;

	/** Effect magnitude (slow ratio, damage multiplier, etc.) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float EffectMagnitude = 1.0f;

	/** Effect range (0 = target only) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float EffectRange = 0.f;

	/** Affected target type */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETargetType AffectedTargets = ETargetType::Ground;
};
