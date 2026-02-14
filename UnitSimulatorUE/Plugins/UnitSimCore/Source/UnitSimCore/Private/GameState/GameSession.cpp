#include "GameState/GameSession.h"
#include "GameState/InitialSetup.h"
#include "Towers/TowerStats.h"
#include "Terrain/MapLayout.h"

void FGameSession::InitializeTowers(const TArray<FTowerSetup>& TowerSetups)
{
	FriendlyTowers.Empty();
	EnemyTowers.Empty();

	int32 TowerId = 1;
	for (const FTowerSetup& Setup : TowerSetups)
	{
		FTower Tower = CreateTowerFromSetup(TowerId++, Setup);
		if (Setup.Faction == EUnitFaction::Friendly)
		{
			FriendlyTowers.Add(Tower);
		}
		else
		{
			EnemyTowers.Add(Tower);
		}
	}

	// Reset session state
	ElapsedTime = 0.f;
	FriendlyCrowns = 0;
	EnemyCrowns = 0;
	Result = EGameResult::InProgress;
	WinConditionType = EWinCondition::None;
	bIsOvertime = false;
}

void FGameSession::InitializeDefaultTowers()
{
	TArray<FTowerSetup> Defaults = TowerSetupDefaults::ClashRoyaleStandard();
	InitializeTowers(Defaults);
}

FTower FGameSession::CreateTowerFromSetup(int32 Id, const FTowerSetup& Setup)
{
	const FVector2D Position = Setup.bHasPosition
		? Setup.Position
		: GetDefaultTowerPosition(Setup.Type, Setup.Faction);

	FTower Tower = Setup.Type == ETowerType::King
		? FTower::CreateKingTower(Id, Setup.Faction, Position)
		: FTower::CreatePrincessTower(Id, Setup.Faction, Position);

	// Optional overrides
	if (Setup.InitialHP >= 0)
	{
		Tower.CurrentHP = Setup.InitialHP;
	}

	if (Setup.IsActivated >= 0)
	{
		Tower.bIsActivated = Setup.IsActivated > 0;
	}

	return Tower;
}

FVector2D FGameSession::GetDefaultTowerPosition(ETowerType Type, EUnitFaction Faction)
{
	if (Type == ETowerType::King)
	{
		return Faction == EUnitFaction::Friendly
			? MapLayout::FriendlyKingPosition()
			: MapLayout::EnemyKingPosition();
	}

	// Princess - default to left position
	return Faction == EUnitFaction::Friendly
		? MapLayout::FriendlyPrincessLeftPosition()
		: MapLayout::EnemyPrincessLeftPosition();
}

TArray<FTower>& FGameSession::GetTowers(EUnitFaction Faction)
{
	return Faction == EUnitFaction::Friendly ? FriendlyTowers : EnemyTowers;
}

const TArray<FTower>& FGameSession::GetTowers(EUnitFaction Faction) const
{
	return Faction == EUnitFaction::Friendly ? FriendlyTowers : EnemyTowers;
}

int32 FGameSession::GetKingTowerIndex(EUnitFaction Faction) const
{
	const TArray<FTower>& Towers = GetTowers(Faction);
	for (int32 i = 0; i < Towers.Num(); ++i)
	{
		if (Towers[i].Type == ETowerType::King)
		{
			return i;
		}
	}
	return -1;
}

FTower* FGameSession::GetKingTower(EUnitFaction Faction)
{
	const int32 Index = GetKingTowerIndex(Faction);
	if (Index >= 0)
	{
		return &GetTowers(Faction)[Index];
	}
	return nullptr;
}

const FTower* FGameSession::GetKingTower(EUnitFaction Faction) const
{
	const int32 Index = GetKingTowerIndex(Faction);
	if (Index >= 0)
	{
		return &GetTowers(Faction)[Index];
	}
	return nullptr;
}

void FGameSession::GetAllTowers(TArray<FTower*>& OutTowers)
{
	OutTowers.Empty();
	for (FTower& Tower : FriendlyTowers)
	{
		OutTowers.Add(&Tower);
	}
	for (FTower& Tower : EnemyTowers)
	{
		OutTowers.Add(&Tower);
	}
}

void FGameSession::UpdateCrowns()
{
	FriendlyCrowns = CountCrownsFromDestroyedTowers(EUnitFaction::Enemy);
	EnemyCrowns = CountCrownsFromDestroyedTowers(EUnitFaction::Friendly);
}

int32 FGameSession::CountCrownsFromDestroyedTowers(EUnitFaction DestroyedFaction) const
{
	int32 Crowns = 0;
	const TArray<FTower>& Towers = GetTowers(DestroyedFaction);
	for (const FTower& Tower : Towers)
	{
		if (Tower.IsDestroyed())
		{
			Crowns += Tower.Type == ETowerType::King ? 3 : 1;
		}
	}
	return FMath::Min(Crowns, 3);
}

void FGameSession::UpdateKingTowerActivation()
{
	UpdateKingActivationForFaction(EUnitFaction::Friendly);
	UpdateKingActivationForFaction(EUnitFaction::Enemy);
}

void FGameSession::UpdateKingActivationForFaction(EUnitFaction Faction)
{
	FTower* King = GetKingTower(Faction);
	if (King == nullptr || King->bIsActivated) return;

	// Check if any princess tower is destroyed
	const TArray<FTower>& Towers = GetTowers(Faction);
	for (const FTower& Tower : Towers)
	{
		if (Tower.Type == ETowerType::Princess && Tower.IsDestroyed())
		{
			King->bIsActivated = true;
			return;
		}
	}
}

float FGameSession::GetTotalTowerHPRatio(EUnitFaction Faction) const
{
	const TArray<FTower>& Towers = GetTowers(Faction);
	if (Towers.Num() == 0) return 0.f;

	float CurrentHP = 0.f;
	float MaxHP = 0.f;
	for (const FTower& Tower : Towers)
	{
		CurrentHP += Tower.CurrentHP;
		MaxHP += Tower.MaxHP;
	}

	return MaxHP > 0.f ? CurrentHP / MaxHP : 0.f;
}
