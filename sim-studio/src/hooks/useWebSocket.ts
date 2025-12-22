import { useState, useEffect, useCallback, useRef } from 'react';
import { FrameData, Command, WebSocketMessage, SessionJoinedData, SessionRole } from '../types';
import { getClientId } from './useClientId';

export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected';

export interface UseWebSocketResult {
  frameData: FrameData | null;
  frameLog: FrameData[];
  connectionStatus: ConnectionStatus;
  sendCommand: (command: Command) => void;
  error: string | null;
  lastMessageType: WebSocketMessage['type'] | null;
  // Session info
  sessionId: string | null;
  role: SessionRole | null;
  isOwnerConnected: boolean;
}

interface UseWebSocketOptions {
  sessionId?: string | null;  // null = create new session, string = join existing
}

export function useWebSocket(
  baseUrl: string,
  options: UseWebSocketOptions = {}
): UseWebSocketResult {
  const [frameData, setFrameData] = useState<FrameData | null>(null);
  const [frameLogMap, setFrameLogMap] = useState<Map<number, FrameData>>(new Map());
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('disconnected');
  const [error, setError] = useState<string | null>(null);
  const [lastMessageType, setLastMessageType] = useState<WebSocketMessage['type'] | null>(null);

  // Session state
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [role, setRole] = useState<SessionRole | null>(null);
  const [isOwnerConnected, setIsOwnerConnected] = useState(true);

  const wsRef = useRef<WebSocket | null>(null);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const reconnectAttempts = useRef(0);

  // Build WebSocket URL based on session option
  const wsUrl = options.sessionId
    ? `${baseUrl}/${options.sessionId}`
    : `${baseUrl}/new`;

  const connect = useCallback(() => {
    if (wsRef.current?.readyState === WebSocket.OPEN) {
      return;
    }

    setConnectionStatus('connecting');
    setError(null);

    try {
      const ws = new WebSocket(wsUrl);
      wsRef.current = ws;

      ws.onopen = () => {
        // Send identify message immediately after connection
        const clientId = getClientId();
        ws.send(JSON.stringify({
          type: 'identify',
          data: { clientId }
        }));
      };

      ws.onmessage = (event) => {
        try {
          const message: WebSocketMessage = JSON.parse(event.data);
          setLastMessageType(message.type);

          switch (message.type) {
            case 'session_joined': {
              const data = message.data as SessionJoinedData;
              setSessionId(data.sessionId);
              setRole(data.role);
              setConnectionStatus('connected');
              setError(null);
              reconnectAttempts.current = 0;
              break;
            }
            case 'frame': {
              const newFrameData = message.data as FrameData;
              setFrameData(newFrameData);
              setFrameLogMap(prev => {
                const updated = new Map(prev);
                updated.set(newFrameData.frameNumber, newFrameData);
                return updated;
              });
              break;
            }
            case 'state_change': {
              const data = message.data as { state?: string; reason?: string; description?: string };
              // Handle owner disconnect/reconnect
              if (data.reason === 'owner_disconnected') {
                setIsOwnerConnected(false);
              } else if (data.reason === 'owner_reconnected') {
                setIsOwnerConnected(true);
              }
              break;
            }
            case 'simulation_complete':
              // Could show completion UI
              break;
            case 'error': {
              const errorData = message.data as { message?: string; code?: string } | string;
              if (typeof errorData === 'string') {
                setError(errorData);
              } else {
                setError(errorData.message || 'Unknown error');
              }
              break;
            }
            default:
              break;
          }
        } catch {
          console.error('Failed to parse WebSocket message:', event.data);
        }
      };

      ws.onclose = () => {
        setConnectionStatus('disconnected');
        wsRef.current = null;

        // Attempt to reconnect with exponential backoff
        if (reconnectAttempts.current < 5) {
          const delay = Math.min(1000 * Math.pow(2, reconnectAttempts.current), 30000);
          reconnectAttempts.current++;

          reconnectTimeoutRef.current = setTimeout(() => {
            connect();
          }, delay);
        } else {
          setError('Failed to connect after multiple attempts');
        }
      };

      ws.onerror = () => {
        setError('WebSocket connection error');
      };
    } catch (err) {
      setError('Failed to create WebSocket connection');
      setConnectionStatus('disconnected');
    }
  }, [wsUrl]);

  const sendCommand = useCallback((command: Command) => {
    if (wsRef.current?.readyState === WebSocket.OPEN) {
      wsRef.current.send(JSON.stringify({
        type: 'command',
        data: command,
        timestamp: Date.now(),
      }));

      // When seeking, clean up frames after the target frame
      if (command.type === 'seek' && command.frameNumber !== undefined) {
        const targetFrame = command.frameNumber;
        setFrameLogMap(prev => {
          const updated = new Map(prev);
          for (const frameNumber of updated.keys()) {
            if (frameNumber > targetFrame) {
              updated.delete(frameNumber);
            }
          }
          return updated;
        });
      }
    } else {
      setError('Not connected to server');
    }
  }, []);

  useEffect(() => {
    connect();

    return () => {
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
      if (wsRef.current) {
        wsRef.current.close();
      }
    };
  }, [connect]);

  // Reset state when session changes
  useEffect(() => {
    setFrameData(null);
    setFrameLogMap(new Map());
    setSessionId(null);
    setRole(null);
    setIsOwnerConnected(true);
  }, [options.sessionId]);

  // Convert frameLogMap to sorted array
  const frameLog = Array.from(frameLogMap.values()).sort(
    (a, b) => a.frameNumber - b.frameNumber
  );

  return {
    frameData,
    frameLog,
    connectionStatus,
    sendCommand,
    error,
    lastMessageType,
    sessionId,
    role,
    isOwnerConnected,
  };
}
