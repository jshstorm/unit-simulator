#pragma once

#include "CoreMinimal.h"
#include "Units/UnitDefinition.h"

/**
 * Registry managing unit definitions.
 * Looks up definitions by UnitId to create actual unit instances.
 * Ported from Units/UnitRegistry.cs (215 lines)
 */
class UNITSIMCORE_API FUnitRegistry
{
public:
	/** Register a unit definition */
	void Register(const FUnitDefinition& Definition);

	/** Register multiple definitions */
	void RegisterAll(const TArray<FUnitDefinition>& Definitions);

	/** Lookup a definition by ID. Returns nullptr if not found. */
	const FUnitDefinition* GetDefinition(const FName& InUnitId) const;

	/** Check if a definition exists */
	bool HasDefinition(const FName& InUnitId) const;

	/** Get all registered IDs */
	TArray<FName> GetRegisteredIds() const;

	/** Create a registry with default unit definitions */
	static FUnitRegistry CreateWithDefaults();

	/** Get the default unit definitions */
	static TArray<FUnitDefinition> GetDefaultDefinitions();

private:
	TMap<FName, FUnitDefinition> Definitions;
};
