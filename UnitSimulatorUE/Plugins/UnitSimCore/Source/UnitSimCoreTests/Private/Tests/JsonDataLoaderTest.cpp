#include "Misc/AutomationTest.h"
#include "Data/JsonDataLoader.h"
#include "Misc/Paths.h"

// ============================================================================
// Helper: Get data/references path
// ============================================================================

static FString GetDataReferencesPath()
{
	// Navigate from the plugin to the project root data/references directory
	// UE project layout: UnitSimulatorUE/Plugins/UnitSimCore/...
	// Data location:     data/references/
	FString PluginDir = FPaths::ConvertRelativePathToFull(
		FPaths::Combine(FPaths::ProjectPluginsDir(), TEXT("UnitSimCore")));

	// Go up to repo root: UnitSimulatorUE -> unit-simulator
	FString ProjectDir = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir());
	FString RepoRoot = FPaths::GetPath(FPaths::GetPath(ProjectDir));
	// Fallback: try various relative paths
	TArray<FString> Candidates = {
		FPaths::Combine(RepoRoot, TEXT("data"), TEXT("references")),
		FPaths::Combine(ProjectDir, TEXT(".."), TEXT("data"), TEXT("references")),
		FPaths::Combine(ProjectDir, TEXT(".."), TEXT(".."), TEXT("data"), TEXT("references")),
		// Direct absolute path for CI/development
		TEXT("/Users/jshstorm/Documents/github/unit-simulator/data/references")
	};

	for (const FString& Candidate : Candidates)
	{
		FString Normalized = FPaths::ConvertRelativePathToFull(Candidate);
		if (FPaths::DirectoryExists(Normalized))
		{
			return Normalized;
		}
	}

	// Return default even if not found (will produce test errors, not crashes)
	return Candidates.Last();
}

// ============================================================================
// LoadUnits
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FJsonLoadUnits,
	"UnitSimCore.JsonDataLoader.LoadUnits.ValidData",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FJsonLoadUnits::RunTest(const FString& Parameters)
{
	// Arrange
	FString FilePath = FPaths::Combine(GetDataReferencesPath(), TEXT("units.json"));
	TMap<FName, FUnitStats> Units;

	// Act
	bool bSuccess = UJsonDataLoader::LoadUnits(FilePath, Units);

	// Assert
	TestTrue(TEXT("Load succeeded"), bSuccess);
	TestTrue(TEXT("Units loaded"), Units.Num() > 0);

	// Verify a well-known unit exists (knight is a standard unit)
	if (Units.Contains(FName(TEXT("knight"))))
	{
		const FUnitStats& Knight = Units[FName(TEXT("knight"))];
		TestTrue(TEXT("Knight HP > 0"), Knight.HP > 0);
		TestTrue(TEXT("Knight Damage > 0"), Knight.Damage > 0);
		TestTrue(TEXT("Knight MoveSpeed > 0"), Knight.MoveSpeed > 0.f);
	}

	return true;
}

// ============================================================================
// LoadSkills
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FJsonLoadSkills,
	"UnitSimCore.JsonDataLoader.LoadSkills.ValidData",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FJsonLoadSkills::RunTest(const FString& Parameters)
{
	// Arrange
	FString FilePath = FPaths::Combine(GetDataReferencesPath(), TEXT("skills.json"));
	TMap<FName, FAbilityData> Skills;

	// Act
	bool bSuccess = UJsonDataLoader::LoadSkills(FilePath, Skills);

	// Assert
	TestTrue(TEXT("Load succeeded"), bSuccess);
	TestTrue(TEXT("Skills loaded"), Skills.Num() > 0);

	return true;
}

// ============================================================================
// LoadTowers
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FJsonLoadTowers,
	"UnitSimCore.JsonDataLoader.LoadTowers.ValidData",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FJsonLoadTowers::RunTest(const FString& Parameters)
{
	// Arrange
	FString FilePath = FPaths::Combine(GetDataReferencesPath(), TEXT("towers.json"));
	TMap<FName, FTowerStats> Towers;

	// Act
	bool bSuccess = UJsonDataLoader::LoadTowers(FilePath, Towers);

	// Assert
	TestTrue(TEXT("Load succeeded"), bSuccess);
	TestTrue(TEXT("Towers loaded"), Towers.Num() > 0);

	return true;
}

// ============================================================================
// LoadBalance
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FJsonLoadBalance,
	"UnitSimCore.JsonDataLoader.LoadBalance.ValidData",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FJsonLoadBalance::RunTest(const FString& Parameters)
{
	// Arrange
	FString FilePath = FPaths::Combine(GetDataReferencesPath(), TEXT("balance.json"));
	FGameBalance Balance;

	// Act
	bool bSuccess = UJsonDataLoader::LoadBalance(FilePath, Balance);

	// Assert
	TestTrue(TEXT("Load succeeded"), bSuccess);
	TestTrue(TEXT("Version >= 1"), Balance.Version >= 1);
	TestTrue(TEXT("SimulationWidth > 0"), Balance.SimulationWidth > 0);
	TestTrue(TEXT("SimulationHeight > 0"), Balance.SimulationHeight > 0);
	TestTrue(TEXT("MaxFrames > 0"), Balance.MaxFrames > 0);

	return true;
}

// ============================================================================
// Load Non-Existent File
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FJsonLoadNonExistent,
	"UnitSimCore.JsonDataLoader.LoadUnits.NonExistentReturnsFalse",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FJsonLoadNonExistent::RunTest(const FString& Parameters)
{
	// Arrange
	TMap<FName, FUnitStats> Units;

	// Act
	bool bSuccess = UJsonDataLoader::LoadUnits(TEXT("/nonexistent/path/units.json"), Units);

	// Assert
	TestFalse(TEXT("Load of non-existent file returns false"), bSuccess);
	TestEqual(TEXT("No units loaded"), Units.Num(), 0);

	return true;
}

// ============================================================================
// Load Non-Existent Skills
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FJsonLoadNonExistentSkills,
	"UnitSimCore.JsonDataLoader.LoadSkills.NonExistentReturnsFalse",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FJsonLoadNonExistentSkills::RunTest(const FString& Parameters)
{
	// Arrange
	TMap<FName, FAbilityData> Skills;

	// Act
	bool bSuccess = UJsonDataLoader::LoadSkills(TEXT("/nonexistent/skills.json"), Skills);

	// Assert
	TestFalse(TEXT("Non-existent skills returns false"), bSuccess);
	TestEqual(TEXT("No skills loaded"), Skills.Num(), 0);

	return true;
}

// ============================================================================
// Load Non-Existent Balance
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FJsonLoadNonExistentBalance,
	"UnitSimCore.JsonDataLoader.LoadBalance.NonExistentReturnsFalse",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FJsonLoadNonExistentBalance::RunTest(const FString& Parameters)
{
	// Arrange
	FGameBalance Balance;

	// Act
	bool bSuccess = UJsonDataLoader::LoadBalance(TEXT("/nonexistent/balance.json"), Balance);

	// Assert
	TestFalse(TEXT("Non-existent balance returns false"), bSuccess);

	return true;
}

// ============================================================================
// Load Non-Existent Towers
// ============================================================================

IMPLEMENT_SIMPLE_AUTOMATION_TEST(FJsonLoadNonExistentTowers,
	"UnitSimCore.JsonDataLoader.LoadTowers.NonExistentReturnsFalse",
	EAutomationTestFlags::EditorContext | EAutomationTestFlags::ProductFilter)

bool FJsonLoadNonExistentTowers::RunTest(const FString& Parameters)
{
	// Arrange
	TMap<FName, FTowerStats> Towers;

	// Act
	bool bSuccess = UJsonDataLoader::LoadTowers(TEXT("/nonexistent/towers.json"), Towers);

	// Assert
	TestFalse(TEXT("Non-existent towers returns false"), bSuccess);
	TestEqual(TEXT("No towers loaded"), Towers.Num(), 0);

	return true;
}
