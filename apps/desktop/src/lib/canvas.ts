// Zajednički render anotacija. Koordinate su CSS px relativno na selekciju.
// Blur uzorkuje iz frozen slike (destruktivno) preko `BlurSource` (physical origin + scale).

import type { Annotation, Point } from "./types";

export interface BlurSource {
  frozen: CanvasImageSource; // <img> ili <canvas> sa zamrznutim pikselima
  originX: number; // physical px gornje-lijeve točke selekcije u frozen slici
  originY: number;
  scale: number; // physical px po CSS px (devicePixelRatio za taj monitor)
}

export function renderAnnotations(
  ctx: CanvasRenderingContext2D,
  list: Annotation[],
  draft: Annotation | null,
  src: BlurSource,
) {
  for (const a of list) drawAnnotation(ctx, a, src);
  if (draft) drawAnnotation(ctx, draft, src);
}

export function drawAnnotation(
  ctx: CanvasRenderingContext2D,
  a: Annotation,
  src: BlurSource,
) {
  ctx.lineCap = "round";
  ctx.lineJoin = "round";
  ctx.strokeStyle = a.color;
  ctx.fillStyle = a.color;
  ctx.lineWidth = a.size;

  const [p0, p1] = a.points;
  switch (a.tool) {
    case "arrow":
      if (p1) drawArrow(ctx, p0, p1, a.size);
      break;
    case "rect":
      if (p1) {
        roundRectStroke(ctx, p0, p1, Math.max(2, a.size));
      }
      break;
    case "ellipse":
      if (p1) ellipse(ctx, p0, p1);
      break;
    case "line":
      if (p1) line(ctx, p0, p1);
      break;
    case "pen":
      polyline(ctx, a.points);
      break;
    case "marker":
      ctx.save();
      ctx.globalAlpha = 0.35;
      ctx.lineWidth = a.size * 3;
      polyline(ctx, a.points);
      ctx.restore();
      break;
    case "blur":
      if (p1) {
        const x = Math.min(p0.x, p1.x);
        const y = Math.min(p0.y, p1.y);
        drawBlur(ctx, src, x, y, Math.abs(p1.x - p0.x), Math.abs(p1.y - p0.y));
      }
      break;
    case "text":
      ctx.font = `600 ${a.size}px -apple-system, "SF Pro Display", Inter, sans-serif`;
      ctx.textBaseline = "top";
      ctx.fillText(a.text ?? "", p0.x, p0.y);
      break;
    case "step": {
      const r = Math.max(13, a.size * 2.4);
      ctx.beginPath();
      ctx.arc(p0.x, p0.y, r, 0, Math.PI * 2);
      ctx.fillStyle = a.color;
      ctx.fill();
      ctx.lineWidth = Math.max(2, a.size * 0.4);
      ctx.strokeStyle = "rgba(255,255,255,0.92)";
      ctx.stroke();
      ctx.fillStyle = "#fff";
      ctx.font = `700 ${Math.round(r * 1.15)}px -apple-system, "SF Pro Display", Inter, sans-serif`;
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillText(a.text ?? "", p0.x, p0.y + 1);
      ctx.textAlign = "start";
      break;
    }
  }
}

function line(ctx: CanvasRenderingContext2D, a: Point, b: Point) {
  ctx.beginPath();
  ctx.moveTo(a.x, a.y);
  ctx.lineTo(b.x, b.y);
  ctx.stroke();
}

function polyline(ctx: CanvasRenderingContext2D, pts: Point[]) {
  if (pts.length < 2) {
    if (pts.length === 1) {
      ctx.beginPath();
      ctx.arc(pts[0].x, pts[0].y, ctx.lineWidth / 2, 0, Math.PI * 2);
      ctx.fill();
    }
    return;
  }
  ctx.beginPath();
  ctx.moveTo(pts[0].x, pts[0].y);
  for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i].x, pts[i].y);
  ctx.stroke();
}

/** Strelica: tijelo staje na bazi vrha (round-cap se ne probija), vrh je oštri chevron s usjekom. */
function drawArrow(ctx: CanvasRenderingContext2D, from: Point, to: Point, size: number) {
  const angle = Math.atan2(to.y - from.y, to.x - from.x);
  const dist = Math.hypot(to.x - from.x, to.y - from.y);
  const head = Math.min(Math.max(13, size * 3.4), dist); // ne dulji od cijele strelice
  const spread = Math.PI / 6.5; // ~28° poluširina krilca
  const cos = Math.cos(angle);
  const sin = Math.sin(angle);

  // tijelo završava unutar vrha → oštar spoj, bez virenja okruglog kraja
  ctx.beginPath();
  ctx.moveTo(from.x, from.y);
  ctx.lineTo(to.x - head * 0.9 * cos, to.y - head * 0.9 * sin);
  ctx.stroke();

  // vrh — chevron s usjekom prema tijelu (ljepše od plosnatog trokuta)
  ctx.beginPath();
  ctx.moveTo(to.x, to.y);
  ctx.lineTo(to.x - head * Math.cos(angle - spread), to.y - head * Math.sin(angle - spread));
  ctx.lineTo(to.x - head * 0.6 * cos, to.y - head * 0.6 * sin);
  ctx.lineTo(to.x - head * Math.cos(angle + spread), to.y - head * Math.sin(angle + spread));
  ctx.closePath();
  ctx.fill();
}

function ellipse(ctx: CanvasRenderingContext2D, a: Point, b: Point) {
  const cx = (a.x + b.x) / 2;
  const cy = (a.y + b.y) / 2;
  const rx = Math.abs(b.x - a.x) / 2;
  const ry = Math.abs(b.y - a.y) / 2;
  ctx.beginPath();
  ctx.ellipse(cx, cy, rx, ry, 0, 0, Math.PI * 2);
  ctx.stroke();
}

function roundRectStroke(ctx: CanvasRenderingContext2D, a: Point, b: Point, radius: number) {
  const x = Math.min(a.x, b.x);
  const y = Math.min(a.y, b.y);
  const w = Math.abs(b.x - a.x);
  const h = Math.abs(b.y - a.y);
  const r = Math.min(radius * 1.5, w / 2, h / 2);
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
  ctx.stroke();
}

/** DESTRUKTIVNI redact: pikselizira regiju uzorkujući iz frozen slike. */
function drawBlur(
  ctx: CanvasRenderingContext2D,
  src: BlurSource,
  x: number,
  y: number,
  w: number,
  h: number,
) {
  if (w < 2 || h < 2) return;
  const sx = src.originX + x * src.scale;
  const sy = src.originY + y * src.scale;
  const sw = w * src.scale;
  const sh = h * src.scale;
  const f = 0.08;
  const tw = Math.max(1, Math.floor(w * f));
  const th = Math.max(1, Math.floor(h * f));
  const tmp = document.createElement("canvas");
  tmp.width = tw;
  tmp.height = th;
  const tctx = tmp.getContext("2d")!;
  tctx.drawImage(src.frozen, sx, sy, sw, sh, 0, 0, tw, th);
  const prev = ctx.imageSmoothingEnabled;
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(tmp, 0, 0, tw, th, x, y, w, h);
  ctx.imageSmoothingEnabled = prev;
}
