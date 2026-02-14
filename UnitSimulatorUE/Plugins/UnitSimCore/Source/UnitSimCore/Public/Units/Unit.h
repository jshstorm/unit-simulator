#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "Abilities/AbilityTypes.h"
#include "Units/ChargeState.h"
#include "Unit.generated.h"

// Forward declarations
class FTower;

/**
 * Core unit state and behavior.
 * Ported from Unit.cs (448 lines)
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FUnit
{
	GENERATED_BODY()

	// ════════════════════════════════════════════════════════════════════════
	// Identity
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Id = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName UnitId;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	// ════════════════════════════════════════════════════════════════════════
	// Transform
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Position = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Velocity = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Forward = FVector2D(1.0, 0.0);

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Radius = 20.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Speed = 4.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float TurnSpeed = 0.1f;

	// ════════════════════════════════════════════════════════════════════════
	// Stats
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 HP = 100;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Damage = 1;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitRole Role = EUnitRole::Melee;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackRange = 60.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackCooldown = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsDead = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EMovementLayer Layer = EMovementLayer::Ground;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETargetType CanTarget = ETargetType::Ground;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETargetPriority TargetPriority = ETargetPriority::Nearest;

	// ════════════════════════════════════════════════════════════════════════
	// Targeting
	// ════════════════════════════════════════════════════════════════════════

	/** Index of the target unit in the unit array (-1 = none) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TargetIndex = -1;

	/** Index of the target tower in the tower array (-1 = none) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TargetTowerIndex = -1;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D CurrentDestination = FVector2D::ZeroVector;

	/** Index of avoidance threat unit (-1 = none) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 AvoidanceThreatIndex = -1;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D AvoidanceTarget = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bHasAvoidanceTarget = false;

	// ════════════════════════════════════════════════════════════════════════
	// Attack Slots
	// ════════════════════════════════════════════════════════════════════════

	/** Attack slot occupants (unit indices, -1 = empty) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<int32> AttackSlots;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 TakenSlotIndex = -1;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FramesSinceSlotEvaluation = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FramesSinceTargetEvaluation = 0;

	// ════════════════════════════════════════════════════════════════════════
	// Path Progress Tracking (Replan Triggers)
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FramesSinceLastWaypointProgress = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FramesSinceAvoidanceStart = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 LastReplanFrame = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D PreviousPosition = FVector2D::ZeroVector;

	// ════════════════════════════════════════════════════════════════════════
	// Shield & Abilities
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxShieldHP = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 ShieldHP = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FChargeState ChargeState;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bHasChargeAbility = false;

	/** Ability data stored as typed structs */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FAbilityData> Abilities;

	// Typed ability caches (populated during init)
	FChargeAttackData ChargeAttackAbility;
	FSplashDamageData SplashDamageAbility;
	FShieldData ShieldAbility;
	FDeathSpawnData DeathSpawnAbility;
	FDeathDamageData DeathDamageAbility;
	FStatusEffectAbilityData StatusEffectAbility;

	bool bHasSplashDamage = false;
	bool bHasShield = false;
	bool bHasDeathSpawn = false;
	bool bHasDeathDamage = false;
	bool bHasStatusEffect = false;

	// ════════════════════════════════════════════════════════════════════════
	// Avoidance / Movement Paths (runtime, not serialized)
	// ════════════════════════════════════════════════════════════════════════

	TArray<FVector2D> AvoidancePath;
	int32 AvoidancePathIndex = 0;

	TArray<FVector2D> MovementPath;
	int32 MovementPathIndex = 0;

	// ════════════════════════════════════════════════════════════════════════
	// Init
	// ════════════════════════════════════════════════════════════════════════

	FUnit()
	{
		AttackSlots.SetNum(UnitSimConstants::NUM_ATTACK_SLOTS);
		for (int32 i = 0; i < UnitSimConstants::NUM_ATTACK_SLOTS; ++i)
		{
			AttackSlots[i] = -1;
		}
	}

	void Initialize(int32 InId, const FName& InUnitId, EUnitFaction InFaction,
		const FVector2D& InPosition, float InRadius, float InSpeed, float InTurnSpeed,
		EUnitRole InRole, int32 InHP, int32 InDamage,
		EMovementLayer InLayer = EMovementLayer::Ground,
		ETargetType InCanTarget = ETargetType::Ground,
		ETargetPriority InTargetPriority = ETargetPriority::Nearest);

	// ════════════════════════════════════════════════════════════════════════
	// Methods
	// ════════════════════════════════════════════════════════════════════════

	FString GetLabel() const;
	bool HasAbility(EAbilityType Type) const;

	/** Check if this unit can attack the target unit */
	bool CanAttackUnit(const FUnit& InTarget) const;

	/** Check if this unit is on the same movement layer */
	bool IsSameLayer(const FUnit& Other) const;

	/** Get slot world position */
	FVector2D GetSlotPosition(int32 SlotIndex, float AttackerRadius) const;

	/** Try to claim an empty attack slot. Returns slot index or -1 */
	int32 TryClaimSlot(int32 AttackerIndex);

	/** Claim the best (nearest) attack slot. Returns slot index or -1 */
	int32 ClaimBestSlot(int32 AttackerIndex, const FVector2D& AttackerPosition, float AttackerRadius);

	/** Release a slot previously occupied by attacker */
	void ReleaseSlot(int32 AttackerIndex, int32 SlotIdx);

	// Path management
	void SetAvoidancePath(const TArray<FVector2D>& Waypoints);
	bool TryGetNextAvoidanceWaypoint(FVector2D& OutWaypoint) const;
	void ClearAvoidancePath();

	void SetMovementPath(const TArray<FVector2D>& Path);
	bool TryGetNextMovementWaypoint(FVector2D& OutWaypoint);
	void ClearMovementPath();

	// Rotation
	void UpdateRotation();

	/**
	 * Apply damage. Shield absorbs first.
	 * Returns actual HP damage dealt (excluding shield).
	 */
	int32 TakeDamage(int32 InDamage = 1);

	/** Effective speed (charge multiplier applied if charging) */
	float GetEffectiveSpeed() const;

	/** Effective damage (charge multiplier applied if charged) */
	int32 GetEffectiveDamage() const;

	/** Called after an attack is performed (consumes charge, etc.) */
	void OnAttackPerformed();
};
