import { useCallback, useEffect, useRef, useState } from 'react';
import { CameraFocusMode, FrameData, UnitStateData } from '../types';

// World constants (match GameConstants)
const WORLD_WIDTH = 3200;
const WORLD_HEIGHT = 5100;

// Zoom limits
const MIN_ZOOM = 0.15;
const MAX_ZOOM = 5;

// Auto-fit
const AUTO_FIT_PADDING = 60;
const AUTO_FIT_MIN_SIZE = 200;

// Animation easing rates (higher = faster convergence)
const PAN_RATE = 8.0;
const ZOOM_RATE = 6.0;

// Drag detection
const DRAG_THRESHOLD = 5;

interface CameraView {
  zoom: number;
  panX: number;
  panY: number;
}

export interface UseCameraOptions {
  canvasSize: { width: number; height: number };
  focusMode: CameraFocusMode;
  frameData: FrameData | null;
  selectedUnitId: number | null;
  selectedFaction: 'Friendly' | 'Enemy' | null;
}

export interface UseCameraReturn {
  view: CameraView;
  baseScale: number;
  zoomPercent: number;
  isManualMode: boolean;
  handleWheelZoom: (canvasX: number, canvasY: number, deltaY: number) => void;
  handlePanStart: (canvasX: number, canvasY: number) => void;
  handlePanMove: (canvasX: number, canvasY: number) => boolean;
  handlePanEnd: () => void;
  canvasToWorld: (canvasX: number, canvasY: number) => { x: number; y: number };
  resetToFit: () => void;
  resumeAutoFocus: () => void;
}

// --- Helpers ---

function getBaseScale(width: number, height: number): number {
  return Math.min(width / WORLD_WIDTH, height / WORLD_HEIGHT);
}

function getUnitsBounds(units: UnitStateData[]) {
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;

  for (const unit of units) {
    minX = Math.min(minX, unit.position.x - unit.radius);
    minY = Math.min(minY, unit.position.y - unit.radius);
    maxX = Math.max(maxX, unit.position.x + unit.radius);
    maxY = Math.max(maxY, unit.position.y + unit.radius);
  }

  if (!Number.isFinite(minX)) return null;
  return { minX, minY, maxX, maxY };
}

function lerpDecay(current: number, target: number, rate: number, dt: number): number {
  return target + (current - target) * Math.exp(-rate * dt);
}

function clampPan(
  panX: number,
  panY: number,
  canvasW: number,
  canvasH: number,
  scale: number,
): { panX: number; panY: number } {
  const worldW = WORLD_WIDTH * scale;
  const worldH = WORLD_HEIGHT * scale;

  // Ensure at least 25% of the world remains visible on screen
  const minPanX = canvasW * 0.25 - worldW;
  const maxPanX = canvasW * 0.75;
  const minPanY = canvasH * 0.25 - worldH;
  const maxPanY = canvasH * 0.75;

  return {
    panX: Math.max(minPanX, Math.min(maxPanX, panX)),
    panY: Math.max(minPanY, Math.min(maxPanY, panY)),
  };
}

// --- Hook ---

export function useCamera({
  canvasSize,
  focusMode,
  frameData,
  selectedUnitId,
  selectedFaction,
}: UseCameraOptions): UseCameraReturn {
  const baseScale = getBaseScale(canvasSize.width, canvasSize.height);
  const initialPanX = (canvasSize.width - WORLD_WIDTH * baseScale) / 2;
  const initialPanY = (canvasSize.height - WORLD_HEIGHT * baseScale) / 2;

  const [view, setView] = useState<CameraView>({ zoom: 1, panX: initialPanX, panY: initialPanY });
  const viewRef = useRef(view);
  const targetViewRef = useRef(view);

  // Animation
  const animFrameRef = useRef<number | null>(null);
  const lastFrameTimeRef = useRef<number | null>(null);

  // Manual mode tracking
  const isManualModeRef = useRef(false);
  const [isManualMode, setIsManualMode] = useState(false);

  // Pan state
  const isPanningRef = useRef(false);
  const panStartRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });
  const lastPosRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });
  const didDragRef = useRef(false);

  // Resize tracking
  const prevCanvasSizeRef = useRef(canvasSize);
  const autoFitAppliedSizeRef = useRef<{ width: number; height: number } | null>(null);

  // Focus tracking
  const lastFocusKeyRef = useRef('none');

  // --- Internal helpers ---

  const updateView = useCallback((next: CameraView) => {
    viewRef.current = next;
    setView(next);
  }, []);

  const animateToTarget = useCallback((time: number) => {
    const lastTime = lastFrameTimeRef.current ?? time;
    const dt = Math.max(0, Math.min((time - lastTime) / 1000, 0.1)); // cap dt to avoid jumps
    lastFrameTimeRef.current = time;

    const current = viewRef.current;
    const target = targetViewRef.current;

    const nextPanX = lerpDecay(current.panX, target.panX, PAN_RATE, dt);
    const nextPanY = lerpDecay(current.panY, target.panY, PAN_RATE, dt);
    const nextZoom = lerpDecay(current.zoom, target.zoom, ZOOM_RATE, dt);

    const reached =
      Math.abs(nextPanX - target.panX) < 0.5 &&
      Math.abs(nextPanY - target.panY) < 0.5 &&
      Math.abs(nextZoom - target.zoom) < 0.001;

    if (reached) {
      updateView({ zoom: target.zoom, panX: target.panX, panY: target.panY });
      animFrameRef.current = null;
      lastFrameTimeRef.current = null;
      return;
    }

    updateView({ zoom: nextZoom, panX: nextPanX, panY: nextPanY });
    animFrameRef.current = requestAnimationFrame(animateToTarget);
  }, [updateView]);

  const setTargetView = useCallback((next: CameraView) => {
    targetViewRef.current = next;
    if (animFrameRef.current === null) {
      animFrameRef.current = requestAnimationFrame(animateToTarget);
    }
  }, [animateToTarget]);

  const enterManualMode = useCallback(() => {
    if (!isManualModeRef.current) {
      isManualModeRef.current = true;
      setIsManualMode(true);
    }
  }, []);

  const exitManualMode = useCallback(() => {
    if (isManualModeRef.current) {
      isManualModeRef.current = false;
      setIsManualMode(false);
    }
  }, []);

  // Cleanup animation on unmount
  useEffect(() => {
    return () => {
      if (animFrameRef.current !== null) {
        cancelAnimationFrame(animFrameRef.current);
      }
    };
  }, []);

  // --- Auto-focus ---

  useEffect(() => {
    if (!frameData) return;
    if (canvasSize.width === 0 || canvasSize.height === 0) return;

    // Manual mode or 'manual' focusMode — skip auto-focus entirely
    if (focusMode === 'manual' || isManualModeRef.current) return;

    const selectionKey =
      selectedUnitId !== null && selectedFaction
        ? `${selectedFaction}-${selectedUnitId}`
        : 'none';
    const focusKey = `${focusMode}-${selectionKey}`;
    const selectionChanged = focusKey !== lastFocusKeyRef.current;
    if (selectionChanged) {
      lastFocusKeyRef.current = focusKey;
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
      canvasSize.height / focusHeight,
    );
    const nextZoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, desiredScale / baseScale));
    const nextPanX = canvasSize.width / 2 - centerX * baseScale * nextZoom;
    const nextPanY = canvasSize.height / 2 - (WORLD_HEIGHT - centerY) * baseScale * nextZoom;

    const { zoom, panX, panY } = targetViewRef.current;
    const zoomDelta = Math.abs(zoom - nextZoom);
    const panDelta = Math.abs(panX - nextPanX) + Math.abs(panY - nextPanY);

    if (zoomDelta < 0.001 && panDelta < 0.5) return;

    autoFitAppliedSizeRef.current = { width: canvasSize.width, height: canvasSize.height };
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

  // --- Canvas resize pan adjustment ---

  useEffect(() => {
    const prevSize = prevCanvasSizeRef.current;
    if (prevSize.width === canvasSize.width && prevSize.height === canvasSize.height) return;

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

  // --- Exit manual mode on focusMode change (except 'manual') ---

  useEffect(() => {
    if (focusMode === 'manual') {
      enterManualMode();
    } else {
      exitManualMode();
    }
  }, [focusMode, enterManualMode, exitManualMode]);

  // --- Coordinate conversion ---

  const canvasToWorld = useCallback((canvasX: number, canvasY: number) => {
    const { zoom, panX, panY } = viewRef.current;
    const screenY = (canvasY - panY) / (baseScale * zoom);
    return {
      x: (canvasX - panX) / (baseScale * zoom),
      y: WORLD_HEIGHT - screenY,
    };
  }, [baseScale]);

  // --- Input handlers ---

  const handleWheelZoom = useCallback((canvasX: number, canvasY: number, deltaY: number) => {
    const { zoom } = viewRef.current;

    if (focusMode !== 'manual') {
      enterManualMode();
    }

    const worldPos = canvasToWorld(canvasX, canvasY);

    // Exponential zoom for consistent feel
    const zoomFactor = Math.pow(1.15, -Math.sign(deltaY));
    const newZoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom * zoomFactor));

    const newPanX = canvasX - worldPos.x * baseScale * newZoom;
    const newPanY = canvasY - (WORLD_HEIGHT - worldPos.y) * baseScale * newZoom;

    const clamped = clampPan(newPanX, newPanY, canvasSize.width, canvasSize.height, baseScale * newZoom);
    setTargetView({ zoom: newZoom, panX: clamped.panX, panY: clamped.panY });
  }, [baseScale, canvasSize.width, canvasSize.height, canvasToWorld, enterManualMode, focusMode, setTargetView]);

  const handlePanStart = useCallback((canvasX: number, canvasY: number) => {
    isPanningRef.current = true;
    didDragRef.current = false;
    panStartRef.current = { x: canvasX, y: canvasY };
    lastPosRef.current = { x: canvasX, y: canvasY };
  }, []);

  const handlePanMove = useCallback((canvasX: number, canvasY: number): boolean => {
    if (!isPanningRef.current) return false;

    // Check drag threshold before starting pan
    if (!didDragRef.current) {
      const dx = canvasX - panStartRef.current.x;
      const dy = canvasY - panStartRef.current.y;
      if (Math.sqrt(dx * dx + dy * dy) < DRAG_THRESHOLD) {
        return false;
      }
      didDragRef.current = true;
      if (focusMode !== 'manual') {
        enterManualMode();
      }
    }

    const last = lastPosRef.current;
    const dx = canvasX - last.x;
    const dy = canvasY - last.y;
    lastPosRef.current = { x: canvasX, y: canvasY };

    if (dx !== 0 || dy !== 0) {
      const { zoom, panX, panY } = viewRef.current;
      const newPanX = panX + dx;
      const newPanY = panY + dy;
      const clamped = clampPan(newPanX, newPanY, canvasSize.width, canvasSize.height, baseScale * zoom);

      // Instant pan (no animation) for responsive dragging
      const next = { zoom, panX: clamped.panX, panY: clamped.panY };
      targetViewRef.current = next;
      updateView(next);
    }

    return true;
  }, [baseScale, canvasSize.width, canvasSize.height, enterManualMode, focusMode, updateView]);

  const handlePanEnd = useCallback(() => {
    isPanningRef.current = false;
  }, []);

  // --- Actions ---

  const computeFitView = useCallback((): CameraView => {
    const fitZoom = 1;
    const fitPanX = (canvasSize.width - WORLD_WIDTH * baseScale * fitZoom) / 2;
    const fitPanY = (canvasSize.height - WORLD_HEIGHT * baseScale * fitZoom) / 2;
    return { zoom: fitZoom, panX: fitPanX, panY: fitPanY };
  }, [baseScale, canvasSize.width, canvasSize.height]);

  const resetToFit = useCallback(() => {
    setTargetView(computeFitView());
  }, [computeFitView, setTargetView]);

  const resumeAutoFocus = useCallback(() => {
    exitManualMode();
  }, [exitManualMode]);

  // --- Derived state ---

  const zoomPercent = Math.round(view.zoom * baseScale * 100);

  return {
    view,
    baseScale,
    zoomPercent,
    isManualMode,
    handleWheelZoom,
    handlePanStart,
    handlePanMove,
    handlePanEnd,
    canvasToWorld,
    resetToFit,
    resumeAutoFocus,
  };
}

export { WORLD_WIDTH, WORLD_HEIGHT };
