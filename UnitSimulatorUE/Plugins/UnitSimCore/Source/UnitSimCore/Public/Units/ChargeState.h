#pragma once

#include "CoreMinimal.h"
#include "ChargeState.generated.h"

/**
 * Tracks a unit's charge attack state.
 * Ported from Abilities/ChargeState.cs
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FChargeState
{
	GENERATED_BODY()

	/** Whether currently charging */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsCharging = false;

	/** Whether charge is complete (required distance traveled) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsCharged = false;

	/** Position where charge started */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D ChargeStartPosition = FVector2D::ZeroVector;

	/** Distance traveled since charge start */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float ChargedDistance = 0.f;

	/** Distance required to complete the charge */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float RequiredDistance = 0.f;

	/** Reset all charge state */
	void Reset()
	{
		bIsCharging = false;
		bIsCharged = false;
		ChargeStartPosition = FVector2D::ZeroVector;
		ChargedDistance = 0.f;
	}

	/** Start a charge from the given position */
	void StartCharge(const FVector2D& Position, float InRequiredDistance)
	{
		bIsCharging = true;
		bIsCharged = false;
		ChargeStartPosition = Position;
		ChargedDistance = 0.f;
		RequiredDistance = InRequiredDistance;
	}

	/** Update charge distance from current position */
	void UpdateChargeDistance(const FVector2D& CurrentPosition)
	{
		if (!bIsCharging) return;

		ChargedDistance = FVector2D::Distance(ChargeStartPosition, CurrentPosition);
		if (ChargedDistance >= RequiredDistance)
		{
			bIsCharged = true;
		}
	}

	/** Consume the charge after attacking (resets state) */
	void ConsumeCharge()
	{
		Reset();
	}
};
