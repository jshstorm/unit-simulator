import { useEffect, useRef, useCallback, useState } from 'react';
import { CameraFocusMode, FrameData, UnitStateData } from '../types';

interface SimulationCanvasProps {
  frameData: FrameData | null;
  selectedUnitId: number | null;
  selectedFaction: 'Friendly' | 'Enemy' | null;
  focusMode: CameraFocusMode;
  onUnitSelect: (unit: UnitStateData | null) => void;
  onCanvasClick: (x: number, y: number) => void;
}

const DEFAULT_CANVAS_WIDTH = 1200;
const DEFAULT_CANVAS_HEIGHT = 800;
const WORLD_WIDTH = 3200;  // Match GameConstants.SIMULATION_WIDTH
const WORLD_HEIGHT = 5100; // Match GameConstants.SIMULATION_HEIGHT
const MIN_ZOOM = 0.5;
const MAX_ZOOM = 3;
const AUTO_FIT_PADDING = 60;
const AUTO_FIT_MIN_SIZE = 200;
const AUTO_FIT_COOLDOWN_MS = 5000; // Increased to 5 seconds to prevent jitter during manual zoom
const CAMERA_PAN_SPEED = 1600;
const CAMERA_ZOOM_SPEED = 1.5;

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

// Tower asset paths - King uses fire tower, Princess uses stone tower
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

const getBaseScale = (width: number, height: number) =>
  Math.min(width / WORLD_WIDTH, height / WORLD_HEIGHT);

const getUnitsBounds = (units: UnitStateData[]) => {
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;

  units.forEach((unit) => {
    minX = Math.min(minX, unit.position.x - unit.radius);
    minY = Math.min(minY, unit.position.y - unit.radius);
    maxX = Math.max(maxX, unit.position.x + unit.radius);
    maxY = Math.max(maxY, unit.position.y + unit.radius);
  });

  if (!Number.isFinite(minX)) {
    return null;
  }

  return { minX, minY, maxX, maxY };
};

const moveTowards = (current: number, target: number, maxDelta: number) => {
  const delta = target - current;
  if (Math.abs(delta) <= maxDelta) return target;
  return current + Math.sign(delta) * maxDelta;
};

function SimulationCanvas({
  frameData,
  selectedUnitId,
  selectedFaction,
  focusMode,
  onUnitSelect,
  onCanvasClick,
}: SimulationCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [canvasSize, setCanvasSize] = useState({
    width: DEFAULT_CANVAS_WIDTH,
    height: DEFAULT_CANVAS_HEIGHT,
  });
  const baseScale = getBaseScale(canvasSize.width, canvasSize.height);
  const initialPanX = (canvasSize.width - WORLD_WIDTH * baseScale) / 2;
  const initialPanY = (canvasSize.height - WORLD_HEIGHT * baseScale) / 2;
  const [view, setView] = useState({ zoom: 1, panX: initialPanX, panY: initialPanY });
  const viewRef = useRef(view);
  const targetViewRef = useRef(view);
  const animationFrameRef = useRef<number | null>(null);
  const lastFrameTimeRef = useRef<number | null>(null);
  const unitIconMapRef = useRef(new Map<string, HTMLImageElement>());
  const towerImageMapRef = useRef(new Map<string, HTMLImageElement>());
  const [iconVersion, setIconVersion] = useState(0);
  const isPanningRef = useRef(false);
  const lastPosRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });
  const prevCanvasSizeRef = useRef(canvasSize);
  const lastManualInteractionRef = useRef(0);
  const lastFocusKeyRef = useRef('none');
  const autoFitAppliedSizeRef = useRef<{ width: number; height: number } | null>(null);

  const updateView = useCallback((next: typeof view) => {
    viewRef.current = next;
    setView(next);
  }, []);

  const animateToTarget = useCallback((time: number) => {
    const lastTime = lastFrameTimeRef.current ?? time;
    const deltaSeconds = Math.max(0, (time - lastTime) / 1000);
    lastFrameTimeRef.current = time;

    const current = viewRef.current;
    const target = targetViewRef.current;

    const nextPanX = moveTowards(current.panX, target.panX, CAMERA_PAN_SPEED * deltaSeconds);
    const nextPanY = moveTowards(current.panY, target.panY, CAMERA_PAN_SPEED * deltaSeconds);
    const nextZoom = moveTowards(current.zoom, target.zoom, CAMERA_ZOOM_SPEED * deltaSeconds);

    const reached =
      Math.abs(nextPanX - target.panX) < 0.5 &&
      Math.abs(nextPanY - target.panY) < 0.5 &&
      Math.abs(nextZoom - target.zoom) < 0.001;

    updateView({ zoom: nextZoom, panX: nextPanX, panY: nextPanY });

    if (reached) {
      animationFrameRef.current = null;
      lastFrameTimeRef.current = null;
      return;
    }

    animationFrameRef.current = requestAnimationFrame(animateToTarget);
  }, [updateView]);

  const setTargetView = useCallback((next: typeof view) => {
    targetViewRef.current = next;
    if (animationFrameRef.current === null) {
      animationFrameRef.current = requestAnimationFrame(animateToTarget);
    }
  }, [animateToTarget]);

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
          : { width: nextWidth, height: nextHeight }
      );
    };

    updateSize();
    const observer = new ResizeObserver(updateSize);
    observer.observe(canvas);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    const map = unitIconMapRef.current;
    Object.entries(UNIT_ICON_PATHS).forEach(([unitId, src]) => {
      if (map.has(unitId)) return;
      const img = new Image();
      img.onload = () => {
        setIconVersion((prev) => prev + 1);
      };
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
      img.onload = () => {
        setIconVersion((prev) => prev + 1);
      };
      img.src = src;
      map.set(key, img);
    });
  }, []);

  useEffect(() => {
    return () => {
      if (animationFrameRef.current !== null) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, []);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    canvas.width = canvasSize.width;
    canvas.height = canvasSize.height;
  }, [canvasSize.height, canvasSize.width]);

  useEffect(() => {
    if (!frameData) return;
    if (canvasSize.width === 0 || canvasSize.height === 0) return;

    const selectionKey =
      selectedUnitId !== null && selectedFaction
        ? `${selectedFaction}-${selectedUnitId}`
        : 'none';
    const focusKey = `${focusMode}-${selectionKey}`;
    const selectionChanged = focusKey !== lastFocusKeyRef.current;
    if (selectionChanged) {
      lastFocusKeyRef.current = focusKey;
    }

    const now = Date.now();
    if (!selectionChanged && now - lastManualInteractionRef.current < AUTO_FIT_COOLDOWN_MS) {
      return;
    }

    const allUnits = [...frameData.friendlyUnits, ...frameData.enemyUnits];
    const selectedUnit =
      selectedUnitId !== null && selectedFaction
        ? (selectedFaction === 'Friendly'
            ? frameData.friendlyUnits
            : frameData.enemyUnits
          ).find((unit) => unit.id === selectedUnitId) ?? null
        : null;

    const livingFriendly = frameData.friendlyUnits.filter((unit) => !unit.isDead);
    const livingEnemy = frameData.enemyUnits.filter((unit) => !unit.isDead);
    const livingUnits = [...livingFriendly, ...livingEnemy];

    let focusUnits: UnitStateData[] = [];
    switch (focusMode) {
      case 'selected':
        focusUnits = selectedUnit ? [selectedUnit] : [];
        break;
      case 'friendly':
        focusUnits = livingFriendly.length > 0 ? livingFriendly : frameData.friendlyUnits;
        break;
      case 'enemy':
        focusUnits = livingEnemy.length > 0 ? livingEnemy : frameData.enemyUnits;
        break;
      case 'all-living':
        focusUnits = livingUnits;
        break;
      case 'all':
        focusUnits = allUnits;
        break;
      case 'auto':
      default:
        if (selectedUnit) {
          focusUnits = [selectedUnit];
        } else {
          focusUnits = livingUnits.length > 0 ? livingUnits : allUnits;
        }
        break;
    }

    if (focusUnits.length === 0) {
      focusUnits = allUnits;
    }

    const bounds = getUnitsBounds(focusUnits);
    let minX = 0;
    let minY = 0;
    let maxX = WORLD_WIDTH;
    let maxY = WORLD_HEIGHT;

    if (bounds) {
      minX = bounds.minX - AUTO_FIT_PADDING;
      minY = bounds.minY - AUTO_FIT_PADDING;
      maxX = bounds.maxX + AUTO_FIT_PADDING;
      maxY = bounds.maxY + AUTO_FIT_PADDING;
    }

    let focusWidth = maxX - minX;
    let focusHeight = maxY - minY;

    if (focusWidth < AUTO_FIT_MIN_SIZE) {
      const extra = (AUTO_FIT_MIN_SIZE - focusWidth) / 2;
      minX -= extra;
      maxX += extra;
      focusWidth = AUTO_FIT_MIN_SIZE;
    }

    if (focusHeight < AUTO_FIT_MIN_SIZE) {
      const extra = (AUTO_FIT_MIN_SIZE - focusHeight) / 2;
      minY -= extra;
      maxY += extra;
      focusHeight = AUTO_FIT_MIN_SIZE;
    }

    const centerX = (minX + maxX) / 2;
    const centerY = (minY + maxY) / 2;

    const desiredScale = Math.min(
      canvasSize.width / focusWidth,
      canvasSize.height / focusHeight
    );
    const nextZoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, desiredScale / baseScale));
    const nextPanX = canvasSize.width / 2 - centerX * baseScale * nextZoom;
    // Flip Y coordinate to match screen space (Y=0 at top)
    const nextPanY = canvasSize.height / 2 - (WORLD_HEIGHT - centerY) * baseScale * nextZoom;

    const { zoom, panX, panY } = targetViewRef.current;
    const zoomDelta = Math.abs(zoom - nextZoom);
    const panDelta = Math.abs(panX - nextPanX) + Math.abs(panY - nextPanY);

    // Don't auto-fit if the deltas are very small (already at target)
    if (zoomDelta < 0.001 && panDelta < 0.5) {
      return;
    }

    // Don't auto-fit if user has manually zoomed/panned and the difference is significant
    // This prevents jittering when user manually controls the view
    const manuallyAdjusted = (zoomDelta > 0.05 || panDelta > 50) &&
                            (now - lastManualInteractionRef.current < AUTO_FIT_COOLDOWN_MS);
    if (manuallyAdjusted) {
      return;
    }

    autoFitAppliedSizeRef.current = {
      width: canvasSize.width,
      height: canvasSize.height,
    };
    setTargetView({ zoom: nextZoom, panX: nextPanX, panY: nextPanY });
  }, [
    baseScale,
    canvasSize.height,
    canvasSize.width,
    frameData,
    focusMode,
    selectedFaction,
    selectedUnitId,
    setTargetView,
  ]);

  useEffect(() => {
    const prevSize = prevCanvasSizeRef.current;
    if (prevSize.width === canvasSize.width && prevSize.height === canvasSize.height) {
      return;
    }

    const autoFitSize = autoFitAppliedSizeRef.current;
    if (
      autoFitSize &&
      autoFitSize.width === canvasSize.width &&
      autoFitSize.height === canvasSize.height
    ) {
      prevCanvasSizeRef.current = canvasSize;
      return;
    }

    const { zoom, panX, panY } = targetViewRef.current;
    const prevBaseScale = getBaseScale(prevSize.width, prevSize.height);
    const worldCenterX = (prevSize.width / 2 - panX) / (prevBaseScale * zoom);
    const worldCenterY = (prevSize.height / 2 - panY) / (prevBaseScale * zoom);
    const nextPanX = canvasSize.width / 2 - worldCenterX * baseScale * zoom;
    const nextPanY = canvasSize.height / 2 - worldCenterY * baseScale * zoom;

    prevCanvasSizeRef.current = canvasSize;
    setTargetView({ zoom, panX: nextPanX, panY: nextPanY });
  }, [baseScale, canvasSize, setTargetView]);

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
    const screenY = (canvasY - panY) / (baseScale * zoom);
    return {
      x: (canvasX - panX) / (baseScale * zoom),
      y: WORLD_HEIGHT - screenY,  // Flip Y back to game coordinates
    };
  }, [baseScale]);

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

  const handleWheel = useCallback((e: WheelEvent) => {
    e.preventDefault();
    const { x: canvasX, y: canvasY } = getCanvasPixelCoords(e.clientX, e.clientY);
    const { zoom } = viewRef.current;
    lastManualInteractionRef.current = Date.now();

    const worldPos = canvasToWorld(canvasX, canvasY);

    const zoomFactor = e.deltaY < 0 ? 1.1 : 0.9;
    const newZoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom * zoomFactor));

    const newPanX = canvasX - worldPos.x * baseScale * newZoom;
    // Use flipped Y for screen position calculation
    const newPanY = canvasY - (WORLD_HEIGHT - worldPos.y) * baseScale * newZoom;

    setTargetView({ zoom: newZoom, panX: newPanX, panY: newPanY });
  }, [baseScale, canvasToWorld, getCanvasPixelCoords, setTargetView]);

  // Attach wheel event listener with { passive: false } to allow preventDefault
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    canvas.addEventListener('wheel', handleWheel, { passive: false });
    return () => {
      canvas.removeEventListener('wheel', handleWheel);
    };
  }, [handleWheel]);

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
    if (dx !== 0 || dy !== 0) {
      lastManualInteractionRef.current = Date.now();
    }
    setTargetView({ zoom, panX: panX + dx, panY: panY + dy });
  }, [getCanvasPixelCoords, setTargetView]);

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
    ctx.fillRect(0, 0, canvasSize.width, canvasSize.height);

    if (!frameData) {
      ctx.fillStyle = '#6b7280';
      ctx.font = '16px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(
        'Waiting for simulation data...',
        canvasSize.width / 2,
        canvasSize.height / 2
      );
      return;
    }

    const { zoom, panX, panY } = view;

    // Apply pan/zoom (world space -> canvas)
    ctx.translate(panX, panY);
    ctx.scale(baseScale * zoom, baseScale * zoom);

    // Helper to flip Y coordinate (game Y=0 is bottom, screen Y=0 is top)
    const flipY = (y: number) => WORLD_HEIGHT - y;

    // Draw grid
    ctx.strokeStyle = '#1f2937';
    ctx.lineWidth = 1 / (baseScale * zoom);

    // Determine visible world bounds for grid tiling
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

    // Draw main target
    const targetX = frameData.mainTarget.x;
    const targetY = flipY(frameData.mainTarget.y);
    ctx.beginPath();
    ctx.arc(targetX, targetY, 15, 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(233, 69, 96, 0.3)';
    ctx.fill();
    ctx.strokeStyle = '#e94560';
    ctx.lineWidth = 2 / (baseScale * zoom);
    ctx.stroke();

    // Draw target cross
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
      const size = isKing ? 120 : 100; // Larger size for image assets
      const halfSize = size / 2;

      // Determine tower level based on HP percentage (for visual upgrade state)
      const healthPercent = tower.currentHP / tower.maxHP;
      const level = healthPercent > 0.66 ? 3 : healthPercent > 0.33 ? 2 : 1;

      // Get tower image key
      const imageKey = isDestroyed
        ? 'destroyed'
        : isKing
          ? `king_${level}`
          : `princess_${level}`;
      const towerImage = towerImageMapRef.current.get(imageKey);

      // Draw attack range using range indicator image or fallback circle
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

      // Draw tower image or fallback
      if (towerImage && towerImage.complete && towerImage.naturalWidth > 0) {
        // Apply tint for enemy towers (reddish overlay)
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
        // Fallback: draw colored rectangle with symbol
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
        ctx.fillText(isKing ? '♔' : '♜', x, y);
      }

      // Draw faction indicator ring
      if (!isDestroyed) {
        ctx.beginPath();
        ctx.arc(x, y + halfSize - 10, halfSize * 0.6, 0, Math.PI * 2);
        ctx.strokeStyle = tower.faction === 'Friendly' ? '#3b82f6' : '#ef4444';
        ctx.lineWidth = 4 / (baseScale * zoom);
        ctx.stroke();
      }

      // Draw health bar
      const healthBarWidth = size;
      const healthBarHeight = 10;
      const healthBarY = y + halfSize + 12;

      // Health bar background
      ctx.fillStyle = '#1f2937';
      ctx.fillRect(x - healthBarWidth / 2, healthBarY, healthBarWidth, healthBarHeight);

      // Health bar fill
      const healthColor = healthPercent > 0.5 ? '#22c55e' : healthPercent > 0.25 ? '#eab308' : '#ef4444';
      ctx.fillStyle = healthColor;
      ctx.fillRect(x - healthBarWidth / 2, healthBarY, healthBarWidth * healthPercent, healthBarHeight);

      // Health bar border
      ctx.strokeStyle = '#374151';
      ctx.lineWidth = 1 / (baseScale * zoom);
      ctx.strokeRect(x - healthBarWidth / 2, healthBarY, healthBarWidth, healthBarHeight);

      // Draw HP text
      ctx.fillStyle = '#ffffff';
      ctx.font = `bold ${12}px sans-serif`;
      ctx.textAlign = 'center';
      ctx.fillText(`${tower.currentHP}/${tower.maxHP}`, x, healthBarY + healthBarHeight + 14);
    };

    // Draw all towers (friendly first, then enemy)
    frameData.friendlyTowers?.forEach(drawTower);
    frameData.enemyTowers?.forEach(drawTower);

    // Draw units
    const MIN_DISPLAY_RADIUS = 20; // Minimum radius for visibility
    const drawUnit = (unit: UnitStateData) => {
      const x = unit.position.x;
      const y = flipY(unit.position.y);
      const radius = Math.max(unit.radius, MIN_DISPLAY_RADIUS); // Ensure minimum size
      const isSelected = unit.id === selectedUnitId && unit.faction === selectedFaction;

      if (unit.isDead) {
        ctx.globalAlpha = 0.3;
      }

      if (isSelected) {
        ctx.beginPath();
        ctx.arc(x, y, radius + 8, 0, Math.PI * 2);
        ctx.strokeStyle = '#fbbf24'; // Yellow selection ring
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
        // Draw unit circle with bright colors
        ctx.beginPath();
        ctx.arc(x, y, radius, 0, Math.PI * 2);
        if (unit.faction === 'Friendly') {
          ctx.fillStyle = unit.role === 'Melee' ? '#4ade80' : '#60a5fa'; // Brighter green/blue
        } else {
          ctx.fillStyle = unit.role === 'Melee' ? '#f87171' : '#fb923c'; // Brighter red/orange
        }
        ctx.fill();

        // White outline for better visibility
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 3 / (baseScale * zoom);
        ctx.stroke();
      }

      // Direction indicator (flip forward.y for correct direction)
      if (!unit.isDead) {
        const fwdLength = radius + 10;
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.lineTo(
          x + unit.forward.x * fwdLength,
          y - unit.forward.y * fwdLength  // Negate Y because we flipped the coordinate system
        );
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 3 / (baseScale * zoom);
        ctx.stroke();
      }

      // Unit label
      ctx.fillStyle = '#000000';
      ctx.font = 'bold 12px sans-serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(unit.label, x, y);

      // Health bar
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
        ctx.lineTo(
          unit.currentDestination.x,
          unit.currentDestination.y
        );
        ctx.strokeStyle = 'rgba(255, 255, 255, 0.5)';
        ctx.lineWidth = 1 / (baseScale * zoom);
        ctx.stroke();
        ctx.setLineDash([]);
      }

      ctx.globalAlpha = 1;
    };

    frameData.enemyUnits.forEach(drawUnit);
    frameData.friendlyUnits.forEach(drawUnit);
  }, [baseScale, canvasSize.height, canvasSize.width, frameData, iconVersion, selectedUnitId, selectedFaction, view]);

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
      style={{ cursor: isPanningRef.current ? 'grabbing' : 'grab' }}
    />
  );
}

export default SimulationCanvas;
