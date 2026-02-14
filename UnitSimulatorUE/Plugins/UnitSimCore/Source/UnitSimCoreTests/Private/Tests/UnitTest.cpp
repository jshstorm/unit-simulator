#include "Misc/AutomationTest.h"
#include "Units/Unit.h"
#include "GameConstants.h"

// ============================================================================
// Helper: Create a unit with Initialize()
// ============================================================================

static FUnit CreateTestUnit(int32 Id, EUnitFaction Faction, const FVector2D& Position,
	EUnitRole Role = EUnitRole::Melee, int32 HP = 100, int32 Damage = 10,
	float Radius = 20.f, float Speed = 4.0f)
{
	FUnit Unit;
	Unit.Initialize(Id, FName(TEXT("test_unit")), Faction, Position,
		Radius, Speed, 0.1f, Role, HP, Damage);
	return Unit;
}

// ============================================================================
// FUnit Creation & Initial State
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitCreation,
	"UnitSimCore.Unit.Creation.InitialState",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitCreation::RunTest(const FString& Parameters)
{
	// Arrange & Act
	FUnit Unit = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D(100.0, 200.0),
		EUnitRole::Melee, 150, 20, 25.f, 5.0f);

	// Assert
	TestEqual(TEXT("Id"), Unit.Id, 1);
	TestEqual(TEXT("Faction"), Unit.Faction, EUnitFaction::Friendly);
	TestEqual(TEXT("Position.X"), Unit.Position.X, 100.0);
	TestEqual(TEXT("Position.Y"), Unit.Position.Y, 200.0);
	TestEqual(TEXT("HP"), Unit.HP, 150);
	TestEqual(TEXT("Damage"), Unit.Damage, 20);
	TestEqual(TEXT("Radius"), Unit.Radius, 25.f);
	TestEqual(TEXT("Speed"), Unit.Speed, 5.0f);
	TestEqual(TEXT("Role"), Unit.Role, EUnitRole::Melee);
	TestFalse(TEXT("bIsDead"), Unit.bIsDead);
	TestEqual(TEXT("Velocity is zero"), Unit.Velocity, FVector2D::ZeroVector);
	TestEqual(TEXT("TargetIndex"), Unit.TargetIndex, -1);
	TestEqual(TEXT("TargetTowerIndex"), Unit.TargetTowerIndex, -1);
	TestEqual(TEXT("AttackCooldown"), Unit.AttackCooldown, 0.f);

	// Attack range should be computed from role and radius
	const float ExpectedRange = 25.f * UnitSimConstants::MELEE_RANGE_MULTIPLIER;
	TestEqual(TEXT("AttackRange melee"), Unit.AttackRange, ExpectedRange);

	// Attack slots should be initialized
	TestEqual(TEXT("AttackSlots count"), Unit.AttackSlots.Num(), UnitSimConstants::NUM_ATTACK_SLOTS);
	for (int32 i = 0; i < Unit.AttackSlots.Num(); ++i)
	{
		TestEqual(TEXT("AttackSlot empty"), Unit.AttackSlots[i], -1);
	}

	return true;
}

// ============================================================================
// FUnit Ranged Attack Range
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitRangedAttackRange,
	"UnitSimCore.Unit.Creation.RangedAttackRange",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitRangedAttackRange::RunTest(const FString& Parameters)
{
	// Arrange & Act
	FUnit Unit = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector,
		EUnitRole::Ranged, 100, 10, 20.f);

	// Assert
	const float ExpectedRange = 20.f * UnitSimConstants::RANGED_RANGE_MULTIPLIER;
	TestEqual(TEXT("AttackRange ranged"), Unit.AttackRange, ExpectedRange);

	return true;
}

// ============================================================================
// FUnit Movement (Position Update)
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitMovement,
	"UnitSimCore.Unit.Movement.PositionUpdate",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitMovement::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit Unit = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D(0.0, 0.0));
	Unit.Velocity = FVector2D(3.0, 4.0);

	// Act
	Unit.Position += Unit.Velocity;

	// Assert
	TestEqual(TEXT("Position.X after move"), Unit.Position.X, 3.0);
	TestEqual(TEXT("Position.Y after move"), Unit.Position.Y, 4.0);

	return true;
}

// ============================================================================
// FUnit Damage -> HP Decrease
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitDamageDecreasesHP,
	"UnitSimCore.Unit.Damage.DecreasesHP",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitDamageDecreasesHP::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit Unit = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector,
		EUnitRole::Melee, 100);

	// Act
	int32 HpDamage = Unit.TakeDamage(30);

	// Assert
	TestEqual(TEXT("HP after damage"), Unit.HP, 70);
	TestEqual(TEXT("HP damage dealt"), HpDamage, 30);
	TestFalse(TEXT("Not dead"), Unit.bIsDead);

	return true;
}

// ============================================================================
// FUnit Death (HP <= 0)
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitDeath,
	"UnitSimCore.Unit.Damage.DeathAtZeroHP",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitDeath::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit Unit = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector,
		EUnitRole::Melee, 5);
	Unit.Velocity = FVector2D(3.0, 4.0);

	// Act
	Unit.TakeDamage(5);

	// Assert
	TestEqual(TEXT("HP at zero"), Unit.HP, 0);
	TestTrue(TEXT("bIsDead"), Unit.bIsDead);
	TestEqual(TEXT("Velocity zeroed on death"), Unit.Velocity, FVector2D::ZeroVector);

	return true;
}

// ============================================================================
// FUnit Overkill
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitOverkill,
	"UnitSimCore.Unit.Damage.OverkillClampsToZero",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitOverkill::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit Unit = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector,
		EUnitRole::Melee, 10);

	// Act
	Unit.TakeDamage(100);

	// Assert
	TestEqual(TEXT("HP clamped to zero"), Unit.HP, 0);
	TestTrue(TEXT("bIsDead"), Unit.bIsDead);

	return true;
}

// ============================================================================
// FUnit Shield Mechanic
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitShieldAbsorbs,
	"UnitSimCore.Unit.Shield.AbsorbsDamageFirst",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitShieldAbsorbs::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit Unit = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector,
		EUnitRole::Melee, 100);
	Unit.ShieldHP = 50;
	Unit.MaxShieldHP = 50;

	// Act: Damage less than shield
	int32 HpDamage = Unit.TakeDamage(30);

	// Assert
	TestEqual(TEXT("ShieldHP after partial"), Unit.ShieldHP, 20);
	TestEqual(TEXT("HP unchanged"), Unit.HP, 100);
	TestEqual(TEXT("HP damage dealt"), HpDamage, 0);
	TestFalse(TEXT("Not dead"), Unit.bIsDead);

	return true;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitShieldBreaks,
	"UnitSimCore.Unit.Shield.OverflowDamageToHP",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitShieldBreaks::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit Unit = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector,
		EUnitRole::Melee, 100);
	Unit.ShieldHP = 20;
	Unit.MaxShieldHP = 20;

	// Act: Damage exceeds shield
	int32 HpDamage = Unit.TakeDamage(50);

	// Assert
	TestEqual(TEXT("ShieldHP depleted"), Unit.ShieldHP, 0);
	TestEqual(TEXT("HP reduced by overflow"), Unit.HP, 70);
	TestEqual(TEXT("HP damage dealt"), HpDamage, 30);
	TestFalse(TEXT("Not dead"), Unit.bIsDead);

	return true;
}

// ============================================================================
// FUnit Attack Slots
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitAttackSlotClaim,
	"UnitSimCore.Unit.AttackSlots.ClaimAndRelease",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitAttackSlotClaim::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit Target = CreateTestUnit(1, EUnitFaction::Enemy, FVector2D(100.0, 0.0));

	// Act: Claim a slot
	int32 Slot = Target.TryClaimSlot(42); // attacker index 42

	// Assert
	TestEqual(TEXT("Claimed slot 0"), Slot, 0);
	TestEqual(TEXT("Slot occupant"), Target.AttackSlots[0], 42);

	// Claim another
	int32 Slot2 = Target.TryClaimSlot(43);
	TestEqual(TEXT("Claimed slot 1"), Slot2, 1);

	// Release first
	Target.ReleaseSlot(42, 0);
	TestEqual(TEXT("Slot 0 released"), Target.AttackSlots[0], -1);
	TestEqual(TEXT("Slot 1 unchanged"), Target.AttackSlots[1], 43);

	return true;
}

// ============================================================================
// FUnit CanAttackUnit
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitCanAttackUnit,
	"UnitSimCore.Unit.Targeting.CanAttackUnit",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitCanAttackUnit::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit GroundAttacker = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector);
	GroundAttacker.CanTarget = ETargetType::Ground;

	FUnit GroundTarget = CreateTestUnit(2, EUnitFaction::Enemy, FVector2D(50.0, 0.0));
	GroundTarget.Layer = EMovementLayer::Ground;

	FUnit AirTarget = CreateTestUnit(3, EUnitFaction::Enemy, FVector2D(50.0, 0.0));
	AirTarget.Layer = EMovementLayer::Air;

	FUnit DeadTarget = CreateTestUnit(4, EUnitFaction::Enemy, FVector2D(50.0, 0.0));
	DeadTarget.bIsDead = true;

	// Assert
	TestTrue(TEXT("Ground can attack Ground"), GroundAttacker.CanAttackUnit(GroundTarget));
	TestFalse(TEXT("Ground cannot attack Air"), GroundAttacker.CanAttackUnit(AirTarget));
	TestFalse(TEXT("Cannot attack dead"), GroundAttacker.CanAttackUnit(DeadTarget));

	// GroundAndAir attacker
	FUnit AllAttacker = CreateTestUnit(5, EUnitFaction::Friendly, FVector2D::ZeroVector);
	AllAttacker.CanTarget = ETargetType::GroundAndAir;
	TestTrue(TEXT("GroundAndAir can attack Air"), AllAttacker.CanAttackUnit(AirTarget));

	return true;
}

// ============================================================================
// FUnit GetLabel
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitGetLabel,
	"UnitSimCore.Unit.GetLabel.FactionPrefix",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitGetLabel::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit Friendly = CreateTestUnit(5, EUnitFaction::Friendly, FVector2D::ZeroVector);
	FUnit Enemy = CreateTestUnit(3, EUnitFaction::Enemy, FVector2D::ZeroVector);

	// Assert
	TestEqual(TEXT("Friendly label"), Friendly.GetLabel(), TEXT("F5"));
	TestEqual(TEXT("Enemy label"), Enemy.GetLabel(), TEXT("E3"));

	return true;
}

// ============================================================================
// FUnit Charge State
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FUnitChargeEffectiveDamage,
	"UnitSimCore.Unit.Charge.EffectiveDamageMultiplied",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FUnitChargeEffectiveDamage::RunTest(const FString& Parameters)
{
	// Arrange
	FUnit Unit = CreateTestUnit(1, EUnitFaction::Friendly, FVector2D::ZeroVector,
		EUnitRole::Melee, 100, 10);
	Unit.bHasChargeAbility = true;
	Unit.ChargeAttackAbility.DamageMultiplier = 2.0f;
	Unit.ChargeAttackAbility.SpeedMultiplier = 2.0f;

	// Normal damage without charge
	TestEqual(TEXT("Normal damage"), Unit.GetEffectiveDamage(), 10);
	TestEqual(TEXT("Normal speed"), Unit.GetEffectiveSpeed(), 4.0f);

	// Start charging
	Unit.ChargeState.bIsCharging = true;
	TestTrue(TEXT("Speed multiplied while charging"),
		FMath::IsNearlyEqual(Unit.GetEffectiveSpeed(), 8.0f));
	TestEqual(TEXT("Damage not multiplied yet"), Unit.GetEffectiveDamage(), 10);

	// Charge complete
	Unit.ChargeState.bIsCharged = true;
	TestEqual(TEXT("Damage multiplied when charged"), Unit.GetEffectiveDamage(), 20);

	// Consume charge
	Unit.OnAttackPerformed();
	TestFalse(TEXT("Charge consumed"), Unit.ChargeState.bIsCharged);
	TestFalse(TEXT("Not charging"), Unit.ChargeState.bIsCharging);
	TestEqual(TEXT("Normal damage after consume"), Unit.GetEffectiveDamage(), 10);

	return true;
}
