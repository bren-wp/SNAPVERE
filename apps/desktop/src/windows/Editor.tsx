import React, { useCallback, useEffect, useRef, useState } from "react";
import { convertFileSrc } from "@tauri-apps/api/core";
import {
  MousePointer2,
  ArrowUpRight,
  Square,
  Minus,
  PenTool,
  Highlighter,
  EyeOff,
  Type,
  Undo2,
  Redo2,
  Copy,
  Save,
} from "lucide-react";
import { ipc } from "../lib/ipc";
import type { Annotation, EditorTool, Point } from "../lib/types";
import WindowFrame from "../components/WindowFrame";

const TOOLS: { id: EditorTool; icon: typeof Square; label: string }[] = [
  { id: "select", icon: MousePointer2, label: "Select" },
  { id: "arrow", icon: ArrowUpRight, label: "Arrow" },
  { id: "rect", icon: Square, label: "Rectangle" },
  { id: "line", icon: Minus, label: "Line" },
  { id: "pen", icon: PenTool, label: "Pen" },
  { id: "marker", icon: Highlighter, label: "Marker" },
  { id: "blur", icon: EyeOff, label: "Blur / redact" },
  { id: "text", icon: Type, label: "Text" },
];

const COLORS = ["#ef4444", "#f59e0b", "#22c55e", "#09bfd9", "#a855f7", "#ffffff", "#18181b"];

let counter = 0;
const uid = () => `a${++counter}`;

export default function Editor({ pathB64 }: { pathB64: string }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const baseImg = useRef<HTMLImageElement | null>(null);
  const [ready, setReady] = useState(false);

  const [tool, setTool] = useState<EditorTool>("arrow");
  const [color, setColor] = useState("#ef4444");
  const [size, setSize] = useState(6);

  const [annotations, setAnnotations] = useState<Annotation[]>([]);
  const [redo, setRedo] = useState<Annotation[]>([]);
  const draft = useRef<Annotation | null>(null);
  const drawing = useRef(false);

  // Učitaj sliku iz proslijeđene (base64) putanje.
  useEffect(() => {
    let path = "";
    try {
      path = atob(pathB64);
    } catch {
      return;
    }
    const img = new Image();
    img.onload = () => {
      baseImg.current = img;
      const c = canvasRef.current!;
      c.width = img.naturalWidth;
      c.height = img.naturalHeight;
      setReady(true);
    };
    img.src = convertFileSrc(path);
  }, [pathB64]);

  // ── crtanje ──
  const drawOne = (ctx: CanvasRenderingContext2D, a: Annotation) => {
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    ctx.strokeStyle = a.color;
    ctx.fillStyle = a.color;
    ctx.lineWidth = a.size;
    const [p0, p1] = a.points;

    switch (a.tool) {
      case "rect":
        if (p1) ctx.strokeRect(p0.x, p0.y, p1.x - p0.x, p1.y - p0.y);
        break;
      case "line":
        if (p1) line(ctx, p0, p1);
        break;
      case "arrow":
        if (p1) {
          line(ctx, p0, p1);
          arrowhead(ctx, p0, p1, a.size, a.color);
        }
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
        if (p1 && baseImg.current) drawBlur(ctx, baseImg.current, p0, p1);
        break;
      case "text":
        ctx.font = `${a.size * 4}px Inter, sans-serif`;
        ctx.textBaseline = "top";
        ctx.fillText(a.text ?? "", p0.x, p0.y);
        break;
    }
  };

  const redraw = useCallback(() => {
    const c = canvasRef.current;
    const ctx = c?.getContext("2d");
    if (!c || !ctx || !baseImg.current) return;
    ctx.clearRect(0, 0, c.width, c.height);
    ctx.drawImage(baseImg.current, 0, 0);
    for (const a of annotations) drawOne(ctx, a);
    if (draft.current) drawOne(ctx, draft.current);
  }, [annotations]);

  useEffect(() => {
    if (ready) redraw();
  }, [ready, annotations, redraw]);

  // ── koordinate (CSS px → canvas px) ──
  const toCanvas = (e: React.MouseEvent): Point => {
    const c = canvasRef.current!;
    const r = c.getBoundingClientRect();
    return {
      x: ((e.clientX - r.left) / r.width) * c.width,
      y: ((e.clientY - r.top) / r.height) * c.height,
    };
  };

  const onDown = (e: React.MouseEvent) => {
    if (tool === "select" || !ready) return;
    const p = toCanvas(e);
    if (tool === "text") {
      const text = window.prompt("Text:");
      if (text) commit({ id: uid(), tool: "text", color, size, points: [p], text });
      return;
    }
    drawing.current = true;
    draft.current = { id: uid(), tool: tool as Annotation["tool"], color, size, points: [p, p] };
    redraw();
  };

  const onMove = (e: React.MouseEvent) => {
    if (!drawing.current || !draft.current) return;
    const p = toCanvas(e);
    if (tool === "pen" || tool === "marker") {
      draft.current.points.push(p);
    } else {
      draft.current.points[1] = p;
    }
    redraw();
  };

  const onUp = () => {
    if (!drawing.current || !draft.current) return;
    drawing.current = false;
    const d = draft.current;
    draft.current = null;
    commit(d);
  };

  const commit = (a: Annotation) => {
    setAnnotations((prev) => [...prev, a]);
    setRedo([]);
  };

  const undo = () =>
    setAnnotations((prev) => {
      if (!prev.length) return prev;
      const last = prev[prev.length - 1];
      setRedo((r) => [...r, last]);
      return prev.slice(0, -1);
    });

  const doRedo = () =>
    setRedo((r) => {
      if (!r.length) return r;
      const item = r[r.length - 1];
      setAnnotations((prev) => [...prev, item]);
      return r.slice(0, -1);
    });

  // ── export ──
  const exportBase64 = () => {
    draft.current = null;
    redraw();
    return canvasRef.current!.toDataURL("image/png").split(",")[1];
  };
  const onSave = async () => {
    try {
      await ipc.saveEditedImage(exportBase64(), "png", 100);
    } catch (e) {
      console.error(e);
    }
  };
  const onCopy = async () => {
    try {
      await ipc.copyBase64ToClipboard(exportBase64());
    } catch (e) {
      console.error(e);
    }
  };

  // tipkovnica
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.ctrlKey && e.key === "z") (e.preventDefault(), undo());
      else if (e.ctrlKey && (e.key === "y" || (e.shiftKey && e.key === "Z"))) (e.preventDefault(), doRedo());
      else if (e.ctrlKey && e.key === "s") (e.preventDefault(), void onSave());
      else if (e.ctrlKey && e.key === "c") (e.preventDefault(), void onCopy());
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  });

  return (
    <WindowFrame title="Snapvere — Editor">
      <div className="flex min-h-0 flex-1 flex-col text-text">
      {/* kontekstni bar */}
      <div className="snapvere-editor-toolbar flex flex-wrap items-center gap-3 px-3 py-2">
        <span className="font-mono text-sm text-muted">Editor</span>
        <div className="mx-1 h-5 w-px bg-border" />
        {/* boje */}
        <div className="flex items-center gap-1">
          {COLORS.map((c) => (
            <button
              key={c}
              onClick={() => setColor(c)}
              className={`h-6 w-6 rounded-full border-2 transition-transform ${color === c ? "scale-110 border-text" : "border-transparent"}`}
              style={{ background: c }}
              title={c}
            />
          ))}
        </div>
        <div className="mx-1 h-5 w-px bg-border" />
        {/* stroke */}
        <label className="flex items-center gap-2 text-xs text-muted">
          Thickness
          <input type="range" min={2} max={28} value={size} onChange={(e) => setSize(+e.target.value)} />
          <span className="w-6 font-mono">{size}</span>
        </label>
        <div className="ml-auto flex items-center gap-1">
          <IconBtn title="Undo (Ctrl+Z)" onClick={undo} disabled={!annotations.length}>
            <Undo2 size={18} />
          </IconBtn>
          <IconBtn title="Redo (Ctrl+Y)" onClick={doRedo} disabled={!redo.length}>
            <Redo2 size={18} />
          </IconBtn>
          <div className="mx-1 h-5 w-px bg-border" />
          <button onClick={onCopy} className="flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm hover:bg-elevated">
            <Copy size={16} /> Copy
          </button>
          <button onClick={onSave} className="flex items-center gap-2 rounded-lg bg-accent px-3 py-1.5 text-sm text-white hover:opacity-90">
            <Save size={16} /> Save
          </button>
        </div>
      </div>

      <div className="flex min-h-0 flex-1">
        {/* paleta alata */}
        <div className="flex w-14 flex-col items-center gap-1 border-r border-border bg-surface py-2">
          {TOOLS.map((t) => (
            <button
              key={t.id}
              onClick={() => setTool(t.id)}
              title={t.label}
              className={`flex h-10 w-10 items-center justify-center rounded-lg transition-colors ${
                tool === t.id ? "bg-accent text-white" : "text-muted hover:bg-elevated hover:text-text"
              }`}
            >
              <t.icon size={20} />
            </button>
          ))}
        </div>

        {/* canvas */}
        <div className="flex min-h-0 flex-1 items-center justify-center overflow-auto bg-bg p-6">
          {!ready && <div className="text-muted">Loading image…</div>}
          <canvas
            ref={canvasRef}
            onMouseDown={onDown}
            onMouseMove={onMove}
            onMouseUp={onUp}
            onMouseLeave={onUp}
            className="max-h-full max-w-full rounded shadow-card"
            style={{ cursor: tool === "select" ? "default" : "crosshair", display: ready ? "block" : "none" }}
          />
        </div>
      </div>
      </div>
    </WindowFrame>
  );
}

// ── canvas helpers ──
function line(ctx: CanvasRenderingContext2D, a: Point, b: Point) {
  ctx.beginPath();
  ctx.moveTo(a.x, a.y);
  ctx.lineTo(b.x, b.y);
  ctx.stroke();
}
function polyline(ctx: CanvasRenderingContext2D, pts: Point[]) {
  if (pts.length < 2) return;
  ctx.beginPath();
  ctx.moveTo(pts[0].x, pts[0].y);
  for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i].x, pts[i].y);
  ctx.stroke();
}
function arrowhead(ctx: CanvasRenderingContext2D, from: Point, to: Point, size: number, color: string) {
  const angle = Math.atan2(to.y - from.y, to.x - from.x);
  const len = Math.max(14, size * 3);
  ctx.beginPath();
  ctx.moveTo(to.x, to.y);
  ctx.lineTo(to.x - len * Math.cos(angle - Math.PI / 7), to.y - len * Math.sin(angle - Math.PI / 7));
  ctx.lineTo(to.x - len * Math.cos(angle + Math.PI / 7), to.y - len * Math.sin(angle + Math.PI / 7));
  ctx.closePath();
  ctx.fillStyle = color;
  ctx.fill();
}
/** DESTRUKTIVNI redact: pikselizira regiju iz BAZNE slike (original se ne vidi u izvozu). */
function drawBlur(ctx: CanvasRenderingContext2D, img: HTMLImageElement, a: Point, b: Point) {
  const x = Math.min(a.x, b.x);
  const y = Math.min(a.y, b.y);
  const w = Math.abs(b.x - a.x);
  const h = Math.abs(b.y - a.y);
  if (w < 2 || h < 2) return;
  const scale = 0.08;
  const tw = Math.max(1, Math.floor(w * scale));
  const th = Math.max(1, Math.floor(h * scale));
  const tmp = document.createElement("canvas");
  tmp.width = tw;
  tmp.height = th;
  const tctx = tmp.getContext("2d")!;
  tctx.drawImage(img, x, y, w, h, 0, 0, tw, th);
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(tmp, 0, 0, tw, th, x, y, w, h);
  ctx.imageSmoothingEnabled = true;
}

function IconBtn({
  children,
  title,
  onClick,
  disabled,
}: {
  children: React.ReactNode;
  title: string;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <button
      title={title}
      onClick={onClick}
      disabled={disabled}
      className="flex h-9 w-9 items-center justify-center rounded-lg text-muted hover:bg-elevated hover:text-text disabled:cursor-not-allowed disabled:opacity-40"
    >
      {children}
    </button>
  );
}
