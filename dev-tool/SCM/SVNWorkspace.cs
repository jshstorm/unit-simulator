using AvalonDevTool.Externals;

namespace AvalonDevTool.SCM;

internal class SVNWorkspace(string workingDirectory, string remoteUrl, SVNWorkspace.Credentials? credentials) : Workspace(workingDirectory, remoteUrl)
{
    public abstract record Credentials
    {
    }

    public record UserCredentials : Credentials
    {
        public required string Username { get; init; }

        public required string Password { get; init; }
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

            var svn = Path.Combine(WorkingDirectory, ".svn");
            if (Directory.Exists(svn) == false)
            {
                return false;
            }

            var db = Path.Combine(svn, "wc.db");
            if (File.Exists(db) == false)
            {
                return false;
            }

            return true;
        }
    }

    public async Task<Terminal.Output> CheckoutAsync(string targetBranch, CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync($"checkout {RemoteUrl}/{targetBranch} .", cancellationToken);
    }

    public async Task<Terminal.Output> InfoAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync($"info .", cancellationToken);
    }

    public async Task<Terminal.Output> RevertAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync($"revert -R .", cancellationToken);
    }

    public async Task<Terminal.Output> SwitchAsync(string targetBranch, CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync($"--ignore-ancestry switch {RemoteUrl}/{targetBranch}", cancellationToken);
    }
    
    private async Task<Terminal.Output> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(WorkingDirectory) == false)
        {
            Directory.CreateDirectory(WorkingDirectory);
        }

        string credentials_str = string.Empty;
        switch (credentials)
        {
            case UserCredentials uc:
                credentials_str = $"--no-auth-cache --username {uc.Username} --password {uc.Password} --trust-server-cert";
                break;
        }

        return await Terminal.ExecuteCommandAsync($"svn --non-interactive {credentials_str} {command}", GenerateTerminalOptions(), cancellationToken);
    }
}
