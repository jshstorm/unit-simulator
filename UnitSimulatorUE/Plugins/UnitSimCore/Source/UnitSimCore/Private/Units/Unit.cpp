#include "Units/Unit.h"

void FUnit::Initialize(int32 InId, const FName& InUnitId, EUnitFaction InFaction,
	const FVector2D& InPosition, float InRadius, float InSpeed, float InTurnSpeed,
	EUnitRole InRole, int32 InHP, int32 InDamage,
	EMovementLayer InLayer, ETargetType InCanTarget, ETargetPriority InTargetPriority)
{
	Id = InId;
	UnitId = InUnitId;
	Faction = InFaction;
	Position = InPosition;
	CurrentDestination = InPosition;
	Radius = InRadius;
	Speed = InSpeed;
	TurnSpeed = InTurnSpeed;
	Role = InRole;
	HP = InHP;
	Damage = InDamage;
	Layer = InLayer;
	CanTarget = InCanTarget;
	TargetPriority = InTargetPriority;

	AttackRange = (InRole == EUnitRole::Melee)
		? InRadius * UnitSimConstants::MELEE_RANGE_MULTIPLIER
		: InRadius * UnitSimConstants::RANGED_RANGE_MULTIPLIER;

	AttackCooldown = 0.f;
	bIsDead = false;
	Velocity = FVector2D::ZeroVector;
	Forward = FVector2D(1.0, 0.0);
	TargetIndex = -1;
	TargetTowerIndex = -1;
}

FString FUnit::GetLabel() const
{
	return FString::Printf(TEXT("%s%d"),
		Faction == EUnitFaction::Friendly ? TEXT("F") : TEXT("E"), Id);
}

bool FUnit::HasAbility(EAbilityType Type) const
{
	for (const FAbilityData& Ability : Abilities)
	{
		if (Ability.Type == Type) return true;
	}
	return false;
}

bool FUnit::CanAttackUnit(const FUnit& InTarget) const
{
	if (InTarget.bIsDead) return false;

	ETargetType TargetLayer = (InTarget.Layer == EMovementLayer::Air)
		? ETargetType::Air
		: ETargetType::Ground;
	return (CanTarget & TargetLayer) != ETargetType::None;
}

bool FUnit::IsSameLayer(const FUnit& Other) const
{
	return Layer == Other.Layer;
}

FVector2D FUnit::GetSlotPosition(int32 SlotIndex, float AttackerRadius) const
{
	const float Angle = (2.f * UE_PI / UnitSimConstants::NUM_ATTACK_SLOTS) * SlotIndex;
	const float Distance = Radius + AttackerRadius + 10.f;
	return Position + FVector2D(FMath::Cos(Angle), FMath::Sin(Angle)) * Distance;
}

int32 FUnit::TryClaimSlot(int32 AttackerIndex)
{
	for (int32 i = 0; i < UnitSimConstants::NUM_ATTACK_SLOTS; ++i)
	{
		if (AttackSlots[i] == -1)
		{
			AttackSlots[i] = AttackerIndex;
			return i;
		}
	}
	return -1;
}

int32 FUnit::ClaimBestSlot(int32 AttackerIndex, const FVector2D& AttackerPosition, float AttackerRadius)
{
	int32 BestIndex = -1;
	float BestDistance = TNumericLimits<float>::Max();

	for (int32 i = 0; i < UnitSimConstants::NUM_ATTACK_SLOTS; ++i)
	{
		const int32 Occupant = AttackSlots[i];
		if (Occupant != -1 && Occupant != AttackerIndex) continue;

		const float Dist = FVector2D::Distance(AttackerPosition, GetSlotPosition(i, AttackerRadius));
		if (Dist < BestDistance)
		{
			BestDistance = Dist;
			BestIndex = i;
		}
	}

	if (BestIndex != -1)
	{
		// Release old slot if different
		if (TakenSlotIndex != -1 && TakenSlotIndex != BestIndex &&
			TakenSlotIndex < AttackSlots.Num() && AttackSlots[TakenSlotIndex] == AttackerIndex)
		{
			AttackSlots[TakenSlotIndex] = -1;
		}
		AttackSlots[BestIndex] = AttackerIndex;
	}

	return BestIndex;
}

void FUnit::ReleaseSlot(int32 AttackerIndex, int32 SlotIdx)
{
	if (SlotIdx >= 0 && SlotIdx < AttackSlots.Num())
	{
		if (AttackSlots[SlotIdx] == AttackerIndex)
		{
			AttackSlots[SlotIdx] = -1;
		}
	}
}

void FUnit::SetAvoidancePath(const TArray<FVector2D>& Waypoints)
{
	AvoidancePath = Waypoints;
	AvoidancePathIndex = 0;
}

bool FUnit::TryGetNextAvoidanceWaypoint(FVector2D& OutWaypoint) const
{
	int32 Index = AvoidancePathIndex;
	while (Index < AvoidancePath.Num())
	{
		const FVector2D& Target = AvoidancePath[Index];
		if (FVector2D::Distance(Position, Target) <= UnitSimConstants::AVOIDANCE_WAYPOINT_THRESHOLD)
		{
			++Index;
			continue;
		}
		OutWaypoint = Target;
		return true;
	}
	OutWaypoint = FVector2D::ZeroVector;
	return false;
}

void FUnit::ClearAvoidancePath()
{
	AvoidancePath.Empty();
	AvoidancePathIndex = 0;
}

void FUnit::SetMovementPath(const TArray<FVector2D>& Path)
{
	MovementPath = Path;
	MovementPathIndex = 0;
}

bool FUnit::TryGetNextMovementWaypoint(FVector2D& OutWaypoint)
{
	if (MovementPathIndex < MovementPath.Num())
	{
		const FVector2D& Target = MovementPath[MovementPathIndex];
		if (FVector2D::Distance(Position, Target) <= UnitSimConstants::AVOIDANCE_WAYPOINT_THRESHOLD)
		{
			++MovementPathIndex;
			if (MovementPathIndex >= MovementPath.Num())
			{
				OutWaypoint = FVector2D::ZeroVector;
				return false;
			}
		}
		OutWaypoint = MovementPath[MovementPathIndex];
		return true;
	}
	OutWaypoint = FVector2D::ZeroVector;
	return false;
}

void FUnit::ClearMovementPath()
{
	MovementPath.Empty();
	MovementPathIndex = 0;
}

void FUnit::UpdateRotation()
{
	if (Velocity.SizeSquared() < 0.001)
	{
		return;
	}

	const float TargetAngle = FMath::Atan2(Velocity.Y, Velocity.X);
	const float CurrentAngle = FMath::Atan2(Forward.Y, Forward.X);
	float AngleDiff = TargetAngle - CurrentAngle;

	// Normalize to [-PI, PI]
	while (AngleDiff > UE_PI) AngleDiff -= 2.f * UE_PI;
	while (AngleDiff < -UE_PI) AngleDiff += 2.f * UE_PI;

	const float Rotation = FMath::Clamp(AngleDiff, -TurnSpeed, TurnSpeed);
	const float NewAngle = CurrentAngle + Rotation;
	Forward = FVector2D(FMath::Cos(NewAngle), FMath::Sin(NewAngle));
}

int32 FUnit::TakeDamage(int32 InDamage)
{
	int32 RemainingDamage = InDamage;
	int32 HpDamage = 0;

	// Shield absorbs first
	if (ShieldHP > 0)
	{
		const int32 ShieldDamage = FMath::Min(ShieldHP, RemainingDamage);
		ShieldHP -= ShieldDamage;
		RemainingDamage -= ShieldDamage;
	}

	// Apply remaining to HP
	if (RemainingDamage > 0)
	{
		HpDamage = FMath::Min(HP, RemainingDamage);
		HP = FMath::Max(0, HP - RemainingDamage);
	}

	if (HP <= 0 && !bIsDead)
	{
		bIsDead = true;
		Velocity = FVector2D::ZeroVector;
	}

	return HpDamage;
}

float FUnit::GetEffectiveSpeed() const
{
	if (bHasChargeAbility && ChargeState.bIsCharging)
	{
		return Speed * ChargeAttackAbility.SpeedMultiplier;
	}
	return Speed;
}

int32 FUnit::GetEffectiveDamage() const
{
	if (bHasChargeAbility && ChargeState.bIsCharged)
	{
		return static_cast<int32>(Damage * ChargeAttackAbility.DamageMultiplier);
	}
	return Damage;
}

void FUnit::OnAttackPerformed()
{
	if (bHasChargeAbility)
	{
		ChargeState.ConsumeCharge();
	}
}
