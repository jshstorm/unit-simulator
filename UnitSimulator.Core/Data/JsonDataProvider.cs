using ReferenceModels.Infrastructure;
using ReferenceModels.Models;
using ReferenceModels.Models.Enums;
using UnitSimulator.Core.Contracts;

namespace UnitSimulator.Core.Data;

/// <summary>
/// JSON 파일 기반 데이터 제공자.
/// ReferenceManager를 래핑하여 IDataProvider 인터페이스를 구현합니다.
/// </summary>
public class JsonDataProvider : IDataProvider
{
    private readonly string _dataDirectory;
    private readonly Action<string>? _logger;
    private ReferenceManager _referenceManager;
    private Dictionary<string, UnitStats> _unitStatsCache = new();
    private Dictionary<int, WaveDefinition> _waveDefinitionsCache = new();
    private GameBalance _gameBalance = GameBalance.Default;

    /// <summary>
    /// JsonDataProvider 생성자
    /// </summary>
    /// <param name="dataDirectory">JSON 데이터 디렉토리 경로</param>
    /// <param name="logger">로그 출력 함수 (선택)</param>
    public JsonDataProvider(string dataDirectory, Action<string>? logger = null)
    {
        _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
        _logger = logger;
        _referenceManager = ReferenceManager.CreateWithDefaultHandlers();
        LoadData();
    }

    /// <summary>
    /// 유닛 ID로 스탯을 조회합니다.
    /// </summary>
    public UnitStats GetUnitStats(string unitId)
    {
        if (string.IsNullOrEmpty(unitId))
            return UnitStats.Default;

        var normalizedId = unitId.ToLowerInvariant();
        if (_unitStatsCache.TryGetValue(normalizedId, out var stats))
            return stats;

        return UnitStats.Default;
    }

    /// <summary>
    /// 유닛 ID가 존재하는지 확인합니다.
    /// </summary>
    public bool HasUnit(string unitId)
    {
        if (string.IsNullOrEmpty(unitId))
            return false;

        return _unitStatsCache.ContainsKey(unitId.ToLowerInvariant());
    }

    /// <summary>
    /// 모든 유닛 ID 목록을 반환합니다.
    /// </summary>
    public IEnumerable<string> GetAllUnitIds()
    {
        return _unitStatsCache.Keys;
    }

    /// <summary>
    /// 웨이브 번호로 웨이브 정의를 조회합니다.
    /// </summary>
    public WaveDefinition GetWaveDefinition(int waveNumber)
    {
        if (_waveDefinitionsCache.TryGetValue(waveNumber, out var wave))
            return wave;

        return WaveDefinition.Empty(waveNumber);
    }

    /// <summary>
    /// 정의된 총 웨이브 수를 반환합니다.
    /// </summary>
    public int GetTotalWaveCount()
    {
        return _waveDefinitionsCache.Count > 0
            ? _waveDefinitionsCache.Keys.Max()
            : 0;
    }

    /// <summary>
    /// 게임 밸런스 설정을 반환합니다.
    /// </summary>
    public GameBalance GetGameBalance()
    {
        return _gameBalance;
    }

    /// <summary>
    /// 데이터를 다시 로드합니다.
    /// </summary>
    public void Reload()
    {
        _logger?.Invoke("[JsonDataProvider] Reloading data...");
        _referenceManager = ReferenceManager.CreateWithDefaultHandlers();
        _unitStatsCache.Clear();
        _waveDefinitionsCache.Clear();
        LoadData();
    }

    private void LoadData()
    {
        _referenceManager.LoadAll(_dataDirectory, _logger);
        BuildUnitStatsCache();
        BuildWaveDefinitionsCache();
        LoadGameBalance();
    }

    private void BuildUnitStatsCache()
    {
        var unitsTable = _referenceManager.Units;
        if (unitsTable == null)
        {
            _logger?.Invoke("[JsonDataProvider] Units table not loaded");
            return;
        }

        foreach (var kvp in unitsTable.GetAllWithIds())
        {
            var stats = ConvertToUnitStats(kvp.Value);
            _unitStatsCache[kvp.Key.ToLowerInvariant()] = stats;
        }

        _logger?.Invoke($"[JsonDataProvider] Built {_unitStatsCache.Count} unit stats");
    }

    private static UnitStats ConvertToUnitStats(UnitReference unit)
    {
        return new UnitStats
        {
            DisplayName = unit.DisplayName,
            HP = unit.MaxHP,
            Damage = unit.Damage,
            MoveSpeed = unit.MoveSpeed,
            TurnSpeed = unit.TurnSpeed,
            AttackRange = unit.AttackRange,
            Radius = unit.Radius,
            AttackSpeed = unit.AttackSpeed,
            Role = unit.Role,
            Layer = unit.Layer,
            CanTarget = unit.CanTarget,
            TargetPriority = unit.TargetPriority,
            AttackType = unit.AttackType,
            ShieldHP = unit.ShieldHP,
            SpawnCount = unit.SpawnCount,
            Skills = unit.Skills.ToList()
        };
    }

    private void BuildWaveDefinitionsCache()
    {
        var wavesTable = _referenceManager.Waves;
        if (wavesTable == null)
        {
            _logger?.Invoke("[JsonDataProvider] Waves table not loaded");
            return;
        }

        foreach (var wave in wavesTable.GetAll())
        {
            var waveDef = ConvertToWaveDefinition(wave);
            _waveDefinitionsCache[wave.WaveNumber] = waveDef;
        }

        _logger?.Invoke($"[JsonDataProvider] Built {_waveDefinitionsCache.Count} wave definitions");
    }

    private static WaveDefinition ConvertToWaveDefinition(WaveReference wave)
    {
        var spawnGroups = wave.Spawns.Select(spawn => new WaveSpawnGroup
        {
            UnitId = spawn.UnitId,
            SpawnX = spawn.Position.X,
            SpawnY = spawn.Position.Y,
        }).ToList();

        return new WaveDefinition
        {
            WaveNumber = wave.WaveNumber,
            DelayFrames = wave.DelayFrames,
            SpawnGroups = spawnGroups
        };
    }

    private void LoadGameBalance()
    {
        var balance = _referenceManager.Balance;
        if (balance == null)
        {
            _logger?.Invoke("[JsonDataProvider] Balance data not loaded, using defaults");
            _gameBalance = GameBalance.Default;
            return;
        }

        _gameBalance = ConvertToGameBalance(balance);
        _logger?.Invoke($"[JsonDataProvider] Loaded game balance (version {balance.Version})");
    }

    private static GameBalance ConvertToGameBalance(BalanceReference balance)
    {
        return new GameBalance
        {
            Version = balance.Version,
            // Simulation
            SimulationWidth = balance.Simulation?.Width ?? 3200,
            SimulationHeight = balance.Simulation?.Height ?? 5100,
            MaxFrames = balance.Simulation?.MaxFrames ?? 3000,
            FrameTimeSeconds = balance.Simulation?.FrameTimeSeconds ?? 1f / 30f,
            // Unit
            UnitRadius = balance.Unit?.DefaultRadius ?? 20f,
            CollisionRadiusScale = balance.Unit?.CollisionRadiusScale ?? 2f / 3f,
            NumAttackSlots = balance.Unit?.NumAttackSlots ?? 8,
            SlotReevaluateDistance = balance.Unit?.SlotReevaluateDistance ?? 40f,
            SlotReevaluateIntervalFrames = balance.Unit?.SlotReevaluateIntervalFrames ?? 60,
            // Combat
            AttackCooldown = balance.Combat?.AttackCooldown ?? 30f,
            MeleeRangeMultiplier = balance.Combat?.MeleeRangeMultiplier ?? 3,
            RangedRangeMultiplier = balance.Combat?.RangedRangeMultiplier ?? 6,
            EngagementTriggerDistanceMultiplier = balance.Combat?.EngagementTriggerDistanceMultiplier ?? 1.5f,
            // Squad
            RallyDistance = balance.Squad?.RallyDistance ?? 300f,
            FormationThreshold = balance.Squad?.FormationThreshold ?? 20f,
            SeparationRadius = balance.Squad?.SeparationRadius ?? 120f,
            FriendlySeparationRadius = balance.Squad?.FriendlySeparationRadius ?? 80f,
            DestinationThreshold = balance.Squad?.DestinationThreshold ?? 10f,
            // Wave
            MaxWaves = balance.Wave?.MaxWaves ?? 3,
            // Targeting
            TargetReevaluateIntervalFrames = balance.Targeting?.ReevaluateIntervalFrames ?? 45,
            TargetSwitchMargin = balance.Targeting?.SwitchMargin ?? 15f,
            TargetCrowdPenaltyPerAttacker = balance.Targeting?.CrowdPenaltyPerAttacker ?? 25f,
            // Avoidance
            AvoidanceAngleStep = balance.Avoidance?.AngleStep ?? MathF.PI / 8f,
            MaxAvoidanceIterations = balance.Avoidance?.MaxIterations ?? 8,
            AvoidanceMaxLookahead = balance.Avoidance?.MaxLookahead ?? 3.5f,
            // Collision
            CollisionResolutionIterations = balance.Collision?.ResolutionIterations ?? 3,
            CollisionPushStrength = balance.Collision?.PushStrength ?? 0.8f
        };
    }
}
