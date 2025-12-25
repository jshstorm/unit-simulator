using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// 게임 세션 상태를 관리합니다.
/// 타워, 크라운, 시간, 승패 상태 등을 포함합니다.
/// </summary>
public class GameSession
{
    // ════════════════════════════════════════════════════════════════════════
    // 타워
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Friendly 진영 타워 목록
    /// </summary>
    public List<Tower> FriendlyTowers { get; } = new();

    /// <summary>
    /// Enemy 진영 타워 목록
    /// </summary>
    public List<Tower> EnemyTowers { get; } = new();

    // ════════════════════════════════════════════════════════════════════════
    // 게임 시간
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 경과 시간 (초)
    /// </summary>
    public float ElapsedTime { get; set; }

    /// <summary>
    /// 정규 시간 (초) - 기본 180초 (3분)
    /// </summary>
    public float RegularTime { get; init; } = 180f;

    /// <summary>
    /// 최대 게임 시간 (초) - 기본 300초 (5분, 연장전 포함)
    /// </summary>
    public float MaxGameTime { get; set; } = 300f;

    // ════════════════════════════════════════════════════════════════════════
    // 크라운 및 결과
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Friendly 진영 획득 크라운 수
    /// </summary>
    public int FriendlyCrowns { get; set; }

    /// <summary>
    /// Enemy 진영 획득 크라운 수
    /// </summary>
    public int EnemyCrowns { get; set; }

    /// <summary>
    /// 현재 게임 결과
    /// </summary>
    public GameResult Result { get; set; } = GameResult.InProgress;

    /// <summary>
    /// 승리 조건 (게임 종료 시)
    /// </summary>
    public WinCondition? WinConditionType { get; set; }

    /// <summary>
    /// 연장전 여부
    /// </summary>
    public bool IsOvertime { get; set; }

    // ════════════════════════════════════════════════════════════════════════
    // 초기화
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 기본 타워 배치로 게임 세션을 초기화합니다.
    /// </summary>
    public void InitializeDefaultTowers()
    {
        FriendlyTowers.Clear();
        EnemyTowers.Clear();

        int towerId = 1;

        // Friendly 타워
        FriendlyTowers.Add(TowerStats.CreateKingTower(
            towerId++, UnitFaction.Friendly, MapLayout.FriendlyKingPosition));
        FriendlyTowers.Add(TowerStats.CreatePrincessTower(
            towerId++, UnitFaction.Friendly, MapLayout.FriendlyPrincessLeftPosition));
        FriendlyTowers.Add(TowerStats.CreatePrincessTower(
            towerId++, UnitFaction.Friendly, MapLayout.FriendlyPrincessRightPosition));

        // Enemy 타워
        EnemyTowers.Add(TowerStats.CreateKingTower(
            towerId++, UnitFaction.Enemy, MapLayout.EnemyKingPosition));
        EnemyTowers.Add(TowerStats.CreatePrincessTower(
            towerId++, UnitFaction.Enemy, MapLayout.EnemyPrincessLeftPosition));
        EnemyTowers.Add(TowerStats.CreatePrincessTower(
            towerId++, UnitFaction.Enemy, MapLayout.EnemyPrincessRightPosition));

        // 상태 초기화
        ElapsedTime = 0f;
        FriendlyCrowns = 0;
        EnemyCrowns = 0;
        Result = GameResult.InProgress;
        WinConditionType = null;
        IsOvertime = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    // 타워 조회
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 지정된 진영의 모든 타워를 반환합니다.
    /// </summary>
    public IEnumerable<Tower> GetTowers(UnitFaction faction)
    {
        return faction == UnitFaction.Friendly ? FriendlyTowers : EnemyTowers;
    }

    /// <summary>
    /// 지정된 진영의 King Tower를 반환합니다.
    /// </summary>
    public Tower? GetKingTower(UnitFaction faction)
    {
        var towers = GetTowers(faction);
        return towers.FirstOrDefault(t => t.Type == TowerType.King);
    }

    /// <summary>
    /// 지정된 진영의 살아있는 Princess Tower 목록을 반환합니다.
    /// </summary>
    public IEnumerable<Tower> GetLivingPrincessTowers(UnitFaction faction)
    {
        return GetTowers(faction)
            .Where(t => t.Type == TowerType.Princess && !t.IsDestroyed);
    }

    /// <summary>
    /// 지정된 진영의 살아있는 모든 타워를 반환합니다.
    /// </summary>
    public IEnumerable<Tower> GetLivingTowers(UnitFaction faction)
    {
        return GetTowers(faction).Where(t => !t.IsDestroyed);
    }

    /// <summary>
    /// 모든 타워를 반환합니다.
    /// </summary>
    public IEnumerable<Tower> GetAllTowers()
    {
        return FriendlyTowers.Concat(EnemyTowers);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 크라운 계산
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 파괴된 적 타워 수를 기준으로 크라운을 계산하고 업데이트합니다.
    /// </summary>
    public void UpdateCrowns()
    {
        FriendlyCrowns = CountCrownsFromDestroyedTowers(UnitFaction.Enemy);
        EnemyCrowns = CountCrownsFromDestroyedTowers(UnitFaction.Friendly);
    }

    /// <summary>
    /// 지정된 진영의 파괴된 타워로부터 크라운 수를 계산합니다.
    /// </summary>
    private int CountCrownsFromDestroyedTowers(UnitFaction destroyedFaction)
    {
        int crowns = 0;
        foreach (var tower in GetTowers(destroyedFaction))
        {
            if (tower.IsDestroyed)
            {
                crowns += tower.Type == TowerType.King ? 3 : 1;
            }
        }
        // 최대 3 크라운 (King 파괴 시 게임 종료이므로)
        return Math.Min(crowns, 3);
    }

    // ════════════════════════════════════════════════════════════════════════
    // King Tower 활성화
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 조건에 따라 King Tower를 활성화합니다.
    /// Princess Tower가 파괴되면 해당 진영의 King Tower가 활성화됩니다.
    /// </summary>
    public void UpdateKingTowerActivation()
    {
        UpdateKingActivationForFaction(UnitFaction.Friendly);
        UpdateKingActivationForFaction(UnitFaction.Enemy);
    }

    private void UpdateKingActivationForFaction(UnitFaction faction)
    {
        var king = GetKingTower(faction);
        if (king == null || king.IsActivated) return;

        // Princess Tower 중 하나라도 파괴되면 King 활성화
        bool princessDestroyed = GetTowers(faction)
            .Any(t => t.Type == TowerType.Princess && t.IsDestroyed);

        if (princessDestroyed)
        {
            king.IsActivated = true;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // 타워 HP 비율
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 지정된 진영의 총 타워 HP 비율을 반환합니다.
    /// </summary>
    public float GetTotalTowerHPRatio(UnitFaction faction)
    {
        var towers = GetTowers(faction).ToList();
        if (towers.Count == 0) return 0f;

        float currentHP = towers.Sum(t => t.CurrentHP);
        float maxHP = towers.Sum(t => t.MaxHP);

        return maxHP > 0 ? currentHP / maxHP : 0f;
    }
}
