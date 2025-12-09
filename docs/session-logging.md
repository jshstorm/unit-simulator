# Session Debugging Information

The Unit Simulator now includes comprehensive session logging to help debug and analyze simulation behavior. Session logs capture all events, commands, and state changes during a WebSocket server session.

## Features

- **Automatic Session Tracking**: Each WebSocket server session is automatically assigned a unique ID
- **Event Logging**: All simulation events, commands, and state changes are logged with timestamps
- **Session Summary**: Statistics about events, duration, and session metadata
- **JSON Export**: Complete session logs are exported in JSON format for analysis

## Session Log Contents

Each session log contains:

### Metadata
- `sessionId`: Unique identifier for the session
- `startTime`: When the session started (UTC)
- `endTime`: When the session ended (UTC)
- `endReason`: Why the session ended ("Server stopped", "Normal completion", etc.)
- `totalEvents`: Total number of events logged

### Events
Each event includes:
- `timestamp`: When the event occurred (UTC)
- `eventType`: Type of event (SessionStarted, CommandReceived, FrameGenerated, etc.)
- `description`: Human-readable description
- `data`: Optional payload with event-specific data

### Summary
- Session duration
- Event counts by type
- Quick overview statistics

## Event Types

The session logger tracks these event types:

- **SessionStarted**: Session initialization
- **SessionEnded**: Session termination
- **CommandReceived**: WebSocket commands from clients (start, stop, step, seek, etc.)
- **FrameGenerated**: Each simulation frame
- **StateChange**: Simulation state modifications
- **UnitEvent**: Unit-specific events (attacks, deaths, etc.)
- **SimulationComplete**: When simulation finishes
- **Error**: Any errors that occurred

## Accessing Session Logs

### Automatic Saving

Session logs are automatically saved when the WebSocket server stops:

```bash
dotnet run -- --server --port 5000
# ... server runs ...
# Press Ctrl+C to stop
```

Logs are saved to: `output/debug/session_{SESSION_ID}_{TIMESTAMP}.json`

Example filename: `session_fa3c3c06-2006-4d5d-bf43-3c8ef39fa84f_20251209_143022.json`

### Retrieving Log During Session

You can request a session summary via WebSocket:

```javascript
// Send command through WebSocket
const command = {
  type: 'command',
  data: {
    type: 'get_session_log'
  }
};
ws.send(JSON.stringify(command));

// Receive response
ws.onmessage = (event) => {
  const message = JSON.parse(event.data);
  if (message.type === 'session_log_summary') {
    console.log('Session Summary:', message.data);
  }
};
```

## Example Session Log

```json
{
  "metadata": {
    "sessionId": "3991b442-cd63-42b8-b235-85101cc22871",
    "startTime": "2025-12-09T14:52:14.1374057Z",
    "endTime": "2025-12-09T14:52:14.1434803Z",
    "endReason": "Server stopped",
    "totalEvents": 15
  },
  "events": [
    {
      "timestamp": "2025-12-09T14:52:14.1378905Z",
      "eventType": 0,
      "description": "Session logger initialized"
    },
    {
      "timestamp": "2025-12-09T14:52:14.1380919Z",
      "eventType": 2,
      "description": "Command: start"
    },
    {
      "timestamp": "2025-12-09T14:52:14.1382415Z",
      "eventType": 3,
      "description": "Frame 0 generated",
      "data": {
        "frameNumber": 0
      }
    }
  ],
  "summary": {
    "sessionId": "3991b442-cd63-42b8-b235-85101cc22871",
    "startTime": "2025-12-09T14:52:14.1374057Z",
    "endTime": "2025-12-09T14:52:14.1434803Z",
    "duration": "00:00:00.0060746",
    "totalEvents": 15,
    "eventCounts": {
      "SessionStarted": 1,
      "CommandReceived": 5,
      "FrameGenerated": 8,
      "SessionEnded": 1
    }
  }
}
```

## Use Cases

### Debugging Simulation Issues

Review session logs to:
- Track command sequences that led to issues
- Identify when state changes occurred
- Analyze frame-by-frame progression
- Understand the timeline of events

### Performance Analysis

Use event timestamps to:
- Measure time between commands and responses
- Identify bottlenecks in frame generation
- Analyze simulation performance

### Behavior Verification

Verify that:
- Commands are being received and processed correctly
- State changes occur as expected
- Unit events are triggered appropriately
- Simulation completes under the right conditions

## Integration with GUI Viewer

The GUI viewer can request session logs through the `get_session_log` command to display debugging information in the UI. This complements the client-side frame logging implemented in PR #14.

## File Format

Session logs use pretty-printed JSON with camelCase property names for easy readability and parsing. The format is compatible with standard JSON tools like `jq`:

```bash
# View session summary
cat output/debug/session_*.json | jq '.summary'

# Count events by type
cat output/debug/session_*.json | jq '.summary.eventCounts'

# Extract all errors
cat output/debug/session_*.json | jq '.events[] | select(.eventType == 7)'
```

## Notes

- Session logs are stored in `output/debug/` by default
- Each session generates a new log file
- Log files are timestamped for easy chronological sorting
- Session IDs are GUIDs to ensure uniqueness
- All timestamps are in UTC for consistency
