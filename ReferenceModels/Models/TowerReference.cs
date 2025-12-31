using System.Text.Json.Serialization;
using ReferenceModels.Models.Enums;

namespace ReferenceModels.Models;

/// <summary>
/// 타워 레퍼런스 데이터
/// </summary>
public class TowerReference
{
    /// <summary>표시 이름</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    /// <summary>타워 유형</summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TowerType Type { get; init; } = TowerType.Princess;

    /// <summary>최대 HP</summary>
    [JsonPropertyName("maxHP")]
    public int MaxHP { get; init; } = 1000;

    /// <summary>공격력</summary>
    [JsonPropertyName("damage")]
    public int Damage { get; init; } = 50;

    /// <summary>초당 공격 횟수</summary>
    [JsonPropertyName("attackSpeed")]
    public float AttackSpeed { get; init; } = 1.0f;

    /// <summary>공격 사거리</summary>
    [JsonPropertyName("attackRadius")]
    public float AttackRadius { get; init; } = 7.5f;

    /// <summary>충돌 반경</summary>
    [JsonPropertyName("radius")]
    public float Radius { get; init; } = 2.5f;

    /// <summary>공격 가능 대상</summary>
    [JsonPropertyName("canTarget")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TargetType CanTarget { get; init; } = TargetType.Ground | TargetType.Air;
}
