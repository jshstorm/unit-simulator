namespace UnitSimulator;

/// <summary>
/// 타워 유형을 정의합니다.
/// </summary>
public enum TowerType
{
    /// <summary>
    /// 사이드 타워 (프린세스 타워) - 2개, 즉시 공격 가능한 대상
    /// </summary>
    Princess,

    /// <summary>
    /// 킹 타워 - 1개, Princess 파괴 또는 직접 피해 시 활성화
    /// </summary>
    King
}
