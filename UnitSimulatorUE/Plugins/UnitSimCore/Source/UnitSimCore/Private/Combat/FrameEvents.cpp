#include "Combat/FrameEvents.h"

void FFrameEvents::AddDamage(int32 SourceIndex, int32 TargetIndex, int32 Amount, EDamageType Type)
{
	FSimDamageEvent Event;
	Event.SourceIndex = SourceIndex;
	Event.TargetIndex = TargetIndex;
	Event.Amount = Amount;
	Event.Type = Type;
	Damages.Add(Event);
}

void FFrameEvents::AddSpawn(const FUnitSpawnRequest& Spawn)
{
	Spawns.Add(Spawn);
}

void FFrameEvents::AddTowerDamage(int32 SourceTowerIndex, int32 TargetIndex, int32 Amount)
{
	FTowerDamageEvent Event;
	Event.SourceTowerIndex = SourceTowerIndex;
	Event.TargetIndex = TargetIndex;
	Event.Amount = Amount;
	TowerDamages.Add(Event);
}

void FFrameEvents::AddDamageToTower(int32 SourceIndex, int32 TargetTowerIndex, int32 Amount)
{
	FDamageToTowerEvent Event;
	Event.SourceIndex = SourceIndex;
	Event.TargetTowerIndex = TargetTowerIndex;
	Event.Amount = Amount;
	DamageToTowers.Add(Event);
}

void FFrameEvents::Clear()
{
	Damages.Empty();
	Spawns.Empty();
	TowerDamages.Empty();
	DamageToTowers.Empty();
}
