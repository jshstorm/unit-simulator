using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;

namespace AvalonDevTool;

internal record AppSettings
{
    public record GitConfig
    {
        public bool InsteadRebase { get; init; } = true;

        public bool Autostash { get; init; } = true;
    }

    public string DataSheetVersion { get; init; } = "";
    public string RunPackage { get; init; } = "protocol";
    public string Environment { get; init; } = "Development";
    public string WorkDirectory { get; init; } = ".";
    public string TargetDB { get; init; } = "local";
    public string ProjectName { get; init; } = "hxh";
    public string Branch { get; init; } = "master";

    public GitConfig Git { get; init; } = new();

    private static string? _currentSettingsPath;
    private static AppSettings? _current;
    public static AppSettings Current => _current ?? throw TerminateException.Internal();

    public static async ValueTask ConfigureAsync(CancellationToken cancellationToken = default)
    {
        _current = null;

        if (_currentSettingsPath == null)
        {
            var assemblyDirectoryPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            string? filePath = null;
            if (assemblyDirectoryPath != null)
            {
                filePath = Path.Combine(assemblyDirectoryPath, "appsettings.json");
                if (File.Exists(filePath) == false)
                {
                    filePath = null;
                }
            }

            if (filePath == null)
            {
                var currentDirectoryPath = Directory.GetCurrentDirectory();
                filePath = Path.Combine(currentDirectoryPath, "appsettings.json");
                if (File.Exists(filePath) == false)
                {
                    AnsiConsole.MarkupLine("[red] appsettings.json: 파일이 {0} 디렉토리에 존재하지 않습니다.[/]", currentDirectoryPath);
                    throw TerminateException.User();
                }
            }

            _currentSettingsPath = filePath;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_currentSettingsPath, cancellationToken);
            var instance = new AppSettings();
            instance = FromJsonOverwrite(json, instance);
            AnsiConsole.MarkupLine("[b]{0}[/] 설정이 [green]로드[/]되었습니다.", _currentSettingsPath);

            var env = instance.Environment;
            var envJson = Path.ChangeExtension(_currentSettingsPath, "." + env + ".json");
            if (File.Exists(envJson))
            {
                var envJsonContent = await File.ReadAllTextAsync(envJson, cancellationToken);
                instance = FromJsonOverwrite(envJsonContent, instance);
                AnsiConsole.MarkupLine("[b]{0}[/] 설정이 [green]로드[/]되었습니다.", envJson);
            }

            if (string.IsNullOrEmpty(instance.WorkDirectory))
            {
                AnsiConsole.MarkupLine("[yellow] appsettings.json: WorkDirectory가 지정되지 않았습니다. 현재 디렉토리를 사용합니다.");
                instance = instance with
                {
                    WorkDirectory = "."
                };
            }
            else
            {
                var workingDirectory = Path.GetFullPath(instance.WorkDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                AnsiConsole.MarkupLine("작업 디렉토리가 [b]{0}[/]으로 설정되었습니다.", workingDirectory);
            }

            AnsiConsole.MarkupLine("[green][b]{0}[/][/] 프로젝트 설정 사용", instance.ProjectName);
            _current = instance;
        }
        catch (JsonException e)
        {
            AnsiConsole.MarkupLine("[red] appsettings.json: {0}[/]", e.Message);
            throw TerminateException.User();
        }
    }

    private static AppSettings FromJsonOverwrite(string json, AppSettings instance)
    {
        // 잘못된 JSON 형식이 오류를 유발하지 않고 기본값으로 재선택되도록 항상 예외 검사를 진행하세요.
        var jsonRootObject = JsonNode.Parse(json)?.AsObject();
        if (jsonRootObject == null)
        {
            return instance;
        }

        string? dataSheetVersion = jsonRootObject[nameof(DataSheetVersion)]?.ToString();
        string? runPackage = jsonRootObject[nameof(RunPackage)]?.ToString();
        string? workDirectory = jsonRootObject[nameof(WorkDirectory)]?.ToString();
        string? targetDB = jsonRootObject[nameof(TargetDB)]?.ToString();
        string? projectName = jsonRootObject[nameof(ProjectName)]?.ToString();
        string? branch = jsonRootObject[nameof(Branch)]?.ToString();
        GitConfig git = FromJsonOverwrite(json, instance.Git);
        return new AppSettings
        {
            DataSheetVersion = dataSheetVersion ?? instance.DataSheetVersion,
            RunPackage = runPackage ?? instance.RunPackage,
            WorkDirectory = workDirectory ?? instance.WorkDirectory,
            TargetDB = targetDB ?? instance.TargetDB,
            ProjectName = projectName ?? instance.ProjectName,
            Branch = branch ?? instance.Branch,
            Git = git
        };
    }

    private static GitConfig FromJsonOverwrite(string json, GitConfig instance)
    {
        var jsonRootObject = JsonNode.Parse(json)?.AsObject();
        if (jsonRootObject == null)
        {
            return instance;
        }
        bool? insteadRebase = jsonRootObject[nameof(GitConfig.InsteadRebase)]?.GetValue<bool>();
        bool? autostash = jsonRootObject[nameof(GitConfig.Autostash)]?.GetValue<bool>();
        return new GitConfig
        {
            InsteadRebase = insteadRebase ?? instance.InsteadRebase,
            Autostash = autostash ?? instance.Autostash
        };
    }

    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public static async ValueTask ModifyAsync(AppSettings newSettings, CancellationToken cancellationToken = default)
    {
        if (_currentSettingsPath == null)
        {
            AnsiConsole.MarkupLine("[red] appsettings.json: 설정이 로드되지 않았습니다.[/]");
            throw TerminateException.Internal();
        }

        var envJsonPath = Path.ChangeExtension(_currentSettingsPath, "." + newSettings.Environment + ".json");
        var json = JsonSerializer.Serialize(newSettings, options: DefaultJsonSerializerOptions);
        await File.WriteAllTextAsync(envJsonPath, json, cancellationToken);
        _current = newSettings;
    }
}
