using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace UnitSimulator;

/// <summary>
/// WebSocket server for real-time communication with the GUI viewer.
/// Provides endpoints for receiving simulation data and sending commands.
/// </summary>
public class WebSocketServer : IDisposable
{
    private readonly SimulatorCore _simulator;
    private readonly HttpListener _httpListener;
    private readonly List<WebSocket> _clients = new();
    private readonly object _clientsLock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _port;
    private bool _isRunning;
    private bool _disposed;
    private Task? _playTask;
    private CancellationTokenSource? _playCts;
    private readonly List<FrameData> _frameHistory = new();
    private readonly object _historyLock = new();
    private const int MaxHistoryFrames = 5000;
    private readonly SessionLogger _sessionLogger;

    private bool IsPlaying => _playTask != null && !_playTask.IsCompleted && _playCts is { IsCancellationRequested: false };

    public WebSocketServer(SimulatorCore simulator, int port = 5000)
    {
        _simulator = simulator;
        _port = port;
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{port}/");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _sessionLogger = new SessionLogger();
        Console.WriteLine($"[WebSocketServer] Session started: {_sessionLogger.SessionId}");
    }

    /// <summary>
    /// Starts the WebSocket server.
    /// </summary>
    public async Task StartAsync()
    {
        if (_isRunning) return;
        
        _isRunning = true;
        _httpListener.Start();
        Console.WriteLine($"[WebSocketServer] Started on http://localhost:{_port}/");
        Console.WriteLine($"[WebSocketServer] WebSocket endpoint: ws://localhost:{_port}/ws");

        while (_isRunning && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                
                if (context.Request.IsWebSocketRequest)
                {
                    _ = HandleWebSocketConnectionAsync(context);
                }
                else
                {
                    // Handle non-WebSocket requests (for health checks, etc.)
                    HandleHttpRequest(context);
                }
            }
            catch when (_cts.Token.IsCancellationRequested)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebSocketServer] Error: {ex.Message}");
            }
        }
    }

    private void HandleHttpRequest(HttpListenerContext context)
    {
        var response = context.Response;
        
        // Add CORS headers
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (context.Request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        if (context.Request.Url?.AbsolutePath == "/health")
        {
            response.StatusCode = 200;
            response.ContentType = "application/json";
            var healthData = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
            response.OutputStream.Write(healthData, 0, healthData.Length);
        }
        else if (context.Request.Url?.AbsolutePath == "/frame")
        {
            // Get current frame data
            try
            {
                if (_simulator.IsInitialized)
                {
                    var frameData = _simulator.GetCurrentFrameData();
                    var json = JsonSerializer.Serialize(new { type = "frame", data = frameData }, _jsonOptions);
                    var data = Encoding.UTF8.GetBytes(json);
                    response.StatusCode = 200;
                    response.ContentType = "application/json";
                    response.OutputStream.Write(data, 0, data.Length);
                }
                else
                {
                    response.StatusCode = 503;
                    var data = Encoding.UTF8.GetBytes("{\"error\":\"Simulator not initialized\"}");
                    response.OutputStream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                var data = Encoding.UTF8.GetBytes($"{{\"error\":\"{ex.Message}\"}}");
                response.OutputStream.Write(data, 0, data.Length);
            }
        }
        else
        {
            response.StatusCode = 404;
        }
        
        response.Close();
    }

    private async Task HandleWebSocketConnectionAsync(HttpListenerContext context)
    {
        WebSocket? webSocket = null;
        
        try
        {
            var wsContext = await context.AcceptWebSocketAsync(null);
            webSocket = wsContext.WebSocket;
            
            lock (_clientsLock)
            {
                _clients.Add(webSocket);
            }
            
            Console.WriteLine($"[WebSocketServer] Client connected. Total clients: {_clients.Count}");

            // Send initial frame data
            if (_simulator.IsInitialized)
            {
                var frameData = _simulator.GetCurrentFrameData();
                await SendToClientAsync(webSocket, "frame", frameData);
            }

            // Handle incoming messages
            var buffer = new byte[4096];
            while (webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleClientMessageAsync(webSocket, message);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebSocketServer] WebSocket error: {ex.Message}");
        }
        finally
        {
            if (webSocket != null)
            {
                lock (_clientsLock)
                {
                    _clients.Remove(webSocket);
                }
                
                Console.WriteLine($"[WebSocketServer] Client disconnected. Total clients: {_clients.Count}");
                
                if (webSocket.State != WebSocketState.Closed)
                {
                    try
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                    catch { }
                }
                
                webSocket.Dispose();
            }
        }
    }

    private async Task HandleClientMessageAsync(WebSocket client, string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("type", out var typeElement))
            {
                await SendToClientAsync(client, "error", "Missing 'type' field");
                return;
            }

            var type = typeElement.GetString();
            
            if (type == "command" && root.TryGetProperty("data", out var dataElement))
            {
                await HandleCommandAsync(client, dataElement);
            }
            else
            {
                await SendToClientAsync(client, "error", $"Unknown message type: {type}");
            }
        }
        catch (JsonException ex)
        {
            await SendToClientAsync(client, "error", $"Invalid JSON: {ex.Message}");
        }
    }

    private async Task HandleCommandAsync(WebSocket client, JsonElement commandData)
    {
        if (!commandData.TryGetProperty("type", out var cmdTypeElement))
        {
            await SendToClientAsync(client, "error", "Missing command type");
            return;
        }

        var cmdType = cmdTypeElement.GetString();
        
        // Log the command
        _sessionLogger.LogCommand(cmdType ?? "unknown", commandData);

        switch (cmdType)
        {
            case "start":
                await StartSimulationAsync();
                break;

            case "stop":
                StopSimulation();
                await SendToClientAsync(client, "command_ack", new { command = "stop", success = true });
                break;

            case "step":
                await StepSimulationAsync();
                break;
            case "step_back":
                await StepBackAsync(client);
                break;
            case "seek":
                if (commandData.TryGetProperty("frameNumber", out var frameElement) &&
                    frameElement.TryGetInt32(out var frameNumber))
                {
                    await SeekAsync(client, frameNumber);
                }
                else
                {
                    await SendToClientAsync(client, "error", "Missing frameNumber for seek command");
                }
                break;

            case "reset":
                ResetSimulation();
                await BroadcastFrameAsync();
                await SendToClientAsync(client, "command_ack", new { command = "reset", success = true });
                break;

            case "move":
                await HandleMoveCommandAsync(client, commandData);
                break;

            case "set_health":
                await HandleSetHealthCommandAsync(client, commandData);
                break;

            case "kill":
                await HandleKillCommandAsync(client, commandData);
                break;

            case "revive":
                await HandleReviveCommandAsync(client, commandData);
                break;

            case "get_session_log":
                await HandleGetSessionLogAsync(client);
                break;

            default:
                await SendToClientAsync(client, "error", $"Unknown command: {cmdType}");
                break;
        }
    }

    private async Task StartSimulationAsync()
    {
        if (_playTask != null && !_playTask.IsCompleted)
        {
            return; // Already playing
        }

        if (!_simulator.IsInitialized)
        {
            _simulator.Initialize();
        }

        if (!_frameHistory.Any())
        {
            var initialFrame = _simulator.GetCurrentFrameData();
            RecordFrame(initialFrame);
        }

        // Use callbacks that broadcast each frame and log events
        var callbacks = new CompositeCallbacks(new WebSocketCallbacks(this), _sessionLogger);

        _playCts?.Cancel();
        _playCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        var token = _playCts.Token;

        _playTask = Task.Run(async () =>
        {
            try
            {
                var frameDelay = TimeSpan.FromMilliseconds(33); // ~30fps playback

                while (!token.IsCancellationRequested)
                {
                    var frameData = _simulator.Step(callbacks);

                    if (frameData.AllWavesCleared || frameData.MaxFramesReached)
                    {
                        await BroadcastAsync("simulation_complete", new
                        {
                            finalFrame = _simulator.CurrentFrame,
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
                Console.WriteLine($"[WebSocketServer] Simulation error: {ex.Message}");
                _sessionLogger.LogError($"Simulation error: {ex.Message}", ex);
            }
            finally
            {
                _playTask = null;
            }
        }, token);
    }

    private void StopSimulation()
    {
        _playCts?.Cancel();
        _simulator.Stop();
    }

    private async Task StepSimulationAsync()
    {
        _playCts?.Cancel();

        if (!_simulator.IsInitialized)
        {
            _simulator.Initialize();
        }

        var callbacks = new CompositeCallbacks(new WebSocketCallbacks(this), _sessionLogger);
        _simulator.Step(callbacks);
    }

    private void ResetSimulation()
    {
        _playCts?.Cancel();
        _simulator.Stop();
        _simulator.Reset();

        lock (_historyLock)
        {
            _frameHistory.Clear();
        }

        // Seed history with the reset state for immediate seeking/backstep
        var initialFrame = _simulator.GetCurrentFrameData();
        RecordFrame(initialFrame);
    }

    private async Task HandleMoveCommandAsync(WebSocket client, JsonElement data)
    {
        if (!TryGetUnitInfo(data, out var unitId, out var faction))
        {
            await SendToClientAsync(client, "error", "Invalid unit info for move command");
            return;
        }

        if (!data.TryGetProperty("position", out var posElement) ||
            !posElement.TryGetProperty("x", out var xElement) ||
            !posElement.TryGetProperty("y", out var yElement))
        {
            await SendToClientAsync(client, "error", "Missing position for move command");
            return;
        }

        var x = xElement.GetSingle();
        var y = yElement.GetSingle();

        var success = _simulator.ModifyUnit(unitId, faction, unit =>
        {
            unit.CurrentDestination = new System.Numerics.Vector2(x, y);
        });

        await SendToClientAsync(client, "command_ack", new { command = "move", success, unitId, x, y });
        
        if (success)
        {
            await BroadcastFrameAsync();
        }
    }

    private async Task HandleSetHealthCommandAsync(WebSocket client, JsonElement data)
    {
        if (!TryGetUnitInfo(data, out var unitId, out var faction))
        {
            await SendToClientAsync(client, "error", "Invalid unit info for set_health command");
            return;
        }

        if (!data.TryGetProperty("health", out var healthElement))
        {
            await SendToClientAsync(client, "error", "Missing health value");
            return;
        }

        var health = healthElement.GetInt32();

        var success = _simulator.ModifyUnit(unitId, faction, unit =>
        {
            unit.HP = health;
            if (health > 0 && unit.IsDead)
            {
                unit.IsDead = false;
            }
            else if (health <= 0)
            {
                unit.IsDead = true;
            }
        });

        await SendToClientAsync(client, "command_ack", new { command = "set_health", success, unitId, health });
        
        if (success)
        {
            await BroadcastFrameAsync();
        }
    }

    private async Task HandleKillCommandAsync(WebSocket client, JsonElement data)
    {
        if (!TryGetUnitInfo(data, out var unitId, out var faction))
        {
            await SendToClientAsync(client, "error", "Invalid unit info for kill command");
            return;
        }

        var success = _simulator.ModifyUnit(unitId, faction, unit =>
        {
            unit.HP = 0;
            unit.IsDead = true;
        });

        await SendToClientAsync(client, "command_ack", new { command = "kill", success, unitId });
        
        if (success)
        {
            await BroadcastFrameAsync();
        }
    }

    private async Task HandleReviveCommandAsync(WebSocket client, JsonElement data)
    {
        if (!TryGetUnitInfo(data, out var unitId, out var faction))
        {
            await SendToClientAsync(client, "error", "Invalid unit info for revive command");
            return;
        }

        var health = 100;
        if (data.TryGetProperty("health", out var healthElement))
        {
            health = healthElement.GetInt32();
        }

        var success = _simulator.ModifyUnit(unitId, faction, unit =>
        {
            unit.HP = health;
            unit.IsDead = false;
        });

        await SendToClientAsync(client, "command_ack", new { command = "revive", success, unitId, health });
        
        if (success)
        {
            await BroadcastFrameAsync();
        }
    }

    private static bool TryGetUnitInfo(JsonElement data, out int unitId, out UnitFaction faction)
    {
        unitId = 0;
        faction = UnitFaction.Friendly;

        if (!data.TryGetProperty("unitId", out var unitIdElement))
            return false;

        unitId = unitIdElement.GetInt32();

        if (!data.TryGetProperty("faction", out var factionElement))
            return false;

        var factionStr = factionElement.GetString();
        faction = factionStr == "Enemy" ? UnitFaction.Enemy : UnitFaction.Friendly;

        return true;
    }

    private async Task HandleGetSessionLogAsync(WebSocket client)
    {
        try
        {
            var summary = _sessionLogger.GetSummary();
            await SendToClientAsync(client, "session_log_summary", summary);
        }
        catch (Exception ex)
        {
            _sessionLogger.LogError($"Failed to get session log: {ex.Message}", ex);
            await SendToClientAsync(client, "error", $"Failed to get session log: {ex.Message}");
        }
    }

    /// <summary>
    /// Broadcasts the current frame data to all connected clients.
    /// </summary>
    public Task BroadcastFrameAsync()
    {
        if (!_simulator.IsInitialized) return Task.CompletedTask;

        var frameData = _simulator.GetCurrentFrameData();
        RecordFrame(frameData);

        return Task.CompletedTask;
    }

    private void RecordFrame(FrameData frameData)
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
        Task.Run(async () => await BroadcastAsync("frame", frameData));
    }

    private FrameData? GetFrameFromHistory(int frameNumber)
    {
        lock (_historyLock)
        {
            return _frameHistory.LastOrDefault(f => f.FrameNumber == frameNumber);
        }
    }

    private FrameData? GetLatestFrame()
    {
        lock (_historyLock)
        {
            return _frameHistory.LastOrDefault();
        }
    }

    private async Task StepBackAsync(WebSocket client)
    {
        _playCts?.Cancel();

        var targetFrame = Math.Max(0, _simulator.CurrentFrame - 1);
        var frame = GetFrameFromHistory(targetFrame);

        if (frame == null)
        {
            await SendToClientAsync(client, "error", $"No frame history for frame {targetFrame}");
            return;
        }

        _simulator.LoadState(frame);
        await BroadcastAsync("frame", frame);
    }

    private async Task SeekAsync(WebSocket client, int targetFrame)
    {
        if (targetFrame < 0)
        {
            await SendToClientAsync(client, "error", "frameNumber must be non-negative");
            return;
        }

        _playCts?.Cancel();

        var historyFrame = GetFrameFromHistory(targetFrame);
        if (historyFrame != null)
        {
            _simulator.LoadState(historyFrame);
            await BroadcastAsync("frame", historyFrame);
            return;
        }

        if (!_simulator.IsInitialized)
        {
            _simulator.Initialize();
        }

        var callbacks = new CompositeCallbacks(new WebSocketCallbacks(this), _sessionLogger);
        FrameData? reached = null;

        while (true)
        {
            var frameData = _simulator.Step(callbacks);
            reached = frameData;

            if (frameData.FrameNumber >= targetFrame ||
                frameData.AllWavesCleared ||
                frameData.MaxFramesReached)
            {
                break;
            }
        }

        if (reached != null)
        {
            await BroadcastAsync("frame", reached);
        }
        else
        {
            await SendToClientAsync(client, "error", $"Unable to seek to frame {targetFrame}");
        }
    }

    /// <summary>
    /// Broadcasts a message to all connected clients.
    /// </summary>
    public async Task BroadcastAsync<T>(string type, T data)
    {
        var message = new { type, data };
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        WebSocket[] clients;
        lock (_clientsLock)
        {
            clients = _clients.Where(c => c.State == WebSocketState.Open).ToArray();
        }

        var tasks = clients.Select(client =>
            client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None)
        );

        await Task.WhenAll(tasks);
    }

    private async Task SendToClientAsync<T>(WebSocket client, string type, T data)
    {
        if (client.State != WebSocketState.Open) return;

        var message = new { type, data };
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;
        
        _isRunning = false;
        _cts.Cancel();
        _playCts?.Cancel();
        _simulator.Stop();
        
        // Save session log when stopping
        try
        {
            _sessionLogger.EndSession("Server stopped");
            var logPath = _sessionLogger.SaveToDefaultLocation();
            Console.WriteLine($"[WebSocketServer] Session log saved: {logPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebSocketServer] Failed to save session log: {ex.Message}");
        }
        
        try
        {
            if (_httpListener.IsListening)
            {
                _httpListener.Stop();
            }
        }
        catch { }
        
        lock (_clientsLock)
        {
            foreach (var client in _clients)
            {
                try
                {
                    if (client.State == WebSocketState.Open)
                    {
                        client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None).Wait(1000);
                    }
                    client.Dispose();
                }
                catch { }
            }
            _clients.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        // Don't save session log in Dispose if it was already saved in Stop
        if (_isRunning)
        {
            Stop();
        }
        
        _cts.Dispose();
        
        try
        {
            _httpListener.Close();
        }
        catch { }
    }

    /// <summary>
    /// Callback implementation that broadcasts frames via WebSocket.
    /// </summary>
    private class WebSocketCallbacks : ISimulatorCallbacks
    {
        private readonly WebSocketServer _server;

        public WebSocketCallbacks(WebSocketServer server)
        {
            _server = server;
        }

        public void OnFrameGenerated(FrameData frameData)
        {
            _server.RecordFrame(frameData);
        }

        public void OnSimulationComplete(int finalFrameNumber, string reason)
        {
            Task.Run(async () => await _server.BroadcastAsync("simulation_complete", new { finalFrame = finalFrameNumber, reason }));
        }

        public void OnStateChanged(string changeDescription)
        {
            Task.Run(async () => await _server.BroadcastAsync("state_change", new { description = changeDescription }));
        }

        public void OnUnitEvent(UnitEventData eventData)
        {
            Task.Run(async () => await _server.BroadcastAsync("unit_event", eventData));
        }
    }

    /// <summary>
    /// Composite callback that forwards events to multiple callback handlers.
    /// </summary>
    private class CompositeCallbacks : ISimulatorCallbacks
    {
        private readonly ISimulatorCallbacks[] _callbacks;

        public CompositeCallbacks(params ISimulatorCallbacks[] callbacks)
        {
            _callbacks = callbacks;
        }

        public void OnFrameGenerated(FrameData frameData)
        {
            foreach (var callback in _callbacks)
            {
                callback.OnFrameGenerated(frameData);
            }
        }

        public void OnSimulationComplete(int finalFrameNumber, string reason)
        {
            foreach (var callback in _callbacks)
            {
                callback.OnSimulationComplete(finalFrameNumber, reason);
            }
        }

        public void OnStateChanged(string changeDescription)
        {
            foreach (var callback in _callbacks)
            {
                callback.OnStateChanged(changeDescription);
            }
        }

        public void OnUnitEvent(UnitEventData eventData)
        {
            foreach (var callback in _callbacks)
            {
                callback.OnUnitEvent(eventData);
            }
        }
    }
}
