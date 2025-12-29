import { useState, useCallback } from 'react';
import { Command, UnitStateData, UnitRole } from '../types';

interface CommandPanelProps {
  selectedUnit: UnitStateData | null | undefined;
  onSendCommand: (command: Command) => void;
  isConnected: boolean;
  disabled?: boolean;  // True if user doesn't have control permission
}

function CommandPanel({
  selectedUnit,
  onSendCommand,
  isConnected,
  disabled = false,
}: CommandPanelProps) {
  const [moveX, setMoveX] = useState('');
  const [moveY, setMoveY] = useState('');
  const [newHealth, setNewHealth] = useState('');

  // Spawn unit state
  const [spawnX, setSpawnX] = useState('1600');
  const [spawnY, setSpawnY] = useState('2550');
  const [spawnFaction, setSpawnFaction] = useState<'Friendly' | 'Enemy'>('Friendly');
  const [spawnRole, setSpawnRole] = useState<UnitRole>('Melee');
  const [spawnHp, setSpawnHp] = useState('');

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

  const handleSpawn = useCallback((e: React.FormEvent) => {
    e.preventDefault();
    const x = parseFloat(spawnX);
    const y = parseFloat(spawnY);
    if (isNaN(x) || isNaN(y)) return;

    const hp = spawnHp ? parseInt(spawnHp, 10) : undefined;

    onSendCommand({
      type: 'spawn',
      position: { x, y },
      faction: spawnFaction,
      role: spawnRole,
      hp: hp,
    });
  }, [spawnX, spawnY, spawnFaction, spawnRole, spawnHp, onSendCommand]);

  const isDisabled = !isConnected || !selectedUnit || disabled;
  const isSpawnDisabled = !isConnected || disabled;

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

      <div className="spawn-section" style={{ marginTop: '1.5rem', paddingTop: '1rem', borderTop: '1px solid #374151' }}>
        <h3 style={{ marginBottom: '0.75rem', color: '#e5e7eb' }}>Spawn Unit</h3>
        <form onSubmit={handleSpawn}>
          <div className="form-group">
            <label>Position</label>
            <div className="form-row">
              <input
                type="number"
                placeholder="X"
                value={spawnX}
                onChange={(e) => setSpawnX(e.target.value)}
                disabled={isSpawnDisabled}
              />
              <input
                type="number"
                placeholder="Y"
                value={spawnY}
                onChange={(e) => setSpawnY(e.target.value)}
                disabled={isSpawnDisabled}
              />
            </div>
          </div>
          <div className="form-group">
            <label>Faction</label>
            <div className="form-row">
              <button
                type="button"
                className={`btn-toggle ${spawnFaction === 'Friendly' ? 'active friendly' : ''}`}
                onClick={() => setSpawnFaction('Friendly')}
                disabled={isSpawnDisabled}
              >
                Friendly
              </button>
              <button
                type="button"
                className={`btn-toggle ${spawnFaction === 'Enemy' ? 'active enemy' : ''}`}
                onClick={() => setSpawnFaction('Enemy')}
                disabled={isSpawnDisabled}
              >
                Enemy
              </button>
            </div>
          </div>
          <div className="form-group">
            <label>Role</label>
            <div className="form-row">
              <button
                type="button"
                className={`btn-toggle ${spawnRole === 'Melee' ? 'active' : ''}`}
                onClick={() => setSpawnRole('Melee')}
                disabled={isSpawnDisabled}
              >
                Melee
              </button>
              <button
                type="button"
                className={`btn-toggle ${spawnRole === 'Ranged' ? 'active' : ''}`}
                onClick={() => setSpawnRole('Ranged')}
                disabled={isSpawnDisabled}
              >
                Ranged
              </button>
            </div>
          </div>
          <div className="form-group">
            <label>HP (optional)</label>
            <input
              type="number"
              placeholder="Default HP"
              value={spawnHp}
              onChange={(e) => setSpawnHp(e.target.value)}
              disabled={isSpawnDisabled}
              min="1"
            />
          </div>
          <button
            type="submit"
            className="btn-primary spawn-btn"
            disabled={isSpawnDisabled || !spawnX || !spawnY}
            style={{ width: '100%', marginTop: '0.5rem' }}
          >
            Spawn {spawnFaction} {spawnRole}
          </button>
        </form>
      </div>

      <style>{`
        .btn-toggle {
          flex: 1;
          padding: 0.5rem;
          background: #374151;
          border: 2px solid transparent;
          color: #9ca3af;
          cursor: pointer;
          border-radius: 4px;
          font-size: 0.875rem;
          transition: all 0.15s;
        }
        .btn-toggle:hover:not(:disabled) {
          background: #4b5563;
        }
        .btn-toggle.active {
          border-color: #3b82f6;
          color: #e5e7eb;
          background: #1e3a5f;
        }
        .btn-toggle.active.friendly {
          border-color: #4ade80;
          background: #14532d;
        }
        .btn-toggle.active.enemy {
          border-color: #f87171;
          background: #7f1d1d;
        }
        .btn-toggle:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
        .spawn-btn {
          font-weight: 500;
        }
      `}</style>
    </div>
  );
}

export default CommandPanel;
