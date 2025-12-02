using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AvalonDevTool.Externals;
using AvalonDevTool.GoogleSheets;
using AvalonDevTool.SCM;
using AvalonDevTool.Utility;
using MySql.Data.MySqlClient;
using Spectre.Console;
using StackExchange.Redis;

namespace AvalonDevTool.Commands;

internal class MainMenuCommands : CommandGroup
{
    private const string RunPackage_full = "full";
    private const string RunPackage_runserver = "runserver";
    private const string RunPackage_protocol = "protocol";

    public override string Title => "메인 메뉴";

    private readonly CommandFilter _filter;
    private readonly List<GitWorkspace> _repositories = [];

    public MainMenuCommands()
    {
        _filter = AppSettings.Current.RunPackage switch
        {
            RunPackage_full => CommandFilter.Any,
            RunPackage_runserver => CommandFilter.Service,
            RunPackage_protocol => CommandFilter.Protobuf,
            _ => CommandFilter.Any
        };

        _repositories.AddRange(
        [
            new GitWorkspace("./avalon_builds", "git@gitlab.clover.games:avalon/avalon_builds.git"),
            new GitWorkspace("./AvalonClient", "git@gitlab.clover.games:avalon/AvalonClient.git"),
            new GitWorkspace("./AvalonShared", "git@gitlab.clover.games:avalon/AvalonShared.git"),
        ]);

        if (AppSettings.Current.RunPackage != RunPackage_protocol)
        {
            _repositories.Add(new GitWorkspace("./AvalonServer", "git@gitlab.clover.games:avalon/AvalonServer.git"));
        }
    }

    [Command(CommandFilter.Any)]
    public ValueTask Trap(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    [Command(CommandFilter.Any, Interrupt = true)]
    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        //git repositories
        {
            foreach (var repo in _repositories)
            {
                if (repo.Exists)
                {
                    var current = await repo.QueryCurrentBranchAsync(cancellationToken);
                    if (current.HasValue && current.Value.Tracking.HasValue)
                    {
                        AnsiConsole.MarkupLine($"[green]{repo.WorkingDirectory}가 이미 존재합니다. 업데이트를 시작합니다.[/]");
                        await repo.PullAsync(AppSettings.Current.MakePullOptions(), cancellationToken).Report();
                    }

                    continue;
                }

                AnsiConsole.MarkupLine($"[green]{repo.WorkingDirectory}가 존재하지 않습니다. 클론을 시작합니다.[/]");
                await repo.CloneAsync(cancellationToken).Report();
            }

            const string destinationDirectory = "./mysql-init";
            if (!Directory.Exists(destinationDirectory)) {
                Directory.CreateDirectory(destinationDirectory);
            }
            Program.CopyFiles("./avalon_builds/compose/dev-compose/", destinationDirectory, "*.sql");
        }
        
        await AnsiConsole.Progress()
            .Columns([
                new SpinnerColumn(),
                new TaskDescriptionColumn() { Alignment = Justify.Left },
                new ElapsedTimeColumn()
            ])
            .StartAsync(async ctx =>
            {
                var stopTask = ctx.AddTask("Stopping existing containers (if any)");

                await Docker.Compose.DownAsync("--remove-orphans", cancellationToken);

                stopTask.StopTask();

                var upTask = ctx.AddTask("docker-compose up --build -d avalon-service-dev cache db" );
                await Docker.Compose.UpAsync("--build -d avalon-service-dev cache db ", cancellationToken);
                upTask.StopTask();
            });

    }

    [Command(CommandFilter.Any, Interrupt = true)]
    public async ValueTask UpdateCodeAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[b]전체 저장소[/]를 업데이트합니다.");

        await AnsiConsole.Progress()
            .Columns([new SpinnerColumn(), new TaskDescriptionColumn() { Alignment = Justify.Left }, new ElapsedTimeColumn()])
            .StartAsync(async ctx =>
            {
                List<Task> tasks = [];
                foreach (var repo in _repositories)
                {
                    var ga = AppSettings.Current.MakePullOptions();
                    var currentBranch = await repo.QueryCurrentBranchAsync(cancellationToken);
                    if (currentBranch.HasValue == false)
                    {
                        // HEAD가 특정 branch에서 detach됨.
                        continue;
                    }

                    if (currentBranch.Value.Tracking.HasValue == false)
                    {
                        // branch가 remote tracking을 갖지 않음.
                        continue;
                    }

                    var prog = ctx.AddTask($"pulling:\t{repo.Name}\t{currentBranch.Value.Name}\t<-- {currentBranch.Value.Tracking.Value.RemoteName}\t");
                    tasks.Add(repo.PullAsync(ga, cancellationToken).ContinueWith(r =>
                    {
                        prog.StopTask();
                        return r.Result;
                    }));
                }

                await Task.WhenAll(tasks);
            });
    }

    [Command(CommandFilter.Any)]
    public async ValueTask PullServerRepositoryAsync(CancellationToken cancellationToken)
    {
        var selected = await SelectBranchAsync(2, cancellationToken);
        selected = selected.StartsWith("origin/") ? selected.Substring("origin/".Length) : selected;
        
        AnsiConsole.MarkupLine($"[green]branch {selected}로 전환합니다.[/]");
        var current = AppSettings.Current;
        await AppSettings.ModifyAsync(current with
        {
            Branch = selected
        }, cancellationToken);
        AnsiConsole.MarkupLine($"[green]branch {selected}를 환경 설정에 저장했습니다.[/]");
        
        AnsiConsole.MarkupLine($"[green]배포 저장소를 동기화합니다.[/]");
        await Program.PullDeployRepositoryAsync(cancellationToken);

        if (Program.ApplyDeployedReferences(cancellationToken) == false)
        {
            AnsiConsole.MarkupLine($"[red]레퍼런스 파일 배포에 실패했습니다.[/]");
            return;
        }
        AnsiConsole.MarkupLine($"[green]레퍼런스 배포했습니다.[/]");
    }

    [Command(CommandFilter.Service, CommandFilter.Protobuf, Interrupt = true)]
    public async ValueTask BuildProtocolAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]프로토콜 빌드를 시작합니다.[/]");
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Arrow3)
            .SpinnerStyle(new Style(foreground: Color.Green))
            .StartAsync("docker-compose exec avalon_builds proto-build.sh", async ctx =>
            {
				AnsiConsole.MarkupLine($"[white]소스 파일을 가져옵니다.[/]");
				Program.ClearDirectory("./avalon_builds/protoTool/proto");
                Program.CopyFiles("./AvalonShared/Protocol/Proto", "./avalon_builds/protoTool/proto/", "*.proto");
                Program.CopyFiles("./AvalonShared/Protocol/Proto/grpc", "./avalon_builds/protoTool/proto/grpc", "*.proto");

				AnsiConsole.MarkupLine($"[white]프로토 파일을 빌드합니다.[/]");
                await Docker.Compose.RunAsync($"proto-tool-worker /bin/bash -c {"cd /avalon_builds/protoTool && ./proto-build.sh".TerminalQuote()}", cancellationToken: cancellationToken);

                AnsiConsole.MarkupLine($"[white]프로토콜 파일들을 복사합니다.[/]");
                Program.CopyFiles("./avalon_builds/protoTool/out/client", "./AvalonClient/Assets/Game/Script/Protocol/", "*.cs");
			});
    }
    
    public class ReferenceFileList
    {
        [JsonPropertyOrder(0), JsonPropertyName("branch")]
        public string? Branch { get; set; }

        [JsonPropertyOrder(1), JsonPropertyName("branch_hash")]
        public string? BranchHash { get; set; }

        [JsonPropertyOrder(2), JsonPropertyName("gamedata")]
        public string? GameData { get; set; }
        
        [JsonPropertyOrder(2), JsonPropertyName("bundle")]
        public string? Bundle { get; set; }

        [JsonPropertyOrder(3), JsonPropertyName("gamedata_list")]
        public List<string>? GameDataList { get; set; }

        public static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
        public static readonly Encoding Encoding = new UTF8Encoding(false);
    }

    [Command(CommandFilter.Service, CommandFilter.Protobuf, Interrupt = true)]
    public async ValueTask BuildDataSheetAsync(CancellationToken cancellationToken)
    {
        await Program.GetGSheetToken(cancellationToken);

        Program.ClearPreviousReferences();
        Program.ClearDataSheet();

        await BuildProtocolAsync(cancellationToken);

        AnsiConsole.MarkupLine("[green]데이터 시트 빌드를 시작합니다.[/]");

        await AppSettings.ConfigureAsync(cancellationToken);
        var versionString = AppSettings.Current.DataSheetVersion;
        if (!string.IsNullOrEmpty(versionString))
        {
            AnsiConsole.MarkupLine($"[white]대상 문서 버전은 {versionString} 입니다.[/]");
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Arrow3)
            .SpinnerStyle(new Style(foreground: Color.Green))
            .StartAsync("XML 직렬화로 데이터 시트 빌드 중...", async ctx =>
            {
                // XML 직렬화를 사용하여 데이터 시트 빌드
                var outputDirectory = "./avalon_builds/protoTool/out";
                var credentialsPath = "./avalon_builds/protoTool/api_key/gcp/service_account.json";
                var configPath = "./avalon_builds/protoTool/datasheet_config.json";

                ctx.Status = "Google Sheets 데이터를 XML로 변환 중...";
                
                var builder = new DataSheetBuilder(credentialsPath, outputDirectory);
                var result = await builder.BuildFromConfigAsync(configPath, versionString, cancellationToken);

                if (!result.Success)
                {
                    AnsiConsole.MarkupLine($"[red]데이터 시트 빌드 실패: {result.ErrorMessage}[/]");
                    return;
                }

                var fileListPath = "./avalon_builds/protoTool/out/filelist.json";
                var shared = _repositories.FirstOrDefault(r => r.Name == "AvalonShared");
                if (shared == null) {
                    AnsiConsole.MarkupLine("[red]AvalonShared 저장소를 찾을 수 없습니다.[/]");
                }

                if (File.Exists(fileListPath) && shared != null)
                {
                    var fileList = await File.ReadAllTextAsync(fileListPath, ReferenceFileList.Encoding, cancellationToken);
                    var content = JsonSerializer.Deserialize<ReferenceFileList>(fileList, ReferenceFileList.SerializerOptions);
                    if (content != null)
                    {
                        var branchInfo = await shared.GetBranchInfo(cancellationToken);
                        content.Branch = branchInfo.HasValue ? branchInfo.Value.Name : "unknown";
                        content.BranchHash = branchInfo.HasValue ? branchInfo.Value.Hash : "unknown";
                        var json = JsonSerializer.Serialize(content, ReferenceFileList.SerializerOptions);
                        await File.WriteAllTextAsync(fileListPath, json, ReferenceFileList.Encoding, cancellationToken);
                    }
                }

                ctx.Status = "Copy to .NET";
                DataSyncOutputHelper.DeployToServer();
            
                ctx.Status = "Copy bytes to Client";
                DataSyncOutputHelper.DeployToClient();
            });

        await BuildTextDataSheetAsync(cancellationToken);
    }

    [Command(CommandFilter.Service, Interrupt = true)]
    public async ValueTask RunServerAsync(CancellationToken cancellationToken)
    {
        await DownServerAsync(cancellationToken);

        AnsiConsole.MarkupLine("[green]서버 저장소를 다운로드 받습니다.[/]");
        await Program.PullDeployRepositoryAsync(cancellationToken);

        AnsiConsole.MarkupLine("[green]서버 실행을 시작합니다..[/]");
        await AnsiConsole.Progress()
            .Columns([new SpinnerColumn(), new TaskDescriptionColumn() { Alignment = Justify.Left }, new ElapsedTimeColumn()])
            .StartAsync(async ctx =>
            {
                var task1 = ctx.AddTask("docker-compose up avalon-service");
                await Docker.Compose.UpAsync("-d avalon-service", cancellationToken);
                task1.StopTask();
              
            });
    }

    //[Command(CommandFilter.Service, CommandFilter.Protobuf, Interrupt = true)]
    public async ValueTask BuildTextDataSheetAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]텍스트 데이터 시트 빌드를 시작합니다.[/]");

        var versionString = AppSettings.Current.DataSheetVersion;
        if (!string.IsNullOrEmpty(versionString))
        {
            AnsiConsole.MarkupLine($"[white]대상 문서 버전은 {versionString} 입니다.[/]");
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Arrow3)
            .SpinnerStyle(new Style(foreground: Color.Green))
            .StartAsync("XML 직렬화로 텍스트 데이터 시트 빌드 중...", async ctx =>
            {
                // XML 직렬화를 사용하여 텍스트 데이터 시트 빌드
                var outputDirectory = "./avalon_builds/protoTool/out";
                var credentialsPath = "./avalon_builds/protoTool/api_key/gcp/service_account.json";
                var configPath = "./avalon_builds/protoTool/datasheet_config.json";

                ctx.Status = "Google Sheets 텍스트 데이터를 XML로 변환 중...";

                // 텍스트 설정 파일에서 스프레드시트 ID를 읽어 빌드
                if (File.Exists(configPath))
                {
                    var configJson = await File.ReadAllTextAsync(configPath, cancellationToken);
                    var config = JsonSerializer.Deserialize<DataSheetConfig>(configJson);
                    
                    if (config?.TextSpreadsheetIds != null && config.TextSpreadsheetIds.Count > 0)
                    {
                        var builder = new DataSheetBuilder(credentialsPath, outputDirectory);
                        var result = await builder.BuildAsync(config.TextSpreadsheetIds, versionString, cancellationToken);

                        if (!result.Success)
                        {
                            AnsiConsole.MarkupLine($"[red]텍스트 데이터 시트 빌드 실패: {result.ErrorMessage}[/]");
                            return;
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]텍스트 스프레드시트 ID가 설정되지 않았습니다.[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]설정 파일이 없습니다: {configPath}[/]");
                }
                
                ctx.Status = "Copy bytes to Client";
                DataSyncOutputHelper.CopyTextBytes();
                
                ctx.Status = "Copy bytes to Server";
                DataSyncOutputHelper.DeployToServer();
                
            });
    }

    [Command(CommandFilter.Service, CommandFilter.Protobuf, Interrupt = true)]
    public async ValueTask BuildDataFromJson(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[white]JSON으로부터 XML reference data를 빌드합니다.[/]");

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Arrow3)
            .SpinnerStyle(new Style(foreground: Color.Green))
            .StartAsync("JSON에서 XML로 변환 중...", async ctx =>
            {
                var sourceDir = "./avalon_builds/protoTool/docs_data";
                var outputDir = "./avalon_builds/protoTool/out";

                if (!Directory.Exists(sourceDir))
                {
                    AnsiConsole.MarkupLine($"[red]소스 디렉토리가 존재하지 않습니다: {sourceDir}[/]");
                    return;
                }

                Directory.CreateDirectory(outputDir);

                var jsonFiles = Directory.GetFiles(sourceDir, "*.json", SearchOption.AllDirectories);
                var processedCount = 0;

                foreach (var jsonFile in jsonFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        ctx.Status = $"변환 중: {Path.GetFileName(jsonFile)}";
                        
                        var jsonContent = await File.ReadAllTextAsync(jsonFile, cancellationToken);
                        var sheetName = Path.GetFileNameWithoutExtension(jsonFile);
                        
                        // JSON을 파싱하여 SheetData 형식으로 변환
                        var sheetData = ConvertJsonToSheetData(jsonContent, sheetName);
                        if (sheetData != null)
                        {
                            // XML 파일로 저장
                            XmlConverter.SaveToXmlFile(sheetData, outputDir);
                            // Bytes 파일로 저장
                            XmlConverter.SaveToBytesFile(sheetData, outputDir);
                            processedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]파일 변환 실패 ({Path.GetFileName(jsonFile)}): {ex.Message}[/]");
                    }
                }

                AnsiConsole.MarkupLine($"[green]{processedCount}개 파일 변환 완료[/]");
            });
    }

    private static SheetData? ConvertJsonToSheetData(string jsonContent, string sheetName)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var rows = new List<IList<object>>();
            List<object>? headers = null;

            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var row = new List<object>();

                foreach (var prop in item.EnumerateObject())
                {
                    if (headers == null)
                    {
                        headers = new List<object>();
                    }
                    
                    // Only add headers on the first row
                    if (rows.Count == 0)
                    {
                        headers.Add(prop.Name);
                    }

                    var value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        JsonValueKind.Number => prop.Value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => "",
                        _ => prop.Value.GetRawText()
                    };
                    row.Add(value);
                }

                // Add headers as first row only once
                if (rows.Count == 0 && headers != null)
                {
                    rows.Add(headers);
                }
                rows.Add(row);
            }


            return rows.Count > 0 ? new SheetData(sheetName, rows) : null;
        }
        catch
        {
            return null;
        }
    }
    
    [Command(CommandFilter.Service, Interrupt = true)]
    public async ValueTask DownServerAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[green]서버 종료를 시작합니다.[/]");
        await AnsiConsole.Progress()
            .Columns([new SpinnerColumn(), new TaskDescriptionColumn() { Alignment = Justify.Left }, new ElapsedTimeColumn()])
            .StartAsync(async ctx =>
            {
                var task2 = ctx.AddTask("docker-compose stop avalon-service");
                await Docker.Compose.StopAsync("avalon-service", cancellationToken);
                
                task2.StopTask();
            });
    }

    //[Command(CommandFilter.Service, Interrupt = true)]
    public async ValueTask BuildShared(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[green]AvalonShared 빌드를 시작합니다.[/]");
        await AnsiConsole.Progress()
            .Columns([
                new SpinnerColumn(), new TaskDescriptionColumn() { Alignment = Justify.Left }, new ElapsedTimeColumn()
            ])
            .StartAsync(async ctx =>
            {
                var task1 = ctx.AddTask("docker-compose up -d avalon-service-dev");
                var task2 = ctx.AddTask("docker-compose exec avalon-service-dev dotnet build");

                await Docker.Compose.UpAsync("-d avalon-service-dev", cancellationToken);
                task1.StopTask();

                //dotnet build and run
                await Docker.Compose.ExecAsync(
                    $"avalon-service-dev /bin/bash -c {"cd /AvalonShared/Protocol && dotnet build --no-incremental".TerminalQuote()}",
                    cancellationToken: cancellationToken);
                task2.StopTask();

                Program.CopyFiles("./AvalonShared/Protocol/obj/Debug/net9.0/", "./AvalonClient/Assets/Game/Script/Protocol/", "*.cs");
                Program.CopyFiles("./AvalonShared/Protocol/bin/Debug/net9.0/grpc/", "./AvalonClient/Assets/Game/Script/Protocol/grpc/", "*.cs");
            });
    }

    [Command(CommandFilter.Service, Interrupt = true)]
    public async ValueTask DiscardServerAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[green]서버 폐기를 시작합니다.[/]");
        // 사용자에게 확인을 요청
        char input = await new TextPrompt<char>("정말로 서버를 폐기하시겠습니까?")
            .AddChoices(['y', 'n'])
            .ShowAsync(AnsiConsole.Console, cancellationToken);

        // 'y'를 입력한 경우에만 실행
        if (input == 'y')
        {
            await AnsiConsole.Status()
                .StartAsync("docker-compose down", async ctx =>
                {
                    await Docker.Compose.DownAsync("-v", cancellationToken: cancellationToken);
                });
            AnsiConsole.MarkupLine("[green]서버가 성공적으로 폐기되었습니다.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]서버 폐기가 취소되었습니다.[/]");
        }
    }

    [Command(CommandFilter.Service, Interrupt = true)]
    public async ValueTask DiscardLocalChangesAsync(CancellationToken cancellationToken = default)
    {
        char input = await new TextPrompt<char>("로컬 변경 사항을 폐기하시겠습니까?")
            .AddChoices(['y', 'n'])
            .ShowAsync(AnsiConsole.Console, cancellationToken);

        if (input != 'y') {
            return;
        }

        AnsiConsole.MarkupLine("[yellow]로컬 변경 사항 폐기를 시작합니다.[/]");
        string[] exceptions = ["avalon_builds"];
        foreach (var repo in _repositories)
        {
            if (exceptions.Contains(repo.Name)) { continue; }

            AnsiConsole.MarkupLine($"{repo.Name} 로컬 변경 사항을 폐기합니다.");
            await repo.ResetAsync("./", new GitWorkspace.ResetOptions
            {
                Mode = GitWorkspace.ResetMode.Hard
            }, cancellationToken);
        }
    }

    [Command(CommandFilter.Any)]
    public ValueTask ConfigAsync(CancellationToken cancellationToken)
    {
        Push<ConfigCommands>();
        return ValueTask.CompletedTask;
    }
    
    [Command(CommandFilter.Any)]
    private async Task<string> SelectBranchAsync(int Idx, CancellationToken cancellationToken)
    {
        var branch = await _repositories[Idx].QueryBranchesAsync(
            new GitWorkspace.QueryBranchesOptions { Remote = true }, cancellationToken);

        var choices = branch.Select((b, i) => $"{i + 1}. {b.Name}").ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("브랜치를 선택하세요:")
                .PageSize(10)
                .AddChoices(choices)
        );

        var selectedIndex = int.Parse(selected.Split('.')[0]) - 1;
        return branch[selectedIndex].Name;
    }
    
    [Command(CommandFilter.Service, Interrupt = true)]
    public async ValueTask CreateMigrationAsync(CancellationToken cancellationToken)
    {
        var migrationName = await new TextPrompt<string>("마이그레이션 이름을 입력하세요:")
            .PromptStyle("green")
            .ShowAsync(AnsiConsole.Console, cancellationToken);

        if (string.IsNullOrWhiteSpace(migrationName))
        {
            AnsiConsole.MarkupLine("[red]마이그레이션 이름은 비워둘 수 없습니다.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]{migrationName} 마이그레이션 생성을 시작합니다.[/]");
        
        // 도커에서 실행하지 않고 호스트 로컬에서 실행하도록 변경
        var projectPath = "./AvalonServer/AvalonService/AvalonService.csproj";

        // 컨텍스트 목록: (컨텍스트 풀 네임, 출력 폴더 이름)
        var contexts = new (string ContextFullName, string OutputFolder)[]
        {
            ("AvalonService.Infrastructure.Data.AppDbContext", "Game"),
            ("AvalonService.Infrastructure.Data.AppLogDbContext", "Log"),
            ("DatabaseConnections.DbContexts.CommonDbContext", "Common")
        };

        var rebuildChoice = await new TextPrompt<char>("기존 [yellow]Migrations[/] 디렉터리를 삭제하고 재생성하시겠습니까? (y/N)")
            .PromptStyle("green")
            .AddChoices(['y', 'n'])
            .DefaultValue('n')
            .ShowAsync(AnsiConsole.Console, cancellationToken);

        var shouldRebuildMigrations = char.ToLowerInvariant(rebuildChoice) == 'y';
        if (shouldRebuildMigrations)
        {
            MigrationHelper.RemoveExistingMigrations(contexts);
            AnsiConsole.MarkupLine("[yellow]기존 마이그레이션 디렉터리를 제거했습니다. 새로운 마이그레이션을 생성합니다.[/]");
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Arrow3)
            .SpinnerStyle(new Style(foreground: Color.Green))
            .StartAsync("dotnet ef migrations add (multiple contexts)", async ctx =>
            {
                foreach (var (contextFullName, folder) in contexts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var migrationOutputPath = Path.Combine("Migrations", folder);
                    var addCommand = $"dotnet ef migrations add {migrationName} --project {projectPath} --startup-project {projectPath} --context {contextFullName} --configuration Debug --output-dir {migrationOutputPath}";
                    ctx.Status = $"Creating migration for {contextFullName}";

                    // 호스트 로컬에서 dotnet ef 실행
                    await Terminal.ExecuteCommandAsync(addCommand, cancellationToken: cancellationToken);
                }
            });

        MigrationHelper.CleanupForeignKeyStatements(contexts, migrationName, projectPath);

        AnsiConsole.MarkupLine($"[green]마이그레이션 생성이 완료되었습니다.[/]");
        AnsiConsole.MarkupLine($"[green]SQL 스크립트 생성을 시작합니다.[/]");

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Arrow3)
            .SpinnerStyle(new Style(foreground: Color.Green))
            .StartAsync("dotnet ef migrations script (multiple contexts)", async ctx =>
            {
                foreach (var (contextFullName, folder) in contexts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var sqlOutputPath = Path.Combine("Migrations", folder, $"{migrationName}.sql");
                    var scriptCommand = $"dotnet ef migrations script --project {projectPath} --startup-project {projectPath} --context {contextFullName} --output {sqlOutputPath} --idempotent";
                    ctx.Status = $"Generating SQL script for {contextFullName}";

                    // 호스트 로컬에서 스크립트 생성 실행
                    await Terminal.ExecuteCommandAsync(scriptCommand, cancellationToken: cancellationToken);
                    AnsiConsole.MarkupLine($"[green]{contextFullName} SQL 생성 완료: {sqlOutputPath}[/]");
                }
            });

        AnsiConsole.MarkupLine("[green]모든 마이그레이션 및 SQL 스크립트 생성이 완료되었습니다.[/]");
    }

    [Command(CommandFilter.Service, Interrupt = true)]
    public async ValueTask ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]DB초기화를 시작합니다.[/]");
        await AnsiConsole.Progress()
            .Columns([new SpinnerColumn(), new TaskDescriptionColumn() { Alignment = Justify.Left }, new ElapsedTimeColumn()])
            .StartAsync(async ctx =>
            {
                string dbConnectionString = "server=localhost;port=3306;user=root;password=avalon123; SSL Mode=None; CharSet=utf8mb4; AllowPublicKeyRetrieval=True;";
                using var connection = new MySqlConnection(dbConnectionString);
                await connection.OpenAsync();

                Console.WriteLine("✅ MySQL에 연결되었습니다.");
                Console.WriteLine("📂 데이터베이스 목록:");

                string query = "SHOW DATABASES;";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"- {reader.GetString(0)}");
                    MigrationHelper.ResetDatabase(dbConnectionString, reader.GetString(0));
                }
                
                // 🔧 Redis 연결 문자열 (환경에 맞게 수정)
                var options = ConfigurationOptions.Parse("localhost:6379");
                options.AllowAdmin = true; // 관리자 명령 허용

                // 비밀번호가 있다면 예: "localhost:6379,password=yourpassword"
                // Redis 연결
                var redis = await ConnectionMultiplexer.ConnectAsync(options);
                Console.WriteLine("🔄 Redis 모든 DB 비우는 중...");
                // 연결된 모든 엔드포인트(서버 노드)에 대해 실행
                foreach (var endPoint in redis.GetEndPoints())
                {
                    var server = redis.GetServer(endPoint);
                    // 실제 Redis 서버에 FlushAll 실행
                    await server.FlushAllDatabasesAsync();
                    Console.WriteLine($"✅ 서버 [{endPoint}] 의 모든 DB 삭제 완료");
                }
                Console.WriteLine("🎉 모든 Redis DB가 완전히 삭제되었습니다!");
                Console.WriteLine("모든 DB가 성공적으로 삭제되었습니다!");

                
                //Program.ClearDirectory("./AvalonServer/AvalonService/Migrations/Game");
                //var process = new Process();
                // process.StartInfo.FileName = "dotnet";
                // process.StartInfo.Arguments = "ef migrations add --project ./AvalonServer/AvalonService/AvalonService.csproj --startup-project ./AvalonServer/AvalonService/AvalonService.csproj --context AvalonService.Infrastructure.Data.AppDbContext --configuration Debug TempInit --output-dir Migrations/Game";
                //
                // process.StartInfo.UseShellExecute = false;
                // process.StartInfo.RedirectStandardOutput = true;
                // process.StartInfo.RedirectStandardError = true;
                // process.StartInfo.CreateNoWindow = true;
                //
                // process.OutputDataReceived += (sender, e) => {
                //     if (!string.IsNullOrEmpty(e.Data))
                //         Console.WriteLine("[출력] " + e.Data);
                // };
                // process.ErrorDataReceived += (sender, e) => {
                //     if (!string.IsNullOrEmpty(e.Data))
                //         Console.WriteLine("[오류] " + e.Data);
                // };
                //
                // process.Start();
                // process.BeginOutputReadLine();
                // process.BeginErrorReadLine();
                //
                // process.WaitForExit();
                //Console.WriteLine($"프로세스 종료 코드: {process.ExitCode}");
                return Task.CompletedTask;
            });
        AnsiConsole.MarkupLine("[green]DB초기화가 완료 되었습니다.[/]");
    }

    [Command(CommandFilter.Any, Interrupt = true)]
    public async ValueTask UpdateToolAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]툴 업데이트를 시작합니다.[/]");
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Arrow3)
            .SpinnerStyle(new Style(foreground: Color.Green))
            .StartAsync("docker-compose exec avalon-service-dev dotnet publish", async ctx =>
            {
                await Docker.Compose.UpAsync("-d avalon-service-dev", cancellationToken);

                await Docker.Compose.ExecAsync(
                    $"avalon-service-dev /bin/bash -c {"cd /avalon_builds/dev-tool && dotnet publish -c Release -r osx-x64 . --self-contained true /p:PublishSingleFile=true".TerminalQuote()}",
                    cancellationToken: cancellationToken);

                await Docker.Compose.ExecAsync(
                    $"avalon-service-dev /bin/bash -c {"cd /avalon_builds/dev-tool && dotnet publish -c Release -r win-x64 . --self-contained true /p:PublishSingleFile=true".TerminalQuote()}",
                    cancellationToken: cancellationToken);

                Program.ClearDirectory("./publish");
                await Terminal.ExecuteCommandAsync($"docker cp hxh-avalon-service-dev-1:/avalon_builds/dev-tool/bin/Release/net9.0/osx-x64/publish/AvalonDevTool ./publish/",
                    cancellationToken: cancellationToken);

                await Terminal.ExecuteCommandAsync($"docker cp hxh-avalon-service-dev-1:/avalon_builds/dev-tool/bin/Release/net9.0/win-x64/publish/AvalonDevTool.exe ./publish/",
                    cancellationToken: cancellationToken);

                AnsiConsole.MarkupLine("[green]빌드 결과가 publish 디렉토리에 생성 되었습니다.[/]");
                
                var publishDir = "./publish";
                var exeNames = new[] { "AvalonDevTool.exe", "AvalonDevTool" };

                foreach (var exeName in exeNames)
                {
                    var publishPath = Path.Combine(publishDir, exeName);
                    var currentExe = Path.Combine("./", exeName);

                    var bakFile = currentExe + ".bak";
                    if (File.Exists(bakFile))
                    {
                        try { File.Delete(bakFile); } catch { /* 무시 */ }
                    }

                    if (File.Exists(publishPath))
                    {
                        // 현재 실행 파일 백업
                        if (File.Exists(currentExe))
                            File.Move(currentExe, bakFile, overwrite: true);

                        // 새 파일로 덮어쓰기
                        File.Move(publishPath, currentExe, overwrite: true);
                    }
                }
                AnsiConsole.MarkupLine("[yellow]잠시 후 종료합니다...[/]");
                await Task.Delay(1000);
                Environment.Exit(0);
            });
    }

    [Command(CommandFilter.Any)]
    public ValueTask Exit(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[b]{Program.AppName}[/]을 [green]종료[/]합니다.");
        Pop();
        return ValueTask.CompletedTask;
    }

    protected override CommandFilter Filter => _filter;
}
