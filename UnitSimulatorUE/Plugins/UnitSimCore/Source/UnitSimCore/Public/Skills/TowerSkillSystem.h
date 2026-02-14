#pragma once

#include "CoreMinimal.h"
#include "Skills/TowerSkill.h"

struct FUnit;
struct FTower;

/**
 * Tower skill registry, activation, and cooldown management.
 * Ported from Skills/TowerSkillSystem.cs (260 lines)
 */
class UNITSIMCORE_API FTowerSkillSystem
{
public:
	/** Register a skill for a tower */
	void RegisterSkill(int32 TowerId, const FTowerSkill& Skill);

	/** Register multiple skills for a tower */
	void RegisterSkills(int32 TowerId, const TArray<FTowerSkill>& Skills);

	/** Get a specific skill by tower and skill ID */
	FTowerSkill* GetSkill(int32 TowerId, const FString& SkillId);

	/** Get all skills for a tower (read-only) */
	const TArray<FTowerSkill>* GetSkills(int32 TowerId) const;

	/** Check if a skill is on cooldown */
	bool IsSkillOnCooldown(int32 TowerId, const FString& SkillId);

	/** Get remaining cooldown in milliseconds */
	int32 GetRemainingCooldown(int32 TowerId, const FString& SkillId);

	/** Activate a skill */
	FSkillActivationResult ActivateSkill(
		int32 TowerId,
		const FString& SkillId,
		FTower* Tower,
		TArray<FUnit>& Enemies,
		const FVector2D* TargetPosition = nullptr,
		int32 TargetUnitId = -1);

	/** Update cooldowns for all skills */
	void UpdateCooldowns(int32 DeltaMs);

	/** Update cooldowns for a specific tower */
	void UpdateCooldowns(int32 TowerId, int32 DeltaMs);

	/** Clear skills for a tower */
	void ClearSkills(int32 TowerId);

	/** Clear all skills */
	void ClearAllSkills();

private:
	TMap<int32, TArray<FTowerSkill>> TowerSkills;

	struct FTargetValidation
	{
		bool bIsValid;
		FString ErrorCode;
		FString ErrorMessage;
	};

	FTargetValidation ValidateTarget(
		const FTowerSkill& Skill,
		const FVector2D* TargetPosition,
		int32 TargetUnitId,
		const TArray<FUnit>& Enemies);

	TArray<FSkillEffectResult> ApplySkillEffects(
		const FTowerSkill& Skill,
		const FTower& Tower,
		TArray<FUnit>& Enemies,
		const FVector2D* TargetPosition,
		int32 TargetUnitId);

	TArray<FSkillEffectResult> ApplyTargetedDamage(
		const FTowerSkill& Skill,
		TArray<FUnit>& Enemies,
		int32 TargetUnitId);

	TArray<FSkillEffectResult> ApplyAreaDamage(
		const FTowerSkill& Skill,
		const FTower& Tower,
		TArray<FUnit>& Enemies,
		const FVector2D* TargetPosition);
};
