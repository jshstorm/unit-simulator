import { useState, useRef, useEffect, ReactNode } from 'react';

interface VerticalResizablePanelProps {
  topPanel: ReactNode;
  bottomPanel: ReactNode;
  defaultTopHeight?: number; // percentage (0-100)
  minTopHeight?: number; // percentage
  minBottomHeight?: number; // percentage
}

function VerticalResizablePanel({
  topPanel,
  bottomPanel,
  defaultTopHeight = 85,
  minTopHeight = 60,
  minBottomHeight = 10,
}: VerticalResizablePanelProps) {
  const [topHeight, setTopHeight] = useState(defaultTopHeight);
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
      const containerHeight = containerRect.height;
      const mouseY = e.clientY - containerRect.top;

      let newTopHeight = (mouseY / containerHeight) * 100;

      // Apply constraints
      newTopHeight = Math.max(minTopHeight, Math.min(100 - minBottomHeight, newTopHeight));

      setTopHeight(newTopHeight);
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
  }, [isDragging, minTopHeight, minBottomHeight]);

  return (
    <div
      ref={containerRef}
      className="vertical-resizable-panel-container"
      style={{
        display: 'flex',
        flexDirection: 'column',
        flex: 1,
        gap: 0,
        minHeight: 0,
      }}
    >
      <div
        className="vertical-resizable-panel-top"
        style={{
          height: `${topHeight}%`,
          minHeight: 0,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {topPanel}
      </div>

      <div
        className="vertical-resizable-panel-handle"
        onMouseDown={handleMouseDown}
        style={{
          height: '4px',
          cursor: 'row-resize',
          backgroundColor: isDragging ? '#e94560' : '#0f3460',
          transition: isDragging ? 'none' : 'background-color 0.2s',
          flexShrink: 0,
          position: 'relative',
        }}
      >
        <div
          style={{
            position: 'absolute',
            top: '-4px',
            left: 0,
            right: 0,
            bottom: '-4px',
            cursor: 'row-resize',
          }}
        />
      </div>

      <div
        className="vertical-resizable-panel-bottom"
        style={{
          height: `${100 - topHeight}%`,
          minHeight: 0,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {bottomPanel}
      </div>
    </div>
  );
}

export default VerticalResizablePanel;
