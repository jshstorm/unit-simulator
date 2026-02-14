#pragma once

#include "CoreMinimal.h"
#include "Units/UnitStats.h"
#include "Abilities/AbilityTypes.h"
#include "Towers/TowerStats.h"
#include "GameState/WaveDefinition.h"
#include "Simulation/GameBalance.h"
#include "JsonDataLoader.generated.h"

/**
 * All game data loaded from JSON files.
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FGameData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TMap<FName, FUnitStats> Units;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TMap<FName, FAbilityData> Skills;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TMap<FName, FTowerStats> Towers;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FWaveDefinition> Waves;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FGameBalance Balance;
};

/**
 * Static utility class for loading game data from JSON files.
 * Uses UE JSON API (FJsonSerializer, FJsonObject) to parse
 * data references JSON files into USTRUCT types.
 */
UCLASS()
class UNITSIMCORE_API UJsonDataLoader : public UObject
{
	GENERATED_BODY()

public:
	/**
	 * Load unit definitions from units.json.
	 * JSON format: { "unitId": { "displayName": "...", "maxHP": N, ... }, ... }
	 */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Data")
	static bool LoadUnits(const FString& FilePath, TMap<FName, FUnitStats>& OutUnits);

	/**
	 * Load skill/ability definitions from skills.json.
	 * JSON format: { "skillId": { "type": "...", ... }, ... }
	 */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Data")
	static bool LoadSkills(const FString& FilePath, TMap<FName, FAbilityData>& OutSkills);

	/**
	 * Load tower definitions from towers.json.
	 * JSON format: { "towerId": { "displayName": "...", "type": "...", ... }, ... }
	 */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Data")
	static bool LoadTowers(const FString& FilePath, TMap<FName, FTowerStats>& OutTowers);

	/**
	 * Load wave definitions from waves.json.
	 * JSON format: { "wave_1": { "waveNumber": 1, "spawns": [...], "delayFrames": N }, ... }
	 */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Data")
	static bool LoadWaves(const FString& FilePath, TArray<FWaveDefinition>& OutWaves);

	/**
	 * Load balance/simulation settings from balance.json.
	 * JSON format: { "version": 1, "simulation": {...}, "unit": {...}, ... }
	 */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Data")
	static bool LoadBalance(const FString& FilePath, FGameBalance& OutBalance);

	/**
	 * Load all game data from a directory containing units.json, skills.json,
	 * towers.json, waves.json, and balance.json.
	 */
	UFUNCTION(BlueprintCallable, Category = "UnitSim|Data")
	static bool LoadAll(const FString& DirectoryPath, FGameData& OutData);

private:
	/** Read a JSON file and parse it into an FJsonObject. */
	static TSharedPtr<FJsonObject> LoadJsonFile(const FString& FilePath);

	/** Parse a unit role string to EUnitRole enum. */
	static EUnitRole ParseUnitRole(const FString& Value);

	/** Parse a movement layer string to EMovementLayer enum. */
	static EMovementLayer ParseMovementLayer(const FString& Value);

	/** Parse a target type string to ETargetType enum. */
	static ETargetType ParseTargetType(const FString& Value);

	/** Parse a target priority string to ETargetPriority enum. */
	static ETargetPriority ParseTargetPriority(const FString& Value);

	/** Parse an attack type string to EAttackType enum. */
	static EAttackType ParseAttackType(const FString& Value);

	/** Parse an ability type string to EAbilityType enum. */
	static EAbilityType ParseAbilityType(const FString& Value);

	/** Parse a tower type string to ETowerType enum. */
	static ETowerType ParseTowerType(const FString& Value);
};
