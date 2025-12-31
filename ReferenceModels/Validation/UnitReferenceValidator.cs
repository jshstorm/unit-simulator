using System.Collections.Generic;
using ReferenceModels.Models;

namespace ReferenceModels.Validation;

/// <summary>
/// UnitReference의 유효성을 검증합니다.
/// </summary>
public class UnitReferenceValidator : IValidator<UnitReference>
{
    public ValidationResult Validate(UnitReference unit, string id)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 필수 필드 검증
        if (unit.MaxHP <= 0)
            errors.Add($"[{id}] MaxHP must be positive (got {unit.MaxHP})");

        if (unit.MoveSpeed < 0)
            errors.Add($"[{id}] MoveSpeed cannot be negative (got {unit.MoveSpeed})");

        if (unit.Radius <= 0)
            errors.Add($"[{id}] Radius must be positive (got {unit.Radius})");

        if (unit.AttackRange < 0)
            errors.Add($"[{id}] AttackRange cannot be negative (got {unit.AttackRange})");

        if (unit.Damage < 0)
            errors.Add($"[{id}] Damage cannot be negative (got {unit.Damage})");

        if (unit.TurnSpeed < 0)
            errors.Add($"[{id}] TurnSpeed cannot be negative (got {unit.TurnSpeed})");

        // Phase 2 새 필드 검증
        if (unit.AttackSpeed <= 0)
            errors.Add($"[{id}] AttackSpeed must be positive (got {unit.AttackSpeed})");

        if (unit.ShieldHP < 0)
            errors.Add($"[{id}] ShieldHP cannot be negative (got {unit.ShieldHP})");

        if (unit.SpawnCount <= 0)
            errors.Add($"[{id}] SpawnCount must be positive (got {unit.SpawnCount})");

        // 선택 필드 경고
        if (string.IsNullOrWhiteSpace(unit.DisplayName))
            warnings.Add($"[{id}] DisplayName is empty");

        // 논리적 검증
        if (unit.Radius > unit.AttackRange)
            warnings.Add($"[{id}] Radius ({unit.Radius}) is larger than AttackRange ({unit.AttackRange})");

        return new ValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }
}
