using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace AvalonDevTool.GoogleSheets;

/// <summary>
/// Google Sheets 데이터를 XML 형식으로 변환하고 배포하는 빌더 클래스
/// </summary>
public class DataSheetBuilder
{
    private readonly string _credentialsPath;
    private readonly string _outputDirectory;

    public DataSheetBuilder(string credentialsPath, string outputDirectory)
    {
        _credentialsPath = credentialsPath ?? throw new ArgumentNullException(nameof(credentialsPath));
        _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
    }

    /// <summary>
    /// 스프레드시트 ID 목록에서 데이터를 가져와 XML 형식으로 변환합니다.
    /// </summary>
    /// <param name="spreadsheetIds">Google Spreadsheet ID 목록</param>
    /// <param name="versionString">버전 문자열 (선택)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>빌드 결과</returns>
    public async Task<DataSheetBuildResult> BuildAsync(
        IEnumerable<string> spreadsheetIds,
        string? versionString = null,
        CancellationToken cancellationToken = default)
    {
        var result = new DataSheetBuildResult();
        var allSheetsData = new List<SheetData>();

        // 출력 디렉토리 생성
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }

        try
        {
            using var sheetsService = new GoogleSheetsService(_credentialsPath);

            foreach (var spreadsheetId in spreadsheetIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AnsiConsole.MarkupLine($"[white]스프레드시트 [{spreadsheetId}] 데이터를 가져오는 중...[/]");

                var spreadsheet = await sheetsService.GetSpreadsheetAsync(spreadsheetId);
                var sheetsData = await sheetsService.GetAllSheetsDataAsync(spreadsheetId);

                AnsiConsole.MarkupLine($"[green]스프레드시트 '{spreadsheet.Properties?.Title}' - {sheetsData.Count}개 시트 로드 완료[/]");

                allSheetsData.AddRange(sheetsData);
            }

            // XML 파일 저장
            AnsiConsole.MarkupLine($"[white]XML 파일로 변환 중...[/]");
            var xmlPaths = XmlConverter.SaveAllToXmlFiles(allSheetsData, _outputDirectory);
            result.XmlFiles.AddRange(xmlPaths);

            // Bytes 파일 저장 (XML을 바이트로)
            AnsiConsole.MarkupLine($"[white]Bytes 파일로 변환 중...[/]");
            var bytesPaths = XmlConverter.SaveAllToBytesFiles(allSheetsData, _outputDirectory);
            result.BytesFiles.AddRange(bytesPaths);

            // JSON 파일 저장
            AnsiConsole.MarkupLine($"[white]JSON 파일로 변환 중...[/]");
            var jsonPaths = XmlConverter.SaveAllToJsonFiles(allSheetsData, _outputDirectory);
            result.JsonFiles.AddRange(jsonPaths);

            // 파일 목록 생성
            await CreateFileListAsync(result, versionString, cancellationToken);

            result.Success = true;
            AnsiConsole.MarkupLine($"[green]총 {allSheetsData.Count}개 시트 변환 완료[/]");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            AnsiConsole.MarkupLine($"[red]빌드 실패: {ex.Message}[/]");
        }

        return result;
    }

    /// <summary>
    /// 스프레드시트 설정 파일에서 ID를 읽어 빌드합니다.
    /// </summary>
    /// <param name="configPath">설정 파일 경로</param>
    /// <param name="versionString">버전 문자열 (선택)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    public async Task<DataSheetBuildResult> BuildFromConfigAsync(
        string configPath,
        string? versionString = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[yellow]설정 파일이 없습니다: {configPath}. 빈 결과를 반환합니다.[/]");
            return new DataSheetBuildResult { Success = true };
        }

        var configJson = await File.ReadAllTextAsync(configPath, cancellationToken);
        var config = JsonSerializer.Deserialize<DataSheetConfig>(configJson);

        if (config?.SpreadsheetIds == null || config.SpreadsheetIds.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]설정 파일에 스프레드시트 ID가 없습니다.[/]");
            return new DataSheetBuildResult { Success = true };
        }

        return await BuildAsync(config.SpreadsheetIds, versionString, cancellationToken);
    }

    private async Task CreateFileListAsync(DataSheetBuildResult result, string? versionString, CancellationToken cancellationToken)
    {
        var fileList = new FileListInfo
        {
            Branch = "",
            BranchHash = "",
            GameData = versionString ?? "",
            Bundle = "",
            GameDataList = result.BytesFiles.Select(Path.GetFileName).ToList()!
        };

        var fileListPath = Path.Combine(_outputDirectory, "filelist.json");
        var json = JsonSerializer.Serialize(fileList, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(fileListPath, json, new UTF8Encoding(false), cancellationToken);

        result.FileListPath = fileListPath;
        AnsiConsole.MarkupLine($"[green]파일 목록 생성 완료: {fileListPath}[/]");
    }
}

/// <summary>
/// 데이터 시트 빌드 결과
/// </summary>
public class DataSheetBuildResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> XmlFiles { get; } = new();
    public List<string> BytesFiles { get; } = new();
    public List<string> JsonFiles { get; } = new();
    public string? FileListPath { get; set; }
}

/// <summary>
/// 데이터 시트 설정
/// </summary>
public class DataSheetConfig
{
    [JsonPropertyName("spreadsheet_ids")]
    public List<string>? SpreadsheetIds { get; set; }

    [JsonPropertyName("text_spreadsheet_ids")]
    public List<string>? TextSpreadsheetIds { get; set; }
}

/// <summary>
/// 파일 목록 정보
/// </summary>
public class FileListInfo
{
    [JsonPropertyOrder(0), JsonPropertyName("branch")]
    public string? Branch { get; set; }

    [JsonPropertyOrder(1), JsonPropertyName("branch_hash")]
    public string? BranchHash { get; set; }

    [JsonPropertyOrder(2), JsonPropertyName("gamedata")]
    public string? GameData { get; set; }

    [JsonPropertyOrder(3), JsonPropertyName("bundle")]
    public string? Bundle { get; set; }

    [JsonPropertyOrder(4), JsonPropertyName("gamedata_list")]
    public List<string?>? GameDataList { get; set; }
}
