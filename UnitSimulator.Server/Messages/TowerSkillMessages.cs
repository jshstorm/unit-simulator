using System.Text.Json.Serialization;

namespace UnitSimulator.Server.Messages;

public record ActivateTowerSkillRequest
{
    [JsonPropertyName("towerId")]
    public required int TowerId { get; init; }

    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }

    [JsonPropertyName("targetPosition")]
    public SkillTargetPosition? TargetPosition { get; init; }

    [JsonPropertyName("targetUnitId")]
    public int? TargetUnitId { get; init; }
}

public record SkillTargetPosition
{
    [JsonPropertyName("x")]
    public required float X { get; init; }

    [JsonPropertyName("y")]
    public required float Y { get; init; }
}

public record ActivateTowerSkillResponse
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("cooldown")]
    public int? Cooldown { get; init; }

    [JsonPropertyName("effects")]
    public List<SkillEffectDto>? Effects { get; init; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    public static ActivateTowerSkillResponse FromResult(Skills.SkillActivationResult result)
    {
        return new ActivateTowerSkillResponse
        {
            Success = result.Success,
            Cooldown = result.CooldownMs,
            Effects = result.Effects?.Select(e => new SkillEffectDto
            {
                Type = e.Type,
                TargetId = e.TargetId,
                Value = e.Value,
                Duration = e.DurationMs
            }).ToList(),
            ErrorCode = result.ErrorCode,
            Error = result.ErrorMessage
        };
    }
}

public record SkillEffectDto
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("targetId")]
    public required string TargetId { get; init; }

    [JsonPropertyName("value")]
    public int? Value { get; init; }

    [JsonPropertyName("duration")]
    public int? Duration { get; init; }
}

public record TowerSkillActivatedEvent
{
    [JsonPropertyName("type")]
    public string Type => "TowerSkillActivatedEvent";

    [JsonPropertyName("towerId")]
    public required int TowerId { get; init; }

    [JsonPropertyName("skillId")]
    public required string SkillId { get; init; }

    [JsonPropertyName("effects")]
    public required List<SkillEffectDto> Effects { get; init; }

    [JsonPropertyName("timestamp")]
    public required long Timestamp { get; init; }
}

public record GetTowerSkillsRequest
{
    [JsonPropertyName("towerId")]
    public required int TowerId { get; init; }
}

public record GetTowerSkillsResponse
{
    [JsonPropertyName("towerId")]
    public required int TowerId { get; init; }

    [JsonPropertyName("skills")]
    public required List<TowerSkillDto> Skills { get; init; }
}

public record TowerSkillDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("effectType")]
    public required string EffectType { get; init; }

    [JsonPropertyName("cooldownMs")]
    public required int CooldownMs { get; init; }

    [JsonPropertyName("remainingCooldownMs")]
    public required int RemainingCooldownMs { get; init; }

    [JsonPropertyName("isOnCooldown")]
    public required bool IsOnCooldown { get; init; }

    [JsonPropertyName("damage")]
    public int? Damage { get; init; }

    [JsonPropertyName("range")]
    public float? Range { get; init; }
}
