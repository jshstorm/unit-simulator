using System.Runtime.InteropServices;

namespace AvalonDevTool;

internal static class PlatformMisc
{
    public static string TerminalQuote(this string source)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // cmd 기준.
            // 따옴표로 감싸는 것은 지원하지 않음.
            // 텍스트에 쌍따옴표가 있을 경우 두 개 연속 배치하는 규칙
            return '\"' + source.Replace("\"", "\"\"") + '\"';
        }
        else
        {
            // 기타 표준 터미널 기준.
            // 따옴표 및 쌍따옴표로 감싸는 것을 지원함.
            // 따옴표는 순수 텍스트. 변수 사용 시 쌍따옴표로 감싸기.
            if (source.Contains('\'') || source.Contains('$'))
            {
                return '\"' + source.Replace("\"", "\\\"") + '\"';
            }
            else
            {
                return '\'' + source + '\'';
            }
        }
    }
}
