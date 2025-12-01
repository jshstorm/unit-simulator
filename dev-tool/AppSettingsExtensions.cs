using AvalonDevTool.SCM;

namespace AvalonDevTool;

internal static class AppSettingsExtensions
{
    public static GitWorkspace.PullOptions MakePullOptions(this AppSettings this_)
    {
        return new GitWorkspace.PullOptions
        {
            Rebase = this_.Git.InsteadRebase,
            Autostash = this_.Git.Autostash
        };
    }
}
