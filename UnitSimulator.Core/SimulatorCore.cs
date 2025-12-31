using System.Numerics;
using System.Linq;
using UnitSimulator.Core.Contracts;
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
    private readonly CombatSystem _combatSystem = new();
    private readonly GameSession _gameSession = new();
    private readonly TowerBehavior _towerBehavior = new();
    private readonly WinConditionEvaluator _winConditionEvaluator = new();
    private readonly TerrainSystem _terrainSystem = new();
    private readonly UnitRegistry _unitRegistry = UnitRegistry.CreateWithDefaults();
    private ReferenceManager? _referenceManager;
    private bool _isInitialized = false;
    private bool _isRunning = false;

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
    public GameSession GameSession => _gameSession;
    public TerrainSystem TerrainSystem => _terrainSystem;

    /// <summary>
    /// 유닛 정의 레지스트리. 외부에서 정의 등록 가능.
    /// </summary>
    [Obsolete("Use ReferenceManager instead")]
    public UnitRegistry UnitRegistry => _unitRegistry;

    /// <summary>
    /// 레퍼런스 매니저. JSON 파일에서 로드된 읽기 전용 데이터.
    /// </summary>
    public ReferenceManager? References => _referenceManager;

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
    /// Initializes the simulation with default settings (Clash Royale standard).
    /// </summary>
    public void Initialize()
    {
        Initialize(null, null);
    }

    /// <summary>
    /// 시뮬레이터를 초기화합니다.
    /// </summary>
    /// <param name="referencePath">레퍼런스 데이터 디렉토리 경로 (null이면 기본 경로 사용)</param>
    public void Initialize(string? referencePath)
    {
        Initialize(null, referencePath);
    }

    /// <summary>
    /// InitialSetup을 사용하여 시뮬레이터를 초기화합니다.
    /// </summary>
    /// <param name="setup">초기 설정 (null이면 클래시 로열 표준 사용)</param>
    /// <param name="referencePath">레퍼런스 데이터 경로 (null이면 기본 경로 사용)</param>
    public void Initialize(InitialSetup? setup, string? referencePath = null)
    {
        Console.WriteLine("[SimulatorCore] Initialize() called");

        // Load reference data
        LoadReferences(referencePath);

        // Use default setup if not provided
        bool usingDefault = setup == null;
        setup ??= InitialSetup.CreateClashRoyaleStandard();
        Console.WriteLine($"[SimulatorCore] Using {(usingDefault ? "default ClashRoyale" : "custom")} InitialSetup");
        Console.WriteLine($"[SimulatorCore] Setup contains {setup.Towers.Count} towers, {setup.InitialUnits.Count} initial units");

        // Set main target on the right side of the simulation area
        _mainTarget = new Vector2(GameConstants.SIMULATION_WIDTH - 100, GameConstants.SIMULATION_HEIGHT / 2);

        // Initialize empty squads
        _friendlySquad = new List<Unit>();
        _enemySquad = new List<Unit>();

        // Spawn initial units (empty in standard Clash Royale mode)
        SpawnInitialUnits(setup.InitialUnits);
        Console.WriteLine($"[SimulatorCore] Spawned {_friendlySquad.Count} friendly, {_enemySquad.Count} enemy initial units");

        // Initialize Pathfinding
        _pathfindingGrid = new PathfindingGrid(GameConstants.SIMULATION_WIDTH, GameConstants.SIMULATION_HEIGHT, GameConstants.UNIT_RADIUS);
        _pathfinder = new AStarPathfinder(_pathfindingGrid);
        Console.WriteLine("[SimulatorCore] Pathfinding grid initialized");

        // Initialize towers from setup
        _gameSession.InitializeTowers(setup.Towers);

        // Apply game time settings if provided
        if (setup.GameTime != null)
        {
            _gameSession.MaxGameTime = setup.GameTime.MaxGameTime;
            Console.WriteLine($"[SimulatorCore] Game time set to {setup.GameTime.MaxGameTime}s");
        }

        _isInitialized = true;
        _currentFrame = 0;
        _currentWave = 0;
        _hasMoreWaves = true;

        Console.WriteLine($"[SimulatorCore] Initialization complete. Towers: {_gameSession.FriendlyTowers.Count}F/{_gameSession.EnemyTowers.Count}E");
    }

    /// <summary>
    /// 레퍼런스 데이터를 로드합니다.
    /// </summary>
    /// <param name="referencePath">레퍼런스 디렉토리 경로 (null이면 기본 경로)</param>
    public void LoadReferences(string? referencePath = null)
    {
        referencePath ??= "data/references";

        _referenceManager = ReferenceManager.CreateWithDefaultHandlers();
        _referenceManager.LoadAll(referencePath, Console.WriteLine);
    }

    /// <summary>
    /// 초기 유닛들을 스폰합니다.
    /// </summary>
    private void SpawnInitialUnits(List<UnitSpawnSetup> unitSetups)
    {
        foreach (var setup in unitSetups)
        {
            for (int i = 0; i < setup.Count; i++)
            {
                Vector2 position = setup.Count > 1
                    ? CalculateSpreadPosition(setup.Position, setup.SpawnRadius, i, setup.Count)
                    : setup.Position;

                SpawnUnitFromSetup(setup.UnitId, setup.Faction, position, setup.HP);
            }
        }
    }

    /// <summary>
    /// 분산 배치 위치를 계산합니다.
    /// </summary>
    private static Vector2 CalculateSpreadPosition(Vector2 center, float radius, int index, int total)
    {
        if (total <= 1) return center;

        float angle = (float)(2 * Math.PI * index / total);
        return new Vector2(
            center.X + radius * (float)Math.Cos(angle),
            center.Y + radius * (float)Math.Sin(angle)
        );
    }

    /// <summary>
    /// 설정에서 유닛을 생성합니다.
    /// </summary>
    private void SpawnUnitFromSetup(string unitId, UnitFaction faction, Vector2 position, int? hpOverride)
    {
        var unitRef = _referenceManager?.Units?.Get(unitId);
        int id = faction == UnitFaction.Friendly ? GetNextFriendlyId() : GetNextEnemyId();

        Unit unit;
        if (unitRef != null)
        {
            unit = unitRef.CreateUnit(unitId, id, faction, position, _referenceManager);
            if (hpOverride.HasValue)
            {
                unit.HP = hpOverride.Value;
            }
        }
        else
        {
            // Fallback: create default unit
            unit = new Unit(
                position,
                GameConstants.UNIT_RADIUS,
                4.0f,
                0.1f,
                UnitRole.Melee,
                hpOverride ?? 100,
                id,
                faction,
                unitId: unitId
            );
        }

        var squad = faction == UnitFaction.Friendly ? _friendlySquad : _enemySquad;
        squad.Add(unit);
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

            if (_gameSession.Result != GameResult.InProgress)
            {
                callbacks.OnSimulationComplete(_currentFrame, _gameSession.Result.ToString());
                Console.WriteLine($"Simulation ended with result {_gameSession.Result} at frame {_currentFrame}.");
                break;
            }
        }

        _isRunning = false;
    }

    /// <summary>
    /// Executes a single simulation step and returns the frame data.
    /// Uses 2-Phase Update pattern for deterministic behavior.
    /// </summary>
    public FrameData Step(ISimulatorCallbacks? callbacks = null)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Simulator must be initialized before stepping. Call Initialize() first.");
        }

        callbacks ??= new DefaultSimulatorCallbacks();
        var events = new FrameEvents();
        float deltaTime = GameConstants.FRAME_TIME_SECONDS;

        // Process queued commands first
        ProcessCommands(callbacks);

        // ════════════════════════════════════════════════════════════════════════
        // Phase 1: Collect - 모든 유닛 틱, 이벤트 수집 (HP 변경 없음)
        // ════════════════════════════════════════════════════════════════════════
        _enemyBehavior.UpdateEnemySquad(this, _enemySquad, _friendlySquad, _gameSession.FriendlyTowers, events);
        _squadBehavior.UpdateFriendlySquad(this, _friendlySquad, _enemySquad, _gameSession.EnemyTowers, _mainTarget, events);
        _towerBehavior.UpdateAllTowers(_gameSession, _friendlySquad, _enemySquad, events, deltaTime);

        // ════════════════════════════════════════════════════════════════════════
        // Phase 2: Apply - 이벤트 일괄 적용
        // ════════════════════════════════════════════════════════════════════════
        ApplyDamageEvents(events);
        ApplyTowerDamageEvents(events);
        ApplyDamageToTowers(events);
        ProcessDeaths(events, callbacks);
        ApplySpawnEvents(events, callbacks);

        _gameSession.ElapsedTime += deltaTime;
        _gameSession.UpdateKingTowerActivation();
        _gameSession.UpdateCrowns();
        _winConditionEvaluator.Evaluate(_gameSession);

        // Generate frame data
        var frameData = FrameData.FromSimulationState(
            _currentFrame,
            _friendlySquad,
            _enemySquad,
            _mainTarget,
            _currentWave,
            _hasMoreWaves,
            _gameSession
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

        if (frameData.FriendlyTowers.Any() || frameData.EnemyTowers.Any())
        {
            _gameSession.LoadFromState(
                frameData.FriendlyTowers,
                frameData.EnemyTowers,
                frameData.ElapsedTime,
                frameData.FriendlyCrowns,
                frameData.EnemyCrowns,
                frameData.GameResult,
                frameData.WinConditionType,
                frameData.IsOvertime
            );
        }
        else
        {
            _gameSession.InitializeDefaultTowers();
        }

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
            var targetPriority = TargetPriority.Nearest;
            if (!string.IsNullOrWhiteSpace(state.TargetPriority))
            {
                Enum.TryParse(state.TargetPriority, out targetPriority);
            }

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
                abilities,
                unitId: string.IsNullOrWhiteSpace(state.UnitId) ? "unknown" : state.UnitId,
                targetPriority: targetPriority
            );

            unit.Velocity = state.Velocity.ToVector2();
            unit.Forward = state.Forward.ToVector2();
            unit.CurrentDestination = state.CurrentDestination.ToVector2();
            unit.AttackCooldown = state.AttackCooldown;
            unit.IsDead = state.IsDead;
            unit.ShieldHP = Math.Min(state.ShieldHP, state.MaxShieldHP);
            if (state.HasChargeState)
            {
                var chargeState = unit.EnsureChargeState();
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

    // ════════════════════════════════════════════════════════════════════════
    // Phase 2: Apply - 이벤트 일괄 적용 메서드
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 수집된 모든 피해 이벤트를 적용합니다. (Phase 2 Step 1)
    /// HP만 감소시키고 사망 처리는 하지 않습니다.
    /// </summary>
    private void ApplyDamageEvents(FrameEvents events)
    {
        foreach (var damage in events.Damages)
        {
            if (damage.Target.IsDead) continue;
            damage.Target.TakeDamage(damage.Amount);
        }
    }

    /// <summary>
    /// 타워가 유닛에게 가한 피해를 적용합니다.
    /// </summary>
    private void ApplyTowerDamageEvents(FrameEvents events)
    {
        foreach (var damage in events.TowerDamages)
        {
            if (damage.Target.IsDead) continue;
            damage.Target.TakeDamage(damage.Amount);
        }
    }

    /// <summary>
    /// 유닛이 타워에게 가한 피해를 적용합니다.
    /// </summary>
    private void ApplyDamageToTowers(FrameEvents events)
    {
        foreach (var damage in events.DamageToTowers)
        {
            if (damage.Target.IsDestroyed) continue;
            damage.Target.TakeDamage(damage.Amount);
        }
    }

    /// <summary>
    /// 사망 판정 및 Death 어빌리티를 처리합니다. (Phase 2 Step 2)
    /// 큐 기반으로 연쇄 사망을 처리합니다.
    /// </summary>
    private void ProcessDeaths(FrameEvents events, ISimulatorCallbacks callbacks)
    {
        var deathQueue = new Queue<Unit>();
        var processed = new HashSet<Unit>();

        // 초기 사망 유닛 수집 (HP <= 0 && !IsDead)
        foreach (var unit in GetAllLivingUnits())
        {
            if (unit.HP <= 0)
            {
                deathQueue.Enqueue(unit);
            }
        }

        // 큐가 빌 때까지 처리 (연쇄 사망 포함)
        while (deathQueue.Count > 0)
        {
            var dead = deathQueue.Dequeue();
            if (processed.Contains(dead)) continue;

            // 사망 처리
            dead.IsDead = true;
            dead.Velocity = System.Numerics.Vector2.Zero;
            if (dead.Target != null) dead.Target.ReleaseSlot(dead);
            processed.Add(dead);

            callbacks.OnUnitEvent(new UnitEventData
            {
                EventType = UnitEventType.Died,
                UnitId = dead.Id,
                Faction = dead.Faction,
                FrameNumber = _currentFrame,
                Position = dead.Position
            });

            // DeathSpawn 처리
            var spawns = _combatSystem.CreateDeathSpawnRequests(dead);
            events.AddSpawns(spawns);

            // DeathDamage 처리 → 추가 사망 유닛 큐에 추가
            var opposingUnits = GetOpposingUnits(dead.Faction);
            var newlyDead = _combatSystem.ApplyDeathDamage(dead, opposingUnits);
            foreach (var killed in newlyDead)
            {
                if (!processed.Contains(killed))
                {
                    deathQueue.Enqueue(killed);
                }
            }
        }
    }

    /// <summary>
    /// 수집된 스폰 요청을 적용합니다. (Phase 2 Step 3)
    /// </summary>
    private void ApplySpawnEvents(FrameEvents events, ISimulatorCallbacks callbacks)
    {
        foreach (var spawn in events.Spawns)
        {
            InjectSpawnedUnit(spawn, callbacks);
        }
    }

    /// <summary>
    /// 외부에서 수집된 FrameEvents를 일괄 적용합니다. (레거시 호환용)
    /// </summary>
    public void ApplyFrameEvents(FrameEvents events, ISimulatorCallbacks? callbacks = null)
    {
        callbacks ??= new DefaultSimulatorCallbacks();
        ApplyDamageEvents(events);
        ApplyTowerDamageEvents(events);
        ApplyDamageToTowers(events);
        ProcessDeaths(events, callbacks);
        ApplySpawnEvents(events, callbacks);
    }

    /// <summary>
    /// 살아있는 모든 유닛을 반환합니다.
    /// </summary>
    private IEnumerable<Unit> GetAllLivingUnits()
    {
        return _friendlySquad.Where(u => !u.IsDead)
            .Concat(_enemySquad.Where(u => !u.IsDead));
    }

    /// <summary>
    /// 지정된 팩션의 반대편 유닛 목록을 반환합니다.
    /// </summary>
    private List<Unit> GetOpposingUnits(UnitFaction faction)
    {
        return faction == UnitFaction.Friendly
            ? _enemySquad.Where(u => !u.IsDead).ToList()
            : _friendlySquad.Where(u => !u.IsDead).ToList();
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

        var unit = new Unit(position, GameConstants.UNIT_RADIUS, unitSpeed, unitTurnSpeed, role, health, id, faction, unitId: role.ToString().ToLowerInvariant());

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
        Unit unit;
        string? displayName = null;

        // 1. ReferenceManager에서 조회 (우선)
        var unitRef = _referenceManager?.Units?.Get(request.UnitId);
        if (unitRef != null)
        {
            unit = unitRef.CreateUnit(request.UnitId, id, request.Faction, request.Position, _referenceManager);
            displayName = unitRef.DisplayName;

            if (request.HP > 0)
            {
                unit.HP = request.HP;
            }
        }
        // 2. UnitRegistry에서 조회 (폴백)
        else
        {
            var definition = _unitRegistry.GetDefinition(request.UnitId);
            if (definition != null)
            {
                unit = definition.CreateUnit(id, request.Faction, request.Position);
                displayName = definition.DisplayName;

                if (request.HP > 0)
                {
                    unit.HP = request.HP;
                }
            }
            // 3. 기본값으로 생성 (레거시 호환)
            else
            {
                int health = request.HP > 0
                    ? request.HP
                    : (request.Faction == UnitFaction.Friendly ? GameConstants.FRIENDLY_HP : GameConstants.ENEMY_HP);
                float unitSpeed = request.Faction == UnitFaction.Friendly ? 4.5f : 4.0f;
                float unitTurnSpeed = request.Faction == UnitFaction.Friendly ? 0.08f : 0.1f;

                unit = new Unit(
                    request.Position,
                    GameConstants.UNIT_RADIUS,
                    unitSpeed,
                    unitTurnSpeed,
                    UnitRole.Melee,
                    health,
                    id,
                    request.Faction,
                    unitId: string.IsNullOrWhiteSpace(request.UnitId) ? "unknown" : request.UnitId
                );

                if (!string.IsNullOrEmpty(request.UnitId))
                {
                    callbacks.OnStateChanged($"Warning: Unknown unit type '{request.UnitId}', using defaults");
                }
            }
        }

        if (displayName != null)
        {
            callbacks.OnStateChanged($"Unit {unit.Label} ({displayName}) spawned at ({request.Position.X:F0}, {request.Position.Y:F0})");
        }
        else
        {
            callbacks.OnStateChanged($"Unit {unit.Label} spawned at ({request.Position.X:F0}, {request.Position.Y:F0})");
        }

        var squad = request.Faction == UnitFaction.Friendly ? _friendlySquad : _enemySquad;
        squad.Add(unit);

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
            _hasMoreWaves,
            _gameSession
        );
    }

    public void Stop()
    {
        _isRunning = false;
        Console.WriteLine($"Simulation stopped at frame {_currentFrame}.");
    }

    public void Reset()
    {
        Console.WriteLine("[SimulatorCore] Reset() called");
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
        Console.WriteLine("[SimulatorCore] Reset complete");
    }
}
