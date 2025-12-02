
using System.IO;
using System.Numerics;
namespace UnitSimulator;

public class Program
{
    private static int _nextFriendlyId = 0;
    private static int _nextEnemyId = 0;

    public static async Task<int> Main(string[] args)
    {
        // 기존 시뮬레이션 로직 실행
        RunSimulation();
        return 0;
    }

    private static void RunSimulation()
    {
        SetupEnvironment();
        var mainTarget = new Vector2(Constants.IMAGE_WIDTH - 100, Constants.IMAGE_HEIGHT / 2);

        var friendlySquad = CreateFriendlySquad();
        var enemySquad = new List<Unit>();
        var waveManager = new WaveManager(GetNextEnemyId);
        var squadBehavior = new SquadBehavior();
        var enemyBehavior = new EnemyBehavior();
        var renderer = new Renderer();

        waveManager.SpawnNextWave(enemySquad);
        Console.WriteLine("Running final polished simulation...");

        for (int frame = 0; frame < Constants.MAX_FRAMES; frame++)
        {
            if (HandleWaveProgression(enemySquad, friendlySquad, waveManager))
            {
                if (!waveManager.HasMoreWaves)
                {
                    Console.WriteLine($"All enemy waves eliminated at frame {frame}.");
                    break;
                }
            }

            enemyBehavior.UpdateEnemySquad(enemySquad, friendlySquad);
            squadBehavior.UpdateFriendlySquad(friendlySquad, enemySquad, mainTarget);
            renderer.GenerateFrame(frame, friendlySquad, enemySquad, mainTarget, waveManager);
        }

        Console.WriteLine($"\nFinished generating frames in '{Constants.OUTPUT_DIRECTORY}'.");
        Console.WriteLine($"ffmpeg -framerate 60 -i {System.IO.Path.Combine(Constants.OUTPUT_DIRECTORY, "frame_%04d.png")} -c:v libx264 -pix_fmt yuv420p output.mp4");
    }

    private static List<Unit> CreateFriendlySquad()
    {
        return new List<Unit>
        {
            CreateFriendlyUnit(new Vector2(200, Constants.IMAGE_HEIGHT / 2 - 45), UnitRole.Melee),
            CreateFriendlyUnit(new Vector2(200, Constants.IMAGE_HEIGHT / 2 + 45), UnitRole.Melee),
            CreateFriendlyUnit(new Vector2(120, Constants.IMAGE_HEIGHT / 2 - 75), UnitRole.Ranged),
            CreateFriendlyUnit(new Vector2(120, Constants.IMAGE_HEIGHT / 2 + 75), UnitRole.Ranged)
        };
    }

    private static Unit CreateFriendlyUnit(Vector2 position, UnitRole role)
    {
        return new(position, Constants.UNIT_RADIUS, 4.5f, 0.08f, role, Constants.FRIENDLY_HP, GetNextFriendlyId(), UnitFaction.Friendly);
    }

    private static bool HandleWaveProgression(List<Unit> enemySquad, List<Unit> friendlySquad, WaveManager waveManager)
    {
        if (!enemySquad.Any(e => !e.IsDead))
        {
            if (waveManager.TryAdvanceWave())
            {
                friendlySquad.ForEach(f => Array.Fill(f.AttackSlots, null));
                waveManager.SpawnNextWave(enemySquad);
            }
            return true;
        }
        return false;
    }

    private static int GetNextFriendlyId() => ++_nextFriendlyId;
    private static int GetNextEnemyId() => ++_nextEnemyId;

    private static void SetupEnvironment()
    {
        try
        {
            var dirInfo = new DirectoryInfo(Constants.OUTPUT_DIRECTORY);
            if (dirInfo.Exists) dirInfo.Delete(true);
            dirInfo.Create();

            var debugDirPath = Path.Combine(Constants.OUTPUT_DIRECTORY, Constants.DEBUG_SUBDIRECTORY);
            Directory.CreateDirectory(debugDirPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting up output directory: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
