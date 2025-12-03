interface SimulationControlsProps {
  onStart: () => void;
  onStop: () => void;
  onStep: () => void;
  onReset: () => void;
  isConnected: boolean;
}

function SimulationControls({
  onStart,
  onStop,
  onStep,
  onReset,
  isConnected,
}: SimulationControlsProps) {
  return (
    <div className="panel">
      <h2>Simulation Controls</h2>
      <div className="controls">
        <button
          className="btn-primary"
          onClick={onStart}
          disabled={!isConnected}
        >
          ▶ Start
        </button>
        <button
          className="btn-secondary"
          onClick={onStop}
          disabled={!isConnected}
        >
          ⏸ Stop
        </button>
        <button
          className="btn-secondary"
          onClick={onStep}
          disabled={!isConnected}
        >
          ⏭ Step
        </button>
        <button
          className="btn-secondary"
          onClick={onReset}
          disabled={!isConnected}
        >
          ↻ Reset
        </button>
      </div>
    </div>
  );
}

export default SimulationControls;
