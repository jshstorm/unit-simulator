#include "Skills/TowerSkillSystem.h"
#include "Units/Unit.h"
#include "Towers/Tower.h"

void FTowerSkillSystem::RegisterSkill(int32 TowerId, const FTowerSkill& Skill)
{
	TArray<FTowerSkill>& Skills = TowerSkills.FindOrAdd(TowerId);
	Skills.Add(Skill);
}

void FTowerSkillSystem::RegisterSkills(int32 TowerId, const TArray<FTowerSkill>& Skills)
{
	for (const FTowerSkill& Skill : Skills)
	{
		RegisterSkill(TowerId, Skill);
	}
}

FTowerSkill* FTowerSkillSystem::GetSkill(int32 TowerId, const FString& SkillId)
{
	TArray<FTowerSkill>* Skills = TowerSkills.Find(TowerId);
	if (Skills == nullptr) return nullptr;

	for (FTowerSkill& Skill : *Skills)
	{
		if (Skill.Id == SkillId) return &Skill;
	}
	return nullptr;
}

const TArray<FTowerSkill>* FTowerSkillSystem::GetSkills(int32 TowerId) const
{
	return TowerSkills.Find(TowerId);
}

bool FTowerSkillSystem::IsSkillOnCooldown(int32 TowerId, const FString& SkillId)
{
	const FTowerSkill* Skill = GetSkill(TowerId, SkillId);
	return Skill != nullptr && Skill->IsOnCooldown();
}

int32 FTowerSkillSystem::GetRemainingCooldown(int32 TowerId, const FString& SkillId)
{
	const FTowerSkill* Skill = GetSkill(TowerId, SkillId);
	return Skill != nullptr ? Skill->RemainingCooldownMs : 0;
}

FSkillActivationResult FTowerSkillSystem::ActivateSkill(
	int32 TowerId,
	const FString& SkillId,
	FTower* Tower,
	TArray<FUnit>& Enemies,
	const FVector2D* TargetPosition,
	int32 TargetUnitId)
{
	if (Tower == nullptr)
	{
		return FSkillActivationResult::CreateFailure(
			SkillErrorCodes::TowerNotFound,
			FString::Printf(TEXT("Tower '%d' not found"), TowerId));
	}

	if (Tower->IsDestroyed())
	{
		return FSkillActivationResult::CreateFailure(
			SkillErrorCodes::TowerNotFound,
			FString::Printf(TEXT("Tower '%d' is destroyed"), TowerId));
	}

	FTowerSkill* Skill = GetSkill(TowerId, SkillId);
	if (Skill == nullptr)
	{
		return FSkillActivationResult::CreateFailure(
			SkillErrorCodes::SkillNotFound,
			FString::Printf(TEXT("Skill '%s' not found on tower '%d'"), *SkillId, TowerId));
	}

	if (Skill->IsOnCooldown())
	{
		return FSkillActivationResult::CreateFailure(
			SkillErrorCodes::SkillOnCooldown,
			FString::Printf(TEXT("Skill is on cooldown. Remaining: %dms"), Skill->RemainingCooldownMs));
	}

	FTargetValidation Validation = ValidateTarget(*Skill, TargetPosition, TargetUnitId, Enemies);
	if (!Validation.bIsValid)
	{
		return FSkillActivationResult::CreateFailure(Validation.ErrorCode, Validation.ErrorMessage);
	}

	TArray<FSkillEffectResult> Effects = ApplySkillEffects(*Skill, *Tower, Enemies, TargetPosition, TargetUnitId);
	Skill->StartCooldown();

	return FSkillActivationResult::CreateSuccess(Skill->CooldownMs, Effects);
}

void FTowerSkillSystem::UpdateCooldowns(int32 DeltaMs)
{
	for (auto& Pair : TowerSkills)
	{
		for (FTowerSkill& Skill : Pair.Value)
		{
			Skill.UpdateCooldown(DeltaMs);
		}
	}
}

void FTowerSkillSystem::UpdateCooldowns(int32 TowerId, int32 DeltaMs)
{
	TArray<FTowerSkill>* Skills = TowerSkills.Find(TowerId);
	if (Skills == nullptr) return;

	for (FTowerSkill& Skill : *Skills)
	{
		Skill.UpdateCooldown(DeltaMs);
	}
}

void FTowerSkillSystem::ClearSkills(int32 TowerId)
{
	TowerSkills.Remove(TowerId);
}

void FTowerSkillSystem::ClearAllSkills()
{
	TowerSkills.Empty();
}

FTowerSkillSystem::FTargetValidation FTowerSkillSystem::ValidateTarget(
	const FTowerSkill& Skill,
	const FVector2D* TargetPosition,
	int32 TargetUnitId,
	const TArray<FUnit>& Enemies)
{
	switch (Skill.TargetType)
	{
	case ESkillTargetType::None:
		return { true, {}, {} };

	case ESkillTargetType::Position:
		if (TargetPosition == nullptr)
		{
			return { false, SkillErrorCodes::TargetRequired,
				TEXT("Target position is required for this skill") };
		}
		return { true, {}, {} };

	case ESkillTargetType::SingleUnit:
		if (TargetUnitId < 0)
		{
			return { false, SkillErrorCodes::TargetRequired,
				TEXT("Target unit is required for this skill") };
		}
		{
			bool bFound = false;
			for (const FUnit& Enemy : Enemies)
			{
				if (Enemy.Id == TargetUnitId && !Enemy.bIsDead)
				{
					bFound = true;
					break;
				}
			}
			if (!bFound)
			{
				return { false, SkillErrorCodes::TargetNotFound,
					FString::Printf(TEXT("Target unit '%d' not found or dead"), TargetUnitId) };
			}
		}
		return { true, {}, {} };

	default:
		return { true, {}, {} };
	}
}

TArray<FSkillEffectResult> FTowerSkillSystem::ApplySkillEffects(
	const FTowerSkill& Skill,
	const FTower& Tower,
	TArray<FUnit>& Enemies,
	const FVector2D* TargetPosition,
	int32 TargetUnitId)
{
	TArray<FSkillEffectResult> Effects;

	switch (Skill.EffectType)
	{
	case ESkillEffectType::TargetedDamage:
		Effects = ApplyTargetedDamage(Skill, Enemies, TargetUnitId);
		break;

	case ESkillEffectType::AreaOfEffect:
		Effects = ApplyAreaDamage(Skill, Tower, Enemies, TargetPosition);
		break;

	case ESkillEffectType::Buff:
	case ESkillEffectType::Debuff:
	case ESkillEffectType::Utility:
		// Phase 2: Implement buff/debuff system
		break;
	}

	return Effects;
}

TArray<FSkillEffectResult> FTowerSkillSystem::ApplyTargetedDamage(
	const FTowerSkill& Skill,
	TArray<FUnit>& Enemies,
	int32 TargetUnitId)
{
	TArray<FSkillEffectResult> Effects;
	if (TargetUnitId < 0) return Effects;

	for (FUnit& Enemy : Enemies)
	{
		if (Enemy.Id == TargetUnitId && !Enemy.bIsDead)
		{
			Enemy.TakeDamage(Skill.Damage);

			FSkillEffectResult Effect;
			Effect.Type = TEXT("Damage");
			Effect.TargetId = FString::FromInt(Enemy.Id);
			Effect.Value = Skill.Damage;
			Effects.Add(Effect);
			break;
		}
	}

	return Effects;
}

TArray<FSkillEffectResult> FTowerSkillSystem::ApplyAreaDamage(
	const FTowerSkill& Skill,
	const FTower& Tower,
	TArray<FUnit>& Enemies,
	const FVector2D* TargetPosition)
{
	TArray<FSkillEffectResult> Effects;
	const FVector2D Center = TargetPosition != nullptr ? *TargetPosition : Tower.Position;

	for (FUnit& Enemy : Enemies)
	{
		if (Enemy.bIsDead) continue;

		const float Distance = FVector2D::Distance(Center, Enemy.Position);
		if (Distance > Skill.Range) continue;

		Enemy.TakeDamage(Skill.Damage);

		FSkillEffectResult Effect;
		Effect.Type = TEXT("Damage");
		Effect.TargetId = FString::FromInt(Enemy.Id);
		Effect.Value = Skill.Damage;
		Effects.Add(Effect);
	}

	return Effects;
}
