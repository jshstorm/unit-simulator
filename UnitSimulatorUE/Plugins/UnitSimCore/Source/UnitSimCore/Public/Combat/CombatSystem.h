#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "Combat/FrameEvents.h"

struct FUnit;
struct FTower;

/**
 * Combat system: splash damage, death spawn, death damage, charge state.
 * Uses 2-Phase Update pattern:
 *   Phase 1 (Collect): CollectAttackEvents - no state changes
 *   Phase 2 (Apply): CreateDeathSpawnRequests / ApplyDeathDamage
 * Ported from Combat/CombatSystem.cs (210 lines)
 */
class UNITSIMCORE_API FCombatSystem
{
public:
	// ════════════════════════════════════════════════════════════════════════
	// Phase 1: Collect (no state changes except attacker charge consumption)
	// ════════════════════════════════════════════════════════════════════════

	/**
	 * Collect damage events for a unit attack.
	 * Handles splash damage if attacker has SplashDamage ability.
	 */
	void CollectAttackEvents(
		FUnit& Attacker,
		int32 AttackerIndex,
		const FUnit& Target,
		int32 TargetIndex,
		TArray<FUnit>& AllEnemies,
		FFrameEvents& Events);

	// ════════════════════════════════════════════════════════════════════════
	// Phase 2: Death Processing
	// ════════════════════════════════════════════════════════════════════════

	/**
	 * Create spawn requests for a dead unit's DeathSpawn ability.
	 * @return Spawn requests to process
	 */
	TArray<FUnitSpawnRequest> CreateDeathSpawnRequests(const FUnit& DeadUnit);

	/**
	 * Apply death damage from a dead unit to nearby enemies.
	 * @return Indices of units newly killed by death damage
	 */
	TArray<int32> ApplyDeathDamage(const FUnit& DeadUnit, TArray<FUnit>& Enemies);

	// ════════════════════════════════════════════════════════════════════════
	// Charge State
	// ════════════════════════════════════════════════════════════════════════

	/** Update charge state for a unit relative to its target */
	void UpdateChargeState(FUnit& Unit, int32 TargetIndex, const TArray<FUnit>& AllUnits);

private:
	/** Collect splash damage events around main target */
	void CollectSplashDamage(
		const FUnit& Attacker,
		int32 AttackerIndex,
		int32 MainTargetIndex,
		const FVector2D& MainTargetPosition,
		int32 BaseDamage,
		TArray<FUnit>& AllEnemies,
		FFrameEvents& Events);
};
