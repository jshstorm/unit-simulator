#pragma once

#include "CoreMinimal.h"

struct FGameSession;

/**
 * Evaluates win conditions: king destroyed, crown count, tower damage.
 * Handles regulation time, overtime, max game time transitions.
 * Ported from GameState/WinConditionEvaluator.cs (87 lines)
 */
class UNITSIMCORE_API FWinConditionEvaluator
{
public:
	/** Evaluate win conditions and update session result */
	void Evaluate(FGameSession& Session);

private:
	static void SetWinnerByCrowns(FGameSession& Session, EWinCondition Condition);
};
