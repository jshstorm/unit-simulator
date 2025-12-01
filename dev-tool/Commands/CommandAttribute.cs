namespace AvalonDevTool.Commands;

[AttributeUsage(AttributeTargets.Method)]
internal class CommandAttribute(params CommandFilter[] filters) : Attribute
{
    public readonly IReadOnlyList<CommandFilter> Filters = filters;

    public string? DisplayName { get; init; }

    public int Id { get; init; } = -1;

    public bool Interrupt { get; init; }

    private readonly bool _any = filters.Length == 0 || filters.Contains(CommandFilter.Any);

    public bool Contains(CommandFilter targetGroup)
    {
        if (_any)
        {
            return true;
        }

        if (targetGroup == CommandFilter.Any)
        {
            return true;
        }

        return Filters.Contains(targetGroup);
    }
}
