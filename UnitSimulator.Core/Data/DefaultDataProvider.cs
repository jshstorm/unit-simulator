using ReferenceModels.Models.Enums;
using UnitSimulator.Core.Contracts;

namespace UnitSimulator.Core.Data;

/// <summary>
/// 기본 데이터 제공자.
/// JSON 파일 없이 하드코딩된 기본값을 제공합니다.
/// 테스트 및 폴백용으로 사용됩니다.
/// </summary>
public class DefaultDataProvider : IDataProvider
{
    private static readonly Dictionary<string, UnitStats> _defaultUnits = new()
    {
        ["skeleton"] = new UnitStats
        {
            DisplayName = "Skeleton",
            HP = 81,
            Damage = 81,
            MoveSpeed = 5.0f,
            TurnSpeed = 0.12f,
            AttackRange = 25f,
            Radius = 15f,
            AttackSpeed = 1.0f,
            Role = UnitRole.Swarm,
            Layer = MovementLayer.Ground,
            CanTarget = TargetType.Ground,
            TargetPriority = TargetPriority.Nearest,
            AttackType = AttackType.MeleeShort,
            ShieldHP = 0,
            SpawnCount = 3,
            Skills = Array.Empty<string>()
        },
        ["knight"] = new UnitStats
        {
            DisplayName = "Knight",
            HP = 1938,
            Damage = 202,
            MoveSpeed = 4.5f,
            TurnSpeed = 0.1f,
            AttackRange = 60f,
            Radius = 20f,
            AttackSpeed = 1.1f,
            Role = UnitRole.MiniTank,
            Layer = MovementLayer.Ground,
            CanTarget = TargetType.Ground,
            TargetPriority = TargetPriority.Nearest,
            AttackType = AttackType.Melee,
            ShieldHP = 0,
            SpawnCount = 1,
            Skills = Array.Empty<string>()
        },
        ["golem"] = new UnitStats
        {
            DisplayName = "Golem",
            HP = 5984,
            Damage = 270,
            MoveSpeed = 2.0f,
            TurnSpeed = 0.08f,
            AttackRange = 60f,
            Radius = 40f,
            AttackSpeed = 2.5f,
            Role = UnitRole.Tank,
            Layer = MovementLayer.Ground,
            CanTarget = TargetType.Building,
            TargetPriority = TargetPriority.Buildings,
            AttackType = AttackType.Melee,
            ShieldHP = 0,
            SpawnCount = 1,
            Skills = new[] { "golem_death_spawn", "golem_death_damage" }
        }
    };

    private readonly GameBalance _balance = GameBalance.Default;

    /// <inheritdoc />
    public UnitStats GetUnitStats(string unitId)
    {
        if (string.IsNullOrEmpty(unitId))
            return UnitStats.Default;

        return _defaultUnits.TryGetValue(unitId.ToLowerInvariant(), out var stats)
            ? stats
            : UnitStats.Default;
    }

    /// <inheritdoc />
    public bool HasUnit(string unitId)
    {
        if (string.IsNullOrEmpty(unitId))
            return false;

        return _defaultUnits.ContainsKey(unitId.ToLowerInvariant());
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllUnitIds()
    {
        return _defaultUnits.Keys;
    }

    /// <inheritdoc />
    public WaveDefinition GetWaveDefinition(int waveNumber)
    {
        // 기본 웨이브: 단순한 테스트 웨이브
        return new WaveDefinition
        {
            WaveNumber = waveNumber,
            Name = $"Default Wave {waveNumber}",
            DelayFrames = 60 * (waveNumber - 1),  // 2초 간격
            SpawnGroups = new[]
            {
                new WaveSpawnGroup
                {
                    UnitId = "skeleton",
                    Count = 3 + waveNumber,
                    Faction = "enemy",
                    SpawnFrame = 0,
                    SpawnInterval = 15
                }
            }
        };
    }

    /// <inheritdoc />
    public int GetTotalWaveCount()
    {
        return GameConstants.MAX_WAVES;
    }

    /// <inheritdoc />
    public GameBalance GetGameBalance()
    {
        return _balance;
    }

    /// <inheritdoc />
    public void Reload()
    {
        // 기본 제공자는 하드코딩된 데이터를 사용하므로 리로드 없음
    }
}
