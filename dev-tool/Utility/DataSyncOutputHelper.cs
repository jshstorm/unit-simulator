using Spectre.Console;

namespace AvalonDevTool;

public class FileList
{
    public string? branch { get; set; }
    public string? branch_hash { get; set; }
    public string? gamedata { get; set; }
    public string? bundle { get; set; }
    public List<string>? gamedata_list { get; set; }
}

internal static class DataSyncOutputHelper
{
    public static void DeployToServer()
    {
        string sourceDir = "avalon_builds/protoTool/out";
        string targetDir = "AvalonServer/AvalonService/protoData";
        Directory.CreateDirectory(targetDir);

        foreach (var pbSource in Directory.GetFiles(sourceDir, "*.bytes", SearchOption.TopDirectoryOnly))
        {
            var pbDest = Path.Combine(targetDir, Path.GetFileName(pbSource));
            File.Copy(pbSource, pbDest, true);
        }
    }
    
    public static void DeployToClient()
    {
        string sourceDir = "avalon_builds/protoTool/out";
        string targetDir = "AvalonClient/Assets/Game/RemoteResources/GameData";
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*.bytes", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(file);
            var dest = Path.Combine(targetDir, fileName);
            AnsiConsole.MarkupLine($"[white]{fileName} 파일을 복사합니다.[/]");
            File.Copy(file, dest, true);
        }

        string jsonFile = Path.Combine(sourceDir, "filelist.json");
        if (File.Exists(jsonFile))
        {
            var destJson = Path.Combine(targetDir, "filelist.json");
            File.Copy(jsonFile, destJson, true);
        }

        DeployToClientDevelopment();
    }

    private static void DeployToClientDevelopment()
    {
        AnsiConsole.MarkupLine($"[green]Development/GameData에 파일을 배포합니다.[/]");

        string sourceDir = "avalon_builds/protoTool/out";
        string targetDir = "AvalonClient/Development/GameData";
        Directory.CreateDirectory(targetDir);
        
        string[] extensions = { "*.json" };

        int fileCount = 0;
        foreach (var ext in extensions)
        {
            var files = Directory.GetFiles(targetDir, ext, SearchOption.TopDirectoryOnly);
            fileCount += files.Length;
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
        AnsiConsole.MarkupLine($"[green]{fileCount}개의 파일을 삭제했습니다.[/]");

        foreach (var file in Directory.GetFiles(sourceDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(file);
            var dest = Path.Combine(targetDir, fileName);
            File.Copy(file, dest, true);
        }
    }

    private static readonly string SourceDir = "avalon_builds/protoTool/out";
    private static readonly string ClientGameDataDir = "AvalonClient/Assets/Game/RemoteResources/GameData";
    private static readonly string EmbeddedDir = "AvalonClient/Assets/Game/Resources/EmbeddedGameData";
    private static readonly string DevelopmentGameDataDir = "AvalonClient/Development/GameData";

    public static void CopyTextBytes()
    {
        // \*.bytes 중 파일명에 text가 포함된 것만 GameData로 복사
        var copied = CopyFiles(SourceDir, "*.bytes", ClientGameDataDir, fileName => fileName.Contains("game-avalon-text"));
        AnsiConsole.MarkupLine($"[green]{copied}개의 text .bytes 파일을 {ClientGameDataDir}로 복사했습니다.[/]");

        copied = CopyFiles(SourceDir, "*.json", DevelopmentGameDataDir, fileName => fileName.Contains("game-avalon-text"));
        AnsiConsole.MarkupLine($"[green]{copied}개의 text .json 파일을 {DevelopmentGameDataDir}로 복사했습니다.[/]");

        // text\_filelist.json 복사
        var fileListPath = Path.Combine(SourceDir, "text_filelist.json");
        if (File.Exists(fileListPath))
        {
            var dest = Path.Combine(ClientGameDataDir, "text_filelist.json");
            Directory.CreateDirectory(ClientGameDataDir);
            File.Copy(fileListPath, dest, true);
        }

        // 임베디드 파일 복사
        CopyTextFilesToEmbedded();
    }

    private static void CopyTextFilesToEmbedded()
    {
        // \*embedded\*.bytes 중 파일명에 text가 포함된 것만 EmbeddedGameData로 복사
        CopyFiles(SourceDir, "*embedded*.bytes", EmbeddedDir, fileName => fileName.Contains("text"));

        // text\_uid.bytes 복사(존재 확인)
        var uidSrc = Path.Combine(SourceDir, "text_uid.bytes");
        if (File.Exists(uidSrc))
        {
            Directory.CreateDirectory(EmbeddedDir);
            var uidDest = Path.Combine(EmbeddedDir, "text_uid.bytes");
            File.Copy(uidSrc, uidDest, true);
        }
    }

    // 공통 복사 헬퍼: 대상 디렉토리 보장, 필터 적용, 덮어쓰기
    private static int CopyFiles(string sourceDir, string searchPattern, string targetDir, Func<string, bool>? fileNameFilter = null)
    {
        Directory.CreateDirectory(targetDir);
        var copied = 0;

        foreach (var path in Directory.EnumerateFiles(sourceDir, searchPattern, SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(path);
            if (fileNameFilter != null && !fileNameFilter(fileName))
                continue;

            var dest = Path.Combine(targetDir, fileName);
            File.Copy(path, dest, true);
            copied++;
        }

        return copied;
    }
}
