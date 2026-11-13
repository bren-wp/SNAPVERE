import { useCallback, useEffect, useRef, useState } from "react";
import { getCurrentWindow } from "@tauri-apps/api/window";
import {
  ChevronLeft,
  ChevronRight,
  Copy,
  FileText,
  ExternalLink,
  FolderOpen,
  Maximize,
  Minimize,
  Trash2,
  X,
} from "lucide-react";
import { ipc } from "../lib/ipc";
import type { Screenshot } from "../lib/types";
import ImageStage from "./viewer/ImageStage";
import VideoStage from "./viewer/VideoStage";

/**
 * Preglednik galerije — slika (zoom/pan) ili video (vlastite kontrole) preko
 * cijelog Home prozora. Uvijek taman, neovisno o temi (konvencija za medije).
 *
 * Tipke: ←/→ prethodna/sljedeća · Esc zatvori · F cijeli zaslon · Del obriši
 *   slika: +/− zoom · 0 prilagodi · 1 = 100 % · Ctrl+C kopiraj
 *   sve datoteke: Ctrl+Shift+C kopiraj putanju
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

  const copyPath = useCallback(async () => {
    if (!shot) return;
    try {
      await ipc.copyMediaPath(shot.id);
      onToast("File path copied");
    } catch (error) {
      onToast(`Could not copy file path: ${String(error)}`);
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
      } else if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key.toLowerCase() === "c") {
        e.preventDefault();
        void copyPath();
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
  }, [prev, next, fullscreen, onClose, onNavigate, setWindowFullscreen, remove, copy, copyPath, wake]);

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
          <button className="lg-btn" title="Copy file path (Ctrl+Shift+C)" aria-label="Copy file path" onClick={() => void copyPath()}>
            <FileText size={16} />
          </button>
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
