using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnitSimulator;

/// <summary>
/// Captures detailed debugging information for a simulation session.
/// This logger records all events, commands, and state changes during a session,
/// providing comprehensive data for debugging and analysis.
/// </summary>
public class SessionLogger : ISimulatorCallbacks
{
    private readonly SessionMetadata _metadata;
    private readonly List<SessionEvent> _events = new();
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Gets the session ID.
    /// </summary>
    public string SessionId => _metadata.SessionId;

    /// <summary>
    /// Initializes a new session logger.
    /// </summary>
    /// <param name="sessionId">Optional session ID. If not provided, a new GUID will be generated.</param>
    public SessionLogger(string? sessionId = null)
    {
        _metadata = new SessionMetadata
        {
            SessionId = sessionId ?? Guid.NewGuid().ToString(),
            StartTime = DateTime.UtcNow
        };

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        LogEvent(SessionEventType.SessionStarted, "Session logger initialized");
    }

    /// <summary>
    /// Logs a command received by the server.
    /// </summary>
    /// <param name="commandType">The type of command.</param>
    /// <param name="parameters">Optional parameters for the command.</param>
    public void LogCommand(string commandType, object? parameters = null)
    {
        LogEvent(SessionEventType.CommandReceived, $"Command: {commandType}", parameters);
    }

    /// <summary>
    /// Logs a state change in the simulation.
    /// </summary>
    /// <param name="description">Description of the state change.</param>
    public void LogStateChange(string description)
    {
        LogEvent(SessionEventType.StateChange, description);
    }

    /// <summary>
    /// Logs a unit event.
    /// </summary>
    /// <param name="eventData">The unit event data.</param>
    public void LogUnitEvent(UnitEventData eventData)
    {
        LogEvent(SessionEventType.UnitEvent, 
            $"{eventData.EventType} - Unit {eventData.UnitId} ({eventData.Faction})", 
            eventData);
    }

    /// <summary>
    /// Logs a frame being generated.
    /// </summary>
    /// <param name="frameNumber">The frame number.</param>
    /// <param name="summary">Optional summary of the frame.</param>
    public void LogFrame(int frameNumber, string? summary = null)
    {
        LogEvent(SessionEventType.FrameGenerated, 
            summary ?? $"Frame {frameNumber} generated", 
            new { frameNumber });
    }

    /// <summary>
    /// Logs an error that occurred during the session.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="exception">Optional exception details.</param>
    public void LogError(string error, Exception? exception = null)
    {
        LogEvent(SessionEventType.Error, error, exception != null ? new { 
            message = exception.Message, 
            stackTrace = exception.StackTrace 
        } : null);
    }

    /// <summary>
    /// Ends the session and updates metadata.
    /// </summary>
    public void EndSession(string reason = "Normal completion")
    {
        lock (_lock)
        {
            _metadata.EndTime = DateTime.UtcNow;
            _metadata.EndReason = reason;
            _metadata.TotalEvents = _events.Count;
            
            LogEvent(SessionEventType.SessionEnded, $"Session ended: {reason}");
        }
    }

    /// <summary>
    /// Gets a summary of the session.
    /// </summary>
    public SessionSummary GetSummary()
    {
        lock (_lock)
        {
            var eventCounts = _events
                .GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            return new SessionSummary
            {
                SessionId = _metadata.SessionId,
                StartTime = _metadata.StartTime,
                EndTime = _metadata.EndTime,
                Duration = _metadata.EndTime.HasValue 
                    ? _metadata.EndTime.Value - _metadata.StartTime 
                    : DateTime.UtcNow - _metadata.StartTime,
                TotalEvents = _events.Count,
                EventCounts = eventCounts
            };
        }
    }

    /// <summary>
    /// Exports the complete session log as JSON.
    /// </summary>
    public string ToJson()
    {
        lock (_lock)
        {
            var log = new SessionLog
            {
                Metadata = _metadata,
                Events = _events.ToList(),
                Summary = GetSummary()
            };

            return JsonSerializer.Serialize(log, _jsonOptions);
        }
    }

    /// <summary>
    /// Saves the session log to a file.
    /// </summary>
    /// <param name="filePath">The path where the log will be saved.</param>
    public void SaveToFile(string filePath)
    {
        var json = ToJson();
        File.WriteAllText(filePath, json);
        Console.WriteLine($"[SessionLogger] Session log saved to: {filePath}");
    }

    /// <summary>
    /// Saves the session log to a default location based on session ID.
    /// </summary>
    /// <param name="outputDirectory">The output directory. Defaults to "./output/debug".</param>
    /// <returns>The path where the file was saved.</returns>
    public string SaveToDefaultLocation(string? outputDirectory = null)
    {
        outputDirectory ??= Path.Combine(Constants.OUTPUT_DIRECTORY, Constants.DEBUG_SUBDIRECTORY);
        Directory.CreateDirectory(outputDirectory);

        var fileName = $"session_{_metadata.SessionId}_{_metadata.StartTime:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(outputDirectory, fileName);
        
        SaveToFile(filePath);
        return filePath;
    }

    private void LogEvent(SessionEventType eventType, string description, object? data = null)
    {
        lock (_lock)
        {
            _events.Add(new SessionEvent
            {
                Timestamp = DateTime.UtcNow,
                EventType = eventType,
                Description = description,
                Data = data
            });
        }
    }

    // ISimulatorCallbacks implementation
    public void OnFrameGenerated(FrameData frameData)
    {
        LogFrame(frameData.FrameNumber, 
            $"Frame {frameData.FrameNumber}: Wave {frameData.CurrentWave}, " +
            $"Friendlies: {frameData.LivingFriendlyCount}, Enemies: {frameData.LivingEnemyCount}");
    }

    public void OnSimulationComplete(int finalFrameNumber, string reason)
    {
        LogEvent(SessionEventType.SimulationComplete, 
            $"Simulation completed at frame {finalFrameNumber}: {reason}", 
            new { finalFrameNumber, reason });
    }

    public void OnStateChanged(string changeDescription)
    {
        LogStateChange(changeDescription);
    }

    public void OnUnitEvent(UnitEventData eventData)
    {
        LogUnitEvent(eventData);
    }
}

/// <summary>
/// Metadata about a simulation session.
/// </summary>
public class SessionMetadata
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? EndReason { get; set; }
    public int TotalEvents { get; set; }
}

/// <summary>
/// A single event that occurred during a session.
/// </summary>
public class SessionEvent
{
    public DateTime Timestamp { get; set; }
    public SessionEventType EventType { get; set; }
    public string Description { get; set; } = string.Empty;
    public object? Data { get; set; }
}

/// <summary>
/// Types of events that can be logged in a session.
/// </summary>
public enum SessionEventType
{
    SessionStarted,
    SessionEnded,
    CommandReceived,
    FrameGenerated,
    StateChange,
    UnitEvent,
    SimulationComplete,
    Error
}

/// <summary>
/// Summary statistics for a session.
/// </summary>
public class SessionSummary
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventCounts { get; set; } = new();
}

/// <summary>
/// Complete session log including metadata, events, and summary.
/// </summary>
public class SessionLog
{
    public SessionMetadata Metadata { get; set; } = new();
    public List<SessionEvent> Events { get; set; } = new();
    public SessionSummary Summary { get; set; } = new();
}
