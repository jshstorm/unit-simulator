using Spectre.Console;
using System.Text.Json;
using UnitSimulator.GoogleSheets;

namespace UnitDevTool;
internal static class Program
{
    private sealed class GoogleSheetsConfig
    {
        public string CredentialsPath { get; set; } = string.Empty;
        public List<SheetDocumentConfig> Documents { get; set; } = new();
    }

    private sealed class SheetDocumentConfig
    {
        public string Name { get; set; } = string.Empty;
        public string SpreadsheetId { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = "./DataSheets";
    }

    private sealed class AppConfig
    {
        public GoogleSheetsConfig GoogleSheets { get; set; } = new();
    }

    private static void Main(string[] args)
    {
        AnsiConsole.MarkupLine("[green]Unit Dev Tool[/] - 기본 TUI 스켈레톤");
        AnsiConsole.MarkupLine("[grey]데이터 시트 다운로드 기능부터 구현합니다.[/]\n");

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]메뉴를 선택하세요[/]")
                    .AddChoices("1. Data Sheet Download", "0. 종료"));

            switch (choice)
            {
                case "1. Data Sheet Download":
                    RunDataSheetDownload();
                    break;
                case "0. 종료":
                    return;
            }
        }
    }

    private static void RunDataSheetDownload()
    {
        try
        {
            var config = LoadConfig();
            if (config.GoogleSheets.Documents.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]다운로드할 Google 문서가 appsettings.json에 정의되어 있지 않습니다.[/]");
                return;
            }

            var doc = AnsiConsole.Prompt(
                new SelectionPrompt<SheetDocumentConfig>()
                    .Title("[yellow]다운로드할 Google Sheet 문서를 선택하세요[/]")
                    .UseConverter(d => string.IsNullOrWhiteSpace(d.Name) ? d.SpreadsheetId : d.Name)
                    .AddChoices(config.GoogleSheets.Documents));

            var outputDir = doc.OutputDirectory;
            var credentialsPath = Path.GetFullPath(config.GoogleSheets.CredentialsPath);

            AnsiConsole.Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(new Style(Color.Green))
                .Start("Google Sheets에서 데이터 시트를 다운로드 중...", ctx =>
                {
                    return DownloadSheetsAsync(doc.SpreadsheetId, outputDir, credentialsPath, status => ctx.Status = status);
                })
                .GetAwaiter().GetResult();

            AnsiConsole.MarkupLine("[green]데이터 시트 다운로드 및 XML 저장이 완료되었습니다.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]오류 발생:[/] {Markup.Escape(ex.Message)}");
        }
    }

    private static async Task DownloadSheetsAsync(string spreadsheetId, string outputDirectory, string credentialsPath, Action<string>? statusReporter)
    {
        statusReporter?.Invoke("크레덴셜 검증 중...");
        if (!File.Exists(credentialsPath))
        {
            throw new FileNotFoundException($"Credentials file not found: {credentialsPath}");
        }

        statusReporter?.Invoke("Google Sheets API에 연결 중...");
        using var service = new GoogleSheetsService(credentialsPath);

        statusReporter?.Invoke("시트 메타데이터 조회 중...");
        var spreadsheet = await service.GetSpreadsheetAsync(spreadsheetId);
        var title = spreadsheet.Properties?.Title ?? spreadsheetId;

        statusReporter?.Invoke($"'{title}' 문서의 시트 데이터 다운로드 중...");
        var sheets = await service.GetAllSheetsDataAsync(spreadsheetId);

        statusReporter?.Invoke("XML 파일로 저장 중...");
        var savedPaths = XmlConverter.SaveAllToXmlFiles(sheets, outputDirectory);

        statusReporter?.Invoke($"총 {savedPaths.Count}개의 시트를 XML로 저장했습니다.");
    }

    private static AppConfig LoadConfig()
    {
        const string configFile = "appsettings.json";
        if (!File.Exists(configFile))
        {
            throw new FileNotFoundException($"환경 설정 파일을 찾을 수 없습니다: {configFile}");
        }

        var json = File.ReadAllText(configFile);
        var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config == null)
        {
            throw new InvalidOperationException("환경 설정을 로드할 수 없습니다.");
        }

        return config;
    }
}
