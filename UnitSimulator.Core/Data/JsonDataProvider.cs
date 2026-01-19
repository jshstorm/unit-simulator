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
        // TODO: BuildWaveDefinitionsCache() when waves.json exists
        // TODO: LoadGameBalance() when balance.json exists
    }

    private void BuildUnitStatsCache()
    {
        var unitsTable = _referenceManager.Units;
        if (unitsTable == null)
        {
            _logger?.Invoke("[JsonDataProvider] Units table not loaded");
            return;
        }

        foreach (var (id, unit) in unitsTable.GetAll())
        {
            var stats = ConvertToUnitStats(unit);
            _unitStatsCache[id.ToLowerInvariant()] = stats;
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
}
