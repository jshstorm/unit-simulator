using System.Linq;
using Spectre.Console;

namespace UnitSimulator.GoogleSheets;

/// <summary>
/// Google Sheets to XML 변환 CLI 인터페이스
/// </summary>
public static class SheetToXmlCli
{
    private const string DefaultOutputDir = "xml_output";
    private const string DefaultCredentialsFile = "credentials.json";

    /// <summary>
    /// CLI 명령을 실행합니다.
    /// </summary>
    /// <param name="args">명령줄 인수</param>
    /// <returns>종료 코드 (0: 성공, 1: 실패)</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            return await RunInteractiveAsync();
        }

        string? spreadsheetId = null;
        string? outputDirectory = null;
        string? credentialsPath = null;

        // 인수 파싱
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--sheet-id" or "-s":
                    if (i + 1 < args.Length)
                        spreadsheetId = args[++i];
                    break;
                case "--output" or "-o":
                    if (i + 1 < args.Length)
                        outputDirectory = args[++i];
                    break;
                case "--credentials" or "-c":
                    if (i + 1 < args.Length)
                        credentialsPath = args[++i];
                    break;
            }
        }

        // 필수 인수 확인
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            Console.Error.WriteLine("Error: Spreadsheet ID is required. Use --sheet-id or -s option.");
            PrintUsage();
            return 1;
        }

        // 기본값 설정
        outputDirectory ??= Path.Combine(Directory.GetCurrentDirectory(), DefaultOutputDir);
        credentialsPath ??= ResolveDefaultCredentialsPath();

        try
        {
            await ConvertSheetToXmlAsync(spreadsheetId, outputDirectory, credentialsPath, useTui: false);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunInteractiveAsync()
    {
        AnsiConsole.Write(new FigletText("Sheets -> XML").Color(Color.Green).LeftJustified());
        AnsiConsole.MarkupLine("[grey]Interactive mode. Press CTRL+C to cancel.[/]");
        AnsiConsole.WriteLine();

        var spreadsheetId = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the [green]Spreadsheet ID[/]:")
                .PromptStyle("bold")
                .Validate(id => string.IsNullOrWhiteSpace(id)
                    ? ValidationResult.Error("Spreadsheet ID is required.")
                    : ValidationResult.Success()))
            .Trim();

        var defaultOutputDir = Path.Combine(Directory.GetCurrentDirectory(), DefaultOutputDir);
        var outputDirectory = AnsiConsole.Prompt(
                new TextPrompt<string>($"Output directory [grey](default: {Markup.Escape(defaultOutputDir)})[/]")
                    .AllowEmpty()
                    .DefaultValue(defaultOutputDir))
            .Trim();
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = defaultOutputDir;
        }

        var defaultCredentials = ResolveDefaultCredentialsPath();
        var credentialsPath = AnsiConsole.Prompt(
                new TextPrompt<string>($"Credentials path [grey](default: {Markup.Escape(defaultCredentials)})[/]")
                    .AllowEmpty()
                    .DefaultValue(defaultCredentials))
            .Trim();
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            credentialsPath = defaultCredentials;
        }

        if (!File.Exists(credentialsPath))
        {
            AnsiConsole.MarkupLine($"[red]Credentials file not found:[/] {Markup.Escape(credentialsPath)}");
            return 1;
        }

        if (!AnsiConsole.Confirm("Proceed with the conversion using the settings above?", true))
        {
            AnsiConsole.MarkupLine("[yellow]Conversion cancelled.[/]");
            return 0;
        }

        try
        {
            await ConvertSheetToXmlAsync(spreadsheetId, outputDirectory, credentialsPath, useTui: true);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    /// <summary>
    /// Google Sheets를 XML로 변환하고 저장합니다.
    /// </summary>
    private static async Task ConvertSheetToXmlAsync(string spreadsheetId, string outputDirectory, string credentialsPath, bool useTui)
    {
        if (useTui)
        {
            var summary = await RunWithTuiAsync(spreadsheetId, outputDirectory, credentialsPath);
            RenderSummary(summary);
        }
        else
        {
            var summary = await PerformConversionAsync(spreadsheetId, outputDirectory, credentialsPath, status => Console.WriteLine(status));
            WriteHeadlessSummary(summary);
        }
    }

    private static async Task<SheetExportSummary> RunWithTuiAsync(string spreadsheetId, string outputDirectory, string credentialsPath)
    {
        SheetExportSummary? summary = null;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.BouncingBar)
            .SpinnerStyle(new Style(Color.Green))
            .StartAsync("Preparing conversion...", async ctx =>
            {
                summary = await PerformConversionAsync(spreadsheetId, outputDirectory, credentialsPath, status => ctx.Status = status);
            });

        return summary ?? throw new InvalidOperationException("Conversion summary was not generated.");
    }

    private static async Task<SheetExportSummary> PerformConversionAsync(string spreadsheetId, string outputDirectory, string credentialsPath, Action<string>? statusReporter)
    {
        statusReporter?.Invoke("Validating credentials");
        if (!File.Exists(credentialsPath))
        {
            throw new FileNotFoundException($"Credentials file not found: {credentialsPath}\n" +
                "Please provide a valid service account JSON file or set GOOGLE_APPLICATION_CREDENTIALS environment variable.");
        }

        statusReporter?.Invoke("Connecting to Google Sheets API");
        using var sheetsService = new GoogleSheetsService(credentialsPath);

        statusReporter?.Invoke("Fetching spreadsheet metadata");
        var spreadsheet = await sheetsService.GetSpreadsheetAsync(spreadsheetId);

        statusReporter?.Invoke("Downloading sheet data");
        var allSheetsData = (await sheetsService.GetAllSheetsDataAsync(spreadsheetId)).ToList();

        statusReporter?.Invoke("Writing XML files");
        var savedPaths = XmlConverter.SaveAllToXmlFiles(allSheetsData, outputDirectory).ToList();

        return new SheetExportSummary(spreadsheet, allSheetsData, savedPaths, Path.GetFullPath(outputDirectory));
    }

    private static void RenderSummary(SheetExportSummary summary)
    {
        var title = summary.Spreadsheet.Properties?.Title ?? "Spreadsheet";
        var rule = new Rule($"[bold]{Markup.Escape(title)}[/]")
        {
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine($"[green]Sheets processed:[/] {summary.Sheets.Count}");
        AnsiConsole.MarkupLine($"[green]Output directory:[/] {Markup.Escape(summary.OutputDirectory)}");

        if (summary.Sheets.Count > 0)
        {
            var table = new Table()
                .RoundedBorder()
                .AddColumn("Sheet")
                .AddColumn("XML Path");

            foreach (var (sheet, path) in summary.Sheets.Zip(summary.Paths, (sheet, path) => (sheet, path)))
            {
                table.AddRow(Markup.Escape(sheet.SheetName), Markup.Escape(path));
            }

            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No data rows were returned from the spreadsheet.[/]");
        }

        AnsiConsole.MarkupLine("[bold green]Conversion complete![/]");
    }

    private static void WriteHeadlessSummary(SheetExportSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine("=== Conversion Complete ===");
        Console.WriteLine($"Spreadsheet Title: {summary.Spreadsheet.Properties?.Title}");
        Console.WriteLine($"Total Sheets: {summary.Sheets.Count}");
        Console.WriteLine($"Output Directory: {summary.OutputDirectory}");
        foreach (var (sheet, path) in summary.Sheets.Zip(summary.Paths, (sheet, path) => (sheet, path)))
        {
            Console.WriteLine($"  - {sheet.SheetName}: {path}");
        }
    }

    private static string ResolveDefaultCredentialsPath()
    {
        var envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), DefaultCredentialsFile);
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"
Google Sheets to XML Converter
==============================

Usage:
  UnitMove sheet-to-xml --sheet-id <SPREADSHEET_ID> [options]
  UnitMove sheet-to-xml                Launch interactive TUI mode

Required:
  --sheet-id, -s <ID>       Google Spreadsheet ID (found in the URL)

Options:
  --output, -o <DIR>        Output directory for XML files (default: ./xml_output)
  --credentials, -c <PATH>  Path to Google service account credentials JSON file
                            (default: GOOGLE_APPLICATION_CREDENTIALS env var or ./credentials.json)
  --help, -h                Show this help message

Examples:
  UnitMove sheet-to-xml -s 1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms -o ./output
  UnitMove sheet-to-xml --sheet-id 1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms --credentials ./my-credentials.json

Environment Variables:
  GOOGLE_APPLICATION_CREDENTIALS  Path to the service account credentials file

Note:
  To use this tool, you need:
  1. A Google Cloud project with Sheets API enabled
  2. A service account with access to the spreadsheet
  3. The service account's JSON key file
");
    }

    private sealed record SheetExportSummary(
        Google.Apis.Sheets.v4.Data.Spreadsheet Spreadsheet,
        IReadOnlyList<SheetData> Sheets,
        IReadOnlyList<string> Paths,
        string OutputDirectory);
}
