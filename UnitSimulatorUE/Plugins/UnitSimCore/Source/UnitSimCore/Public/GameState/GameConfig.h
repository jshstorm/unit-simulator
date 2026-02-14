#pragma once

#include "CoreMinimal.h"
#include "GameConstants.h"
#include "GameConfig.generated.h"

/**
 * Configuration for initializing a simulation instance.
 * Ported from Contracts/GameConfig.cs
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FGameConfig
{
	GENERATED_BODY()

	/** Map width used by the simulation */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MapWidth = UnitSimConstants::SIMULATION_WIDTH;

	/** Map height used by the simulation */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MapHeight = UnitSimConstants::SIMULATION_HEIGHT;

	/** Maximum number of frames to simulate */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MaxFrames = UnitSimConstants::MAX_FRAMES;

	/** Optional random seed for deterministic runs (-1 = random) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 RandomSeed = -1;

	/** Initial wave number to start from */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 InitialWave = 0;

	/** Whether more waves are expected at initialization time */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bHasMoreWaves = true;

	/** Whether RandomSeed is set (non-default) */
	bool HasRandomSeed() const { return RandomSeed >= 0; }
};
