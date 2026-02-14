#pragma once

#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "SimPlayerController.generated.h"

class ASimGameMode;
class UInputAction;
class UInputMappingContext;

/**
 * RTS-style player controller for the unit simulator.
 *
 * Responsibilities:
 * - Camera control: WASD pan, mouse wheel zoom, edge scrolling
 * - Unit selection: click select, box selection
 * - Unit commands: right-click move, spawn commands
 * - Converts player input into ISimulationCommand and enqueues into SimulatorCore
 */
UCLASS()
class UNITSIMGAME_API ASimPlayerController : public APlayerController
{
	GENERATED_BODY()

public:
	ASimPlayerController();

	virtual void BeginPlay() override;
	virtual void SetupInputComponent() override;
	virtual void PlayerTick(float DeltaTime) override;

	// ════════════════════════════════════════════════════════════════════════
	// Selection
	// ════════════════════════════════════════════════════════════════════════

	/** Get IDs of currently selected units */
	UFUNCTION(BlueprintPure, Category = "UnitSim|Selection")
	const TArray<int32>& GetSelectedUnitIds() const { return SelectedUnitIds; }

	/** Check if a specific unit is selected */
	UFUNCTION(BlueprintPure, Category = "UnitSim|Selection")
	bool IsUnitSelected(int32 UnitId) const { return SelectedUnitIds.Contains(UnitId); }

	/** Clear selection */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Selection")
	void ClearSelection();

	/** Select a specific unit by ID */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Selection")
	void SelectUnit(int32 UnitId);

	// ════════════════════════════════════════════════════════════════════════
	// Commands
	// ════════════════════════════════════════════════════════════════════════

	/** Issue a move command for selected units to a world position */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Commands")
	void IssueMoveCommand(const FVector2D& Destination);

	/** Issue a spawn command at a world position */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Commands")
	void IssueSpawnCommand(const FVector2D& Position, FName UnitId);

	// ════════════════════════════════════════════════════════════════════════
	// Camera Configuration
	// ════════════════════════════════════════════════════════════════════════

	/** Camera pan speed (units per second) */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|Camera")
	float CameraPanSpeed = 1000.f;

	/** Camera zoom speed (units per scroll tick) */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|Camera")
	float CameraZoomSpeed = 200.f;

	/** Minimum camera height (zoom in limit) */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|Camera")
	float CameraMinHeight = 500.f;

	/** Maximum camera height (zoom out limit) */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|Camera")
	float CameraMaxHeight = 5000.f;

	/** Screen edge margin for edge scrolling (pixels) */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|Camera")
	float EdgeScrollMargin = 20.f;

	/** Whether edge scrolling is enabled */
	UPROPERTY(EditDefaultsOnly, Category = "UnitSim|Camera")
	bool bEnableEdgeScrolling = true;

	// ════════════════════════════════════════════════════════════════════════
	// Selection Events
	// ════════════════════════════════════════════════════════════════════════

	DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnSelectionChanged, const TArray<int32>&, SelectedIds);

	UPROPERTY(BlueprintAssignable, Category = "UnitSim|Events")
	FOnSelectionChanged OnSelectionChanged;

protected:
	/** Get the game mode (cached) */
	ASimGameMode* GetSimGameMode() const;

	// ════════════════════════════════════════════════════════════════════════
	// Input Handlers
	// ════════════════════════════════════════════════════════════════════════

	/** Handle camera pan from WASD / arrow keys */
	void HandleCameraPan(float DeltaTime);

	/** Handle edge scrolling */
	void HandleEdgeScrolling(float DeltaTime);

	/** Handle mouse scroll for zoom */
	void HandleZoom(float AxisValue);

	/** Handle left click (select unit) */
	void HandleLeftClick();

	/** Handle right click (issue command) */
	void HandleRightClick();

	/** Handle box selection start/end */
	void HandleBoxSelectStart();
	void HandleBoxSelectEnd();

	// ════════════════════════════════════════════════════════════════════════
	// Selection Helpers
	// ════════════════════════════════════════════════════════════════════════

	/**
	 * Find the unit closest to a 2D simulation-space position.
	 * Returns the unit ID, or -1 if none within SelectionRadius.
	 */
	int32 FindUnitAtPosition(const FVector2D& SimPosition) const;

	/** Convert mouse screen position to simulation 2D coordinates */
	bool GetMouseSimPosition(FVector2D& OutSimPosition) const;

private:
	/** Cached game mode pointer */
	mutable TWeakObjectPtr<ASimGameMode> CachedGameMode;

	/** Currently selected unit IDs */
	TArray<int32> SelectedUnitIds;

	/** Box selection state */
	bool bIsBoxSelecting = false;
	FVector2D BoxSelectStart = FVector2D::ZeroVector;

	/** Camera movement input axes (accumulated per frame) */
	float CameraInputX = 0.f;
	float CameraInputY = 0.f;

	/** Selection click radius in simulation units */
	static constexpr float SelectionRadius = 40.f;
};
