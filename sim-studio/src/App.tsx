import { useState, useCallback, useEffect, useRef } from 'react';
import { CameraFocusMode, Command, UnitStateData } from './types';
import { useWebSocket } from './hooks/useWebSocket';
import { downloadFrameLog } from './utils/frameLogDownload';
import SimulationCanvas, { SimulationCanvasHandle } from './components/SimulationCanvas';
import UnitStateViewer from './components/UnitStateViewer';
import CommandPanel from './components/CommandPanel';
import SimulationControls from './components/SimulationControls';
import SessionSelector from './components/SessionSelector';
import DataEditor from './components/DataEditor';
import ResizablePanel from './components/ResizablePanel';
import VerticalResizablePanel from './components/VerticalResizablePanel';
import CameraOverlay from './components/CameraOverlay';

const API_BASE_URL = 'http://localhost:5001';
const WS_BASE_URL = 'ws://localhost:5001/ws';

function App() {
  const [activeView, setActiveView] = useState<'sim' | 'data'>('sim');
  // Session selection state
  const [selectedSession, setSelectedSession] = useState<string | null | undefined>(undefined);
  const [showSessionSelector, setShowSessionSelector] = useState(true);

  const [selectedUnitId, setSelectedUnitId] = useState<number | null>(null);
  const [selectedFaction, setSelectedFaction] = useState<'Friendly' | 'Enemy' | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [seekFrameInput, setSeekFrameInput] = useState<string>('0');
  const [focusMode, setFocusMode] = useState<CameraFocusMode>('auto');
  const canvasRef = useRef<SimulationCanvasHandle>(null);
  const [cameraState, setCameraState] = useState({ zoomPercent: 100, isManualMode: false });

  const {
    frameData,
    frameLog,
    connectionStatus,
    sendCommand,
    error,
    lastMessageType,
    sessionId,
    role,
    isOwnerConnected,
  } = useWebSocket(WS_BASE_URL, {
    sessionId: selectedSession === undefined ? undefined : selectedSession,
  });

  // Handle session selection
  const handleSessionSelect = useCallback((sessionId: string | null) => {
    setSelectedSession(sessionId);
    setShowSessionSelector(false);
  }, []);

  // Show session selector when disconnected or on explicit request
  const handleChangeSession = useCallback(() => {
    setShowSessionSelector(true);
    setSelectedSession(undefined);
  }, []);

  const handleUnitSelect = useCallback((unit: UnitStateData | null) => {
    if (unit) {
      setSelectedUnitId(unit.id);
      setSelectedFaction(unit.faction);
    } else {
      setSelectedUnitId(null);
      setSelectedFaction(null);
    }
  }, []);

  const handleSendCommand = useCallback((command: Command) => {
    sendCommand(command);
  }, [sendCommand]);

  // Keep play state in sync with connection and server completion
  useEffect(() => {
    if (connectionStatus !== 'connected') {
      setIsPlaying(false);
    }
  }, [connectionStatus]);

  useEffect(() => {
    if (lastMessageType === 'simulation_complete' || lastMessageType === 'error') {
      setIsPlaying(false);
    }
  }, [lastMessageType]);

  useEffect(() => {
    if (frameData) {
      setSeekFrameInput(frameData.frameNumber.toString());
    }
  }, [frameData]);

  const handlePlayPause = useCallback(() => {
    if (connectionStatus !== 'connected') return;

    if (isPlaying) {
      sendCommand({ type: 'stop' });
      setIsPlaying(false);
    } else {
      sendCommand({ type: 'start' });
      setIsPlaying(true);
    }
  }, [connectionStatus, isPlaying, sendCommand]);

  const handleStep = useCallback(() => {
    if (connectionStatus !== 'connected') return;

    if (isPlaying) {
      sendCommand({ type: 'stop' });
      setIsPlaying(false);
    }
    sendCommand({ type: 'step' });
  }, [connectionStatus, isPlaying, sendCommand]);

  const handleStepBack = useCallback(() => {
    if (connectionStatus !== 'connected') return;

    if (isPlaying) {
      sendCommand({ type: 'stop' });
      setIsPlaying(false);
    }
    sendCommand({ type: 'step_back' });
  }, [connectionStatus, isPlaying, sendCommand]);

  const handleSeek = useCallback(() => {
    if (connectionStatus !== 'connected') return;
    const target = parseInt(seekFrameInput, 10);
    if (Number.isNaN(target) || target < 0) return;

    if (isPlaying) {
      sendCommand({ type: 'stop' });
      setIsPlaying(false);
    }
    sendCommand({ type: 'seek', frameNumber: target });
  }, [connectionStatus, isPlaying, seekFrameInput, sendCommand]);

  const handleReset = useCallback(() => {
    setIsPlaying(false);
    sendCommand({ type: 'reset' });
  }, [sendCommand]);

  const handleDownloadFrameLog = useCallback(() => {
    downloadFrameLog(frameLog);
  }, [frameLog]);

  const handleCameraStateChange = useCallback((state: { zoomPercent: number; isManualMode: boolean }) => {
    setCameraState(state);
  }, []);

  // Keyboard shortcuts for step/step back
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (connectionStatus !== 'connected') return;
      if (e.key === 'ArrowRight') {
        e.preventDefault();
        handleStep();
      } else if (e.key === 'ArrowLeft') {
        e.preventDefault();
        handleStepBack();
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [connectionStatus, handleStep, handleStepBack]);

  const selectedUnit = frameData
    ? [...frameData.friendlyUnits, ...frameData.enemyUnits].find(
        u => u.id === selectedUnitId && u.faction === selectedFaction
      )
    : null;

  // Check if user can control (owner and owner connected)
  const canControl = role === 'owner' && isOwnerConnected;

  if (activeView === 'data') {
    return (
      <div className="app">
        <header className="header">
          <h1>Unit Simulator - Sim Studio</h1>
          <div className="header-controls">
            <div className="header-tabs">
              <button
                className="tab-button"
                onClick={() => setActiveView('sim')}
              >
                Simulation
              </button>
              <button
                className="tab-button active"
                onClick={() => setActiveView('data')}
              >
                Data Editor
              </button>
            </div>
          </div>
        </header>
        <DataEditor apiBaseUrl={API_BASE_URL} />
      </div>
    );
  }

  // Show session selector if not connected to a session
  if (showSessionSelector || selectedSession === undefined) {
    return (
      <div className="app">
        <header className="header">
          <h1>Unit Simulator - Sim Studio</h1>
          <div className="header-controls">
            <div className="header-tabs">
              <button
                className="tab-button active"
                onClick={() => setActiveView('sim')}
              >
                Simulation
              </button>
              <button
                className="tab-button"
                onClick={() => setActiveView('data')}
              >
                Data Editor
              </button>
            </div>
          </div>
        </header>
        <SessionSelector
          apiBaseUrl={API_BASE_URL}
          onSessionSelect={handleSessionSelect}
        />
      </div>
    );
  }

  return (
    <div className="app">
      <header className="header">
        <h1>Unit Simulator - Sim Studio</h1>
        <div className="header-controls">
          <div className="header-tabs">
            <button
              className="tab-button active"
              onClick={() => setActiveView('sim')}
            >
              Simulation
            </button>
            <button
              className="tab-button"
              onClick={() => setActiveView('data')}
            >
              Data Editor
            </button>
          </div>
          <div className="session-info">
            {sessionId && (
              <>
                <span className="session-id" title={sessionId}>
                  Session: {sessionId.substring(0, 8)}...
                </span>
                <span className={`role-badge role-${role}`}>
                  {role}
                </span>
                {!isOwnerConnected && (
                  <span className="owner-warning">Owner disconnected</span>
                )}
                <button className="btn-secondary btn-small" onClick={handleChangeSession}>
                  Change Session
                </button>
              </>
            )}
          </div>
          <div className="connection-status">
            <span
              className={`status-indicator ${connectionStatus}`}
              title={connectionStatus}
            />
            <span>
              {connectionStatus === 'connected' && 'Connected'}
              {connectionStatus === 'disconnected' && 'Disconnected'}
              {connectionStatus === 'connecting' && 'Connecting...'}
            </span>
            {error && <span style={{ color: '#f87171', marginLeft: '1rem' }}>{error}</span>}
          </div>
          <button
            className="btn-secondary"
            onClick={handleDownloadFrameLog}
            disabled={frameLog.length === 0}
            title={frameLog.length === 0 ? 'No frames to download' : `Download ${frameLog.length} frames as JSON`}
          >
            Download Frames ({frameLog.length})
          </button>
        </div>
      </header>

      <main className="main-content">
        <ResizablePanel
          leftPanel={
            <div className="simulation-view">
              <VerticalResizablePanel
                topPanel={
                  <>
                    <div className="panel">
                      <h2>Simulation Frame</h2>
                      {frameData && (
                        <div className="frame-info">
                          <span>Frame: {frameData.frameNumber}</span>
                          <span>Wave: {frameData.currentWave}</span>
                          <span>Friendlies: {frameData.livingFriendlyCount}</span>
                          <span>Enemies: {frameData.livingEnemyCount}</span>
                        </div>
                      )}
                    </div>

                    <div style={{ position: 'relative', display: 'flex', flex: 1, minHeight: 0 }}>
                      <SimulationCanvas
                        ref={canvasRef}
                        frameData={frameData}
                        selectedUnitId={selectedUnitId}
                        selectedFaction={selectedFaction}
                        focusMode={focusMode}
                        onUnitSelect={handleUnitSelect}
                        onCanvasClick={(x, y) => {
                          if (selectedUnit && !selectedUnit.isDead && canControl) {
                            handleSendCommand({
                              type: 'move',
                              unitId: selectedUnit.id,
                              faction: selectedUnit.faction,
                              position: { x, y },
                            });
                          }
                        }}
                        onCameraStateChange={handleCameraStateChange}
                      />
                      <CameraOverlay
                        zoomPercent={cameraState.zoomPercent}
                        isManualMode={cameraState.isManualMode}
                        onResetView={() => canvasRef.current?.resetView()}
                        onResumeAuto={() => {
                          canvasRef.current?.resumeAutoFocus();
                          if (focusMode === 'manual') setFocusMode('auto');
                        }}
                      />
                    </div>
                  </>
                }
                bottomPanel={
                  <>
                    <SimulationControls
                      onPlayPause={handlePlayPause}
                      onStep={handleStep}
                      onStepBack={handleStepBack}
                      onSeek={handleSeek}
                      seekValue={seekFrameInput}
                      onSeekValueChange={setSeekFrameInput}
                      onReset={handleReset}
                      isConnected={connectionStatus === 'connected'}
                      isPlaying={isPlaying}
                      focusMode={focusMode}
                      onFocusModeChange={setFocusMode}
                      disabled={!canControl}
                    />

                    {!canControl && role === 'viewer' && (
                      <div className="viewer-notice">
                        You are viewing as a spectator. Only the session owner can control the simulation.
                      </div>
                    )}
                  </>
                }
                defaultTopHeight={85}
                minTopHeight={60}
                minBottomHeight={10}
              />
            </div>
          }
          rightPanel={
            <div className="sidebar">
              <UnitStateViewer
                frameData={frameData}
                selectedUnitId={selectedUnitId}
                selectedFaction={selectedFaction}
                onUnitSelect={handleUnitSelect}
              />

              <CommandPanel
                selectedUnit={selectedUnit}
                onSendCommand={handleSendCommand}
                isConnected={connectionStatus === 'connected'}
                disabled={!canControl}
              />
            </div>
          }
          defaultLeftWidth={70}
          minLeftWidth={40}
          minRightWidth={20}
        />
      </main>

      <style>{`
        .session-info {
          display: flex;
          align-items: center;
          gap: 0.75rem;
          margin-right: 1rem;
        }

        .session-id {
          font-family: monospace;
          color: #94a3b8;
          font-size: 0.875rem;
        }

        .role-badge {
          padding: 0.25rem 0.5rem;
          border-radius: 4px;
          font-size: 0.75rem;
          font-weight: 500;
          text-transform: uppercase;
        }

        .role-owner {
          background: #166534;
          color: #86efac;
        }

        .role-viewer {
          background: #1e3a8a;
          color: #93c5fd;
        }

        .owner-warning {
          color: #facc15;
          font-size: 0.75rem;
        }

        .btn-small {
          padding: 0.25rem 0.5rem;
          font-size: 0.75rem;
        }

        .viewer-notice {
          background: #1e3a8a;
          color: #93c5fd;
          padding: 0.75rem;
          border-radius: 4px;
          text-align: center;
          font-size: 0.875rem;
          margin-top: 0.5rem;
        }
      `}</style>
    </div>
  );
}

export default App;
