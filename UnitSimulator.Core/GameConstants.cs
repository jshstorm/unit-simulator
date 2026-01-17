namespace UnitSimulator;

/// <summary>
/// Game logic constants used by the simulation core.
/// These define game balance, unit stats, and simulation rules.
/// </summary>
public static class GameConstants
{
    // Simulation space (used for unit spawning and boundary checks)
    public const int SIMULATION_WIDTH = 3200;
    public const int SIMULATION_HEIGHT = 5100;
    public const int MAX_FRAMES = 3000;
    public const float FRAME_TIME_SECONDS = 1f / 30f;

    // Unit settings
    public const float UNIT_RADIUS = 20f;
    public const float COLLISION_RADIUS_SCALE = 2f / 3f;
    public const int NUM_ATTACK_SLOTS = 8;
    public const float SLOT_REEVALUATE_DISTANCE = 40f;
    public const int SLOT_REEVALUATE_INTERVAL_FRAMES = 60;
    public const int FRIENDLY_HP = 100;
    public const int ENEMY_HP = 10;

    // Combat settings
    public const float ATTACK_COOLDOWN = 30f;
    public const int FRIENDLY_ATTACK_DAMAGE = 1;
    public const int ENEMY_ATTACK_DAMAGE = 1;
    public const int MELEE_RANGE_MULTIPLIER = 3;
    public const int RANGED_RANGE_MULTIPLIER = 6;
    public const float ENGAGEMENT_TRIGGER_DISTANCE_MULTIPLIER = 1.5f;

    // Squad behavior settings
    public const float RALLY_DISTANCE = 300f;
    public const float FORMATION_THRESHOLD = 20f;
    public const float SEPARATION_RADIUS = 120f;
    public const float FRIENDLY_SEPARATION_RADIUS = 80f;
    public const float DESTINATION_THRESHOLD = 10f;

    // Wave settings
    public const int MAX_WAVES = 3;

    // Targeting settings (enemy)
    public const int TARGET_REEVALUATE_INTERVAL_FRAMES = 45;
    public const float TARGET_SWITCH_MARGIN = 15f;
    public const float TARGET_CROWD_PENALTY_PER_ATTACKER = 25f;

    // Avoidance settings
    public const float AVOIDANCE_ANGLE_STEP = MathF.PI / 8f; // 22.5 degrees
    public const int MAX_AVOIDANCE_ITERATIONS = 8;
    public const float AVOIDANCE_MAX_LOOKAHEAD = 3.5f;
    public const int AVOIDANCE_SEGMENT_COUNT = 3;
    public const float AVOIDANCE_SEGMENT_START_DISTANCE = 20f;
    public const float AVOIDANCE_LATERAL_PADDING = 25f;
    public const float AVOIDANCE_PARALLEL_DISTANCE_MULTIPLIER = 1.5f;
    public const float AVOIDANCE_WAYPOINT_THRESHOLD = 12f;

    // ════════════════════════════════════════════════════════════════════════
    // Phase 1: Static Obstacle Settings
    // ════════════════════════════════════════════════════════════════════════
    public const float TOWER_COLLISION_PADDING = 10f;
    public const float RIVER_OBSTACLE_MARGIN = 5f;

    // ════════════════════════════════════════════════════════════════════════
    // Phase 2: Replan Trigger Settings
    // ════════════════════════════════════════════════════════════════════════
    public const int REPLAN_STALL_THRESHOLD = 30;           // ~1 second at 30fps
    public const int REPLAN_AVOIDANCE_THRESHOLD = 60;       // ~2 seconds continuous avoidance
    public const int REPLAN_PERIODIC_INTERVAL = 300;        // ~10 seconds for long paths
    public const float WAYPOINT_PROGRESS_THRESHOLD = 5f;    // Min movement to count as progress
    public const int REPLAN_COOLDOWN_FRAMES = 15;           // Cooldown between replans

    // ════════════════════════════════════════════════════════════════════════
    // Phase 3: Dynamic Obstacle Settings
    // ════════════════════════════════════════════════════════════════════════
    public const int DYNAMIC_OBSTACLE_DENSITY_THRESHOLD = 3;    // Units per cell to block
    public const int DYNAMIC_OBSTACLE_UPDATE_INTERVAL = 15;     // Frames between updates

    // ════════════════════════════════════════════════════════════════════════
    // Phase 4: Path Smoothing Settings
    // ════════════════════════════════════════════════════════════════════════
    public const bool PATH_SMOOTHING_ENABLED = true;
    public const int PATH_SMOOTHING_MAX_SKIP = 10;              // Max waypoints to try skipping

    // ════════════════════════════════════════════════════════════════════════
    // Phase 5: Debug Settings
    // ════════════════════════════════════════════════════════════════════════
    public const bool PATHFINDING_DEBUG_ENABLED = false;
}
