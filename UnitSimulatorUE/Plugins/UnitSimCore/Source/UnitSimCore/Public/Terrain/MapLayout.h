#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"

/**
 * Static map layout definition.
 * Clash Royale style vertical map (3200 x 5100).
 * Ported from GameState/MapLayout.cs (236 lines)
 */
namespace MapLayout
{
	// ════════════════════════════════════════════════════════════════════════
	// Map Size
	// ════════════════════════════════════════════════════════════════════════

	constexpr float MAP_WIDTH = static_cast<float>(UnitSimConstants::SIMULATION_WIDTH);
	constexpr float MAP_HEIGHT = static_cast<float>(UnitSimConstants::SIMULATION_HEIGHT);
	constexpr float TILE_SIZE = 100.f;

	// ════════════════════════════════════════════════════════════════════════
	// Friendly Tower Positions (bottom of map)
	// ════════════════════════════════════════════════════════════════════════

	inline FVector2D FriendlyKingPosition() { return FVector2D(1600.0, 700.0); }
	inline FVector2D FriendlyPrincessLeftPosition() { return FVector2D(600.0, 1200.0); }
	inline FVector2D FriendlyPrincessRightPosition() { return FVector2D(2600.0, 1200.0); }

	// ════════════════════════════════════════════════════════════════════════
	// Enemy Tower Positions (top of map)
	// ════════════════════════════════════════════════════════════════════════

	inline FVector2D EnemyKingPosition() { return FVector2D(1600.0, 4400.0); }
	inline FVector2D EnemyPrincessLeftPosition() { return FVector2D(600.0, 3900.0); }
	inline FVector2D EnemyPrincessRightPosition() { return FVector2D(2600.0, 3900.0); }

	// ════════════════════════════════════════════════════════════════════════
	// River
	// ════════════════════════════════════════════════════════════════════════

	constexpr float RIVER_Y_MIN = 2400.f;
	constexpr float RIVER_Y_MAX = 2700.f;
	constexpr float RIVER_WIDTH = RIVER_Y_MAX - RIVER_Y_MIN;

	// ════════════════════════════════════════════════════════════════════════
	// Bridges
	// ════════════════════════════════════════════════════════════════════════

	constexpr float LEFT_BRIDGE_X_MIN = 400.f;
	constexpr float LEFT_BRIDGE_X_MAX = 800.f;
	constexpr float RIGHT_BRIDGE_X_MIN = 2400.f;
	constexpr float RIGHT_BRIDGE_X_MAX = 2800.f;

	// ════════════════════════════════════════════════════════════════════════
	// Spawn Areas
	// ════════════════════════════════════════════════════════════════════════

	constexpr float FRIENDLY_SPAWN_Y_MAX = RIVER_Y_MIN;
	constexpr float ENEMY_SPAWN_Y_MIN = RIVER_Y_MAX;

	// Spawn Zones
	constexpr float FRIENDLY_SPAWN_ZONE_X_MIN = 800.f;
	constexpr float FRIENDLY_SPAWN_ZONE_X_MAX = 2400.f;
	constexpr float FRIENDLY_SPAWN_ZONE_Y_MIN = 1400.f;
	constexpr float FRIENDLY_SPAWN_ZONE_Y_MAX = 1700.f;

	constexpr float ENEMY_SPAWN_ZONE_X_MIN = 800.f;
	constexpr float ENEMY_SPAWN_ZONE_X_MAX = 2400.f;
	constexpr float ENEMY_SPAWN_ZONE_Y_MIN = 3400.f;
	constexpr float ENEMY_SPAWN_ZONE_Y_MAX = 3700.f;

	inline FVector2D FriendlyDefaultSpawnPosition() { return FVector2D(1600.0, 1500.0); }
	inline FVector2D EnemyDefaultSpawnPosition() { return FVector2D(1600.0, 3600.0); }

	// ════════════════════════════════════════════════════════════════════════
	// Utility
	// ════════════════════════════════════════════════════════════════════════

	/** Check if position is in the river area */
	inline bool IsInRiver(const FVector2D& Pos)
	{
		return Pos.Y >= RIVER_Y_MIN && Pos.Y <= RIVER_Y_MAX;
	}

	/** Check if position is on a bridge */
	inline bool IsOnBridge(const FVector2D& Pos)
	{
		if (!IsInRiver(Pos)) return false;
		const bool bOnLeft = Pos.X >= LEFT_BRIDGE_X_MIN && Pos.X <= LEFT_BRIDGE_X_MAX;
		const bool bOnRight = Pos.X >= RIGHT_BRIDGE_X_MIN && Pos.X <= RIGHT_BRIDGE_X_MAX;
		return bOnLeft || bOnRight;
	}

	/** Check if a ground unit can move to this position */
	inline bool CanGroundUnitMoveTo(const FVector2D& Pos)
	{
		if (IsInRiver(Pos) && !IsOnBridge(Pos)) return false;
		return true;
	}

	/** Check if position is within map bounds */
	inline bool IsWithinBounds(const FVector2D& Pos)
	{
		return Pos.X >= 0.0 && Pos.X <= MAP_WIDTH
			&& Pos.Y >= 0.0 && Pos.Y <= MAP_HEIGHT;
	}

	/** Clamp position to map bounds */
	inline FVector2D ClampToBounds(const FVector2D& Pos)
	{
		return FVector2D(
			FMath::Clamp(Pos.X, 0.0, static_cast<double>(MAP_WIDTH)),
			FMath::Clamp(Pos.Y, 0.0, static_cast<double>(MAP_HEIGHT))
		);
	}
}
