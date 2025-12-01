using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using AvalonDevTool;
using AvalonDevTool.CLI;
using AvalonDevTool.Commands;
using AvalonDevTool.Externals;
using AvalonDevTool.SCM;
using CommandLine;
using CommandLine.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Spectre.Console;
using YamlDotNet.RepresentationModel;

internal static class Program
{
    public const string AppName = "AvalonDevTool";
    private const string tokenFilePath = "./avalon_builds/protoTool/gsheet_token";
    private const string cacheUrl = "https://avalon-gsheet-cache-dev.clovergames.dev:5901";

    private static Dictionary<string, string> DockerImages = new();

    static async Task<int> Main(string[] args)
    {
        RemovePreviousExeFiles();

        var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, args) =>
        {
            args.Cancel = true;
            cancellationTokenSource.Cancel();
            AnsiConsole.MarkupLine("[red]SIGINT[/]");
        };

        try
        {
            await AppSettings.ConfigureAsync(cancellationTokenSource.Token);

            if (args.Length > 0)
            {
                var command = args[0];
                var results = Parser.Default.ParseArguments<Options>(args);

                results.WithNotParsed(errors =>
                {
                    var helpText = HelpText.AutoBuild(results);
                    if (errors.Any(p => p is HelpRequestedError or VersionRequestedError))
                    {
                        AnsiConsole.WriteLine(helpText);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine(string.Join('\n', errors.Select(p => $"[red]{p.ToString().EscapeMarkup()}[/]")));
                    }

                    throw TerminateException.User();
                });

                var options = results.Value;
                CommandGroup.Push<MainMenuCommands>();
                await CommandGroup.ExecuteRouteAsync(options.CommandRoute, cancellationTokenSource.Token);
                AnsiConsole.WriteLine("작업이 완료되었습니다.");
            }
            else
            {
                CommandGroup.Push<MainMenuCommands>();
                AnsiConsole.Write(new FigletText(AppName).LeftJustified().Color(Color.Green));

                while (CommandGroup.IsLive)
                {
                    try
                    {
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        await CommandGroup.DisplayPromptAsync(cancellationTokenSource.Token);
                    }
                    catch (CommandFailureException)
                    {
                        AnsiConsole.MarkupLine("[red]작업이 하나 이상 실패하였습니다. 자세한 내용은 오류 로그를 확인하세요.[/]");
                        AnsiConsole.WriteLine("계속하려면 키를 누르세요...");
                        await AnsiConsole.Console.Input.ReadKeyAsync(true, cancellationTokenSource.Token);
                    }
                }
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Operation aborted.[/]");
            return TerminateException.EC_Abort;
        }
        catch (TerminateException e)
        {
            return e.ExitCode;
        }
        catch (AggregateException e)
        {
            if (e.InnerExceptions.Any(p => p is not OperationCanceledException))
            {
                throw;
            }

            AnsiConsole.MarkupLine("[red]Operation aborted.[/]");
            return TerminateException.EC_Abort;
        }
    }

    public static void RemovePreviousExeFiles()
    {
        var publishDir = "./publish";
        if (!Directory.Exists(publishDir))
            return;

        var exeNames = new[] { "AvalonDevTool.exe", "AvalonDevTool" };

        foreach (var exeName in exeNames)
        {
            var prevFile = "./" + exeName + ".bak";
            if (File.Exists(prevFile))
                File.Delete(prevFile);
        }
        Directory.Delete(publishDir, true);
    }

    private class GSheetTokenInfo
    {
        public string IdToken { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime Expiry { get; set; }
    }

    private static async Task<string?> GetCachedGSheetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(tokenFilePath))
        {
            AnsiConsole.MarkupLine($"[red]토큰 파일이 존재하지 않습니다. (path: {tokenFilePath})[/]");
            return null;
        }

        string? token;
        using (var reader = new StreamReader(tokenFilePath, Encoding.UTF8))
        {
            token = await reader.ReadLineAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            AnsiConsole.MarkupLine("[red]토큰 파일이 비어 있습니다.[/]");
            return null;
        }
    
        AnsiConsole.MarkupLine($"[green]로컬 토큰 읽음: {token}[/]");

        var checkTokenUrl = $"{cacheUrl}/check-token";
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
            var response = await client.GetAsync(checkTokenUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine("[green]캐시 서버에서 토큰 유효성 확인 성공.[/]");
                return token;
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]캐시 서버 토큰 확인 실패: {response.StatusCode}[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]토큰 체크 요청 중 예외 발생: {ex.Message}[/]");
        }

        return null;
    }

    private static async Task SaveTokenToCacheAsync(GSheetTokenInfo tokenInfo, CancellationToken cancellationToken = default)
    {
        var url = $"{cacheUrl}/register-id-token";

        try
        {
            using var client = new HttpClient();
            var payload = new { id_token = tokenInfo.IdToken };

            var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken);
                if (content != null && content.TryGetValue("token", out var token))
                {
                    await File.WriteAllTextAsync(tokenFilePath, token, cancellationToken);
                    AnsiConsole.MarkupLine($"[green]Remote cache token received: {token}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No token found in the remote cache response.[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Token cache registration failed: {response.StatusCode}[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Exception during token cache registration: {ex.Message}[/]");
        }
    }

    private static async Task GenerateGSheetToken(CancellationToken cancellationToken = default)
    {
        var credPath = "gcp-token";
        if (Directory.Exists(credPath))
        {
            Directory.Delete(credPath, true);
            AnsiConsole.MarkupLine($"[green]Deleted existing {credPath} directory.[/]");
        }

        await GetGSheetToken(cancellationToken);
    }
    
    public static async Task GetGSheetToken(CancellationToken cancellationToken = default)
    {
        var cachedToken = await GetCachedGSheetTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(cachedToken))
        {
            AnsiConsole.MarkupLine($"[green]캐시된 토큰을 사용합니다:[/] {cachedToken}");
            return;
        }

        var scopes = new[] { "https://www.googleapis.com/auth/userinfo.profile" };
        var credPath = "gcp-token";

        if(Directory.Exists(credPath)) {
            AnsiConsole.MarkupLine($"[green]기존 credential directory를 삭제합니다.:[/]");
            try {
                Directory.Delete(credPath, true); // true enables recursive deletion
                AnsiConsole.MarkupLine($"[green]Deleted existing {credPath} directory.[/]");
            }
            catch (IOException ex) {
                AnsiConsole.MarkupLine($"[red]Failed to delete {credPath}: {ex.Message}[/]");
                // Try alternative approach if needed
            }
        }

        await using var stream = new FileStream("./avalon_builds/protoTool/api_key/gcp/avalonClientSecret.json", FileMode.Open, FileAccess.Read);
        var clientSecrets = (await GoogleClientSecrets.FromStreamAsync(stream, cancellationToken)).Secrets;
    
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets,
            scopes,
            "user",
            cancellationToken,
            new FileDataStore(credPath, true)
        );
    
        AnsiConsole.MarkupLine("[green]Token successfully obtained.[/]");
        //AnsiConsole.MarkupLine($"[green]Access Token:[/] {credential.Token.AccessToken}");

        if (credential.Token.ExpiresInSeconds != null)
        {
            DateTime expiry = credential.Token.IssuedUtc.AddSeconds(credential.Token.ExpiresInSeconds.Value);
            var tokenInfo = new GSheetTokenInfo
            {
                IdToken = credential.Token.IdToken,
                AccessToken = credential.Token.AccessToken,
                RefreshToken = credential.Token.RefreshToken,
                Expiry = expiry
            };

            await SaveTokenToCacheAsync(tokenInfo, cancellationToken);
        }
    }

    private static Process? StartProcess(string command, string workingDirectory = ".")
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/c {command}" : $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

        var process = Process.Start(processInfo);
        if (process == null)
        {
            AnsiConsole.MarkupLine("[red]프로세스를 시작할 수 없습니다.[/]");
            return null;
        }

        return process;
    }
    
    private static string ReadProcessOutput(Process? process)
    {
        if (process == null) {
            return string.Empty;
        }

        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result.Trim();
    }

    private static async Task ExecuteCommandAsync(string command, string workingDirectory = ".", CancellationToken cancellationToken = default)
    {
        await Terminal.ExecuteCommandAsync(command, workingDirectory, cancellationToken).Report();
    }

    private static async Task ExecuteCommandAsync(string[] commands, string workingDirectory = ".", CancellationToken cancellationToken = default)
    {
        foreach (var command in commands) {
            await ExecuteCommandAsync(command, workingDirectory, cancellationToken);
        }
    }
    
    private static string ExecuteCommandWithResult(string command, string workingDirectory = ".")
    {
        var process = StartProcess(command, workingDirectory);
        return ReadProcessOutput(process);
    }
    
    private static async Task<(string Output, string Error, int ExitCode)> ExecuteCommandWithResultAsync(string command, string workingDirectory = ".")
    {
        var process = StartProcess(command, workingDirectory);
        if (process == null) {
            return (string.Empty, "프로세스를 시작할 수 없습니다.", -1);
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (output.Trim(), error.Trim(), process.ExitCode);
    }
    
    public static int CopyFiles(string sourceDirectory, string destinationDirectory, params string[] filePatterns)
    {
        int copied = 0;

        // 소스 디렉토리가 없으면 예외 방지 및 알림
        if (!Directory.Exists(sourceDirectory))
        {
            AnsiConsole.MarkupLine($"[red]소스 디렉토리가 존재하지 않습니다: {sourceDirectory}[/]");
            return copied;
        }

        // 대상 디렉토리가 없으면 생성
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        foreach (var filePattern in filePatterns)
        {
            foreach (var sourceFilePath in Directory.GetFiles(sourceDirectory, filePattern))
            {
                var fileName = Path.GetFileName(sourceFilePath);
                var destinationFilePath = Path.Combine(destinationDirectory, fileName);
                File.Copy(sourceFilePath, destinationFilePath, true);
                copied++;
            }
        }

        return copied;
    }

    public static int ClearDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }

            Directory.CreateDirectory(dir);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]디렉토리를 정리하는 중 오류 발생: {ex.Message}[/]");
            return -1;
        }
    }

    private static bool IsRunningOnAppleSilicon()
    {
        var osDescription = RuntimeInformation.OSDescription;
        return osDescription.Contains("Darwin") && osDescription.Contains("ARM64");
    }
    
    public static List<string> GetDockerComposeServiceNames(string filePath)
    {
        var serviceNames = new List<string>();
        var yaml = new YamlStream();

        using (var reader = File.OpenText(filePath))
        {
            yaml.Load(reader);
        }

        if (yaml.Documents.Count > 0)
        {
            var rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;
            if (rootNode.Children.ContainsKey("services"))
            {
                var servicesNode = (YamlMappingNode)rootNode.Children[new YamlScalarNode("services")];
                foreach (var entry in servicesNode)
                {
                    var value = ((YamlScalarNode)entry.Key).Value;
                    if (value != null) serviceNames.Add(value);
                }
            }
        }

        return serviceNames;
    }
    
    private static string GetCurrentBranchName(string directoryPath)
    {
        // Check if the directory is a Git repository
        if (Directory.Exists(Path.Combine(directoryPath, ".git")))
        {
            var gitBranch = ExecuteCommandWithResult("git rev-parse --abbrev-ref HEAD", directoryPath);
            return $"Git ({gitBranch})";
        }
        // Check if the directory is a SVN repository
        else if (Directory.Exists(Path.Combine(directoryPath, ".svn")))
        {
            var svnInfo = ExecuteCommandWithResult("svn info --show-item relative-url", directoryPath);
            var svnBranch = svnInfo.Split('/').Last();
            return $"SVN ({svnBranch})";
        }
        else
        {
            return "The directory is not a Git or SVN repository.";
        }
    }
    
    public static bool DecompressGZip(string gzipFilePath, string destinationFilePath)
    {
        try
        {
            using var originalFileStream = File.OpenRead(gzipFilePath);
            using var decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress);
            using var decompressedFileStream = File.Create(destinationFilePath);
            decompressionStream.CopyTo(decompressedFileStream);

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]GZIP 압축 해제에 실패했습니다: {ex.Message}[/]");
            return false;
        }
    }

    private static void DeleteOldDataSyncCacheFilesUseWeeks(int weeks)
    {
        AnsiConsole.MarkupLine($"[green]{weeks} 주일이 초과한 문서를 정리합니다.[/]");

        string targetDirectory = "./AvalonGameServer/protoTool/DataSync/docs_data/";
        if (!Directory.Exists(targetDirectory)) {
            return;
        }

        var cutoff = DateTime.Now.AddDays(-7 * weeks);
        var files = Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories)
            .Where(file => File.GetLastWriteTime(file) < cutoff);

        int fileCount = files.Count();

        foreach (var file in files)
        {
            File.Delete(file);
        }

        AnsiConsole.MarkupLine($"[green]{fileCount}개의 파일을 삭제했습니다.[/]");
    }

    public static void ClearPreviousReferences()
    {
        AnsiConsole.MarkupLine($"[green]모든 레퍼런스 데이터 파일을 정리합니다.[/]");

        string targetDirectory = "./avalon_builds/protoTool/out/";
        if (!Directory.Exists(targetDirectory)) {
            return;
        }

        string[] extensions = { "*.json", "*.bytes", "*.pb" };
        int fileCount = 0;

        foreach (var ext in extensions)
        {
            var files = Directory.GetFiles(targetDirectory, ext, SearchOption.TopDirectoryOnly);
            fileCount += files.Length;
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        AnsiConsole.MarkupLine($"[green]{fileCount}개의 파일을 삭제했습니다.[/]");
    }

    public static void ClearDataSheet()
    {
        AnsiConsole.MarkupLine($"[green]모든 문서를 정리합니다.[/]");

        string targetDirectory = "./avalon_builds/protoTool/docs_data/";
        if (!Directory.Exists(targetDirectory))
        {
            return;
        }

        var files = Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories);
        int fileCount = files.Length;

        foreach (var file in files)
        {
            File.Delete(file);
        }

        AnsiConsole.MarkupLine($"[green]{fileCount}개의 파일을 삭제했습니다.[/]");
    }

    static bool ExecuteGitCommand(string arguments, string workingDirectory)
    {
        try
        {
            var processInfo = new ProcessStartInfo("git", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            using var process = new Process { StartInfo = processInfo };
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AnsiConsole.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AnsiConsole.WriteLine($"Error: {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
            return false;
        }
    }

    static List<string> GetRemoteBranches(string workingDirectory)
    {
        var branches = new List<string>();
        try
        {
            var processInfo = new ProcessStartInfo("git", "branch -r")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            using var process = new Process { StartInfo = processInfo };
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    branches.Add(e.Data.Trim());
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AnsiConsole.MarkupLine($"[red]{e.Data}[/]");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]An error occurred while retrieving remote branches: {ex.Message}[/]");
        }

        return branches;
    }

    static void PrintRemoteBranches(string workingDirectory)
    {
        var remoteBranches = GetRemoteBranches(workingDirectory);
        AnsiConsole.MarkupLine("[yellow]Available remote branches:[/]");
        foreach (var branch in remoteBranches)
        {
            AnsiConsole.MarkupLine(branch);
        }
    }

    static bool ForceUpdateToLatest(string workingDirectory, string branchName)
    {
        return ExecuteGitCommand($"reset --hard origin/{branchName}", workingDirectory);
    }
    
    public static async Task PullDeployRepositoryAsync(CancellationToken cancellationToken)
    {
        var current = AppSettings.Current;
        var branchName = current.Branch;
        branchName = branchName.StartsWith("origin/") ? branchName.Substring("origin/".Length) : branchName;
        AnsiConsole.MarkupLine($"서버 branch ({branchName}) 시작합니다..");

        // svn checkout -> get dll
        var url = $"trunk/server/{branchName}"; 
        AnsiConsole.MarkupLine($"===== svn checkout=====");
        AnsiConsole.MarkupLine($"===== svn url: ===== https://avalon-svn.clover.games:18080/svn/AvalonPatchData/trunk/server/{branchName}");

        var workspace = new SVNWorkspace("./app", "https://avalon-svn.clover.games:18080/svn/AvalonPatchData", new SVNWorkspace.UserCredentials {
            Username = "avalon-buildbot",
            Password = "MJTyN8y3v5a91jM"
        });

        if (workspace.Exists == false)
        {
            AnsiConsole.MarkupLine($"server repository가 존재하지 않습니다. 체크아웃을 시작합니다.");
            var result = await workspace.CheckoutAsync(url, cancellationToken).Report();
            if (result.IsFailure) {
                throw new CommandFailureException();
            }
            return;
        }

        AnsiConsole.MarkupLine($"server repository가 존재합니다. 로컬 변경 사항을 폐기합니다.");
        await workspace.RevertAsync(cancellationToken);

        AnsiConsole.MarkupLine($"업데이트를 시작합니다.");
        await workspace.SwitchAsync(url, cancellationToken);

        var output = await workspace.InfoAsync(cancellationToken);
        AnsiConsole.MarkupLine($"{output}");
    }
    
    public static async Task DiscardLocalReferenceChanges(List<GitWorkspace> repositories, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[green]local changes를 폐기합니다.[/]");
        var clientRepo = repositories.FirstOrDefault(r => r.Name == "AvalonClient");
        if (clientRepo != null) {
            await clientRepo.DiscardLocalChangesAsync("./Assets/Game/RemoteResources/GameData/", cancellationToken);
            await clientRepo.DiscardLocalChangesAsync("./Assets/Game/Script/Protocol/", cancellationToken);
            AnsiConsole.MarkupLine($"client local changes를 폐기했습니다.");
        }
        var serverRepo = repositories.FirstOrDefault(r => r.Name == "AvalonServer");
        if (serverRepo != null) {
            await serverRepo.DiscardLocalChangesAsync("./AvalonService/protoData/", cancellationToken);
            AnsiConsole.MarkupLine($"server local changes를 폐기했습니다.");
        }
        AnsiConsole.MarkupLine($"[green]local changes를 폐기했습니다.[/]");
    }
    
    public static bool ApplyDeployedReferences(CancellationToken cancellationToken)
    {
        const string sourceDirectory = "./app/reference";
        if (Directory.Exists(sourceDirectory) == false) {
            AnsiConsole.MarkupLine("[red]app/reference 디렉토리가 존재하지 않습니다.[/]");
            return false;
        }

        var fileNames = new[] { "filelist.json", "text_filelist.json" };
        var clientDirectory = "./AvalonClient/Assets/Game/RemoteResources/GameData/";

        foreach (var fileName in fileNames)
        {
            var sourceFile = Path.Combine(sourceDirectory, fileName);
            var destinationFile = Path.Combine(clientDirectory, fileName);

            File.Copy(sourceFile, destinationFile, true);
        }
        
        Program.CopyFiles(sourceDirectory,"./AvalonClient/Assets/Game/RemoteResources/GameData/", "*.bytes");
        Program.CopyFiles(sourceDirectory,"./AvalonServer/AvalonService/protoData/", "*.bytes");
        Program.CopyFiles($"{sourceDirectory}/client_source","./AvalonClient/Assets/Game/Script/Protocol/", "*.cs");
        return true;
    }
}
