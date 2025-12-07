using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

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
    private Task? _simulationTask;

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

            default:
                await SendToClientAsync(client, "error", $"Unknown command: {cmdType}");
                break;
        }
    }

    private async Task StartSimulationAsync()
    {
        if (_simulationTask != null && !_simulationTask.IsCompleted)
        {
            return; // Already running
        }

        if (!_simulator.IsInitialized)
        {
            _simulator.Initialize();
        }

        // Create a WebSocket callback handler that broadcasts frames
        var callbacks = new WebSocketCallbacks(this);

        _simulationTask = Task.Run(() =>
        {
            try
            {
                _simulator.Run(callbacks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebSocketServer] Simulation error: {ex.Message}");
            }
        });
    }

    private void StopSimulation()
    {
        _simulator.Stop();
    }

    private async Task StepSimulationAsync()
    {
        if (!_simulator.IsInitialized)
        {
            _simulator.Initialize();
        }

        var callbacks = new WebSocketCallbacks(this);
        _simulator.Step(callbacks);
        await BroadcastFrameAsync();
    }

    private void ResetSimulation()
    {
        _simulator.Stop();
        _simulator.Reset();
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

    /// <summary>
    /// Broadcasts the current frame data to all connected clients.
    /// </summary>
    public async Task BroadcastFrameAsync()
    {
        if (!_simulator.IsInitialized) return;

        var frameData = _simulator.GetCurrentFrameData();
        await BroadcastAsync("frame", frameData);
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
        _simulator.Stop();
        
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
        
        Stop();
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
        private int _frameCount = 0;

        public WebSocketCallbacks(WebSocketServer server)
        {
            _server = server;
        }

        public void OnFrameGenerated(FrameData frameData)
        {
            // Broadcast every 5th frame to reduce network traffic
            // (simulation runs at 60fps, we broadcast at ~12fps)
            _frameCount++;
            if (_frameCount % 5 == 0)
            {
                Task.Run(async () => await _server.BroadcastAsync("frame", frameData));
            }
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
}
