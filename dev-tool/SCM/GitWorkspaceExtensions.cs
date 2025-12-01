namespace AvalonDevTool.SCM;

internal static class GitWorkspaceExtensions
{
    public static async Task<GitWorkspace.BranchInfo?> QueryCurrentBranchAsync(this GitWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var branches = await workspace.QueryBranchesAsync(default, cancellationToken);
        var current = branches.FirstOrDefault(p => p.IsCurrent);
        return current == default ? null : current;
    }
}
