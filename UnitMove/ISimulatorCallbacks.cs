using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// Interface for simulator callback handlers.
/// Implement this interface to receive notifications about simulation events.
/// This enables external tools (such as GUI applications) to react to simulation state changes.
/// </summary>
public interface ISimulatorCallbacks
{
    /// <summary>
    /// Called after each simulation frame is computed.
    /// Use this to capture frame data, update visualizations, or perform per-frame processing.
    /// </summary>
    /// <param name="frameData">The complete state of the simulation for this frame.</param>
    void OnFrameGenerated(FrameData frameData);

    /// <summary>
    /// Called when the simulation completes (all waves cleared or max frames reached).
    /// Use this for cleanup, final reporting, or triggering post-simulation actions.
    /// </summary>
    /// <param name="finalFrameNumber">The last frame number of the simulation.</param>
    /// <param name="reason">The reason the simulation ended (e.g., "AllWavesCleared", "MaxFramesReached").</param>
    void OnSimulationComplete(int finalFrameNumber, string reason);

    /// <summary>
    /// Called when simulation state is modified externally (e.g., through LoadState or ModifyUnit).
    /// Use this to synchronize external systems with the current simulation state.
    /// </summary>
    /// <param name="changeDescription">A description of what changed in the simulation state.</param>
    void OnStateChanged(string changeDescription);

    /// <summary>
    /// Called when a significant unit event occurs during simulation.
    /// Use this to track important events like unit deaths, attacks, or state transitions.
    /// </summary>
    /// <param name="eventData">Data describing the unit event.</param>
    void OnUnitEvent(UnitEventData eventData);
}

/// <summary>
/// Data structure representing a unit event in the simulation.
/// Used to communicate significant unit actions to callback handlers.
/// </summary>
public class UnitEventData
{
    /// <summary>
    /// The type of event that occurred.
    /// </summary>
    public UnitEventType EventType { get; set; }

    /// <summary>
    /// The ID of the unit that triggered the event.
    /// </summary>
    public int UnitId { get; set; }

    /// <summary>
    /// The faction of the unit that triggered the event.
    /// </summary>
    public UnitFaction Faction { get; set; }

    /// <summary>
    /// The frame number when this event occurred.
    /// </summary>
    public int FrameNumber { get; set; }

    /// <summary>
    /// Optional: The ID of another unit involved in the event (e.g., target of an attack).
    /// </summary>
    public int? TargetUnitId { get; set; }

    /// <summary>
    /// Optional: Additional data about the event (e.g., damage amount).
    /// </summary>
    public int? Value { get; set; }

    /// <summary>
    /// Optional: Position where the event occurred.
    /// </summary>
    public Vector2? Position { get; set; }
}

/// <summary>
/// Enumeration of possible unit events in the simulation.
/// </summary>
public enum UnitEventType
{
    /// <summary>Unit was created/spawned.</summary>
    Spawned,
    /// <summary>Unit died.</summary>
    Died,
    /// <summary>Unit attacked another unit.</summary>
    Attack,
    /// <summary>Unit took damage.</summary>
    Damaged,
    /// <summary>Unit acquired a new target.</summary>
    TargetAcquired,
    /// <summary>Unit lost its target.</summary>
    TargetLost,
    /// <summary>Unit started moving.</summary>
    MovementStarted,
    /// <summary>Unit stopped moving.</summary>
    MovementStopped,
    /// <summary>Unit entered combat range.</summary>
    EnteredCombat,
    /// <summary>Unit exited combat range.</summary>
    ExitedCombat
}

/// <summary>
/// A default implementation of ISimulatorCallbacks that does nothing.
/// Use this as a base class or when no callbacks are needed.
/// </summary>
public class DefaultSimulatorCallbacks : ISimulatorCallbacks
{
    /// <inheritdoc />
    public virtual void OnFrameGenerated(FrameData frameData)
    {
        // Default: no action
    }

    /// <inheritdoc />
    public virtual void OnSimulationComplete(int finalFrameNumber, string reason)
    {
        // Default: no action
    }

    /// <inheritdoc />
    public virtual void OnStateChanged(string changeDescription)
    {
        // Default: no action
    }

    /// <inheritdoc />
    public virtual void OnUnitEvent(UnitEventData eventData)
    {
        // Default: no action
    }
}

/// <summary>
/// A callback implementation that logs events to the console.
/// Useful for debugging and development.
/// </summary>
public class ConsoleLoggingCallbacks : DefaultSimulatorCallbacks
{
    /// <inheritdoc />
    public override void OnFrameGenerated(FrameData frameData)
    {
        // Only log every 100 frames to avoid excessive output
        if (frameData.FrameNumber % 100 == 0)
        {
            Console.WriteLine($"[Frame {frameData.FrameNumber}] Wave: {frameData.CurrentWave}, " +
                $"Friendlies: {frameData.LivingFriendlyCount}, Enemies: {frameData.LivingEnemyCount}");
        }
    }

    /// <inheritdoc />
    public override void OnSimulationComplete(int finalFrameNumber, string reason)
    {
        Console.WriteLine($"[Simulation Complete] Final frame: {finalFrameNumber}, Reason: {reason}");
    }

    /// <inheritdoc />
    public override void OnStateChanged(string changeDescription)
    {
        Console.WriteLine($"[State Changed] {changeDescription}");
    }

    /// <inheritdoc />
    public override void OnUnitEvent(UnitEventData eventData)
    {
        Console.WriteLine($"[Unit Event] {eventData.EventType} - Unit {eventData.UnitId} ({eventData.Faction}) at frame {eventData.FrameNumber}");
    }
}
