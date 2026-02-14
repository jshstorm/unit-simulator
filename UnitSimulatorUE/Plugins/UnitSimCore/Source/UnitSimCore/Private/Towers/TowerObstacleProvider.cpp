#include "Towers/TowerObstacleProvider.h"
#include "Towers/Tower.h"
#include "GameConstants.h"

FTowerObstacleProvider::FTowerObstacleProvider(const FTower* InTowers, int32 InCount)
	: Towers(InTowers)
	, TowerCount(InCount)
{
}

TArray<FObstacleRect> FTowerObstacleProvider::GetUnwalkableRects() const
{
	// Towers have no rectangular obstacles
	return TArray<FObstacleRect>();
}

TArray<FObstacleCircle> FTowerObstacleProvider::GetUnwalkableCircles() const
{
	TArray<FObstacleCircle> Circles;

	if (Towers == nullptr) return Circles;

	Circles.Reserve(TowerCount);
	for (int32 i = 0; i < TowerCount; ++i)
	{
		const FTower& Tower = Towers[i];
		// Destroyed towers remain as obstacles (rubble)
		const float EffectiveRadius = Tower.Radius + UnitSimConstants::TOWER_COLLISION_PADDING;

		FObstacleCircle Circle;
		Circle.Center = Tower.Position;
		Circle.Radius = EffectiveRadius;
		Circles.Add(Circle);
	}

	return Circles;
}
