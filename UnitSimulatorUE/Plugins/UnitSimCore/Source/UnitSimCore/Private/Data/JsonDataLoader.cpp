#include "Data/JsonDataLoader.h"
#include "Misc/FileHelper.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"

DEFINE_LOG_CATEGORY_STATIC(LogJsonDataLoader, Log, All);

// ============================================================================
// JSON File Loading
// ============================================================================

TSharedPtr<FJsonObject> UJsonDataLoader::LoadJsonFile(const FString& FilePath)
{
	FString JsonString;
	if (!FFileHelper::LoadFileToString(JsonString, *FilePath))
	{
		UE_LOG(LogJsonDataLoader, Warning, TEXT("Failed to load file: %s"), *FilePath);
		return nullptr;
	}

	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonString);
	TSharedPtr<FJsonObject> JsonObject;

	if (!FJsonSerializer::Deserialize(Reader, JsonObject) || !JsonObject.IsValid())
	{
		UE_LOG(LogJsonDataLoader, Warning, TEXT("Failed to parse JSON: %s"), *FilePath);
		return nullptr;
	}

	return JsonObject;
}

// ============================================================================
// LoadUnits
// ============================================================================

bool UJsonDataLoader::LoadUnits(const FString& FilePath, TMap<FName, FUnitStats>& OutUnits)
{
	TSharedPtr<FJsonObject> Root = LoadJsonFile(FilePath);
	if (!Root.IsValid())
	{
		return false;
	}

	OutUnits.Empty();

	for (const auto& Pair : Root->Values)
	{
		const FString& UnitId = Pair.Key;
		const TSharedPtr<FJsonObject>* UnitObj = nullptr;
		if (!Pair.Value->TryGetObject(UnitObj) || !UnitObj || !(*UnitObj).IsValid())
		{
			UE_LOG(LogJsonDataLoader, Warning, TEXT("Skipping invalid unit entry: %s"), *UnitId);
			continue;
		}

		const TSharedPtr<FJsonObject>& Obj = *UnitObj;
		FUnitStats Stats;

		Stats.DisplayName = Obj->GetStringField(TEXT("displayName"));
		Stats.HP = Obj->GetIntegerField(TEXT("maxHP"));
		Stats.Damage = Obj->GetIntegerField(TEXT("damage"));
		Stats.MoveSpeed = static_cast<float>(Obj->GetNumberField(TEXT("moveSpeed")));
		Stats.TurnSpeed = static_cast<float>(Obj->GetNumberField(TEXT("turnSpeed")));
		Stats.AttackRange = static_cast<float>(Obj->GetNumberField(TEXT("attackRange")));
		Stats.Radius = static_cast<float>(Obj->GetNumberField(TEXT("radius")));

		// Optional fields with defaults
		Stats.AttackSpeed = Obj->HasField(TEXT("attackSpeed"))
			? static_cast<float>(Obj->GetNumberField(TEXT("attackSpeed")))
			: 1.0f;
		Stats.SpawnCount = Obj->HasField(TEXT("spawnCount"))
			? Obj->GetIntegerField(TEXT("spawnCount"))
			: 1;

		// Enum fields
		Stats.Role = ParseUnitRole(Obj->GetStringField(TEXT("role")));
		Stats.Layer = ParseMovementLayer(Obj->GetStringField(TEXT("layer")));
		Stats.CanTarget = ParseTargetType(Obj->GetStringField(TEXT("canTarget")));

		if (Obj->HasField(TEXT("targetPriority")))
		{
			Stats.TargetPriority = ParseTargetPriority(Obj->GetStringField(TEXT("targetPriority")));
		}
		else
		{
			Stats.TargetPriority = ETargetPriority::Nearest;
		}

		if (Obj->HasField(TEXT("attackType")))
		{
			Stats.AttackType = ParseAttackType(Obj->GetStringField(TEXT("attackType")));
		}
		else
		{
			Stats.AttackType = EAttackType::Melee;
		}

		// Skills array
		Stats.Skills.Empty();
		const TArray<TSharedPtr<FJsonValue>>* SkillsArray = nullptr;
		if (Obj->TryGetArrayField(TEXT("skills"), SkillsArray) && SkillsArray)
		{
			for (const auto& SkillVal : *SkillsArray)
			{
				FString SkillId;
				if (SkillVal->TryGetString(SkillId))
				{
					Stats.Skills.Add(FName(*SkillId));
				}
			}
		}

		OutUnits.Add(FName(*UnitId), Stats);
	}

	UE_LOG(LogJsonDataLoader, Log, TEXT("Loaded %d units from %s"), OutUnits.Num(), *FilePath);
	return true;
}

// ============================================================================
// LoadSkills
// ============================================================================

bool UJsonDataLoader::LoadSkills(const FString& FilePath, TMap<FName, FAbilityData>& OutSkills)
{
	TSharedPtr<FJsonObject> Root = LoadJsonFile(FilePath);
	if (!Root.IsValid())
	{
		return false;
	}

	OutSkills.Empty();

	for (const auto& Pair : Root->Values)
	{
		const FString& SkillId = Pair.Key;
		const TSharedPtr<FJsonObject>* SkillObj = nullptr;
		if (!Pair.Value->TryGetObject(SkillObj) || !SkillObj || !(*SkillObj).IsValid())
		{
			UE_LOG(LogJsonDataLoader, Warning, TEXT("Skipping invalid skill entry: %s"), *SkillId);
			continue;
		}

		const TSharedPtr<FJsonObject>& Obj = *SkillObj;
		FAbilityData Ability;

		Ability.Type = ParseAbilityType(Obj->GetStringField(TEXT("type")));

		// Parse type-specific fields
		switch (Ability.Type)
		{
		case EAbilityType::ChargeAttack:
			Ability.ChargeAttack.TriggerDistance = Obj->HasField(TEXT("triggerDistance"))
				? static_cast<float>(Obj->GetNumberField(TEXT("triggerDistance")))
				: 150.0f;
			Ability.ChargeAttack.RequiredChargeDistance = Obj->HasField(TEXT("requiredChargeDistance"))
				? static_cast<float>(Obj->GetNumberField(TEXT("requiredChargeDistance")))
				: 100.0f;
			Ability.ChargeAttack.DamageMultiplier = Obj->HasField(TEXT("damageMultiplier"))
				? static_cast<float>(Obj->GetNumberField(TEXT("damageMultiplier")))
				: 2.0f;
			Ability.ChargeAttack.SpeedMultiplier = Obj->HasField(TEXT("speedMultiplier"))
				? static_cast<float>(Obj->GetNumberField(TEXT("speedMultiplier")))
				: 2.0f;
			break;

		case EAbilityType::SplashDamage:
			Ability.SplashDamage.Radius = Obj->HasField(TEXT("radius"))
				? static_cast<float>(Obj->GetNumberField(TEXT("radius")))
				: 60.0f;
			Ability.SplashDamage.DamageFalloff = Obj->HasField(TEXT("damageFalloff"))
				? static_cast<float>(Obj->GetNumberField(TEXT("damageFalloff")))
				: 0.0f;
			break;

		case EAbilityType::Shield:
			Ability.Shield.MaxShieldHP = Obj->HasField(TEXT("maxShieldHP"))
				? Obj->GetIntegerField(TEXT("maxShieldHP"))
				: 200;
			break;

		case EAbilityType::DeathSpawn:
			Ability.DeathSpawn.SpawnUnitId = Obj->HasField(TEXT("spawnUnitId"))
				? FName(*Obj->GetStringField(TEXT("spawnUnitId")))
				: NAME_None;
			Ability.DeathSpawn.SpawnCount = Obj->HasField(TEXT("spawnCount"))
				? Obj->GetIntegerField(TEXT("spawnCount"))
				: 2;
			Ability.DeathSpawn.SpawnRadius = Obj->HasField(TEXT("spawnRadius"))
				? static_cast<float>(Obj->GetNumberField(TEXT("spawnRadius")))
				: 30.0f;
			break;

		case EAbilityType::DeathDamage:
			Ability.DeathDamage.Damage = Obj->HasField(TEXT("damage"))
				? Obj->GetIntegerField(TEXT("damage"))
				: 100;
			Ability.DeathDamage.Radius = Obj->HasField(TEXT("radius"))
				? static_cast<float>(Obj->GetNumberField(TEXT("radius")))
				: 60.0f;
			break;

		default:
			UE_LOG(LogJsonDataLoader, Warning, TEXT("Unknown ability type for skill: %s"), *SkillId);
			break;
		}

		OutSkills.Add(FName(*SkillId), Ability);
	}

	UE_LOG(LogJsonDataLoader, Log, TEXT("Loaded %d skills from %s"), OutSkills.Num(), *FilePath);
	return true;
}

// ============================================================================
// LoadTowers
// ============================================================================

bool UJsonDataLoader::LoadTowers(const FString& FilePath, TMap<FName, FTowerStats>& OutTowers)
{
	TSharedPtr<FJsonObject> Root = LoadJsonFile(FilePath);
	if (!Root.IsValid())
	{
		return false;
	}

	OutTowers.Empty();

	for (const auto& Pair : Root->Values)
	{
		const FString& TowerId = Pair.Key;
		const TSharedPtr<FJsonObject>* TowerObj = nullptr;
		if (!Pair.Value->TryGetObject(TowerObj) || !TowerObj || !(*TowerObj).IsValid())
		{
			UE_LOG(LogJsonDataLoader, Warning, TEXT("Skipping invalid tower entry: %s"), *TowerId);
			continue;
		}

		const TSharedPtr<FJsonObject>& Obj = *TowerObj;
		FTowerStats Stats;

		Stats.DisplayName = Obj->GetStringField(TEXT("displayName"));
		Stats.TowerType = ParseTowerType(Obj->GetStringField(TEXT("type")));
		Stats.MaxHP = Obj->GetIntegerField(TEXT("maxHP"));
		Stats.Damage = Obj->GetIntegerField(TEXT("damage"));
		Stats.AttackSpeed = static_cast<float>(Obj->GetNumberField(TEXT("attackSpeed")));
		Stats.AttackRadius = static_cast<float>(Obj->GetNumberField(TEXT("attackRadius")));
		Stats.Radius = static_cast<float>(Obj->GetNumberField(TEXT("radius")));
		Stats.CanTarget = ParseTargetType(Obj->GetStringField(TEXT("canTarget")));

		OutTowers.Add(FName(*TowerId), Stats);
	}

	UE_LOG(LogJsonDataLoader, Log, TEXT("Loaded %d towers from %s"), OutTowers.Num(), *FilePath);
	return true;
}

// ============================================================================
// LoadWaves
// ============================================================================

bool UJsonDataLoader::LoadWaves(const FString& FilePath, TArray<FWaveDefinition>& OutWaves)
{
	TSharedPtr<FJsonObject> Root = LoadJsonFile(FilePath);
	if (!Root.IsValid())
	{
		return false;
	}

	OutWaves.Empty();

	for (const auto& Pair : Root->Values)
	{
		const TSharedPtr<FJsonObject>* WaveObj = nullptr;
		if (!Pair.Value->TryGetObject(WaveObj) || !WaveObj || !(*WaveObj).IsValid())
		{
			UE_LOG(LogJsonDataLoader, Warning, TEXT("Skipping invalid wave entry: %s"), *Pair.Key);
			continue;
		}

		const TSharedPtr<FJsonObject>& Obj = *WaveObj;
		FWaveDefinition Wave;

		Wave.WaveNumber = Obj->GetIntegerField(TEXT("waveNumber"));
		Wave.DelayFrames = Obj->GetIntegerField(TEXT("delayFrames"));

		// Parse spawns array
		const TArray<TSharedPtr<FJsonValue>>* SpawnsArray = nullptr;
		if (Obj->TryGetArrayField(TEXT("spawns"), SpawnsArray) && SpawnsArray)
		{
			for (const auto& SpawnVal : *SpawnsArray)
			{
				const TSharedPtr<FJsonObject>* SpawnObj = nullptr;
				if (!SpawnVal->TryGetObject(SpawnObj) || !SpawnObj || !(*SpawnObj).IsValid())
				{
					continue;
				}

				FWaveSpawnGroup Entry;
				Entry.UnitId = FName(*(*SpawnObj)->GetStringField(TEXT("unitId")));
				Entry.Count = (*SpawnObj)->GetIntegerField(TEXT("count"));

				// Parse faction
				FString FactionStr;
				if ((*SpawnObj)->TryGetStringField(TEXT("faction"), FactionStr))
				{
					Entry.Faction = FactionStr;
				}

				// Parse position
				const TSharedPtr<FJsonObject>* PosObj = nullptr;
				if ((*SpawnObj)->TryGetObjectField(TEXT("position"), PosObj) && PosObj && (*PosObj).IsValid())
				{
					Entry.SpawnX = (*PosObj)->GetNumberField(TEXT("x"));
					Entry.SpawnY = (*PosObj)->GetNumberField(TEXT("y"));
				}

				Wave.SpawnGroups.Add(Entry);
			}
		}

		OutWaves.Add(Wave);
	}

	// Sort waves by wave number
	OutWaves.Sort([](const FWaveDefinition& A, const FWaveDefinition& B)
	{
		return A.WaveNumber < B.WaveNumber;
	});

	UE_LOG(LogJsonDataLoader, Log, TEXT("Loaded %d waves from %s"), OutWaves.Num(), *FilePath);
	return true;
}

// ============================================================================
// LoadBalance
// ============================================================================

bool UJsonDataLoader::LoadBalance(const FString& FilePath, FGameBalance& OutBalance)
{
	TSharedPtr<FJsonObject> Root = LoadJsonFile(FilePath);
	if (!Root.IsValid())
	{
		return false;
	}

	// Default values
	OutBalance = FGameBalance();

	OutBalance.Version = Root->HasField(TEXT("version"))
		? Root->GetIntegerField(TEXT("version"))
		: 1;

	// Simulation section
	const TSharedPtr<FJsonObject>* SimObj = nullptr;
	if (Root->TryGetObjectField(TEXT("simulation"), SimObj) && SimObj && (*SimObj).IsValid())
	{
		OutBalance.SimulationWidth = (*SimObj)->GetIntegerField(TEXT("width"));
		OutBalance.SimulationHeight = (*SimObj)->GetIntegerField(TEXT("height"));
		OutBalance.MaxFrames = (*SimObj)->GetIntegerField(TEXT("maxFrames"));
		OutBalance.FrameTimeSeconds = static_cast<float>((*SimObj)->GetNumberField(TEXT("frameTimeSeconds")));
	}

	// Unit section
	const TSharedPtr<FJsonObject>* UnitObj = nullptr;
	if (Root->TryGetObjectField(TEXT("unit"), UnitObj) && UnitObj && (*UnitObj).IsValid())
	{
		OutBalance.UnitRadius = static_cast<float>((*UnitObj)->GetNumberField(TEXT("defaultRadius")));
		OutBalance.CollisionRadiusScale = static_cast<float>((*UnitObj)->GetNumberField(TEXT("collisionRadiusScale")));
		OutBalance.NumAttackSlots = (*UnitObj)->GetIntegerField(TEXT("numAttackSlots"));
		OutBalance.SlotReevaluateDistance = static_cast<float>((*UnitObj)->GetNumberField(TEXT("slotReevaluateDistance")));
		OutBalance.SlotReevaluateIntervalFrames = (*UnitObj)->GetIntegerField(TEXT("slotReevaluateIntervalFrames"));
	}

	// Combat section
	const TSharedPtr<FJsonObject>* CombatObj = nullptr;
	if (Root->TryGetObjectField(TEXT("combat"), CombatObj) && CombatObj && (*CombatObj).IsValid())
	{
		OutBalance.AttackCooldown = static_cast<float>((*CombatObj)->GetNumberField(TEXT("attackCooldown")));
		OutBalance.MeleeRangeMultiplier = (*CombatObj)->GetIntegerField(TEXT("meleeRangeMultiplier"));
		OutBalance.RangedRangeMultiplier = (*CombatObj)->GetIntegerField(TEXT("rangedRangeMultiplier"));
		OutBalance.EngagementTriggerDistanceMultiplier = static_cast<float>((*CombatObj)->GetNumberField(TEXT("engagementTriggerDistanceMultiplier")));
	}

	// Squad section
	const TSharedPtr<FJsonObject>* SquadObj = nullptr;
	if (Root->TryGetObjectField(TEXT("squad"), SquadObj) && SquadObj && (*SquadObj).IsValid())
	{
		OutBalance.RallyDistance = static_cast<float>((*SquadObj)->GetNumberField(TEXT("rallyDistance")));
		OutBalance.FormationThreshold = static_cast<float>((*SquadObj)->GetNumberField(TEXT("formationThreshold")));
		OutBalance.SeparationRadius = static_cast<float>((*SquadObj)->GetNumberField(TEXT("separationRadius")));
		OutBalance.FriendlySeparationRadius = static_cast<float>((*SquadObj)->GetNumberField(TEXT("friendlySeparationRadius")));
		OutBalance.DestinationThreshold = static_cast<float>((*SquadObj)->GetNumberField(TEXT("destinationThreshold")));
	}

	// Wave section
	const TSharedPtr<FJsonObject>* WaveObj = nullptr;
	if (Root->TryGetObjectField(TEXT("wave"), WaveObj) && WaveObj && (*WaveObj).IsValid())
	{
		OutBalance.MaxWaves = (*WaveObj)->GetIntegerField(TEXT("maxWaves"));
	}

	// Targeting section
	const TSharedPtr<FJsonObject>* TargetObj = nullptr;
	if (Root->TryGetObjectField(TEXT("targeting"), TargetObj) && TargetObj && (*TargetObj).IsValid())
	{
		OutBalance.TargetReevaluateIntervalFrames = (*TargetObj)->GetIntegerField(TEXT("reevaluateIntervalFrames"));
		OutBalance.TargetSwitchMargin = static_cast<float>((*TargetObj)->GetNumberField(TEXT("switchMargin")));
		OutBalance.TargetCrowdPenaltyPerAttacker = static_cast<float>((*TargetObj)->GetNumberField(TEXT("crowdPenaltyPerAttacker")));
	}

	// Avoidance section
	const TSharedPtr<FJsonObject>* AvoidObj = nullptr;
	if (Root->TryGetObjectField(TEXT("avoidance"), AvoidObj) && AvoidObj && (*AvoidObj).IsValid())
	{
		OutBalance.AvoidanceAngleStep = static_cast<float>((*AvoidObj)->GetNumberField(TEXT("angleStep")));
		OutBalance.MaxAvoidanceIterations = (*AvoidObj)->GetIntegerField(TEXT("maxIterations"));
		OutBalance.AvoidanceMaxLookahead = static_cast<float>((*AvoidObj)->GetNumberField(TEXT("maxLookahead")));
	}

	// Collision section
	const TSharedPtr<FJsonObject>* CollObj = nullptr;
	if (Root->TryGetObjectField(TEXT("collision"), CollObj) && CollObj && (*CollObj).IsValid())
	{
		OutBalance.CollisionResolutionIterations = (*CollObj)->GetIntegerField(TEXT("resolutionIterations"));
		OutBalance.CollisionPushStrength = static_cast<float>((*CollObj)->GetNumberField(TEXT("pushStrength")));
	}

	UE_LOG(LogJsonDataLoader, Log, TEXT("Loaded balance data (version %d) from %s"), OutBalance.Version, *FilePath);
	return true;
}

// ============================================================================
// LoadAll
// ============================================================================

bool UJsonDataLoader::LoadAll(const FString& DirectoryPath, FGameData& OutData)
{
	bool bAllSuccess = true;

	if (!LoadUnits(DirectoryPath / TEXT("units.json"), OutData.Units))
	{
		UE_LOG(LogJsonDataLoader, Warning, TEXT("Failed to load units.json"));
		bAllSuccess = false;
	}

	if (!LoadSkills(DirectoryPath / TEXT("skills.json"), OutData.Skills))
	{
		UE_LOG(LogJsonDataLoader, Warning, TEXT("Failed to load skills.json"));
		bAllSuccess = false;
	}

	if (!LoadTowers(DirectoryPath / TEXT("towers.json"), OutData.Towers))
	{
		UE_LOG(LogJsonDataLoader, Warning, TEXT("Failed to load towers.json"));
		bAllSuccess = false;
	}

	if (!LoadWaves(DirectoryPath / TEXT("waves.json"), OutData.Waves))
	{
		UE_LOG(LogJsonDataLoader, Warning, TEXT("Failed to load waves.json"));
		bAllSuccess = false;
	}

	if (!LoadBalance(DirectoryPath / TEXT("balance.json"), OutData.Balance))
	{
		UE_LOG(LogJsonDataLoader, Warning, TEXT("Failed to load balance.json"));
		bAllSuccess = false;
	}

	if (bAllSuccess)
	{
		UE_LOG(LogJsonDataLoader, Log, TEXT("Successfully loaded all game data from %s"), *DirectoryPath);
	}

	return bAllSuccess;
}

// ============================================================================
// Enum Parsers
// ============================================================================

EUnitRole UJsonDataLoader::ParseUnitRole(const FString& Value)
{
	if (Value == TEXT("Melee"))        return EUnitRole::Melee;
	if (Value == TEXT("Ranged"))       return EUnitRole::Ranged;
	if (Value == TEXT("Tank"))         return EUnitRole::Tank;
	if (Value == TEXT("MiniTank"))     return EUnitRole::MiniTank;
	if (Value == TEXT("GlassCannon"))  return EUnitRole::GlassCannon;
	if (Value == TEXT("Swarm"))        return EUnitRole::Swarm;
	if (Value == TEXT("Spawner"))      return EUnitRole::Spawner;
	if (Value == TEXT("Support"))      return EUnitRole::Support;
	if (Value == TEXT("Siege"))        return EUnitRole::Siege;

	UE_LOG(LogJsonDataLoader, Warning, TEXT("Unknown UnitRole: %s, defaulting to Melee"), *Value);
	return EUnitRole::Melee;
}

EMovementLayer UJsonDataLoader::ParseMovementLayer(const FString& Value)
{
	if (Value == TEXT("Ground")) return EMovementLayer::Ground;
	if (Value == TEXT("Air"))    return EMovementLayer::Air;

	UE_LOG(LogJsonDataLoader, Warning, TEXT("Unknown MovementLayer: %s, defaulting to Ground"), *Value);
	return EMovementLayer::Ground;
}

ETargetType UJsonDataLoader::ParseTargetType(const FString& Value)
{
	if (Value == TEXT("Ground"))       return ETargetType::Ground;
	if (Value == TEXT("Air"))          return ETargetType::Air;
	if (Value == TEXT("GroundAndAir")) return ETargetType::GroundAndAir;
	if (Value == TEXT("Building"))     return ETargetType::Building;
	if (Value == TEXT("All"))          return ETargetType::All;
	if (Value == TEXT("None"))         return ETargetType::None;

	UE_LOG(LogJsonDataLoader, Warning, TEXT("Unknown TargetType: %s, defaulting to Ground"), *Value);
	return ETargetType::Ground;
}

ETargetPriority UJsonDataLoader::ParseTargetPriority(const FString& Value)
{
	if (Value == TEXT("Nearest"))   return ETargetPriority::Nearest;
	if (Value == TEXT("Buildings")) return ETargetPriority::Buildings;

	UE_LOG(LogJsonDataLoader, Warning, TEXT("Unknown TargetPriority: %s, defaulting to Nearest"), *Value);
	return ETargetPriority::Nearest;
}

EAttackType UJsonDataLoader::ParseAttackType(const FString& Value)
{
	if (Value == TEXT("MeleeShort"))  return EAttackType::MeleeShort;
	if (Value == TEXT("Melee"))       return EAttackType::Melee;
	if (Value == TEXT("MeleeMedium")) return EAttackType::MeleeMedium;
	if (Value == TEXT("MeleeLong"))   return EAttackType::MeleeLong;
	if (Value == TEXT("Ranged"))      return EAttackType::Ranged;
	if (Value == TEXT("None"))        return EAttackType::None;

	UE_LOG(LogJsonDataLoader, Warning, TEXT("Unknown AttackType: %s, defaulting to Melee"), *Value);
	return EAttackType::Melee;
}

EAbilityType UJsonDataLoader::ParseAbilityType(const FString& Value)
{
	if (Value == TEXT("ChargeAttack")) return EAbilityType::ChargeAttack;
	if (Value == TEXT("SplashDamage")) return EAbilityType::SplashDamage;
	if (Value == TEXT("Shield"))       return EAbilityType::Shield;
	if (Value == TEXT("DeathSpawn"))   return EAbilityType::DeathSpawn;
	if (Value == TEXT("DeathDamage"))  return EAbilityType::DeathDamage;

	UE_LOG(LogJsonDataLoader, Warning, TEXT("Unknown AbilityType: %s"), *Value);
	return EAbilityType::ChargeAttack;
}

ETowerType UJsonDataLoader::ParseTowerType(const FString& Value)
{
	if (Value == TEXT("Princess")) return ETowerType::Princess;
	if (Value == TEXT("King"))     return ETowerType::King;

	UE_LOG(LogJsonDataLoader, Warning, TEXT("Unknown TowerType: %s, defaulting to Princess"), *Value);
	return ETowerType::Princess;
}
