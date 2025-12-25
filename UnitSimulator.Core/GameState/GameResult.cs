namespace UnitSimulator;

/// <summary>
/// 게임 결과 상태
/// </summary>
public enum GameResult
{
    /// <summary>
    /// 게임 진행 중
    /// </summary>
    InProgress,

    /// <summary>
    /// 아군 승리
    /// </summary>
    FriendlyWin,

    /// <summary>
    /// 적군 승리
    /// </summary>
    EnemyWin,

    /// <summary>
    /// 무승부
    /// </summary>
    Draw
}

/// <summary>
/// 승리 조건 유형
/// </summary>
public enum WinCondition
{
    /// <summary>
    /// King Tower 파괴로 승리
    /// </summary>
    KingDestroyed,

    /// <summary>
    /// 정규 시간 종료 시 더 많은 크라운으로 승리
    /// </summary>
    MoreCrownCount,

    /// <summary>
    /// 연장전 종료 시 타워 HP 비율로 승리
    /// </summary>
    MoreTowerDamage,

    /// <summary>
    /// 서든데스에서 첫 타워 파괴로 승리
    /// </summary>
    TieBreaker
}
