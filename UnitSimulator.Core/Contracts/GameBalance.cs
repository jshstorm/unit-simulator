namespace UnitSimulator.Core.Contracts;

/// <summary>
/// 게임 밸런스 및 시뮬레이션 설정 데이터.
/// GameConstants를 런타임에 재정의할 수 있습니다.
/// </summary>
public sealed class GameBalance
{
    /// <summary>데이터 버전</summary>
    public int Version { get; init; } = 1;

    // ═══════════════════════════════════════════════════════════════════
    // Simulation Space
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>시뮬레이션 공간 너비</summary>
    public int SimulationWidth { get; init; } = 3200;

    /// <summary>시뮬레이션 공간 높이</summary>
    public int SimulationHeight { get; init; } = 5100;

    /// <summary>최대 프레임 수</summary>
    public int MaxFrames { get; init; } = 3000;

    /// <summary>프레임당 시간 (초)</summary>
    public float FrameTimeSeconds { get; init; } = 1f / 30f;

    // ═══════════════════════════════════════════════════════════════════
    // Unit Settings
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>기본 유닛 반경</summary>
    public float UnitRadius { get; init; } = 20f;

    /// <summary>충돌 반경 스케일</summary>
    public float CollisionRadiusScale { get; init; } = 2f / 3f;

    /// <summary>공격 슬롯 수</summary>
    public int NumAttackSlots { get; init; } = 8;

    /// <summary>슬롯 재평가 거리</summary>
    public float SlotReevaluateDistance { get; init; } = 40f;

    /// <summary>슬롯 재평가 간격 프레임</summary>
    public int SlotReevaluateIntervalFrames { get; init; } = 60;

    // ═══════════════════════════════════════════════════════════════════
    // Combat Settings
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>공격 쿨다운 (프레임)</summary>
    public float AttackCooldown { get; init; } = 30f;

    /// <summary>근접 공격 범위 배율</summary>
    public int MeleeRangeMultiplier { get; init; } = 3;

    /// <summary>원거리 공격 범위 배율</summary>
    public int RangedRangeMultiplier { get; init; } = 6;

    /// <summary>교전 트리거 거리 배율</summary>
    public float EngagementTriggerDistanceMultiplier { get; init; } = 1.5f;

    // ═══════════════════════════════════════════════════════════════════
    // Squad Behavior
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>집결 거리</summary>
    public float RallyDistance { get; init; } = 300f;

    /// <summary>대형 임계값</summary>
    public float FormationThreshold { get; init; } = 20f;

    /// <summary>분리 반경</summary>
    public float SeparationRadius { get; init; } = 120f;

    /// <summary>아군 분리 반경</summary>
    public float FriendlySeparationRadius { get; init; } = 80f;

    /// <summary>목적지 도착 임계값</summary>
    public float DestinationThreshold { get; init; } = 10f;

    // ═══════════════════════════════════════════════════════════════════
    // Wave Settings
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>최대 웨이브 수</summary>
    public int MaxWaves { get; init; } = 3;

    // ═══════════════════════════════════════════════════════════════════
    // Targeting Settings
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>타겟 재평가 간격 프레임</summary>
    public int TargetReevaluateIntervalFrames { get; init; } = 45;

    /// <summary>타겟 전환 마진</summary>
    public float TargetSwitchMargin { get; init; } = 15f;

    /// <summary>타겟 밀집 패널티 (공격자당)</summary>
    public float TargetCrowdPenaltyPerAttacker { get; init; } = 25f;

    // ═══════════════════════════════════════════════════════════════════
    // Avoidance Settings
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>회피 각도 스텝 (라디안)</summary>
    public float AvoidanceAngleStep { get; init; } = MathF.PI / 8f;

    /// <summary>최대 회피 반복 횟수</summary>
    public int MaxAvoidanceIterations { get; init; } = 8;

    /// <summary>회피 최대 선행 거리</summary>
    public float AvoidanceMaxLookahead { get; init; } = 3.5f;

    // ═══════════════════════════════════════════════════════════════════
    // Collision Resolution
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>충돌 해소 반복 횟수</summary>
    public int CollisionResolutionIterations { get; init; } = 3;

    /// <summary>충돌 밀어내기 강도</summary>
    public float CollisionPushStrength { get; init; } = 0.8f;

    /// <summary>
    /// 기본 게임 밸런스 설정
    /// </summary>
    public static GameBalance Default => new();
}
