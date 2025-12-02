using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace AvalonDevTool.GoogleSheets;

/// <summary>
/// Google Sheets API를 통해 스프레드시트 데이터를 가져오는 서비스 클래스
/// </summary>
public class GoogleSheetsService : IDisposable
{
    private readonly SheetsService _sheetsService;

    /// <summary>
    /// 서비스 계정 인증을 사용하여 GoogleSheetsService를 초기화합니다.
    /// </summary>
    /// <param name="credentialsPath">서비스 계정 JSON 파일 경로</param>
    public GoogleSheetsService(string credentialsPath)
    {
        if (string.IsNullOrWhiteSpace(credentialsPath))
            throw new ArgumentException("Credentials path cannot be null or empty.", nameof(credentialsPath));

        if (!File.Exists(credentialsPath))
            throw new FileNotFoundException($"Credentials file not found: {credentialsPath}");

        // Using stream-based approach to read credentials file safely
        // Note: GoogleCredential.FromStream is deprecated but CredentialFactory is not yet widely adopted
#pragma warning disable CS0618 // Type or member is obsolete
        using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var credential = GoogleCredential.FromStream(stream)
            .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
#pragma warning restore CS0618

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AvalonDevTool Google Sheets to XML Converter"
        });
    }

    /// <summary>
    /// 스프레드시트의 모든 시트 정보를 가져옵니다.
    /// </summary>
    /// <param name="spreadsheetId">Google Spreadsheet ID</param>
    /// <returns>스프레드시트 정보</returns>
    public async Task<Spreadsheet> GetSpreadsheetAsync(string spreadsheetId)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
            throw new ArgumentException("Spreadsheet ID cannot be null or empty.", nameof(spreadsheetId));

        var request = _sheetsService.Spreadsheets.Get(spreadsheetId);
        return await request.ExecuteAsync();
    }

    /// <summary>
    /// 스프레드시트의 모든 시트 이름을 가져옵니다.
    /// </summary>
    /// <param name="spreadsheetId">Google Spreadsheet ID</param>
    /// <returns>시트 이름 목록</returns>
    public async Task<IList<string>> GetSheetNamesAsync(string spreadsheetId)
    {
        var spreadsheet = await GetSpreadsheetAsync(spreadsheetId);
        return spreadsheet.Sheets.Select(s => s.Properties.Title).ToList();
    }

    /// <summary>
    /// 특정 시트의 데이터를 가져옵니다.
    /// </summary>
    /// <param name="spreadsheetId">Google Spreadsheet ID</param>
    /// <param name="sheetName">시트 이름</param>
    /// <returns>시트 데이터 (행과 열의 2차원 배열)</returns>
    public async Task<SheetData> GetSheetDataAsync(string spreadsheetId, string sheetName)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
            throw new ArgumentException("Spreadsheet ID cannot be null or empty.", nameof(spreadsheetId));

        if (string.IsNullOrWhiteSpace(sheetName))
            throw new ArgumentException("Sheet name cannot be null or empty.", nameof(sheetName));

        var range = $"'{sheetName}'";
        var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
        var response = await request.ExecuteAsync();

        var values = response.Values ?? new List<IList<object>>();
        return new SheetData(sheetName, values);
    }

    /// <summary>
    /// 스프레드시트의 모든 시트 데이터를 가져옵니다.
    /// </summary>
    /// <param name="spreadsheetId">Google Spreadsheet ID</param>
    /// <returns>모든 시트의 데이터</returns>
    public async Task<IList<SheetData>> GetAllSheetsDataAsync(string spreadsheetId)
    {
        var sheetNames = await GetSheetNamesAsync(spreadsheetId);
        var result = new List<SheetData>();

        foreach (var sheetName in sheetNames)
        {
            var sheetData = await GetSheetDataAsync(spreadsheetId, sheetName);
            result.Add(sheetData);
        }

        return result;
    }

    public void Dispose()
    {
        _sheetsService?.Dispose();
    }
}

/// <summary>
/// 시트 데이터를 나타내는 클래스
/// </summary>
public class SheetData
{
    /// <summary>
    /// 시트 이름
    /// </summary>
    public string SheetName { get; }

    /// <summary>
    /// 시트의 행 데이터 (첫 번째 행은 헤더로 간주)
    /// </summary>
    public IList<IList<object>> Rows { get; }

    /// <summary>
    /// 헤더 행 (첫 번째 행)
    /// </summary>
    public IList<object> Headers => Rows.Count > 0 ? Rows[0] : new List<object>();

    /// <summary>
    /// 데이터 행 (헤더 제외)
    /// </summary>
    public IEnumerable<IList<object>> DataRows => Rows.Skip(1);

    public SheetData(string sheetName, IList<IList<object>> rows)
    {
        SheetName = sheetName ?? throw new ArgumentNullException(nameof(sheetName));
        Rows = rows ?? new List<IList<object>>();
    }
}
