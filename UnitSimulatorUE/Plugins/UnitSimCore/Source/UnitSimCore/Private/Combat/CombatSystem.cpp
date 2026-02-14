#include "Combat/CombatSystem.h"
#include "Units/Unit.h"
#include "Abilities/AbilityTypes.h"

void FCombatSystem::CollectAttackEvents(
	FUnit& Attacker,
	int32 AttackerIndex,
	const FUnit& Target,
	int32 TargetIndex,
	TArray<FUnit>& AllEnemies,
	FFrameEvents& Events)
{
	if (Target.bIsDead) return;

	const int32 Damage = Attacker.GetEffectiveDamage();

	// Primary target damage event
	Events.AddDamage(AttackerIndex, TargetIndex, Damage, EDamageType::Normal);

	// Splash damage events
	if (Attacker.bHasSplashDamage)
	{
		CollectSplashDamage(Attacker, AttackerIndex, TargetIndex, Target.Position,
			Damage, AllEnemies, Events);
	}

	// Post-attack processing (charge consumption etc.)
	Attacker.OnAttackPerformed();
}

void FCombatSystem::CollectSplashDamage(
	const FUnit& Attacker,
	int32 AttackerIndex,
	int32 MainTargetIndex,
	const FVector2D& MainTargetPosition,
	int32 BaseDamage,
	TArray<FUnit>& AllEnemies,
	FFrameEvents& Events)
{
	const FSplashDamageData& SplashData = Attacker.SplashDamageAbility;

	for (int32 i = 0; i < AllEnemies.Num(); ++i)
	{
		const FUnit& Enemy = AllEnemies[i];
		if (i == MainTargetIndex || Enemy.bIsDead) continue;
		if (!Attacker.CanAttackUnit(Enemy)) continue;

		const float Distance = FVector2D::Distance(MainTargetPosition, Enemy.Position);
		if (Distance > SplashData.Radius) continue;

		// Distance-based damage falloff
		int32 SplashDamage = BaseDamage;
		if (SplashData.DamageFalloff > 0.f)
		{
			const float FalloffFactor = 1.f - (Distance / SplashData.Radius) * SplashData.DamageFalloff;
			SplashDamage = static_cast<int32>(BaseDamage * FMath::Max(0.f, FalloffFactor));
		}

		if (SplashDamage > 0)
		{
			Events.AddDamage(AttackerIndex, i, SplashDamage, EDamageType::Splash);
		}
	}
}

TArray<FUnitSpawnRequest> FCombatSystem::CreateDeathSpawnRequests(const FUnit& DeadUnit)
{
	TArray<FUnitSpawnRequest> Spawns;

	if (!DeadUnit.bHasDeathSpawn) return Spawns;

	const FDeathSpawnData& SpawnData = DeadUnit.DeathSpawnAbility;
	if (SpawnData.SpawnCount <= 0) return Spawns;

	Spawns.Reserve(SpawnData.SpawnCount);
	for (int32 i = 0; i < SpawnData.SpawnCount; ++i)
	{
		const float Angle = (2.f * UE_PI / SpawnData.SpawnCount) * i;
		const FVector2D Offset(FMath::Cos(Angle), FMath::Sin(Angle));
		const FVector2D SpawnPos = DeadUnit.Position + Offset * SpawnData.SpawnRadius;

		FUnitSpawnRequest Request;
		Request.UnitId = SpawnData.SpawnUnitId;
		Request.Position = SpawnPos;
		Request.Faction = DeadUnit.Faction;
		Request.HP = SpawnData.SpawnUnitHP;
		Spawns.Add(Request);
	}

	return Spawns;
}

TArray<int32> FCombatSystem::ApplyDeathDamage(const FUnit& DeadUnit, TArray<FUnit>& Enemies)
{
	TArray<int32> NewlyDead;

	if (!DeadUnit.bHasDeathDamage) return NewlyDead;

	const FDeathDamageData& DmgData = DeadUnit.DeathDamageAbility;
	if (DmgData.Damage <= 0) return NewlyDead;

	for (int32 i = 0; i < Enemies.Num(); ++i)
	{
		FUnit& Enemy = Enemies[i];
		if (Enemy.bIsDead) continue;

		const float Distance = FVector2D::Distance(DeadUnit.Position, Enemy.Position);
		if (Distance > DmgData.Radius) continue;

		const bool bWasAlive = !Enemy.bIsDead;
		Enemy.TakeDamage(DmgData.Damage);

		// Knockback
		if (DmgData.KnockbackDistance > 0.f && !Enemy.bIsDead)
		{
			FVector2D KnockbackDir = Enemy.Position - DeadUnit.Position;
			const float Len = KnockbackDir.Size();
			if (Len > KINDA_SMALL_NUMBER)
			{
				KnockbackDir /= Len;
				Enemy.Position += KnockbackDir * DmgData.KnockbackDistance;
			}
		}

		if (bWasAlive && Enemy.bIsDead)
		{
			NewlyDead.Add(i);
		}
	}

	return NewlyDead;
}

void FCombatSystem::UpdateChargeState(FUnit& Unit, int32 TargetIndex, const TArray<FUnit>& AllUnits)
{
	if (!Unit.bHasChargeAbility) return;

	const FChargeAttackData& ChargeData = Unit.ChargeAttackAbility;

	// No target or dead target -> reset charge
	if (TargetIndex < 0 || TargetIndex >= AllUnits.Num() || AllUnits[TargetIndex].bIsDead)
	{
		Unit.ChargeState.Reset();
		return;
	}

	const FUnit& Target = AllUnits[TargetIndex];
	const float DistanceToTarget = FVector2D::Distance(Unit.Position, Target.Position);

	// Start charging if beyond trigger distance
	if (!Unit.ChargeState.bIsCharging && DistanceToTarget >= ChargeData.TriggerDistance)
	{
		Unit.ChargeState.StartCharge(Unit.Position, ChargeData.RequiredChargeDistance);
	}

	// Update charge distance while charging
	if (Unit.ChargeState.bIsCharging)
	{
		Unit.ChargeState.UpdateChargeDistance(Unit.Position);
	}
}
