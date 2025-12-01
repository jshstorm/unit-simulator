namespace AvalonDevTool.Commands;

internal class ConfigCommands : CommandGroup
{
    public override string Title => "설정";

    [Command(CommandFilter.Any, Id = 1)]
    public async ValueTask InsteadRebaseAsync(CancellationToken cancellationToken)
    {
        var current = AppSettings.Current;
        await AppSettings.ModifyAsync(current with
        {
            Git = current.Git with
            {
                InsteadRebase = !current.Git.InsteadRebase
            }
        }, cancellationToken);
    }

    [Command(CommandFilter.Any, Id = 2)]
    public async ValueTask AutostashAsync(CancellationToken cancellationToken)
    {
        var current = AppSettings.Current;
        await AppSettings.ModifyAsync(current with
        {
            Git = current.Git with
            {
                Autostash = !current.Git.Autostash
            }
        }, cancellationToken);
    }

    [Command(CommandFilter.Any)]
    public ValueTask Return(CancellationToken cancellationToken)
    {
        Pop();
        return ValueTask.CompletedTask;
    }

    protected override string FormatDisplayName(int id, string displayName)
    {
        switch (id)
        {
            case 1:
                return $"{displayName} = {AppSettings.Current.Git.InsteadRebase}";
            case 2:
                return $"{displayName} = {AppSettings.Current.Git.Autostash}";
        }

        return base.FormatDisplayName(id, displayName);
    }
}
