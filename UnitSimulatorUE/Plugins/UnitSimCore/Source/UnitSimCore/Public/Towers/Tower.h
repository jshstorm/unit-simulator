#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "Towers/TowerStats.h"
#include "Tower.generated.h"

// Forward declaration
struct FUnit;

/**
 * Game tower state.
 * Each faction has 2 Princess Towers and 1 King Tower.
 * Ported from Towers/Tower.cs (170 lines)
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FTower
{
	GENERATED_BODY()

	// ════════════════════════════════════════════════════════════════════════
	// Identity
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Id = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETowerType Type = ETowerType::Princess;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EUnitFaction Faction = EUnitFaction::Friendly;

	// ════════════════════════════════════════════════════════════════════════
	// Position & Size
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Position = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Radius = 100.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackRange = 350.f;

	// ════════════════════════════════════════════════════════════════════════
	// Stats
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxHP = 3052;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 CurrentHP = 3052;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Damage = 109;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackSpeed = 1.25f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ETargetType CanTarget = ETargetType::GroundAndAir;

	// ════════════════════════════════════════════════════════════════════════
	// State
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsActivated = true;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float AttackCooldown = 0.f;

	/** Index of current target unit (-1 = none) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 CurrentTargetIndex = -1;

	// ════════════════════════════════════════════════════════════════════════
	// Computed
	// ════════════════════════════════════════════════════════════════════════

	bool IsDestroyed() const { return CurrentHP <= 0; }
	bool IsReadyToAttack() const { return AttackCooldown <= 0.f && !IsDestroyed(); }

	// ════════════════════════════════════════════════════════════════════════
	// Methods
	// ════════════════════════════════════════════════════════════════════════

	void TakeDamage(int32 Amount);
	bool CanAttackUnit(const FUnit& InTarget) const;
	void OnAttackPerformed();
	void UpdateCooldown(float DeltaTime);

	// ════════════════════════════════════════════════════════════════════════
	// Factory
	// ════════════════════════════════════════════════════════════════════════

	static FTower CreatePrincessTower(int32 InId, EUnitFaction InFaction, const FVector2D& InPosition);
	static FTower CreatePrincessTower(int32 InId, EUnitFaction InFaction, const FVector2D& InPosition, int32 InHP);
	static FTower CreateKingTower(int32 InId, EUnitFaction InFaction, const FVector2D& InPosition);
	static FTower CreateKingTower(int32 InId, EUnitFaction InFaction, const FVector2D& InPosition, int32 InHP);
};
