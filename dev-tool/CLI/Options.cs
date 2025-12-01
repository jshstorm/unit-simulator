using CommandLine;

namespace AvalonDevTool.CLI;

internal record Options
{
    /// <summary>
    /// 프로그램 실행 시 즉시 실행할 명령을 인자로 전달합니다. 명령은 디렉토리 분할자('/')로 연결하여 하위 항목을 실행할 수 있습니다.
    /// </summary>
    [Option('c', "command-route", Required = true, HelpText = "Set command route to execute immediately.")]
    public string CommandRoute { get; init; } = string.Empty;
}
