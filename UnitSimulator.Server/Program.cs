using System.IO;
using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// Main entry point for the Unit Simulator application.
///
/// This program supports multiple modes of operation:
/// 1. Default mode: Runs the simulation using the SimulatorCore engine with Command Queue.
/// 2. Legacy mode (--legacy): Uses the original simulation logic for backward compatibility.
/// 3. Server mode (--server): Starts a WebSocket server for the GUI viewer.
///
/// Additional command-line options:
/// - --headless: Run simulation without generating image frames (faster).
/// - --resume &lt;path&gt;: Resume simulation from a saved frame data JSON file.
/// - --port &lt;number&gt;: Port number for the WebSocket server (default: 5000).
/// </summary>
public class Program
{
    // Legacy ID counters (only used in legacy mode)
    private static int _nextFriendlyId = 0;
    private static int _nextEnemyId = 0;

    public static async Task<int> Main(string[] args)
    {
        bool useLegacy = args.Contains("--legacy");
        bool useServer = args.Contains("--server");
        bool headless = args.Contains("--headless");
        string? resumePath = GetArgumentValue(args, "--resume");
        string? portStr = GetArgumentValue(args, "--port");
        int port = int.TryParse(portStr, out var p) ? p : 5000;

        if (useServer)
        {
            Console.WriteLine($"Starting WebSocket server on port {port}...");
            await RunServerAsync(port);
        }
        else if (useLegacy)
        {
            Console.WriteLine("Running in legacy mode...");
            RunLegacySimulation(headless);
        }
        else if (resumePath != null)
        {
            Console.WriteLine($"Resuming simulation from: {resumePath}");
            RunResumedSimulation(resumePath, headless);
        }
        else
        {
            Console.WriteLine("Running with SimulatorCore engine...");
            RunCoreSimulation(headless);
        }

        return 0;
    }

    private static async Task RunServerAsync(int port)
    {
        Console.WriteLine("[Server] Starting in WebSocket server mode...");

        var sessionOptions = new SessionManagerOptions
        {
            MaxSessions = 100,
            IdleTimeout = TimeSpan.FromMinutes(5),  // 5 minutes for dev, 30 for prod
            CleanupIntervalMs = 60_000  // 1 minute
        };

        using var server = new WebSocketServer(port, sessionOptions);

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n[Server] Shutting down...");
            server.Stop();
        };

        Console.WriteLine("[Server] Ready. Waiting for client connections...");
        Console.WriteLine($"API endpoints:");
        Console.WriteLine($"  - GET  http://localhost:{port}/sessions     (list sessions)");
        Console.WriteLine($"  - POST http://localhost:{port}/sessions     (create session)");
        Console.WriteLine($"  - GET  http://localhost:{port}/sessions/{{id}} (session info)");
        Console.WriteLine($"  - GET  http://localhost:{port}/data/files   (list data files)");
        Console.WriteLine($"  - GET  http://localhost:{port}/data/file?path=... (read data file)");
        Console.WriteLine($"  - PUT  http://localhost:{port}/data/file?path=... (write data file)");
        Console.WriteLine($"  - DELETE http://localhost:{port}/data/file?path=... (delete data file)");

        await server.StartAsync();
    }

    /// <summary>
    /// Runs the simulation using the new Command Queue-based architecture.
    /// </summary>
    private static void RunCoreSimulation(bool headless)
    {
        var simulator = new SimulatorCore();
        var waveManager = new WaveManager();
        Renderer? renderer = null;

        if (!headless)
        {
            renderer = new Renderer(ServerConstants.OUTPUT_DIRECTORY);
            renderer.SetupOutputDirectory();
        }

        simulator.Initialize();

        // Spawn first wave via commands
        var firstWaveCommands = waveManager.SpawnFirstWave(0);
        simulator.EnqueueCommands(firstWaveCommands);
        simulator.CurrentWave = waveManager.CurrentWave;
        simulator.HasMoreWaves = waveManager.HasMoreWaves;

        ISimulatorCallbacks callbacks = new ConsoleLoggingCallbacks();

        Console.WriteLine("Running simulation with Command Queue...");

        while (simulator.CurrentFrame < GameConstants.MAX_FRAMES)
        {
            var frameData = simulator.Step(callbacks);

            // Render frame if not headless
            if (renderer != null)
            {
                renderer.GenerateFrame(frameData, simulator.MainTarget);
            }

            // Handle wave progression
            if (simulator.AllEnemiesDead && waveManager.HasMoreWaves)
            {
                if (waveManager.TryAdvanceWave())
                {
                    simulator.ClearFriendlyAttackSlots();
                    var nextWaveCommands = waveManager.GetWaveCommands(waveManager.CurrentWave, simulator.CurrentFrame);
                    simulator.EnqueueCommands(nextWaveCommands);
                    simulator.CurrentWave = waveManager.CurrentWave;
                    simulator.HasMoreWaves = waveManager.HasMoreWaves;
                    callbacks.OnStateChanged($"Wave {waveManager.CurrentWave} spawned");
                }
            }

            if (frameData.AllWavesCleared)
            {
                callbacks.OnSimulationComplete(simulator.CurrentFrame, "AllWavesCleared");
                Console.WriteLine($"All enemy waves eliminated at frame {simulator.CurrentFrame}.");
                break;
            }

            if (frameData.MaxFramesReached)
            {
                callbacks.OnSimulationComplete(simulator.CurrentFrame, "MaxFramesReached");
                Console.WriteLine($"Maximum frames reached.");
                break;
            }
        }

        Console.WriteLine($"\nFinished generating frames in '{ServerConstants.OUTPUT_DIRECTORY}'.");
        Console.WriteLine($"ffmpeg -framerate 60 -i {Path.Combine(ServerConstants.OUTPUT_DIRECTORY, "frame_%04d.png")} -c:v libx264 -pix_fmt yuv420p output.mp4");
    }

    private static void RunResumedSimulation(string framePath, bool headless)
    {
        var frameData = FrameData.LoadFromJsonFile(framePath);

        if (frameData == null)
        {
            Console.WriteLine($"Failed to load frame data from: {framePath}");
            return;
        }

        var simulator = new SimulatorCore();
        var waveManager = new WaveManager();
        Renderer? renderer = null;

        if (!headless)
        {
            renderer = new Renderer(ServerConstants.OUTPUT_DIRECTORY);
            renderer.SetupOutputDirectory(clearExisting: false);
        }

        simulator.LoadState(frameData);
        waveManager.SetWave(frameData.CurrentWave);

        var callbacks = new ConsoleLoggingCallbacks();

        while (simulator.CurrentFrame < GameConstants.MAX_FRAMES)
        {
            var frame = simulator.Step(callbacks);

            if (renderer != null)
            {
                renderer.GenerateFrame(frame, simulator.MainTarget);
            }

            if (simulator.AllEnemiesDead && waveManager.HasMoreWaves)
            {
                if (waveManager.TryAdvanceWave())
                {
                    simulator.ClearFriendlyAttackSlots();
                    var nextWaveCommands = waveManager.GetWaveCommands(waveManager.CurrentWave, simulator.CurrentFrame);
                    simulator.EnqueueCommands(nextWaveCommands);
                    simulator.CurrentWave = waveManager.CurrentWave;
                    simulator.HasMoreWaves = waveManager.HasMoreWaves;
                }
            }

            if (frame.AllWavesCleared || frame.MaxFramesReached)
                break;
        }
    }

    private static string? GetArgumentValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    // ================================================================================
    // Legacy Simulation Code (for backward compatibility)
    // ================================================================================

    private static void RunLegacySimulation(bool headless)
    {
        Renderer? renderer = null;

        if (!headless)
        {
            renderer = new Renderer(ServerConstants.OUTPUT_DIRECTORY);
            renderer.SetupOutputDirectory();
        }

        var mainTarget = new Vector2(GameConstants.SIMULATION_WIDTH - 100, GameConstants.SIMULATION_HEIGHT / 2);

        var friendlySquad = CreateFriendlySquad();
        var enemySquad = new List<Unit>();
        var waveManager = new WaveManager();
        var squadBehavior = new SquadBehavior();
        var enemyBehavior = new EnemyBehavior();
        var pathfindingSim = new SimulatorCore();
        pathfindingSim.Initialize();

        // Spawn first wave
        SpawnWaveUnits(enemySquad, waveManager.SpawnFirstWave(0));

        Console.WriteLine("Running legacy simulation...");

        for (int frame = 0; frame < GameConstants.MAX_FRAMES; frame++)
        {
            if (HandleLegacyWaveProgression(enemySquad, friendlySquad, waveManager, frame))
            {
                if (!waveManager.HasMoreWaves)
                {
                    Console.WriteLine($"All enemy waves eliminated at frame {frame}.");
                    break;
                }
            }

            var events = new FrameEvents();
            var friendlyTowers = new List<Tower>();
            var enemyTowers = new List<Tower>();
            enemyBehavior.UpdateEnemySquad(pathfindingSim, enemySquad, friendlySquad, friendlyTowers, events);
            squadBehavior.UpdateFriendlySquad(pathfindingSim, friendlySquad, enemySquad, enemyTowers, mainTarget, events);
            pathfindingSim.ApplyFrameEvents(events);

            if (renderer != null)
            {
                renderer.GenerateFrame(frame, friendlySquad, enemySquad, mainTarget, waveManager.CurrentWave);
            }
        }

        Console.WriteLine($"\nFinished generating frames in '{ServerConstants.OUTPUT_DIRECTORY}'.");
        Console.WriteLine($"ffmpeg -framerate 60 -i {Path.Combine(ServerConstants.OUTPUT_DIRECTORY, "frame_%04d.png")} -c:v libx264 -pix_fmt yuv420p output.mp4");
    }

    private static void SpawnWaveUnits(List<Unit> enemySquad, IEnumerable<SpawnUnitCommand> commands)
    {
        enemySquad.Clear();
        foreach (var cmd in commands)
        {
            var unit = new Unit(
                cmd.Position,
                GameConstants.UNIT_RADIUS,
                cmd.Speed ?? 4.0f,
                cmd.TurnSpeed ?? 0.1f,
                cmd.Role,
                cmd.HP ?? GameConstants.ENEMY_HP,
                GetNextEnemyId(),
                cmd.Faction
            );
            enemySquad.Add(unit);
        }
    }

    private static List<Unit> CreateFriendlySquad()
    {
        int h = GameConstants.SIMULATION_HEIGHT;
        return new List<Unit>
        {
            CreateFriendlyUnit(new Vector2(200, h / 2 - 45), UnitRole.Melee),
            CreateFriendlyUnit(new Vector2(200, h / 2 + 45), UnitRole.Melee),
            CreateFriendlyUnit(new Vector2(120, h / 2 - 75), UnitRole.Ranged),
            CreateFriendlyUnit(new Vector2(120, h / 2 + 75), UnitRole.Ranged)
        };
    }

    private static Unit CreateFriendlyUnit(Vector2 position, UnitRole role)
    {
        return new(position, GameConstants.UNIT_RADIUS, 4.5f, 0.08f, role, GameConstants.FRIENDLY_HP, GetNextFriendlyId(), UnitFaction.Friendly);
    }

    private static bool HandleLegacyWaveProgression(List<Unit> enemySquad, List<Unit> friendlySquad, WaveManager waveManager, int frame)
    {
        if (!enemySquad.Any(e => !e.IsDead))
        {
            if (waveManager.TryAdvanceWave())
            {
                friendlySquad.ForEach(f => Array.Fill(f.AttackSlots, null));
                SpawnWaveUnits(enemySquad, waveManager.GetWaveCommands(waveManager.CurrentWave, frame));
            }
            return true;
        }
        return false;
    }

    private static int GetNextFriendlyId() => ++_nextFriendlyId;
    private static int GetNextEnemyId() => ++_nextEnemyId;
}
