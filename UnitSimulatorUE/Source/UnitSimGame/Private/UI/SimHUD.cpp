#include "UI/SimHUD.h"
#include "Debug/SimDebugDrawer.h"
#include "GameModes/SimGameMode.h"
#include "Player/SimPlayerController.h"
#include "Simulation/SimulatorCore.h"
#include "Simulation/FrameData.h"
#include "Units/Unit.h"
#include "Engine/Canvas.h"
#include "Engine/Font.h"

ASimHUD::ASimHUD()
{
}

void ASimHUD::BeginPlay()
{
	Super::BeginPlay();

	DebugDrawer = NewObject<USimDebugDrawer>(this);
}

void ASimHUD::DrawHUD()
{
	Super::DrawHUD();

	// Draw debug world-space overlays
	if (DebugDrawer && DebugDrawer->IsEnabled())
	{
		ASimGameMode* GM = GetSimGameMode();
		if (GM && GM->GetSimulatorCore())
		{
			DebugDrawer->DrawAll(GetWorld(), GM->GetSimulatorCore());
		}
	}

	// Draw HUD overlays
	if (bShowSimInfo)
	{
		DrawSimulationInfo();
	}

	DrawSpeedIndicator();

	if (bShowSelectedUnitInfo)
	{
		DrawSelectedUnitInfo();
	}
}

// ════════════════════════════════════════════════════════════════════════════
// Debug Toggle
// ════════════════════════════════════════════════════════════════════════════

void ASimHUD::ToggleDebugDraw()
{
	if (DebugDrawer)
	{
		DebugDrawer->ToggleEnabled();
	}
}

// ════════════════════════════════════════════════════════════════════════════
// Simulation Info (Top-Left)
// ════════════════════════════════════════════════════════════════════════════

void ASimHUD::DrawSimulationInfo()
{
	ASimGameMode* GM = GetSimGameMode();
	if (!GM)
	{
		return;
	}

	float Y = ScreenMargin;
	const float X = ScreenMargin;

	// Status line
	FString StatusText;
	if (!GM->IsSimulationInitialized())
	{
		StatusText = TEXT("NOT INITIALIZED");
	}
	else if (GM->IsSimulationPaused())
	{
		StatusText = TEXT("PAUSED");
	}
	else if (GM->IsSimulationRunning())
	{
		StatusText = TEXT("RUNNING");
	}
	else
	{
		StatusText = TEXT("STOPPED");
	}

	DrawTextWithBackground(FString::Printf(TEXT("Sim: %s"), *StatusText), X, Y);
	Y += LineHeight;

	if (!GM->IsSimulationInitialized())
	{
		return;
	}

	// Frame info
	FFrameData FrameData = GM->GetCurrentFrameData();

	DrawTextWithBackground(FString::Printf(TEXT("Frame: %d"), FrameData.FrameNumber), X, Y);
	Y += LineHeight;

	DrawTextWithBackground(FString::Printf(TEXT("Wave: %d"), FrameData.CurrentWave), X, Y);
	Y += LineHeight;

	// Unit counts
	DrawTextWithBackground(
		FString::Printf(TEXT("Friendly: %d  Enemy: %d"),
			FrameData.LivingFriendlyCount, FrameData.LivingEnemyCount),
		X, Y);
	Y += LineHeight;

	// Crowns
	DrawTextWithBackground(
		FString::Printf(TEXT("Crowns: %d - %d"),
			FrameData.FriendlyCrowns, FrameData.EnemyCrowns),
		X, Y);
	Y += LineHeight;

	// Game result
	if (FrameData.GameResult != EGameResult::InProgress)
	{
		FString ResultText;
		switch (FrameData.GameResult)
		{
		case EGameResult::FriendlyWin: ResultText = TEXT("WIN"); break;
		case EGameResult::EnemyWin:    ResultText = TEXT("LOSS"); break;
		case EGameResult::Draw:        ResultText = TEXT("DRAW"); break;
		default: ResultText = TEXT("UNKNOWN"); break;
		}

		DrawTextWithBackground(FString::Printf(TEXT("Result: %s"), *ResultText), X, Y);
		Y += LineHeight;
	}

	// Debug drawer status
	if (DebugDrawer && DebugDrawer->IsEnabled())
	{
		DrawTextWithBackground(TEXT("Debug: ON"), X, Y);
	}
}

// ════════════════════════════════════════════════════════════════════════════
// Speed Indicator (Top-Right)
// ════════════════════════════════════════════════════════════════════════════

void ASimHUD::DrawSpeedIndicator()
{
	ASimGameMode* GM = GetSimGameMode();
	if (!GM || !GM->IsSimulationRunning())
	{
		return;
	}

	float Speed = GM->GetSimulationSpeed();
	FString SpeedText = FString::Printf(TEXT("x%.1f"), Speed);

	float TextWidth = SpeedText.Len() * 8.f;
	float X = Canvas->SizeX - TextWidth - ScreenMargin;
	float Y = ScreenMargin;

	DrawTextWithBackground(SpeedText, X, Y);
}

// ════════════════════════════════════════════════════════════════════════════
// Selected Unit Info (Bottom-Left)
// ════════════════════════════════════════════════════════════════════════════

void ASimHUD::DrawSelectedUnitInfo()
{
	ASimPlayerController* PC = Cast<ASimPlayerController>(GetOwningPlayerController());
	if (!PC)
	{
		return;
	}

	const TArray<int32>& SelectedIds = PC->GetSelectedUnitIds();
	if (SelectedIds.Num() == 0)
	{
		return;
	}

	ASimGameMode* GM = GetSimGameMode();
	if (!GM || !GM->GetSimulatorCore())
	{
		return;
	}

	const FSimulatorCore* Sim = GM->GetSimulatorCore();
	const TArray<FUnit>& Friendlies = Sim->GetFriendlyUnits();

	float Y = Canvas->SizeY - ScreenMargin - LineHeight * 6;
	const float X = ScreenMargin;

	if (SelectedIds.Num() == 1)
	{
		// Single unit details
		int32 TargetId = SelectedIds[0];
		const FUnit* FoundUnit = nullptr;

		for (const FUnit& Unit : Friendlies)
		{
			if (Unit.Id == TargetId)
			{
				FoundUnit = &Unit;
				break;
			}
		}

		if (!FoundUnit)
		{
			return;
		}

		DrawTextWithBackground(TEXT("--- Selected Unit ---"), X, Y);
		Y += LineHeight;

		DrawTextWithBackground(FString::Printf(TEXT("ID: %d  %s"), FoundUnit->Id, *FoundUnit->GetLabel()), X, Y);
		Y += LineHeight;

		DrawTextWithBackground(FString::Printf(TEXT("HP: %d  DMG: %d"), FoundUnit->HP, FoundUnit->Damage), X, Y);
		Y += LineHeight;

		DrawTextWithBackground(
			FString::Printf(TEXT("Pos: (%.0f, %.0f)"), FoundUnit->Position.X, FoundUnit->Position.Y), X, Y);
		Y += LineHeight;

		FString StateText = FoundUnit->bIsDead ? TEXT("DEAD") :
			(FoundUnit->TargetIndex >= 0 ? TEXT("IN COMBAT") : TEXT("MOVING"));
		DrawTextWithBackground(FString::Printf(TEXT("State: %s"), *StateText), X, Y);
	}
	else
	{
		// Multi-selection summary
		DrawTextWithBackground(
			FString::Printf(TEXT("Selected: %d units"), SelectedIds.Num()), X, Y);
	}
}

// ════════════════════════════════════════════════════════════════════════════
// Helpers
// ════════════════════════════════════════════════════════════════════════════

void ASimHUD::DrawTextWithBackground(const FString& Text, float X, float Y, float Scale)
{
	if (!Canvas)
	{
		return;
	}

	// Draw background
	float TextWidth = Text.Len() * 7.f * Scale;
	float TextHeight = LineHeight * Scale;

	FCanvasTileItem Background(
		FVector2D(X - 2.f, Y - 1.f),
		FVector2D(TextWidth + 4.f, TextHeight + 2.f),
		FLinearColor(BackgroundColor));
	Canvas->DrawItem(Background);

	// Draw text
	FCanvasTextItem TextItem(
		FVector2D(X, Y),
		FText::FromString(Text),
		GEngine->GetSmallFont(),
		TextColor);
	TextItem.Scale = FVector2D(Scale, Scale);
	Canvas->DrawItem(TextItem);
}

ASimGameMode* ASimHUD::GetSimGameMode()
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
