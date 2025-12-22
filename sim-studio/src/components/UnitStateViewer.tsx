import { FrameData, UnitStateData } from '../types';

interface UnitStateViewerProps {
  frameData: FrameData | null;
  selectedUnitId: number | null;
  selectedFaction: 'Friendly' | 'Enemy' | null;
  onUnitSelect: (unit: UnitStateData | null) => void;
}

function UnitStateViewer({
  frameData,
  selectedUnitId,
  selectedFaction,
  onUnitSelect,
}: UnitStateViewerProps) {
  if (!frameData) {
    return (
      <div className="panel">
        <h2>Unit State Viewer</h2>
        <div className="empty-state">No data available</div>
      </div>
    );
  }

  const renderUnitItem = (unit: UnitStateData) => {
    const isSelected = unit.id === selectedUnitId && unit.faction === selectedFaction;
    const healthPercent = unit.hp / 100; // Assuming max HP is 100
    
    let healthBarClass = '';
    if (healthPercent < 0.3) healthBarClass = 'critical';
    else if (healthPercent < 0.6) healthBarClass = 'low';

    return (
      <div
        key={`${unit.faction}-${unit.id}`}
        className={`unit-item ${unit.faction.toLowerCase()} ${unit.isDead ? 'dead' : ''} ${isSelected ? 'selected' : ''}`}
        onClick={() => onUnitSelect(isSelected ? null : unit)}
      >
        <div className="unit-header">
          <span className="unit-label">{unit.label}</span>
          <span className="unit-role">{unit.role}</span>
        </div>
        <div className="unit-stats">
          <span>HP: {unit.hp}</span>
          <span>Pos: ({Math.round(unit.position.x)}, {Math.round(unit.position.y)})</span>
          {unit.isDead && <span style={{ color: '#f87171' }}>DEAD</span>}
          {unit.isMoving && !unit.isDead && <span style={{ color: '#fbbf24' }}>Moving</span>}
          {unit.inAttackRange && !unit.isDead && <span style={{ color: '#ef4444' }}>In Range</span>}
        </div>
        <div className="health-bar">
          <div 
            className={`health-bar-fill ${healthBarClass}`}
            style={{ width: `${healthPercent * 100}%` }}
          />
        </div>
      </div>
    );
  };

  return (
    <div className="panel">
      <h2>Unit State Viewer</h2>
      
      <h3 style={{ fontSize: '0.875rem', color: '#4ade80', marginBottom: '0.5rem' }}>
        Friendly Units ({frameData.livingFriendlyCount} alive)
      </h3>
      <div className="unit-list">
        {frameData.friendlyUnits.map(renderUnitItem)}
      </div>

      <h3 style={{ fontSize: '0.875rem', color: '#f87171', marginTop: '1rem', marginBottom: '0.5rem' }}>
        Enemy Units ({frameData.livingEnemyCount} alive)
      </h3>
      <div className="unit-list">
        {frameData.enemyUnits.map(renderUnitItem)}
      </div>
    </div>
  );
}

export default UnitStateViewer;
