using System.Collections.Generic;
using ReferenceModels.Models;
using ReferenceModels.Models.Enums;

namespace ReferenceModels.Validation;

/// <summary>
/// SkillReference의 유효성을 검증합니다.
/// </summary>
public class SkillReferenceValidator : IValidator<SkillReference>
{
    public ValidationResult Validate(SkillReference skill, string id)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Type 필드 검증
        if (string.IsNullOrWhiteSpace(skill.Type))
        {
            errors.Add($"[{id}] Type cannot be empty");
            return new ValidationResult { Errors = errors, Warnings = warnings };
        }

        // 타입별 필수 필드 검증
        var type = skill.Type.ToLowerInvariant();
        switch (type)
        {
            case "chargeattack":
                if (skill.TriggerDistance <= 0)
                    errors.Add($"[{id}] ChargeAttack: TriggerDistance must be positive");
                if (skill.RequiredChargeDistance < 0)
                    errors.Add($"[{id}] ChargeAttack: RequiredChargeDistance cannot be negative");
                if (skill.DamageMultiplier <= 0)
                    warnings.Add($"[{id}] ChargeAttack: DamageMultiplier is not positive");
                if (skill.SpeedMultiplier <= 0)
                    warnings.Add($"[{id}] ChargeAttack: SpeedMultiplier is not positive");
                break;

            case "splashdamage":
                if (skill.Radius <= 0)
                    errors.Add($"[{id}] SplashDamage: Radius must be positive");
                if (skill.DamageFalloff < 0 || skill.DamageFalloff > 1)
                    warnings.Add($"[{id}] SplashDamage: DamageFalloff should be between 0 and 1");
                break;

            case "shield":
                if (skill.MaxShieldHP <= 0)
                    errors.Add($"[{id}] Shield: MaxShieldHP must be positive");
                break;

            case "deathspawn":
                if (string.IsNullOrWhiteSpace(skill.SpawnUnitId))
                    errors.Add($"[{id}] DeathSpawn: SpawnUnitId cannot be empty");
                if (skill.SpawnCount <= 0)
                    errors.Add($"[{id}] DeathSpawn: SpawnCount must be positive");
                if (skill.SpawnRadius < 0)
                    errors.Add($"[{id}] DeathSpawn: SpawnRadius cannot be negative");
                if (skill.SpawnUnitHP < 0)
                    warnings.Add($"[{id}] DeathSpawn: SpawnUnitHP is negative (will use default)");
                break;

            case "deathdamage":
                if (skill.Damage < 0)
                    errors.Add($"[{id}] DeathDamage: Damage cannot be negative");
                if (skill.Radius <= 0)
                    errors.Add($"[{id}] DeathDamage: Radius must be positive");
                if (skill.KnockbackDistance < 0)
                    warnings.Add($"[{id}] DeathDamage: KnockbackDistance is negative");
                break;

            default:
                warnings.Add($"[{id}] Unknown skill type: {skill.Type}");
                break;
        }

        // Phase 4 상태 효과 필드 검증
        if (skill.AppliedEffect != StatusEffectType.None)
        {
            if (skill.EffectDuration <= 0)
                errors.Add($"[{id}] AppliedEffect is set but EffectDuration must be positive");

            if (skill.EffectRange > 0 && skill.AffectedTargets == TargetType.None)
                warnings.Add($"[{id}] EffectRange is set but AffectedTargets is None");
        }

        if (skill.EffectRange < 0)
            errors.Add($"[{id}] EffectRange cannot be negative");

        return new ValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }
}
