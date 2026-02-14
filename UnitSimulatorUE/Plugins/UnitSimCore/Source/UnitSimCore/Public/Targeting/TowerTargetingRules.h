#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"

struct FUnit;
struct FTower;

/**
 * Static targeting rules for units selecting tower/unit targets.
 * Ported from Targeting/TowerTargetingRules.cs (49 lines)
 */
namespace TowerTargetingRules
{
	/**
	 * Select the nearest attackable tower for a unit.
	 * @return Tower index or -1 if none found
	 */
	UNITSIMCORE_API int32 SelectTowerTarget(
		const FUnit& Unit,
		const TArray<FTower>& Towers);

	/**
	 * Select the nearest attackable enemy unit.
	 * @return Unit index or -1 if none found
	 */
	UNITSIMCORE_API int32 SelectUnitTarget(
		const FUnit& Unit,
		const TArray<FUnit>& Enemies);

	/**
	 * Select the best target (unit or tower) based on target priority.
	 * @param OutUnitTargetIndex    Index of selected unit target (-1 if none)
	 * @param OutTowerTargetIndex   Index of selected tower target (-1 if none)
	 */
	UNITSIMCORE_API void SelectTarget(
		const FUnit& Unit,
		const TArray<FUnit>& Enemies,
		const TArray<FTower>& Towers,
		int32& OutUnitTargetIndex,
		int32& OutTowerTargetIndex);
}
