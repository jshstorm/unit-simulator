#pragma once

#include "CoreMinimal.h"
#include "GameState/GameResult.h"

struct FSimGameSession;

/**
 * Evaluates win conditions: king destroyed, crown count, tower damage.
 * Handles regulation time, overtime, max game time transitions.
 * Ported from GameState/WinConditionEvaluator.cs (87 lines)
 */
class UNITSIMCORE_API FWinConditionEvaluator
{
public:
	/** Evaluate win conditions and update session result */
	void Evaluate(FSimGameSession& Session);

private:
	static void SetWinnerByCrowns(FSimGameSession& Session, EWinCondition Condition);
};
