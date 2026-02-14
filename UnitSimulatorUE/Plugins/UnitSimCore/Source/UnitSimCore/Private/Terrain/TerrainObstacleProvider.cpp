#include "Terrain/TerrainObstacleProvider.h"
#include "Terrain/MapLayout.h"
#include "GameConstants.h"

TArray<FObstacleRect> FTerrainObstacleProvider::GetUnwalkableRects() const
{
	TArray<FObstacleRect> Rects;
	const float Margin = UnitSimConstants::RIVER_OBSTACLE_MARGIN;

	// 1. Left river area (X: 0 ~ LeftBridgeXMin)
	{
		FObstacleRect Rect;
		Rect.Min = FVector2D(0.0, MapLayout::RIVER_Y_MIN + Margin);
		Rect.Max = FVector2D(MapLayout::LEFT_BRIDGE_X_MIN - Margin, MapLayout::RIVER_Y_MAX - Margin);
		Rects.Add(Rect);
	}

	// 2. Center river area (X: LeftBridgeXMax ~ RightBridgeXMin)
	{
		FObstacleRect Rect;
		Rect.Min = FVector2D(MapLayout::LEFT_BRIDGE_X_MAX + Margin, MapLayout::RIVER_Y_MIN + Margin);
		Rect.Max = FVector2D(MapLayout::RIGHT_BRIDGE_X_MIN - Margin, MapLayout::RIVER_Y_MAX - Margin);
		Rects.Add(Rect);
	}

	// 3. Right river area (X: RightBridgeXMax ~ MapWidth)
	{
		FObstacleRect Rect;
		Rect.Min = FVector2D(MapLayout::RIGHT_BRIDGE_X_MAX + Margin, MapLayout::RIVER_Y_MIN + Margin);
		Rect.Max = FVector2D(MapLayout::MAP_WIDTH, MapLayout::RIVER_Y_MAX - Margin);
		Rects.Add(Rect);
	}

	return Rects;
}

TArray<FObstacleCircle> FTerrainObstacleProvider::GetUnwalkableCircles() const
{
	// Terrain has no circular obstacles
	return TArray<FObstacleCircle>();
}
