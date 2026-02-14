#include "Behaviors/SquadBehavior.h"
#include "Units/Unit.h"
#include "Towers/Tower.h"
#include "Combat/FrameEvents.h"
#include "Combat/CombatSystem.h"
#include "Combat/AvoidanceSystem.h"
#include "Targeting/TowerTargetingRules.h"
#include "Pathfinding/PathProgressMonitor.h"
#include "Simulation/SimulatorCore.h"
#include "Pathfinding/AStarPathfinder.h"

// ============================================================================
// Formation Offsets
// ============================================================================

const TArray<FVector2D>& FSquadBehavior::GetFormationOffsets()
{
	static TArray<FVector2D> Offsets = {
		FVector2D(0.0, 0.0),
		FVector2D(0.0, 90.0),
		FVector2D(-80.0, -45.0),
		FVector2D(-80.0, 135.0)
	};
	return Offsets;
}

// ============================================================================
// Main Update
// ============================================================================

void FSquadBehavior::UpdateFriendlySquad(
	FSimulatorCore& Sim,
	TArray<FUnit>& Friendlies,
	TArray<FUnit>& Enemies,
	TArray<FTower>& EnemyTowers,
	const FVector2D& MainTarget,
	FFrameEvents& Events)
{
	// Filter living enemies
	TArray<FUnit*> LivingEnemyPtrs;
	TArray<int32> LivingEnemyOrigIndices;
	for (int32 i = 0; i < Enemies.Num(); i++)
	{
		if (!Enemies[i].bIsDead)
		{
			LivingEnemyPtrs.Add(&Enemies[i]);
			LivingEnemyOrigIndices.Add(i);
		}
	}

	if (LivingEnemyPtrs.Num() > 0)
	{
		UpdateSquadTargetAndRallyPoint(Friendlies, Enemies);
		TSet<int32> EngagedIndices = DetermineEngagedUnits(Friendlies, Enemies);

		if (EngagedIndices.Num() > 0)
		{
			UpdateCombatBehavior(Sim, Friendlies, Enemies, EnemyTowers, EngagedIndices, Events);
		}

		if (EngagedIndices.Num() < Friendlies.Num())
		{
			UpdateFormation(Sim, Friendlies, &EngagedIndices);
		}
	}
	else
	{
		// No living enemies - check for towers
		TArray<int32> LivingTowerIndices;
		for (int32 i = 0; i < EnemyTowers.Num(); i++)
		{
			if (!EnemyTowers[i].IsDestroyed())
			{
				LivingTowerIndices.Add(i);
			}
		}

		if (LivingTowerIndices.Num() > 0)
		{
			UpdateTowerAssault(Sim, Friendlies, EnemyTowers, Events);
		}
		else
		{
			ResetSquadState(Friendlies);
			MoveToMainTarget(Sim, Friendlies, MainTarget);
		}
	}
}

// ============================================================================
// Squad Target & Rally
// ============================================================================

void FSquadBehavior::UpdateSquadTargetAndRallyPoint(
	TArray<FUnit>& Friendlies,
	TArray<FUnit>& LivingEnemies)
{
	// Validate current target
	if (SquadTargetIndex >= 0)
	{
		if (SquadTargetIndex >= LivingEnemies.Num() || LivingEnemies[SquadTargetIndex].bIsDead)
		{
			SquadTargetIndex = -1;
		}
	}

	if (SquadTargetIndex < 0 && Friendlies.Num() > 0)
	{
		const FUnit& Leader = Friendlies[0];

		float BestDist = TNumericLimits<float>::Max();
		int32 BestIdx = -1;

		for (int32 i = 0; i < LivingEnemies.Num(); i++)
		{
			if (LivingEnemies[i].bIsDead) continue;
			if (!Leader.CanAttackUnit(LivingEnemies[i])) continue;

			const float Dist = FVector2D::Distance(Leader.Position, LivingEnemies[i].Position);
			if (Dist < BestDist)
			{
				BestDist = Dist;
				BestIdx = i;
			}
		}

		if (BestIdx >= 0)
		{
			SquadTargetIndex = BestIdx;
			FVector2D DirToTarget = LivingEnemies[BestIdx].Position - Leader.Position;
			const double Len = DirToTarget.Size();
			if (Len > KINDA_SMALL_NUMBER)
			{
				DirToTarget /= Len;
			}
			RallyPoint = LivingEnemies[BestIdx].Position - DirToTarget * UnitSimConstants::RALLY_DISTANCE;
		}
	}
}

// ============================================================================
// Formation
// ============================================================================

void FSquadBehavior::UpdateFormation(
	FSimulatorCore& Sim,
	TArray<FUnit>& Friendlies,
	const TSet<int32>* EngagedIndices)
{
	if (Friendlies.Num() == 0) return;

	FUnit& Leader = Friendlies[0];
	const bool bLeaderEngaged = EngagedIndices && EngagedIndices->Contains(0);
	const FVector2D LeaderTargetPosition = bLeaderEngaged ? Leader.Position : RallyPoint;

	if (!bLeaderEngaged)
	{
		MoveUnit(Sim, Leader, 0, LeaderTargetPosition, Friendlies, nullptr);
	}

	const TArray<FVector2D>& Offsets = GetFormationOffsets();

	for (int32 i = 1; i < Friendlies.Num(); i++)
	{
		if (EngagedIndices && EngagedIndices->Contains(i)) continue;

		FUnit& Follower = Friendlies[i];

		// Rotate offset by leader's facing direction
		const float Angle = FMath::Atan2(Leader.Forward.Y, Leader.Forward.X);
		const float CosA = FMath::Cos(Angle);
		const float SinA = FMath::Sin(Angle);

		const int32 OffsetIdx = FMath::Min(i, Offsets.Num() - 1);
		const FVector2D& Offset = Offsets[OffsetIdx];
		const FVector2D RotatedOffset(
			Offset.X * CosA - Offset.Y * SinA,
			Offset.X * SinA + Offset.Y * CosA
		);

		const FVector2D FormationTarget = Leader.Position + RotatedOffset;
		MoveUnit(Sim, Follower, i, FormationTarget, Friendlies, nullptr);
	}
}

// ============================================================================
// Engagement Detection
// ============================================================================

TSet<int32> FSquadBehavior::DetermineEngagedUnits(
	TArray<FUnit>& Friendlies,
	TArray<FUnit>& LivingEnemies)
{
	TSet<int32> Engaged;
	for (int32 i = 0; i < Friendlies.Num(); i++)
	{
		if (IsUnitReadyToEngage(Friendlies[i], LivingEnemies))
		{
			Engaged.Add(i);
		}
	}
	return Engaged;
}

bool FSquadBehavior::IsUnitReadyToEngage(
	const FUnit& Friendly,
	const TArray<FUnit>& LivingEnemies)
{
	bool bAnyLiving = false;
	for (const FUnit& E : LivingEnemies)
	{
		if (!E.bIsDead) { bAnyLiving = true; break; }
	}
	if (!bAnyLiving) return false;

	// Already has a valid target
	if (Friendly.TargetIndex >= 0 && Friendly.TargetIndex < LivingEnemies.Num())
	{
		const FUnit& Target = LivingEnemies[Friendly.TargetIndex];
		if (!Target.bIsDead && Friendly.CanAttackUnit(Target)) return true;
	}

	const float TriggerDistance = Friendly.AttackRange * UnitSimConstants::ENGAGEMENT_TRIGGER_DISTANCE_MULTIPLIER;
	for (const FUnit& Enemy : LivingEnemies)
	{
		if (Enemy.bIsDead) continue;
		if (Friendly.CanAttackUnit(Enemy) &&
			FVector2D::Distance(Friendly.Position, Enemy.Position) <= TriggerDistance)
		{
			return true;
		}
	}
	return false;
}

// ============================================================================
// Combat
// ============================================================================

void FSquadBehavior::UpdateCombatBehavior(
	FSimulatorCore& Sim,
	TArray<FUnit>& Friendlies,
	TArray<FUnit>& LivingEnemies,
	TArray<FTower>& EnemyTowers,
	const TSet<int32>& EngagedIndices,
	FFrameEvents& Events)
{
	for (int32 i = 0; i < Friendlies.Num(); i++)
	{
		if (!EngagedIndices.Contains(i)) continue;

		FUnit& Friendly = Friendlies[i];
		UpdateUnitTarget(Friendly, i, LivingEnemies, EnemyTowers);
		UpdateCombat(Sim, Friendly, i, LivingEnemies, EnemyTowers, Friendlies, Events);
		Friendly.Position += Friendly.Velocity;
		Friendly.UpdateRotation();
	}
}

void FSquadBehavior::UpdateUnitTarget(
	FUnit& Friendly,
	int32 FriendlyIndex,
	TArray<FUnit>& LivingEnemies,
	TArray<FTower>& EnemyTowers)
{
	// Invalidate dead/unattackable target
	if (Friendly.TargetIndex >= 0 && Friendly.TargetIndex < LivingEnemies.Num())
	{
		FUnit& Target = LivingEnemies[Friendly.TargetIndex];
		if (Target.bIsDead || !Friendly.CanAttackUnit(Target))
		{
			Target.ReleaseSlot(FriendlyIndex, Friendly.TakenSlotIndex);
			Friendly.TargetIndex = -1;
			Friendly.TakenSlotIndex = -1;
		}
	}

	if (Friendly.TargetTowerIndex >= 0 && Friendly.TargetTowerIndex < EnemyTowers.Num())
	{
		if (EnemyTowers[Friendly.TargetTowerIndex].IsDestroyed())
		{
			Friendly.TargetTowerIndex = -1;
		}
	}

	Friendly.AttackCooldown = FMath::Max(0.f, Friendly.AttackCooldown - 1.f);
	const int32 PreviousTargetIndex = Friendly.TargetIndex;

	// Select new target
	int32 NewUnitTarget = -1;
	int32 NewTowerTarget = -1;
	TowerTargetingRules::SelectTarget(Friendly, LivingEnemies, EnemyTowers, NewUnitTarget, NewTowerTarget);

	Friendly.TargetIndex = NewUnitTarget;
	Friendly.TargetTowerIndex = NewTowerTarget;

	// Release previous target's slot if changed
	if (PreviousTargetIndex >= 0 && PreviousTargetIndex != Friendly.TargetIndex &&
		PreviousTargetIndex < LivingEnemies.Num())
	{
		LivingEnemies[PreviousTargetIndex].ReleaseSlot(FriendlyIndex, Friendly.TakenSlotIndex);
	}

	// Claim slot on new target
	if (Friendly.TargetIndex >= 0 && Friendly.TargetIndex < LivingEnemies.Num())
	{
		Friendly.TakenSlotIndex = LivingEnemies[Friendly.TargetIndex].ClaimBestSlot(
			FriendlyIndex, Friendly.Position, Friendly.Radius);
	}
	else if (PreviousTargetIndex >= 0)
	{
		Friendly.TakenSlotIndex = -1;
	}
}

void FSquadBehavior::UpdateCombat(
	FSimulatorCore& Sim,
	FUnit& Friendly,
	int32 FriendlyIndex,
	TArray<FUnit>& LivingEnemies,
	TArray<FTower>& EnemyTowers,
	TArray<FUnit>& Friendlies,
	FFrameEvents& Events)
{
	// Tower combat has priority if targeting a tower
	if (Friendly.TargetTowerIndex >= 0 && Friendly.TargetTowerIndex < EnemyTowers.Num())
	{
		UpdateTowerCombat(Sim, Friendly, FriendlyIndex,
			EnemyTowers[Friendly.TargetTowerIndex], Friendlies, Events);
		return;
	}

	if (Friendly.TargetIndex < 0 || Friendly.TargetIndex >= LivingEnemies.Num())
	{
		Friendly.ClearMovementPath();
		Friendly.CurrentDestination = Friendly.Position;
		Friendly.Velocity = FVector2D::ZeroVector;
		Friendly.ChargeState.Reset();
		return;
	}

	FUnit& Target = LivingEnemies[Friendly.TargetIndex];

	// Update charge state
	FCombatSystem CombatSys;
	CombatSys.UpdateChargeState(Friendly, Friendly.TargetIndex, LivingEnemies);

	const int32 SlotIndex = Friendly.TakenSlotIndex;
	const FVector2D AttackPosition = (SlotIndex != -1)
		? Target.GetSlotPosition(SlotIndex, Friendly.Radius)
		: Target.Position;

	const double DistToTargetCenter = FVector2D::Distance(Friendly.Position, Target.Position);
	const bool bInAttackRange = DistToTargetCenter <= Friendly.AttackRange;

	if (bInAttackRange)
	{
		Friendly.Velocity = FVector2D::ZeroVector;
		Friendly.ClearMovementPath();
		Friendly.ClearAvoidancePath();
		Friendly.CurrentDestination = Friendly.Position;
		if (Friendly.AttackCooldown <= 0.f)
		{
			CombatSys.CollectAttackEvents(Friendly, FriendlyIndex, Target, Friendly.TargetIndex,
				LivingEnemies, Events);
			Friendly.AttackCooldown = UnitSimConstants::ATTACK_COOLDOWN;
		}
	}
	else
	{
		MoveUnit(Sim, Friendly, FriendlyIndex, AttackPosition, Friendlies, &LivingEnemies);
	}
}

// ============================================================================
// Tower Assault
// ============================================================================

void FSquadBehavior::UpdateTowerAssault(
	FSimulatorCore& Sim,
	TArray<FUnit>& Friendlies,
	TArray<FTower>& EnemyTowers,
	FFrameEvents& Events)
{
	// Create empty enemies list for target selection
	TArray<FUnit> EmptyEnemies;

	for (int32 i = 0; i < Friendlies.Num(); i++)
	{
		FUnit& Friendly = Friendlies[i];
		if (Friendly.bIsDead) continue;

		UpdateUnitTarget(Friendly, i, EmptyEnemies, EnemyTowers);
		if (Friendly.TargetTowerIndex >= 0 && Friendly.TargetTowerIndex < EnemyTowers.Num())
		{
			UpdateTowerCombat(Sim, Friendly, i, EnemyTowers[Friendly.TargetTowerIndex],
				Friendlies, Events);
			Friendly.Position += Friendly.Velocity;
			Friendly.UpdateRotation();
		}
	}
}

void FSquadBehavior::UpdateTowerCombat(
	FSimulatorCore& Sim,
	FUnit& Unit,
	int32 UnitIndex,
	FTower& TargetTower,
	TArray<FUnit>& Friendlies,
	FFrameEvents& Events)
{
	const double DistToTarget = FVector2D::Distance(Unit.Position, TargetTower.Position);
	const bool bInAttackRange = DistToTarget <= Unit.AttackRange;

	if (bInAttackRange)
	{
		Unit.Velocity = FVector2D::ZeroVector;
		Unit.ClearMovementPath();
		Unit.ClearAvoidancePath();
		Unit.CurrentDestination = Unit.Position;
		if (Unit.AttackCooldown <= 0.f)
		{
			// Find tower index in the towers array
			Events.AddDamageToTower(UnitIndex, TargetTower.Id, Unit.GetEffectiveDamage());
			Unit.AttackCooldown = UnitSimConstants::ATTACK_COOLDOWN;
		}
	}
	else
	{
		MoveUnit(Sim, Unit, UnitIndex, TargetTower.Position, Friendlies, nullptr);
	}
}

// ============================================================================
// Movement
// ============================================================================

void FSquadBehavior::MoveUnit(
	FSimulatorCore& Sim,
	FUnit& Unit,
	int32 UnitIndex,
	const FVector2D& Destination,
	TArray<FUnit>& Allies,
	TArray<FUnit>* Opponents)
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
		FVector2D DesiredDirection = Waypoint - Unit.Position;
		FVector2D DesiredForward = AvoidanceSystem::SafeNormalize(DesiredDirection);

		// Separation from allies
		FVector2D SeparationVector = FVector2D::ZeroVector;
		for (int32 j = 0; j < Allies.Num(); j++)
		{
			if (j == UnitIndex || Allies[j].bIsDead) continue;
			const FVector2D Delta = Unit.Position - Allies[j].Position;
			const double Dist = Delta.Size();
			if (Dist > KINDA_SMALL_NUMBER && Dist < UnitSimConstants::FRIENDLY_SEPARATION_RADIUS)
			{
				SeparationVector += Delta / (Dist * Dist);
			}
		}

		// Avoidance
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

// ============================================================================
// Helpers
// ============================================================================

void FSquadBehavior::ResetSquadState(TArray<FUnit>& Friendlies)
{
	SquadTargetIndex = -1;

	for (int32 i = 0; i < Friendlies.Num(); i++)
	{
		FUnit& F = Friendlies[i];
		if (F.TargetIndex >= 0)
		{
			// Can't release slot on enemy without the array, but we clear our own state
			F.TakenSlotIndex = -1;
			F.TargetIndex = -1;
		}
		F.ClearMovementPath();
		F.ClearAvoidancePath();
		F.CurrentDestination = F.Position;
	}
}

void FSquadBehavior::MoveToMainTarget(
	FSimulatorCore& Sim,
	TArray<FUnit>& Friendlies,
	const FVector2D& MainTarget)
{
	if (Friendlies.Num() == 0) return;

	FUnit& Leader = Friendlies[0];
	MoveUnit(Sim, Leader, 0, MainTarget, Friendlies, nullptr);

	const TArray<FVector2D>& Offsets = GetFormationOffsets();

	for (int32 i = 1; i < Friendlies.Num(); i++)
	{
		FUnit& Follower = Friendlies[i];

		const float Angle = FMath::Atan2(Leader.Forward.Y, Leader.Forward.X);
		const float CosA = FMath::Cos(Angle);
		const float SinA = FMath::Sin(Angle);

		const int32 OffsetIdx = FMath::Min(i, Offsets.Num() - 1);
		const FVector2D& Offset = Offsets[OffsetIdx];
		const FVector2D RotatedOffset(
			Offset.X * CosA - Offset.Y * SinA,
			Offset.X * SinA + Offset.Y * CosA
		);

		const FVector2D FormationTarget = Leader.Position + RotatedOffset;
		MoveUnit(Sim, Follower, i, FormationTarget, Friendlies, nullptr);
	}
}
