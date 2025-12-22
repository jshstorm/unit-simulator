import { useEffect, useRef, useCallback, useState } from 'react';
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
const WORLD_WIDTH = 1200;
const WORLD_HEIGHT = 720;
const BASE_SCALE = Math.min(CANVAS_WIDTH / WORLD_WIDTH, CANVAS_HEIGHT / WORLD_HEIGHT); // uniform scale to avoid distortion
const MIN_ZOOM = 0.5;
const MAX_ZOOM = 3;

function SimulationCanvas({
  frameData,
  selectedUnitId,
  selectedFaction,
  onUnitSelect,
  onCanvasClick,
}: SimulationCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const initialPanX = (CANVAS_WIDTH - WORLD_WIDTH * BASE_SCALE) / 2;
  const initialPanY = (CANVAS_HEIGHT - WORLD_HEIGHT * BASE_SCALE) / 2;
  const [view, setView] = useState({ zoom: 1, panX: initialPanX, panY: initialPanY });
  const viewRef = useRef(view);
  const isPanningRef = useRef(false);
  const lastPosRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });

  const updateView = (next: typeof view) => {
    viewRef.current = next;
    setView(next);
  };

  const getCanvasPixelCoords = useCallback((clientX: number, clientY: number) => {
    const canvas = canvasRef.current;
    if (!canvas) return { x: 0, y: 0 };

    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;

    return {
      x: (clientX - rect.left) * scaleX,
      y: (clientY - rect.top) * scaleY,
    };
  }, []);

  const canvasToWorld = useCallback((canvasX: number, canvasY: number) => {
    const { zoom, panX, panY } = viewRef.current;
    return {
      x: (canvasX - panX) / (BASE_SCALE * zoom),
      y: (canvasY - panY) / (BASE_SCALE * zoom),
    };
  }, []);

  const getWorldCoordinates = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    const { x, y } = getCanvasPixelCoords(e.clientX, e.clientY);
    return canvasToWorld(x, y);
  }, [canvasToWorld, getCanvasPixelCoords]);

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
    const { x, y } = getWorldCoordinates(e);
    const unit = findUnitAtPosition(x, y);

    if (unit) {
      onUnitSelect(unit);
    } else if (selectedUnitId !== null) {
      onCanvasClick(x, y);
    }
  }, [getWorldCoordinates, findUnitAtPosition, selectedUnitId, onUnitSelect, onCanvasClick]);

  const handleWheel = useCallback((e: React.WheelEvent<HTMLCanvasElement>) => {
    e.preventDefault();
    const { x: canvasX, y: canvasY } = getCanvasPixelCoords(e.clientX, e.clientY);
    const { zoom } = viewRef.current;

    const worldPos = canvasToWorld(canvasX, canvasY);

    const zoomFactor = e.deltaY < 0 ? 1.1 : 0.9;
    const newZoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom * zoomFactor));

    const newPanX = canvasX - worldPos.x * BASE_SCALE * newZoom;
    const newPanY = canvasY - worldPos.y * BASE_SCALE * newZoom;

    updateView({ zoom: newZoom, panX: newPanX, panY: newPanY });
  }, [canvasToWorld, getCanvasPixelCoords]);

  const handleMouseDown = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    isPanningRef.current = true;
    lastPosRef.current = getCanvasPixelCoords(e.clientX, e.clientY);
  }, [getCanvasPixelCoords]);

  const handleMouseMove = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!isPanningRef.current) return;
    const pos = getCanvasPixelCoords(e.clientX, e.clientY);
    const last = lastPosRef.current;
    const dx = pos.x - last.x;
    const dy = pos.y - last.y;

    lastPosRef.current = pos;

    const { zoom, panX, panY } = viewRef.current;
    updateView({ zoom, panX: panX + dx, panY: panY + dy });
  }, [getCanvasPixelCoords]);

  const handleMouseUpOrLeave = useCallback(() => {
    isPanningRef.current = false;
  }, []);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.fillStyle = '#0f0f23';
    ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);

    if (!frameData) {
      ctx.fillStyle = '#6b7280';
      ctx.font = '16px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText('Waiting for simulation data...', CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2);
      return;
    }

    const { zoom, panX, panY } = view;

    // Apply pan/zoom (world space -> canvas)
    ctx.translate(panX, panY);
    ctx.scale(BASE_SCALE * zoom, BASE_SCALE * zoom);

    // Draw grid
    ctx.strokeStyle = '#1f2937';
    ctx.lineWidth = 1 / (BASE_SCALE * zoom);

    // Determine visible world bounds for grid tiling
    const invScale = 1 / (BASE_SCALE * zoom);
    const minWorldX = -panX * invScale;
    const minWorldY = -panY * invScale;
    const maxWorldX = minWorldX + CANVAS_WIDTH * invScale;
    const maxWorldY = minWorldY + CANVAS_HEIGHT * invScale;

    const gridStartX = Math.floor(minWorldX / 50) * 50;
    const gridEndX = Math.ceil(maxWorldX / 50) * 50;
    const gridStartY = Math.floor(minWorldY / 50) * 50;
    const gridEndY = Math.ceil(maxWorldY / 50) * 50;

    for (let x = gridStartX; x <= gridEndX; x += 50) {
      ctx.beginPath();
      ctx.moveTo(x, minWorldY);
      ctx.lineTo(x, maxWorldY);
      ctx.stroke();
    }
    for (let y = gridStartY; y <= gridEndY; y += 50) {
      ctx.beginPath();
      ctx.moveTo(minWorldX, y);
      ctx.lineTo(maxWorldX, y);
      ctx.stroke();
    }

    // World border
    ctx.strokeStyle = '#374151';
    ctx.lineWidth = 2 / (BASE_SCALE * zoom);
    ctx.strokeRect(0, 0, WORLD_WIDTH, WORLD_HEIGHT);

    // Draw main target
    const targetX = frameData.mainTarget.x;
    const targetY = frameData.mainTarget.y;
    ctx.beginPath();
    ctx.arc(targetX, targetY, 15, 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(233, 69, 96, 0.3)';
    ctx.fill();
    ctx.strokeStyle = '#e94560';
    ctx.lineWidth = 2 / (BASE_SCALE * zoom);
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
      const x = unit.position.x;
      const y = unit.position.y;
      const radius = unit.radius;
      const isSelected = unit.id === selectedUnitId && unit.faction === selectedFaction;

      if (unit.isDead) {
        ctx.globalAlpha = 0.3;
      }

      if (isSelected) {
        ctx.beginPath();
        ctx.arc(x, y, radius + 6, 0, Math.PI * 2);
        ctx.strokeStyle = '#e94560';
        ctx.lineWidth = 3 / (BASE_SCALE * zoom);
        ctx.stroke();
      }

      if (isSelected && !unit.isDead) {
        ctx.beginPath();
        ctx.arc(x, y, unit.attackRange, 0, Math.PI * 2);
        ctx.strokeStyle = 'rgba(233, 69, 96, 0.3)';
        ctx.lineWidth = 1 / (BASE_SCALE * zoom);
        ctx.stroke();
      }

      ctx.beginPath();
      ctx.arc(x, y, radius, 0, Math.PI * 2);
      if (unit.faction === 'Friendly') {
        ctx.fillStyle = unit.role === 'Melee' ? '#22c55e' : '#3b82f6';
      } else {
        ctx.fillStyle = unit.role === 'Melee' ? '#ef4444' : '#f97316';
      }
      ctx.fill();

      ctx.strokeStyle = unit.faction === 'Friendly' ? '#4ade80' : '#f87171';
      ctx.lineWidth = 2 / (BASE_SCALE * zoom);
      ctx.stroke();

      if (!unit.isDead) {
        const fwdLength = radius + 8;
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.lineTo(
          x + unit.forward.x * fwdLength,
          y + unit.forward.y * fwdLength
        );
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2 / (BASE_SCALE * zoom);
        ctx.stroke();
      }

      ctx.fillStyle = '#fff';
      ctx.font = 'bold 10px sans-serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(unit.label, x, y);

      const healthBarWidth = radius * 2;
      const healthBarHeight = 4;
      const healthBarY = y - radius - 8;
      const healthPercent = unit.hp / 100;

      ctx.fillStyle = '#333';
      ctx.fillRect(x - healthBarWidth / 2, healthBarY, healthBarWidth, healthBarHeight);

      let healthColor = '#4ade80';
      if (healthPercent < 0.3) healthColor = '#f87171';
      else if (healthPercent < 0.6) healthColor = '#fbbf24';

      ctx.fillStyle = healthColor;
      ctx.fillRect(x - healthBarWidth / 2, healthBarY, healthBarWidth * healthPercent, healthBarHeight);

      if (isSelected && !unit.isDead && unit.isMoving) {
        ctx.beginPath();
        ctx.setLineDash([5, 5]);
        ctx.moveTo(x, y);
        ctx.lineTo(
          unit.currentDestination.x,
          unit.currentDestination.y
        );
        ctx.strokeStyle = 'rgba(255, 255, 255, 0.5)';
        ctx.lineWidth = 1 / (BASE_SCALE * zoom);
        ctx.stroke();
        ctx.setLineDash([]);
      }

      ctx.globalAlpha = 1;
    };

    frameData.enemyUnits.forEach(drawUnit);
    frameData.friendlyUnits.forEach(drawUnit);
  }, [frameData, selectedUnitId, selectedFaction, view]);

  return (
    <canvas
      ref={canvasRef}
      className="simulation-canvas"
      width={CANVAS_WIDTH}
      height={CANVAS_HEIGHT}
      onClick={handleCanvasClick}
      onWheel={handleWheel}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUpOrLeave}
      onMouseLeave={handleMouseUpOrLeave}
      style={{ cursor: isPanningRef.current ? 'grabbing' : 'grab' }}
    />
  );
}

export default SimulationCanvas;
