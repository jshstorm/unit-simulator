#include "Misc/AutomationTest.h"
#include "Units/UnitStats.h"
#include "Simulation/GameBalance.h"
#include "GameState/WaveDefinition.h"
#include "GameConstants.h"
#include "Abilities/AbilityTypes.h"

// ============================================================================
// FUnitStats Default Values
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitStatsDefaultValues,
	"UnitSimCore.BasicTypes.UnitStats.DefaultValues",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitStatsDefaultValues::RunTest(const FString& Parameters)
{
	// Arrange & Act
	FUnitStats Stats = FUnitStats::Default();

	// Assert
	TestEqual(TEXT("DisplayName default"), Stats.DisplayName, TEXT("Unknown"));
	TestEqual(TEXT("HP default"), Stats.HP, 100);
	TestEqual(TEXT("Damage default"), Stats.Damage, 10);
	TestEqual(TEXT("MoveSpeed default"), Stats.MoveSpeed, 4.0f);
	TestEqual(TEXT("TurnSpeed default"), Stats.TurnSpeed, 0.1f);
	TestEqual(TEXT("AttackRange default"), Stats.AttackRange, 30.f);
	TestEqual(TEXT("Radius default"), Stats.Radius, 20.f);
	TestEqual(TEXT("AttackSpeed default"), Stats.AttackSpeed, 1.0f);
	TestEqual(TEXT("Role default"), Stats.Role, EUnitRole::Melee);
	TestEqual(TEXT("Layer default"), Stats.Layer, EMovementLayer::Ground);
	TestEqual(TEXT("CanTarget default"), Stats.CanTarget, ETargetType::Ground);
	TestEqual(TEXT("TargetPriority default"), Stats.TargetPriority, ETargetPriority::Nearest);
	TestEqual(TEXT("AttackType default"), Stats.AttackType, EAttackType::Melee);
	TestEqual(TEXT("ShieldHP default"), Stats.ShieldHP, 0);
	TestEqual(TEXT("SpawnCount default"), Stats.SpawnCount, 1);
	TestEqual(TEXT("Skills default empty"), Stats.Skills.Num(), 0);

	return true;
}

// ============================================================================
// FGameBalance Default Values
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FGameBalanceDefaultValues,
	"UnitSimCore.BasicTypes.GameBalance.DefaultValues",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FGameBalanceDefaultValues::RunTest(const FString& Parameters)
{
	// Arrange & Act
	FGameBalance Balance = FGameBalance::Default();

	// Assert
	TestEqual(TEXT("Version"), Balance.Version, 1);
	TestEqual(TEXT("SimulationWidth"), Balance.SimulationWidth, 3200);
	TestEqual(TEXT("SimulationHeight"), Balance.SimulationHeight, 5100);
	TestEqual(TEXT("MaxFrames"), Balance.MaxFrames, 3000);
	TestTrue(TEXT("FrameTimeSeconds approx 1/30"),
		FMath::IsNearlyEqual(Balance.FrameTimeSeconds, 1.f / 30.f));
	TestEqual(TEXT("UnitRadius"), Balance.UnitRadius, 20.f);
	TestEqual(TEXT("NumAttackSlots"), Balance.NumAttackSlots, 8);
	TestEqual(TEXT("AttackCooldown"), Balance.AttackCooldown, 30.f);
	TestEqual(TEXT("MaxWaves"), Balance.MaxWaves, 3);
	TestEqual(TEXT("CollisionResolutionIterations"), Balance.CollisionResolutionIterations, 3);

	return true;
}

// ============================================================================
// FWaveDefinition Empty
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FWaveDefinitionEmpty,
	"UnitSimCore.BasicTypes.WaveDefinition.Empty",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FWaveDefinitionEmpty::RunTest(const FString& Parameters)
{
	// Arrange & Act
	FWaveDefinition Wave = FWaveDefinition::Empty(3);

	// Assert
	TestEqual(TEXT("WaveNumber"), Wave.WaveNumber, 3);
	TestEqual(TEXT("Name"), Wave.Name, TEXT("Wave 3"));
	TestEqual(TEXT("DelayFrames"), Wave.DelayFrames, 0);
	TestEqual(TEXT("SpawnGroups empty"), Wave.SpawnGroups.Num(), 0);

	return true;
}

// ============================================================================
// FWaveSpawnGroup Defaults
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FWaveSpawnGroupDefaults,
	"UnitSimCore.BasicTypes.WaveSpawnGroup.DefaultValues",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FWaveSpawnGroupDefaults::RunTest(const FString& Parameters)
{
	// Arrange & Act
	FWaveSpawnGroup Group;

	// Assert
	TestEqual(TEXT("Count default"), Group.Count, 1);
	TestEqual(TEXT("Faction default"), Group.Faction, TEXT("enemy"));
	TestEqual(TEXT("SpawnFrame default"), Group.SpawnFrame, 0);
	TestEqual(TEXT("SpawnInterval default"), Group.SpawnInterval, 30);
	TestFalse(TEXT("HasSpawnX default"), Group.HasSpawnX());
	TestFalse(TEXT("HasSpawnY default"), Group.HasSpawnY());

	return true;
}

// ============================================================================
// UENUM Value Range
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FEnumValueRange,
	"UnitSimCore.BasicTypes.Enums.ValueRange",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FEnumValueRange::RunTest(const FString& Parameters)
{
	// EUnitRole
	TestEqual(TEXT("UnitRole::Melee"), static_cast<uint8>(EUnitRole::Melee), 0);
	TestEqual(TEXT("UnitRole::Siege"), static_cast<uint8>(EUnitRole::Siege), 8);

	// EMovementLayer
	TestEqual(TEXT("MovementLayer::Ground"), static_cast<uint8>(EMovementLayer::Ground), 0);
	TestEqual(TEXT("MovementLayer::Air"), static_cast<uint8>(EMovementLayer::Air), 1);

	// ETargetType bitmask
	TestEqual(TEXT("TargetType::Ground"), static_cast<uint8>(ETargetType::Ground), 1);
	TestEqual(TEXT("TargetType::Air"), static_cast<uint8>(ETargetType::Air), 2);
	TestEqual(TEXT("TargetType::Building"), static_cast<uint8>(ETargetType::Building), 4);
	TestEqual(TEXT("TargetType::GroundAndAir"), static_cast<uint8>(ETargetType::GroundAndAir), 3);

	// EGameResult
	TestEqual(TEXT("GameResult::InProgress"), static_cast<uint8>(EGameResult::InProgress), 0);
	TestEqual(TEXT("GameResult::Draw"), static_cast<uint8>(EGameResult::Draw), 3);

	// EAbilityType
	TestEqual(TEXT("AbilityType::ChargeAttack"), static_cast<uint8>(EAbilityType::ChargeAttack), 0);
	TestEqual(TEXT("AbilityType::StatusEffect"), static_cast<uint8>(EAbilityType::StatusEffect), 7);

	// ETowerType
	TestEqual(TEXT("TowerType::Princess"), static_cast<uint8>(ETowerType::Princess), 0);
	TestEqual(TEXT("TowerType::King"), static_cast<uint8>(ETowerType::King), 1);

	return true;
}

// ============================================================================
// FAbilityData Types
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FAbilityDataTypes,
	"UnitSimCore.BasicTypes.AbilityData.TypeDefaults",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FAbilityDataTypes::RunTest(const FString& Parameters)
{
	// ChargeAttackData
	{
		FChargeAttackData Data;
		TestEqual(TEXT("ChargeAttack Type"), Data.Type, EAbilityType::ChargeAttack);
		TestEqual(TEXT("TriggerDistance"), Data.TriggerDistance, 150.f);
		TestEqual(TEXT("RequiredChargeDistance"), Data.RequiredChargeDistance, 100.f);
		TestEqual(TEXT("DamageMultiplier"), Data.DamageMultiplier, 2.0f);
		TestEqual(TEXT("SpeedMultiplier"), Data.SpeedMultiplier, 2.0f);
	}

	// SplashDamageData
	{
		FSplashDamageData Data;
		TestEqual(TEXT("SplashDamage Type"), Data.Type, EAbilityType::SplashDamage);
		TestEqual(TEXT("Radius"), Data.Radius, 60.f);
		TestEqual(TEXT("DamageFalloff"), Data.DamageFalloff, 0.f);
	}

	// ShieldData
	{
		FShieldData Data;
		TestEqual(TEXT("Shield Type"), Data.Type, EAbilityType::Shield);
		TestEqual(TEXT("MaxShieldHP"), Data.MaxShieldHP, 200);
		TestFalse(TEXT("BlocksStun"), Data.BlocksStun);
		TestFalse(TEXT("BlocksKnockback"), Data.BlocksKnockback);
	}

	// DeathSpawnData
	{
		FDeathSpawnData Data;
		TestEqual(TEXT("DeathSpawn Type"), Data.Type, EAbilityType::DeathSpawn);
		TestEqual(TEXT("SpawnCount"), Data.SpawnCount, 2);
		TestEqual(TEXT("SpawnRadius"), Data.SpawnRadius, 30.f);
		TestEqual(TEXT("SpawnUnitHP"), Data.SpawnUnitHP, 0);
	}

	// DeathDamageData
	{
		FDeathDamageData Data;
		TestEqual(TEXT("DeathDamage Type"), Data.Type, EAbilityType::DeathDamage);
		TestEqual(TEXT("Damage"), Data.Damage, 100);
		TestEqual(TEXT("Radius"), Data.Radius, 60.f);
		TestEqual(TEXT("KnockbackDistance"), Data.KnockbackDistance, 0.f);
	}

	// StatusEffectAbilityData
	{
		FStatusEffectAbilityData Data;
		TestEqual(TEXT("StatusEffect Type"), Data.Type, EAbilityType::StatusEffect);
		TestEqual(TEXT("AppliedEffect"), Data.AppliedEffect, 0);
		TestEqual(TEXT("EffectDuration"), Data.EffectDuration, 0.f);
		TestEqual(TEXT("EffectMagnitude"), Data.EffectMagnitude, 1.0f);
		TestEqual(TEXT("EffectRange"), Data.EffectRange, 0.f);
		TestEqual(TEXT("AffectedTargets"), Data.AffectedTargets, ETargetType::Ground);
	}

	return true;
}
