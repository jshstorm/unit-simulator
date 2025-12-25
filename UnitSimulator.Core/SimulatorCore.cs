using System.Numerics;
using System.Linq;
using UnitSimulator.Core.Pathfinding;

namespace UnitSimulator;

/// <summary>
/// The core simulation engine.
///
/// SimulatorCore manages the simulation loop and state, providing a clean interface
/// for running simulations, capturing frame data, and integrating with external tools.
///
/// Key features:
/// - Pure simulation logic with no rendering dependencies
/// - Command Queue for external control (spawning, state changes)
/// - Supports callbacks for external integrations (GUI tools, analyzers, etc.)
/// - Allows loading simulation state from saved frames
/// - Enables runtime state injection for interactive debugging
///
/// Usage:
/// <code>
/// var simulator = new SimulatorCore();
/// simulator.Initialize();
/// simulator.EnqueueCommands(waveManager.GetWaveCommands(1, 0));
/// simulator.Run(new ConsoleLoggingCallbacks());
/// </code>
/// </summary>
public class SimulatorCore
{
    // ================================================================================
    // Private fields for simulation state
    // ================================================================================

    private int _nextFriendlyId = 0;
    private int _nextEnemyId = 0;
    private int _currentFrame = 0;
    private Vector2 _mainTarget;
    private List<Unit> _friendlySquad = new();
    private List<Unit> _enemySquad = new();
    private readonly SquadBehavior _squadBehavior = new();
    private readonly EnemyBehavior _enemyBehavior = new();
    private bool _isInitialized = false;
    private bool _isRunning = false;
    private readonly List<UnitSpawnRequest> _pendingSpawns = new();

    // Pathfinding System
    private PathfindingGrid? _pathfindingGrid;
    private AStarPathfinder? _pathfinder;

    /// <summary>
    /// Command queue for external control of the simulation.
    /// Commands are processed at the start of each frame.
    /// </summary>
    private readonly Queue<ISimulationCommand> _commandQueue = new();

    /// <summary>
    /// Current wave number (managed externally via commands).
    /// </summary>
    private int _currentWave = 0;

    /// <summary>
    /// Whether there are more waves (managed externally).
    /// </summary>
    private bool _hasMoreWaves = true;

    // ================================================================================
    // Public properties for external access
    // ================================================================================

    public int CurrentFrame => _currentFrame;
    public bool IsInitialized => _isInitialized;
    public bool IsRunning => _isRunning;
    public IReadOnlyList<Unit> FriendlyUnits => _friendlySquad.AsReadOnly();
    public IReadOnlyList<Unit> EnemyUnits => _enemySquad.AsReadOnly();
    public Vector2 MainTarget => _mainTarget;
    public PathfindingGrid? PathfindingGrid => _pathfindingGrid;
    public AStarPathfinder? Pathfinder => _pathfinder;

    /// <summary>
    /// Gets or sets the current wave number.
    /// This is managed externally by the wave system.
    /// </summary>
    public int CurrentWave
    {
        get => _currentWave;
        set => _currentWave = value;
    }

    /// <summary>
    /// Gets or sets whether there are more waves to spawn.
    /// This is managed externally by the wave system.
    /// </summary>
    public bool HasMoreWaves
    {
        get => _hasMoreWaves;
        set => _hasMoreWaves = value;
    }

    /// <summary>
    /// Returns true if all enemies are dead.
    /// </summary>
    public bool AllEnemiesDead => !_enemySquad.Any(e => !e.IsDead);

    // ================================================================================
    // Command Queue
    // ================================================================================

    /// <summary>
    /// Enqueues a command to be processed at the specified frame.
    /// </summary>
    public void EnqueueCommand(ISimulationCommand command)
    {
        _commandQueue.Enqueue(command);
    }

    /// <summary>
    /// Enqueues multiple commands.
    /// </summary>
    public void EnqueueCommands(IEnumerable<ISimulationCommand> commands)
    {
        foreach (var cmd in commands)
            _commandQueue.Enqueue(cmd);
    }

    /// <summary>
    /// Processes all commands scheduled for the current frame or earlier.
    /// </summary>
    private void ProcessCommands(ISimulatorCallbacks callbacks)
    {
        // Process commands that should execute at or before current frame
        while (_commandQueue.TryPeek(out var cmd) && cmd.FrameNumber <= _currentFrame)
        {
            _commandQueue.Dequeue();
            ExecuteCommand(cmd, callbacks);
        }
    }

    /// <summary>
    /// Executes a single command.
    /// </summary>
    private void ExecuteCommand(ISimulationCommand cmd, ISimulatorCallbacks callbacks)
    {
        switch (cmd)
        {
            case SpawnUnitCommand spawn:
                var unit = InjectUnit(
                    spawn.Position,
                    spawn.Role,
                    spawn.Faction,
                    spawn.HP,
                    spawn.Speed,
                    spawn.TurnSpeed,
                    callbacks
                );
                break;

            case DamageUnitCommand damage:
                ModifyUnit(damage.UnitId, damage.Faction, u => u.TakeDamage(damage.Damage), callbacks);
                break;

            case KillUnitCommand kill:
                ModifyUnit(kill.UnitId, kill.Faction, u =>
                {
                    u.HP = 0;
                    u.IsDead = true;
                    u.Velocity = Vector2.Zero;
                }, callbacks);
                break;

            case RemoveUnitCommand remove:
                RemoveUnit(remove.UnitId, remove.Faction, callbacks);
                break;

            case MoveUnitCommand move:
                ModifyUnit(move.UnitId, move.Faction, u => u.CurrentDestination = move.Destination, callbacks);
                break;

            case ReviveUnitCommand revive:
                ModifyUnit(revive.UnitId, revive.Faction, u =>
                {
                    u.HP = revive.HP;
                    u.IsDead = false;
                }, callbacks);
                break;

            case SetUnitHealthCommand setHealth:
                ModifyUnit(setHealth.UnitId, setHealth.Faction, u =>
                {
                    u.HP = setHealth.HP;
                    u.IsDead = setHealth.HP <= 0;
                    if (u.IsDead)
                    {
                        u.Velocity = Vector2.Zero;
                    }
                }, callbacks);
                break;
        }
    }

    // ================================================================================
    // Initialization and Setup
    // ================================================================================

    /// <summary>
    /// Initializes the simulation with default settings.
    /// Creates the friendly squad but does NOT spawn enemies.
    /// Enemies should be spawned via commands from an external wave manager.
    /// </summary>
    public void Initialize()
    {
        // Set main target on the right side of the simulation area
        _mainTarget = new Vector2(GameConstants.SIMULATION_WIDTH - 100, GameConstants.SIMULATION_HEIGHT / 2);

        // Create the friendly squad
        _friendlySquad = CreateFriendlySquad();

        // Initialize empty enemy squad (will be populated via commands)
        _enemySquad = new List<Unit>();

        // Initialize Pathfinding
        _pathfindingGrid = new PathfindingGrid(GameConstants.SIMULATION_WIDTH, GameConstants.SIMULATION_HEIGHT, GameConstants.UNIT_RADIUS);
        _pathfinder = new AStarPathfinder(_pathfindingGrid);

        _isInitialized = true;
        _currentFrame = 0;
        _currentWave = 0;
        _hasMoreWaves = true;

        Console.WriteLine("SimulatorCore initialized successfully.");
    }

    private List<Unit> CreateFriendlySquad()
    {
        return new List<Unit>
        {
            CreateFriendlyUnit(new Vector2(200, GameConstants.SIMULATION_HEIGHT / 2 - 45), UnitRole.Melee),
            CreateFriendlyUnit(new Vector2(200, GameConstants.SIMULATION_HEIGHT / 2 + 45), UnitRole.Melee),
            CreateFriendlyUnit(new Vector2(120, GameConstants.SIMULATION_HEIGHT / 2 - 75), UnitRole.Ranged),
            CreateFriendlyUnit(new Vector2(120, GameConstants.SIMULATION_HEIGHT / 2 + 75), UnitRole.Ranged)
        };
    }

    private Unit CreateFriendlyUnit(Vector2 position, UnitRole role)
    {
        return new Unit(
            position,
            GameConstants.UNIT_RADIUS,
            4.5f,
            0.08f,
            role,
            GameConstants.FRIENDLY_HP,
            GetNextFriendlyId(),
            UnitFaction.Friendly
        );
    }

    private int GetNextFriendlyId() => ++_nextFriendlyId;
    private int GetNextEnemyId() => ++_nextEnemyId;

    // ================================================================================
    // Simulation Running
    // ================================================================================

    /// <summary>
    /// Runs the complete simulation from current state to completion.
    /// Note: Without external wave management, this will run until max frames.
    /// </summary>
    public void Run(ISimulatorCallbacks? callbacks = null)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Simulator must be initialized before running. Call Initialize() first.");
        }

        _isRunning = true;
        callbacks ??= new DefaultSimulatorCallbacks();

        Console.WriteLine("Starting simulation...");

        while (_currentFrame < GameConstants.MAX_FRAMES && _isRunning)
        {
            var frameData = Step(callbacks);

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
    }

    /// <summary>
    /// Executes a single simulation step and returns the frame data.
    /// </summary>
    public FrameData Step(ISimulatorCallbacks? callbacks = null)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Simulator must be initialized before stepping. Call Initialize() first.");
        }

        callbacks ??= new DefaultSimulatorCallbacks();

        // Process queued commands first
        ProcessCommands(callbacks);

        // Update enemy behavior
        _enemyBehavior.UpdateEnemySquad(this, _enemySquad, _friendlySquad);

        // Update friendly behavior
        _squadBehavior.UpdateFriendlySquad(this, _friendlySquad, _enemySquad, _mainTarget);

        // Apply delayed spawns after combat resolution
        ApplyPendingSpawns(callbacks);

        // Generate frame data
        var frameData = FrameData.FromSimulationState(
            _currentFrame,
            _friendlySquad,
            _enemySquad,
            _mainTarget,
            _currentWave,
            _hasMoreWaves
        );

        // Notify callbacks of frame generation
        callbacks.OnFrameGenerated(frameData);

        // Advance frame counter
        _currentFrame++;

        return frameData;
    }

    // ================================================================================
    // State Loading and Resuming
    // ================================================================================

    /// <summary>
    /// Loads simulation state from frame data.
    /// </summary>
    public void LoadState(FrameData frameData, ISimulatorCallbacks? callbacks = null)
    {
        if (frameData == null)
        {
            throw new ArgumentNullException(nameof(frameData));
        }

        callbacks ??= new DefaultSimulatorCallbacks();

        _currentFrame = frameData.FrameNumber;
        _mainTarget = frameData.MainTarget.ToVector2();
        _friendlySquad = ReconstructUnits(frameData.FriendlyUnits, UnitFaction.Friendly);
        _enemySquad = ReconstructUnits(frameData.EnemyUnits, UnitFaction.Enemy);
        _nextFriendlyId = _friendlySquad.Any() ? _friendlySquad.Max(u => u.Id) : 0;
        _nextEnemyId = _enemySquad.Any() ? _enemySquad.Max(u => u.Id) : 0;
        _currentWave = frameData.CurrentWave;

        ReestablishTargetReferences();

        _isInitialized = true;

        callbacks.OnStateChanged($"State loaded from frame {frameData.FrameNumber}");
        Console.WriteLine($"Simulation state loaded from frame {frameData.FrameNumber}.");
    }

    private List<Unit> ReconstructUnits(List<UnitStateData> stateList, UnitFaction expectedFaction)
    {
        var units = new List<Unit>();

        foreach (var state in stateList)
        {
            var role = Enum.Parse<UnitRole>(state.Role);
            var faction = Enum.Parse<UnitFaction>(state.Faction);
            var abilities = RehydrateAbilities(state.Abilities ?? new List<AbilityType>());

            var unit = new Unit(
                state.Position.ToVector2(),
                state.Radius,
                state.Speed,
                state.TurnSpeed,
                role,
                state.HP,
                state.Id,
                faction,
                state.Layer,
                state.CanTarget,
                state.Damage,
                abilities
            );

            unit.Velocity = state.Velocity.ToVector2();
            unit.Forward = state.Forward.ToVector2();
            unit.CurrentDestination = state.CurrentDestination.ToVector2();
            unit.AttackCooldown = state.AttackCooldown;
            unit.IsDead = state.IsDead;
            unit.ShieldHP = Math.Min(state.ShieldHP, state.MaxShieldHP);
            unit.ChargeState ??= state.HasChargeState ? new ChargeState() : null;
            if (unit.ChargeState != null)
            {
                unit.ChargeState.IsCharging = state.IsCharging;
                unit.ChargeState.IsCharged = state.IsCharged;
                unit.ChargeState.RequiredDistance = state.RequiredChargeDistance;
            }
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

    private List<AbilityData> RehydrateAbilities(List<AbilityType> abilityTypes)
    {
        var abilities = new List<AbilityData>();
        foreach (var type in abilityTypes)
        {
            AbilityData? ability = type switch
            {
                AbilityType.ChargeAttack => new ChargeAttackData(),
                AbilityType.SplashDamage => new SplashDamageData(),
                AbilityType.Shield => new ShieldData(),
                AbilityType.DeathSpawn => new DeathSpawnData(),
                AbilityType.DeathDamage => new DeathDamageData(),
                _ => null
            };

            if (ability != null)
            {
                abilities.Add(ability);
            }
        }

        return abilities;
    }

    private void ReestablishTargetReferences()
    {
        // Target references will be re-acquired by behavior systems on next frame
    }

    // ================================================================================
    // State Injection (Runtime Modification)
    // ================================================================================

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
    /// Processes combat results such as spawn requests after an attack.
    /// </summary>
    public void ProcessAttackResult(UnitFaction attackerFaction, AttackResult result)
    {
        if (result == null) return;
        if (result.SpawnRequests.Any())
        {
            _pendingSpawns.AddRange(result.SpawnRequests);
        }
    }

    private void ApplyPendingSpawns(ISimulatorCallbacks callbacks)
    {
        if (!_pendingSpawns.Any()) return;

        foreach (var spawn in _pendingSpawns)
        {
            InjectSpawnedUnit(spawn, callbacks);
        }

        _pendingSpawns.Clear();
    }

    public Unit InjectUnit(
        Vector2 position,
        UnitRole role,
        UnitFaction faction,
        int? hp = null,
        float? speed = null,
        float? turnSpeed = null,
        ISimulatorCallbacks? callbacks = null)
    {
        callbacks ??= new DefaultSimulatorCallbacks();

        int id = faction == UnitFaction.Friendly ? GetNextFriendlyId() : GetNextEnemyId();
        int health = hp ?? (faction == UnitFaction.Friendly ? GameConstants.FRIENDLY_HP : GameConstants.ENEMY_HP);
        float unitSpeed = speed ?? (faction == UnitFaction.Friendly ? 4.5f : 4.0f);
        float unitTurnSpeed = turnSpeed ?? (faction == UnitFaction.Friendly ? 0.08f : 0.1f);

        var unit = new Unit(position, GameConstants.UNIT_RADIUS, unitSpeed, unitTurnSpeed, role, health, id, faction);

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

    private Unit InjectSpawnedUnit(UnitSpawnRequest request, ISimulatorCallbacks callbacks)
    {
        callbacks ??= new DefaultSimulatorCallbacks();

        int id = request.Faction == UnitFaction.Friendly ? GetNextFriendlyId() : GetNextEnemyId();
        int health = request.HP > 0
            ? request.HP
            : (request.Faction == UnitFaction.Friendly ? GameConstants.FRIENDLY_HP : GameConstants.ENEMY_HP);
        float unitSpeed = request.Faction == UnitFaction.Friendly ? 4.5f : 4.0f;
        float unitTurnSpeed = request.Faction == UnitFaction.Friendly ? 0.08f : 0.1f;

        // 기본값: 지상, Ground 타겟 전용 근접 유닛
        var unit = new Unit(
            request.Position,
            GameConstants.UNIT_RADIUS,
            unitSpeed,
            unitTurnSpeed,
            UnitRole.Melee,
            health,
            id,
            request.Faction
        );

        var squad = request.Faction == UnitFaction.Friendly ? _friendlySquad : _enemySquad;
        squad.Add(unit);

        callbacks.OnStateChanged($"Unit {unit.Label} spawned from death effect at ({request.Position.X}, {request.Position.Y})");
        callbacks.OnUnitEvent(new UnitEventData
        {
            EventType = UnitEventType.Spawned,
            UnitId = unit.Id,
            Faction = request.Faction,
            FrameNumber = _currentFrame,
            Position = request.Position
        });

        return unit;
    }

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

        if (unit.Target != null)
        {
            unit.Target.ReleaseSlot(unit);
        }

        squad.Remove(unit);
        callbacks.OnStateChanged($"Unit {unit.Label} removed from simulation");

        return true;
    }

    /// <summary>
    /// Clears attack slots on all friendly units.
    /// Typically called when transitioning between waves.
    /// </summary>
    public void ClearFriendlyAttackSlots()
    {
        _friendlySquad.ForEach(f => Array.Fill(f.AttackSlots, null));
    }

    public FrameData GetCurrentFrameData()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Simulator must be initialized first.");
        }

        return FrameData.FromSimulationState(
            _currentFrame,
            _friendlySquad,
            _enemySquad,
            _mainTarget,
            _currentWave,
            _hasMoreWaves
        );
    }

    public void Stop()
    {
        _isRunning = false;
        Console.WriteLine($"Simulation stopped at frame {_currentFrame}.");
    }

    public void Reset()
    {
        _isRunning = false;
        _isInitialized = false;
        _currentFrame = 0;
        _nextFriendlyId = 0;
        _nextEnemyId = 0;
        _friendlySquad.Clear();
        _enemySquad.Clear();
        _commandQueue.Clear();
        _pathfindingGrid = null;
        _pathfinder = null;

        Initialize();
    }
}
