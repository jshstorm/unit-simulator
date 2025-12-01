namespace AvalonDevTool;

internal class TerminateException(int ExitCode) : Exception
{
    public readonly int ExitCode = ExitCode;

    public const int EC_User = 1;
    public const int EC_Internal = 2;
    public const int EC_Abort = 3;

    public static Exception User() => new TerminateException(EC_User);
    public static Exception Internal() => new TerminateException(EC_Internal);
    public static Exception Abort() => new TerminateException(EC_Abort);
}
