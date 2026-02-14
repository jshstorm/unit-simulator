#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"

// Forward declarations
struct FUnit;
struct FTower;
struct FFrameEvents;
class FSimulatorCore;

/**
 * Enemy AI behavior: target scoring/selection, slot-based positioning, tower combat.
 * Uses 2-Phase Update pattern: only collects events in Phase 1.
 * Ported from EnemyBehavior.cs (316 lines)
 */
class UNITSIMCORE_API FEnemyBehavior
{
public:
	/**
	 * Update all enemy units for one frame.
	 * @param Sim                Simulator core (for pathfinding, terrain, current frame)
	 * @param Enemies            Enemy unit array (modified: position, velocity, targeting)
	 * @param Friendlies         Friendly unit array (targets for enemies)
	 * @param FriendlyTowers     Friendly tower array
	 * @param Events             Frame events collector
	 */
	void UpdateEnemySquad(
		FSimulatorCore& Sim,
		TArray<FUnit>& Enemies,
		TArray<FUnit>& Friendlies,
		TArray<FTower>& FriendlyTowers,
		FFrameEvents& Events);

private:
	// ════════════════════════════════════════════════════════════════════════
	// Targeting
	// ════════════════════════════════════════════════════════════════════════

	void UpdateEnemyTarget(
		FUnit& Enemy,
		int32 EnemyIndex,
		TArray<FUnit>& LivingFriendlies,
		TArray<FTower>& FriendlyTowers);

	int32 SelectBestTarget(
		const FUnit& Enemy,
		const TArray<FUnit>& Candidates);

	float EvaluateTargetScore(
		const FUnit& Enemy,
		const FUnit& Candidate);

	// ════════════════════════════════════════════════════════════════════════
	// Movement & Combat
	// ════════════════════════════════════════════════════════════════════════

	void UpdateEnemyMovement(
		FSimulatorCore& Sim,
		FUnit& Enemy,
		int32 EnemyIndex,
		TArray<FUnit>& Enemies,
		TArray<FUnit>& LivingFriendlies,
		TArray<FTower>& FriendlyTowers,
		FFrameEvents& Events);

	void UpdateTowerCombat(
		FSimulatorCore& Sim,
		FUnit& Enemy,
		int32 EnemyIndex,
		FTower& TargetTower,
		TArray<FUnit>& Enemies,
		TArray<FUnit>& LivingFriendlies,
		FFrameEvents& Events);

	void TryAttack(
		FUnit& Attacker,
		int32 AttackerIndex,
		FUnit& Target,
		int32 TargetIndex,
		TArray<FUnit>& AllFriendlies,
		FFrameEvents& Events);

	// ════════════════════════════════════════════════════════════════════════
	// Movement (shared utility)
	// ════════════════════════════════════════════════════════════════════════

	void MoveUnit(
		FSimulatorCore& Sim,
		FUnit& Unit,
		int32 UnitIndex,
		const FVector2D& Destination,
		TArray<FUnit>& Allies,
		TArray<FUnit>& Opponents);
};
