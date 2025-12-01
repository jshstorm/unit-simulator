using System.Text.RegularExpressions;
using AvalonDevTool.Externals;
using Spectre.Console;

namespace AvalonDevTool.SCM;

internal partial class GitWorkspace(string workingDirectory, string remoteUrl) : Workspace(workingDirectory, remoteUrl)
{
    public readonly record struct QueryBranchesOptions
    {
        public bool Remote { get; init; }
    }

    public readonly record struct PullOptions
    {
        public bool Rebase { get; init; }

        public bool Autostash { get; init; }
    }

    public enum ResetMode
    {
        Mixed,
        Soft,
        Hard
    }

    public readonly record struct ResetOptions
    {
        public ResetMode Mode { get; init; }
    }

    public readonly record struct TrackingInfo
    {
        public required string RemoteName { get; init; }

        public int Difference { get; init; }

        public override string ToString()
        {
            return Difference switch
            {
                0 => $"{RemoteName}",
                <0 => $"{RemoteName}: behind {Difference}",
                >0 => $"{RemoteName}: ahead {-Difference}"
            };
        }
    }

    public readonly partial record struct BranchInfo
    {
        /// <summary>
        /// 브랜치 이름입니다.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// 브랜치의 현재 위치를 나타내는 해시(SHA) 값입니다.
        /// </summary>
        public required string Hash { get; init; }

        /// <summary>
        /// 브랜치 정보가 원격 브랜치를 대상으로 하는지 나타내는 값입니다.
        /// </summary>
        public bool IsRemote { get; init; }

        /// <summary>
        /// 작업 공간이 이 브랜치를 대상으로 하고 있는지 나타내는 값입니다.
        /// </summary>
        public bool IsCurrent { get; init; }

        /// <summary>
        /// 현재 브랜치가 원격 브랜치를 추적하는 경우, 추적에 대한 정보를 제공합니다.
        /// </summary>
        public TrackingInfo? Tracking { get; init; }

        /// <summary>
        /// 현재 브랜치의 간단한 정보를 제공합니다.
        /// </summary>
        public required string Description { get; init; }

        public override string ToString()
        {
            if (Tracking.HasValue)
            {
                return $"{Name} {Hash} [{Tracking.Value}] {Description}";
            }
            else
            {
                return $"{Name} {Hash} {Description}";
            }
        }

        public static bool TryParse(bool isRemote, string input, out BranchInfo result)
        {
            bool isCurrent = input.StartsWith('*');
            if (isCurrent)
            {
                input = input.TrimStart('*', ' ', '\t');
            }

            var match1 = ParseBranchOutputRegex().Match(input);
            if (match1.Success == false)
            {
                result = default;
                return false;
            }

            result = new BranchInfo
            {
                Name = match1.Groups[1].Value,
                Hash = match1.Groups[2].Value,
                IsRemote = isRemote,
                IsCurrent = isCurrent,
                Description = match1.Groups[4].Value
            };

            if (string.IsNullOrEmpty(match1.Groups[3].Value) == false)
            {
                var tracking = match1.Groups[3].Value;
                var match2 = ParseRemoteTrackingBehindRegex().Match(tracking);
                int behind = 0;
                if (match2.Success)
                {
                    if (match2.Groups[1].Value == "behind")
                    {
                        behind = -int.Parse(match2.Groups[2].Value);
                    }
                    else
                    {
                        behind = int.Parse(match2.Groups[2].Value);
                    }

                    tracking = tracking.Replace(match2.Value, string.Empty);
                }

                result = result with
                {
                    Tracking = new TrackingInfo
                    {
                        RemoteName = tracking.Trim('[', ']'),
                        Difference = behind
                    }
                };
            }

            return true;
        }

        [GeneratedRegex(@"(\S+)\s+([a-f0-9]+)(?:\s+(\[origin\/.*?\]))?\s+(.*)")]
        private static partial Regex ParseBranchOutputRegex();
        [GeneratedRegex(@": (behind|ahead) (\d+)")]
        private static partial Regex ParseRemoteTrackingBehindRegex();
    }

    public override bool Exists
    {
        get
        {
            // 디렉토리 및 필수 파일들이 존재하는지 간단하게 검사합니다.
            if (Directory.Exists(WorkingDirectory) == false)
            {
                return false;
            }

            var git = Path.Combine(WorkingDirectory, ".git");
            if (Directory.Exists(git) == false)
            {
                return false;
            }

            var index = Path.Combine(git, "index");
            if (File.Exists(index) == false)
            {
                return false;
            }

            return true;
        }
    }

    public async Task<Terminal.Output> CloneAsync(CancellationToken cancellationToken = default)
    {
        var options = GenerateTerminalOptions();
        return await ExecuteCommandAsync($"clone {RemoteUrl} .", options, cancellationToken);
    }

    public async Task<Terminal.Output> PullAsync(PullOptions pullOptions, CancellationToken cancellationToken = default)
    {
        List<string> args = [];
        if (pullOptions.Rebase)
        {
            args.Add("--rebase");
        }
        if (pullOptions.Autostash)
        {
            args.Add("--autostash");
        }

        var terminalOptions = GenerateTerminalOptions();
        return await ExecuteCommandAsync($"pull {string.Join(' ', args)}", terminalOptions, cancellationToken);
    }

    public async Task<BranchInfo?> GetBranchInfo(CancellationToken cancellationToken = default)
    {
        List<string> args = ["-vv"];

        var terminalOptions = GenerateTerminalOptions() with { Logging = Terminal.LiveLog.None };
        var output = await ExecuteCommandAsync($"branch {string.Join(' ', args)}", terminalOptions, cancellationToken);
        output.ThrowIfFailure();

        var lines = output.StdOut.Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith('*') && BranchInfo.TryParse(false, line, out var branchInfo))
            {
                return branchInfo;
            }
        }

        return null;
    }
    
    public string ConvertToLocalBranch(string branchName)
    {
        var match = Regex.Match(branchName, @"origin/(.+)");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return branchName;
    }

    public async Task<bool> SwitchBranchAsync(string target, CancellationToken cancellationToken = default)
    {
        // 로컬 브랜치 목록 조회
        var branches = await QueryBranchesAsync(new QueryBranchesOptions { Remote = true }, cancellationToken);
        bool exists = branches.Any(b => b.Name == target);

        var terminalOptions = GenerateTerminalOptions();

        var status = await ExecuteCommandAsync("status --porcelain", terminalOptions, cancellationToken);
        bool hasConflict = status.StdOut
            .Split('\n')
            .Any(line => line.StartsWith("UU") || line.StartsWith("AA") || line.StartsWith("DD") || line.StartsWith("DU"));
        if (hasConflict)
        {
            AnsiConsole.MarkupLine($"----> {Name} [yellow]충돌이 있어 브랜치를 전환할 수 없습니다.[/]");
            return false;
        }
        
        var statusLines = status.StdOut
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("??"))
            .ToArray();
        
        bool hasChanges = statusLines.Length > 0;
        if (hasChanges) {
            AnsiConsole.MarkupLine($"{status.StdOut}");
            AnsiConsole.MarkupLine($"---------stashing----------");
            await ExecuteCommandAsync("stash push -u", terminalOptions, cancellationToken);
        }

        string command = exists ? $"checkout {target}" : $"checkout -b {target} --track origin/{target}";
        await ExecuteCommandAsync(command, terminalOptions, cancellationToken);
        if (hasChanges == false) {
            return true;
        }

        //AnsiConsole.MarkupLine($"---------stash restore----------");
        //await ExecuteCommandAsync("stash pop", terminalOptions, cancellationToken);
        return true;
    }

    public async Task<Terminal.Output> DiscardLocalChangesAsync(string path, CancellationToken cancellationToken = default)
    {
        var option = GenerateTerminalOptions();
        await ExecuteCommandAsync($"checkout -- {path}", option, cancellationToken);
        return await ExecuteCommandAsync($"clean -fd -- {path}", option, cancellationToken);
    }

    public async Task<Terminal.Output> ResetAsync(string target, ResetOptions resetOptions, CancellationToken cancellationToken = default)
    {
        List<string> args = [];
        switch (resetOptions.Mode)
        {
            case ResetMode.Soft:
                args.Add("--soft");
                break;
            case ResetMode.Hard:
                args.Add("--hard");
                break;
        }

        var terminalOptions = GenerateTerminalOptions();
        return await ExecuteCommandAsync($"reset {string.Join(' ', args)} {target}", terminalOptions, cancellationToken);
    }

    public async Task<BranchInfo[]> QueryBranchesAsync(QueryBranchesOptions queryOptions, CancellationToken cancellationToken = default)
    {
        List<string> args = ["-vv"];
        if (queryOptions.Remote)
        {
            args.Add("-r");
        }

        var terminalOptions = GenerateTerminalOptions() with { Logging = Terminal.LiveLog.None };
        var output = await ExecuteCommandAsync($"branch {string.Join(' ', args)}", terminalOptions, cancellationToken);
        output.ThrowIfFailure();

        var lines = output.StdOut.Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var result = new List<BranchInfo>();
        foreach (var line in lines)
        {
            if (BranchInfo.TryParse(queryOptions.Remote, line, out var branchInfo))
            {
                result.Add(branchInfo);
            }
        }

        return [.. result];
    }

    private async Task<Terminal.Output> ExecuteCommandAsync(string command, Terminal.Options options, CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(WorkingDirectory) == false)
        {
            Directory.CreateDirectory(WorkingDirectory);
        }

        return await Terminal.ExecuteCommandAsync($"git {command}", options, cancellationToken);
    }
}
