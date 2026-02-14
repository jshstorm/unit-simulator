#include "GameState/WinConditionEvaluator.h"
#include "GameState/SimGameSession.h"

void FWinConditionEvaluator::Evaluate(FSimGameSession& Session)
{
	if (Session.Result != EGameResult::InProgress)
	{
		return;
	}

	const FTower* FriendlyKing = Session.GetKingTower(EUnitFaction::Friendly);
	const FTower* EnemyKing = Session.GetKingTower(EUnitFaction::Enemy);

	const bool bFriendlyKingDestroyed = FriendlyKing != nullptr && FriendlyKing->IsDestroyed();
	const bool bEnemyKingDestroyed = EnemyKing != nullptr && EnemyKing->IsDestroyed();

	// King destroyed -> immediate end
	if (bFriendlyKingDestroyed || bEnemyKingDestroyed)
	{
		if (bFriendlyKingDestroyed && bEnemyKingDestroyed)
		{
			Session.Result = EGameResult::Draw;
		}
		else if (bEnemyKingDestroyed)
		{
			Session.Result = EGameResult::FriendlyWin;
		}
		else
		{
			Session.Result = EGameResult::EnemyWin;
		}
		Session.WinConditionType = EWinCondition::KingDestroyed;
		return;
	}

	// Still in regulation time
	if (Session.ElapsedTime < Session.RegularTime)
	{
		return;
	}

	// Regulation time ended
	if (!Session.bIsOvertime)
	{
		// Check crown difference
		if (Session.FriendlyCrowns != Session.EnemyCrowns)
		{
			SetWinnerByCrowns(Session, EWinCondition::MoreCrownCount);
			return;
		}

		// Tied -> enter overtime
		Session.bIsOvertime = true;
		return;
	}

	// During overtime: check crown difference
	if (Session.FriendlyCrowns != Session.EnemyCrowns)
	{
		SetWinnerByCrowns(Session, EWinCondition::TieBreaker);
		return;
	}

	// Max game time not reached yet
	if (Session.ElapsedTime < Session.MaxGameTime)
	{
		return;
	}

	// Max game time reached: compare tower HP ratio
	const float FriendlyRatio = Session.GetTotalTowerHPRatio(EUnitFaction::Friendly);
	const float EnemyRatio = Session.GetTotalTowerHPRatio(EUnitFaction::Enemy);

	if (FMath::Abs(FriendlyRatio - EnemyRatio) < 0.0001f)
	{
		Session.Result = EGameResult::Draw;
		Session.WinConditionType = EWinCondition::MoreTowerDamage;
		return;
	}

	Session.Result = FriendlyRatio > EnemyRatio ? EGameResult::FriendlyWin : EGameResult::EnemyWin;
	Session.WinConditionType = EWinCondition::MoreTowerDamage;
}

void FWinConditionEvaluator::SetWinnerByCrowns(FSimGameSession& Session, EWinCondition Condition)
{
	Session.Result = Session.FriendlyCrowns > Session.EnemyCrowns
		? EGameResult::FriendlyWin
		: EGameResult::EnemyWin;
	Session.WinConditionType = Condition;
}
