// Type definitions matching the .NET FrameData and related types

export interface SerializableVector2 {
  x: number;
  y: number;
}

export interface UnitStateData {
  id: number;
  label: string;
  role: 'Melee' | 'Ranged';
  faction: 'Friendly' | 'Enemy';
  isDead: boolean;
  hp: number;
  radius: number;
  speed: number;
  turnSpeed: number;
  attackRange: number;
  attackCooldown: number;
  position: SerializableVector2;
  velocity: SerializableVector2;
  forward: SerializableVector2;
  currentDestination: SerializableVector2;
  targetId: number | null;
  takenSlotIndex: number;
  hasAvoidanceTarget: boolean;
  avoidanceTarget: SerializableVector2 | null;
  isMoving: boolean;
  inAttackRange: boolean;
}

export interface FrameData {
  frameNumber: number;
  currentWave: number;
  livingFriendlyCount: number;
  livingEnemyCount: number;
  mainTarget: SerializableVector2;
  friendlyUnits: UnitStateData[];
  enemyUnits: UnitStateData[];
  allWavesCleared: boolean;
  maxFramesReached: boolean;
}

// WebSocket message types
export type MessageType =
  | 'frame'
  | 'state_change'
  | 'unit_event'
  | 'simulation_complete'
  | 'command_ack'
  | 'session_log_summary'
  | 'session_joined'
  | 'error';

// Session types
export type SessionRole = 'owner' | 'viewer';

export interface SessionInfo {
  sessionId: string;
  createdAt: string;
  lastActivityAt: string;
  clientCount: number;
  simulatorState: 'idle' | 'running' | 'paused' | 'completed';
  currentFrame: number;
  hasOwner: boolean;
  isOwnerConnected: boolean;
}

export interface SessionJoinedData {
  sessionId: string;
  role: SessionRole;
  simulatorState: string;
  currentFrame: number;
  clientCount: number;
}

export interface WebSocketMessage {
  type: MessageType;
  data: unknown;
  timestamp?: number;
}

export interface FrameMessage extends WebSocketMessage {
  type: 'frame';
  data: FrameData;
}

export interface StateChangeMessage extends WebSocketMessage {
  type: 'state_change';
  data: {
    description: string;
  };
}

export interface UnitEventMessage extends WebSocketMessage {
  type: 'unit_event';
  data: {
    eventType: string;
    unitId: number;
    faction: string;
    frameNumber: number;
    targetUnitId?: number;
    value?: number;
    position?: SerializableVector2;
  };
}

export interface SimulationCompleteMessage extends WebSocketMessage {
  type: 'simulation_complete';
  data: {
    finalFrame: number;
    reason: string;
  };
}

// Commands that can be sent to the server
export type CommandType = 
  | 'move'
  | 'set_health'
  | 'kill'
  | 'revive'
  | 'start'
  | 'stop'
  | 'step'
  | 'step_back'
  | 'seek'
  | 'reset'
  | 'get_session_log';

export interface Command {
  type: CommandType;
  unitId?: number;
  faction?: 'Friendly' | 'Enemy';
  position?: SerializableVector2;
  health?: number;
  frameNumber?: number;
}

// Session logging types
export interface SessionSummary {
  sessionId: string;
  startTime: string; // ISO 8601 timestamp
  endTime?: string; // ISO 8601 timestamp
  duration: string; // TimeSpan format: HH:mm:ss.fffffff (e.g., "00:00:05.1234567")
  totalEvents: number;
  eventCounts: Record<string, number>;
}
