#pragma once

#include "CoreMinimal.h"
#include "SimDebugDrawer.generated.h"

class FSimulatorCore;
struct FFrameData;

/**
 * Debug visualization utility for the unit simulation.
 *
 * Draws debug information using UE DrawDebug* functions:
 * - Unit positions with HP bars and faction coloring
 * - A* pathfinding paths as line segments
 * - Tower positions and attack ranges
 * - Pathfinding grid walkability overlay
 *
 * All drawing is toggleable via console command or key binding.
 * Uses simulation 2D coordinates mapped to UE world space (X, Y plane at Z=0).
 */
UCLASS(BlueprintType)
class UNITSIMGAME_API USimDebugDrawer : public UObject
{
	GENERATED_BODY()

public:
	// ════════════════════════════════════════════════════════════════════════
	// Master Toggle
	// ════════════════════════════════════════════════════════════════════════

	/** Enable/disable all debug drawing */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Debug")
	void SetEnabled(bool bInEnabled) { bEnabled = bInEnabled; }

	UFUNCTION(BlueprintPure, Category = "UnitSim|Debug")
	bool IsEnabled() const { return bEnabled; }

	/** Toggle debug drawing on/off */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Debug")
	void ToggleEnabled() { bEnabled = !bEnabled; }

	// ════════════════════════════════════════════════════════════════════════
	// Individual Layer Toggles
	// ════════════════════════════════════════════════════════════════════════

	UFUNCTION(BlueprintCallable, Category = "UnitSim|Debug")
	void SetDrawUnits(bool bDraw) { bDrawUnits = bDraw; }

	UFUNCTION(BlueprintCallable, Category = "UnitSim|Debug")
	void SetDrawPaths(bool bDraw) { bDrawPaths = bDraw; }

	UFUNCTION(BlueprintCallable, Category = "UnitSim|Debug")
	void SetDrawTowers(bool bDraw) { bDrawTowers = bDraw; }

	UFUNCTION(BlueprintCallable, Category = "UnitSim|Debug")
	void SetDrawGrid(bool bDraw) { bDrawGrid = bDraw; }

	// ════════════════════════════════════════════════════════════════════════
	// Draw Methods
	// ════════════════════════════════════════════════════════════════════════

	/**
	 * Draw all enabled debug layers.
	 * Call this each frame from the GameMode or HUD.
	 */
	/** Draw all enabled debug layers. Call this each frame from the GameMode or HUD. */
	void DrawAll(const UWorld* World, const FSimulatorCore* Simulator);

	/** Draw unit positions with HP and status indicators */
	void DrawDebugUnits(const UWorld* World, const FSimulatorCore* Simulator);

	/** Draw A* pathfinding paths as line segments */
	void DrawDebugPaths(const UWorld* World, const FSimulatorCore* Simulator);

	/** Draw tower positions and attack ranges */
	void DrawDebugTowers(const UWorld* World, const FSimulatorCore* Simulator);

	/** Draw pathfinding grid walkability overlay */
	void DrawDebugGrid(const UWorld* World, const FSimulatorCore* Simulator);

	// ════════════════════════════════════════════════════════════════════════
	// Configuration
	// ════════════════════════════════════════════════════════════════════════

	/** Height offset for debug drawing above ground plane */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UnitSim|Debug")
	float DrawHeight = 5.f;

	/** Size of HP text on units */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UnitSim|Debug")
	float TextScale = 1.f;

	/** Line thickness for path and grid drawing */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UnitSim|Debug")
	float LineThickness = 2.f;

private:
	/** Convert 2D simulation position to 3D world position */
	FVector SimToWorld(const FVector2D& SimPos) const;

	bool bEnabled = false;
	bool bDrawUnits = true;
	bool bDrawPaths = true;
	bool bDrawTowers = true;
	bool bDrawGrid = false;
};
