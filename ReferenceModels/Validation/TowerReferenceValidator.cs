using System.Collections.Generic;
using ReferenceModels.Models;
using ReferenceModels.Models.Enums;

namespace ReferenceModels.Validation;

/// <summary>
/// TowerReference의 유효성을 검증합니다.
/// </summary>
public class TowerReferenceValidator : IValidator<TowerReference>
{
    public ValidationResult Validate(TowerReference tower, string id)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 기본 필드 검증
        if (string.IsNullOrWhiteSpace(tower.DisplayName))
            warnings.Add($"[{id}] DisplayName is empty");

        if (tower.MaxHP <= 0)
            errors.Add($"[{id}] MaxHP must be positive (got {tower.MaxHP})");

        if (tower.Damage <= 0)
            errors.Add($"[{id}] Damage must be positive (got {tower.Damage})");

        if (tower.AttackSpeed <= 0)
            errors.Add($"[{id}] AttackSpeed must be positive (got {tower.AttackSpeed})");

        if (tower.AttackRadius <= 0)
            errors.Add($"[{id}] AttackRadius must be positive (got {tower.AttackRadius})");

        if (tower.Radius <= 0)
            errors.Add($"[{id}] Radius must be positive (got {tower.Radius})");

        if (tower.CanTarget == TargetType.None)
            errors.Add($"[{id}] CanTarget cannot be None");

        // 논리적 검증
        if (tower.Radius > tower.AttackRadius)
            warnings.Add($"[{id}] Radius ({tower.Radius}) is larger than AttackRadius ({tower.AttackRadius})");

        return new ValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }
}
