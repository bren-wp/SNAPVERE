import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { convertFileSrc } from "@tauri-apps/api/core";
import { getCurrentWindow } from "@tauri-apps/api/window";
import {
  AlertTriangle,
  ChevronLeft,
  ChevronRight,
  Copy,
  ExternalLink,
  FolderOpen,
  Maximize,
  Minimize,
  Pause,
  Play,
  RotateCcw,
  Trash2,
  Volume2,
  VolumeX,
  X,
  ZoomIn,
  ZoomOut,
} from "lucide-react";
import { ipc } from "../lib/ipc";
import type { Screenshot } from "../lib/types";

/**
 * Preglednik galerije — slika (zoom/pan) ili video (vlastite kontrole) preko
 * cijelog Home prozora. Uvijek taman, neovisno o temi (konvencija za medije).
 *
 * Tipke: ←/→ prethodna/sljedeća · Esc zatvori · F cijeli zaslon · Del obriši
 *   slika: +/− zoom · 0 prilagodi · 1 = 100 % · Ctrl+C kopiraj
 *   video: Space/K pusti/pauza · J/L ±5 s · M zvuk
 */

type Props = {
  items: Screenshot[];
  id: string;
  onNavigate: (id: string) => void;
  onClose: () => void;
  /** true = obrisano (korisnik potvrdio). */
  onDelete: (s: Screenshot) => Promise<boolean>;
  onToast: (msg: string) => void;
};

const IDLE_MS = 2400;

/** MP4 ide u <video>; GIF je animirana slika i ide u preglednik slika. */
const playsAsVideo = (s: Screenshot) => s.format === "mp4";

export default function Viewer({ items, id, onNavigate, onClose, onDelete, onToast }: Props) {
  const index = items.findIndex((s) => s.id === id);
  const shot = index >= 0 ? items[index] : null;
  const prev = index > 0 ? items[index - 1] : null;
  const next = index >= 0 && index < items.length - 1 ? items[index + 1] : null;

  const [fullscreen, setFullscreen] = useState(false);
  const [playing, setPlaying] = useState(false);
  const [idle, setIdle] = useState(false);
  const idleTimer = useRef<number>();

  // Zapis je nestao (obrisan izvana, novi filter) → nema se što pokazati.
  useEffect(() => {
    if (!shot) onClose();
  }, [shot, onClose]);

  const setWindowFullscreen = useCallback(async (on: boolean) => {
    try {
      await getCurrentWindow().setFullscreen(on);
      setFullscreen(on);
    } catch (e) {
      console.error(e);
    }
  }, []);

  // Zatvaranje preglednika uvijek vraća prozor iz fullscreena.
  useEffect(() => {
    return () => {
      void getCurrentWindow().setFullscreen(false).catch(() => {});
    };
  }, []);

  // Kontrole se sklanjaju dok video svira i miš miruje.
  const wake = useCallback(() => {
    setIdle(false);
    window.clearTimeout(idleTimer.current);
    idleTimer.current = window.setTimeout(() => setIdle(true), IDLE_MS);
  }, []);
  useEffect(() => {
    wake();
    return () => window.clearTimeout(idleTimer.current);
  }, [wake, id]);
  const chromeHidden = idle && playing;

  const remove = useCallback(async () => {
    if (!shot) return;
    const neighbour = next ?? prev;
    if (!(await onDelete(shot))) return;
    if (neighbour) onNavigate(neighbour.id);
    else onClose();
  }, [shot, next, prev, onDelete, onNavigate, onClose]);

  const copy = useCallback(async () => {
    if (!shot || playsAsVideo(shot)) return;
    try {
      await ipc.copyMediaImage(shot.id);
      onToast("Copied to clipboard");
    } catch (e) {
      onToast(String(e));
    }
  }, [shot, onToast]);

  const run = useCallback(
    (fn: (id: string) => Promise<void>) => async () => {
      if (!shot) return;
      try {
        await fn(shot.id);
      } catch (e) {
        onToast(String(e));
      }
    },
    [shot, onToast],
  );

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.target instanceof HTMLInputElement) return;
      if (e.key === "Escape") {
        e.preventDefault();
        if (fullscreen) void setWindowFullscreen(false);
        else onClose();
      } else if (e.key === "ArrowLeft" && prev) {
        e.preventDefault();
        onNavigate(prev.id);
      } else if (e.key === "ArrowRight" && next) {
        e.preventDefault();
        onNavigate(next.id);
      } else if (e.key === "f" || e.key === "F") {
        void setWindowFullscreen(!fullscreen);
      } else if (e.key === "Delete") {
        void remove();
      } else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "c") {
        e.preventDefault();
        void copy();
      } else {
        return;
      }
      wake();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [prev, next, fullscreen, onClose, onNavigate, setWindowFullscreen, remove, copy, wake]);

  if (!shot) return null;
  const video = playsAsVideo(shot);

  return (
    <div
      className={`viewer fixed inset-0 z-40 flex flex-col bg-[#0a0a0b] animate-fade ${chromeHidden ? "cursor-none" : ""}`}
      onMouseMove={wake}
      onMouseDown={(e) => {
        if ((e.target as HTMLElement).closest("button")) e.preventDefault();
      }}
      role="dialog"
      aria-label={shot.name}
    >
      {/* gornja traka */}
      <header
        className={`absolute inset-x-0 top-0 z-20 flex items-center gap-3 bg-gradient-to-b from-black/70 to-transparent px-4 pb-6 pt-3 transition-opacity duration-300 ${
          chromeHidden ? "pointer-events-none opacity-0" : "opacity-100"
        }`}
      >
        <div className="min-w-0 flex-1">
          <div className="truncate text-sm font-medium">{shot.name}</div>
          <div className="truncate font-mono text-[11px] text-muted">
            {shot.format.toUpperCase()} · {shot.width}×{shot.height} · {fmtBytes(shot.sizeBytes)} ·{" "}
            {fmtDate(shot.createdAt)}
          </div>
        </div>

        {items.length > 1 && (
          <span className="shrink-0 font-mono text-[11px] tabular-nums text-muted">
            {index + 1} / {items.length}
          </span>
        )}

        <div className="lg lg-bar shrink-0">
          {!video && (
            <button className="lg-btn" title="Copy image (Ctrl+C)" onClick={copy}>
              <Copy size={16} />
            </button>
          )}
          <button className="lg-btn" title="Show in folder" onClick={run(ipc.revealMedia)}>
            <FolderOpen size={16} />
          </button>
          <button className="lg-btn" title="Open in default app" onClick={run(ipc.openMedia)}>
            <ExternalLink size={16} />
          </button>
          <span className="lg-sep" />
          <button
            className="lg-btn"
            title={fullscreen ? "Exit full screen (F)" : "Full screen (F)"}
            onClick={() => void setWindowFullscreen(!fullscreen)}
          >
            {fullscreen ? <Minimize size={16} /> : <Maximize size={16} />}
          </button>
          <button className="lg-btn hover:!bg-danger/80" title="Delete (Del)" onClick={() => void remove()}>
            <Trash2 size={16} />
          </button>
        </div>

        <button
          className="lg grid h-10 w-10 shrink-0 place-items-center !rounded-full transition hover:brightness-125 active:scale-95"
          title="Close (Esc)"
          onClick={onClose}
        >
          <X size={18} />
        </button>
      </header>

      {/* scena */}
      <div className="relative min-h-0 flex-1">
        {video ? (
          <VideoStage
            key={shot.id}
            shot={shot}
            chromeHidden={chromeHidden}
            onPlayingChange={setPlaying}
            onToggleFullscreen={() => void setWindowFullscreen(!fullscreen)}
            onOpenExternal={run(ipc.openMedia)}
          />
        ) : (
          <ImageStage key={shot.id} shot={shot} chromeHidden={chromeHidden} onOpenExternal={run(ipc.openMedia)} />
        )}

        {prev && (
          <NavButton side="left" hidden={chromeHidden} onClick={() => onNavigate(prev.id)} />
        )}
        {next && (
          <NavButton side="right" hidden={chromeHidden} onClick={() => onNavigate(next.id)} />
        )}
      </div>
    </div>
  );
}

function NavButton({ side, hidden, onClick }: { side: "left" | "right"; hidden: boolean; onClick: () => void }) {
  const Icon = side === "left" ? ChevronLeft : ChevronRight;
  return (
    <div
      className={`absolute top-1/2 z-10 -translate-y-1/2 transition-opacity duration-300 ${
        side === "left" ? "left-4" : "right-4"
      } ${hidden ? "pointer-events-none opacity-0" : "opacity-100"}`}
    >
      <button
        onClick={onClick}
        title={side === "left" ? "Previous (←)" : "Next (→)"}
        className="lg grid h-11 w-11 place-items-center !rounded-full transition hover:brightness-125 active:scale-95"
      >
        <Icon size={20} />
      </button>
    </div>
  );
}

function StageError({
  video,
  detail,
  onOpenExternal,
}: {
  video: boolean;
  /** Tehnički razlog (MediaError) — mali red za bug reportove. */
  detail?: string;
  onOpenExternal: () => void;
}) {
  return (
    <div className="grid h-full place-items-center p-8 text-center">
      <div className="max-w-sm">
        <div className="mx-auto mb-4 grid h-12 w-12 place-items-center rounded-2xl bg-white/5 ring-1 ring-white/10">
          <AlertTriangle size={22} className="text-warn" />
        </div>
        <h2 className="mb-1 text-base font-semibold">
          {video ? "This video can’t be played here" : "This image can’t be shown"}
        </h2>
        <p className="mb-5 text-sm text-muted">The file may have been moved, renamed or deleted.</p>
        {detail && <p className="-mt-3 mb-5 font-mono text-[10px] text-white/35">{detail}</p>}
        <button
          onClick={onOpenExternal}
          className="rounded-full px-4 py-2 text-sm ring-1 ring-white/15 transition hover:bg-white/10 active:scale-95"
        >
          Open in default app
        </button>
      </div>
    </div>
  );
}

// ───────────────────────────────── slika ─────────────────────────────────

const MAX_ZOOM = 8;
const STEP = 1.2;

function ImageStage({
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
  const fit = box.w && box.h ? Math.min((box.w - 96) / natural.w, (box.h - 160) / natural.h, 1) : 1;
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
    const r = boxRef.current!.getBoundingClientRect();
    return { x: e.clientX - r.left - r.width / 2, y: e.clientY - r.top - r.height / 2 };
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

// ───────────────────────────────── video ─────────────────────────────────

const SPEEDS = [1, 1.5, 2, 0.5];

function readVolume(): { volume: number; muted: boolean } {
  try {
    const v = JSON.parse(localStorage.getItem("snapvere-volume") ?? "null");
    if (v && typeof v.volume === "number") return { volume: v.volume, muted: !!v.muted };
  } catch {
    /* pokvaren zapis → default */
  }
  return { volume: 1, muted: false };
}

function VideoStage({
  shot,
  chromeHidden,
  onPlayingChange,
  onToggleFullscreen,
  onOpenExternal,
}: {
  shot: Screenshot;
  chromeHidden: boolean;
  onPlayingChange: (p: boolean) => void;
  onToggleFullscreen: () => void;
  onOpenExternal: () => void;
}) {
  const ref = useRef<HTMLVideoElement>(null);
  const [playing, setPlaying] = useState(false);
  const [ended, setEnded] = useState(false);
  const [time, setTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [failed, setFailed] = useState<string | null>(null);
  const [speed, setSpeed] = useState(1);
  const [{ volume, muted }, setSound] = useState(readVolume);

  useEffect(() => onPlayingChange(playing), [playing, onPlayingChange]);
  useEffect(() => () => onPlayingChange(false), [onPlayingChange]);

  useEffect(() => {
    const v = ref.current;
    if (!v) return;
    v.volume = volume;
    v.muted = muted;
    try {
      localStorage.setItem("snapvere-volume", JSON.stringify({ volume, muted }));
    } catch {
      /* bez pohrane — samo se ne zapamti */
    }
  }, [volume, muted]);

  useEffect(() => {
    if (ref.current) ref.current.playbackRate = speed;
  }, [speed]);

  // timeupdate dolazi ~4× u sekundi — za glatku traku čitamo vrijeme svaki frame.
  useEffect(() => {
    if (!playing) return;
    let raf = 0;
    const tick = () => {
      if (ref.current) setTime(ref.current.currentTime);
      raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [playing]);

  const toggle = useCallback(() => {
    const v = ref.current;
    if (!v) return;
    if (v.paused || v.ended) void v.play().catch(() => {});
    else v.pause();
  }, []);

  const seekTo = useCallback((t: number) => {
    const v = ref.current;
    if (!v || !Number.isFinite(v.duration)) return;
    v.currentTime = Math.max(0, Math.min(v.duration, t));
    setTime(v.currentTime);
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.ctrlKey || e.metaKey || e.altKey) return;
      const v = ref.current;
      if (!v) return;
      const k = e.key.toLowerCase();
      if (k === " " || k === "k") {
        e.preventDefault();
        toggle();
      } else if (k === "j") seekTo(v.currentTime - 5);
      else if (k === "l") seekTo(v.currentTime + 5);
      else if (k === "m") setSound((s) => ({ ...s, muted: !s.muted }));
      else if (k === "home") seekTo(0);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [toggle, seekTo]);

  if (failed !== null) return <StageError video detail={failed} onOpenExternal={onOpenExternal} />;

  const silent = muted || volume === 0;

  return (
    <div className="absolute inset-0 flex items-center justify-center px-12 pb-24 pt-16">
      <video
        ref={ref}
        src={convertFileSrc(shot.filePath)}
        poster={shot.thumbPath ? convertFileSrc(shot.thumbPath) : undefined}
        autoPlay
        playsInline
        preload="auto"
        className="max-h-full min-h-0 max-w-full rounded-lg bg-black shadow-[0_24px_80px_rgba(0,0,0,0.6)]"
        onClick={toggle}
        onDoubleClick={onToggleFullscreen}
        onPlay={() => {
          setPlaying(true);
          setEnded(false);
        }}
        onPause={() => setPlaying(false)}
        onEnded={() => {
          setPlaying(false);
          setEnded(true);
        }}
        onTimeUpdate={(e) => setTime(e.currentTarget.currentTime)}
        onLoadedMetadata={(e) => setDuration(e.currentTarget.duration)}
        onDurationChange={(e) => setDuration(e.currentTarget.duration)}
        onError={(e) => {
          const err = e.currentTarget.error;
          setFailed(err ? `MediaError ${err.code}${err.message ? `: ${err.message}` : ""}` : "");
        }}
      />

      {/* veliki gumb kad stoji */}
      {!playing && (
        <div className="absolute left-1/2 top-[calc(50%-1rem)] -translate-x-1/2 -translate-y-1/2">
          <button
            onClick={toggle}
            className="lg grid h-16 w-16 place-items-center !rounded-full transition hover:brightness-125 active:scale-95 animate-pop"
            title={ended ? "Replay" : "Play (Space)"}
          >
            {ended ? <RotateCcw size={24} /> : <Play size={26} fill="currentColor" className="ml-1" />}
          </button>
        </div>
      )}

      {/* kontrole */}
      <div
        className={`absolute bottom-5 left-1/2 z-10 w-[min(760px,calc(100%-2.5rem))] -translate-x-1/2 transition-opacity duration-300 ${
          chromeHidden ? "pointer-events-none opacity-0" : "opacity-100"
        }`}
      >
        <div className="lg flex items-center gap-2 !rounded-full px-2 py-1.5">
          <button className="lg-btn shrink-0" title={playing ? "Pause (Space)" : "Play (Space)"} onClick={toggle}>
            {playing ? <Pause size={16} fill="currentColor" /> : <Play size={16} fill="currentColor" className="ml-0.5" />}
          </button>
          <span className="w-[92px] shrink-0 text-center font-mono text-[11px] tabular-nums text-white/80">
            {fmtTime(time)} / {fmtTime(duration)}
          </span>
          <Scrubber time={time} duration={duration} onSeek={seekTo} />
          <button
            className="lg-btn shrink-0"
            title={silent ? "Unmute (M)" : "Mute (M)"}
            onClick={() => setSound((s) => ({ volume: s.volume || 1, muted: !silent }))}
          >
            {silent ? <VolumeX size={16} /> : <Volume2 size={16} />}
          </button>
          <input
            type="range"
            min={0}
            max={1}
            step={0.05}
            value={silent ? 0 : volume}
            onChange={(e) => setSound({ volume: Number(e.target.value), muted: false })}
            onPointerUp={(e) => e.currentTarget.blur()}
            className="vw-range w-20 shrink-0"
            aria-label="Volume"
            style={{ ["--p" as string]: `${(silent ? 0 : volume) * 100}%` }}
          />
          <button
            className="h-8 w-11 shrink-0 rounded-full font-mono text-[11px] tabular-nums transition hover:bg-white/15"
            title="Playback speed"
            onClick={() => setSpeed((s) => SPEEDS[(SPEEDS.indexOf(s) + 1) % SPEEDS.length])}
          >
            {speed}×
          </button>
        </div>
      </div>
    </div>
  );
}

/** Traka napretka — klik ili povlačenje premota; hover pokaže vrijeme. */
function Scrubber({ time, duration, onSeek }: { time: number; duration: number; onSeek: (t: number) => void }) {
  const ref = useRef<HTMLDivElement>(null);
  const [hover, setHover] = useState<number | null>(null);
  const [dragging, setDragging] = useState(false);
  const pct = duration > 0 ? Math.min(100, (time / duration) * 100) : 0;

  const at = (clientX: number) => {
    const r = ref.current!.getBoundingClientRect();
    return Math.max(0, Math.min(1, (clientX - r.left) / r.width));
  };

  return (
    <div
      ref={ref}
      className="group relative h-8 min-w-0 flex-1 cursor-pointer"
      onPointerDown={(e) => {
        e.currentTarget.setPointerCapture(e.pointerId);
        setDragging(true);
        onSeek(at(e.clientX) * duration);
      }}
      onPointerMove={(e) => {
        const f = at(e.clientX);
        setHover(f);
        if (dragging) onSeek(f * duration);
      }}
      onPointerUp={() => setDragging(false)}
      onPointerLeave={() => setHover(null)}
      role="slider"
      aria-label="Seek"
      aria-valuemin={0}
      aria-valuemax={Math.round(duration)}
      aria-valuenow={Math.round(time)}
    >
      <div className="absolute inset-x-0 top-1/2 h-1 -translate-y-1/2 overflow-hidden rounded-full bg-white/15 transition-[height] group-hover:h-1.5">
        {hover !== null && (
          <div className="absolute inset-y-0 left-0 bg-white/15" style={{ width: `${hover * 100}%` }} />
        )}
        <div className="absolute inset-y-0 left-0 rounded-full bg-accent" style={{ width: `${pct}%` }} />
      </div>
      <div
        className={`absolute top-1/2 h-3 w-3 -translate-x-1/2 -translate-y-1/2 rounded-full bg-white shadow transition-transform ${
          dragging ? "scale-110" : "scale-0 group-hover:scale-100"
        }`}
        style={{ left: `${pct}%` }}
      />
      {hover !== null && duration > 0 && (
        <span
          className="pointer-events-none absolute -top-7 -translate-x-1/2 rounded-md bg-black/85 px-1.5 py-0.5 font-mono text-[10px] tabular-nums text-white"
          style={{ left: `${hover * 100}%` }}
        >
          {fmtTime(hover * duration)}
        </span>
      )}
    </div>
  );
}

// ──────────────────────────────── formati ────────────────────────────────

function fmtTime(s: number) {
  if (!Number.isFinite(s) || s < 0) s = 0;
  const m = Math.floor(s / 60);
  const sec = Math.floor(s % 60);
  return `${m}:${sec.toString().padStart(2, "0")}`;
}

export function fmtBytes(n: number) {
  if (n < 1024) return `${n} B`;
  if (n < 1024 * 1024) return `${(n / 1024).toFixed(0)} KB`;
  return `${(n / 1024 / 1024).toFixed(1)} MB`;
}

function fmtDate(ms: number) {
  return new Date(ms).toLocaleString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}
