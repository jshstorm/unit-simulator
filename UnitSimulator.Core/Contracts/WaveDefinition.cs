namespace UnitSimulator.Core.Contracts;

/// <summary>
/// 웨이브 정의 데이터.
/// 어떤 유닛들이 어떤 타이밍에 스폰되는지 정의합니다.
/// </summary>
public sealed class WaveDefinition
{
    /// <summary>1-based 웨이브 인덱스</summary>
    public int WaveNumber { get; init; }

    /// <summary>웨이브 이름 (표시용)</summary>
    public string Name { get; init; } = "";

    /// <summary>웨이브 시작 전 대기 프레임</summary>
    public int DelayFrames { get; init; }

    /// <summary>스폰할 유닛 그룹 목록</summary>
    public IReadOnlyList<WaveSpawnGroup> SpawnGroups { get; init; } = Array.Empty<WaveSpawnGroup>();

    /// <summary>
    /// 기본 빈 웨이브 정의
    /// </summary>
    public static WaveDefinition Empty(int waveNumber) => new()
    {
        WaveNumber = waveNumber,
        Name = $"Wave {waveNumber}",
        DelayFrames = 0,
        SpawnGroups = Array.Empty<WaveSpawnGroup>()
    };
}

/// <summary>
/// 웨이브 내 스폰 그룹 정의
/// </summary>
public sealed class WaveSpawnGroup
{
    /// <summary>스폰할 유닛 ID</summary>
    public string UnitId { get; init; } = "";

    /// <summary>스폰 수량</summary>
    public int Count { get; init; } = 1;

    /// <summary>진영 (friendly/enemy)</summary>
    public string Faction { get; init; } = "enemy";

    /// <summary>스폰 시작 프레임 (웨이브 시작 기준)</summary>
    public int SpawnFrame { get; init; }

    /// <summary>스폰 간격 프레임 (여러 유닛 스폰 시)</summary>
    public int SpawnInterval { get; init; } = 30;

    /// <summary>스폰 위치 X (기본값은 랜덤)</summary>
    public float? SpawnX { get; init; }

    /// <summary>스폰 위치 Y (기본값은 랜덤)</summary>
    public float? SpawnY { get; init; }
}
