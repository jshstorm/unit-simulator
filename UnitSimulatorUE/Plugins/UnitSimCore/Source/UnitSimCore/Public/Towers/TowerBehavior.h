#pragma once

#include "CoreMinimal.h"

struct FTower;
struct FUnit;
struct FFrameEvents;
struct FSimGameSession;

/**
 * Tower behavior: targeting, attack, cooldown.
 * Ported from Towers/TowerBehavior.cs (128 lines)
 */
class UNITSIMCORE_API FTowerBehavior
{
public:
	/**
	 * Update a list of towers against enemy units.
	 * @param Towers       Tower array (modified: cooldown, target)
	 * @param TowerCount   Number of towers
	 * @param Enemies      Enemy unit array
	 * @param EnemyCount   Number of enemies
	 * @param Events       Frame events to collect attack events into
	 * @param DeltaTime    Frame time in seconds
	 */
	void UpdateTowers(
		TArray<FTower>& Towers,
		const TArray<FUnit>& Enemies,
		FFrameEvents& Events,
		float DeltaTime);

	/**
	 * Update all towers for both factions.
	 * @param Session          Game session with tower lists
	 * @param FriendlyUnits    Friendly units (targets for enemy towers)
	 * @param EnemyUnits       Enemy units (targets for friendly towers)
	 * @param Events           Frame events collector
	 * @param DeltaTime        Frame time in seconds
	 */
	void UpdateAllTowers(
		FSimGameSession& Session,
		const TArray<FUnit>& FriendlyUnits,
		const TArray<FUnit>& EnemyUnits,
		FFrameEvents& Events,
		float DeltaTime);

private:
	/** Update a single tower */
	void UpdateTower(
		FTower& Tower,
		int32 TowerIndex,
		const TArray<FUnit>& Enemies,
		FFrameEvents& Events,
		float DeltaTime);

	/** Validate current target and select new one if needed */
	void ValidateAndUpdateTarget(FTower& Tower, const TArray<FUnit>& Enemies);

	/** Find nearest valid target unit for this tower */
	int32 FindNearestTarget(const FTower& Tower, const TArray<FUnit>& Enemies);

	/** Process tower attack */
	void ProcessAttack(FTower& Tower, int32 TowerIndex, FFrameEvents& Events);
};
