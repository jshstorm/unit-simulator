import { useState, useEffect, useCallback } from 'react';
import { SessionInfo } from '../types';

interface SessionSelectorProps {
  apiBaseUrl: string;
  onSessionSelect: (sessionId: string | null) => void;
}

export default function SessionSelector({ apiBaseUrl, onSessionSelect }: SessionSelectorProps) {
  const [sessions, setSessions] = useState<SessionInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null);

  const fetchSessions = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch(`${apiBaseUrl}/sessions`);
      if (!response.ok) {
        throw new Error('Failed to fetch sessions');
      }
      const data = await response.json();
      setSessions(data.sessions || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch sessions');
    } finally {
      setLoading(false);
    }
  }, [apiBaseUrl]);

  useEffect(() => {
    fetchSessions();
    // Refresh session list every 5 seconds
    const interval = setInterval(fetchSessions, 5000);
    return () => clearInterval(interval);
  }, [fetchSessions]);

  const handleCreateNew = () => {
    setSelectedSessionId(null);
    onSessionSelect(null);  // null means create new
  };

  const handleJoinSession = (sessionId: string) => {
    setSelectedSessionId(sessionId);
    onSessionSelect(sessionId);
  };

  const formatTimeAgo = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMin = Math.floor(diffMs / 60000);

    if (diffMin < 1) return 'Just now';
    if (diffMin < 60) return `${diffMin}m ago`;
    const diffHour = Math.floor(diffMin / 60);
    if (diffHour < 24) return `${diffHour}h ago`;
    return `${Math.floor(diffHour / 24)}d ago`;
  };

  const getStateColor = (state: string) => {
    switch (state) {
      case 'running': return '#4ade80';
      case 'paused': return '#facc15';
      case 'idle': return '#94a3b8';
      case 'completed': return '#60a5fa';
      default: return '#94a3b8';
    }
  };

  return (
    <div className="session-selector">
      <div className="session-selector-header">
        <h2>Select Session</h2>
        <button className="btn-refresh" onClick={fetchSessions} disabled={loading}>
          {loading ? 'Loading...' : 'Refresh'}
        </button>
      </div>

      {error && (
        <div className="session-error">
          {error}
        </div>
      )}

      <div className="session-options">
        <button
          className={`session-option session-option-new ${selectedSessionId === null ? 'selected' : ''}`}
          onClick={handleCreateNew}
        >
          <div className="session-option-icon">+</div>
          <div className="session-option-content">
            <div className="session-option-title">Create New Session</div>
            <div className="session-option-desc">Start a new isolated simulation</div>
          </div>
        </button>

        {sessions.length > 0 && (
          <div className="session-divider">
            <span>Existing Sessions ({sessions.length})</span>
          </div>
        )}

        {sessions.map((session) => (
          <button
            key={session.sessionId}
            className={`session-option ${selectedSessionId === session.sessionId ? 'selected' : ''}`}
            onClick={() => handleJoinSession(session.sessionId)}
          >
            <div
              className="session-option-icon session-state-indicator"
              style={{ backgroundColor: getStateColor(session.simulatorState) }}
            />
            <div className="session-option-content">
              <div className="session-option-title">
                {session.sessionId.substring(0, 8)}...
              </div>
              <div className="session-option-meta">
                <span className="session-state" style={{ color: getStateColor(session.simulatorState) }}>
                  {session.simulatorState}
                </span>
                <span className="session-frame">Frame {session.currentFrame}</span>
                <span className="session-clients">{session.clientCount} client(s)</span>
                <span className="session-time">{formatTimeAgo(session.lastActivityAt)}</span>
              </div>
              {!session.isOwnerConnected && session.hasOwner && (
                <div className="session-warning">Owner disconnected - Read only</div>
              )}
            </div>
          </button>
        ))}
      </div>

      <style>{`
        .session-selector {
          background: #1e293b;
          border-radius: 8px;
          padding: 1.5rem;
          max-width: 400px;
          margin: 2rem auto;
        }

        .session-selector-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 1rem;
        }

        .session-selector-header h2 {
          margin: 0;
          color: #f1f5f9;
          font-size: 1.25rem;
        }

        .btn-refresh {
          background: #334155;
          border: none;
          color: #94a3b8;
          padding: 0.5rem 1rem;
          border-radius: 4px;
          cursor: pointer;
          font-size: 0.875rem;
        }

        .btn-refresh:hover:not(:disabled) {
          background: #475569;
          color: #f1f5f9;
        }

        .btn-refresh:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .session-error {
          background: #7f1d1d;
          color: #fca5a5;
          padding: 0.75rem;
          border-radius: 4px;
          margin-bottom: 1rem;
          font-size: 0.875rem;
        }

        .session-options {
          display: flex;
          flex-direction: column;
          gap: 0.5rem;
        }

        .session-option {
          display: flex;
          align-items: flex-start;
          gap: 1rem;
          background: #334155;
          border: 2px solid transparent;
          border-radius: 6px;
          padding: 1rem;
          cursor: pointer;
          text-align: left;
          transition: all 0.2s;
          width: 100%;
        }

        .session-option:hover {
          background: #475569;
        }

        .session-option.selected {
          border-color: #3b82f6;
          background: #1e3a5f;
        }

        .session-option-new .session-option-icon {
          background: #3b82f6;
          color: white;
          width: 40px;
          height: 40px;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 1.5rem;
          font-weight: bold;
          flex-shrink: 0;
        }

        .session-state-indicator {
          width: 12px;
          height: 12px;
          border-radius: 50%;
          flex-shrink: 0;
          margin-top: 4px;
        }

        .session-option-content {
          flex: 1;
          min-width: 0;
        }

        .session-option-title {
          color: #f1f5f9;
          font-weight: 500;
          font-size: 0.95rem;
          font-family: monospace;
        }

        .session-option-desc {
          color: #94a3b8;
          font-size: 0.875rem;
          margin-top: 0.25rem;
        }

        .session-option-meta {
          display: flex;
          flex-wrap: wrap;
          gap: 0.75rem;
          margin-top: 0.25rem;
          font-size: 0.75rem;
          color: #94a3b8;
        }

        .session-state {
          font-weight: 500;
        }

        .session-warning {
          color: #facc15;
          font-size: 0.75rem;
          margin-top: 0.25rem;
        }

        .session-divider {
          display: flex;
          align-items: center;
          margin: 0.75rem 0;
          color: #64748b;
          font-size: 0.75rem;
        }

        .session-divider::before,
        .session-divider::after {
          content: '';
          flex: 1;
          height: 1px;
          background: #475569;
        }

        .session-divider span {
          padding: 0 0.75rem;
        }
      `}</style>
    </div>
  );
}
