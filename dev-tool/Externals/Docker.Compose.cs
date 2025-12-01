using AvalonDevTool.Externals;

namespace AvalonDevTool;

internal static partial class Docker
{
    public static class Compose
    {
        public static string ProjectName => AppSettings.Current.ProjectName;

        public static Task RunAsync(string args, CancellationToken cancellationToken = default)
            => DoCommandAsync("run --rm", args, cancellationToken);
        
        public static Task UpAsync(string args, CancellationToken cancellationToken = default)
            => DoCommandAsync("up", args, cancellationToken);

        public static Task StopAsync(string args, CancellationToken cancellationToken = default)
            => DoCommandAsync("stop", args, cancellationToken);

        public static Task DownAsync(string args, CancellationToken cancellationToken = default)
            => DoCommandAsync("down", args, cancellationToken);

        public static Task ExecAsync(string args, CancellationToken cancellationToken = default)
            => DoCommandAsync("exec", args, cancellationToken);

        public static async Task DoCommandAsync(string command, string args, CancellationToken cancellationToken = default)
        {
            var composePath = GetComposeFilePath();
            if (!File.Exists(composePath))
                throw new FileNotFoundException($"Compose file not found: {composePath}");

            var output = await Terminal.ExecuteCommandAsync($"docker-compose -f {composePath} --project-name {ProjectName} --project-directory . {command} {args}", new Terminal.Options { WorkingDirectory = ".", Logging = Terminal.LiveLog.StdOut | Terminal.LiveLog.StdErr }, cancellationToken: cancellationToken).Report();
            if (output.IsFailure)
            {
                throw new CommandFailureException();
            }
        }

        private static string GetComposeFilePath()
        {
            return "./avalon_builds/compose/dev-compose/docker-compose.yml";
        }
    }
}