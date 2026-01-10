using System.Numerics;

namespace UnitSimulator.Skills;

public class TowerSkillSystem
{
    private readonly Dictionary<int, List<TowerSkill>> _towerSkills = new();
    
    public void RegisterSkill(int towerId, TowerSkill skill)
    {
        if (!_towerSkills.ContainsKey(towerId))
        {
            _towerSkills[towerId] = new List<TowerSkill>();
        }
        _towerSkills[towerId].Add(skill);
    }
    
    public void RegisterSkills(int towerId, IEnumerable<TowerSkill> skills)
    {
        foreach (var skill in skills)
        {
            RegisterSkill(towerId, skill);
        }
    }
    
    public TowerSkill? GetSkill(int towerId, string skillId)
    {
        if (!_towerSkills.TryGetValue(towerId, out var skills))
        {
            return null;
        }
        return skills.FirstOrDefault(s => s.Id == skillId);
    }
    
    public IReadOnlyList<TowerSkill> GetSkills(int towerId)
    {
        if (!_towerSkills.TryGetValue(towerId, out var skills))
        {
            return Array.Empty<TowerSkill>();
        }
        return skills;
    }
    
    public bool IsSkillOnCooldown(int towerId, string skillId)
    {
        var skill = GetSkill(towerId, skillId);
        return skill?.IsOnCooldown ?? false;
    }
    
    public int GetRemainingCooldown(int towerId, string skillId)
    {
        var skill = GetSkill(towerId, skillId);
        return skill?.RemainingCooldownMs ?? 0;
    }
    
    public SkillActivationResult ActivateSkill(
        int towerId,
        string skillId,
        Tower? tower,
        List<Unit> enemies,
        Vector2? targetPosition = null,
        int? targetUnitId = null)
    {
        if (tower == null)
        {
            return SkillActivationResult.CreateFailure(
                SkillErrorCodes.TowerNotFound,
                $"Tower '{towerId}' not found");
        }
        
        if (tower.IsDestroyed)
        {
            return SkillActivationResult.CreateFailure(
                SkillErrorCodes.TowerNotFound,
                $"Tower '{towerId}' is destroyed");
        }
        
        var skill = GetSkill(towerId, skillId);
        if (skill == null)
        {
            return SkillActivationResult.CreateFailure(
                SkillErrorCodes.SkillNotFound,
                $"Skill '{skillId}' not found on tower '{towerId}'");
        }
        
        if (skill.IsOnCooldown)
        {
            return SkillActivationResult.CreateFailure(
                SkillErrorCodes.SkillOnCooldown,
                $"Skill is on cooldown. Remaining: {skill.RemainingCooldownMs}ms");
        }
        
        var targetValidation = ValidateTarget(skill, targetPosition, targetUnitId, enemies);
        if (!targetValidation.IsValid)
        {
            return SkillActivationResult.CreateFailure(
                targetValidation.ErrorCode!,
                targetValidation.ErrorMessage!);
        }
        
        var effects = ApplySkillEffects(skill, tower, enemies, targetPosition, targetUnitId);
        
        skill.StartCooldown();
        
        return SkillActivationResult.CreateSuccess(skill.CooldownMs, effects);
    }
    
    public void UpdateCooldowns(int deltaMs)
    {
        foreach (var skills in _towerSkills.Values)
        {
            foreach (var skill in skills)
            {
                skill.UpdateCooldown(deltaMs);
            }
        }
    }
    
    public void UpdateCooldowns(int towerId, int deltaMs)
    {
        if (_towerSkills.TryGetValue(towerId, out var skills))
        {
            foreach (var skill in skills)
            {
                skill.UpdateCooldown(deltaMs);
            }
        }
    }
    
    private (bool IsValid, string? ErrorCode, string? ErrorMessage) ValidateTarget(
        TowerSkill skill,
        Vector2? targetPosition,
        int? targetUnitId,
        List<Unit> enemies)
    {
        switch (skill.TargetType)
        {
            case SkillTargetType.None:
                return (true, null, null);
                
            case SkillTargetType.Position:
                if (targetPosition == null)
                {
                    return (false, SkillErrorCodes.TargetRequired, 
                        "Target position is required for this skill");
                }
                return (true, null, null);
                
            case SkillTargetType.SingleUnit:
                if (targetUnitId == null)
                {
                    return (false, SkillErrorCodes.TargetRequired,
                        "Target unit is required for this skill");
                }
                var targetUnit = enemies.FirstOrDefault(u => u.Id == targetUnitId);
                if (targetUnit == null || targetUnit.IsDead)
                {
                    return (false, SkillErrorCodes.TargetNotFound,
                        $"Target unit '{targetUnitId}' not found or dead");
                }
                return (true, null, null);
                
            default:
                return (true, null, null);
        }
    }
    
    private List<SkillEffectResult> ApplySkillEffects(
        TowerSkill skill,
        Tower tower,
        List<Unit> enemies,
        Vector2? targetPosition,
        int? targetUnitId)
    {
        var effects = new List<SkillEffectResult>();
        
        switch (skill.EffectType)
        {
            case SkillEffectType.TargetedDamage:
                effects.AddRange(ApplyTargetedDamage(skill, enemies, targetUnitId));
                break;
                
            case SkillEffectType.AreaOfEffect:
                effects.AddRange(ApplyAreaDamage(skill, tower, enemies, targetPosition));
                break;
                
            case SkillEffectType.Buff:
            case SkillEffectType.Debuff:
            case SkillEffectType.Utility:
                // Phase 2: Implement buff/debuff system
                break;
        }
        
        return effects;
    }
    
    private List<SkillEffectResult> ApplyTargetedDamage(
        TowerSkill skill,
        List<Unit> enemies,
        int? targetUnitId)
    {
        var effects = new List<SkillEffectResult>();
        
        if (targetUnitId == null) return effects;
        
        var target = enemies.FirstOrDefault(u => u.Id == targetUnitId);
        if (target == null || target.IsDead) return effects;
        
        target.TakeDamage(skill.Damage);
        
        effects.Add(new SkillEffectResult
        {
            Type = "Damage",
            TargetId = target.Id.ToString(),
            Value = skill.Damage
        });
        
        return effects;
    }
    
    private List<SkillEffectResult> ApplyAreaDamage(
        TowerSkill skill,
        Tower tower,
        List<Unit> enemies,
        Vector2? targetPosition)
    {
        var effects = new List<SkillEffectResult>();
        
        var center = targetPosition ?? tower.Position;
        
        foreach (var enemy in enemies)
        {
            if (enemy.IsDead) continue;
            
            var distance = Vector2.Distance(center, enemy.Position);
            if (distance > skill.Range) continue;
            
            enemy.TakeDamage(skill.Damage);
            
            effects.Add(new SkillEffectResult
            {
                Type = "Damage",
                TargetId = enemy.Id.ToString(),
                Value = skill.Damage
            });
        }
        
        return effects;
    }
    
    public void ClearSkills(int towerId)
    {
        _towerSkills.Remove(towerId);
    }
    
    public void ClearAllSkills()
    {
        _towerSkills.Clear();
    }
}
