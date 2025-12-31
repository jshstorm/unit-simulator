using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReferenceModels.Models.Enums;

namespace ReferenceModels.Models;

/// <summary>
/// 건물 레퍼런스 데이터
/// </summary>
public class BuildingReference
{
    /// <summary>표시 이름</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    /// <summary>건물 유형</summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BuildingType Type { get; init; } = BuildingType.Defensive;

    /// <summary>최대 HP</summary>
    [JsonPropertyName("maxHP")]
    public int MaxHP { get; init; } = 100;

    /// <summary>충돌 반경</summary>
    [JsonPropertyName("radius")]
    public float Radius { get; init; } = 2.0f;

    /// <summary>생명 시간 (초, 0 = 무한)</summary>
    [JsonPropertyName("lifetime")]
    public float Lifetime { get; init; } = 0f;

    // === Spawner 전용 필드 ===

    /// <summary>소환할 유닛 ID</summary>
    [JsonPropertyName("spawnUnitId")]
    public string? SpawnUnitId { get; init; }

    /// <summary>소환 수량</summary>
    [JsonPropertyName("spawnCount")]
    public int SpawnCount { get; init; } = 0;

    /// <summary>소환 간격 (초)</summary>
    [JsonPropertyName("spawnInterval")]
    public float SpawnInterval { get; init; } = 0f;

    /// <summary>첫 소환 딜레이 (초)</summary>
    [JsonPropertyName("firstSpawnDelay")]
    public float FirstSpawnDelay { get; init; } = 0f;

    // === Defensive 전용 필드 ===

    /// <summary>공격 사거리</summary>
    [JsonPropertyName("attackRange")]
    public float AttackRange { get; init; } = 0f;

    /// <summary>공격력</summary>
    [JsonPropertyName("damage")]
    public int Damage { get; init; } = 0;

    /// <summary>초당 공격 횟수</summary>
    [JsonPropertyName("attackSpeed")]
    public float AttackSpeed { get; init; } = 0f;

    /// <summary>공격 가능 대상</summary>
    [JsonPropertyName("canTarget")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TargetType CanTarget { get; init; } = TargetType.None;

    // === 공통 ===

    /// <summary>스킬 ID 목록</summary>
    [JsonPropertyName("skills")]
    public List<string> Skills { get; init; } = new();
}
