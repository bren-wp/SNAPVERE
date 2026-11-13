import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { convertFileSrc } from "@tauri-apps/api/core";
import { ZoomIn, ZoomOut } from "lucide-react";
import type { Screenshot } from "../../lib/types";
import StageError from "./StageError";

// ───────────────────────────────── slika ─────────────────────────────────

const MAX_ZOOM = 8;
const STEP = 1.2;

export default function ImageStage({
  shot,
  chromeHidden,
  onOpenExternal,
}: {
  shot: Screenshot;
  chromeHidden: boolean;
  onOpenExternal: () => void;
}) {
  const boxRef = useRef<HTMLDivElement>(null);
  const [box, setBox] = useState({ w: 0, h: 0 });
  const [natural, setNatural] = useState({ w: shot.width || 1, h: shot.height || 1 });
  const [loaded, setLoaded] = useState(false);
  const [failed, setFailed] = useState(false);
  /** null = prilagodi prozoru (prati promjenu veličine). */
  const [zoom, setZoom] = useState<number | null>(null);
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const drag = useRef<{ x: number; y: number; px: number; py: number } | null>(null);
  const [dragging, setDragging] = useState(false);

  useLayoutEffect(() => {
    const el = boxRef.current;
    if (!el) return;
    const ro = new ResizeObserver(() => setBox({ w: el.clientWidth, h: el.clientHeight }));
    ro.observe(el);
    setBox({ w: el.clientWidth, h: el.clientHeight });
    return () => ro.disconnect();
  }, []);

  // Rub od 48 px ostavlja mjesta trakama; male slike se ne razvlače preko 100 %.
  const fit = box.w && box.h ? Math.max(0.1, Math.min((box.w - 96) / natural.w, (box.h - 160) / natural.h, 1)) : 1;
  const scale = zoom ?? fit;
  const w = natural.w * scale;
  const h = natural.h * scale;

  const clampPan = useCallback(
    (p: { x: number; y: number }, s: number) => {
      const ox = natural.w * s - box.w;
      const oy = natural.h * s - box.h;
      const mx = ox > 0 ? ox / 2 + 24 : 0;
      const my = oy > 0 ? oy / 2 + 24 : 0;
      return { x: Math.max(-mx, Math.min(mx, p.x)), y: Math.max(-my, Math.min(my, p.y)) };
    },
    [natural, box],
  );

  /** Zoom oko točke (px od središta scene) — ta točka ostaje pod kursorom. */
  const zoomTo = useCallback(
    (target: number, at = { x: 0, y: 0 }) => {
      const s = Math.max(Math.min(fit, 1) * 0.5, Math.min(MAX_ZOOM, target));
      const k = s / scale;
      setZoom(Math.abs(s - fit) < 0.001 ? null : s);
      setPan((p) => clampPan({ x: at.x - (at.x - p.x) * k, y: at.y - (at.y - p.y) * k }, s));
    },
    [fit, scale, clampPan],
  );

  const resetFit = useCallback(() => {
    setZoom(null);
    setPan({ x: 0, y: 0 });
  }, []);

  const relToCenter = (e: { clientX: number; clientY: number }) => {
    const r = boxRef.current?.getBoundingClientRect();
    return r
      ? { x: e.clientX - r.left - r.width / 2, y: e.clientY - r.top - r.height / 2 }
      : { x: 0, y: 0 };
  };

  useEffect(() => {
    const el = boxRef.current;
    if (!el) return;
    // passive:false — inače preventDefault ne zaustavi scroll stranice
    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      zoomTo(scale * (e.deltaY < 0 ? STEP : 1 / STEP), relToCenter(e));
    };
    el.addEventListener("wheel", onWheel, { passive: false });
    return () => el.removeEventListener("wheel", onWheel);
  }, [scale, zoomTo]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.ctrlKey || e.metaKey || e.altKey) return;
      if (e.key === "+" || e.key === "=") zoomTo(scale * STEP);
      else if (e.key === "-") zoomTo(scale / STEP);
      else if (e.key === "0") resetFit();
      else if (e.key === "1") zoomTo(1);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [scale, zoomTo, resetFit]);

  const pannable = w > box.w || h > box.h;

  if (failed) return <StageError video={false} onOpenExternal={onOpenExternal} />;

  return (
    <div
      ref={boxRef}
      className={`absolute inset-0 overflow-hidden ${pannable ? (dragging ? "cursor-grabbing" : "cursor-grab") : ""}`}
      onPointerDown={(e) => {
        if (e.button !== 0 || !pannable) return;
        drag.current = { x: e.clientX, y: e.clientY, px: pan.x, py: pan.y };
        setDragging(true);
        e.currentTarget.setPointerCapture(e.pointerId);
      }}
      onPointerMove={(e) => {
        const d = drag.current;
        if (!d) return;
        setPan(clampPan({ x: d.px + e.clientX - d.x, y: d.py + e.clientY - d.y }, scale));
      }}
      onPointerUp={() => {
        drag.current = null;
        setDragging(false);
      }}
      onPointerCancel={() => {
        drag.current = null;
        setDragging(false);
      }}
      onLostPointerCapture={() => {
        drag.current = null;
        setDragging(false);
      }}
      onDoubleClick={(e) => {
        // prilagođeno ↔ 100 % (mala slika koja je već 1:1 ide na 2×)
        if (zoom === null) zoomTo(fit >= 1 ? 2 : 1, relToCenter(e));
        else resetFit();
      }}
    >
      <div
        className="absolute left-1/2 top-1/2"
        style={{
          width: w,
          height: h,
          transform: `translate(calc(-50% + ${pan.x}px), calc(-50% + ${pan.y}px))`,
        }}
      >
        {/* thumbnail odmah, original preko njega kad se učita */}
        {shot.thumbPath && !loaded && (
          <img src={convertFileSrc(shot.thumbPath)} alt="" className="absolute inset-0 h-full w-full blur-[2px]" draggable={false} />
        )}
        <img
          src={convertFileSrc(shot.filePath)}
          alt={shot.name}
          draggable={false}
          onLoad={(e) => {
            const img = e.currentTarget;
            setNatural({ w: img.naturalWidth || 1, h: img.naturalHeight || 1 });
            setLoaded(true);
          }}
          onError={() => setFailed(true)}
          className={`absolute inset-0 h-full w-full shadow-[0_24px_80px_rgba(0,0,0,0.6)] transition-opacity duration-150 ${
            loaded ? "opacity-100" : "opacity-0"
          }`}
          style={{ imageRendering: scale >= 2 ? "pixelated" : "auto" }}
        />
      </div>

      {/* zoom kapsula */}
      <div
        className={`absolute bottom-5 left-1/2 z-10 -translate-x-1/2 transition-opacity duration-300 ${
          chromeHidden ? "pointer-events-none opacity-0" : "opacity-100"
        }`}
        onPointerDown={(e) => e.stopPropagation()}
        onDoubleClick={(e) => e.stopPropagation()}
      >
        <div className="lg lg-bar">
          <button className="lg-btn" title="Zoom out (−)" onClick={() => zoomTo(scale / STEP)}>
            <ZoomOut size={16} />
          </button>
          <span className="w-14 text-center font-mono text-xs tabular-nums">{Math.round(scale * 100)}%</span>
          <button className="lg-btn" title="Zoom in (+)" onClick={() => zoomTo(scale * STEP)}>
            <ZoomIn size={16} />
          </button>
          <span className="lg-sep" />
          <button
            className={`h-8 rounded-full px-3 text-xs transition hover:bg-white/15 ${zoom === null ? "text-accent" : ""}`}
            title="Fit to window (0)"
            onClick={resetFit}
          >
            Fit
          </button>
          <button
            className={`h-8 rounded-full px-3 text-xs transition hover:bg-white/15 ${Math.abs(scale - 1) < 0.001 ? "text-accent" : ""}`}
            title="Actual size (1)"
            onClick={() => zoomTo(1)}
          >
            1:1
          </button>
        </div>
      </div>
    </div>
  );
}

