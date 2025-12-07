import { useState, useCallback } from 'react';
import { Command, UnitStateData } from '../types';

interface CommandPanelProps {
  selectedUnit: UnitStateData | null | undefined;
  onSendCommand: (command: Command) => void;
  isConnected: boolean;
}

function CommandPanel({
  selectedUnit,
  onSendCommand,
  isConnected,
}: CommandPanelProps) {
  const [moveX, setMoveX] = useState('');
  const [moveY, setMoveY] = useState('');
  const [newHealth, setNewHealth] = useState('');

  const handleMoveSubmit = useCallback((e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedUnit || selectedUnit.isDead) return;

    const x = parseFloat(moveX);
    const y = parseFloat(moveY);

    if (isNaN(x) || isNaN(y)) return;

    onSendCommand({
      type: 'move',
      unitId: selectedUnit.id,
      faction: selectedUnit.faction,
      position: { x, y },
    });

    setMoveX('');
    setMoveY('');
  }, [selectedUnit, moveX, moveY, onSendCommand]);

  const handleSetHealth = useCallback(() => {
    if (!selectedUnit) return;

    const hp = parseInt(newHealth, 10);
    if (isNaN(hp)) return;

    onSendCommand({
      type: 'set_health',
      unitId: selectedUnit.id,
      faction: selectedUnit.faction,
      health: hp,
    });

    setNewHealth('');
  }, [selectedUnit, newHealth, onSendCommand]);

  const handleKill = useCallback(() => {
    if (!selectedUnit || selectedUnit.isDead) return;

    onSendCommand({
      type: 'kill',
      unitId: selectedUnit.id,
      faction: selectedUnit.faction,
    });
  }, [selectedUnit, onSendCommand]);

  const handleRevive = useCallback(() => {
    if (!selectedUnit || !selectedUnit.isDead) return;

    onSendCommand({
      type: 'revive',
      unitId: selectedUnit.id,
      faction: selectedUnit.faction,
      health: 100,
    });
  }, [selectedUnit, onSendCommand]);

  const isDisabled = !isConnected || !selectedUnit;

  return (
    <div className="panel command-panel">
      <h2>Command Input</h2>
      
      {!selectedUnit ? (
        <div className="empty-state">
          Select a unit to send commands
        </div>
      ) : (
        <div className="command-form">
          <div style={{ marginBottom: '0.75rem', color: '#9ca3af' }}>
            Selected: <strong style={{ color: selectedUnit.faction === 'Friendly' ? '#4ade80' : '#f87171' }}>
              {selectedUnit.label}
            </strong>
            {selectedUnit.isDead && <span style={{ color: '#f87171', marginLeft: '0.5rem' }}>(Dead)</span>}
          </div>

          <form onSubmit={handleMoveSubmit}>
            <div className="form-group">
              <label>Move to Position</label>
              <div className="form-row">
                <input
                  type="number"
                  placeholder="X"
                  value={moveX}
                  onChange={(e) => setMoveX(e.target.value)}
                  disabled={isDisabled || selectedUnit.isDead}
                />
                <input
                  type="number"
                  placeholder="Y"
                  value={moveY}
                  onChange={(e) => setMoveY(e.target.value)}
                  disabled={isDisabled || selectedUnit.isDead}
                />
              </div>
              <button
                type="submit"
                className="btn-primary"
                disabled={isDisabled || selectedUnit.isDead || !moveX || !moveY}
                style={{ marginTop: '0.5rem' }}
              >
                Move Unit
              </button>
            </div>
          </form>

          <div className="form-group">
            <label>Set Health</label>
            <div className="form-row">
              <input
                type="number"
                placeholder="New HP"
                value={newHealth}
                onChange={(e) => setNewHealth(e.target.value)}
                disabled={isDisabled}
                min="0"
                max="100"
              />
              <button
                type="button"
                className="btn-primary"
                onClick={handleSetHealth}
                disabled={isDisabled || !newHealth}
              >
                Set
              </button>
            </div>
          </div>

          <div className="form-group">
            <label>Quick Actions</label>
            <div className="form-row">
              {selectedUnit.isDead ? (
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={handleRevive}
                  disabled={!isConnected}
                  style={{ flex: 1 }}
                >
                  Revive Unit
                </button>
              ) : (
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={handleKill}
                  disabled={!isConnected}
                  style={{ flex: 1 }}
                >
                  Kill Unit
                </button>
              )}
            </div>
          </div>

          <div style={{ marginTop: '1rem', fontSize: '0.75rem', color: '#6b7280' }}>
            <p>Tip: Click on the canvas to move the selected unit to that position.</p>
          </div>
        </div>
      )}
    </div>
  );
}

export default CommandPanel;
