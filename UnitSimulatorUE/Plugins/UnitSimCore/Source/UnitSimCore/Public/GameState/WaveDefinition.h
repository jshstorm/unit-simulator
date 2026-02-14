#pragma once

#include "CoreMinimal.h"
#include "WaveDefinition.generated.h"

/**
 * Spawn group within a wave.
 * Ported from Contracts/WaveDefinition.cs
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FWaveSpawnGroup
{
	GENERATED_BODY()

	/** Unit ID to spawn */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName UnitId;

	/** Number of units to spawn */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Count = 1;

	/** Faction (friendly/enemy) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Faction = TEXT("enemy");

	/** Spawn start frame (relative to wave start) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SpawnFrame = 0;

	/** Spawn interval frames (for multiple units) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 SpawnInterval = 30;

	/** Spawn X position (-1 = random) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float SpawnX = -1.f;

	/** Spawn Y position (-1 = random) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float SpawnY = -1.f;

	/** Whether SpawnX is set (non-default) */
	bool HasSpawnX() const { return SpawnX >= 0.f; }

	/** Whether SpawnY is set (non-default) */
	bool HasSpawnY() const { return SpawnY >= 0.f; }
};

/**
 * Wave definition data.
 * Defines which units spawn at which timing.
 * Ported from Contracts/WaveDefinition.cs
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FWaveDefinition
{
	GENERATED_BODY()

	/** 1-based wave index */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 WaveNumber = 0;

	/** Wave name (display) */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Name;

	/** Delay frames before wave starts */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 DelayFrames = 0;

	/** Spawn groups in this wave */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FWaveSpawnGroup> SpawnGroups;

	/** Create an empty wave definition */
	static FWaveDefinition Empty(int32 InWaveNumber)
	{
		FWaveDefinition Def;
		Def.WaveNumber = InWaveNumber;
		Def.Name = FString::Printf(TEXT("Wave %d"), InWaveNumber);
		Def.DelayFrames = 0;
		return Def;
	}
};
