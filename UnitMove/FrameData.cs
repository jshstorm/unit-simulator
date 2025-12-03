using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnitSimulator;

/// <summary>
/// Represents the complete state of a simulation frame.
/// This data structure captures all information needed to:
/// - Render the frame visually
/// - Save/load simulation state
/// - Resume simulation from a specific point
/// - Analyze simulation behavior
/// 
/// Frame data is serializable to JSON and can be used independently of rendering.
/// </summary>
public class FrameData
{
    /// <summary>
    /// JSON serialization options for consistent formatting.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// The frame number in the simulation sequence.
    /// </summary>
    public int FrameNumber { get; set; }

    /// <summary>
    /// The current wave number.
    /// </summary>
    public int CurrentWave { get; set; }

    /// <summary>
    /// Number of living friendly units.
    /// </summary>
    public int LivingFriendlyCount { get; set; }

    /// <summary>
    /// Number of living enemy units.
    /// </summary>
    public int LivingEnemyCount { get; set; }

    /// <summary>
    /// The main target position for the friendly squad.
    /// </summary>
    public SerializableVector2 MainTarget { get; set; } = new();

    /// <summary>
    /// State data for all friendly units.
    /// </summary>
    public List<UnitStateData> FriendlyUnits { get; set; } = new();

    /// <summary>
    /// State data for all enemy units.
    /// </summary>
    public List<UnitStateData> EnemyUnits { get; set; } = new();

    /// <summary>
    /// Indicates whether all waves have been cleared.
    /// </summary>
    public bool AllWavesCleared { get; set; }

    /// <summary>
    /// Indicates whether the simulation has reached the maximum frame count.
    /// </summary>
    public bool MaxFramesReached { get; set; }

    /// <summary>
    /// Creates a FrameData instance from the current simulation state.
    /// This is the primary method for capturing frame data during simulation.
    /// </summary>
    /// <param name="frameNumber">The current frame number.</param>
    /// <param name="friendlies">List of friendly units.</param>
    /// <param name="enemies">List of enemy units.</param>
    /// <param name="mainTarget">The main target position.</param>
    /// <param name="waveManager">The wave manager instance.</param>
    /// <returns>A new FrameData instance containing the current state.</returns>
    public static FrameData FromSimulationState(
        int frameNumber,
        List<Unit> friendlies,
        List<Unit> enemies,
        Vector2 mainTarget,
        WaveManager waveManager)
    {
        var frameData = new FrameData
        {
            FrameNumber = frameNumber,
            CurrentWave = waveManager.CurrentWave,
            LivingFriendlyCount = friendlies.Count(f => !f.IsDead),
            LivingEnemyCount = enemies.Count(e => !e.IsDead),
            MainTarget = new SerializableVector2(mainTarget),
            FriendlyUnits = friendlies.Select(UnitStateData.FromUnit).ToList(),
            EnemyUnits = enemies.Select(UnitStateData.FromUnit).ToList(),
            AllWavesCleared = !waveManager.HasMoreWaves && !enemies.Any(e => !e.IsDead),
            MaxFramesReached = frameNumber >= Constants.MAX_FRAMES - 1
        };

        return frameData;
    }

    /// <summary>
    /// Serializes the frame data to a JSON string.
    /// </summary>
    /// <returns>JSON representation of the frame data.</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    /// <summary>
    /// Saves the frame data to a JSON file.
    /// </summary>
    /// <param name="filePath">The path where the JSON file will be saved.</param>
    public void SaveToJson(string filePath)
    {
        var json = ToJson();
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads frame data from a JSON string.
    /// </summary>
    /// <param name="json">JSON string containing frame data.</param>
    /// <returns>Deserialized FrameData instance.</returns>
    public static FrameData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<FrameData>(json, JsonOptions);
    }

    /// <summary>
    /// Loads frame data from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <returns>Deserialized FrameData instance, or null if file doesn't exist or is invalid.</returns>
    public static FrameData? LoadFromJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Frame data file not found: {filePath}");
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return FromJson(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading frame data from {filePath}: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Represents the complete state of a single unit.
/// Contains all information needed to recreate the unit's state.
/// </summary>
public class UnitStateData
{
    /// <summary>
    /// The unit's unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The unit's display label (e.g., "F1" for friendly 1, "E3" for enemy 3).
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The unit's role (Melee or Ranged).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The unit's faction (Friendly or Enemy).
    /// </summary>
    public string Faction { get; set; } = string.Empty;

    /// <summary>
    /// Whether the unit is dead.
    /// </summary>
    public bool IsDead { get; set; }

    /// <summary>
    /// The unit's current health points.
    /// </summary>
    public int HP { get; set; }

    /// <summary>
    /// The unit's radius (collision size).
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// The unit's movement speed.
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// The unit's turn speed.
    /// </summary>
    public float TurnSpeed { get; set; }

    /// <summary>
    /// The unit's attack range.
    /// </summary>
    public float AttackRange { get; set; }

    /// <summary>
    /// The unit's current attack cooldown.
    /// </summary>
    public float AttackCooldown { get; set; }

    /// <summary>
    /// The unit's current position.
    /// </summary>
    public SerializableVector2 Position { get; set; } = new();

    /// <summary>
    /// The unit's current velocity.
    /// </summary>
    public SerializableVector2 Velocity { get; set; } = new();

    /// <summary>
    /// The unit's forward facing direction.
    /// </summary>
    public SerializableVector2 Forward { get; set; } = new();

    /// <summary>
    /// The unit's current destination.
    /// </summary>
    public SerializableVector2 CurrentDestination { get; set; } = new();

    /// <summary>
    /// The ID of the unit's current target, or null if no target.
    /// </summary>
    public int? TargetId { get; set; }

    /// <summary>
    /// The index of the attack slot the unit has claimed on its target.
    /// </summary>
    public int TakenSlotIndex { get; set; } = -1;

    /// <summary>
    /// Whether the unit has an active avoidance target.
    /// </summary>
    public bool HasAvoidanceTarget { get; set; }

    /// <summary>
    /// The unit's current avoidance target position.
    /// </summary>
    public SerializableVector2? AvoidanceTarget { get; set; }

    /// <summary>
    /// Whether the unit is currently moving.
    /// </summary>
    public bool IsMoving { get; set; }

    /// <summary>
    /// Whether the unit is currently in attack range of its target.
    /// </summary>
    public bool InAttackRange { get; set; }

    /// <summary>
    /// Creates a UnitStateData instance from a Unit object.
    /// </summary>
    /// <param name="unit">The unit to capture state from.</param>
    /// <returns>A new UnitStateData instance.</returns>
    public static UnitStateData FromUnit(Unit unit)
    {
        bool isMoving = unit.Velocity.LengthSquared() > 0.01f;
        bool inRange = unit.Target != null && !unit.Target.IsDead &&
            Vector2.Distance(unit.Position, unit.Target.Position) <= unit.AttackRange;

        return new UnitStateData
        {
            Id = unit.Id,
            Label = unit.Label,
            Role = unit.Role.ToString(),
            Faction = unit.Faction.ToString(),
            IsDead = unit.IsDead,
            HP = unit.HP,
            Radius = unit.Radius,
            Speed = unit.Speed,
            TurnSpeed = unit.TurnSpeed,
            AttackRange = unit.AttackRange,
            AttackCooldown = unit.AttackCooldown,
            Position = new SerializableVector2(unit.Position),
            Velocity = new SerializableVector2(unit.Velocity),
            Forward = new SerializableVector2(unit.Forward),
            CurrentDestination = new SerializableVector2(unit.CurrentDestination),
            TargetId = unit.Target?.Id,
            TakenSlotIndex = unit.TakenSlotIndex,
            HasAvoidanceTarget = unit.HasAvoidanceTarget,
            AvoidanceTarget = unit.HasAvoidanceTarget && unit.AvoidanceTarget != Vector2.Zero
                ? new SerializableVector2(unit.AvoidanceTarget)
                : null,
            IsMoving = isMoving,
            InAttackRange = inRange
        };
    }
}

/// <summary>
/// A serializable wrapper for Vector2, since System.Numerics.Vector2 
/// doesn't serialize well with System.Text.Json by default.
/// </summary>
public class SerializableVector2
{
    /// <summary>
    /// The X component of the vector.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The Y component of the vector.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Default constructor for deserialization.
    /// </summary>
    public SerializableVector2() { }

    /// <summary>
    /// Creates a SerializableVector2 from a Vector2.
    /// </summary>
    /// <param name="vector">The vector to convert.</param>
    public SerializableVector2(Vector2 vector)
    {
        X = vector.X;
        Y = vector.Y;
    }

    /// <summary>
    /// Converts back to a Vector2.
    /// </summary>
    /// <returns>A Vector2 with the same X and Y values.</returns>
    public Vector2 ToVector2()
    {
        return new Vector2(X, Y);
    }

    /// <summary>
    /// Implicit conversion to Vector2.
    /// </summary>
    public static implicit operator Vector2(SerializableVector2 sv)
    {
        return sv.ToVector2();
    }

    /// <summary>
    /// Implicit conversion from Vector2.
    /// </summary>
    public static implicit operator SerializableVector2(Vector2 v)
    {
        return new SerializableVector2(v);
    }
}
