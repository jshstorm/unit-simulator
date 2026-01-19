namespace UnitSimulator.Core.Contracts;

/// <summary>
/// 런타임에 게임 데이터를 제공하는 인터페이스.
/// SimulatorCore에서 유닛 스탯, 웨이브 정의, 밸런스 설정을 조회할 때 사용합니다.
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// 유닛 ID로 스탯을 조회합니다.
    /// </summary>
    /// <param name="unitId">유닛 ID (예: "skeleton", "golem")</param>
    /// <returns>유닛 스탯, 없으면 기본값</returns>
    UnitStats GetUnitStats(string unitId);

    /// <summary>
    /// 유닛 ID가 존재하는지 확인합니다.
    /// </summary>
    bool HasUnit(string unitId);

    /// <summary>
    /// 모든 유닛 ID 목록을 반환합니다.
    /// </summary>
    IEnumerable<string> GetAllUnitIds();

    /// <summary>
    /// 웨이브 번호로 웨이브 정의를 조회합니다.
    /// </summary>
    /// <param name="waveNumber">1-based 웨이브 번호</param>
    /// <returns>웨이브 정의, 없으면 빈 정의</returns>
    WaveDefinition GetWaveDefinition(int waveNumber);

    /// <summary>
    /// 정의된 총 웨이브 수를 반환합니다.
    /// </summary>
    int GetTotalWaveCount();

    /// <summary>
    /// 게임 밸런스 설정을 반환합니다.
    /// </summary>
    GameBalance GetGameBalance();

    /// <summary>
    /// 데이터를 다시 로드합니다 (개발 모드용).
    /// </summary>
    void Reload();
}
