#include "Misc/AutomationTest.h"
#include "Combat/CombatSystem.h"
#include "Combat/FrameEvents.h"
#include "Units/Unit.h"

// ============================================================================
// Helper
// ============================================================================

static FUnit CreateCombatUnit(int32 Id, EUnitFaction Faction, const FVector2D& Position,
	int32 HP = 100, int32 Damage = 10, float Radius = 10.f)
{
	FUnit Unit;
	Unit.Initialize(Id, FName(TEXT("combat_unit")), Faction, Position,
		Radius, 5.f, 0.1f, EUnitRole::Melee, HP, Damage);
	return Unit;
}

// ============================================================================
// FCombatSystem Damage Event Collection
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FCombatCollectDamageEvents,
	"UnitSimCore.Combat.CollectAttackEvents.GeneratesDamageEvent",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FCombatCollectDamageEvents::RunTest(const FString& Parameters)
{
	// Arrange
	FCombatSystem Combat;
	FFrameEvents Events;

	FUnit Attacker = CreateCombatUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector, 100, 10);
	FUnit Target = CreateCombatUnit(2, EUnitFaction::Enemy, FVector2D(10.0, 0.0), 50, 0);
	TArray<FUnit> Enemies = { Target };

	// Act
	Combat.CollectAttackEvents(Attacker, 0, Target, 0, Enemies, Events);

	// Assert: event collected, HP unchanged (Phase 1)
	TestEqual(TEXT("Damage event count"), Events.GetDamageCount(), 1);
	TestEqual(TEXT("Source index"), Events.Damages[0].SourceIndex, 0);
	TestEqual(TEXT("Target index"), Events.Damages[0].TargetIndex, 0);
	TestEqual(TEXT("Damage amount"), Events.Damages[0].Amount, 10);
	TestEqual(TEXT("Damage type"), Events.Damages[0].Type, EDamageType::Normal);
	TestEqual(TEXT("Target HP unchanged (Phase 1)"), Target.HP, 50);

	return true;
}

// ============================================================================
// Splash Damage Range Calculation
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FCombatSplashDamage,
	"UnitSimCore.Combat.SplashDamage.HitsNearbyEnemies",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FCombatSplashDamage::RunTest(const FString& Parameters)
{
	// Arrange
	FCombatSystem Combat;
	FFrameEvents Events;

	FUnit Attacker = CreateCombatUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector, 100, 10);
	Attacker.bHasSplashDamage = true;
	Attacker.SplashDamageAbility.Radius = 40.f;
	Attacker.SplashDamageAbility.DamageFalloff = 0.f;

	FUnit Primary = CreateCombatUnit(2, EUnitFaction::Enemy, FVector2D(10.0, 0.0), 50, 0);
	FUnit InRange = CreateCombatUnit(3, EUnitFaction::Enemy, FVector2D(20.0, 0.0), 30, 0);
	FUnit OutOfRange = CreateCombatUnit(4, EUnitFaction::Enemy, FVector2D(200.0, 0.0), 30, 0);

	TArray<FUnit> Enemies = { Primary, InRange, OutOfRange };

	// Act
	Combat.CollectAttackEvents(Attacker, 0, Primary, 0, Enemies, Events);

	// Assert: primary + 1 splash (InRange), OutOfRange excluded
	TestEqual(TEXT("Total damage events"), Events.GetDamageCount(), 2);

	// Primary event
	bool bHasPrimary = false;
	bool bHasSplash = false;
	for (const FDamageEvent& Evt : Events.Damages)
	{
		if (Evt.TargetIndex == 0 && Evt.Type == EDamageType::Normal) bHasPrimary = true;
		if (Evt.TargetIndex == 1 && Evt.Type == EDamageType::Splash) bHasSplash = true;
	}
	TestTrue(TEXT("Primary target hit"), bHasPrimary);
	TestTrue(TEXT("Splash target hit"), bHasSplash);

	return true;
}

// ============================================================================
// DeathSpawn Event Generation
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FCombatDeathSpawn,
	"UnitSimCore.Combat.DeathSpawn.GeneratesSpawnRequests",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FCombatDeathSpawn::RunTest(const FString& Parameters)
{
	// Arrange
	FCombatSystem Combat;

	FUnit DeadUnit = CreateCombatUnit(1, EUnitFaction::Enemy, FVector2D(100.0, 100.0), 0, 0);
	DeadUnit.bIsDead = true;
	DeadUnit.bHasDeathSpawn = true;
	DeadUnit.DeathSpawnAbility.SpawnUnitId = FName(TEXT("minion"));
	DeadUnit.DeathSpawnAbility.SpawnCount = 3;
	DeadUnit.DeathSpawnAbility.SpawnRadius = 20.f;
	DeadUnit.DeathSpawnAbility.SpawnUnitHP = 0;

	// Act
	TArray<FUnitSpawnRequest> Spawns = Combat.CreateDeathSpawnRequests(DeadUnit);

	// Assert
	TestEqual(TEXT("Spawn count"), Spawns.Num(), 3);
	for (const FUnitSpawnRequest& Req : Spawns)
	{
		TestEqual(TEXT("Spawn UnitId"), Req.UnitId, FName(TEXT("minion")));
		TestEqual(TEXT("Spawn Faction"), Req.Faction, EUnitFaction::Enemy);
		// Positions should be spread around dead unit
		float Dist = FVector2D::Distance(Req.Position, DeadUnit.Position);
		TestTrue(TEXT("Spawn within radius"), Dist <= 20.f + 1.f); // small epsilon
	}

	return true;
}

// ============================================================================
// DeathSpawn - No Ability
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FCombatDeathSpawnNoAbility,
	"UnitSimCore.Combat.DeathSpawn.NoAbilityReturnsEmpty",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FCombatDeathSpawnNoAbility::RunTest(const FString& Parameters)
{
	// Arrange
	FCombatSystem Combat;
	FUnit DeadUnit = CreateCombatUnit(1, EUnitFaction::Enemy, FVector2D::ZeroVector);
	DeadUnit.bIsDead = true;

	// Act
	TArray<FUnitSpawnRequest> Spawns = Combat.CreateDeathSpawnRequests(DeadUnit);

	// Assert
	TestEqual(TEXT("No spawns"), Spawns.Num(), 0);

	return true;
}

// ============================================================================
// 2-Phase Order Verification
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FCombatTwoPhaseOrder,
	"UnitSimCore.Combat.TwoPhase.CollectThenApply",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FCombatTwoPhaseOrder::RunTest(const FString& Parameters)
{
	// Arrange
	FCombatSystem Combat;
	FFrameEvents Events;

	FUnit Attacker = CreateCombatUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector, 100, 25);
	FUnit Target = CreateCombatUnit(2, EUnitFaction::Enemy, FVector2D(10.0, 0.0), 50, 0);
	TArray<FUnit> Enemies = { Target };

	// Phase 1: Collect
	Combat.CollectAttackEvents(Attacker, 0, Target, 0, Enemies, Events);

	// Phase 1 verification: target HP must NOT change
	TestEqual(TEXT("Target HP unchanged in Phase 1"), Enemies[0].HP, 50);
	TestEqual(TEXT("Events collected"), Events.GetDamageCount(), 1);

	// Phase 2: Apply (simulate manual application)
	for (const FDamageEvent& Evt : Events.Damages)
	{
		if (Evt.TargetIndex >= 0 && Evt.TargetIndex < Enemies.Num())
		{
			Enemies[Evt.TargetIndex].TakeDamage(Evt.Amount);
		}
	}

	// Phase 2 verification: target HP should now change
	TestEqual(TEXT("Target HP reduced in Phase 2"), Enemies[0].HP, 25);

	return true;
}

// ============================================================================
// FFrameEvents Clear
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FFrameEventsClear,
	"UnitSimCore.Combat.FrameEvents.ClearResetsAll",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FFrameEventsClear::RunTest(const FString& Parameters)
{
	// Arrange
	FFrameEvents Events;
	Events.AddDamage(0, 1, 10);
	Events.AddTowerDamage(0, 1, 20);
	Events.AddDamageToTower(0, 1, 30);
	FUnitSpawnRequest Req;
	Req.UnitId = FName(TEXT("test"));
	Events.AddSpawn(Req);

	// Act
	Events.Clear();

	// Assert
	TestEqual(TEXT("Damages cleared"), Events.GetDamageCount(), 0);
	TestEqual(TEXT("TowerDamages cleared"), Events.GetTowerDamageCount(), 0);
	TestEqual(TEXT("DamageToTowers cleared"), Events.GetDamageToTowerCount(), 0);
	TestEqual(TEXT("Spawns cleared"), Events.GetSpawnCount(), 0);

	return true;
}

// ============================================================================
// Death Damage
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FCombatDeathDamage,
	"UnitSimCore.Combat.DeathDamage.DamagesNearbyEnemies",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FCombatDeathDamage::RunTest(const FString& Parameters)
{
	// Arrange
	FCombatSystem Combat;

	FUnit DeadUnit = CreateCombatUnit(1, EUnitFaction::Enemy, FVector2D(10.0, 0.0), 0, 0);
	DeadUnit.bIsDead = true;
	DeadUnit.bHasDeathDamage = true;
	DeadUnit.DeathDamageAbility.Damage = 50;
	DeadUnit.DeathDamageAbility.Radius = 30.f;

	FUnit NearEnemy = CreateCombatUnit(2, EUnitFaction::Friendly, FVector2D(20.0, 0.0), 40, 0);
	FUnit FarEnemy = CreateCombatUnit(3, EUnitFaction::Friendly, FVector2D(200.0, 0.0), 40, 0);

	TArray<FUnit> Enemies = { NearEnemy, FarEnemy };

	// Act
	TArray<int32> NewlyDead = Combat.ApplyDeathDamage(DeadUnit, Enemies);

	// Assert
	TestTrue(TEXT("Near enemy killed"), Enemies[0].bIsDead);
	TestFalse(TEXT("Far enemy alive"), Enemies[1].bIsDead);
	TestEqual(TEXT("Far enemy HP unchanged"), Enemies[1].HP, 40);
	TestTrue(TEXT("NewlyDead contains near"), NewlyDead.Contains(0));
	TestFalse(TEXT("NewlyDead excludes far"), NewlyDead.Contains(1));

	return true;
}
