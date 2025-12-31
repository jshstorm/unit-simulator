using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReferenceModels.Models.Enums;

namespace ReferenceModels.Models;

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

    /// <summary>타겟 우선순위</summary>
    [JsonPropertyName("targetPriority")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TargetPriority TargetPriority { get; init; } = TargetPriority.Nearest;

    /// <summary>보유 능력 목록</summary>
    [JsonPropertyName("skills")]
    public List<string> Skills { get; init; } = new();

    /// <summary>
    /// 이 레퍼런스를 기반으로 Unit 인스턴스를 생성합니다.
    /// </summary>
    public Unit CreateUnit(string unitId, int id, UnitFaction faction, System.Numerics.Vector2 position, ReferenceManager? referenceManager = null)
    {
        var abilities = ConvertSkills(referenceManager);
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
            unitId: unitId,
            targetPriority: TargetPriority
        );
        return unit;
    }

    private List<AbilityData> ConvertSkills(ReferenceManager? referenceManager)
    {
        var result = new List<AbilityData>();
        if (referenceManager?.Skills == null || Skills.Count == 0)
        {
            return result;
        }

        foreach (var skillId in Skills)
        {
            if (!referenceManager.Skills.TryGet(skillId, out var skillRef) || skillRef == null)
            {
                continue;
            }

            var ability = skillRef.ToAbilityData();
            if (ability != null)
            {
                result.Add(ability);
            }
        }
        return result;
    }
}
