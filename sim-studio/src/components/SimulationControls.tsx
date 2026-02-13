import { CameraFocusMode } from '../types';

interface SimulationControlsProps {
  onPlayPause: () => void;
  onStep: () => void;
  onStepBack: () => void;
  onSeek: () => void;
  seekValue: string;
  onSeekValueChange: (value: string) => void;
  onReset: () => void;
  isConnected: boolean;
  isPlaying: boolean;
  focusMode: CameraFocusMode;
  onFocusModeChange: (mode: CameraFocusMode) => void;
  disabled?: boolean;  // True if user doesn't have control permission
}

function SimulationControls({
  onPlayPause,
  onStep,
  onStepBack,
  onSeek,
  seekValue,
  onSeekValueChange,
  onReset,
  isConnected,
  isPlaying,
  focusMode,
  onFocusModeChange,
  disabled = false,
}: SimulationControlsProps) {
  const isDisabled = !isConnected || disabled;

  return (
    <div className="panel">
      <h2>Simulation Controls</h2>
      <div className="controls">
        <button
          className="btn-primary"
          onClick={onPlayPause}
          disabled={isDisabled}
        >
          {isPlaying ? 'Pause' : 'Play'}
        </button>
        <button
          className="btn-secondary"
          onClick={onStep}
          disabled={isDisabled}
        >
          Step
        </button>
        <button
          className="btn-secondary"
          onClick={onStepBack}
          disabled={isDisabled}
        >
          Step Back
        </button>
        <button
          className="btn-secondary"
          onClick={onReset}
          disabled={isDisabled}
        >
          Reset
        </button>
      </div>
      <div className="controls" style={{ marginTop: '0.5rem' }}>
        <input
          type="number"
          value={seekValue}
          onChange={(e) => onSeekValueChange(e.target.value)}
          min={0}
          placeholder="Frame #"
          style={{ width: '120px' }}
          disabled={!isConnected}
        />
        <button
          className="btn-secondary"
          onClick={onSeek}
          disabled={!isConnected}
        >
          Seek
        </button>
      </div>
      <div className="controls" style={{ marginTop: '0.75rem' }}>
        <label className="control-label">
          Camera Focus
          <select
            value={focusMode}
            onChange={(e) => onFocusModeChange(e.target.value as CameraFocusMode)}
            disabled={!isConnected}
          >
            <option value="auto">Auto (Selected &gt; Living)</option>
            <option value="manual">Manual (Free Camera)</option>
            <option value="selected">Selected Only</option>
            <option value="all-living">All Living</option>
            <option value="friendly">Friendly</option>
            <option value="enemy">Enemy</option>
            <option value="all">All Units</option>
          </select>
        </label>
      </div>
    </div>
  );
}

export default SimulationControls;
