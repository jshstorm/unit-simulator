using System.Linq.Expressions;
using System.Reflection;
using Spectre.Console;

namespace AvalonDevTool.Commands;

internal abstract class CommandGroup
{
    public record CommandDescriptor
    {
        public required CommandAttribute Attribute { get; init; }
        public required string Name { get; init; }
        public required Func<CommandGroup, CancellationToken, ValueTask> Method { get; init; }

        public string DisplayName => Attribute.DisplayName ?? Name;

        public override string ToString()
        {
            return DisplayName;
        }

        public bool Contains(CommandFilter filter)
        {
            return Attribute.Contains(filter);
        }
    }

    public record PromptRecord(CommandGroup Group, CommandDescriptor Descriptor, int Number)
    {
        public readonly CommandGroup Group = Group;
        public readonly CommandDescriptor Descriptor = Descriptor;
        public readonly int Number = Number;

        public override string ToString()
        {
            return $"{Number,2}. {Group.FormatDisplayName(Descriptor.Attribute.Id, Descriptor.DisplayName)}";
        }
    }

    private static readonly Dictionary<Type, CommandDescriptor[]> _descriptors = [];

    static CommandGroup()
    {
        foreach (var type in typeof(CommandGroup).Assembly.GetTypes())
        {
            if (type.IsAssignableTo(typeof(CommandGroup)) && type != typeof(CommandGroup))
            {
                List<CommandDescriptor> descriptors = [];

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    var attribute = method.GetCustomAttribute<CommandAttribute>();
                    if (attribute == null)
                    {
                        continue;
                    }

                    if (method.IsStatic)
                    {
                        throw new InvalidOperationException($"Method is static. Static method not allowed with CommandAttribute.");
                    }

                    var @params = method.GetParameters();
                    if (@params.Length != 1 && @params[0].ParameterType != typeof(CancellationToken))
                    {
                        throw new InvalidOperationException($"Mismatch method signature detected in {type.Name}.{method.Name}. Parameter must be (CancellationToken) or return type is void.");
                    }

                    var cancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                    var @this = Expression.Parameter(typeof(CommandGroup), "this");
                    var call = Expression.Call(Expression.Convert(@this, type), method, cancellationToken);
                    Func<CommandGroup, CancellationToken, ValueTask> lambda;

                    if (method.ReturnType == typeof(ValueTask))
                    {
                        lambda = Expression.Lambda<Func<CommandGroup, CancellationToken, ValueTask>>(call, @this, cancellationToken).Compile();
                    }
                    else if (method.ReturnType.IsAssignableTo(typeof(Task)))
                    {
                        var innerLambda = Expression.Lambda<Func<CommandGroup, CancellationToken, Task>>(call, @this, cancellationToken).Compile();
                        lambda = (@this, p) =>
                        {
                            return new ValueTask(innerLambda(@this, p));
                        };
                    }
                    else
                    {
                        throw new InvalidOperationException($"Mismatch method signature detected in {type.Name}.{method.Name}. Return type must be ValueTask or Task.");
                    }

                    var name = method.Name;
                    if (name.EndsWith("Async"))
                    {
                        name = name[..^5];
                    }

                    descriptors.Add(new CommandDescriptor
                    {
                        Attribute = attribute,
                        Name = name,
                        Method = lambda
                    });
                }

                _descriptors.TryAdd(type, [.. descriptors]);
            }
        }
    }

    public string Name { get; init; }

    private static readonly Stack<CommandGroup> _stack = new();
    private readonly CommandDescriptor[] _commands;
    private readonly Dictionary<string, int> _commandsMap;

    public CommandGroup()
    {
        Name = GetType().Name;
        if (Name.EndsWith("Commands"))
        {
            Name = Name[..^"Commands".Length];
        }

        var descriptors = _descriptors[GetType()];
        _commands = descriptors;
        _commandsMap = descriptors.ToDictionary(p => p.Name, p => Array.IndexOf(descriptors, p));
    }

    public ValueTask ExecuteAsync(string command, CancellationToken cancellationToken = default)
    {
        if (_commandsMap.TryGetValue(command, out var index) == false)
        {
            AnsiConsole.MarkupLine("[red][b]{0}[/]: 올바르지 않은 명령 이름입니다.[/]", command);
            throw TerminateException.User();
        }

        return InternalExecuteAsync(_commands[index], cancellationToken);
    }

    private async ValueTask InternalExecuteAsync(CommandDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        await descriptor.Method(this, cancellationToken);
        if (descriptor.Attribute.Interrupt)
        {
            AnsiConsole.MarkupLine("작업이 [green]완료[/]되었습니다. 계속하려면 [b]아무[/] 키나 입력하세요.");
            await AnsiConsole.Console.Input.ReadKeyAsync(true, cancellationToken);
        }
    }

    public static void Push<T>() where T : CommandGroup, new()
    {
        _stack.Push(new T());
    }

    public static void Pop()
    {
        _stack.Pop();
    }

    public static bool IsLive => _stack.Count > 0;

    public static CommandGroup Current => _stack.Peek();

    public static ValueTask DisplayPromptAsync(CancellationToken cancellationToken)
    {
        var currentGroup = _stack.Peek();
        return currentGroup.InternalDisplayPromptAsync(cancellationToken);
    }

    private async ValueTask InternalDisplayPromptAsync(CancellationToken cancellationToken)
    {
        while (Console.KeyAvailable)
        {
            Console.ReadKey(true);
        }

        var current = AppSettings.Current;
        int index = 0;
        var choices = _commands.Where(p => p.Contains(Filter)).Select(p => new PromptRecord(this, p, ++index));
        var prompt = new SelectionPrompt<PromptRecord>()
            .Title($"{Title} branch->({current.Branch})")
            .PageSize(20)
            .EnableSearch()
            .AddChoices(choices);

        var result = await prompt.ShowAsync(AnsiConsole.Console, cancellationToken);
        await InternalExecuteAsync(result.Descriptor, cancellationToken);
    }

    public static async ValueTask ExecuteRouteAsync(string route, CancellationToken cancellationToken)
    {
        var commands = route.Split('/');
        foreach (var command in commands)
        {
            await Current.ExecuteAsync(command, cancellationToken);
        }
    }

    protected virtual CommandFilter Filter { get; } = CommandFilter.Any;

    public abstract string Title { get; }

    protected virtual string FormatDisplayName(int id, string displayName)
    {
        return displayName;
    }
}
