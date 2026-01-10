using System.Numerics;

namespace UnitSimulator.Skills;

/// <summary>
/// 스킬 효과 타입
/// </summary>
public enum SkillEffectType
{
    /// <summary>단일 대상 데미지</summary>
    TargetedDamage,
    
    /// <summary>범위 데미지</summary>
    AreaOfEffect,
    
    /// <summary>아군 버프</summary>
    Buff,
    
    /// <summary>적군 디버프</summary>
    Debuff,
    
    /// <summary>특수 효과</summary>
    Utility
}

/// <summary>
/// 스킬 대상 타입
/// </summary>
public enum SkillTargetType
{
    /// <summary>대상 지정 불필요 (자동 또는 자기 자신)</summary>
    None,
    
    /// <summary>단일 유닛 대상</summary>
    SingleUnit,
    
    /// <summary>위치 지정 (범위 스킬)</summary>
    Position
}

/// <summary>
/// 타워 스킬 정의
/// </summary>
public class TowerSkill
{
    /// <summary>
    /// 스킬 고유 ID
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// 스킬 이름
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 스킬 효과 타입
    /// </summary>
    public required SkillEffectType EffectType { get; init; }
    
    /// <summary>
    /// 대상 타입
    /// </summary>
    public required SkillTargetType TargetType { get; init; }
    
    /// <summary>
    /// 쿨다운 시간 (밀리초)
    /// </summary>
    public required int CooldownMs { get; init; }
    
    /// <summary>
    /// 스킬 범위 (범위 스킬용)
    /// </summary>
    public float Range { get; init; }
    
    /// <summary>
    /// 기본 데미지
    /// </summary>
    public int Damage { get; init; }
    
    /// <summary>
    /// 효과 지속 시간 (밀리초, 버프/디버프용)
    /// </summary>
    public int DurationMs { get; init; }
    
    /// <summary>
    /// 버프/디버프 수치 (퍼센트, 예: 20 = 20% 증가)
    /// </summary>
    public int EffectValue { get; init; }
    
    // ════════════════════════════════════════════════════════════════════════
    // 런타임 상태
    // ════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// 현재 쿨다운 남은 시간 (밀리초)
    /// </summary>
    public int RemainingCooldownMs { get; private set; }
    
    /// <summary>
    /// 스킬이 쿨다운 중인지 여부
    /// </summary>
    public bool IsOnCooldown => RemainingCooldownMs > 0;
    
    /// <summary>
    /// 쿨다운을 시작합니다.
    /// </summary>
    public void StartCooldown()
    {
        RemainingCooldownMs = CooldownMs;
    }
    
    /// <summary>
    /// 쿨다운을 업데이트합니다.
    /// </summary>
    /// <param name="deltaMs">경과 시간 (밀리초)</param>
    public void UpdateCooldown(int deltaMs)
    {
        if (RemainingCooldownMs > 0)
        {
            RemainingCooldownMs = Math.Max(0, RemainingCooldownMs - deltaMs);
        }
    }
    
    /// <summary>
    /// 쿨다운을 리셋합니다 (테스트/디버그용).
    /// </summary>
    public void ResetCooldown()
    {
        RemainingCooldownMs = 0;
    }
}

/// <summary>
/// 스킬 발동 결과
/// </summary>
public class SkillActivationResult
{
    /// <summary>
    /// 발동 성공 여부
    /// </summary>
    public required bool Success { get; init; }
    
    /// <summary>
    /// 쿨다운 시간 (밀리초)
    /// </summary>
    public int? CooldownMs { get; init; }
    
    /// <summary>
    /// 적용된 효과 목록
    /// </summary>
    public List<SkillEffectResult>? Effects { get; init; }
    
    /// <summary>
    /// 에러 코드 (실패 시)
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// 에러 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// 성공 결과 생성
    /// </summary>
    public static SkillActivationResult CreateSuccess(int cooldownMs, List<SkillEffectResult> effects)
    {
        return new SkillActivationResult
        {
            Success = true,
            CooldownMs = cooldownMs,
            Effects = effects
        };
    }
    
    /// <summary>
    /// 실패 결과 생성
    /// </summary>
    public static SkillActivationResult CreateFailure(string errorCode, string errorMessage)
    {
        return new SkillActivationResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// 스킬 효과 결과
/// </summary>
public class SkillEffectResult
{
    /// <summary>
    /// 효과 타입
    /// </summary>
    public required string Type { get; init; }
    
    /// <summary>
    /// 대상 ID
    /// </summary>
    public required string TargetId { get; init; }
    
    /// <summary>
    /// 효과 값 (데미지량, 버프 수치 등)
    /// </summary>
    public int? Value { get; init; }
    
    /// <summary>
    /// 지속 시간 (밀리초)
    /// </summary>
    public int? DurationMs { get; init; }
}

/// <summary>
/// 스킬 에러 코드
/// </summary>
public static class SkillErrorCodes
{
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string InvalidTowerId = "INVALID_TOWER_ID";
    public const string TowerNotFound = "TOWER_NOT_FOUND";
    public const string InvalidSkillId = "INVALID_SKILL_ID";
    public const string SkillNotFound = "SKILL_NOT_FOUND";
    public const string SkillOnCooldown = "SKILL_ON_COOLDOWN";
    public const string TargetRequired = "TARGET_REQUIRED";
    public const string InvalidTargetPosition = "INVALID_TARGET_POSITION";
    public const string TargetNotFound = "TARGET_NOT_FOUND";
    public const string InternalError = "INTERNAL_ERROR";
}
