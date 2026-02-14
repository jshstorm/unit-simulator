#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"

/**
 * Tower base stats and factory methods.
 * Level 11 stats reference.
 * Ported from Towers/TowerStats.cs
 */
namespace TowerStatsData
{
	// Princess Tower Stats (Level 11)
	constexpr int32 PrincessMaxHP = 3052;
	constexpr int32 PrincessDamage = 109;
	constexpr float PrincessAttackSpeed = 1.25f;
	constexpr float PrincessAttackRange = 350.f;
	constexpr float PrincessRadius = 100.f;

	// King Tower Stats (Level 11)
	constexpr int32 KingMaxHP = 4824;
	constexpr int32 KingDamage = 109;
	constexpr float KingAttackSpeed = 1.0f;
	constexpr float KingAttackRange = 350.f;
	constexpr float KingRadius = 150.f;
}
