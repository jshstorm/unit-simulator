using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// 맵 레이아웃 정의.
/// 클래시로얄 스타일의 세로 맵 (3200 x 5100).
/// </summary>
public static class MapLayout
{
    // ════════════════════════════════════════════════════════════════════════
    // 맵 크기
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 맵 너비
    /// </summary>
    public const float MapWidth = GameConstants.SIMULATION_WIDTH;

    /// <summary>
    /// 맵 높이
    /// </summary>
    public const float MapHeight = GameConstants.SIMULATION_HEIGHT;

    /// <summary>
    /// 타일 크기 (참조용)
    /// </summary>
    public const float TileSize = 100f;

    // ════════════════════════════════════════════════════════════════════════
    // Friendly 타워 위치 (맵 하단)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Friendly King Tower 위치
    /// </summary>
    public static Vector2 FriendlyKingPosition => new(1600f, 700f);

    /// <summary>
    /// Friendly 왼쪽 Princess Tower 위치
    /// </summary>
    public static Vector2 FriendlyPrincessLeftPosition => new(600f, 1200f);

    /// <summary>
    /// Friendly 오른쪽 Princess Tower 위치
    /// </summary>
    public static Vector2 FriendlyPrincessRightPosition => new(2600f, 1200f);

    // ════════════════════════════════════════════════════════════════════════
    // Enemy 타워 위치 (맵 상단)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Enemy King Tower 위치
    /// </summary>
    public static Vector2 EnemyKingPosition => new(1600f, 4400f);

    /// <summary>
    /// Enemy 왼쪽 Princess Tower 위치
    /// </summary>
    public static Vector2 EnemyPrincessLeftPosition => new(600f, 3900f);

    /// <summary>
    /// Enemy 오른쪽 Princess Tower 위치
    /// </summary>
    public static Vector2 EnemyPrincessRightPosition => new(2600f, 3900f);

    // ════════════════════════════════════════════════════════════════════════
    // 강 (River) 영역
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 강 시작 Y 좌표
    /// </summary>
    public const float RiverYMin = 2400f;

    /// <summary>
    /// 강 끝 Y 좌표
    /// </summary>
    public const float RiverYMax = 2700f;

    /// <summary>
    /// 강 너비
    /// </summary>
    public const float RiverWidth = RiverYMax - RiverYMin;

    // ════════════════════════════════════════════════════════════════════════
    // 다리 (Bridge) 영역
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 왼쪽 다리 X 시작
    /// </summary>
    public const float LeftBridgeXMin = 400f;

    /// <summary>
    /// 왼쪽 다리 X 끝
    /// </summary>
    public const float LeftBridgeXMax = 800f;

    /// <summary>
    /// 오른쪽 다리 X 시작
    /// </summary>
    public const float RightBridgeXMin = 2400f;

    /// <summary>
    /// 오른쪽 다리 X 끝
    /// </summary>
    public const float RightBridgeXMax = 2800f;

    // ════════════════════════════════════════════════════════════════════════
    // 스폰 영역 (진영별 배치 가능 범위)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Friendly 유닛 스폰 가능 Y 최대값 (강 이전)
    /// </summary>
    public const float FriendlySpawnYMax = RiverYMin;

    /// <summary>
    /// Enemy 유닛 스폰 가능 Y 최소값 (강 이후)
    /// </summary>
    public const float EnemySpawnYMin = RiverYMax;

    // ════════════════════════════════════════════════════════════════════════
    // 스폰 존 (유닛 생성 시 기본 영역)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Friendly 스폰 존 X 최소값
    /// </summary>
    public const float FriendlySpawnZoneXMin = 800f;

    /// <summary>
    /// Friendly 스폰 존 X 최대값
    /// </summary>
    public const float FriendlySpawnZoneXMax = 2400f;

    /// <summary>
    /// Friendly 스폰 존 Y 최소값 (King Tower 전방)
    /// </summary>
    public const float FriendlySpawnZoneYMin = 1400f;

    /// <summary>
    /// Friendly 스폰 존 Y 최대값
    /// </summary>
    public const float FriendlySpawnZoneYMax = 1700f;

    /// <summary>
    /// Enemy 스폰 존 X 최소값
    /// </summary>
    public const float EnemySpawnZoneXMin = 800f;

    /// <summary>
    /// Enemy 스폰 존 X 최대값
    /// </summary>
    public const float EnemySpawnZoneXMax = 2400f;

    /// <summary>
    /// Enemy 스폰 존 Y 최소값 (King Tower 전방)
    /// </summary>
    public const float EnemySpawnZoneYMin = 3400f;

    /// <summary>
    /// Enemy 스폰 존 Y 최대값
    /// </summary>
    public const float EnemySpawnZoneYMax = 3700f;

    /// <summary>
    /// Friendly 기본 스폰 위치 (King Tower 전방 중앙)
    /// </summary>
    public static Vector2 FriendlyDefaultSpawnPosition => new(1600f, 1500f);

    /// <summary>
    /// Enemy 기본 스폰 위치 (King Tower 전방 중앙)
    /// </summary>
    public static Vector2 EnemyDefaultSpawnPosition => new(1600f, 3600f);

    // ════════════════════════════════════════════════════════════════════════
    // 유틸리티 메서드
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 지정된 위치가 강 영역 내에 있는지 확인합니다.
    /// </summary>
    public static bool IsInRiver(Vector2 position)
    {
        return position.Y >= RiverYMin && position.Y <= RiverYMax;
    }

    /// <summary>
    /// 지정된 위치가 다리 위에 있는지 확인합니다.
    /// </summary>
    public static bool IsOnBridge(Vector2 position)
    {
        if (!IsInRiver(position)) return false;

        bool onLeftBridge = position.X >= LeftBridgeXMin && position.X <= LeftBridgeXMax;
        bool onRightBridge = position.X >= RightBridgeXMin && position.X <= RightBridgeXMax;

        return onLeftBridge || onRightBridge;
    }

    /// <summary>
    /// Ground 유닛이 해당 위치로 이동 가능한지 확인합니다.
    /// 강 영역은 다리를 통해서만 통과 가능합니다.
    /// </summary>
    public static bool CanGroundUnitMoveTo(Vector2 position)
    {
        if (IsInRiver(position) && !IsOnBridge(position))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 맵 경계 내에 있는지 확인합니다.
    /// </summary>
    public static bool IsWithinBounds(Vector2 position)
    {
        return position.X >= 0 && position.X <= MapWidth
            && position.Y >= 0 && position.Y <= MapHeight;
    }

    /// <summary>
    /// 위치를 맵 경계 내로 클램핑합니다.
    /// </summary>
    public static Vector2 ClampToBounds(Vector2 position)
    {
        return new Vector2(
            Math.Clamp(position.X, 0, MapWidth),
            Math.Clamp(position.Y, 0, MapHeight)
        );
    }
}
