#include "Towers/Tower.h"
#include "Units/Unit.h"

void FTower::TakeDamage(int32 Amount)
{
	if (IsDestroyed()) return;

	CurrentHP -= Amount;
	if (CurrentHP < 0) CurrentHP = 0;

	// King Tower activates when damaged
	if (Type == ETowerType::King && !bIsActivated)
	{
		bIsActivated = true;
	}
}

bool FTower::CanAttackUnit(const FUnit& InTarget) const
{
	if (InTarget.bIsDead) return false;
	if (IsDestroyed()) return false;
	if (Type == ETowerType::King && !bIsActivated) return false;

	// Layer check
	const ETargetType TargetLayer = (InTarget.Layer == EMovementLayer::Air)
		? ETargetType::Air
		: ETargetType::Ground;

	if ((CanTarget & TargetLayer) == ETargetType::None) return false;

	// Range check
	const float Distance = FVector2D::Distance(Position, InTarget.Position);
	return Distance <= AttackRange;
}

void FTower::OnAttackPerformed()
{
	AttackCooldown = 1.f / AttackSpeed;
}

void FTower::UpdateCooldown(float DeltaTime)
{
	if (AttackCooldown > 0.f)
	{
		AttackCooldown -= DeltaTime;
	}
}

FTower FTower::CreatePrincessTower(int32 InId, EUnitFaction InFaction, const FVector2D& InPosition)
{
	FTower Tower;
	Tower.Id = InId;
	Tower.Type = ETowerType::Princess;
	Tower.Faction = InFaction;
	Tower.Position = InPosition;
	Tower.Radius = TowerStatsData::PrincessRadius;
	Tower.AttackRange = TowerStatsData::PrincessAttackRange;
	Tower.MaxHP = TowerStatsData::PrincessMaxHP;
	Tower.CurrentHP = TowerStatsData::PrincessMaxHP;
	Tower.Damage = TowerStatsData::PrincessDamage;
	Tower.AttackSpeed = TowerStatsData::PrincessAttackSpeed;
	Tower.CanTarget = ETargetType::GroundAndAir;
	Tower.bIsActivated = true;
	Tower.AttackCooldown = 0.f;
	return Tower;
}

FTower FTower::CreatePrincessTower(int32 InId, EUnitFaction InFaction, const FVector2D& InPosition, int32 InHP)
{
	FTower Tower = CreatePrincessTower(InId, InFaction, InPosition);
	Tower.CurrentHP = InHP;
	return Tower;
}

FTower FTower::CreateKingTower(int32 InId, EUnitFaction InFaction, const FVector2D& InPosition)
{
	FTower Tower;
	Tower.Id = InId;
	Tower.Type = ETowerType::King;
	Tower.Faction = InFaction;
	Tower.Position = InPosition;
	Tower.Radius = TowerStatsData::KingRadius;
	Tower.AttackRange = TowerStatsData::KingAttackRange;
	Tower.MaxHP = TowerStatsData::KingMaxHP;
	Tower.CurrentHP = TowerStatsData::KingMaxHP;
	Tower.Damage = TowerStatsData::KingDamage;
	Tower.AttackSpeed = TowerStatsData::KingAttackSpeed;
	Tower.CanTarget = ETargetType::GroundAndAir;
	Tower.bIsActivated = false; // King activates conditionally
	Tower.AttackCooldown = 0.f;
	return Tower;
}

FTower FTower::CreateKingTower(int32 InId, EUnitFaction InFaction, const FVector2D& InPosition, int32 InHP)
{
	FTower Tower = CreateKingTower(InId, InFaction, InPosition);
	Tower.CurrentHP = InHP;
	return Tower;
}
