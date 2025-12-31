using System.Text.Json.Serialization;
using ReferenceModels.Models.Enums;

namespace ReferenceModels.Models;

/// <summary>
/// 스펠 레퍼런스 데이터
/// </summary>
public class SpellReference
{
    /// <summary>표시 이름</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    /// <summary>스펠 유형</summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SpellType Type { get; init; } = SpellType.Instant;

    /// <summary>효과 반경</summary>
    [JsonPropertyName("radius")]
    public float Radius { get; init; } = 0f;

    /// <summary>지속 시간 (초, Instant는 0)</summary>
    [JsonPropertyName("duration")]
    public float Duration { get; init; } = 0f;

    /// <summary>시전 딜레이 (초)</summary>
    [JsonPropertyName("castDelay")]
    public float CastDelay { get; init; } = 0f;

    // === 피해 관련 ===

    /// <summary>즉시 피해 (Instant용)</summary>
    [JsonPropertyName("damage")]
    public int Damage { get; init; } = 0;

    /// <summary>틱당 피해 (AreaOverTime용)</summary>
    [JsonPropertyName("damagePerTick")]
    public int DamagePerTick { get; init; } = 0;

    /// <summary>틱 간격 (초)</summary>
    [JsonPropertyName("tickInterval")]
    public float TickInterval { get; init; } = 0f;

    /// <summary>건물 피해 배율</summary>
    [JsonPropertyName("buildingDamageMultiplier")]
    public float BuildingDamageMultiplier { get; init; } = 1.0f;

    // === 효과 관련 ===

    /// <summary>영향받는 대상</summary>
    [JsonPropertyName("affectedTargets")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TargetType AffectedTargets { get; init; } = TargetType.None;

    /// <summary>부여할 상태 효과</summary>
    [JsonPropertyName("appliedEffect")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusEffectType AppliedEffect { get; init; } = StatusEffectType.None;

    /// <summary>효과 크기 (슬로우: 속도 감소율 등)</summary>
    [JsonPropertyName("effectMagnitude")]
    public float EffectMagnitude { get; init; } = 0f;

    // === 소환 관련 ===

    /// <summary>소환할 유닛 ID</summary>
    [JsonPropertyName("spawnUnitId")]
    public string? SpawnUnitId { get; init; }

    /// <summary>소환 수량</summary>
    [JsonPropertyName("spawnCount")]
    public int SpawnCount { get; init; } = 0;

    /// <summary>소환 간격 (초)</summary>
    [JsonPropertyName("spawnInterval")]
    public float SpawnInterval { get; init; } = 0f;
}
