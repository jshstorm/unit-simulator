using System.Collections.Generic;
using ReferenceModels.Models;
using ReferenceModels.Models.Enums;

namespace ReferenceModels.Validation;

/// <summary>
/// SpellReference의 유효성을 검증합니다.
/// </summary>
public class SpellReferenceValidator : IValidator<SpellReference>
{
    public ValidationResult Validate(SpellReference spell, string id)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 기본 필드 검증
        if (string.IsNullOrWhiteSpace(spell.DisplayName))
            warnings.Add($"[{id}] DisplayName is empty");

        if (spell.Radius < 0)
            errors.Add($"[{id}] Radius cannot be negative (got {spell.Radius})");

        if (spell.Duration < 0)
            errors.Add($"[{id}] Duration cannot be negative (got {spell.Duration})");

        if (spell.CastDelay < 0)
            errors.Add($"[{id}] CastDelay cannot be negative (got {spell.CastDelay})");

        // 타입별 필수 필드 검증
        switch (spell.Type)
        {
            case SpellType.Instant:
                if (spell.Damage <= 0 && spell.AppliedEffect == StatusEffectType.None)
                    warnings.Add($"[{id}] Instant: Neither Damage nor AppliedEffect is set (spell has no effect)");

                if (spell.Radius <= 0)
                    warnings.Add($"[{id}] Instant: Radius is not positive (got {spell.Radius})");
                break;

            case SpellType.AreaOverTime:
                if (spell.Duration <= 0)
                    errors.Add($"[{id}] AreaOverTime: Duration must be positive (got {spell.Duration})");

                if (spell.DamagePerTick < 0)
                    errors.Add($"[{id}] AreaOverTime: DamagePerTick cannot be negative (got {spell.DamagePerTick})");

                if (spell.TickInterval <= 0 && spell.DamagePerTick > 0)
                    errors.Add($"[{id}] AreaOverTime: TickInterval must be positive when DamagePerTick is set");

                if (spell.Radius <= 0)
                    errors.Add($"[{id}] AreaOverTime: Radius must be positive (got {spell.Radius})");
                break;

            case SpellType.Utility:
                if (spell.AppliedEffect == StatusEffectType.None)
                    warnings.Add($"[{id}] Utility: AppliedEffect is None (spell has no effect)");

                if (spell.Duration <= 0)
                    warnings.Add($"[{id}] Utility: Duration is not positive (got {spell.Duration})");
                break;

            case SpellType.Spawning:
                if (string.IsNullOrWhiteSpace(spell.SpawnUnitId))
                    errors.Add($"[{id}] Spawning: SpawnUnitId cannot be empty");

                if (spell.SpawnCount <= 0)
                    errors.Add($"[{id}] Spawning: SpawnCount must be positive (got {spell.SpawnCount})");

                if (spell.SpawnInterval < 0)
                    errors.Add($"[{id}] Spawning: SpawnInterval cannot be negative (got {spell.SpawnInterval})");
                break;
        }

        // 공통 필드 검증
        if (spell.BuildingDamageMultiplier < 0)
            errors.Add($"[{id}] BuildingDamageMultiplier cannot be negative (got {spell.BuildingDamageMultiplier})");

        if (spell.AppliedEffect != StatusEffectType.None && spell.EffectMagnitude == 0)
            warnings.Add($"[{id}] AppliedEffect is set but EffectMagnitude is 0");

        return new ValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }
}
