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
  | 'error';

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
  | 'reset';

export interface Command {
  type: CommandType;
  unitId?: number;
  faction?: 'Friendly' | 'Enemy';
  position?: SerializableVector2;
  health?: number;
}
