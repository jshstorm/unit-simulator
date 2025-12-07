using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// Placeholder class for integrating GUI or external visual tools with the simulator.
/// 
/// This class provides a structure for connecting external applications (such as 
/// desktop GUI tools, web interfaces, or game engines) to the simulation engine.
/// 
/// Usage:
/// <code>
/// var gui = new GuiIntegration(simulator);
/// gui.OnFrameRequest = (frameNumber) => HandleFrameRequest(frameNumber);
/// gui.OnStateModification = (changes) => ApplyChangesToGui(changes);
/// gui.Start();
/// </code>
/// 
/// TODO: Implement actual GUI integration based on your chosen framework:
/// - WPF/WinForms for desktop applications
/// - Avalonia for cross-platform desktop
/// - MAUI for mobile/desktop
/// - Unity/Godot for game engine integration
/// - Web API for browser-based tools
/// </summary>
public class GuiIntegration : ISimulatorCallbacks, IDisposable
{
    // ================================================================================
    // Private fields
    // ================================================================================

    /// <summary>
    /// Reference to the simulator core being controlled.
    /// </summary>
    private readonly SimulatorCore? _simulator;

    /// <summary>
    /// Buffer of recent frame data for playback.
    /// </summary>
    private readonly Queue<FrameData> _frameBuffer = new();

    /// <summary>
    /// Maximum number of frames to buffer.
    /// </summary>
    private const int MaxBufferSize = 100;

    /// <summary>
    /// Indicates whether the GUI integration is active.
    /// </summary>
    private bool _isActive = false;

    /// <summary>
    /// Indicates whether the object has been disposed.
    /// </summary>
    private bool _disposed = false;

    // ================================================================================
    // Events and Callbacks
    // ================================================================================

    /// <summary>
    /// Event raised when a new frame is available.
    /// GUI applications should subscribe to this to update their display.
    /// </summary>
    public event Action<FrameData>? OnNewFrame;

    /// <summary>
    /// Event raised when the simulation state changes.
    /// GUI applications should subscribe to this to update state indicators.
    /// </summary>
    public event Action<string>? OnStateChange;

    /// <summary>
    /// Event raised when a unit event occurs.
    /// GUI applications can use this for animations or sound effects.
    /// </summary>
    public event Action<UnitEventData>? OnUnitStateChange;

    /// <summary>
    /// Event raised when the simulation completes.
    /// </summary>
    public event Action<int, string>? OnSimulationEnd;

    // ================================================================================
    // Callback function properties
    // These can be set by external tools to receive simulation data.
    // ================================================================================

    /// <summary>
    /// Callback invoked when a specific frame is requested.
    /// Set this to handle frame requests from the GUI.
    /// </summary>
    public Func<int, FrameData?>? OnFrameRequest { get; set; }

    /// <summary>
    /// Callback invoked when state modifications are received from the GUI.
    /// Set this to apply user-initiated changes to the simulation.
    /// </summary>
    public Action<StateModificationRequest>? OnStateModification { get; set; }

    /// <summary>
    /// Callback invoked when playback control commands are received.
    /// Set this to handle play/pause/step commands from the GUI.
    /// </summary>
    public Action<PlaybackAction>? OnPlaybackControl { get; set; }

    // ================================================================================
    // Constructors
    // ================================================================================

    /// <summary>
    /// Creates a new GUI integration instance without a simulator reference.
    /// Use this when the simulator will be provided later.
    /// </summary>
    public GuiIntegration()
    {
        _simulator = null;
    }

    /// <summary>
    /// Creates a new GUI integration instance with a simulator reference.
    /// </summary>
    /// <param name="simulator">The simulator to integrate with.</param>
    public GuiIntegration(SimulatorCore simulator)
    {
        _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
    }

    // ================================================================================
    // ISimulatorCallbacks Implementation
    // ================================================================================

    /// <inheritdoc />
    public void OnFrameGenerated(FrameData frameData)
    {
        // Add to buffer
        _frameBuffer.Enqueue(frameData);
        while (_frameBuffer.Count > MaxBufferSize)
        {
            _frameBuffer.Dequeue();
        }

        // Raise event for GUI subscribers
        OnNewFrame?.Invoke(frameData);
    }

    /// <inheritdoc />
    public void OnSimulationComplete(int finalFrameNumber, string reason)
    {
        OnSimulationEnd?.Invoke(finalFrameNumber, reason);
    }

    /// <inheritdoc />
    public void OnStateChanged(string changeDescription)
    {
        OnStateChange?.Invoke(changeDescription);
    }

    /// <inheritdoc />
    public void OnUnitEvent(UnitEventData eventData)
    {
        OnUnitStateChange?.Invoke(eventData);
    }

    // ================================================================================
    // Public API for GUI Integration
    // ================================================================================

    /// <summary>
    /// Starts the GUI integration.
    /// Call this after setting up all event handlers.
    /// </summary>
    public void Start()
    {
        _isActive = true;
        Console.WriteLine("[GuiIntegration] Started. Waiting for GUI connections...");
        
        // TODO: Implement actual connection handling for your GUI framework
        // This could involve:
        // - Starting a local web server for browser-based GUIs
        // - Setting up IPC for desktop applications
        // - Initializing shared memory for game engine integrations
    }

    /// <summary>
    /// Stops the GUI integration.
    /// </summary>
    public void Stop()
    {
        _isActive = false;
        Console.WriteLine("[GuiIntegration] Stopped.");
    }

    /// <summary>
    /// Gets whether the GUI integration is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Gets a frame from the buffer by frame number.
    /// Returns null if the frame is not in the buffer.
    /// </summary>
    /// <param name="frameNumber">The frame number to retrieve.</param>
    /// <returns>The frame data, or null if not found.</returns>
    public FrameData? GetBufferedFrame(int frameNumber)
    {
        return _frameBuffer.FirstOrDefault(f => f.FrameNumber == frameNumber);
    }

    /// <summary>
    /// Gets all frames currently in the buffer.
    /// </summary>
    /// <returns>Array of buffered frame data.</returns>
    public FrameData[] GetBufferedFrames()
    {
        return _frameBuffer.ToArray();
    }

    /// <summary>
    /// Clears the frame buffer.
    /// </summary>
    public void ClearBuffer()
    {
        _frameBuffer.Clear();
    }

    // ================================================================================
    // Simulation Control Methods (for GUI to call)
    // ================================================================================

    /// <summary>
    /// Requests the simulation to advance by one frame.
    /// </summary>
    public void StepSimulation()
    {
        if (_simulator == null)
        {
            Console.WriteLine("[GuiIntegration] No simulator attached.");
            return;
        }

        if (!_simulator.IsInitialized)
        {
            Console.WriteLine("[GuiIntegration] Simulator not initialized.");
            return;
        }

        _simulator.Step(this);
    }

    /// <summary>
    /// Requests the simulation to run continuously.
    /// This is a blocking call that runs until the simulation completes.
    /// </summary>
    public void RunSimulation()
    {
        if (_simulator == null)
        {
            Console.WriteLine("[GuiIntegration] No simulator attached.");
            return;
        }

        if (!_simulator.IsInitialized)
        {
            Console.WriteLine("[GuiIntegration] Simulator not initialized.");
            return;
        }

        _simulator.Run(this);
    }

    /// <summary>
    /// Stops the currently running simulation.
    /// </summary>
    public void StopSimulation()
    {
        _simulator?.Stop();
    }

    /// <summary>
    /// Resets the simulation to its initial state.
    /// </summary>
    public void ResetSimulation()
    {
        _simulator?.Reset();
    }

    /// <summary>
    /// Modifies a unit in the simulation based on a modification request.
    /// </summary>
    /// <param name="request">The modification request from the GUI.</param>
    public void ApplyModification(StateModificationRequest request)
    {
        if (_simulator == null)
        {
            Console.WriteLine("[GuiIntegration] No simulator attached.");
            return;
        }

        switch (request.ModificationType)
        {
            case ModificationType.SetPosition:
                if (request.NewPosition.HasValue)
                {
                    _simulator.ModifyUnit(request.UnitId, request.Faction, unit =>
                    {
                        unit.Position = request.NewPosition.Value;
                    }, this);
                }
                break;

            case ModificationType.SetHealth:
                if (request.NewHealth.HasValue)
                {
                    _simulator.ModifyUnit(request.UnitId, request.Faction, unit =>
                    {
                        unit.HP = request.NewHealth.Value;
                    }, this);
                }
                break;

            case ModificationType.Kill:
                _simulator.ModifyUnit(request.UnitId, request.Faction, unit =>
                {
                    unit.IsDead = true;
                    unit.HP = 0;
                }, this);
                break;

            case ModificationType.Revive:
                if (request.NewHealth.HasValue)
                {
                    _simulator.ModifyUnit(request.UnitId, request.Faction, unit =>
                    {
                        unit.IsDead = false;
                        unit.HP = request.NewHealth.Value;
                    }, this);
                }
                break;

            case ModificationType.InjectUnit:
                if (request.NewPosition.HasValue && request.Role.HasValue)
                {
                    _simulator.InjectUnit(
                        request.NewPosition.Value,
                        request.Role.Value,
                        request.Faction,
                        request.NewHealth,
                        this
                    );
                }
                break;

            case ModificationType.RemoveUnit:
                _simulator.RemoveUnit(request.UnitId, request.Faction, this);
                break;
        }
    }

    // ================================================================================
    // IDisposable Implementation
    // ================================================================================

    /// <summary>
    /// Releases resources used by the GUI integration.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Stop();
                _frameBuffer.Clear();
            }
            _disposed = true;
        }
    }
}

// ================================================================================
// Supporting Types for GUI Integration
// ================================================================================

/// <summary>
/// Represents a request to modify simulation state from the GUI.
/// </summary>
public class StateModificationRequest
{
    /// <summary>
    /// The type of modification to apply.
    /// </summary>
    public ModificationType ModificationType { get; set; }

    /// <summary>
    /// The ID of the unit to modify.
    /// </summary>
    public int UnitId { get; set; }

    /// <summary>
    /// The faction of the unit to modify.
    /// </summary>
    public UnitFaction Faction { get; set; }

    /// <summary>
    /// Optional new position for the unit.
    /// </summary>
    public Vector2? NewPosition { get; set; }

    /// <summary>
    /// Optional new health value for the unit.
    /// </summary>
    public int? NewHealth { get; set; }

    /// <summary>
    /// Optional role for new units.
    /// </summary>
    public UnitRole? Role { get; set; }
}

/// <summary>
/// Types of modifications that can be applied to the simulation.
/// </summary>
public enum ModificationType
{
    /// <summary>Set the unit's position.</summary>
    SetPosition,
    /// <summary>Set the unit's health.</summary>
    SetHealth,
    /// <summary>Kill the unit.</summary>
    Kill,
    /// <summary>Revive the unit.</summary>
    Revive,
    /// <summary>Inject a new unit.</summary>
    InjectUnit,
    /// <summary>Remove a unit from the simulation.</summary>
    RemoveUnit
}

/// <summary>
/// Playback control actions that can be sent from the GUI.
/// </summary>
public enum PlaybackAction
{
    /// <summary>Start or resume playback.</summary>
    Play,
    /// <summary>Pause playback.</summary>
    Pause,
    /// <summary>Stop playback and reset.</summary>
    Stop,
    /// <summary>Advance one frame.</summary>
    StepForward,
    /// <summary>Go back one frame (if supported).</summary>
    StepBackward,
    /// <summary>Jump to a specific frame.</summary>
    JumpToFrame,
    /// <summary>Set playback speed.</summary>
    SetSpeed
}
