using System.Text.Json.Serialization;

namespace ReferenceModels.Models;

/// <summary>
/// JSON에서 로드되는 스킬 레퍼런스 데이터.
/// type 필드로 실제 AbilityData 타입을 결정합니다.
/// </summary>
public class SkillReference
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    // ChargeAttack
    [JsonPropertyName("triggerDistance")]
    public float TriggerDistance { get; init; } = 150f;

    [JsonPropertyName("requiredChargeDistance")]
    public float RequiredChargeDistance { get; init; } = 100f;

    [JsonPropertyName("damageMultiplier")]
    public float DamageMultiplier { get; init; } = 2.0f;

    [JsonPropertyName("speedMultiplier")]
    public float SpeedMultiplier { get; init; } = 2.0f;

    // SplashDamage
    [JsonPropertyName("radius")]
    public float Radius { get; init; } = 60f;

    [JsonPropertyName("damageFalloff")]
    public float DamageFalloff { get; init; } = 0f;

    // Shield
    [JsonPropertyName("maxShieldHP")]
    public int MaxShieldHP { get; init; } = 200;

    [JsonPropertyName("blocksStun")]
    public bool BlocksStun { get; init; } = false;

    [JsonPropertyName("blocksKnockback")]
    public bool BlocksKnockback { get; init; } = false;

    // DeathSpawn
    [JsonPropertyName("spawnUnitId")]
    public string SpawnUnitId { get; init; } = "";

    [JsonPropertyName("spawnCount")]
    public int SpawnCount { get; init; } = 2;

    [JsonPropertyName("spawnRadius")]
    public float SpawnRadius { get; init; } = 30f;

    [JsonPropertyName("spawnUnitHP")]
    public int SpawnUnitHP { get; init; } = 0;

    // DeathDamage
    [JsonPropertyName("damage")]
    public int Damage { get; init; } = 100;

    [JsonPropertyName("knockbackDistance")]
    public float KnockbackDistance { get; init; } = 0f;

    /// <summary>
    /// JSON 데이터를 실제 AbilityData로 변환합니다.
    /// </summary>
    public AbilityData? ToAbilityData()
    {
        return Type.ToLowerInvariant() switch
        {
            "chargeattack" => new ChargeAttackData
            {
                TriggerDistance = TriggerDistance,
                RequiredChargeDistance = RequiredChargeDistance,
                DamageMultiplier = DamageMultiplier,
                SpeedMultiplier = SpeedMultiplier
            },
            "splashdamage" => new SplashDamageData
            {
                Radius = Radius,
                DamageFalloff = DamageFalloff
            },
            "shield" => new ShieldData
            {
                MaxShieldHP = MaxShieldHP,
                BlocksStun = BlocksStun,
                BlocksKnockback = BlocksKnockback
            },
            "deathspawn" => new DeathSpawnData
            {
                SpawnUnitId = SpawnUnitId,
                SpawnCount = SpawnCount,
                SpawnRadius = SpawnRadius,
                SpawnUnitHP = SpawnUnitHP
            },
            "deathdamage" => new DeathDamageData
            {
                Damage = Damage,
                Radius = Radius,
                KnockbackDistance = KnockbackDistance
            },
            _ => null
        };
    }
}
