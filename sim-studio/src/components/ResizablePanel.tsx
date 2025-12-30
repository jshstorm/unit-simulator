import { useState, useRef, useEffect, ReactNode } from 'react';

interface ResizablePanelProps {
  leftPanel: ReactNode;
  rightPanel: ReactNode;
  defaultLeftWidth?: number; // percentage (0-100)
  minLeftWidth?: number; // percentage
  minRightWidth?: number; // percentage
}

function ResizablePanel({
  leftPanel,
  rightPanel,
  defaultLeftWidth = 65,
  minLeftWidth = 30,
  minRightWidth = 20,
}: ResizablePanelProps) {
  const [leftWidth, setLeftWidth] = useState(defaultLeftWidth);
  const [isDragging, setIsDragging] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const handleMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  useEffect(() => {
    if (!isDragging) return;

    const handleMouseMove = (e: MouseEvent) => {
      e.preventDefault();
      e.stopPropagation();

      if (!containerRef.current) return;

      const containerRect = containerRef.current.getBoundingClientRect();
      const containerWidth = containerRect.width;
      const mouseX = e.clientX - containerRect.left;

      let newLeftWidth = (mouseX / containerWidth) * 100;

      // Apply constraints
      newLeftWidth = Math.max(minLeftWidth, Math.min(100 - minRightWidth, newLeftWidth));

      setLeftWidth(newLeftWidth);
    };

    const handleMouseUp = (e: MouseEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragging(false);
    };

    document.addEventListener('mousemove', handleMouseMove, { passive: false });
    document.addEventListener('mouseup', handleMouseUp, { passive: false });

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging, minLeftWidth, minRightWidth]);

  return (
    <div
      ref={containerRef}
      className="resizable-panel-container"
      style={{ display: 'flex', flex: 1, gap: 0, minHeight: 0 }}
    >
      <div
        className="resizable-panel-left"
        style={{
          width: `${leftWidth}%`,
          minWidth: 0,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {leftPanel}
      </div>

      <div
        className="resizable-panel-handle"
        onMouseDown={handleMouseDown}
        style={{
          width: '4px',
          cursor: 'col-resize',
          backgroundColor: isDragging ? '#e94560' : '#0f3460',
          transition: isDragging ? 'none' : 'background-color 0.2s',
          flexShrink: 0,
          position: 'relative',
        }}
      >
        <div
          style={{
            position: 'absolute',
            top: 0,
            left: '-4px',
            right: '-4px',
            bottom: 0,
            cursor: 'col-resize',
          }}
        />
      </div>

      <div
        className="resizable-panel-right"
        style={{
          width: `${100 - leftWidth}%`,
          minWidth: 0,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {rightPanel}
      </div>
    </div>
  );
}

export default ResizablePanel;
