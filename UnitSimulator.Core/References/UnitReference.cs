using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnitSimulator;

/// <summary>
/// 유닛의 읽기 전용 레퍼런스 데이터.
/// JSON에서 로드되어 런타임에 유닛 생성 시 참조됩니다.
/// </summary>
public class UnitReference
{
    /// <summary>표시 이름</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    /// <summary>최대 HP</summary>
    [JsonPropertyName("maxHP")]
    public int MaxHP { get; init; } = 100;

    /// <summary>공격력</summary>
    [JsonPropertyName("damage")]
    public int Damage { get; init; } = 10;

    /// <summary>이동 속도</summary>
    [JsonPropertyName("moveSpeed")]
    public float MoveSpeed { get; init; } = 4.0f;

    /// <summary>회전 속도</summary>
    [JsonPropertyName("turnSpeed")]
    public float TurnSpeed { get; init; } = 0.1f;

    /// <summary>공격 사거리</summary>
    [JsonPropertyName("attackRange")]
    public float AttackRange { get; init; } = 30f;

    /// <summary>충돌 반경</summary>
    [JsonPropertyName("radius")]
    public float Radius { get; init; } = 20f;

    /// <summary>역할 (Melee, Ranged 등)</summary>
    [JsonPropertyName("role")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UnitRole Role { get; init; } = UnitRole.Melee;

    /// <summary>이동 레이어 (Ground, Air)</summary>
    [JsonPropertyName("layer")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MovementLayer Layer { get; init; } = MovementLayer.Ground;

    /// <summary>공격 가능 대상</summary>
    [JsonPropertyName("canTarget")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TargetType CanTarget { get; init; } = TargetType.Ground;

    /// <summary>보유 능력 목록</summary>
    [JsonPropertyName("abilities")]
    public List<AbilityReferenceData> Abilities { get; init; } = new();

    /// <summary>
    /// 이 레퍼런스를 기반으로 Unit 인스턴스를 생성합니다.
    /// </summary>
    public Unit CreateUnit(string unitId, int id, UnitFaction faction, System.Numerics.Vector2 position)
    {
        var abilities = ConvertAbilities();
        var unit = new Unit(
            position: position,
            radius: Radius,
            speed: MoveSpeed,
            turnSpeed: TurnSpeed,
            role: Role,
            hp: MaxHP,
            id: id,
            faction: faction,
            layer: Layer,
            canTarget: CanTarget,
            damage: Damage,
            abilities: abilities,
            unitId: unitId
        );
        return unit;
    }

    private List<AbilityData> ConvertAbilities()
    {
        var result = new List<AbilityData>();
        foreach (var abilityRef in Abilities)
        {
            var ability = abilityRef.ToAbilityData();
            if (ability != null)
            {
                result.Add(ability);
            }
        }
        return result;
    }
}

/// <summary>
/// JSON에서 로드되는 능력 레퍼런스 데이터.
/// type 필드로 실제 AbilityData 타입을 결정합니다.
/// </summary>
public class AbilityReferenceData
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
