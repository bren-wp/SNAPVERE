import { useCallback, useEffect, useRef, useState } from "react";
import { convertFileSrc } from "@tauri-apps/api/core";
import { Pause, Play, RotateCcw, Volume2, VolumeX } from "lucide-react";
import type { Screenshot } from "../../lib/types";
import { PLAYBACK_SPEEDS, readVolume } from "../../lib/playerPreferences";
import StageError from "./StageError";

// ───────────────────────────────── video ─────────────────────────────────

export default function VideoStage({
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
    let lastUpdate = 0;
    const tick = (timestamp: number) => {
      // 10Hz is smooth enough for playback progress and saves React renders/CPU.
      if (timestamp - lastUpdate >= 100 && ref.current) {
        setTime(ref.current.currentTime);
        lastUpdate = timestamp;
      }
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
            onClick={() => setSpeed((s) => PLAYBACK_SPEEDS[(PLAYBACK_SPEEDS.indexOf(s) + 1) % PLAYBACK_SPEEDS.length])}
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
  const safeDuration = Number.isFinite(duration) && duration > 0 ? duration : 0;
  const pct = safeDuration > 0 ? Math.min(100, (time / safeDuration) * 100) : 0;

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
        if (safeDuration > 0) onSeek(at(e.clientX) * safeDuration);
      }}
      onPointerMove={(e) => {
        const f = at(e.clientX);
        setHover(f);
        if (dragging && safeDuration > 0) onSeek(f * safeDuration);
      }}
      onPointerUp={() => setDragging(false)}
      onPointerCancel={() => setDragging(false)}
      onLostPointerCapture={() => setDragging(false)}
      onPointerLeave={() => setHover(null)}
      onKeyDown={(event) => {
        if (!safeDuration) return;
        let target: number;
        switch (event.key) {
          case "ArrowLeft": case "ArrowDown":
            target = Math.max(0, time - 5); break;
          case "ArrowRight": case "ArrowUp":
            target = Math.min(safeDuration, time + 5); break;
          case "Home": target = 0; break;
          case "End": target = safeDuration; break;
          default: return;
        }
        event.preventDefault();
        onSeek(target);
      }}
      tabIndex={0}
      role="slider"
      aria-label="Seek"
      aria-valuemin={0}
      aria-valuemax={Math.round(safeDuration)}
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

function fmtTime(s: number) {
  if (!Number.isFinite(s) || s < 0) s = 0;
  const m = Math.floor(s / 60);
  const sec = Math.floor(s % 60);
  return `${m}:${sec.toString().padStart(2, "0")}`;
}

