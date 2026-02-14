#include "Targeting/TowerTargetingRules.h"
#include "Units/Unit.h"
#include "Towers/Tower.h"

int32 TowerTargetingRules::SelectTowerTarget(
	const FUnit& Unit,
	const TArray<FTower>& Towers)
{
	int32 BestIndex = -1;
	float BestDistance = TNumericLimits<float>::Max();

	for (int32 i = 0; i < Towers.Num(); ++i)
	{
		const FTower& Tower = Towers[i];
		if (Tower.IsDestroyed()) continue;

		// Check if unit can target buildings
		if ((Unit.CanTarget & ETargetType::Building) == ETargetType::None) continue;

		const float Dist = FVector2D::Distance(Unit.Position, Tower.Position);
		if (Dist < BestDistance)
		{
			BestDistance = Dist;
			BestIndex = i;
		}
	}

	return BestIndex;
}

int32 TowerTargetingRules::SelectUnitTarget(
	const FUnit& Unit,
	const TArray<FUnit>& Enemies)
{
	int32 BestIndex = -1;
	float BestDistance = TNumericLimits<float>::Max();

	for (int32 i = 0; i < Enemies.Num(); ++i)
	{
		const FUnit& Enemy = Enemies[i];
		if (Enemy.bIsDead) continue;
		if (!Unit.CanAttackUnit(Enemy)) continue;

		const float Dist = FVector2D::Distance(Unit.Position, Enemy.Position);
		if (Dist < BestDistance)
		{
			BestDistance = Dist;
			BestIndex = i;
		}
	}

	return BestIndex;
}

void TowerTargetingRules::SelectTarget(
	const FUnit& Unit,
	const TArray<FUnit>& Enemies,
	const TArray<FTower>& Towers,
	int32& OutUnitTargetIndex,
	int32& OutTowerTargetIndex)
{
	OutUnitTargetIndex = -1;
	OutTowerTargetIndex = -1;

	// Buildings priority: prefer towers first
	if (Unit.TargetPriority == ETargetPriority::Buildings)
	{
		OutTowerTargetIndex = SelectTowerTarget(Unit, Towers);
		if (OutTowerTargetIndex >= 0)
		{
			return;
		}

		// Fallback to unit target
		OutUnitTargetIndex = SelectUnitTarget(Unit, Enemies);
		return;
	}

	// Default (Nearest): prefer enemy units, fallback to towers
	// Count living enemies
	bool bHasLivingEnemy = false;
	for (const FUnit& Enemy : Enemies)
	{
		if (!Enemy.bIsDead)
		{
			bHasLivingEnemy = true;
			break;
		}
	}

	if (bHasLivingEnemy)
	{
		OutUnitTargetIndex = SelectUnitTarget(Unit, Enemies);
		return;
	}

	OutTowerTargetIndex = SelectTowerTarget(Unit, Towers);
}
