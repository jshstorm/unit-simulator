#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "SimHUD.generated.h"

class ASimGameMode;
class USimDebugDrawer;
class ASimPlayerController;

/**
 * HUD for the unit simulation.
 *
 * Displays:
 * - Frame number, living unit counts, wave info
 * - Selected unit information panel
 * - Simulation speed indicator
 * - Debug visualization toggle
 *
 * Uses Canvas drawing for overlay text and integrates with
 * USimDebugDrawer for world-space debug rendering.
 */
UCLASS()
class UNITSIMGAME_API ASimHUD : public AHUD
{
	GENERATED_BODY()

public:
	ASimHUD();

	virtual void BeginPlay() override;
	virtual void DrawHUD() override;

	// ════════════════════════════════════════════════════════════════════════
	// Debug Drawer Access
	// ════════════════════════════════════════════════════════════════════════

	/** Get the debug drawer instance */
	UFUNCTION(BlueprintPure, Category = "UnitSim|Debug")
	USimDebugDrawer* GetDebugDrawer() const { return DebugDrawer; }

	/** Toggle debug visualization */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Debug")
	void ToggleDebugDraw();

	// ════════════════════════════════════════════════════════════════════════
	// Display Configuration
	// ════════════════════════════════════════════════════════════════════════

	/** Whether to show the simulation info overlay */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|UI")
	bool bShowSimInfo = true;

	/** Whether to show selected unit details */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|UI")
	bool bShowSelectedUnitInfo = true;

	/** Overlay text color */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|UI")
	FLinearColor TextColor = FLinearColor::White;

	/** Overlay background color (semi-transparent) */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|UI")
	FLinearColor BackgroundColor = FLinearColor(0.f, 0.f, 0.f, 0.5f);

protected:
	/** Draw simulation status overlay (top-left corner) */
	void DrawSimulationInfo();

	/** Draw selected unit details panel (bottom-left corner) */
	void DrawSelectedUnitInfo();

	/** Draw simulation speed indicator */
	void DrawSpeedIndicator();

	/** Helper: draw text with background */
	void DrawTextWithBackground(const FString& Text, float X, float Y, float Scale = 1.f);

private:
	/** Debug drawer (created as UObject subobject) */
	UPROPERTY()
	USimDebugDrawer* DebugDrawer = nullptr;

	/** Cached game mode */
	TWeakObjectPtr<ASimGameMode> CachedGameMode;

	/** Get cached game mode */
	ASimGameMode* GetSimGameMode();

	/** Margin from screen edges */
	static constexpr float ScreenMargin = 10.f;

	/** Line height for overlay text */
	static constexpr float LineHeight = 18.f;
};
