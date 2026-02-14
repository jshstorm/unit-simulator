#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "GameState/GameResult.h"
#include "Towers/Tower.h"
#include "SimGameSession.generated.h"

struct FTowerSetup;

/**
 * Game session state: towers, crowns, time, result.
 * Ported from GameState/GameSession.cs (323 lines)
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FSimGameSession
{
	GENERATED_BODY()

	// ════════════════════════════════════════════════════════════════════════
	// Towers
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FTower> FriendlyTowers;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FTower> EnemyTowers;

	// ════════════════════════════════════════════════════════════════════════
	// Game Time
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float ElapsedTime = 0.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float RegularTime = 180.f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float MaxGameTime = 300.f;

	// ════════════════════════════════════════════════════════════════════════
	// Crowns & Result
	// ════════════════════════════════════════════════════════════════════════

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 FriendlyCrowns = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 EnemyCrowns = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EGameResult Result = EGameResult::InProgress;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EWinCondition WinConditionType = EWinCondition::None;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsOvertime = false;

	// ════════════════════════════════════════════════════════════════════════
	// Initialization
	// ════════════════════════════════════════════════════════════════════════

	/** Initialize towers from setup list */
	void InitializeTowers(const TArray<FTowerSetup>& TowerSetups);

	/** Initialize with default Clash Royale layout (6 towers) */
	void InitializeDefaultTowers();

	// ════════════════════════════════════════════════════════════════════════
	// Tower Queries
	// ════════════════════════════════════════════════════════════════════════

	/** Get towers for a faction */
	TArray<FTower>& GetTowers(EUnitFaction Faction);
	const TArray<FTower>& GetTowers(EUnitFaction Faction) const;

	/** Get king tower index for faction (-1 if not found) */
	int32 GetKingTowerIndex(EUnitFaction Faction) const;

	/** Get king tower for faction (nullptr if not found) */
	FTower* GetKingTower(EUnitFaction Faction);
	const FTower* GetKingTower(EUnitFaction Faction) const;

	/** Get all towers combined */
	void GetAllTowers(TArray<FTower*>& OutTowers);

	// ════════════════════════════════════════════════════════════════════════
	// Crown Calculation
	// ════════════════════════════════════════════════════════════════════════

	/** Update crown counts from destroyed towers */
	void UpdateCrowns();

	// ════════════════════════════════════════════════════════════════════════
	// King Tower Activation
	// ════════════════════════════════════════════════════════════════════════

	/** Activate king tower when princess is destroyed */
	void UpdateKingTowerActivation();

	// ════════════════════════════════════════════════════════════════════════
	// Tower HP Ratio
	// ════════════════════════════════════════════════════════════════════════

	/** Get total tower HP ratio for a faction (0.0 - 1.0) */
	float GetTotalTowerHPRatio(EUnitFaction Faction) const;

private:
	FTower CreateTowerFromSetup(int32 Id, const FTowerSetup& Setup);
	static FVector2D GetDefaultTowerPosition(ETowerType Type, EUnitFaction Faction);
	int32 CountCrownsFromDestroyedTowers(EUnitFaction DestroyedFaction) const;
	void UpdateKingActivationForFaction(EUnitFaction Faction);
};
