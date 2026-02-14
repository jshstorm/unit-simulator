#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "TowerSkill.generated.h"

/** Skill effect type */
UENUM(BlueprintType)
enum class ESkillEffectType : uint8
{
	TargetedDamage,
	AreaOfEffect,
	Buff,
	Debuff,
	Utility
};

/** Skill target type */
UENUM(BlueprintType)
enum class ESkillTargetType : uint8
{
	None,
	SingleUnit,
	Position
};

/**
 * Tower skill definition and runtime state.
 * Ported from Skills/TowerSkill.cs (233 lines)
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FTowerSkill
{
	GENERATED_BODY()

	/** Skill unique ID */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Id;

	/** Skill display name */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Name;

	/** Effect type */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ESkillEffectType EffectType = ESkillEffectType::TargetedDamage;

	/** Target type */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ESkillTargetType TargetType = ESkillTargetType::None;

	/** Cooldown time (milliseconds) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 CooldownMs = 0;

	/** Skill range (for area skills) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Range = 0.f;

	/** Base damage */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Damage = 0;

	/** Effect duration (milliseconds, for buff/debuff) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 DurationMs = 0;

	/** Buff/debuff value (percent, e.g. 20 = 20% increase) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 EffectValue = 0;

	// ════════════════════════════════════════════════════════════════════════
	// Runtime State
	// ════════════════════════════════════════════════════════════════════════

	/** Remaining cooldown (milliseconds) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 RemainingCooldownMs = 0;

	bool IsOnCooldown() const { return RemainingCooldownMs > 0; }

	void StartCooldown() { RemainingCooldownMs = CooldownMs; }

	void UpdateCooldown(int32 DeltaMs)
	{
		if (RemainingCooldownMs > 0)
		{
			RemainingCooldownMs = FMath::Max(0, RemainingCooldownMs - DeltaMs);
		}
	}

	void ResetCooldown() { RemainingCooldownMs = 0; }
};

/**
 * Skill activation result.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FSkillEffectResult
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Type;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString TargetId;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Value = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 DurationMs = 0;
};

USTRUCT(BlueprintType)
struct UNITSIMCORE_API FSkillActivationResult
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bSuccess = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 CooldownMs = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FSkillEffectResult> Effects;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString ErrorCode;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString ErrorMessage;

	static FSkillActivationResult CreateSuccess(int32 InCooldownMs, const TArray<FSkillEffectResult>& InEffects)
	{
		FSkillActivationResult Result;
		Result.bSuccess = true;
		Result.CooldownMs = InCooldownMs;
		Result.Effects = InEffects;
		return Result;
	}

	static FSkillActivationResult CreateFailure(const FString& InErrorCode, const FString& InErrorMessage)
	{
		FSkillActivationResult Result;
		Result.bSuccess = false;
		Result.ErrorCode = InErrorCode;
		Result.ErrorMessage = InErrorMessage;
		return Result;
	}
};

/** Skill error codes */
namespace SkillErrorCodes
{
	const FString InvalidRequest = TEXT("INVALID_REQUEST");
	const FString InvalidTowerId = TEXT("INVALID_TOWER_ID");
	const FString TowerNotFound = TEXT("TOWER_NOT_FOUND");
	const FString InvalidSkillId = TEXT("INVALID_SKILL_ID");
	const FString SkillNotFound = TEXT("SKILL_NOT_FOUND");
	const FString SkillOnCooldown = TEXT("SKILL_ON_COOLDOWN");
	const FString TargetRequired = TEXT("TARGET_REQUIRED");
	const FString InvalidTargetPosition = TEXT("INVALID_TARGET_POSITION");
	const FString TargetNotFound = TEXT("TARGET_NOT_FOUND");
	const FString InternalError = TEXT("INTERNAL_ERROR");
}
