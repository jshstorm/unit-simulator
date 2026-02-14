#include "Units/UnitRegistry.h"

void FUnitRegistry::Register(const FUnitDefinition& Definition)
{
	Definitions.Add(Definition.UnitId, Definition);
}

void FUnitRegistry::RegisterAll(const TArray<FUnitDefinition>& InDefinitions)
{
	for (const FUnitDefinition& Def : InDefinitions)
	{
		Register(Def);
	}
}

const FUnitDefinition* FUnitRegistry::GetDefinition(const FName& InUnitId) const
{
	return Definitions.Find(InUnitId);
}

bool FUnitRegistry::HasDefinition(const FName& InUnitId) const
{
	return Definitions.Contains(InUnitId);
}

TArray<FName> FUnitRegistry::GetRegisteredIds() const
{
	TArray<FName> Ids;
	Definitions.GetKeys(Ids);
	return Ids;
}

FUnitRegistry FUnitRegistry::CreateWithDefaults()
{
	FUnitRegistry Registry;
	Registry.RegisterAll(GetDefaultDefinitions());
	return Registry;
}

TArray<FUnitDefinition> FUnitRegistry::GetDefaultDefinitions()
{
	TArray<FUnitDefinition> Defs;

	// Golemite - spawned on Golem death
	{
		FUnitDefinition Def;
		Def.UnitId = FName(TEXT("golemite"));
		Def.DisplayName = TEXT("Golemite");
		Def.MaxHP = 900;
		Def.Damage = 50;
		Def.AttackRange = 30.f;
		Def.MoveSpeed = 3.0f;
		Def.TurnSpeed = 0.1f;
		Def.Radius = 25.f;
		Def.Role = EUnitRole::Melee;
		Def.Layer = EMovementLayer::Ground;
		Def.CanTarget = ETargetType::Ground;
		Def.bHasDeathDamage = true;
		Def.DeathDamageData.Damage = 100;
		Def.DeathDamageData.Radius = 40.f;
		Defs.Add(Def);
	}

	// Skeleton
	{
		FUnitDefinition Def;
		Def.UnitId = FName(TEXT("skeleton"));
		Def.DisplayName = TEXT("Skeleton");
		Def.MaxHP = 81;
		Def.Damage = 81;
		Def.AttackRange = 25.f;
		Def.MoveSpeed = 5.0f;
		Def.TurnSpeed = 0.12f;
		Def.Radius = 15.f;
		Def.Role = EUnitRole::Melee;
		Def.Layer = EMovementLayer::Ground;
		Def.CanTarget = ETargetType::Ground;
		Defs.Add(Def);
	}

	// Lava Pup - spawned on Lava Hound death
	{
		FUnitDefinition Def;
		Def.UnitId = FName(TEXT("lava_pup"));
		Def.DisplayName = TEXT("Lava Pup");
		Def.MaxHP = 209;
		Def.Damage = 55;
		Def.AttackRange = 60.f;
		Def.MoveSpeed = 4.5f;
		Def.TurnSpeed = 0.1f;
		Def.Radius = 15.f;
		Def.Role = EUnitRole::Ranged;
		Def.Layer = EMovementLayer::Air;
		Def.CanTarget = ETargetType::GroundAndAir;
		Defs.Add(Def);
	}

	// Minion
	{
		FUnitDefinition Def;
		Def.UnitId = FName(TEXT("minion"));
		Def.DisplayName = TEXT("Minion");
		Def.MaxHP = 252;
		Def.Damage = 84;
		Def.AttackRange = 60.f;
		Def.MoveSpeed = 5.0f;
		Def.TurnSpeed = 0.1f;
		Def.Radius = 18.f;
		Def.Role = EUnitRole::Ranged;
		Def.Layer = EMovementLayer::Air;
		Def.CanTarget = ETargetType::GroundAndAir;
		Defs.Add(Def);
	}

	// Bat
	{
		FUnitDefinition Def;
		Def.UnitId = FName(TEXT("bat"));
		Def.DisplayName = TEXT("Bat");
		Def.MaxHP = 81;
		Def.Damage = 81;
		Def.AttackRange = 25.f;
		Def.MoveSpeed = 5.5f;
		Def.TurnSpeed = 0.15f;
		Def.Radius = 12.f;
		Def.Role = EUnitRole::Melee;
		Def.Layer = EMovementLayer::Air;
		Def.CanTarget = ETargetType::GroundAndAir;
		Defs.Add(Def);
	}

	// Elixir Golemite
	{
		FUnitDefinition Def;
		Def.UnitId = FName(TEXT("elixir_golemite"));
		Def.DisplayName = TEXT("Elixir Golemite");
		Def.MaxHP = 560;
		Def.Damage = 42;
		Def.AttackRange = 30.f;
		Def.MoveSpeed = 3.5f;
		Def.TurnSpeed = 0.1f;
		Def.Radius = 22.f;
		Def.Role = EUnitRole::Melee;
		Def.Layer = EMovementLayer::Ground;
		Def.CanTarget = ETargetType::Ground;
		Def.bHasDeathSpawn = true;
		Def.DeathSpawnData.SpawnUnitId = FName(TEXT("elixir_blob"));
		Def.DeathSpawnData.SpawnCount = 2;
		Def.DeathSpawnData.SpawnRadius = 20.f;
		Defs.Add(Def);
	}

	// Elixir Blob
	{
		FUnitDefinition Def;
		Def.UnitId = FName(TEXT("elixir_blob"));
		Def.DisplayName = TEXT("Elixir Blob");
		Def.MaxHP = 280;
		Def.Damage = 21;
		Def.AttackRange = 25.f;
		Def.MoveSpeed = 3.5f;
		Def.TurnSpeed = 0.1f;
		Def.Radius = 18.f;
		Def.Role = EUnitRole::Melee;
		Def.Layer = EMovementLayer::Ground;
		Def.CanTarget = ETargetType::Ground;
		Defs.Add(Def);
	}

	// Guard - skeleton with shield
	{
		FUnitDefinition Def;
		Def.UnitId = FName(TEXT("guard"));
		Def.DisplayName = TEXT("Guard");
		Def.MaxHP = 90;
		Def.Damage = 90;
		Def.AttackRange = 30.f;
		Def.MoveSpeed = 4.5f;
		Def.TurnSpeed = 0.1f;
		Def.Radius = 18.f;
		Def.Role = EUnitRole::Melee;
		Def.Layer = EMovementLayer::Ground;
		Def.CanTarget = ETargetType::Ground;
		Def.bHasShield = true;
		Def.ShieldData.MaxShieldHP = 199;
		Defs.Add(Def);
	}

	return Defs;
}
