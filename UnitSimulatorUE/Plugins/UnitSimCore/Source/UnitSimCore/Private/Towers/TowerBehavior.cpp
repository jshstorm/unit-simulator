#include "Towers/TowerBehavior.h"
#include "Towers/Tower.h"
#include "Units/Unit.h"
#include "Combat/FrameEvents.h"
#include "GameState/SimGameSession.h"

void FTowerBehavior::UpdateTowers(
	TArray<FTower>& Towers,
	const TArray<FUnit>& Enemies,
	FFrameEvents& Events,
	float DeltaTime)
{
	for (int32 i = 0; i < Towers.Num(); ++i)
	{
		UpdateTower(Towers[i], i, Enemies, Events, DeltaTime);
	}
}

void FTowerBehavior::UpdateTower(
	FTower& Tower,
	int32 TowerIndex,
	const TArray<FUnit>& Enemies,
	FFrameEvents& Events,
	float DeltaTime)
{
	if (Tower.IsDestroyed()) return;
	if (Tower.Type == ETowerType::King && !Tower.bIsActivated) return;

	Tower.UpdateCooldown(DeltaTime);
	ValidateAndUpdateTarget(Tower, Enemies);
	ProcessAttack(Tower, TowerIndex, Events);
}

void FTowerBehavior::ValidateAndUpdateTarget(FTower& Tower, const TArray<FUnit>& Enemies)
{
	// Validate current target
	if (Tower.CurrentTargetIndex >= 0)
	{
		if (Tower.CurrentTargetIndex >= Enemies.Num() ||
			Enemies[Tower.CurrentTargetIndex].bIsDead ||
			!Tower.CanAttackUnit(Enemies[Tower.CurrentTargetIndex]))
		{
			Tower.CurrentTargetIndex = -1;
		}
	}

	// Select new target if needed
	if (Tower.CurrentTargetIndex < 0)
	{
		Tower.CurrentTargetIndex = FindNearestTarget(Tower, Enemies);
	}
}

int32 FTowerBehavior::FindNearestTarget(const FTower& Tower, const TArray<FUnit>& Enemies)
{
	int32 BestIndex = -1;
	float BestDistance = TNumericLimits<float>::Max();

	for (int32 i = 0; i < Enemies.Num(); ++i)
	{
		const FUnit& Enemy = Enemies[i];
		if (Enemy.bIsDead) continue;
		if (!Tower.CanAttackUnit(Enemy)) continue;

		const float Dist = FVector2D::Distance(Tower.Position, Enemy.Position);
		if (Dist < BestDistance)
		{
			BestDistance = Dist;
			BestIndex = i;
		}
	}

	return BestIndex;
}

void FTowerBehavior::ProcessAttack(FTower& Tower, int32 TowerIndex, FFrameEvents& Events)
{
	if (!Tower.IsReadyToAttack()) return;
	if (Tower.CurrentTargetIndex < 0) return;

	Events.AddTowerDamage(TowerIndex, Tower.CurrentTargetIndex, Tower.Damage);
	Tower.OnAttackPerformed();
}

void FTowerBehavior::UpdateAllTowers(
	FSimGameSession& Session,
	const TArray<FUnit>& FriendlyUnits,
	const TArray<FUnit>& EnemyUnits,
	FFrameEvents& Events,
	float DeltaTime)
{
	// Friendly towers attack enemy units
	UpdateTowers(Session.FriendlyTowers, EnemyUnits, Events, DeltaTime);

	// Enemy towers attack friendly units
	UpdateTowers(Session.EnemyTowers, FriendlyUnits, Events, DeltaTime);
}
