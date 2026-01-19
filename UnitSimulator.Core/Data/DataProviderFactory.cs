using UnitSimulator.Core.Contracts;

namespace UnitSimulator.Core.Data;

/// <summary>
/// 데이터 제공자 팩토리.
/// 환경에 따라 적절한 IDataProvider 구현체를 생성합니다.
/// </summary>
public static class DataProviderFactory
{
    /// <summary>
    /// 지정된 디렉토리에서 JSON 데이터를 로드하는 제공자를 생성합니다.
    /// 디렉토리가 존재하지 않으면 기본 제공자를 반환합니다.
    /// </summary>
    /// <param name="dataDirectory">JSON 데이터 디렉토리 경로</param>
    /// <param name="logger">로그 출력 함수 (선택)</param>
    /// <returns>IDataProvider 구현체</returns>
    public static IDataProvider Create(string? dataDirectory, Action<string>? logger = null)
    {
        if (string.IsNullOrEmpty(dataDirectory) || !Directory.Exists(dataDirectory))
        {
            logger?.Invoke($"[DataProviderFactory] Data directory not found, using DefaultDataProvider");
            return new DefaultDataProvider();
        }

        try
        {
            return new JsonDataProvider(dataDirectory, logger);
        }
        catch (Exception ex)
        {
            logger?.Invoke($"[DataProviderFactory] Failed to create JsonDataProvider: {ex.Message}");
            return new DefaultDataProvider();
        }
    }

    /// <summary>
    /// 기본 데이터 제공자를 생성합니다 (테스트용).
    /// </summary>
    public static IDataProvider CreateDefault()
    {
        return new DefaultDataProvider();
    }

    /// <summary>
    /// 프로젝트 기본 경로에서 데이터를 로드합니다.
    /// processed/ 폴더를 우선 사용하고, 없으면 references/ 폴더를 사용합니다.
    /// </summary>
    /// <param name="projectRoot">프로젝트 루트 경로</param>
    /// <param name="logger">로그 출력 함수 (선택)</param>
    public static IDataProvider CreateFromProject(string projectRoot, Action<string>? logger = null)
    {
        // 우선순위: processed > references
        var processedPath = Path.Combine(projectRoot, "data", "processed");
        if (Directory.Exists(processedPath))
        {
            logger?.Invoke($"[DataProviderFactory] Using processed data: {processedPath}");
            return Create(processedPath, logger);
        }

        var referencesPath = Path.Combine(projectRoot, "data", "references");
        if (Directory.Exists(referencesPath))
        {
            logger?.Invoke($"[DataProviderFactory] Using reference data: {referencesPath}");
            return Create(referencesPath, logger);
        }

        logger?.Invoke($"[DataProviderFactory] No data directory found in project: {projectRoot}");
        return new DefaultDataProvider();
    }
}
