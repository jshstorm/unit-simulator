#include "Simulation/FrameData.h"
#include "Units/Unit.h"
#include "Towers/Tower.h"
#include "GameState/GameSession.h"

// ============================================================================
// FUnitStateData
// ============================================================================

FUnitStateData FUnitStateData::FromUnit(const FUnit& Unit, const TArray<FUnit>& AllEnemies)
{
	FUnitStateData Data;
	Data.Id = Unit.Id;
	Data.Label = Unit.GetLabel();
	Data.UnitId = Unit.UnitId.ToString();

	// Enum to string conversions
	switch (Unit.TargetPriority)
	{
	case ETargetPriority::Nearest:   Data.TargetPriority = TEXT("Nearest"); break;
	case ETargetPriority::Buildings: Data.TargetPriority = TEXT("Buildings"); break;
	default:                         Data.TargetPriority = TEXT("Nearest"); break;
	}

	switch (Unit.Role)
	{
	case EUnitRole::Melee:       Data.Role = TEXT("Melee"); break;
	case EUnitRole::Ranged:      Data.Role = TEXT("Ranged"); break;
	case EUnitRole::Tank:        Data.Role = TEXT("Tank"); break;
	case EUnitRole::MiniTank:    Data.Role = TEXT("MiniTank"); break;
	case EUnitRole::GlassCannon: Data.Role = TEXT("GlassCannon"); break;
	case EUnitRole::Swarm:       Data.Role = TEXT("Swarm"); break;
	case EUnitRole::Spawner:     Data.Role = TEXT("Spawner"); break;
	case EUnitRole::Support:     Data.Role = TEXT("Support"); break;
	case EUnitRole::Siege:       Data.Role = TEXT("Siege"); break;
	default:                     Data.Role = TEXT("Melee"); break;
	}

	switch (Unit.Faction)
	{
	case EUnitFaction::Friendly: Data.Faction = TEXT("Friendly"); break;
	case EUnitFaction::Enemy:    Data.Faction = TEXT("Enemy"); break;
	default:                     Data.Faction = TEXT("Friendly"); break;
	}

	Data.bIsDead = Unit.bIsDead;
	Data.HP = Unit.HP;
	Data.Radius = Unit.Radius;
	Data.Speed = Unit.Speed;
	Data.TurnSpeed = Unit.TurnSpeed;
	Data.AttackRange = Unit.AttackRange;
	Data.AttackCooldown = Unit.AttackCooldown;
	Data.Layer = Unit.Layer;
	Data.CanTarget = Unit.CanTarget;
	Data.Damage = Unit.Damage;
	Data.ShieldHP = Unit.ShieldHP;
	Data.MaxShieldHP = Unit.MaxShieldHP;

	// Charge state
	Data.bHasChargeState = Unit.bHasChargeAbility;
	Data.bIsCharging = Unit.ChargeState.bIsCharging;
	Data.bIsCharged = Unit.ChargeState.bIsCharged;
	Data.RequiredChargeDistance = Unit.ChargeState.RequiredDistance;

	// Abilities
	for (const auto& Ability : Unit.Abilities)
	{
		Data.Abilities.Add(Ability.Type);
	}

	Data.Position = Unit.Position;
	Data.Velocity = Unit.Velocity;
	Data.Forward = Unit.Forward;
	Data.CurrentDestination = Unit.CurrentDestination;

	// Target ID: use the target unit's Id if valid
	if (Unit.TargetIndex >= 0 && Unit.TargetIndex < AllEnemies.Num())
	{
		Data.TargetId = AllEnemies[Unit.TargetIndex].Id;
	}
	else
	{
		Data.TargetId = -1;
	}

	Data.TakenSlotIndex = Unit.TakenSlotIndex;
	Data.bHasAvoidanceTarget = Unit.bHasAvoidanceTarget;
	Data.AvoidanceTarget = Unit.bHasAvoidanceTarget ? Unit.AvoidanceTarget : FVector2D::ZeroVector;

	// Movement detection
	Data.bIsMoving = Unit.Velocity.SizeSquared() > 0.01;

	// In attack range detection
	if (Unit.TargetIndex >= 0 && Unit.TargetIndex < AllEnemies.Num() && !AllEnemies[Unit.TargetIndex].bIsDead)
	{
		const double DistToTarget = FVector2D::Distance(Unit.Position, AllEnemies[Unit.TargetIndex].Position);
		Data.bInAttackRange = DistToTarget <= Unit.AttackRange;
	}
	else
	{
		Data.bInAttackRange = false;
	}

	return Data;
}

// ============================================================================
// FTowerStateData
// ============================================================================

FTowerStateData FTowerStateData::FromTower(const FTower& Tower)
{
	FTowerStateData Data;
	Data.Id = Tower.Id;

	switch (Tower.Type)
	{
	case ETowerType::Princess: Data.Type = TEXT("Princess"); break;
	case ETowerType::King:     Data.Type = TEXT("King"); break;
	default:                   Data.Type = TEXT("Princess"); break;
	}

	switch (Tower.Faction)
	{
	case EUnitFaction::Friendly: Data.Faction = TEXT("Friendly"); break;
	case EUnitFaction::Enemy:    Data.Faction = TEXT("Enemy"); break;
	default:                     Data.Faction = TEXT("Friendly"); break;
	}

	Data.Position = Tower.Position;
	Data.Radius = Tower.Radius;
	Data.AttackRange = Tower.AttackRange;
	Data.MaxHP = Tower.MaxHP;
	Data.CurrentHP = Tower.CurrentHP;
	Data.bIsActivated = Tower.bIsActivated;
	Data.AttackCooldown = Tower.AttackCooldown;
	return Data;
}

// ============================================================================
// FFrameData
// ============================================================================

FFrameData FFrameData::FromSimulationState(
	int32 FrameNumber,
	const TArray<FUnit>& Friendlies,
	const TArray<FUnit>& Enemies,
	const FVector2D& MainTarget,
	int32 CurrentWave,
	bool bHasMoreWaves,
	const FGameSession* Session)
{
	FFrameData Data;
	Data.FrameNumber = FrameNumber;
	Data.CurrentWave = CurrentWave;
	Data.MainTarget = MainTarget;

	// Count living units
	int32 LivingFriendly = 0;
	int32 LivingEnemy = 0;

	// Friendly units: their targets are in Enemies array
	for (const FUnit& F : Friendlies)
	{
		if (!F.bIsDead) LivingFriendly++;
		Data.FriendlyUnits.Add(FUnitStateData::FromUnit(F, Enemies));
	}

	// Enemy units: their targets are in Friendlies array
	for (const FUnit& E : Enemies)
	{
		if (!E.bIsDead) LivingEnemy++;
		Data.EnemyUnits.Add(FUnitStateData::FromUnit(E, Friendlies));
	}

	Data.LivingFriendlyCount = LivingFriendly;
	Data.LivingEnemyCount = LivingEnemy;

	Data.bAllWavesCleared = !bHasMoreWaves && LivingEnemy == 0;
	Data.bMaxFramesReached = FrameNumber >= UnitSimConstants::MAX_FRAMES - 1;

	if (Session)
	{
		for (const FTower& T : Session->FriendlyTowers)
		{
			Data.FriendlyTowers.Add(FTowerStateData::FromTower(T));
		}
		for (const FTower& T : Session->EnemyTowers)
		{
			Data.EnemyTowers.Add(FTowerStateData::FromTower(T));
		}
		Data.ElapsedTime = Session->ElapsedTime;
		Data.FriendlyCrowns = Session->FriendlyCrowns;
		Data.EnemyCrowns = Session->EnemyCrowns;
		Data.GameResult = Session->Result;
		Data.WinConditionType = Session->WinConditionType;
		Data.bIsOvertime = Session->bIsOvertime;
	}

	return Data;
}

FString FFrameData::ToJson() const
{
	TSharedRef<FJsonObject> Root = MakeShared<FJsonObject>();
	Root->SetNumberField(TEXT("frameNumber"), FrameNumber);
	Root->SetNumberField(TEXT("currentWave"), CurrentWave);
	Root->SetNumberField(TEXT("livingFriendlyCount"), LivingFriendlyCount);
	Root->SetNumberField(TEXT("livingEnemyCount"), LivingEnemyCount);
	Root->SetNumberField(TEXT("elapsedTime"), ElapsedTime);
	Root->SetNumberField(TEXT("friendlyCrowns"), FriendlyCrowns);
	Root->SetNumberField(TEXT("enemyCrowns"), EnemyCrowns);
	Root->SetBoolField(TEXT("isOvertime"), bIsOvertime);
	Root->SetBoolField(TEXT("allWavesCleared"), bAllWavesCleared);
	Root->SetBoolField(TEXT("maxFramesReached"), bMaxFramesReached);

	FString OutputString;
	TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&OutputString);
	FJsonSerializer::Serialize(Root, Writer);
	return OutputString;
}

bool FFrameData::FromJson(const FString& JsonString, FFrameData& OutFrameData)
{
	TSharedPtr<FJsonObject> Root;
	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonString);
	if (!FJsonSerializer::Deserialize(Reader, Root) || !Root.IsValid())
	{
		return false;
	}

	OutFrameData.FrameNumber = static_cast<int32>(Root->GetNumberField(TEXT("frameNumber")));
	OutFrameData.CurrentWave = static_cast<int32>(Root->GetNumberField(TEXT("currentWave")));
	OutFrameData.LivingFriendlyCount = static_cast<int32>(Root->GetNumberField(TEXT("livingFriendlyCount")));
	OutFrameData.LivingEnemyCount = static_cast<int32>(Root->GetNumberField(TEXT("livingEnemyCount")));
	OutFrameData.ElapsedTime = static_cast<float>(Root->GetNumberField(TEXT("elapsedTime")));
	OutFrameData.FriendlyCrowns = static_cast<int32>(Root->GetNumberField(TEXT("friendlyCrowns")));
	OutFrameData.EnemyCrowns = static_cast<int32>(Root->GetNumberField(TEXT("enemyCrowns")));
	OutFrameData.bIsOvertime = Root->GetBoolField(TEXT("isOvertime"));
	OutFrameData.bAllWavesCleared = Root->GetBoolField(TEXT("allWavesCleared"));
	OutFrameData.bMaxFramesReached = Root->GetBoolField(TEXT("maxFramesReached"));

	return true;
}
