#include "Behaviors/EnemyBehavior.h"
#include "Units/Unit.h"
#include "Towers/Tower.h"
#include "Combat/FrameEvents.h"
#include "Combat/CombatSystem.h"
#include "Combat/AvoidanceSystem.h"
#include "Targeting/TowerTargetingRules.h"
#include "Pathfinding/PathProgressMonitor.h"
#include "Simulation/SimulatorCore.h"

// ============================================================================
// Main Update
// ============================================================================

void FEnemyBehavior::UpdateEnemySquad(
	FSimulatorCore& Sim,
	TArray<FUnit>& Enemies,
	TArray<FUnit>& Friendlies,
	TArray<FTower>& FriendlyTowers,
	FFrameEvents& Events)
{
	// Check if any targets exist
	bool bAnyLivingFriendly = false;
	for (const FUnit& F : Friendlies)
	{
		if (!F.bIsDead) { bAnyLivingFriendly = true; break; }
	}
	bool bAnyLivingTower = false;
	for (const FTower& T : FriendlyTowers)
	{
		if (!T.IsDestroyed()) { bAnyLivingTower = true; break; }
	}

	if (!bAnyLivingFriendly && !bAnyLivingTower)
	{
		// No targets: stop all enemies
		for (FUnit& Enemy : Enemies)
		{
			Enemy.Velocity = FVector2D::ZeroVector;
			Enemy.ClearMovementPath();
		}
		return;
	}

	for (int32 i = 0; i < Enemies.Num(); i++)
	{
		FUnit& Enemy = Enemies[i];
		if (Enemy.bIsDead) continue;

		Enemy.AttackCooldown = FMath::Max(0.f, Enemy.AttackCooldown - 1.f);

		if (Enemy.HP <= 0)
		{
			Enemy.bIsDead = true;
			Enemy.Velocity = FVector2D::ZeroVector;
			if (Enemy.TargetIndex >= 0 && Enemy.TargetIndex < Friendlies.Num())
			{
				Friendlies[Enemy.TargetIndex].ReleaseSlot(i, Enemy.TakenSlotIndex);
			}
			continue;
		}

		UpdateEnemyTarget(Enemy, i, Friendlies, FriendlyTowers);
		UpdateEnemyMovement(Sim, Enemy, i, Enemies, Friendlies, FriendlyTowers, Events);

		Enemy.Position += Enemy.Velocity;
		Enemy.UpdateRotation();
	}
}

// ============================================================================
// Targeting
// ============================================================================

void FEnemyBehavior::UpdateEnemyTarget(
	FUnit& Enemy,
	int32 EnemyIndex,
	TArray<FUnit>& LivingFriendlies,
	TArray<FTower>& FriendlyTowers)
{
	const int32 PreviousTargetIndex = Enemy.TargetIndex;
	Enemy.FramesSinceTargetEvaluation++;

	// Clear destroyed tower target
	if (Enemy.TargetTowerIndex >= 0 && Enemy.TargetTowerIndex < FriendlyTowers.Num())
	{
		if (FriendlyTowers[Enemy.TargetTowerIndex].IsDestroyed())
		{
			Enemy.TargetTowerIndex = -1;
		}
	}

	// Use TowerTargetingRules for selection
	int32 NewUnitTarget = -1;
	int32 NewTowerTarget = -1;
	TowerTargetingRules::SelectTarget(Enemy, LivingFriendlies, FriendlyTowers, NewUnitTarget, NewTowerTarget);

	// If tower target found, use it
	if (NewTowerTarget >= 0)
	{
		if (Enemy.TargetIndex >= 0 && Enemy.TargetIndex < LivingFriendlies.Num())
		{
			LivingFriendlies[Enemy.TargetIndex].ReleaseSlot(EnemyIndex, Enemy.TakenSlotIndex);
		}
		Enemy.TargetIndex = -1;
		Enemy.TakenSlotIndex = -1;
		Enemy.TargetTowerIndex = NewTowerTarget;
		Enemy.FramesSinceTargetEvaluation = 0;
		return;
	}

	if (Enemy.TargetTowerIndex >= 0)
	{
		Enemy.TargetTowerIndex = -1;
	}

	// Check if current target is invalid
	bool bNeedsTarget = Enemy.TargetIndex < 0;
	if (!bNeedsTarget && Enemy.TargetIndex < LivingFriendlies.Num())
	{
		const FUnit& Current = LivingFriendlies[Enemy.TargetIndex];
		if (Current.bIsDead || !Enemy.CanAttackUnit(Current))
		{
			bNeedsTarget = true;
		}
	}
	else if (Enemy.TargetIndex >= LivingFriendlies.Num())
	{
		bNeedsTarget = true;
	}

	if (bNeedsTarget)
	{
		if (Enemy.TargetIndex >= 0 && Enemy.TargetIndex < LivingFriendlies.Num())
		{
			LivingFriendlies[Enemy.TargetIndex].ReleaseSlot(EnemyIndex, Enemy.TakenSlotIndex);
		}
		Enemy.TargetIndex = SelectBestTarget(Enemy, LivingFriendlies);
		Enemy.FramesSinceTargetEvaluation = 0;
	}
	else
	{
		// Re-evaluate: see if there's a clearly better target
		const int32 BestIdx = SelectBestTarget(Enemy, LivingFriendlies);
		if (BestIdx >= 0 && BestIdx != Enemy.TargetIndex)
		{
			const float CurrentScore = (Enemy.TargetIndex >= 0 && Enemy.TargetIndex < LivingFriendlies.Num())
				? EvaluateTargetScore(Enemy, LivingFriendlies[Enemy.TargetIndex])
				: TNumericLimits<float>::Max();
			const float BestScore = EvaluateTargetScore(Enemy, LivingFriendlies[BestIdx]);
			const bool bIntervalElapsed = Enemy.FramesSinceTargetEvaluation >= UnitSimConstants::TARGET_REEVALUATE_INTERVAL_FRAMES;
			const bool bClearlyBetter = (BestScore + UnitSimConstants::TARGET_SWITCH_MARGIN) < CurrentScore;

			if (bIntervalElapsed || bClearlyBetter)
			{
				if (Enemy.TargetIndex >= 0 && Enemy.TargetIndex < LivingFriendlies.Num())
				{
					LivingFriendlies[Enemy.TargetIndex].ReleaseSlot(EnemyIndex, Enemy.TakenSlotIndex);
				}
				Enemy.TargetIndex = BestIdx;
				Enemy.FramesSinceTargetEvaluation = 0;
			}
		}
	}

	// Claim slot on new target
	if (Enemy.TargetIndex >= 0 && Enemy.TargetIndex != PreviousTargetIndex &&
		Enemy.TargetIndex < LivingFriendlies.Num())
	{
		Enemy.TakenSlotIndex = LivingFriendlies[Enemy.TargetIndex].ClaimBestSlot(
			EnemyIndex, Enemy.Position, Enemy.Radius);
		Enemy.FramesSinceSlotEvaluation = 0;
	}
	else if (Enemy.TargetIndex < 0)
	{
		Enemy.FramesSinceSlotEvaluation = 0;
	}
}

int32 FEnemyBehavior::SelectBestTarget(
	const FUnit& Enemy,
	const TArray<FUnit>& Candidates)
{
	int32 BestIdx = -1;
	float BestScore = TNumericLimits<float>::Max();

	for (int32 i = 0; i < Candidates.Num(); i++)
	{
		const FUnit& Candidate = Candidates[i];
		if (Candidate.bIsDead || !Enemy.CanAttackUnit(Candidate)) continue;

		const float Score = EvaluateTargetScore(Enemy, Candidate);
		if (Score < BestScore)
		{
			BestScore = Score;
			BestIdx = i;
		}
	}
	return BestIdx;
}

float FEnemyBehavior::EvaluateTargetScore(
	const FUnit& Enemy,
	const FUnit& Candidate)
{
	const float Distance = FVector2D::Distance(Enemy.Position, Candidate.Position);
	int32 Occupied = 0;
	for (int32 Slot : Candidate.AttackSlots)
	{
		if (Slot >= 0) Occupied++;
	}
	const float CrowdPenalty = Occupied * UnitSimConstants::TARGET_CROWD_PENALTY_PER_ATTACKER;
	return Distance + CrowdPenalty;
}

// ============================================================================
// Movement & Combat
// ============================================================================

void FEnemyBehavior::UpdateEnemyMovement(
	FSimulatorCore& Sim,
	FUnit& Enemy,
	int32 EnemyIndex,
	TArray<FUnit>& Enemies,
	TArray<FUnit>& LivingFriendlies,
	TArray<FTower>& FriendlyTowers,
	FFrameEvents& Events)
{
	// Tower combat
	if (Enemy.TargetTowerIndex >= 0 && Enemy.TargetTowerIndex < FriendlyTowers.Num())
	{
		UpdateTowerCombat(Sim, Enemy, EnemyIndex,
			FriendlyTowers[Enemy.TargetTowerIndex], Enemies, LivingFriendlies, Events);
		return;
	}

	// No unit target
	if (Enemy.TargetIndex < 0 || Enemy.TargetIndex >= LivingFriendlies.Num())
	{
		Enemy.ClearMovementPath();
		Enemy.CurrentDestination = Enemy.Position;
		Enemy.Velocity = FVector2D::ZeroVector;
		Enemy.ChargeState.Reset();
		return;
	}

	FUnit& Target = LivingFriendlies[Enemy.TargetIndex];

	// Update charge state
	FCombatSystem CombatSys;
	CombatSys.UpdateChargeState(Enemy, Enemy.TargetIndex, LivingFriendlies);

	// Slot refresh logic
	Enemy.FramesSinceSlotEvaluation++;
	int32 SlotIndex = Enemy.TakenSlotIndex;
	FVector2D TargetPosition;

	bool bNeedsSlotRefresh = (SlotIndex == -1);
	if (!bNeedsSlotRefresh)
	{
		const FVector2D DesiredSlotPos = Target.GetSlotPosition(SlotIndex, Enemy.Radius);
		const float SlotOffset = FVector2D::Distance(DesiredSlotPos, Enemy.Position);
		const bool bOffsetTooLarge = SlotOffset > UnitSimConstants::SLOT_REEVALUATE_DISTANCE;
		const bool bIntervalElapsed = Enemy.FramesSinceSlotEvaluation >= UnitSimConstants::SLOT_REEVALUATE_INTERVAL_FRAMES;
		bNeedsSlotRefresh = bOffsetTooLarge || bIntervalElapsed;
	}

	if (bNeedsSlotRefresh)
	{
		Enemy.TakenSlotIndex = Target.ClaimBestSlot(EnemyIndex, Enemy.Position, Enemy.Radius);
		Enemy.FramesSinceSlotEvaluation = 0;
		SlotIndex = Enemy.TakenSlotIndex;
	}

	if (SlotIndex != -1)
	{
		TargetPosition = Target.GetSlotPosition(SlotIndex, Enemy.Radius);
	}
	else
	{
		// Fallback: move perpendicular to target direction
		const FVector2D ToTarget = Target.Position - Enemy.Position;
		const FVector2D Perpendicular(-ToTarget.Y, ToTarget.X);
		TargetPosition = Target.Position + AvoidanceSystem::SafeNormalize(Perpendicular) * 200.0;
	}

	const double DistToCenter = FVector2D::Distance(Enemy.Position, Target.Position);
	if (DistToCenter <= Enemy.AttackRange)
	{
		Enemy.Velocity = FVector2D::ZeroVector;
		Enemy.ClearMovementPath();
		TryAttack(Enemy, EnemyIndex, Target, Enemy.TargetIndex, LivingFriendlies, Events);
	}
	else
	{
		MoveUnit(Sim, Enemy, EnemyIndex, TargetPosition, Enemies, LivingFriendlies);
	}
}

void FEnemyBehavior::UpdateTowerCombat(
	FSimulatorCore& Sim,
	FUnit& Enemy,
	int32 EnemyIndex,
	FTower& TargetTower,
	TArray<FUnit>& Enemies,
	TArray<FUnit>& LivingFriendlies,
	FFrameEvents& Events)
{
	const double DistToTarget = FVector2D::Distance(Enemy.Position, TargetTower.Position);
	if (DistToTarget <= Enemy.AttackRange)
	{
		Enemy.Velocity = FVector2D::ZeroVector;
		Enemy.ClearMovementPath();
		Enemy.ClearAvoidancePath();
		if (Enemy.AttackCooldown <= 0.f)
		{
			Events.AddDamageToTower(EnemyIndex, TargetTower.Id, Enemy.GetEffectiveDamage());
			Enemy.AttackCooldown = UnitSimConstants::ATTACK_COOLDOWN;
		}
	}
	else
	{
		MoveUnit(Sim, Enemy, EnemyIndex, TargetTower.Position, Enemies, LivingFriendlies);
	}
}

void FEnemyBehavior::TryAttack(
	FUnit& Attacker,
	int32 AttackerIndex,
	FUnit& Target,
	int32 TargetIndex,
	TArray<FUnit>& AllFriendlies,
	FFrameEvents& Events)
{
	if (Target.bIsDead) return;

	const double DistToTarget = FVector2D::Distance(Attacker.Position, Target.Position);
	if (DistToTarget <= Attacker.AttackRange)
	{
		Attacker.Velocity = FVector2D::ZeroVector;
		if (Attacker.AttackCooldown <= 0.f)
		{
			FCombatSystem CombatSys;
			CombatSys.CollectAttackEvents(Attacker, AttackerIndex, Target, TargetIndex,
				AllFriendlies, Events);
			Attacker.AttackCooldown = UnitSimConstants::ATTACK_COOLDOWN;
		}
	}
}

// ============================================================================
// Movement
// ============================================================================

void FEnemyBehavior::MoveUnit(
	FSimulatorCore& Sim,
	FUnit& Unit,
	int32 UnitIndex,
	const FVector2D& Destination,
	TArray<FUnit>& Allies,
	TArray<FUnit>& Opponents)
{
	const FVector2D AdjustedDest = Sim.GetTerrainSystem().GetAdjustedDestination(Unit, Destination);

	// Path replanning
	const bool bDestChanged = FVector2D::Distance(Unit.CurrentDestination, AdjustedDest) > UnitSimConstants::DESTINATION_THRESHOLD;
	const bool bShouldReplan = PathProgressMonitor::ShouldReplan(Unit, Sim.GetCurrentFrame());
	const bool bNeedsNewPath = bDestChanged || bShouldReplan;

	if (bNeedsNewPath)
	{
		TArray<FVector2D> Path;
		if (Sim.GetPathfinder() && Sim.GetPathfinder()->FindPath(Unit.Position, AdjustedDest, Path))
		{
			Unit.SetMovementPath(Path);
		}
		Unit.CurrentDestination = AdjustedDest;
		PathProgressMonitor::OnReplan(Unit, Sim.GetCurrentFrame());
	}

	FVector2D Waypoint;
	if (Unit.TryGetNextMovementWaypoint(Waypoint))
	{
		const FVector2D DesiredDirection = Waypoint - Unit.Position;
		const FVector2D DesiredForward = AvoidanceSystem::SafeNormalize(DesiredDirection);

		// Separation from allies
		FVector2D SeparationVector = FVector2D::ZeroVector;
		for (int32 j = 0; j < Allies.Num(); j++)
		{
			if (j == UnitIndex || Allies[j].bIsDead) continue;
			const FVector2D Delta = Unit.Position - Allies[j].Position;
			const double Dist = Delta.Size();
			if (Dist > KINDA_SMALL_NUMBER && Dist < UnitSimConstants::SEPARATION_RADIUS)
			{
				SeparationVector += Delta / (Dist * Dist);
			}
		}

		// Build avoidance candidates from both allies and opponents
		// For simplicity we pass the allies array to the avoidance system
		FVector2D AvoidTarget;
		bool bIsDetouring;
		int32 AvoidanceThreatIdx;
		const FVector2D Avoidance = AvoidanceSystem::PredictiveAvoidanceVector(
			Unit, UnitIndex, Allies.GetData(), Allies.Num(),
			DesiredForward, AvoidTarget, bIsDetouring, AvoidanceThreatIdx);

		FVector2D AvoidanceWaypoint;
		const bool bHasAvoidWP = Unit.TryGetNextAvoidanceWaypoint(AvoidanceWaypoint);
		const FVector2D SteeringTarget = bHasAvoidWP ? AvoidanceWaypoint : Waypoint;
		const bool bHasDetour = bHasAvoidWP || bIsDetouring;

		if (!bHasDetour)
		{
			Unit.ClearAvoidancePath();
		}

		Unit.bHasAvoidanceTarget = bHasDetour;
		Unit.AvoidanceTarget = bHasAvoidWP ? SteeringTarget : (bIsDetouring ? AvoidTarget : FVector2D::ZeroVector);
		Unit.AvoidanceThreatIndex = bHasDetour ? AvoidanceThreatIdx : -1;

		const FVector2D SteeringDir = AvoidanceSystem::SafeNormalize(SteeringTarget - Unit.Position);
		const FVector2D FinalDir = AvoidanceSystem::SafeNormalize(SteeringDir + SeparationVector + Avoidance);
		Unit.Velocity = FinalDir * Unit.GetEffectiveSpeed();

		const bool bMadeProgress = PathProgressMonitor::CheckProgress(Unit, Waypoint);
		PathProgressMonitor::UpdateProgress(Unit, bHasDetour, bMadeProgress);
	}
	else
	{
		Unit.Velocity = FVector2D::ZeroVector;
		PathProgressMonitor::UpdateProgress(Unit, false, true);
	}

	Unit.Position += Unit.Velocity;
	Unit.UpdateRotation();
}
