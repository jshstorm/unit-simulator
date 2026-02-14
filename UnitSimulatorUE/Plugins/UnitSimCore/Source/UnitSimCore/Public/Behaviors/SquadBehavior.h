#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"

// Forward declarations
struct FUnit;
struct FTower;
struct FFrameEvents;
class FSimulatorCore;

/**
 * Friendly squad behavior: formation movement, combat targeting, tower assault.
 * Uses 2-Phase Update pattern: only collects events in Phase 1.
 * Ported from SquadBehavior.cs (387 lines)
 */
class UNITSIMCORE_API FSquadBehavior
{
public:
	/**
	 * Update all friendly units for one frame.
	 * @param Sim            Simulator core (for pathfinding, terrain, current frame)
	 * @param Friendlies     Friendly unit array (modified: position, velocity, targeting)
	 * @param Enemies        Enemy unit array
	 * @param EnemyTowers    Enemy tower array
	 * @param MainTarget     Fallback movement target
	 * @param Events         Frame events collector (Phase 1: damage events only)
	 */
	void UpdateFriendlySquad(
		FSimulatorCore& Sim,
		TArray<FUnit>& Friendlies,
		TArray<FUnit>& Enemies,
		TArray<FTower>& EnemyTowers,
		const FVector2D& MainTarget,
		FFrameEvents& Events);

private:
	/** Current squad target index (-1 = none) */
	int32 SquadTargetIndex = -1;

	/** Rally point for formation movement */
	FVector2D RallyPoint = FVector2D::ZeroVector;

	/** Formation offsets for followers relative to leader */
	static const TArray<FVector2D>& GetFormationOffsets();

	// ════════════════════════════════════════════════════════════════════════
	// Squad Target & Rally
	// ════════════════════════════════════════════════════════════════════════

	void UpdateSquadTargetAndRallyPoint(
		TArray<FUnit>& Friendlies,
		TArray<FUnit>& LivingEnemies);

	// ════════════════════════════════════════════════════════════════════════
	// Formation
	// ════════════════════════════════════════════════════════════════════════

	void UpdateFormation(
		FSimulatorCore& Sim,
		TArray<FUnit>& Friendlies,
		const TSet<int32>* EngagedIndices = nullptr);

	// ════════════════════════════════════════════════════════════════════════
	// Engagement Detection
	// ════════════════════════════════════════════════════════════════════════

	TSet<int32> DetermineEngagedUnits(
		TArray<FUnit>& Friendlies,
		TArray<FUnit>& LivingEnemies);

	bool IsUnitReadyToEngage(
		const FUnit& Friendly,
		const TArray<FUnit>& LivingEnemies);

	// ════════════════════════════════════════════════════════════════════════
	// Combat
	// ════════════════════════════════════════════════════════════════════════

	void UpdateCombatBehavior(
		FSimulatorCore& Sim,
		TArray<FUnit>& Friendlies,
		TArray<FUnit>& LivingEnemies,
		TArray<FTower>& EnemyTowers,
		const TSet<int32>& EngagedIndices,
		FFrameEvents& Events);

	void UpdateUnitTarget(
		FUnit& Friendly,
		int32 FriendlyIndex,
		TArray<FUnit>& LivingEnemies,
		TArray<FTower>& EnemyTowers);

	void UpdateCombat(
		FSimulatorCore& Sim,
		FUnit& Friendly,
		int32 FriendlyIndex,
		TArray<FUnit>& LivingEnemies,
		TArray<FTower>& EnemyTowers,
		TArray<FUnit>& Friendlies,
		FFrameEvents& Events);

	// ════════════════════════════════════════════════════════════════════════
	// Tower Assault
	// ════════════════════════════════════════════════════════════════════════

	void UpdateTowerAssault(
		FSimulatorCore& Sim,
		TArray<FUnit>& Friendlies,
		TArray<FTower>& EnemyTowers,
		FFrameEvents& Events);

	void UpdateTowerCombat(
		FSimulatorCore& Sim,
		FUnit& Unit,
		int32 UnitIndex,
		FTower& TargetTower,
		TArray<FUnit>& Friendlies,
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
		TArray<FUnit>* Opponents);

	// ════════════════════════════════════════════════════════════════════════
	// Helpers
	// ════════════════════════════════════════════════════════════════════════

	void ResetSquadState(TArray<FUnit>& Friendlies);

	void MoveToMainTarget(
		FSimulatorCore& Sim,
		TArray<FUnit>& Friendlies,
		const FVector2D& MainTarget);
};
