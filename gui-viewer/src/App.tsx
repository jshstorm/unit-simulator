import { useState, useCallback } from 'react';
import { Command, UnitStateData } from './types';
import { useWebSocket } from './hooks/useWebSocket';
import SimulationCanvas from './components/SimulationCanvas';
import UnitStateViewer from './components/UnitStateViewer';
import CommandPanel from './components/CommandPanel';
import SimulationControls from './components/SimulationControls';

function App() {
  const [selectedUnitId, setSelectedUnitId] = useState<number | null>(null);
  const [selectedFaction, setSelectedFaction] = useState<'Friendly' | 'Enemy' | null>(null);

  const { 
    frameData, 
    connectionStatus, 
    sendCommand, 
    error 
  } = useWebSocket('ws://localhost:5000/ws');

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

  const selectedUnit = frameData
    ? [...frameData.friendlyUnits, ...frameData.enemyUnits].find(
        u => u.id === selectedUnitId && u.faction === selectedFaction
      )
    : null;

  return (
    <div className="app">
      <header className="header">
        <h1>Unit Simulator - GUI Viewer</h1>
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
      </header>

      <main className="main-content">
        <div className="simulation-view">
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

          <SimulationCanvas
            frameData={frameData}
            selectedUnitId={selectedUnitId}
            selectedFaction={selectedFaction}
            onUnitSelect={handleUnitSelect}
            onCanvasClick={(x, y) => {
              if (selectedUnit && !selectedUnit.isDead) {
                handleSendCommand({
                  type: 'move',
                  unitId: selectedUnit.id,
                  faction: selectedUnit.faction,
                  position: { x, y },
                });
              }
            }}
          />

          <SimulationControls
            onStart={() => handleSendCommand({ type: 'start' })}
            onStop={() => handleSendCommand({ type: 'stop' })}
            onStep={() => handleSendCommand({ type: 'step' })}
            onReset={() => handleSendCommand({ type: 'reset' })}
            isConnected={connectionStatus === 'connected'}
          />
        </div>

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
          />
        </div>
      </main>
    </div>
  );
}

export default App;
