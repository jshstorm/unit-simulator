#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"

struct FUnit;

/**
 * River/bridge movement constraints and destination adjustment.
 * Ported from Terrain/TerrainSystem.cs (61 lines)
 */
class UNITSIMCORE_API FTerrainSystem
{
public:
	/** Check if a unit can move to the given position */
	bool CanMoveTo(const FUnit& Unit, const FVector2D& Position) const;

	/** Adjust destination for ground units (route through bridge if crossing river) */
	FVector2D GetAdjustedDestination(const FUnit& Unit, const FVector2D& Destination) const;

private:
	static bool IsCrossingRiver(const FVector2D& From, const FVector2D& To);
	static FVector2D GetNearestBridgeCenter(const FVector2D& Position);
};
