#include "Terrain/TerrainSystem.h"
#include "Terrain/MapLayout.h"
#include "Units/Unit.h"

namespace
{
	const FVector2D LeftBridgeCenter(
		(MapLayout::LEFT_BRIDGE_X_MIN + MapLayout::LEFT_BRIDGE_X_MAX) / 2.f,
		(MapLayout::RIVER_Y_MIN + MapLayout::RIVER_Y_MAX) / 2.f);

	const FVector2D RightBridgeCenter(
		(MapLayout::RIGHT_BRIDGE_X_MIN + MapLayout::RIGHT_BRIDGE_X_MAX) / 2.f,
		(MapLayout::RIVER_Y_MIN + MapLayout::RIVER_Y_MAX) / 2.f);
}

bool FTerrainSystem::CanMoveTo(const FUnit& Unit, const FVector2D& Position) const
{
	if (Unit.Layer == EMovementLayer::Air)
	{
		return MapLayout::IsWithinBounds(Position);
	}

	return MapLayout::IsWithinBounds(Position) && MapLayout::CanGroundUnitMoveTo(Position);
}

FVector2D FTerrainSystem::GetAdjustedDestination(const FUnit& Unit, const FVector2D& Destination) const
{
	if (Unit.Layer == EMovementLayer::Air)
	{
		return MapLayout::ClampToBounds(Destination);
	}

	if (!IsCrossingRiver(Unit.Position, Destination))
	{
		return MapLayout::ClampToBounds(Destination);
	}

	if (MapLayout::IsOnBridge(Unit.Position) || MapLayout::IsOnBridge(Destination))
	{
		return MapLayout::ClampToBounds(Destination);
	}

	return GetNearestBridgeCenter(Unit.Position);
}

bool FTerrainSystem::IsCrossingRiver(const FVector2D& From, const FVector2D& To)
{
	const bool bFromLower = From.Y < MapLayout::RIVER_Y_MIN;
	const bool bFromUpper = From.Y > MapLayout::RIVER_Y_MAX;
	const bool bToLower = To.Y < MapLayout::RIVER_Y_MIN;
	const bool bToUpper = To.Y > MapLayout::RIVER_Y_MAX;

	return (bFromLower && bToUpper) || (bFromUpper && bToLower);
}

FVector2D FTerrainSystem::GetNearestBridgeCenter(const FVector2D& Position)
{
	const float LeftDistance = FVector2D::Distance(Position, LeftBridgeCenter);
	const float RightDistance = FVector2D::Distance(Position, RightBridgeCenter);
	return LeftDistance <= RightDistance ? LeftBridgeCenter : RightBridgeCenter;
}
