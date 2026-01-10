using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UnitSimulator.Server.Handlers;

namespace UnitSimulator;

/// <summary>
/// WebSocket server for real-time communication with the GUI viewer.
/// Supports multiple isolated simulation sessions.
/// </summary>
public class WebSocketServer : IDisposable
{
    private readonly SessionManager _sessionManager;
    private readonly HttpListener _httpListener;
    private readonly CancellationTokenSource _cts = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _port;
    private readonly string _dataRoot;
    private bool _isRunning;
    private bool _disposed;

    public WebSocketServer(int port = 5000, SessionManagerOptions? sessionOptions = null)
    {
        _port = port;
        _sessionManager = new SessionManager(sessionOptions);
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{port}/");

        _dataRoot = Path.Combine(Directory.GetCurrentDirectory(), "data", "processed");

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
        Console.WriteLine($"[WebSocketServer] WebSocket endpoints:");
        Console.WriteLine($"  - ws://localhost:{_port}/ws          (create new session)");
        Console.WriteLine($"  - ws://localhost:{_port}/ws/new      (create new session)");
        Console.WriteLine($"  - ws://localhost:{_port}/ws/{{id}}     (join existing session)");

        while (_isRunning && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    _ = HandleWebSocketRequestAsync(context);
                }
                else
                {
                    HandleHttpRequest(context);
                }
            }
            catch when (_cts.Token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebSocketServer] Error: {ex.Message}");
            }
        }
    }

    #region HTTP Request Handling

    private void HandleHttpRequest(HttpListenerContext context)
    {
        var response = context.Response;
        var path = context.Request.Url?.AbsolutePath ?? "";

        // Add CORS headers
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (context.Request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        try
        {
            if (path == "/health")
            {
                RespondJson(response, 200, new { status = "ok", sessions = _sessionManager.SessionCount });
            }
            else if (path == "/sessions")
            {
                HandleSessionsRequest(context);
            }
            else if (path.StartsWith("/sessions/"))
            {
                HandleSessionRequest(context, path);
            }
            else if (path == "/data/files")
            {
                HandleDataFilesRequest(context);
            }
            else if (path == "/data/file")
            {
                HandleDataFileRequest(context);
            }
            else
            {
                response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            RespondJson(response, 500, new { error = ex.Message });
        }

        response.Close();
    }

    private void HandleSessionsRequest(HttpListenerContext context)
    {
        var response = context.Response;

        if (context.Request.HttpMethod == "GET")
        {
            // List all sessions
            var sessions = _sessionManager.ListSessions().ToList();
            RespondJson(response, 200, new { sessions, count = sessions.Count });
        }
        else if (context.Request.HttpMethod == "POST")
        {
            // Create new session (without WebSocket connection)
            var session = _sessionManager.CreateSession();
            if (session != null)
            {
                RespondJson(response, 201, new { sessionId = session.SessionId, message = "Session created" });
            }
            else
            {
                RespondJson(response, 503, new { error = "Max sessions reached" });
            }
        }
        else
        {
            response.StatusCode = 405;
        }
    }

    private void HandleDataFilesRequest(HttpListenerContext context)
    {
        var response = context.Response;

        if (context.Request.HttpMethod != "GET")
        {
            response.StatusCode = 405;
            return;
        }

        if (!Directory.Exists(_dataRoot))
        {
            RespondJson(response, 200, new { root = _dataRoot, files = Array.Empty<object>() });
            return;
        }

        var files = Directory.EnumerateFiles(_dataRoot, "*.json", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .Select(info =>
            {
                var relative = Path.GetRelativePath(_dataRoot, info.FullName).Replace('\\', '/');
                return new
                {
                    path = relative,
                    size = info.Length,
                    modifiedUtc = info.LastWriteTimeUtc,
                    etag = ComputeEtag(File.ReadAllText(info.FullName))
                };
            })
            .ToList();

        RespondJson(response, 200, new { root = _dataRoot, files });
    }

    private void HandleDataFileRequest(HttpListenerContext context)
    {
        var response = context.Response;
        var query = ParseQuery(context.Request.Url?.Query ?? "");
        query.TryGetValue("path", out var relativePath);
        var fullPath = ResolveDataPath(relativePath);

        if (fullPath == null)
        {
            RespondJson(response, 400, new { error = "Invalid path." });
            return;
        }

        if (context.Request.HttpMethod == "GET")
        {
            if (!File.Exists(fullPath))
            {
                RespondJson(response, 404, new { error = "File not found." });
                return;
            }

            var content = File.ReadAllText(fullPath);
            RespondJson(response, 200, new
            {
                path = relativePath,
                content,
                etag = ComputeEtag(content),
                modifiedUtc = File.GetLastWriteTimeUtc(fullPath)
            });
            return;
        }

        if (context.Request.HttpMethod == "PUT")
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
            var body = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(body))
            {
                RespondJson(response, 400, new { error = "Empty request body." });
                return;
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("content", out var contentElement))
            {
                RespondJson(response, 400, new { error = "Missing content." });
                return;
            }

            var content = contentElement.GetString() ?? "";
            string? expectedEtag = null;
            if (doc.RootElement.TryGetProperty("etag", out var etagElement))
            {
                expectedEtag = etagElement.GetString();
            }

            try
            {
                JsonDocument.Parse(content);
            }
            catch (JsonException ex)
            {
                RespondJson(response, 400, new { error = $"Invalid JSON content: {ex.Message}" });
                return;
            }

            if (File.Exists(fullPath) && expectedEtag != null)
            {
                var currentContent = File.ReadAllText(fullPath);
                var currentEtag = ComputeEtag(currentContent);
                if (!string.Equals(currentEtag, expectedEtag, StringComparison.Ordinal))
                {
                    RespondJson(response, 409, new { error = "Conflict detected.", etag = currentEtag });
                    return;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
            RespondJson(response, 200, new { path = relativePath, etag = ComputeEtag(content) });
            return;
        }

        if (context.Request.HttpMethod == "DELETE")
        {
            if (!File.Exists(fullPath))
            {
                RespondJson(response, 404, new { error = "File not found." });
                return;
            }

            File.Delete(fullPath);
            RespondJson(response, 200, new { path = relativePath });
            return;
        }

        response.StatusCode = 405;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        var trimmed = query.StartsWith("?") ? query[1..] : query;
        var pairs = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
            result[key] = value;
        }

        return result;
    }

    private string? ResolveDataPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(_dataRoot, relativePath));
        if (!fullPath.StartsWith(_dataRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }

    private static string ComputeEtag(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private void HandleSessionRequest(HttpListenerContext context, string path)
    {
        var response = context.Response;
        var sessionId = path.Substring("/sessions/".Length);

        if (string.IsNullOrEmpty(sessionId))
        {
            response.StatusCode = 400;
            return;
        }

        var session = _sessionManager.GetSession(sessionId);

        if (context.Request.HttpMethod == "GET")
        {
            if (session != null)
            {
                RespondJson(response, 200, session.GetInfo());
            }
            else
            {
                RespondJson(response, 404, new { error = "Session not found" });
            }
        }
        else if (context.Request.HttpMethod == "DELETE")
        {
            if (_sessionManager.RemoveSession(sessionId))
            {
                RespondJson(response, 200, new { message = "Session deleted" });
            }
            else
            {
                RespondJson(response, 404, new { error = "Session not found" });
            }
        }
        else
        {
            response.StatusCode = 405;
        }
    }

    private void RespondJson<T>(HttpListenerResponse response, int statusCode, T data)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        response.OutputStream.Write(bytes, 0, bytes.Length);
    }

    #endregion

    #region WebSocket Request Handling

    private async Task HandleWebSocketRequestAsync(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        string? requestedSessionId = null;
        bool createNew = false;

        // Parse WebSocket path
        // /ws or /ws/new -> create new session
        // /ws/{sessionId} -> join existing session
        if (path == "/ws" || path == "/ws/new")
        {
            createNew = true;
        }
        else if (path.StartsWith("/ws/"))
        {
            requestedSessionId = path.Substring("/ws/".Length);
            if (string.IsNullOrEmpty(requestedSessionId))
            {
                createNew = true;
            }
        }
        else
        {
            // Invalid path
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        WebSocket? webSocket = null;

        try
        {
            Console.WriteLine($"[WebSocketServer] New WebSocket connection from {context.Request.RemoteEndPoint}");
            var wsContext = await context.AcceptWebSocketAsync(null);
            webSocket = wsContext.WebSocket;

            // Wait for identify message
            var clientId = await WaitForIdentifyAsync(webSocket);
            if (clientId == null)
            {
                Console.WriteLine("[WebSocketServer] Client failed to identify, closing connection");
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.PolicyViolation,
                    "Identify message required",
                    CancellationToken.None);
                return;
            }
            Console.WriteLine($"[WebSocketServer] Client identified: {clientId[..Math.Min(8, clientId.Length)]}");

            // Get or create session
            SimulationSession? session;
            if (createNew)
            {
                Console.WriteLine("[WebSocketServer] Creating new session...");
                session = _sessionManager.CreateSession();
                if (session == null)
                {
                    Console.WriteLine("[WebSocketServer] Failed to create session: max sessions reached");
                    await SendMessageAsync(webSocket, "error", new { message = "Max sessions reached" });
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.PolicyViolation,
                        "Max sessions reached",
                        CancellationToken.None);
                    return;
                }
            }
            else
            {
                Console.WriteLine($"[WebSocketServer] Joining existing session: {requestedSessionId![..Math.Min(8, requestedSessionId.Length)]}");
                session = _sessionManager.GetSession(requestedSessionId!);
                if (session == null)
                {
                    Console.WriteLine($"[WebSocketServer] Session not found: {requestedSessionId}");
                    await SendMessageAsync(webSocket, "error", new { message = "Session not found", sessionId = requestedSessionId });
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.PolicyViolation,
                        "Session not found",
                        CancellationToken.None);
                    return;
                }
            }

            // Add client to session (this triggers simulator initialization if needed)
            var client = session.AddClient(webSocket, clientId);

            // Send session info to client
            Console.WriteLine($"[WebSocketServer] Sending session_joined to client {clientId[..Math.Min(8, clientId.Length)]}");
            await SendMessageAsync(webSocket, "session_joined", new
            {
                sessionId = session.SessionId,
                role = client.Role.ToString().ToLower(),
                simulatorState = session.SimulatorState,
                currentFrame = session.Simulator.IsInitialized ? session.Simulator.CurrentFrame : 0,
                clientCount = session.ClientCount
            });

            // Send current frame if available
            if (session.Simulator.IsInitialized)
            {
                var frameData = session.Simulator.GetCurrentFrameData();
                Console.WriteLine($"[WebSocketServer] Sending initial frame: {frameData.FriendlyTowers.Count}F/{frameData.EnemyTowers.Count}E towers, {frameData.FriendlyUnits.Count}F/{frameData.EnemyUnits.Count}E units");
                await session.SendToClientAsync(client, "frame", frameData);
            }
            else
            {
                Console.WriteLine("[WebSocketServer] WARNING: Simulator not initialized after AddClient!");
            }

            // Handle client messages
            Console.WriteLine($"[WebSocketServer] Client {clientId[..Math.Min(8, clientId.Length)]} ready, entering message loop");
            await HandleClientConnectionAsync(client, session);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebSocketServer] WebSocket error: {ex.Message}");
            Console.WriteLine($"[WebSocketServer] Stack trace: {ex.StackTrace}");
        }
        finally
        {
            webSocket?.Dispose();
        }
    }

    private async Task<string?> WaitForIdentifyAsync(WebSocket webSocket)
    {
        var buffer = new byte[4096];
        var timeout = TimeSpan.FromSeconds(10);
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeEl) &&
                    typeEl.GetString() == "identify" &&
                    root.TryGetProperty("data", out var dataEl) &&
                    dataEl.TryGetProperty("clientId", out var clientIdEl))
                {
                    return clientIdEl.GetString();
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[WebSocketServer] Identify timeout");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebSocketServer] Identify error: {ex.Message}");
        }

        return null;
    }

    private async Task HandleClientConnectionAsync(SessionClient client, SimulationSession session)
    {
        var buffer = new byte[4096];

        try
        {
            while (client.IsConnected && !_cts.Token.IsCancellationRequested)
            {
                var result = await client.Socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleClientMessageAsync(client, session, message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (WebSocketException)
        {
            // Connection closed
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebSocketServer] Client error: {ex.Message}");
        }
        finally
        {
            session.RemoveClient(client);

            if (client.IsConnected)
            {
                try
                {
                    await client.Socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None);
                }
                catch { }
            }
        }
    }

    private async Task HandleClientMessageAsync(SessionClient client, SimulationSession session, string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
            {
                await session.SendToClientAsync(client, "error", new { message = "Missing 'type' field" });
                return;
            }

            var type = typeElement.GetString();

            if (type == "command" && root.TryGetProperty("data", out var dataElement))
            {
                await HandleCommandAsync(client, session, dataElement);
            }
            else
            {
                await session.SendToClientAsync(client, "error", new { message = $"Unknown message type: {type}" });
            }
        }
        catch (JsonException ex)
        {
            await session.SendToClientAsync(client, "error", new { message = $"Invalid JSON: {ex.Message}" });
        }
    }

    private async Task HandleCommandAsync(SessionClient client, SimulationSession session, JsonElement commandData)
    {
        if (!commandData.TryGetProperty("type", out var cmdTypeElement))
        {
            await session.SendToClientAsync(client, "error", new { message = "Missing command type" });
            return;
        }

        var cmdType = cmdTypeElement.GetString() ?? "";

        // Log the command
        session.Logger.LogCommand(cmdType, commandData);

        // Validate permission
        var validationError = session.ValidateCommand(client, cmdType);
        if (validationError != null)
        {
            await session.SendToClientAsync(client, "error", new
            {
                message = validationError,
                code = client.Role == SessionRole.Viewer ? "permission_denied" : "owner_disconnected"
            });
            return;
        }

        Console.WriteLine($"[WebSocketServer] Processing command: {cmdType}");

        switch (cmdType)
        {
            case "start":
                Console.WriteLine($"[WebSocketServer] Executing 'start' command");
                await session.StartSimulationAsync();
                await session.SendToClientAsync(client, "command_ack", new { command = "start", success = true });
                break;

            case "stop":
                Console.WriteLine($"[WebSocketServer] Executing 'stop' command");
                session.StopSimulation();
                await session.SendToClientAsync(client, "command_ack", new { command = "stop", success = true });
                break;

            case "step":
                Console.WriteLine($"[WebSocketServer] Executing 'step' command");
                session.StepSimulation();
                var stepFrame = session.Simulator.GetCurrentFrameData();
                Console.WriteLine($"[WebSocketServer] Step complete: frame={stepFrame.FrameNumber}, towers={stepFrame.FriendlyTowers.Count}F/{stepFrame.EnemyTowers.Count}E, units={stepFrame.FriendlyUnits.Count}F/{stepFrame.EnemyUnits.Count}E");
                await session.SendToClientAsync(client, "command_ack", new { command = "step", success = true });
                break;

            case "step_back":
                Console.WriteLine($"[WebSocketServer] Executing 'step_back' command");
                await HandleStepBackAsync(client, session);
                break;

            case "seek":
                if (commandData.TryGetProperty("frameNumber", out var frameElement) &&
                    frameElement.TryGetInt32(out var frameNumber))
                {
                    Console.WriteLine($"[WebSocketServer] Executing 'seek' to frame {frameNumber}");
                    await HandleSeekAsync(client, session, frameNumber);
                }
                else
                {
                    await session.SendToClientAsync(client, "error", new { message = "Missing frameNumber for seek command" });
                }
                break;

            case "reset":
                Console.WriteLine($"[WebSocketServer] Executing 'reset' command");
                session.ResetSimulation();
                var resetFrame = session.Simulator.GetCurrentFrameData();
                Console.WriteLine($"[WebSocketServer] Reset complete: towers={resetFrame.FriendlyTowers.Count}F/{resetFrame.EnemyTowers.Count}E, units={resetFrame.FriendlyUnits.Count}F/{resetFrame.EnemyUnits.Count}E");
                await session.BroadcastAsync("frame", resetFrame);
                await session.SendToClientAsync(client, "command_ack", new { command = "reset", success = true });
                break;

            case "move":
                await HandleMoveCommandAsync(client, session, commandData);
                break;

            case "set_health":
                await HandleSetHealthCommandAsync(client, session, commandData);
                break;

            case "kill":
                await HandleKillCommandAsync(client, session, commandData);
                break;

            case "revive":
                await HandleReviveCommandAsync(client, session, commandData);
                break;

            case "spawn":
                await HandleSpawnCommandAsync(client, session, commandData);
                break;

            case "get_session_log":
                await HandleGetSessionLogAsync(client, session);
                break;

            case "activate_skill":
                await TowerSkillHandler.HandleActivateSkillAsync(client, session, commandData);
                break;

            case "get_tower_skills":
                await TowerSkillHandler.HandleGetSkillsAsync(client, session, commandData);
                break;

            default:
                await session.SendToClientAsync(client, "error", new { message = $"Unknown command: {cmdType}" });
                break;
        }
    }

    #endregion

    #region Command Handlers

    private async Task HandleStepBackAsync(SessionClient client, SimulationSession session)
    {
        var targetFrame = Math.Max(0, session.Simulator.CurrentFrame - 1);
        var frame = session.GetFrameFromHistory(targetFrame);

        if (frame == null)
        {
            await session.SendToClientAsync(client, "error", new { message = $"No frame history for frame {targetFrame}" });
            return;
        }

        session.Simulator.LoadState(frame);
        await session.BroadcastAsync("frame", frame);
    }

    private async Task HandleSeekAsync(SessionClient client, SimulationSession session, int targetFrame)
    {
        if (targetFrame < 0)
        {
            await session.SendToClientAsync(client, "error", new { message = "frameNumber must be non-negative" });
            return;
        }

        // For Viewer, only local seek (send frame from history without affecting simulation)
        if (client.Role == SessionRole.Viewer)
        {
            var historyFrame = session.GetFrameFromHistory(targetFrame);
            if (historyFrame != null)
            {
                await session.SendToClientAsync(client, "frame", historyFrame);
            }
            else
            {
                await session.SendToClientAsync(client, "error", new { message = $"Frame {targetFrame} not in history" });
            }
            return;
        }

        // For Owner, seek affects the simulation state
        var frame = session.GetFrameFromHistory(targetFrame);
        if (frame != null)
        {
            session.Simulator.LoadState(frame);
            await session.BroadcastAsync("frame", frame);
            return;
        }

        // Frame not in history, simulate forward
        if (!session.Simulator.IsInitialized)
        {
            session.Simulator.Initialize();
        }

        var callbacks = new SeekCallbacks(session);
        FrameData? reached = null;

        while (true)
        {
            var frameData = session.Simulator.Step(callbacks);
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
            await session.BroadcastAsync("frame", reached);
        }
        else
        {
            await session.SendToClientAsync(client, "error", new { message = $"Unable to seek to frame {targetFrame}" });
        }
    }

    private async Task HandleMoveCommandAsync(SessionClient client, SimulationSession session, JsonElement data)
    {
        if (!TryGetUnitInfo(data, out var unitId, out var faction))
        {
            await session.SendToClientAsync(client, "error", new { message = "Invalid unit info for move command" });
            return;
        }

        if (!data.TryGetProperty("position", out var posElement) ||
            !posElement.TryGetProperty("x", out var xElement) ||
            !posElement.TryGetProperty("y", out var yElement))
        {
            await session.SendToClientAsync(client, "error", new { message = "Missing position for move command" });
            return;
        }

        var x = xElement.GetSingle();
        var y = yElement.GetSingle();

        var success = session.Simulator.ModifyUnit(unitId, faction, unit =>
        {
            unit.CurrentDestination = new System.Numerics.Vector2(x, y);
        });

        await session.SendToClientAsync(client, "command_ack", new { command = "move", success, unitId, x, y });

        if (success)
        {
            await session.BroadcastAsync("frame", session.Simulator.GetCurrentFrameData());
        }
    }

    private async Task HandleSetHealthCommandAsync(SessionClient client, SimulationSession session, JsonElement data)
    {
        if (!TryGetUnitInfo(data, out var unitId, out var faction))
        {
            await session.SendToClientAsync(client, "error", new { message = "Invalid unit info for set_health command" });
            return;
        }

        if (!data.TryGetProperty("health", out var healthElement))
        {
            await session.SendToClientAsync(client, "error", new { message = "Missing health value" });
            return;
        }

        var health = healthElement.GetInt32();

        var success = session.Simulator.ModifyUnit(unitId, faction, unit =>
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

        await session.SendToClientAsync(client, "command_ack", new { command = "set_health", success, unitId, health });

        if (success)
        {
            await session.BroadcastAsync("frame", session.Simulator.GetCurrentFrameData());
        }
    }

    private async Task HandleKillCommandAsync(SessionClient client, SimulationSession session, JsonElement data)
    {
        if (!TryGetUnitInfo(data, out var unitId, out var faction))
        {
            await session.SendToClientAsync(client, "error", new { message = "Invalid unit info for kill command" });
            return;
        }

        var success = session.Simulator.ModifyUnit(unitId, faction, unit =>
        {
            unit.HP = 0;
            unit.IsDead = true;
        });

        await session.SendToClientAsync(client, "command_ack", new { command = "kill", success, unitId });

        if (success)
        {
            await session.BroadcastAsync("frame", session.Simulator.GetCurrentFrameData());
        }
    }

    private async Task HandleReviveCommandAsync(SessionClient client, SimulationSession session, JsonElement data)
    {
        if (!TryGetUnitInfo(data, out var unitId, out var faction))
        {
            await session.SendToClientAsync(client, "error", new { message = "Invalid unit info for revive command" });
            return;
        }

        var health = 100;
        if (data.TryGetProperty("health", out var healthElement))
        {
            health = healthElement.GetInt32();
        }

        var success = session.Simulator.ModifyUnit(unitId, faction, unit =>
        {
            unit.HP = health;
            unit.IsDead = false;
        });

        await session.SendToClientAsync(client, "command_ack", new { command = "revive", success, unitId, health });

        if (success)
        {
            await session.BroadcastAsync("frame", session.Simulator.GetCurrentFrameData());
        }
    }

    private async Task HandleSpawnCommandAsync(SessionClient client, SimulationSession session, JsonElement data)
    {
        // Parse position
        if (!data.TryGetProperty("position", out var posElement))
        {
            await session.SendToClientAsync(client, "error", new { message = "Missing position for spawn command" });
            return;
        }

        float x = 0, y = 0;
        if (posElement.TryGetProperty("x", out var xElem)) x = xElem.GetSingle();
        if (posElement.TryGetProperty("y", out var yElem)) y = yElem.GetSingle();

        // Parse faction (default: Enemy)
        var faction = UnitFaction.Enemy;
        if (data.TryGetProperty("faction", out var factionElem))
        {
            var factionStr = factionElem.GetString();
            if (factionStr?.Equals("Friendly", StringComparison.OrdinalIgnoreCase) == true)
                faction = UnitFaction.Friendly;
        }

        // Parse role (default: Melee)
        var role = UnitRole.Melee;
        if (data.TryGetProperty("role", out var roleElem))
        {
            var roleStr = roleElem.GetString();
            if (roleStr?.Equals("Ranged", StringComparison.OrdinalIgnoreCase) == true)
                role = UnitRole.Ranged;
        }

        // Parse optional unitId (for reference-based spawning)
        string? unitId = null;
        if (data.TryGetProperty("unitId", out var unitIdElem))
        {
            unitId = unitIdElem.GetString();
        }

        // Parse optional HP
        int? hp = null;
        if (data.TryGetProperty("hp", out var hpElem))
        {
            hp = hpElem.GetInt32();
        }

        Console.WriteLine($"[WebSocketServer] Spawn command: faction={faction}, role={role}, pos=({x}, {y}), unitId={unitId ?? "default"}");

        var position = new System.Numerics.Vector2(x, y);

        // Use session.SpawnUnit to spawn the unit
        var unit = session.SpawnUnit(
            position,
            role,
            faction,
            hp
        );

        if (unit == null)
        {
            await session.SendToClientAsync(client, "command_ack", new
            {
                command = "spawn",
                success = false,
                message = "Spawn rejected by manual spawn rules."
            });
            return;
        }

        await session.SendToClientAsync(client, "command_ack", new
        {
            command = "spawn",
            success = true,
            unitLabel = unit.Label,
            unitId = unit.Id,
            faction = faction.ToString()
        });

        await session.BroadcastAsync("frame", session.Simulator.GetCurrentFrameData());
    }

    private async Task HandleGetSessionLogAsync(SessionClient client, SimulationSession session)
    {
        try
        {
            var summary = session.Logger.GetSummary();
            await session.SendToClientAsync(client, "session_log_summary", summary);
        }
        catch (Exception ex)
        {
            session.Logger.LogError($"Failed to get session log: {ex.Message}", ex);
            await session.SendToClientAsync(client, "error", new { message = $"Failed to get session log: {ex.Message}" });
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

    #endregion

    #region Utilities

    private async Task SendMessageAsync<T>(WebSocket socket, string type, T data)
    {
        if (socket.State != WebSocketState.Open) return;

        var message = new { type, data };
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    #endregion

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _cts.Cancel();
        _sessionManager.Dispose();

        try
        {
            if (_httpListener.IsListening)
            {
                _httpListener.Stop();
            }
        }
        catch { }

        Console.WriteLine("[WebSocketServer] Stopped");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

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
    /// Callback for seek operation that records frames.
    /// </summary>
    private class SeekCallbacks : ISimulatorCallbacks
    {
        private readonly SimulationSession _session;

        public SeekCallbacks(SimulationSession session)
        {
            _session = session;
        }

        public void OnFrameGenerated(FrameData frameData)
        {
            _session.RecordFrame(frameData);
        }

        public void OnSimulationComplete(int finalFrameNumber, string reason) { }
        public void OnStateChanged(string changeDescription) { }
        public void OnUnitEvent(UnitEventData eventData) { }
    }
}
