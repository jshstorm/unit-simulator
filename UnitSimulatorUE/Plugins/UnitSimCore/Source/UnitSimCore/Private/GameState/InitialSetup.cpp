#include "GameState/InitialSetup.h"

FInitialSetup FInitialSetup::CreateClashRoyaleStandard()
{
	FInitialSetup Setup;
	Setup.Towers = TowerSetupDefaults::ClashRoyaleStandard();
	Setup.GameTime = FGameTimeSetup();
	Setup.bHasGameTime = true;
	return Setup;
}

TArray<FTowerSetup> TowerSetupDefaults::ClashRoyaleStandard()
{
	TArray<FTowerSetup> Towers;

	// Friendly King
	{
		FTowerSetup T;
		T.Type = ETowerType::King;
		T.Faction = EUnitFaction::Friendly;
		Towers.Add(T);
	}

	// Friendly Princess Left
	{
		FTowerSetup T;
		T.Type = ETowerType::Princess;
		T.Faction = EUnitFaction::Friendly;
		T.Position = MapLayout::FriendlyPrincessLeftPosition();
		T.bHasPosition = true;
		Towers.Add(T);
	}

	// Friendly Princess Right
	{
		FTowerSetup T;
		T.Type = ETowerType::Princess;
		T.Faction = EUnitFaction::Friendly;
		T.Position = MapLayout::FriendlyPrincessRightPosition();
		T.bHasPosition = true;
		Towers.Add(T);
	}

	// Enemy King
	{
		FTowerSetup T;
		T.Type = ETowerType::King;
		T.Faction = EUnitFaction::Enemy;
		Towers.Add(T);
	}

	// Enemy Princess Left
	{
		FTowerSetup T;
		T.Type = ETowerType::Princess;
		T.Faction = EUnitFaction::Enemy;
		T.Position = MapLayout::EnemyPrincessLeftPosition();
		T.bHasPosition = true;
		Towers.Add(T);
	}

	// Enemy Princess Right
	{
		FTowerSetup T;
		T.Type = ETowerType::Princess;
		T.Faction = EUnitFaction::Enemy;
		T.Position = MapLayout::EnemyPrincessRightPosition();
		T.bHasPosition = true;
		Towers.Add(T);
	}

	return Towers;
}
