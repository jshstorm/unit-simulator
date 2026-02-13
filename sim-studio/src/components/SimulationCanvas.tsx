import { useEffect, useRef, useCallback, useState, forwardRef, useImperativeHandle } from 'react';
import { CameraFocusMode, FrameData, UnitStateData } from '../types';
import { useCamera, WORLD_WIDTH, WORLD_HEIGHT } from '../hooks/useCamera';

export interface SimulationCanvasHandle {
  resetView: () => void;
  resumeAutoFocus: () => void;
}

interface SimulationCanvasProps {
  frameData: FrameData | null;
  selectedUnitId: number | null;
  selectedFaction: 'Friendly' | 'Enemy' | null;
  focusMode: CameraFocusMode;
  onUnitSelect: (unit: UnitStateData | null) => void;
  onCanvasClick: (x: number, y: number) => void;
  onCameraStateChange?: (state: { zoomPercent: number; isManualMode: boolean }) => void;
}

const DEFAULT_CANVAS_WIDTH = 1200;
const DEFAULT_CANVAS_HEIGHT = 800;

const UNIT_ICON_PATHS: Record<string, string> = {
  skeleton: '/assets/previews/units/skeleton.svg',
  golemite: '/assets/previews/units/golemite.svg',
  lava_pup: '/assets/previews/units/lava_pup.svg',
  minion: '/assets/previews/units/minion.svg',
  bat: '/assets/previews/units/bat.svg',
  elixir_golemite: '/assets/previews/units/elixir_golemite.svg',
  elixir_blob: '/assets/previews/units/elixir_blob.svg',
  guard: '/assets/previews/units/guard.svg',
  knight: '/assets/previews/units/knight.svg',
  prince: '/assets/previews/units/prince.svg',
  baby_dragon: '/assets/previews/units/baby_dragon.svg',
  golem: '/assets/previews/units/golem.svg',
  lava_hound: '/assets/previews/units/lava_hound.svg',
};

const TOWER_IMAGE_PATHS: Record<string, string> = {
  king_1: '/assets/towers/fire/level1.png',
  king_2: '/assets/towers/fire/level2.png',
  king_3: '/assets/towers/fire/level3.png',
  princess_1: '/assets/towers/stone/level1.png',
  princess_2: '/assets/towers/stone/level2.png',
  princess_3: '/assets/towers/stone/level3.png',
  destroyed: '/assets/towers/destroyed/tower.png',
  range_indicator: '/assets/towers/ui/range_dotted.png',
};

const SimulationCanvas = forwardRef<SimulationCanvasHandle, SimulationCanvasProps>(function SimulationCanvas(
  {
    frameData,
    selectedUnitId,
    selectedFaction,
    focusMode,
    onUnitSelect,
    onCanvasClick,
    onCameraStateChange,
  },
  ref,
) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [canvasSize, setCanvasSize] = useState({
    width: DEFAULT_CANVAS_WIDTH,
    height: DEFAULT_CANVAS_HEIGHT,
  });
  const unitIconMapRef = useRef(new Map<string, HTMLImageElement>());
  const towerImageMapRef = useRef(new Map<string, HTMLImageElement>());
  const [iconVersion, setIconVersion] = useState(0);
  const didDragRef = useRef(false);

  const camera = useCamera({
    canvasSize,
    focusMode,
    frameData,
    selectedUnitId,
    selectedFaction,
  });

  // Expose imperative methods
  useImperativeHandle(ref, () => ({
    resetView: camera.resetToFit,
    resumeAutoFocus: camera.resumeAutoFocus,
  }), [camera.resetToFit, camera.resumeAutoFocus]);

  // Notify parent of camera state changes
  useEffect(() => {
    onCameraStateChange?.({ zoomPercent: camera.zoomPercent, isManualMode: camera.isManualMode });
  }, [camera.zoomPercent, camera.isManualMode, onCameraStateChange]);

  // ResizeObserver
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const updateSize = () => {
      const rect = canvas.getBoundingClientRect();
      const nextWidth = Math.max(1, Math.round(rect.width));
      const nextHeight = Math.max(1, Math.round(rect.height));
      setCanvasSize((prev) =>
        prev.width === nextWidth && prev.height === nextHeight
          ? prev
          : { width: nextWidth, height: nextHeight },
      );
    };

    updateSize();
    const observer = new ResizeObserver(updateSize);
    observer.observe(canvas);
    return () => observer.disconnect();
  }, []);

  // Load unit icons
  useEffect(() => {
    const map = unitIconMapRef.current;
    Object.entries(UNIT_ICON_PATHS).forEach(([unitId, src]) => {
      if (map.has(unitId)) return;
      const img = new Image();
      img.onload = () => setIconVersion((prev) => prev + 1);
      img.src = src;
      map.set(unitId, img);
    });
  }, []);

  // Load tower images
  useEffect(() => {
    const map = towerImageMapRef.current;
    Object.entries(TOWER_IMAGE_PATHS).forEach(([key, src]) => {
      if (map.has(key)) return;
      const img = new Image();
      img.onload = () => setIconVersion((prev) => prev + 1);
      img.src = src;
      map.set(key, img);
    });
  }, []);

  // Sync canvas element size
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    canvas.width = canvasSize.width;
    canvas.height = canvasSize.height;
  }, [canvasSize.width, canvasSize.height]);

  // --- Canvas pixel coords from client coords ---
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

  const getWorldCoordinates = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    const { x, y } = getCanvasPixelCoords(e.clientX, e.clientY);
    return camera.canvasToWorld(x, y);
  }, [camera.canvasToWorld, getCanvasPixelCoords]);

  // --- Unit hit-testing ---
  const findUnitAtPosition = useCallback((x: number, y: number): UnitStateData | null => {
    if (!frameData) return null;

    const allUnits = [...frameData.friendlyUnits, ...frameData.enemyUnits];
    for (const unit of allUnits) {
      const distance = Math.sqrt(
        Math.pow(unit.position.x - x, 2) + Math.pow(unit.position.y - y, 2),
      );
      if (distance <= unit.radius + 5) return unit;
    }
    return null;
  }, [frameData]);

  // --- Click handler (skip if dragged) ---
  const handleCanvasClick = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    if (didDragRef.current) return;

    const { x, y } = getWorldCoordinates(e);
    const unit = findUnitAtPosition(x, y);

    if (unit) {
      onUnitSelect(unit);
    } else if (selectedUnitId !== null) {
      onCanvasClick(x, y);
    }
  }, [getWorldCoordinates, findUnitAtPosition, selectedUnitId, onUnitSelect, onCanvasClick]);

  // --- Wheel (passive: false) ---
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      const { x, y } = getCanvasPixelCoords(e.clientX, e.clientY);
      camera.handleWheelZoom(x, y, e.deltaY);
    };

    canvas.addEventListener('wheel', onWheel, { passive: false });
    return () => canvas.removeEventListener('wheel', onWheel);
  }, [camera.handleWheelZoom, getCanvasPixelCoords]);

  // --- Mouse pan handlers ---
  const handleMouseDown = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    didDragRef.current = false;
    const pos = getCanvasPixelCoords(e.clientX, e.clientY);
    camera.handlePanStart(pos.x, pos.y);
  }, [camera.handlePanStart, getCanvasPixelCoords]);

  const handleMouseMove = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    const pos = getCanvasPixelCoords(e.clientX, e.clientY);
    const dragging = camera.handlePanMove(pos.x, pos.y);
    if (dragging) didDragRef.current = true;
  }, [camera.handlePanMove, getCanvasPixelCoords]);

  const handleMouseUpOrLeave = useCallback(() => {
    camera.handlePanEnd();
  }, [camera.handlePanEnd]);

  // --- Rendering ---
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.fillStyle = '#0f0f23';
    ctx.fillRect(0, 0, canvasSize.width, canvasSize.height);

    if (!frameData) {
      ctx.fillStyle = '#6b7280';
      ctx.font = '16px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(
        'Waiting for simulation data...',
        canvasSize.width / 2,
        canvasSize.height / 2,
      );
      return;
    }

    const { zoom, panX, panY } = camera.view;
    const { baseScale } = camera;

    ctx.translate(panX, panY);
    ctx.scale(baseScale * zoom, baseScale * zoom);

    const flipY = (y: number) => WORLD_HEIGHT - y;

    // Grid
    ctx.strokeStyle = '#1f2937';
    ctx.lineWidth = 1 / (baseScale * zoom);

    const invScale = 1 / (baseScale * zoom);
    const minWorldX = -panX * invScale;
    const minWorldY = -panY * invScale;
    const maxWorldX = minWorldX + canvasSize.width * invScale;
    const maxWorldY = minWorldY + canvasSize.height * invScale;

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
    ctx.lineWidth = 2 / (baseScale * zoom);
    ctx.strokeRect(0, 0, WORLD_WIDTH, WORLD_HEIGHT);

    // Main target
    const targetX = frameData.mainTarget.x;
    const targetY = flipY(frameData.mainTarget.y);
    ctx.beginPath();
    ctx.arc(targetX, targetY, 15, 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(233, 69, 96, 0.3)';
    ctx.fill();
    ctx.strokeStyle = '#e94560';
    ctx.lineWidth = 2 / (baseScale * zoom);
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(targetX - 10, targetY);
    ctx.lineTo(targetX + 10, targetY);
    ctx.moveTo(targetX, targetY - 10);
    ctx.lineTo(targetX, targetY + 10);
    ctx.stroke();

    // Draw towers
    const drawTower = (tower: { id: number; type: string; faction: string; position: { x: number; y: number }; radius: number; attackRange: number; maxHP: number; currentHP: number; isActivated: boolean }) => {
      const x = tower.position.x;
      const y = flipY(tower.position.y);
      const isKing = tower.type === 'King';
      const isDestroyed = tower.currentHP <= 0;
      const size = isKing ? 120 : 100;
      const halfSize = size / 2;

      const healthPercent = tower.currentHP / tower.maxHP;
      const level = healthPercent > 0.66 ? 3 : healthPercent > 0.33 ? 2 : 1;

      const imageKey = isDestroyed
        ? 'destroyed'
        : isKing
          ? `king_${level}`
          : `princess_${level}`;
      const towerImage = towerImageMapRef.current.get(imageKey);

      if (tower.isActivated && !isDestroyed) {
        const rangeImage = towerImageMapRef.current.get('range_indicator');
        if (rangeImage && rangeImage.complete && rangeImage.naturalWidth > 0) {
          const rangeSize = tower.attackRange * 2;
          ctx.globalAlpha = tower.faction === 'Friendly' ? 0.4 : 0.3;
          ctx.drawImage(rangeImage, x - rangeSize / 2, y - rangeSize / 2, rangeSize, rangeSize);
          ctx.globalAlpha = 1;
        } else {
          ctx.beginPath();
          ctx.arc(x, y, tower.attackRange, 0, Math.PI * 2);
          ctx.strokeStyle = tower.faction === 'Friendly' ? 'rgba(59, 130, 246, 0.3)' : 'rgba(239, 68, 68, 0.3)';
          ctx.lineWidth = 2 / (baseScale * zoom);
          ctx.stroke();
        }
      }

      if (towerImage && towerImage.complete && towerImage.naturalWidth > 0) {
        if (tower.faction === 'Enemy' && !isDestroyed) {
          ctx.save();
          ctx.globalAlpha = 0.85;
          ctx.drawImage(towerImage, x - halfSize, y - halfSize, size, size);
          ctx.globalCompositeOperation = 'source-atop';
          ctx.fillStyle = 'rgba(239, 68, 68, 0.25)';
          ctx.fillRect(x - halfSize, y - halfSize, size, size);
          ctx.restore();
        } else {
          ctx.drawImage(towerImage, x - halfSize, y - halfSize, size, size);
        }
      } else {
        const baseColor = tower.faction === 'Friendly' ? '#3b82f6' : '#ef4444';
        const lightColor = tower.faction === 'Friendly' ? '#60a5fa' : '#f87171';
        ctx.beginPath();
        ctx.roundRect(x - halfSize, y - halfSize, size, size, 8);
        ctx.fillStyle = baseColor;
        ctx.fill();
        ctx.strokeStyle = lightColor;
        ctx.lineWidth = 3 / (baseScale * zoom);
        ctx.stroke();

        ctx.fillStyle = '#ffffff';
        ctx.font = `bold ${isKing ? 28 : 22}px sans-serif`;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(isKing ? '\u2654' : '\u265C', x, y);
      }

      if (!isDestroyed) {
        ctx.beginPath();
        ctx.arc(x, y + halfSize - 10, halfSize * 0.6, 0, Math.PI * 2);
        ctx.strokeStyle = tower.faction === 'Friendly' ? '#3b82f6' : '#ef4444';
        ctx.lineWidth = 4 / (baseScale * zoom);
        ctx.stroke();
      }

      const healthBarWidth = size;
      const healthBarHeight = 10;
      const healthBarY = y + halfSize + 12;

      ctx.fillStyle = '#1f2937';
      ctx.fillRect(x - healthBarWidth / 2, healthBarY, healthBarWidth, healthBarHeight);

      const healthColor = healthPercent > 0.5 ? '#22c55e' : healthPercent > 0.25 ? '#eab308' : '#ef4444';
      ctx.fillStyle = healthColor;
      ctx.fillRect(x - healthBarWidth / 2, healthBarY, healthBarWidth * healthPercent, healthBarHeight);

      ctx.strokeStyle = '#374151';
      ctx.lineWidth = 1 / (baseScale * zoom);
      ctx.strokeRect(x - healthBarWidth / 2, healthBarY, healthBarWidth, healthBarHeight);

      ctx.fillStyle = '#ffffff';
      ctx.font = `bold ${12}px sans-serif`;
      ctx.textAlign = 'center';
      ctx.fillText(`${tower.currentHP}/${tower.maxHP}`, x, healthBarY + healthBarHeight + 14);
    };

    frameData.friendlyTowers?.forEach(drawTower);
    frameData.enemyTowers?.forEach(drawTower);

    // Draw units
    const MIN_DISPLAY_RADIUS = 20;
    const drawUnit = (unit: UnitStateData) => {
      const x = unit.position.x;
      const y = flipY(unit.position.y);
      const radius = Math.max(unit.radius, MIN_DISPLAY_RADIUS);
      const isSelected = unit.id === selectedUnitId && unit.faction === selectedFaction;

      if (unit.isDead) ctx.globalAlpha = 0.3;

      if (isSelected) {
        ctx.beginPath();
        ctx.arc(x, y, radius + 8, 0, Math.PI * 2);
        ctx.strokeStyle = '#fbbf24';
        ctx.lineWidth = 4 / (baseScale * zoom);
        ctx.stroke();
      }

      if (isSelected && !unit.isDead) {
        ctx.beginPath();
        ctx.arc(x, y, unit.attackRange, 0, Math.PI * 2);
        ctx.strokeStyle = 'rgba(251, 191, 36, 0.3)';
        ctx.lineWidth = 2 / (baseScale * zoom);
        ctx.stroke();
      }

      const unitKey = unit.unitId ? unit.unitId.toLowerCase() : '';
      const icon = unitKey ? unitIconMapRef.current.get(unitKey) : undefined;

      if (icon && icon.complete && icon.naturalWidth > 0) {
        const size = radius * 2;
        ctx.save();
        ctx.translate(x - size / 2, y - size / 2);
        ctx.drawImage(icon, 0, 0, size, size);
        ctx.restore();
      } else {
        ctx.beginPath();
        ctx.arc(x, y, radius, 0, Math.PI * 2);
        if (unit.faction === 'Friendly') {
          ctx.fillStyle = unit.role === 'Melee' ? '#4ade80' : '#60a5fa';
        } else {
          ctx.fillStyle = unit.role === 'Melee' ? '#f87171' : '#fb923c';
        }
        ctx.fill();

        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 3 / (baseScale * zoom);
        ctx.stroke();
      }

      if (!unit.isDead) {
        const fwdLength = radius + 10;
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.lineTo(
          x + unit.forward.x * fwdLength,
          y - unit.forward.y * fwdLength,
        );
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 3 / (baseScale * zoom);
        ctx.stroke();
      }

      ctx.fillStyle = '#000000';
      ctx.font = 'bold 12px sans-serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(unit.label, x, y);

      const healthBarWidth = radius * 2.5;
      const healthBarHeight = 6;
      const healthBarY = y - radius - 12;
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
        ctx.lineTo(unit.currentDestination.x, unit.currentDestination.y);
        ctx.strokeStyle = 'rgba(255, 255, 255, 0.5)';
        ctx.lineWidth = 1 / (baseScale * zoom);
        ctx.stroke();
        ctx.setLineDash([]);
      }

      ctx.globalAlpha = 1;
    };

    frameData.enemyUnits.forEach(drawUnit);
    frameData.friendlyUnits.forEach(drawUnit);
  }, [camera.baseScale, camera.view, canvasSize.height, canvasSize.width, frameData, iconVersion, selectedUnitId, selectedFaction]);

  return (
    <canvas
      ref={canvasRef}
      className="simulation-canvas"
      width={canvasSize.width}
      height={canvasSize.height}
      onClick={handleCanvasClick}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUpOrLeave}
      onMouseLeave={handleMouseUpOrLeave}
      style={{ cursor: didDragRef.current ? 'grabbing' : 'grab' }}
    />
  );
});

export default SimulationCanvas;
