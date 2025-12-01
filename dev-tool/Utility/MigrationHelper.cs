using MySql.Data.MySqlClient;
using Spectre.Console;

namespace AvalonDevTool.Utility;

public static class MigrationHelper
{
    public static void RemoveExistingMigrations((string ContextFullName, string OutputFolder)[] contexts)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, outputFolder) in contexts)
        {
            if (!visited.Add(outputFolder))
            {
                continue;
            }

            var migrationDir = Path.Combine("AvalonServer", "AvalonService", "Migrations", outputFolder);
            if (!Directory.Exists(migrationDir))
            {
                AnsiConsole.MarkupLine($"[yellow]기존 마이그레이션 디렉터리를 찾을 수 없어 건너뜁니다: {migrationDir}[/]");
                continue;
            }

            try
            {
                Directory.Delete(migrationDir, recursive: true);
                AnsiConsole.MarkupLine($"[yellow]삭제됨: {migrationDir}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{migrationDir} 디렉터리 삭제에 실패했습니다: {ex.Message}[/]");
                throw;
            }
        }
    }

    public static void CleanupForeignKeyStatements((string ContextFullName, string OutputFolder)[] contexts, string migrationName, string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? ".";
        foreach (var (_, outputFolder) in contexts)
        {
            var migrationDir = Path.Combine(projectDirectory, "Migrations", outputFolder);
            if (!Directory.Exists(migrationDir))
            {
                continue;
            }

            var pattern = $"*_{migrationName}.cs";
            foreach (var file in Directory.GetFiles(migrationDir, pattern, SearchOption.TopDirectoryOnly))
            {
                RemoveForeignKeyBlocks(file);
            }
        }
    }

    public static void RemoveForeignKeyBlocks(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var result = new List<string>(lines.Length);
        var skipping = false;

        foreach (var line in lines)
        {
            if (!skipping && line.Contains("table.ForeignKey(", StringComparison.Ordinal))
            {
                skipping = true;
                continue;
            }

            if (skipping)
            {
                if (line.TrimEnd().EndsWith(");", StringComparison.Ordinal))
                {
                    skipping = false;
                }
                continue;
            }

            result.Add(line);
        }

        if (result.Count == lines.Length)
        {
            return;
        }

        File.WriteAllLines(filePath, result);
        AnsiConsole.MarkupLine($"[yellow]외래키 제약을 제거했습니다: {filePath}[/]");
    }
    
    public static void ResetDatabase(string connectionString, string targetSchema)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            string dropSchemaQuery = $"DROP DATABASE IF EXISTS `{targetSchema}`;";

            using (var command = new MySqlCommand(dropSchemaQuery, connection))
            {
                try
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine($"스키마 '{targetSchema}' 삭제 완료.");
                }
                catch (MySqlException)
                {
                    Console.WriteLine($"스키마 '{targetSchema}' 삭제 건너뜀 (권한 없음 또는 접근 불가).");
                }
            }
        }
    }   
}