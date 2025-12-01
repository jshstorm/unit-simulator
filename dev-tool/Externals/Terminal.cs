using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using AvalonDevTool.Externals;
using Spectre.Console;
// ReSharper disable MemberHidesStaticFromOuterClass

namespace AvalonDevTool.Externals;

internal static class Terminal
{
    [Flags]
    public enum LiveLog
    {
        None,
        StdOut = 0x01,
        StdErr = 0x02
    }

    public readonly record struct Output
    {
        public required string Command { get; init; }

        public required int ExitCode { get; init; }

        public required LiveLog Logging { get; init; }

        public required string Logs { get; init; }

        public required string StdOut { get; init; }

        public required string StdErr { get; init; }

        public bool IsSuccess => ExitCode == 0;

        public bool IsFailure => ExitCode != 0;

        public void ThrowIfFailure()
        {
            if (IsFailure)
            {
                throw TerminateException.Internal();
            }
        }
    }

    public readonly record struct Options
    {
        public required string WorkingDirectory { get; init; }

        public required LiveLog Logging { get; init; }
    }

    public static Task<Output> ExecuteCommandAsync(string command, string workingDirectory = ".", CancellationToken cancellationToken = default)
        => ExecuteCommandAsync(command, new Options
        {
            WorkingDirectory = workingDirectory,
            Logging = LiveLog.StdOut
        }, cancellationToken);

    public static async Task<Output> ExecuteCommandAsync(string command, Options options, CancellationToken cancellationToken = default)
    {
        var process = StartTerminalProcessWith(command, options.WorkingDirectory);
        List<string> logs = [];
        List<string> stdout = [];
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (stdout)
                {
                    stdout.Add(e.Data);
                }

                lock (logs)
                {
                    logs.Add(e.Data);
                }

                if (options.Logging.HasFlag(LiveLog.StdOut))
                {
                    lock (AnsiConsole.Console)
                    {
                        AnsiConsole.WriteLine(e.Data);
                    }
                }
            }
        };

        List<string> stderr = [];
        process.ErrorDataReceived += (_, e) =>
        {
            const string pattern = @"\[(.*?)\]";
            const string replacement = "{$1}";
            var result = Regex.Replace($"{e.Data}", pattern, replacement);
            if (string.IsNullOrEmpty(result) == false)
            {
                lock (stderr)
                {
                    stderr.Add(result);
                }

                lock (logs)
                {
                    logs.Add(result);
                }

                if (options.Logging.HasFlag(LiveLog.StdErr))
                {
                    lock (AnsiConsole.Console)
                    {
                        AnsiConsole.WriteLine("{0}", result);
                    }
                }
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await using (cancellationToken.Register(() => process.Kill()))
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        int exitCode = process.ExitCode;

        return new Output
        {
            Command = command,
            ExitCode = exitCode,
            Logging = options.Logging,
            Logs = string.Join('\n', logs).Trim(),
            StdOut = string.Join('\n', stdout).Trim(),
            StdErr = string.Join('\n', stderr).Trim()
        };
    }

    public static Output Report(this Output output)
    {
        if (output.ExitCode != 0)
        {
            lock (AnsiConsole.Console)
            {
                AnsiConsole.MarkupLine("[red][b]{0}[/] 명령 실행이 {1} 오류 코드로 실패했습니다.[/]", output.Command, output.ExitCode);

                if ((output.Logging & LiveLog.StdErr) == 0)
                {
                    AnsiConsole.MarkupLine("[red]----- 에러 로그 시작 -----[/]");
                    AnsiConsole.WriteLine(output.StdErr);
                    AnsiConsole.MarkupLine("[red]----- 에러 로그 끝 -----[/]");
                }
            }
        }

        return output;
    }

    public static Task<Output> Report(this Task<Output> outputTask) => outputTask.ContinueWith(p => p.Result.Report());

    public static Task<string> StdOut(this Task<Output> outputTask) => outputTask.ContinueWith(p => p.Result.StdOut);

    private static Process StartTerminalProcessWith(string command, string workingDirectory = ".")
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/c {command}" : $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

        var process = Process.Start(processInfo);
        if (process == null)
        {
            AnsiConsole.MarkupLine("[red]내부 오류: 프로세스를 시작할 수 없습니다.[/]");
            throw TerminateException.Internal();
        }

        return process;
    }

}
