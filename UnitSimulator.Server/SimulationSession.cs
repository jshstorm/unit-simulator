using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace UnitSimulator;

/// <summary>
/// Represents an isolated simulation session with its own SimulatorCore and connected clients.
/// </summary>
public class SimulationSession : IDisposable
{
    private readonly List<SessionClient> _clients = new();
    private readonly object _clientsLock = new();
    private readonly List<FrameData> _frameHistory = new();
    private readonly object _historyLock = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly CancellationTokenSource _cts = new();

    private Task? _playTask;
    private CancellationTokenSource? _playCts;
    private bool _disposed;

    private const int MaxHistoryFrames = 5000;

    /// <summary>
    /// Unique identifier for this session (UUID format).
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// When this session was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// When the last activity occurred in this session.
    /// </summary>
    public DateTime LastActivityAt { get; private set; }

    /// <summary>
    /// When the session became empty (no clients). Null if clients are connected.
    /// Used for idle session cleanup - sessions are cleaned up based on how long they've been empty,
    /// not based on LastActivityAt.
    /// </summary>
    public DateTime? EmptyAt { get; private set; }

    /// <summary>
    /// The client ID of the session owner.
    /// </summary>
    public string? OwnerClientId { get; private set; }

    /// <summary>
    /// Whether the owner is currently connected.
    /// </summary>
    public bool IsOwnerConnected { get; private set; }

    /// <summary>
    /// The simulation engine for this session.
    /// </summary>
    public SimulatorCore Simulator { get; }

    /// <summary>
    /// Session event logger.
    /// </summary>
    public SessionLogger Logger { get; }

    /// <summary>
    /// Whether simulation is currently playing.
    /// </summary>
    public bool IsPlaying => _playTask != null && !_playTask.IsCompleted && _playCts is { IsCancellationRequested: false };

    /// <summary>
    /// Current number of connected clients.
    /// </summary>
    public int ClientCount
    {
        get
        {
            lock (_clientsLock)
            {
                return _clients.Count;
            }
        }
    }

    /// <summary>
    /// Current simulation state description.
    /// </summary>
    public string SimulatorState
    {
        get
        {
            if (!Simulator.IsInitialized) return "idle";
            if (IsPlaying) return "running";
            if (Simulator.CurrentFrame > 0) return "paused";
            return "idle";
        }
    }

    /// <summary>
    /// Output directory for this session's files.
    /// </summary>
    public string OutputDirectory { get; }

    public SimulationSession(string? sessionId = null)
    {
        SessionId = sessionId ?? Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;

        // Set session-specific output directory
        OutputDirectory = Path.Combine(ServerConstants.OUTPUT_DIRECTORY, SessionId);

        Simulator = new SimulatorCore();

        Logger = new SessionLogger(SessionId, OutputDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Updates the last activity timestamp.
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the role for a given client ID.
    /// </summary>
    public SessionRole GetRoleForClient(string clientId)
    {
        return clientId == OwnerClientId ? SessionRole.Owner : SessionRole.Viewer;
    }

    /// <summary>
    /// Adds a client to this session.
    /// </summary>
    public SessionClient AddClient(WebSocket socket, string clientId)
    {
        var role = SessionRole.Viewer;

        // First client becomes the owner
        if (OwnerClientId == null)
        {
            OwnerClientId = clientId;
            role = SessionRole.Owner;
            IsOwnerConnected = true;
        }
        else if (clientId == OwnerClientId)
        {
            // Owner reconnecting
            role = SessionRole.Owner;
            IsOwnerConnected = true;

            // Notify other clients that owner reconnected
            _ = BroadcastAsync("state_change", new
            {
                state = SimulatorState,
                reason = "owner_reconnected"
            });
        }

        var client = new SessionClient(socket, clientId, role)
        {
            Session = this
        };

        lock (_clientsLock)
        {
            _clients.Add(client);
        }

        // Clear EmptyAt when a client connects
        EmptyAt = null;

        // Initialize simulator if not already initialized so clients can see initial state
        if (!Simulator.IsInitialized)
        {
            Console.WriteLine($"[Session {SessionId[..8]}] Initializing simulator for first client");
            Simulator.Initialize();
            var initialFrame = Simulator.GetCurrentFrameData();
            RecordFrame(initialFrame);
            Console.WriteLine($"[Session {SessionId[..8]}] Initial frame recorded: {initialFrame.FriendlyTowers.Count}F/{initialFrame.EnemyTowers.Count}E towers");
        }

        UpdateActivity();
        Console.WriteLine($"[Session {SessionId[..8]}] Client {clientId[..8]} joined as {role}. Total: {ClientCount}");

        return client;
    }

    /// <summary>
    /// Removes a client from this session.
    /// </summary>
    public void RemoveClient(SessionClient client)
    {
        int remainingClients;
        lock (_clientsLock)
        {
            _clients.Remove(client);
            remainingClients = _clients.Count;
        }

        // If owner disconnected
        if (client.ClientId == OwnerClientId)
        {
            IsOwnerConnected = false;

            // Pause simulation if running
            if (IsPlaying)
            {
                StopSimulation();
            }

            // Notify remaining clients
            _ = BroadcastAsync("state_change", new
            {
                state = "paused",
                reason = "owner_disconnected"
            });

            Console.WriteLine($"[Session {SessionId[..8]}] Owner disconnected. Session paused.");
        }

        // Set EmptyAt when session becomes empty (for idle cleanup)
        // Do NOT update LastActivityAt here - this allows cleanup to happen sooner
        if (remainingClients == 0)
        {
            EmptyAt = DateTime.UtcNow;
            Console.WriteLine($"[Session {SessionId[..8]}] Session is now empty. EmptyAt set.");
        }

        Console.WriteLine($"[Session {SessionId[..8]}] Client {client.ClientId[..8]} left. Total: {remainingClients}");
    }

    /// <summary>
    /// Checks if a command requires owner permission.
    /// </summary>
    public bool RequiresOwnerPermission(string cmdType)
    {
        return cmdType switch
        {
            "start" or "stop" or "step" or "reset" => true,
            "move" or "set_health" or "kill" or "revive" => true,
            "step_back" => true,
            "seek" or "get_session_log" => false,  // Viewer can use these
            _ => true  // Default to requiring owner permission
        };
    }

    /// <summary>
    /// Validates if a client can execute a command.
    /// Returns null if allowed, or an error message if denied.
    /// </summary>
    public string? ValidateCommand(SessionClient client, string cmdType)
    {
        if (!RequiresOwnerPermission(cmdType))
        {
            return null;  // Command allowed for everyone
        }

        if (client.Role != SessionRole.Owner)
        {
            return "This command requires owner permission";
        }

        if (!IsOwnerConnected)
        {
            return "Session owner is disconnected. Commands are disabled.";
        }

        return null;  // Command allowed
    }

    #region Simulation Control

    public async Task StartSimulationAsync()
    {
        if (IsPlaying) return;

        if (!Simulator.IsInitialized)
        {
            Simulator.Initialize();
        }

        if (!_frameHistory.Any())
        {
            var initialFrame = Simulator.GetCurrentFrameData();
            RecordFrame(initialFrame);
        }

        var callbacks = new SessionCallbacks(this);

        _playCts?.Cancel();
        _playCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        var token = _playCts.Token;

        _playTask = Task.Run(async () =>
        {
            try
            {
                var frameDelay = TimeSpan.FromMilliseconds(33); // ~30fps

                while (!token.IsCancellationRequested)
                {
                    var frameData = Simulator.Step(callbacks);

                    if (frameData.AllWavesCleared || frameData.MaxFramesReached)
                    {
                        await BroadcastAsync("simulation_complete", new
                        {
                            finalFrame = Simulator.CurrentFrame,
                            reason = frameData.AllWavesCleared ? "AllWavesCleared" : "MaxFramesReached"
                        });
                        break;
                    }

                    await Task.Delay(frameDelay, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on pause/stop
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Session {SessionId[..8]}] Simulation error: {ex.Message}");
                Logger.LogError($"Simulation error: {ex.Message}", ex);
            }
        }, token);

        UpdateActivity();
    }

    public void StopSimulation()
    {
        _playCts?.Cancel();
        Simulator.Stop();
        UpdateActivity();
    }

    public void StepSimulation()
    {
        _playCts?.Cancel();

        if (!Simulator.IsInitialized)
        {
            Simulator.Initialize();
        }

        var callbacks = new SessionCallbacks(this);
        Simulator.Step(callbacks);
        UpdateActivity();
    }

    public void ResetSimulation()
    {
        Console.WriteLine($"[Session {SessionId[..8]}] ResetSimulation() called");
        _playCts?.Cancel();
        Simulator.Stop();
        Simulator.Reset();

        lock (_historyLock)
        {
            _frameHistory.Clear();
        }

        var initialFrame = Simulator.GetCurrentFrameData();
        RecordFrame(initialFrame);
        Console.WriteLine($"[Session {SessionId[..8]}] Reset complete: {initialFrame.FriendlyTowers.Count}F/{initialFrame.EnemyTowers.Count}E towers");
        UpdateActivity();
    }

    #endregion

    #region Frame History

    public void RecordFrame(FrameData frameData)
    {
        lock (_historyLock)
        {
            _frameHistory.Add(frameData);
            if (_frameHistory.Count > MaxHistoryFrames)
            {
                _frameHistory.RemoveAt(0);
            }
        }

        // Fire-and-forget broadcast
        _ = BroadcastAsync("frame", frameData);
    }

    public FrameData? GetFrameFromHistory(int frameNumber)
    {
        lock (_historyLock)
        {
            return _frameHistory.LastOrDefault(f => f.FrameNumber == frameNumber);
        }
    }

    public FrameData? GetLatestFrame()
    {
        lock (_historyLock)
        {
            return _frameHistory.LastOrDefault();
        }
    }

    #endregion

    #region Broadcasting

    public async Task BroadcastAsync<T>(string type, T data)
    {
        var message = new { type, data };
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        SessionClient[] clients;
        lock (_clientsLock)
        {
            clients = _clients.Where(c => c.IsConnected).ToArray();
        }

        var tasks = clients.Select(client =>
            client.Socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None)
        );

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Session {SessionId[..8]}] Broadcast error: {ex.Message}");
        }
    }

    public async Task SendToClientAsync<T>(SessionClient client, string type, T data)
    {
        if (!client.IsConnected) return;

        var message = new { type, data };
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            await client.Socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Session {SessionId[..8]}] Send error: {ex.Message}");
        }
    }

    #endregion

    #region Session Info

    public SessionInfo GetInfo()
    {
        return new SessionInfo
        {
            SessionId = SessionId,
            CreatedAt = CreatedAt,
            LastActivityAt = LastActivityAt,
            ClientCount = ClientCount,
            SimulatorState = SimulatorState,
            CurrentFrame = Simulator.IsInitialized ? Simulator.CurrentFrame : 0,
            HasOwner = OwnerClientId != null,
            IsOwnerConnected = IsOwnerConnected
        };
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _playCts?.Cancel();

        try
        {
            Logger.EndSession("Session disposed");
            Logger.SaveToDefaultLocation();
        }
        catch { }

        lock (_clientsLock)
        {
            foreach (var client in _clients)
            {
                try
                {
                    if (client.IsConnected)
                    {
                        client.Socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Session closed",
                            CancellationToken.None).Wait(1000);
                    }
                }
                catch { }
            }
            _clients.Clear();
        }

        _cts.Dispose();
        _playCts?.Dispose();
    }

    /// <summary>
    /// Callback implementation for simulation events.
    /// </summary>
    private class SessionCallbacks : ISimulatorCallbacks
    {
        private readonly SimulationSession _session;

        public SessionCallbacks(SimulationSession session)
        {
            _session = session;
        }

        public void OnFrameGenerated(FrameData frameData)
        {
            _session.RecordFrame(frameData);
            _session.Logger.OnFrameGenerated(frameData);
        }

        public void OnSimulationComplete(int finalFrameNumber, string reason)
        {
            _session.Logger.OnSimulationComplete(finalFrameNumber, reason);
        }

        public void OnStateChanged(string changeDescription)
        {
            _ = _session.BroadcastAsync("state_change", new { description = changeDescription });
            _session.Logger.OnStateChanged(changeDescription);
        }

        public void OnUnitEvent(UnitEventData eventData)
        {
            _ = _session.BroadcastAsync("unit_event", eventData);
            _session.Logger.OnUnitEvent(eventData);
        }
    }
}

/// <summary>
/// Session information for API responses.
/// </summary>
public class SessionInfo
{
    public string SessionId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int ClientCount { get; set; }
    public string SimulatorState { get; set; } = "idle";
    public int CurrentFrame { get; set; }
    public bool HasOwner { get; set; }
    public bool IsOwnerConnected { get; set; }
}
