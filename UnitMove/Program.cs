using System.IO;
using System.Numerics;

namespace UnitSimulator;

/// <summary>
/// Main entry point for the Unit Simulator application.
/// 
/// This program supports multiple modes of operation:
/// 1. Default mode: Runs the simulation using the new SimulatorCore engine.
/// 2. Legacy mode (--legacy): Uses the original simulation logic for backward compatibility.
/// 3. Server mode (--server): Starts a WebSocket server for the GUI viewer.
/// 
/// Additional command-line options:
/// - --headless: Run simulation without generating image frames (faster).
/// - --resume &lt;path&gt;: Resume simulation from a saved frame data JSON file.
/// - --port &lt;number&gt;: Port number for the WebSocket server (default: 5000).
/// 
/// For information about extending the simulation with GUI tools, see development_infra_rules.md.
/// </summary>
public class Program
{
    // Legacy ID counters (only used in legacy mode)
    private static int _nextFriendlyId = 0;
    private static int _nextEnemyId = 0;

    /// <summary>
    /// Application entry point.
    /// Parses command line arguments and runs the appropriate simulation mode.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success).</returns>
    public static async Task<int> Main(string[] args)
    {
        // Check for command line options
        bool useLegacy = args.Contains("--legacy");
        bool useServer = args.Contains("--server");
        bool headless = args.Contains("--headless");
        string? resumePath = GetArgumentValue(args, "--resume");
        string? portStr = GetArgumentValue(args, "--port");
        int port = int.TryParse(portStr, out var p) ? p : 5000;

        if (useServer)
        {
            // Run WebSocket server for GUI viewer
            Console.WriteLine($"Starting WebSocket server on port {port}...");
            await RunServerAsync(port);
        }
        else if (useLegacy)
        {
            // Run using the original simulation logic (backward compatibility)
            Console.WriteLine("Running in legacy mode...");
            RunLegacySimulation();
        }
        else if (resumePath != null)
        {
            // Resume simulation from saved frame data
            Console.WriteLine($"Resuming simulation from: {resumePath}");
            RunResumedSimulation(resumePath, headless);
        }
        else
        {
            // Run using the new SimulatorCore engine (default)
            Console.WriteLine("Running with SimulatorCore engine...");
            RunCoreSimulation(headless);
        }

        return 0;
    }

    /// <summary>
    /// Runs the WebSocket server for the GUI viewer.
    /// The server listens for WebSocket connections and provides real-time
    /// simulation data to connected clients.
    /// </summary>
    /// <param name="port">The port number to listen on.</param>
    private static async Task RunServerAsync(int port)
    {
        var simulator = new SimulatorCore();
        simulator.RenderingEnabled = false; // No image rendering in server mode
        simulator.Initialize();

        using var server = new WebSocketServer(simulator, port);

        // Handle Ctrl+C to gracefully shutdown
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nShutting down server...");
            server.Stop();
        };

        Console.WriteLine("Press Ctrl+C to stop the server.");
        Console.WriteLine("Connect the GUI viewer to ws://localhost:" + port + "/ws");

        await server.StartAsync();
    }

    /// <summary>
    /// Runs the simulation using the new SimulatorCore engine.
    /// This is the default mode of operation.
    /// </summary>
    /// <param name="headless">If true, runs without generating image frames.</param>
    private static void RunCoreSimulation(bool headless)
    {
        // Create and initialize the simulator core
        var simulator = new SimulatorCore();
        
        // Configure rendering based on headless flag
        simulator.RenderingEnabled = !headless;
        
        // Initialize the simulation (sets up environment, units, waves)
        simulator.Initialize();

        // Create callbacks - use console logging for development/debugging
        // In production or when integrating with GUI, use custom callbacks
        ISimulatorCallbacks callbacks = new ConsoleLoggingCallbacks();

        // Run the simulation to completion
        simulator.Run(callbacks);
    }

    /// <summary>
    /// Resumes a simulation from a saved frame data JSON file.
    /// This demonstrates the state loading capability of SimulatorCore.
    /// </summary>
    /// <param name="framePath">Path to the saved frame data JSON file.</param>
    /// <param name="headless">If true, runs without generating image frames.</param>
    private static void RunResumedSimulation(string framePath, bool headless)
    {
        // Load the frame data from the specified file
        var frameData = FrameData.LoadFromJsonFile(framePath);
        
        if (frameData == null)
        {
            Console.WriteLine($"Failed to load frame data from: {framePath}");
            return;
        }

        // Create simulator and load the saved state
        var simulator = new SimulatorCore();
        simulator.RenderingEnabled = !headless;
        
        // Load state from the saved frame
        // Note: This sets up the simulation to continue from the saved point
        simulator.LoadState(frameData);

        // Run the simulation from the loaded state
        var callbacks = new ConsoleLoggingCallbacks();
        simulator.Run(callbacks);
    }

    /// <summary>
    /// Gets the value of a command line argument.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="name">The argument name (e.g., "--resume").</param>
    /// <returns>The value following the argument, or null if not found.</returns>
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
    // The code below is preserved to allow running the original simulation logic
    // when needed. Use --legacy flag to run in this mode.
    // ================================================================================

    /// <summary>
    /// Runs the simulation using the original (legacy) logic.
    /// This is preserved for backward compatibility and testing.
    /// </summary>
    private static void RunLegacySimulation()
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
        Console.WriteLine($"ffmpeg -framerate 60 -i {Path.Combine(Constants.OUTPUT_DIRECTORY, "frame_%04d.png")} -c:v libx264 -pix_fmt yuv420p output.mp4");
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
