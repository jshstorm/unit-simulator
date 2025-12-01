using AvalonDevTool.Externals;

namespace AvalonDevTool.SCM;

internal abstract class Workspace(string workingDirectory, string remoteUrl)
{
    public string WorkingDirectory { get; } = Path.GetFullPath(workingDirectory);

    public string RemoteUrl { get; } = remoteUrl;

    public abstract bool Exists { get; }

    public virtual string Name { get; } = Path.GetFileName(workingDirectory);

    public Terminal.LiveLog Logging { get; set; }

    protected Terminal.Options GenerateTerminalOptions()
        => new()
        {
            WorkingDirectory = WorkingDirectory,
            Logging = Logging
        };
}
