interface CameraOverlayProps {
  zoomPercent: number;
  isManualMode: boolean;
  onResetView: () => void;
  onResumeAuto: () => void;
}

function CameraOverlay({ zoomPercent, isManualMode, onResetView, onResumeAuto }: CameraOverlayProps) {
  return (
    <div className="camera-overlay">
      <button
        className="camera-overlay-zoom"
        onClick={onResetView}
        title="Reset view to fit"
      >
        {zoomPercent}%
      </button>
      {isManualMode && (
        <button
          className="camera-overlay-resume"
          onClick={onResumeAuto}
          title="Resume auto-focus"
        >
          Resume Auto
        </button>
      )}
    </div>
  );
}

export default CameraOverlay;
