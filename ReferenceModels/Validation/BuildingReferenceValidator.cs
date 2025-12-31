using System.Collections.Generic;
using ReferenceModels.Models;
using ReferenceModels.Models.Enums;

namespace ReferenceModels.Validation;

/// <summary>
/// BuildingReference의 유효성을 검증합니다.
/// </summary>
public class BuildingReferenceValidator : IValidator<BuildingReference>
{
    public ValidationResult Validate(BuildingReference building, string id)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 기본 필드 검증
        if (string.IsNullOrWhiteSpace(building.DisplayName))
            warnings.Add($"[{id}] DisplayName is empty");

        if (building.MaxHP <= 0)
            errors.Add($"[{id}] MaxHP must be positive (got {building.MaxHP})");

        if (building.Radius <= 0)
            errors.Add($"[{id}] Radius must be positive (got {building.Radius})");

        if (building.Lifetime < 0)
            errors.Add($"[{id}] Lifetime cannot be negative (got {building.Lifetime})");

        // 타입별 필수 필드 검증
        switch (building.Type)
        {
            case BuildingType.Spawner:
                if (string.IsNullOrWhiteSpace(building.SpawnUnitId))
                    errors.Add($"[{id}] Spawner: SpawnUnitId cannot be empty");

                if (building.SpawnCount <= 0)
                    errors.Add($"[{id}] Spawner: SpawnCount must be positive (got {building.SpawnCount})");

                if (building.SpawnInterval <= 0)
                    errors.Add($"[{id}] Spawner: SpawnInterval must be positive (got {building.SpawnInterval})");

                if (building.FirstSpawnDelay < 0)
                    warnings.Add($"[{id}] Spawner: FirstSpawnDelay is negative (got {building.FirstSpawnDelay})");
                break;

            case BuildingType.Defensive:
                if (building.Damage <= 0)
                    errors.Add($"[{id}] Defensive: Damage must be positive (got {building.Damage})");

                if (building.AttackSpeed <= 0)
                    errors.Add($"[{id}] Defensive: AttackSpeed must be positive (got {building.AttackSpeed})");

                if (building.AttackRange <= 0)
                    errors.Add($"[{id}] Defensive: AttackRange must be positive (got {building.AttackRange})");

                if (building.CanTarget == TargetType.None)
                    errors.Add($"[{id}] Defensive: CanTarget cannot be None");
                break;

            case BuildingType.Utility:
                // Utility 건물은 특별한 필수 필드가 없음
                break;
        }

        return new ValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }
}
