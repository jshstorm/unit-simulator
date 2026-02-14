#pragma once

#include "CoreMinimal.h"
#include "GameConstants.generated.h"

// ============================================================================
// Enums (ported from ReferenceModels/Models/Enums/ and UnitSimulator.Core)
// ============================================================================

/** Simulation lifecycle status */
UENUM(BlueprintType)
enum class ESimulationStatus : uint8
{
	Uninitialized,
	Initialized,
	Running,
	Completed
};

/** Unit faction */
UENUM(BlueprintType)
enum class EUnitFaction : uint8
{
	Friendly,
	Enemy
};

/** Unit tactical role */
UENUM(BlueprintType)
enum class EUnitRole : uint8
{
	Melee,
	Ranged,
	Tank,
	MiniTank,
	GlassCannon,
	Swarm,
	Spawner,
	Support,
	Siege
};

/** Movement layer */
UENUM(BlueprintType)
enum class EMovementLayer : uint8
{
	Ground,
	Air
};

/** Target type (flags) */
UENUM(BlueprintType, Meta = (Bitflags, UseEnumValuesAsMaskValuesInEditor = "true"))
enum class ETargetType : uint8
{
	None     = 0        UMETA(Hidden),
	Ground   = 1 << 0,
	Air      = 1 << 1,
	Building = 1 << 2,

	GroundAndAir = Ground | Air   UMETA(Hidden),
	All = Ground | Air | Building UMETA(Hidden)
};
ENUM_CLASS_FLAGS(ETargetType);

/** Target priority */
UENUM(BlueprintType)
enum class ETargetPriority : uint8
{
	Nearest,
	Buildings
};

/** Attack type */
UENUM(BlueprintType)
enum class EAttackType : uint8
{
	MeleeShort,
	Melee,
	MeleeMedium,
	MeleeLong,
	Ranged,
	None
};

/** Status effect type (flags) */
UENUM(BlueprintType, Meta = (Bitflags, UseEnumValuesAsMaskValuesInEditor = "true"))
enum class EStatusEffectType : uint16
{
	None         = 0         UMETA(Hidden),
	Stunned      = 1 << 0,
	Frozen       = 1 << 1,
	Slowed       = 1 << 2,
	Rooted       = 1 << 3,
	Poisoned     = 1 << 4,
	Burning      = 1 << 5,
	Raged        = 1 << 6,
	Healing      = 1 << 7,
	Shielded     = 1 << 8,
	Invisible    = 1 << 9,
	Marked       = 1 << 10,
	Invulnerable = 1 << 11
};
ENUM_CLASS_FLAGS(EStatusEffectType);

/** Tower type */
UENUM(BlueprintType)
enum class ETowerType : uint8
{
	Princess,
	King
};

/** Ability type */
UENUM(BlueprintType)
enum class EAbilityType : uint8
{
	ChargeAttack,
	SplashDamage,
	ChainDamage,
	PiercingAttack,
	Shield,
	DeathSpawn,
	DeathDamage,
	StatusEffect
};

// ============================================================================
// Game Constants (ported from GameConstants.cs)
// ============================================================================

namespace UnitSimConstants
{
	// Simulation space
	constexpr int32 SIMULATION_WIDTH = 3200;
	constexpr int32 SIMULATION_HEIGHT = 5100;
	constexpr int32 MAX_FRAMES = 3000;
	constexpr float FRAME_TIME_SECONDS = 1.f / 30.f;

	// Unit settings
	constexpr float UNIT_RADIUS = 20.f;
	constexpr float COLLISION_RADIUS_SCALE = 2.f / 3.f;
	constexpr int32 NUM_ATTACK_SLOTS = 8;
	constexpr float SLOT_REEVALUATE_DISTANCE = 40.f;
	constexpr int32 SLOT_REEVALUATE_INTERVAL_FRAMES = 60;
	constexpr int32 FRIENDLY_HP = 100;
	constexpr int32 ENEMY_HP = 10;

	// Combat settings
	constexpr float ATTACK_COOLDOWN = 30.f;
	constexpr int32 FRIENDLY_ATTACK_DAMAGE = 1;
	constexpr int32 ENEMY_ATTACK_DAMAGE = 1;
	constexpr int32 MELEE_RANGE_MULTIPLIER = 3;
	constexpr int32 RANGED_RANGE_MULTIPLIER = 6;
	constexpr float ENGAGEMENT_TRIGGER_DISTANCE_MULTIPLIER = 1.5f;

	// Squad behavior settings
	constexpr float RALLY_DISTANCE = 300.f;
	constexpr float FORMATION_THRESHOLD = 20.f;
	constexpr float SEPARATION_RADIUS = 120.f;
	constexpr float FRIENDLY_SEPARATION_RADIUS = 80.f;
	constexpr float DESTINATION_THRESHOLD = 10.f;

	// Wave settings
	constexpr int32 MAX_WAVES = 3;

	// Targeting settings (enemy)
	constexpr int32 TARGET_REEVALUATE_INTERVAL_FRAMES = 45;
	constexpr float TARGET_SWITCH_MARGIN = 15.f;
	constexpr float TARGET_CROWD_PENALTY_PER_ATTACKER = 25.f;

	// Avoidance settings
	constexpr float AVOIDANCE_ANGLE_STEP = UE_PI / 8.f; // 22.5 degrees
	constexpr int32 MAX_AVOIDANCE_ITERATIONS = 8;
	constexpr float AVOIDANCE_MAX_LOOKAHEAD = 3.5f;
	constexpr int32 AVOIDANCE_SEGMENT_COUNT = 3;
	constexpr float AVOIDANCE_SEGMENT_START_DISTANCE = 20.f;
	constexpr float AVOIDANCE_LATERAL_PADDING = 25.f;
	constexpr float AVOIDANCE_PARALLEL_DISTANCE_MULTIPLIER = 1.5f;
	constexpr float AVOIDANCE_WAYPOINT_THRESHOLD = 12.f;

	// Phase 1: Static Obstacle Settings
	constexpr float TOWER_COLLISION_PADDING = 10.f;
	constexpr float RIVER_OBSTACLE_MARGIN = 5.f;

	// Phase 2: Replan Trigger Settings
	constexpr int32 REPLAN_STALL_THRESHOLD = 30;
	constexpr int32 REPLAN_AVOIDANCE_THRESHOLD = 60;
	constexpr int32 REPLAN_PERIODIC_INTERVAL = 300;
	constexpr float WAYPOINT_PROGRESS_THRESHOLD = 5.f;
	constexpr int32 REPLAN_COOLDOWN_FRAMES = 15;

	// Phase 3: Dynamic Obstacle Settings
	constexpr int32 DYNAMIC_OBSTACLE_DENSITY_THRESHOLD = 3;
	constexpr int32 DYNAMIC_OBSTACLE_UPDATE_INTERVAL = 15;

	// Phase 4: Path Smoothing Settings
	constexpr bool PATH_SMOOTHING_ENABLED = true;
	constexpr int32 PATH_SMOOTHING_MAX_SKIP = 10;

	// Phase 5: Debug Settings
	constexpr bool PATHFINDING_DEBUG_ENABLED = false;

	// Phase 6: Collision Resolution Settings (Body Blocking)
	constexpr int32 COLLISION_RESOLUTION_ITERATIONS = 3;
	constexpr float COLLISION_PUSH_STRENGTH = 0.8f;
}
