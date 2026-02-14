#pragma once

#include "CoreMinimal.h"
#include "GameResult.generated.h"

/** Game result status */
UENUM(BlueprintType)
enum class EGameResult : uint8
{
	InProgress,
	FriendlyWin,
	EnemyWin,
	Draw
};

/** Win condition type */
UENUM(BlueprintType)
enum class EWinCondition : uint8
{
	/** Game still in progress */
	None,
	/** King Tower destroyed */
	KingDestroyed,
	/** More crowns at regulation time */
	MoreCrownCount,
	/** More tower damage in overtime */
	MoreTowerDamage,
	/** First tower destroyed in sudden death */
	TieBreaker
};
