#include "Misc/AutomationTest.h"
#include "GameState/GameSession.h"
#include "GameState/InitialSetup.h"
#include "GameState/WinConditionEvaluator.h"
#include "GameState/GameResult.h"
#include "Towers/Tower.h"
#include "Towers/TowerStats.h"

// ============================================================================
// FGameSession Initialization (Default Towers)
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FGameSessionInit,
	"UnitSimCore.GameSession.Initialize.DefaultTowers",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FGameSessionInit::RunTest(const FString& Parameters)
{
	// Arrange & Act
	FGameSession Session;
	Session.InitializeDefaultTowers();

	// Assert: Clash Royale standard = 3 towers per faction
	TestEqual(TEXT("Friendly towers"), Session.FriendlyTowers.Num(), 3);
	TestEqual(TEXT("Enemy towers"), Session.EnemyTowers.Num(), 3);

	// Verify tower types
	int32 FriendlyKingCount = 0;
	int32 FriendlyPrincessCount = 0;
	for (const FTower& T : Session.FriendlyTowers)
	{
		TestEqual(TEXT("Friendly faction"), T.Faction, EUnitFaction::Friendly);
		if (T.Type == ETowerType::King) ++FriendlyKingCount;
		if (T.Type == ETowerType::Princess) ++FriendlyPrincessCount;
	}
	TestEqual(TEXT("1 King tower"), FriendlyKingCount, 1);
	TestEqual(TEXT("2 Princess towers"), FriendlyPrincessCount, 2);

	// King tower should start deactivated by default
	int32 KingIdx = Session.GetKingTowerIndex(EUnitFaction::Friendly);
	TestTrue(TEXT("King tower found"), KingIdx >= 0);
	if (KingIdx >= 0)
	{
		TestFalse(TEXT("King tower starts deactivated"),
			Session.FriendlyTowers[KingIdx].bIsActivated);
	}

	return true;
}

// ============================================================================
// Tower Damage -> Crown Award
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FGameSessionCrowns,
	"UnitSimCore.GameSession.Crowns.AwardedOnTowerDestruction",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FGameSessionCrowns::RunTest(const FString& Parameters)
{
	// Arrange
	FGameSession Session;
	Session.InitializeDefaultTowers();

	// Initial crowns should be 0
	TestEqual(TEXT("Initial friendly crowns"), Session.FriendlyCrowns, 0);
	TestEqual(TEXT("Initial enemy crowns"), Session.EnemyCrowns, 0);

	// Destroy an enemy princess tower
	for (FTower& T : Session.EnemyTowers)
	{
		if (T.Type == ETowerType::Princess)
		{
			T.TakeDamage(T.MaxHP); // destroy it
			break;
		}
	}

	// Act
	Session.UpdateCrowns();

	// Assert: friendly side gains a crown for destroying enemy tower
	TestEqual(TEXT("Friendly crowns after destroying enemy princess"), Session.FriendlyCrowns, 1);
	TestEqual(TEXT("Enemy crowns unchanged"), Session.EnemyCrowns, 0);

	return true;
}

// ============================================================================
// King Tower Activation
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FGameSessionKingActivation,
	"UnitSimCore.GameSession.KingTower.ActivatesOnPrincessDestruction",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FGameSessionKingActivation::RunTest(const FString& Parameters)
{
	// Arrange
	FGameSession Session;
	Session.InitializeDefaultTowers();

	// King tower should start deactivated
	const FTower* FriendlyKing = Session.GetKingTower(EUnitFaction::Friendly);
	TestNotNull(TEXT("Friendly king exists"), FriendlyKing);
	if (FriendlyKing)
	{
		TestFalse(TEXT("King starts deactivated"), FriendlyKing->bIsActivated);
	}

	// Destroy a friendly princess tower (enemy attacks)
	for (FTower& T : Session.FriendlyTowers)
	{
		if (T.Type == ETowerType::Princess)
		{
			T.TakeDamage(T.MaxHP);
			break;
		}
	}

	// Act
	Session.UpdateKingTowerActivation();

	// Assert: king tower should now be activated
	FriendlyKing = Session.GetKingTower(EUnitFaction::Friendly);
	TestNotNull(TEXT("King still exists"), FriendlyKing);
	if (FriendlyKing)
	{
		TestTrue(TEXT("King activated after princess destroyed"), FriendlyKing->bIsActivated);
	}

	return true;
}

// ============================================================================
// WinConditionEvaluator: King Destroyed
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FWinCondKingDestroyed,
	"UnitSimCore.GameSession.WinCondition.KingDestroyedWins",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FWinCondKingDestroyed::RunTest(const FString& Parameters)
{
	// Arrange
	FGameSession Session;
	Session.InitializeDefaultTowers();
	Session.ElapsedTime = 10.f; // within regulation time

	FWinConditionEvaluator Evaluator;

	// Destroy enemy king tower
	FTower* EnemyKing = Session.GetKingTower(EUnitFaction::Enemy);
	TestNotNull(TEXT("Enemy king exists"), EnemyKing);
	if (EnemyKing)
	{
		EnemyKing->TakeDamage(EnemyKing->MaxHP);
	}
	Session.UpdateCrowns();

	// Act
	Evaluator.Evaluate(Session);

	// Assert
	TestEqual(TEXT("Friendly wins"), Session.Result, EGameResult::FriendlyWin);
	TestEqual(TEXT("Win by king destroyed"), Session.WinConditionType, EWinCondition::KingDestroyed);

	return true;
}

// ============================================================================
// FTower Stats
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FTowerCreation,
	"UnitSimCore.GameSession.Tower.FactoryMethods",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FTowerCreation::RunTest(const FString& Parameters)
{
	// Princess Tower
	FTower Princess = FTower::CreatePrincessTower(1, EUnitFaction::Friendly, FVector2D(600.0, 1200.0));
	TestEqual(TEXT("Princess Type"), Princess.Type, ETowerType::Princess);
	TestEqual(TEXT("Princess MaxHP"), Princess.MaxHP, TowerStatsData::PrincessMaxHP);
	TestEqual(TEXT("Princess CurrentHP"), Princess.CurrentHP, TowerStatsData::PrincessMaxHP);
	TestEqual(TEXT("Princess Damage"), Princess.Damage, TowerStatsData::PrincessDamage);
	TestTrue(TEXT("Princess activated"), Princess.bIsActivated);

	// King Tower
	FTower King = FTower::CreateKingTower(2, EUnitFaction::Enemy, FVector2D(1600.0, 4400.0));
	TestEqual(TEXT("King Type"), King.Type, ETowerType::King);
	TestEqual(TEXT("King MaxHP"), King.MaxHP, TowerStatsData::KingMaxHP);
	TestEqual(TEXT("King Radius"), King.Radius, TowerStatsData::KingRadius);
	TestFalse(TEXT("King starts deactivated"), King.bIsActivated);

	return true;
}

// ============================================================================
// FTower Damage
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FTowerDamage,
	"UnitSimCore.GameSession.Tower.TakeDamageAndDestroyed",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FTowerDamage::RunTest(const FString& Parameters)
{
	// Arrange
	FTower Tower = FTower::CreatePrincessTower(1, EUnitFaction::Friendly, FVector2D::ZeroVector);
	int32 InitialHP = Tower.CurrentHP;

	// Act: partial damage
	Tower.TakeDamage(100);
	TestEqual(TEXT("HP reduced"), Tower.CurrentHP, InitialHP - 100);
	TestFalse(TEXT("Not destroyed"), Tower.IsDestroyed());

	// Act: lethal damage
	Tower.TakeDamage(Tower.CurrentHP);
	TestTrue(TEXT("Destroyed"), Tower.IsDestroyed());
	TestTrue(TEXT("HP <= 0"), Tower.CurrentHP <= 0);

	return true;
}

// ============================================================================
// FGameSession GetTotalTowerHPRatio
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FGameSessionHPRatio,
	"UnitSimCore.GameSession.Tower.HPRatio",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FGameSessionHPRatio::RunTest(const FString& Parameters)
{
	// Arrange
	FGameSession Session;
	Session.InitializeDefaultTowers();

	// Act: full HP
	float FullRatio = Session.GetTotalTowerHPRatio(EUnitFaction::Friendly);
	TestTrue(TEXT("Full HP ratio is 1.0"),
		FMath::IsNearlyEqual(FullRatio, 1.0f, 0.01f));

	// Damage a tower
	Session.FriendlyTowers[0].TakeDamage(Session.FriendlyTowers[0].MaxHP / 2);
	float PartialRatio = Session.GetTotalTowerHPRatio(EUnitFaction::Friendly);
	TestTrue(TEXT("Partial ratio < 1.0"), PartialRatio < 1.0f);
	TestTrue(TEXT("Partial ratio > 0"), PartialRatio > 0.f);

	return true;
}

// ============================================================================
// FTower Cooldown
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FTowerCooldown,
	"UnitSimCore.GameSession.Tower.AttackCooldown",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FTowerCooldown::RunTest(const FString& Parameters)
{
	// Arrange
	FTower Tower = FTower::CreatePrincessTower(1, EUnitFaction::Friendly, FVector2D::ZeroVector);

	// Initially ready
	TestTrue(TEXT("Ready to attack initially"), Tower.IsReadyToAttack());

	// Perform attack
	Tower.OnAttackPerformed();
	TestFalse(TEXT("Not ready after attack"), Tower.IsReadyToAttack());

	// Update cooldown
	float LargeDelta = 10.f; // large enough to clear cooldown
	Tower.UpdateCooldown(LargeDelta);
	TestTrue(TEXT("Ready after cooldown"), Tower.IsReadyToAttack());

	return true;
}
