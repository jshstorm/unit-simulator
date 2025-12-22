import { FrameData } from '../types';

/**
 * Downloads frame log data as a JSON file
 * @param frames Array of FrameData sorted by frameNumber
 */
export function downloadFrameLog(frames: FrameData[]): void {
  if (frames.length === 0) {
    return;
  }

  // Sort frames by frameNumber (should already be sorted, but ensure it)
  const sortedFrames = [...frames].sort((a, b) => a.frameNumber - b.frameNumber);
  
  // Generate filename with frame range
  const firstFrame = sortedFrames[0].frameNumber;
  const lastFrame = sortedFrames[sortedFrames.length - 1].frameNumber;
  const filename = `frames_${firstFrame}_${lastFrame}.json`;
  
  // Serialize to pretty-printed JSON
  const jsonContent = JSON.stringify(sortedFrames, null, 2);
  
  // Create blob and download
  const blob = new Blob([jsonContent], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.style.display = 'none';
  
  document.body.appendChild(anchor);
  anchor.click();
  
  // Clean up
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}
