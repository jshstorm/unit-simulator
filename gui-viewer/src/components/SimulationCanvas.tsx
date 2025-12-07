import { useEffect, useRef, useCallback } from 'react';
import { FrameData, UnitStateData } from '../types';

interface SimulationCanvasProps {
  frameData: FrameData | null;
  selectedUnitId: number | null;
  selectedFaction: 'Friendly' | 'Enemy' | null;
  onUnitSelect: (unit: UnitStateData | null) => void;
  onCanvasClick: (x: number, y: number) => void;
}

const CANVAS_WIDTH = 1200;
const CANVAS_HEIGHT = 500;
const SCALE_X = CANVAS_WIDTH / 1200; // Assuming simulation width is 1200
const SCALE_Y = CANVAS_HEIGHT / 720;  // Assuming simulation height is 720

function SimulationCanvas({
  frameData,
  selectedUnitId,
  selectedFaction,
  onUnitSelect,
  onCanvasClick,
}: SimulationCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  const getCanvasCoordinates = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return { x: 0, y: 0 };

    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;

    return {
      x: ((e.clientX - rect.left) * scaleX) / SCALE_X,
      y: ((e.clientY - rect.top) * scaleY) / SCALE_Y,
    };
  }, []);

  const findUnitAtPosition = useCallback((x: number, y: number): UnitStateData | null => {
    if (!frameData) return null;

    const allUnits = [...frameData.friendlyUnits, ...frameData.enemyUnits];
    
    for (const unit of allUnits) {
      const distance = Math.sqrt(
        Math.pow(unit.position.x - x, 2) + Math.pow(unit.position.y - y, 2)
      );
      if (distance <= unit.radius + 5) {
        return unit;
      }
    }
    
    return null;
  }, [frameData]);

  const handleCanvasClick = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    const { x, y } = getCanvasCoordinates(e);
    const unit = findUnitAtPosition(x, y);

    if (unit) {
      onUnitSelect(unit);
    } else if (selectedUnitId !== null) {
      // If a unit is selected and we clicked on empty space, send move command
      onCanvasClick(x, y);
    }
  }, [getCanvasCoordinates, findUnitAtPosition, selectedUnitId, onUnitSelect, onCanvasClick]);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.fillStyle = '#0f0f23';
    ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);

    if (!frameData) {
      // Draw "waiting for data" message
      ctx.fillStyle = '#6b7280';
      ctx.font = '16px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText('Waiting for simulation data...', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2);
      return;
    }

    // Draw grid
    ctx.strokeStyle = '#1f2937';
    ctx.lineWidth = 1;
    for (let x = 0; x < CANVAS_WIDTH; x += 50) {
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, CANVAS_HEIGHT);
      ctx.stroke();
    }
    for (let y = 0; y < CANVAS_HEIGHT; y += 50) {
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(CANVAS_WIDTH, y);
      ctx.stroke();
    }

    // Draw main target
    const targetX = frameData.mainTarget.x * SCALE_X;
    const targetY = frameData.mainTarget.y * SCALE_Y;
    ctx.beginPath();
    ctx.arc(targetX, targetY, 15, 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(233, 69, 96, 0.3)';
    ctx.fill();
    ctx.strokeStyle = '#e94560';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Draw target cross
    ctx.beginPath();
    ctx.moveTo(targetX - 10, targetY);
    ctx.lineTo(targetX + 10, targetY);
    ctx.moveTo(targetX, targetY - 10);
    ctx.lineTo(targetX, targetY + 10);
    ctx.stroke();

    // Draw units
    const drawUnit = (unit: UnitStateData) => {
      const x = unit.position.x * SCALE_X;
      const y = unit.position.y * SCALE_Y;
      const radius = unit.radius * Math.min(SCALE_X, SCALE_Y);
      const isSelected = unit.id === selectedUnitId && unit.faction === selectedFaction;

      // Skip dead units (draw them faded)
      if (unit.isDead) {
        ctx.globalAlpha = 0.3;
      }

      // Draw selection ring
      if (isSelected) {
        ctx.beginPath();
        ctx.arc(x, y, radius + 6, 0, Math.PI * 2);
        ctx.strokeStyle = '#e94560';
        ctx.lineWidth = 3;
        ctx.stroke();
      }

      // Draw attack range for selected unit
      if (isSelected && !unit.isDead) {
        ctx.beginPath();
        ctx.arc(x, y, unit.attackRange * Math.min(SCALE_X, SCALE_Y), 0, Math.PI * 2);
        ctx.strokeStyle = 'rgba(233, 69, 96, 0.3)';
        ctx.lineWidth = 1;
        ctx.stroke();
      }

      // Draw unit body
      ctx.beginPath();
      ctx.arc(x, y, radius, 0, Math.PI * 2);
      
      if (unit.faction === 'Friendly') {
        ctx.fillStyle = unit.role === 'Melee' ? '#22c55e' : '#3b82f6';
      } else {
        ctx.fillStyle = unit.role === 'Melee' ? '#ef4444' : '#f97316';
      }
      ctx.fill();

      // Draw border
      ctx.strokeStyle = unit.faction === 'Friendly' ? '#4ade80' : '#f87171';
      ctx.lineWidth = 2;
      ctx.stroke();

      // Draw forward direction
      if (!unit.isDead) {
        const fwdLength = radius + 8;
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.lineTo(
          x + unit.forward.x * fwdLength,
          y + unit.forward.y * fwdLength
        );
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2;
        ctx.stroke();
      }

      // Draw unit label
      ctx.fillStyle = '#fff';
      ctx.font = 'bold 10px sans-serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(unit.label, x, y);

      // Draw health bar
      const healthBarWidth = radius * 2;
      const healthBarHeight = 4;
      const healthBarY = y - radius - 8;
      const healthPercent = unit.hp / 100; // Assuming max HP is 100

      ctx.fillStyle = '#333';
      ctx.fillRect(x - healthBarWidth / 2, healthBarY, healthBarWidth, healthBarHeight);

      let healthColor = '#4ade80';
      if (healthPercent < 0.3) healthColor = '#f87171';
      else if (healthPercent < 0.6) healthColor = '#fbbf24';

      ctx.fillStyle = healthColor;
      ctx.fillRect(x - healthBarWidth / 2, healthBarY, healthBarWidth * healthPercent, healthBarHeight);

      // Draw destination line for selected unit
      if (isSelected && !unit.isDead && unit.isMoving) {
        ctx.beginPath();
        ctx.setLineDash([5, 5]);
        ctx.moveTo(x, y);
        ctx.lineTo(
          unit.currentDestination.x * SCALE_X,
          unit.currentDestination.y * SCALE_Y
        );
        ctx.strokeStyle = 'rgba(255, 255, 255, 0.5)';
        ctx.lineWidth = 1;
        ctx.stroke();
        ctx.setLineDash([]);
      }

      ctx.globalAlpha = 1;
    };

    // Draw all units (enemies first, then friendlies so friendlies are on top)
    frameData.enemyUnits.forEach(drawUnit);
    frameData.friendlyUnits.forEach(drawUnit);

  }, [frameData, selectedUnitId, selectedFaction]);

  return (
    <canvas
      ref={canvasRef}
      className="simulation-canvas"
      width={CANVAS_WIDTH}
      height={CANVAS_HEIGHT}
      onClick={handleCanvasClick}
    />
  );
}

export default SimulationCanvas;
