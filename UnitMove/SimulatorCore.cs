using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// The core simulation engine.
/// 
/// SimulatorCore manages the simulation loop and state, providing a clean interface
/// for running simulations, capturing frame data, and integrating with external tools.
/// 
/// Key features:
/// - Separates simulation logic from rendering
/// - Generates frame data (JSON) independently of image generation
/// - Supports callbacks for external integrations (GUI tools, analyzers, etc.)
/// - Allows loading simulation state from saved frames
/// - Enables runtime state injection for interactive debugging
/// 
/// Usage:
/// <code>
/// var simulator = new SimulatorCore();
/// simulator.Initialize();
/// simulator.Run(new ConsoleLoggingCallbacks());
/// </code>
/// </summary>
public class SimulatorCore
{
    // ================================================================================
    // Private fields for simulation state
    // ================================================================================

    /// <summary>
    /// Counter for generating unique friendly unit IDs.
    /// </summary>
    private int _nextFriendlyId = 0;

    /// <summary>
    /// Counter for generating unique enemy unit IDs.
    /// </summary>
    private int _nextEnemyId = 0;

    /// <summary>
    /// The current simulation frame number.
    /// </summary>
    private int _currentFrame = 0;

    /// <summary>
    /// The main target position for the friendly squad.
    /// </summary>
    private Vector2 _mainTarget;

    /// <summary>
    /// List of all friendly units in the simulation.
    /// </summary>
    private List<Unit> _friendlySquad = new();

    /// <summary>
    /// List of all enemy units in the simulation.
    /// </summary>
    private List<Unit> _enemySquad = new();

    /// <summary>
    /// The wave manager responsible for spawning enemy waves.
    /// </summary>
    private WaveManager? _waveManager;

    /// <summary>
    /// Behavior controller for friendly units.
    /// </summary>
    private readonly SquadBehavior _squadBehavior = new();

    /// <summary>
    /// Behavior controller for enemy units.
    /// </summary>
    private readonly EnemyBehavior _enemyBehavior = new();

    /// <summary>
    /// Optional renderer for generating frame images.
    /// Can be null if running in headless mode.
    /// </summary>
    private Renderer? _renderer;

    /// <summary>
    /// Indicates whether the simulation has been initialized.
    /// </summary>
    private bool _isInitialized = false;

    /// <summary>
    /// Indicates whether the simulation is currently running.
    /// </summary>
    private bool _isRunning = false;

    /// <summary>
    /// Indicates whether rendering is enabled.
    /// </summary>
    private bool _renderingEnabled = true;

    // ================================================================================
    // Public properties for external access
    // ================================================================================

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public int CurrentFrame => _currentFrame;

    /// <summary>
    /// Gets whether the simulation has been initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets whether the simulation is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets or sets whether rendering is enabled.
    /// When disabled, no image files are generated (headless mode).
    /// </summary>
    public bool RenderingEnabled
    {
        get => _renderingEnabled;
        set => _renderingEnabled = value;
    }

    /// <summary>
    /// Gets a read-only list of friendly units.
    /// </summary>
    public IReadOnlyList<Unit> FriendlyUnits => _friendlySquad.AsReadOnly();

    /// <summary>
    /// Gets a read-only list of enemy units.
    /// </summary>
    public IReadOnlyList<Unit> EnemyUnits => _enemySquad.AsReadOnly();

    /// <summary>
    /// Gets the current wave number.
    /// </summary>
    public int CurrentWave => _waveManager?.CurrentWave ?? 0;

    /// <summary>
    /// Gets whether there are more waves to spawn.
    /// </summary>
    public bool HasMoreWaves => _waveManager?.HasMoreWaves ?? false;

    // ================================================================================
    // Initialization and Setup
    // ================================================================================

    /// <summary>
    /// Initializes the simulation with default settings.
    /// Sets up the environment, creates units, and prepares for running.
    /// </summary>
    public void Initialize()
    {
        SetupEnvironment();
        
        // Set main target on the right side of the simulation area
        _mainTarget = new Vector2(Constants.IMAGE_WIDTH - 100, Constants.IMAGE_HEIGHT / 2);

        // Create the friendly squad
        _friendlySquad = CreateFriendlySquad();

        // Initialize enemy squad (will be populated when waves spawn)
        _enemySquad = new List<Unit>();

        // Create wave manager with enemy ID provider
        _waveManager = new WaveManager(GetNextEnemyId);

        // Create renderer for image generation
        _renderer = new Renderer();

        // Spawn the first wave of enemies
        _waveManager.SpawnNextWave(_enemySquad);

        _isInitialized = true;
        _currentFrame = 0;

        Console.WriteLine("SimulatorCore initialized successfully.");
    }

    /// <summary>
    /// Sets up the output directory structure for the simulation.
    /// </summary>
    private void SetupEnvironment()
    {
        try
        {
            var dirInfo = new DirectoryInfo(Constants.OUTPUT_DIRECTORY);
            if (dirInfo.Exists) dirInfo.Delete(true);
            dirInfo.Create();

            var debugDirPath = Path.Combine(Constants.OUTPUT_DIRECTORY, Constants.DEBUG_SUBDIRECTORY);
            Directory.CreateDirectory(debugDirPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting up output directory: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates the initial friendly squad with default positions and roles.
    /// </summary>
    /// <returns>List of friendly units.</returns>
    private List<Unit> CreateFriendlySquad()
    {
        return new List<Unit>
        {
            CreateFriendlyUnit(new Vector2(200, Constants.IMAGE_HEIGHT / 2 - 45), UnitRole.Melee),
            CreateFriendlyUnit(new Vector2(200, Constants.IMAGE_HEIGHT / 2 + 45), UnitRole.Melee),
            CreateFriendlyUnit(new Vector2(120, Constants.IMAGE_HEIGHT / 2 - 75), UnitRole.Ranged),
            CreateFriendlyUnit(new Vector2(120, Constants.IMAGE_HEIGHT / 2 + 75), UnitRole.Ranged)
        };
    }

    /// <summary>
    /// Creates a single friendly unit with the specified position and role.
    /// </summary>
    private Unit CreateFriendlyUnit(Vector2 position, UnitRole role)
    {
        return new Unit(
            position,
            Constants.UNIT_RADIUS,
            4.5f,  // speed
            0.08f, // turn speed
            role,
            Constants.FRIENDLY_HP,
            GetNextFriendlyId(),
            UnitFaction.Friendly
        );
    }

    /// <summary>
    /// Generates the next unique friendly unit ID.
    /// </summary>
    private int GetNextFriendlyId() => ++_nextFriendlyId;

    /// <summary>
    /// Generates the next unique enemy unit ID.
    /// </summary>
    private int GetNextEnemyId() => ++_nextEnemyId;

    // ================================================================================
    // Simulation Running
    // ================================================================================

    /// <summary>
    /// Runs the complete simulation from current state to completion.
    /// </summary>
    /// <param name="callbacks">Optional callbacks for receiving simulation events.</param>
    public void Run(ISimulatorCallbacks? callbacks = null)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Simulator must be initialized before running. Call Initialize() first.");
        }

        _isRunning = true;
        callbacks ??= new DefaultSimulatorCallbacks();

        Console.WriteLine("Starting simulation...");

        while (_currentFrame < Constants.MAX_FRAMES && _isRunning)
        {
            // Run a single simulation step
            var frameData = Step(callbacks);

            // Check if simulation should end
            if (frameData.AllWavesCleared)
            {
                callbacks.OnSimulationComplete(_currentFrame, "AllWavesCleared");
                Console.WriteLine($"All enemy waves eliminated at frame {_currentFrame}.");
                break;
            }

            if (frameData.MaxFramesReached)
            {
                callbacks.OnSimulationComplete(_currentFrame, "MaxFramesReached");
                Console.WriteLine($"Maximum frames reached at frame {_currentFrame}.");
                break;
            }
        }

        _isRunning = false;
        Console.WriteLine($"\nFinished generating frames in '{Constants.OUTPUT_DIRECTORY}'.");
        Console.WriteLine($"ffmpeg -framerate 60 -i {Path.Combine(Constants.OUTPUT_DIRECTORY, "frame_%04d.png")} -c:v libx264 -pix_fmt yuv420p output.mp4");
    }

    /// <summary>
    /// Executes a single simulation step and returns the frame data.
    /// This is the core method for advancing the simulation.
    /// </summary>
    /// <param name="callbacks">Optional callbacks for receiving simulation events.</param>
    /// <returns>The frame data for this step.</returns>
    public FrameData Step(ISimulatorCallbacks? callbacks = null)
    {
        if (!_isInitialized || _waveManager == null)
        {
            throw new InvalidOperationException("Simulator must be initialized before stepping. Call Initialize() first.");
        }

        callbacks ??= new DefaultSimulatorCallbacks();

        // Handle wave progression
        HandleWaveProgression(callbacks);

        // Update enemy behavior
        _enemyBehavior.UpdateEnemySquad(_enemySquad, _friendlySquad);

        // Update friendly behavior
        _squadBehavior.UpdateFriendlySquad(_friendlySquad, _enemySquad, _mainTarget);

        // Generate frame data (separate from rendering)
        // Note: _waveManager is guaranteed non-null due to the check above
        var frameData = FrameData.FromSimulationState(
            _currentFrame,
            _friendlySquad,
            _enemySquad,
            _mainTarget,
            _waveManager
        );

        // Notify callbacks of frame generation
        callbacks.OnFrameGenerated(frameData);

        // Render frame image if enabled
        if (_renderingEnabled && _renderer != null)
        {
            _renderer.GenerateFrame(_currentFrame, _friendlySquad, _enemySquad, _mainTarget, _waveManager);
        }

        // Advance frame counter
        _currentFrame++;

        return frameData;
    }

    /// <summary>
    /// Handles wave progression logic (spawning new waves when current is cleared).
    /// </summary>
    private void HandleWaveProgression(ISimulatorCallbacks callbacks)
    {
        if (_waveManager == null) return;

        if (!_enemySquad.Any(e => !e.IsDead))
        {
            if (_waveManager.TryAdvanceWave())
            {
                // Clear attack slots on friendlies when wave changes
                _friendlySquad.ForEach(f => Array.Fill(f.AttackSlots, null));
                _waveManager.SpawnNextWave(_enemySquad);
                
                callbacks.OnStateChanged($"Wave {_waveManager.CurrentWave} spawned with {_enemySquad.Count} enemies.");
            }
        }
    }

    // ================================================================================
    // State Loading and Resuming
    // ================================================================================

    /// <summary>
    /// Loads simulation state from frame data.
    /// This allows resuming a simulation from a saved point.
    /// 
    /// Note: This creates a new simulation state based on the frame data.
    /// Some internal state (like behavior controllers) will be reset.
    /// </summary>
    /// <param name="frameData">The frame data to load.</param>
    /// <param name="callbacks">Optional callbacks for receiving state change events.</param>
    public void LoadState(FrameData frameData, ISimulatorCallbacks? callbacks = null)
    {
        if (frameData == null)
        {
            throw new ArgumentNullException(nameof(frameData));
        }

        callbacks ??= new DefaultSimulatorCallbacks();

        // Set frame number
        _currentFrame = frameData.FrameNumber;

        // Set main target
        _mainTarget = frameData.MainTarget.ToVector2();

        // Reconstruct friendly units from state data
        _friendlySquad = ReconstructUnits(frameData.FriendlyUnits, UnitFaction.Friendly);

        // Reconstruct enemy units from state data
        _enemySquad = ReconstructUnits(frameData.EnemyUnits, UnitFaction.Enemy);

        // Update ID counters to avoid conflicts
        _nextFriendlyId = _friendlySquad.Any() ? _friendlySquad.Max(u => u.Id) : 0;
        _nextEnemyId = _enemySquad.Any() ? _enemySquad.Max(u => u.Id) : 0;

        // Re-establish target references between units
        ReestablishTargetReferences();

        // Create wave manager at the current wave
        _waveManager = new WaveManager(GetNextEnemyId);
        // Note: Wave manager internal state cannot be fully reconstructed,
        // but the simulation can continue from this point

        _isInitialized = true;

        callbacks.OnStateChanged($"State loaded from frame {frameData.FrameNumber}");
        Console.WriteLine($"Simulation state loaded from frame {frameData.FrameNumber}.");
    }

    /// <summary>
    /// Reconstructs Unit objects from UnitStateData.
    /// </summary>
    private List<Unit> ReconstructUnits(List<UnitStateData> stateList, UnitFaction expectedFaction)
    {
        var units = new List<Unit>();

        foreach (var state in stateList)
        {
            var role = Enum.Parse<UnitRole>(state.Role);
            var faction = Enum.Parse<UnitFaction>(state.Faction);

            var unit = new Unit(
                state.Position.ToVector2(),
                state.Radius,
                state.Speed,
                state.TurnSpeed,
                role,
                state.HP,
                state.Id,
                faction
            );

            // Restore additional state
            unit.Velocity = state.Velocity.ToVector2();
            unit.Forward = state.Forward.ToVector2();
            unit.CurrentDestination = state.CurrentDestination.ToVector2();
            unit.AttackCooldown = state.AttackCooldown;
            unit.IsDead = state.IsDead;
            unit.TakenSlotIndex = state.TakenSlotIndex;
            unit.HasAvoidanceTarget = state.HasAvoidanceTarget;
            if (state.AvoidanceTarget != null)
            {
                unit.AvoidanceTarget = state.AvoidanceTarget.ToVector2();
            }

            units.Add(unit);
        }

        return units;
    }

    /// <summary>
    /// Re-establishes target references between units after loading state.
    /// </summary>
    private void ReestablishTargetReferences()
    {
        var allUnits = _friendlySquad.Concat(_enemySquad).ToList();
        var unitLookup = allUnits.ToDictionary(u => u.Id);

        // Note: Target references would need to be reconstructed based on
        // saved target IDs. For now, targets will be re-acquired by the
        // behavior systems on the next frame update.
    }

    // ================================================================================
    // State Injection (Runtime Modification)
    // ================================================================================

    /// <summary>
    /// Modifies a unit's state at runtime.
    /// This allows external tools to inject changes into the simulation.
    /// </summary>
    /// <param name="unitId">The ID of the unit to modify.</param>
    /// <param name="faction">The faction of the unit.</param>
    /// <param name="modifier">An action that modifies the unit.</param>
    /// <param name="callbacks">Optional callbacks for receiving state change events.</param>
    /// <returns>True if the unit was found and modified, false otherwise.</returns>
    public bool ModifyUnit(int unitId, UnitFaction faction, Action<Unit> modifier, ISimulatorCallbacks? callbacks = null)
    {
        callbacks ??= new DefaultSimulatorCallbacks();

        var squad = faction == UnitFaction.Friendly ? _friendlySquad : _enemySquad;
        var unit = squad.FirstOrDefault(u => u.Id == unitId);

        if (unit == null)
        {
            Console.WriteLine($"Unit {unitId} ({faction}) not found.");
            return false;
        }

        modifier(unit);
        callbacks.OnStateChanged($"Unit {unit.Label} modified at frame {_currentFrame}");

        return true;
    }

    /// <summary>
    /// Injects a new unit into the simulation.
    /// </summary>
    /// <param name="position">The position for the new unit.</param>
    /// <param name="role">The role of the new unit.</param>
    /// <param name="faction">The faction of the new unit.</param>
    /// <param name="hp">Optional custom HP value.</param>
    /// <param name="callbacks">Optional callbacks for receiving state change events.</param>
    /// <returns>The newly created unit.</returns>
    public Unit InjectUnit(Vector2 position, UnitRole role, UnitFaction faction, int? hp = null, ISimulatorCallbacks? callbacks = null)
    {
        callbacks ??= new DefaultSimulatorCallbacks();

        int id = faction == UnitFaction.Friendly ? GetNextFriendlyId() : GetNextEnemyId();
        int health = hp ?? (faction == UnitFaction.Friendly ? Constants.FRIENDLY_HP : Constants.ENEMY_HP);
        float speed = faction == UnitFaction.Friendly ? 4.5f : 4.0f;
        float turnSpeed = faction == UnitFaction.Friendly ? 0.08f : 0.1f;

        var unit = new Unit(position, Constants.UNIT_RADIUS, speed, turnSpeed, role, health, id, faction);

        var squad = faction == UnitFaction.Friendly ? _friendlySquad : _enemySquad;
        squad.Add(unit);

        callbacks.OnStateChanged($"Unit {unit.Label} injected at position ({position.X}, {position.Y})");
        callbacks.OnUnitEvent(new UnitEventData
        {
            EventType = UnitEventType.Spawned,
            UnitId = unit.Id,
            Faction = faction,
            FrameNumber = _currentFrame,
            Position = position
        });

        return unit;
    }

    /// <summary>
    /// Removes a unit from the simulation.
    /// </summary>
    /// <param name="unitId">The ID of the unit to remove.</param>
    /// <param name="faction">The faction of the unit.</param>
    /// <param name="callbacks">Optional callbacks for receiving state change events.</param>
    /// <returns>True if the unit was found and removed, false otherwise.</returns>
    public bool RemoveUnit(int unitId, UnitFaction faction, ISimulatorCallbacks? callbacks = null)
    {
        callbacks ??= new DefaultSimulatorCallbacks();

        var squad = faction == UnitFaction.Friendly ? _friendlySquad : _enemySquad;
        var unit = squad.FirstOrDefault(u => u.Id == unitId);

        if (unit == null)
        {
            Console.WriteLine($"Unit {unitId} ({faction}) not found.");
            return false;
        }

        // Release any slots the unit had claimed
        if (unit.Target != null)
        {
            unit.Target.ReleaseSlot(unit);
        }

        squad.Remove(unit);
        callbacks.OnStateChanged($"Unit {unit.Label} removed from simulation");

        return true;
    }

    /// <summary>
    /// Gets the current frame data without advancing the simulation.
    /// Useful for external tools that need to inspect the current state.
    /// </summary>
    /// <returns>The current frame data.</returns>
    public FrameData GetCurrentFrameData()
    {
        if (!_isInitialized || _waveManager == null)
        {
            throw new InvalidOperationException("Simulator must be initialized first.");
        }

        return FrameData.FromSimulationState(
            _currentFrame,
            _friendlySquad,
            _enemySquad,
            _mainTarget,
            _waveManager
        );
    }

    /// <summary>
    /// Stops the currently running simulation.
    /// The simulation can be resumed by calling Run() again.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        Console.WriteLine($"Simulation stopped at frame {_currentFrame}.");
    }

    /// <summary>
    /// Resets the simulation to its initial state.
    /// This is equivalent to calling Initialize() again.
    /// </summary>
    public void Reset()
    {
        _isRunning = false;
        _isInitialized = false;
        _currentFrame = 0;
        _nextFriendlyId = 0;
        _nextEnemyId = 0;
        _friendlySquad.Clear();
        _enemySquad.Clear();
        
        Initialize();
    }
}
