#include "Player/SimPlayerController.h"
#include "GameModes/SimGameMode.h"
#include "Simulation/SimulatorCore.h"
#include "Commands/SimulationCommands.h"
#include "GameConstants.h"
#include "Engine/World.h"

ASimPlayerController::ASimPlayerController()
{
	bShowMouseCursor = true;
	bEnableClickEvents = true;
	bEnableMouseOverEvents = true;
	PrimaryActorTick.bCanEverTick = true;
}

void ASimPlayerController::BeginPlay()
{
	Super::BeginPlay();
}

void ASimPlayerController::SetupInputComponent()
{
	Super::SetupInputComponent();

	if (!InputComponent)
	{
		return;
	}

	// Camera zoom
	InputComponent->BindAxis(TEXT("MouseWheelAxis"), this, &ASimPlayerController::HandleZoom);

	// Click actions
	InputComponent->BindAction(TEXT("LeftClick"), IE_Pressed, this, &ASimPlayerController::HandleLeftClick);
	InputComponent->BindAction(TEXT("RightClick"), IE_Pressed, this, &ASimPlayerController::HandleRightClick);

	// Box selection
	InputComponent->BindAction(TEXT("BoxSelect"), IE_Pressed, this, &ASimPlayerController::HandleBoxSelectStart);
	InputComponent->BindAction(TEXT("BoxSelect"), IE_Released, this, &ASimPlayerController::HandleBoxSelectEnd);
}

void ASimPlayerController::PlayerTick(float DeltaTime)
{
	Super::PlayerTick(DeltaTime);

	HandleCameraPan(DeltaTime);

	if (bEnableEdgeScrolling)
	{
		HandleEdgeScrolling(DeltaTime);
	}

	CameraInputX = 0.f;
	CameraInputY = 0.f;
}

// ════════════════════════════════════════════════════════════════════════════
// Selection
// ════════════════════════════════════════════════════════════════════════════

void ASimPlayerController::ClearSelection()
{
	SelectedUnitIds.Empty();
	OnSelectionChanged.Broadcast(SelectedUnitIds);
}

void ASimPlayerController::SelectUnit(int32 UnitId)
{
	SelectedUnitIds.Empty();
	SelectedUnitIds.Add(UnitId);
	OnSelectionChanged.Broadcast(SelectedUnitIds);
}

// ════════════════════════════════════════════════════════════════════════════
// Commands
// ════════════════════════════════════════════════════════════════════════════

void ASimPlayerController::IssueMoveCommand(const FVector2D& Destination)
{
	ASimGameMode* GM = GetSimGameMode();
	if (!GM || !GM->GetSimulatorCore())
	{
		return;
	}

	FSimulatorCore* Sim = GM->GetSimulatorCore();
	const int32 CurrentFrame = Sim->GetCurrentFrame();

	for (int32 UnitId : SelectedUnitIds)
	{
		FMoveUnitCommand MoveCmd;
		MoveCmd.FrameNumber = CurrentFrame;
		MoveCmd.UnitId = UnitId;
		MoveCmd.Faction = EUnitFaction::Friendly;
		MoveCmd.Destination = Destination;

		Sim->EnqueueCommand(FSimCommandWrapper::MakeMove(MoveCmd));
	}
}

void ASimPlayerController::IssueSpawnCommand(const FVector2D& Position, FName UnitId)
{
	ASimGameMode* GM = GetSimGameMode();
	if (!GM || !GM->GetSimulatorCore())
	{
		return;
	}

	FSimulatorCore* Sim = GM->GetSimulatorCore();

	FSpawnUnitCommand SpawnCmd;
	SpawnCmd.FrameNumber = Sim->GetCurrentFrame();
	SpawnCmd.Position = Position;
	SpawnCmd.Faction = EUnitFaction::Friendly;

	Sim->EnqueueCommand(FSimCommandWrapper::MakeSpawn(SpawnCmd));
}

// ════════════════════════════════════════════════════════════════════════════
// Camera
// ════════════════════════════════════════════════════════════════════════════

void ASimPlayerController::HandleCameraPan(float DeltaTime)
{
	APawn* ControlledPawn = GetPawn();
	if (!ControlledPawn)
	{
		return;
	}

	// Read WASD input
	float MoveX = 0.f;
	float MoveY = 0.f;

	if (IsInputKeyDown(EKeys::W) || IsInputKeyDown(EKeys::Up))
	{
		MoveY += 1.f;
	}
	if (IsInputKeyDown(EKeys::S) || IsInputKeyDown(EKeys::Down))
	{
		MoveY -= 1.f;
	}
	if (IsInputKeyDown(EKeys::D) || IsInputKeyDown(EKeys::Right))
	{
		MoveX += 1.f;
	}
	if (IsInputKeyDown(EKeys::A) || IsInputKeyDown(EKeys::Left))
	{
		MoveX -= 1.f;
	}

	if (FMath::Abs(MoveX) > KINDA_SMALL_NUMBER || FMath::Abs(MoveY) > KINDA_SMALL_NUMBER)
	{
		FVector Movement(MoveX, MoveY, 0.f);
		Movement.Normalize();
		Movement *= CameraPanSpeed * DeltaTime;

		FVector NewLocation = ControlledPawn->GetActorLocation() + Movement;
		ControlledPawn->SetActorLocation(NewLocation);
	}
}

void ASimPlayerController::HandleEdgeScrolling(float DeltaTime)
{
	APawn* ControlledPawn = GetPawn();
	if (!ControlledPawn)
	{
		return;
	}

	float MouseX = 0.f;
	float MouseY = 0.f;
	if (!GetMousePosition(MouseX, MouseY))
	{
		return;
	}

	int32 ViewportSizeX = 0;
	int32 ViewportSizeY = 0;
	GetViewportSize(ViewportSizeX, ViewportSizeY);

	FVector EdgeMovement = FVector::ZeroVector;

	if (MouseX < EdgeScrollMargin)
	{
		EdgeMovement.X -= 1.f;
	}
	else if (MouseX > ViewportSizeX - EdgeScrollMargin)
	{
		EdgeMovement.X += 1.f;
	}

	if (MouseY < EdgeScrollMargin)
	{
		EdgeMovement.Y += 1.f;
	}
	else if (MouseY > ViewportSizeY - EdgeScrollMargin)
	{
		EdgeMovement.Y -= 1.f;
	}

	if (!EdgeMovement.IsNearlyZero())
	{
		EdgeMovement.Normalize();
		EdgeMovement *= CameraPanSpeed * DeltaTime;

		FVector NewLocation = ControlledPawn->GetActorLocation() + EdgeMovement;
		ControlledPawn->SetActorLocation(NewLocation);
	}
}

void ASimPlayerController::HandleZoom(float AxisValue)
{
	if (FMath::IsNearlyZero(AxisValue))
	{
		return;
	}

	APawn* ControlledPawn = GetPawn();
	if (!ControlledPawn)
	{
		return;
	}

	FVector Location = ControlledPawn->GetActorLocation();
	Location.Z -= AxisValue * CameraZoomSpeed;
	Location.Z = FMath::Clamp(Location.Z, CameraMinHeight, CameraMaxHeight);
	ControlledPawn->SetActorLocation(Location);
}

// ════════════════════════════════════════════════════════════════════════════
// Click Handlers
// ════════════════════════════════════════════════════════════════════════════

void ASimPlayerController::HandleLeftClick()
{
	FVector2D SimPos;
	if (!GetMouseSimPosition(SimPos))
	{
		return;
	}

	int32 FoundUnitId = FindUnitAtPosition(SimPos);

	if (FoundUnitId >= 0)
	{
		// Check if Shift is held for multi-select
		if (IsInputKeyDown(EKeys::LeftShift) || IsInputKeyDown(EKeys::RightShift))
		{
			if (SelectedUnitIds.Contains(FoundUnitId))
			{
				SelectedUnitIds.Remove(FoundUnitId);
			}
			else
			{
				SelectedUnitIds.AddUnique(FoundUnitId);
			}
		}
		else
		{
			SelectedUnitIds.Empty();
			SelectedUnitIds.Add(FoundUnitId);
		}

		OnSelectionChanged.Broadcast(SelectedUnitIds);
	}
	else if (!IsInputKeyDown(EKeys::LeftShift) && !IsInputKeyDown(EKeys::RightShift))
	{
		ClearSelection();
	}
}

void ASimPlayerController::HandleRightClick()
{
	if (SelectedUnitIds.Num() == 0)
	{
		return;
	}

	FVector2D SimPos;
	if (!GetMouseSimPosition(SimPos))
	{
		return;
	}

	IssueMoveCommand(SimPos);
}

void ASimPlayerController::HandleBoxSelectStart()
{
	FVector2D SimPos;
	if (GetMouseSimPosition(SimPos))
	{
		bIsBoxSelecting = true;
		BoxSelectStart = SimPos;
	}
}

void ASimPlayerController::HandleBoxSelectEnd()
{
	if (!bIsBoxSelecting)
	{
		return;
	}
	bIsBoxSelecting = false;

	FVector2D SimPos;
	if (!GetMouseSimPosition(SimPos))
	{
		return;
	}

	// Compute selection box bounds
	FVector2D BoxMin(
		FMath::Min(BoxSelectStart.X, SimPos.X),
		FMath::Min(BoxSelectStart.Y, SimPos.Y));
	FVector2D BoxMax(
		FMath::Max(BoxSelectStart.X, SimPos.X),
		FMath::Max(BoxSelectStart.Y, SimPos.Y));

	// Find all friendly units within the box
	ASimGameMode* GM = GetSimGameMode();
	if (!GM || !GM->GetSimulatorCore())
	{
		return;
	}

	SelectedUnitIds.Empty();

	const TArray<FUnit>& Friendlies = GM->GetSimulatorCore()->GetFriendlyUnits();
	for (const FUnit& Unit : Friendlies)
	{
		if (Unit.bIsDead)
		{
			continue;
		}

		if (Unit.Position.X >= BoxMin.X && Unit.Position.X <= BoxMax.X &&
			Unit.Position.Y >= BoxMin.Y && Unit.Position.Y <= BoxMax.Y)
		{
			SelectedUnitIds.Add(Unit.Id);
		}
	}

	OnSelectionChanged.Broadcast(SelectedUnitIds);
}

// ════════════════════════════════════════════════════════════════════════════
// Selection Helpers
// ════════════════════════════════════════════════════════════════════════════

int32 ASimPlayerController::FindUnitAtPosition(const FVector2D& SimPosition) const
{
	ASimGameMode* GM = GetSimGameMode();
	if (!GM || !GM->GetSimulatorCore())
	{
		return -1;
	}

	float BestDistSq = SelectionRadius * SelectionRadius;
	int32 BestUnitId = -1;

	const TArray<FUnit>& Friendlies = GM->GetSimulatorCore()->GetFriendlyUnits();
	for (const FUnit& Unit : Friendlies)
	{
		if (Unit.bIsDead)
		{
			continue;
		}

		float DistSq = FVector2D::DistSquared(SimPosition, Unit.Position);
		if (DistSq < BestDistSq)
		{
			BestDistSq = DistSq;
			BestUnitId = Unit.Id;
		}
	}

	return BestUnitId;
}

bool ASimPlayerController::GetMouseSimPosition(FVector2D& OutSimPosition) const
{
	// Project mouse cursor onto the ground plane (Z=0)
	FVector WorldLocation;
	FVector WorldDirection;

	if (!DeprojectMousePositionToWorld(WorldLocation, WorldDirection))
	{
		return false;
	}

	// Ray-plane intersection with Z=0
	if (FMath::IsNearlyZero(WorldDirection.Z))
	{
		return false;
	}

	float T = -WorldLocation.Z / WorldDirection.Z;
	if (T < 0.f)
	{
		return false;
	}

	FVector HitPoint = WorldLocation + WorldDirection * T;
	OutSimPosition = FVector2D(HitPoint.X, HitPoint.Y);
	return true;
}

// ════════════════════════════════════════════════════════════════════════════
// Helpers
// ════════════════════════════════════════════════════════════════════════════

ASimGameMode* ASimPlayerController::GetSimGameMode() const
{
	if (CachedGameMode.IsValid())
	{
		return CachedGameMode.Get();
	}

	UWorld* World = GetWorld();
	if (!World)
	{
		return nullptr;
	}

	ASimGameMode* GM = Cast<ASimGameMode>(World->GetAuthGameMode());
	CachedGameMode = GM;
	return GM;
}
