/** Pixel-precise selection geometry, shared by pointer drag and keyboard nudging. */
export interface Rect { x: number; y: number; w: number; h: number }
export type ResizeDirection = "nw" | "n" | "ne" | "e" | "se" | "s" | "sw" | "w";
export interface Viewport { width: number; height: number }
const MIN_SIZE = 12;

export function clampRect(r: Rect, viewport: Viewport): Rect {
  const W = Number.isFinite(viewport.width) ? Math.max(1, viewport.width) : 1;
  const H = Number.isFinite(viewport.height) ? Math.max(1, viewport.height) : 1;
  const finite = (value: number, fallback: number) => Number.isFinite(value) ? value : fallback;
  const w = Math.max(Math.min(MIN_SIZE, W), Math.min(finite(r.w, MIN_SIZE), W));
  const h = Math.max(Math.min(MIN_SIZE, H), Math.min(finite(r.h, MIN_SIZE), H));
  const x = Math.max(0, Math.min(finite(r.x, 0), W - w));
  const y = Math.max(0, Math.min(finite(r.y, 0), H - h));
  return { x, y, w, h };
}

export function applyResize(dir: ResizeDirection, s: Rect, dx: number, dy: number, viewport: Viewport): Rect {
  let { x, y, w, h } = s;
  if (dir.includes("w")) { x = s.x + dx; w = s.w - dx; }
  if (dir.includes("e")) w = s.w + dx;
  if (dir.includes("n")) { y = s.y + dy; h = s.h - dy; }
  if (dir.includes("s")) h = s.h + dy;
  const minW = Math.min(MIN_SIZE, Math.max(1, viewport.width));
  const minH = Math.min(MIN_SIZE, Math.max(1, viewport.height));
  if (w < minW) { if (dir.includes("w")) x = s.x + s.w - minW; w = minW; }
  if (h < minH) { if (dir.includes("n")) y = s.y + s.h - minH; h = minH; }
  return clampRect({ x, y, w, h }, viewport);
}
