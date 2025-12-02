namespace UnitSimulator;

public static class Constants
{
    // Simulation settings
    public const int IMAGE_WIDTH = 2000;
    public const int IMAGE_HEIGHT = 1000;
    public const int MAX_FRAMES = 3000;
    public const string OUTPUT_DIRECTORY = "output";
    public const string DEBUG_SUBDIRECTORY = "debug";
    
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
}
