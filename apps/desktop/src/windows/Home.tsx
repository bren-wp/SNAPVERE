import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { convertFileSrc } from "@tauri-apps/api/core";
import { WebviewWindow } from "@tauri-apps/api/webviewWindow";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { ask } from "@tauri-apps/plugin-dialog";
import { readCapturePreferences } from "../lib/capturePreferencesStore";
import { listen } from "@tauri-apps/api/event";
import {
  Crop,
  Monitor,
  Settings as SettingsIcon,
  Search,
  Trash2,
  FolderOpen,
  ImageOff,
  Video,
  Play,
} from "lucide-react";
import { ipc } from "../lib/ipc";
import { isVideo } from "../lib/types";
import type { RecordDone, Screenshot } from "../lib/types";
import WindowFrame from "../components/WindowFrame";
import Viewer, { fmtBytes } from "../components/Viewer";

type Filter = "all" | "images" | "videos";

export default function Home() {
  const [items, setItems] = useState<Screenshot[]>([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<Filter>("all");
  const [toast, setToast] = useState<string | null>(null);
  const [captureBusy, setCaptureBusy] = useState(false);
  /** Otvoren u pregledniku (id, ne indeks — lista se osvježava ispod njega). */
  const [viewId, setViewId] = useState<string | null>(null);
  const galleryRequest = useRef(0);

  const load = useCallback(async (q: string) => {
    const request = ++galleryRequest.current;
    setLoading(true);
    try {
      const res = q.trim() ? await ipc.searchScreenshots(q) : await ipc.listScreenshots(300, 0);
      if (request === galleryRequest.current) setItems(res);
    } catch (e) {
      console.error(e);
      if (request === galleryRequest.current) setToast("Could not load the gallery");
    } finally {
      if (request === galleryRequest.current) setLoading(false);
    }
  }, []);

  useEffect(() => {
    const t = setTimeout(() => void load(query), 200);
    return () => {
      clearTimeout(t);
      galleryRequest.current += 1;
    };
  }, [query, load]);

  useEffect(() => {
    const w = getCurrentWindow();
    const un = w.onFocusChanged(({ payload }) => {
      if (payload) void load(query);
    });
    return () => void un.then((f) => f());
  }, [load, query]);

  // Snimanje i tray-capture zavrsavaju izvan ovog prozora — event je jedini signal.
  useEffect(() => {
    const subs = [
      listen<RecordDone>("snapvere:rec-done", (e) => {
        const { error, cancelled } = e.payload;
        if (error) setToast(`Recording failed: ${error}`);
        else if (!cancelled) {
          setToast("Recording saved");
          void load(query);
        }
      }),
      listen<Screenshot>("snapvere:shot-saved", () => void load(query)),
      listen<string>("snapvere:toast", (e) => setToast(e.payload)),
    ];
    return () => {
      subs.forEach((x) => void x.then((un) => un()));
    };
  }, [load, query]);

  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 2600);
    return () => clearTimeout(t);
  }, [toast]);

  const visible = useMemo(() => {
    if (filter === "all") return items;
    const wantVideo = filter === "videos";
    return items.filter((s) => isVideo(s) === wantVideo);
  }, [items, filter]);

  const onDelete = useCallback(async (s: Screenshot) => {
    const what = isVideo(s) ? "recording" : "screenshot";
    const ok = await ask(`"${s.name}" will be removed from the gallery and deleted from disk.`, {
      title: `Delete ${what}?`,
      kind: "warning",
      okLabel: "Delete",
      cancelLabel: "Cancel",
    });
    if (!ok) return false;
    try {
      await ipc.deleteScreenshot(s.id);
      setItems((prev) => prev.filter((x) => x.id !== s.id));
      return true;
    } catch (e) {
      setToast(`Could not delete capture: ${String(e)}`);
      return false;
    }
  }, []);

  const reveal = useCallback(async (s: Screenshot) => {
    try {
      await ipc.revealMedia(s.id);
    } catch (e) {
      setToast(String(e));
    }
  }, []);

  const closeViewer = useCallback(() => setViewId(null), []);

  const captureRegion = async () => {
    try {
      await getCurrentWindow().hide();
      await ipc.beginRegionCapture();
    } catch (e) {
      await getCurrentWindow().show().catch(console.error);
      setToast(`Capture could not start: ${String(e)}`);
    }
  };
  const captureFull = async () => {
    if (captureBusy) return;
    setCaptureBusy(true);
    const win = getCurrentWindow();
    try {
      // Hide our own gallery before the OS takes its framebuffer snapshot.
      // A short compositor delay prevents the disappearing WebView from being
      // captured on slower Windows / WebView2 devices.
      await win.hide();
      await new Promise<void>((resolve) => setTimeout(resolve, 180));
      const { format, quality } = await readCapturePreferences();
      await ipc.captureFullscreen(format, quality);
      await load(query);
      setToast("Full-screen screenshot saved.");
    } catch (e) {
      setToast(`Screenshot failed: ${String(e)}`);
    } finally {
      await win.show().catch(console.error);
      await win.setFocus().catch(console.error);
      setCaptureBusy(false);
    }
  };
  const recordArea = async () => {
    try {
      await getCurrentWindow().hide();
      await ipc.beginRegionCapture("record");
    } catch (e) {
      await getCurrentWindow().show().catch(console.error);
      setToast(`Recording could not start: ${String(e)}`);
    }
  };
  const openSettings = async () => {
    const existing = await WebviewWindow.getByLabel("settings");
    if (existing) return existing.setFocus();
    new WebviewWindow("settings", {
      url: "index.html#settings",
      title: "Snapvere — Settings",
      width: 860,
      height: 660,
      center: true,
    });
  };

  return (
    <WindowFrame title="Snapvere">
      {/* toolbar */}
      <header className="snapvere-app-toolbar flex shrink-0 flex-wrap items-center gap-3 px-5 py-3">
        <div className="relative min-w-[200px] flex-1 max-w-lg">
          <Search size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted" />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search by name or tag…"
            aria-label="Search captures by name or tag"
            className="w-full rounded-full bg-elevated/80 py-2 pl-10 pr-4 text-sm outline-none ring-1 ring-white/10 transition focus:ring-2 focus:ring-accent"
          />
        </div>
        <div className="ml-auto flex flex-wrap items-center gap-2">
          <button disabled={captureBusy} onClick={captureRegion} className="snapvere-btn-primary flex items-center gap-2 rounded-full bg-accent px-4 py-2 text-sm font-semibold text-white transition hover:brightness-110 active:scale-95">
            <Crop size={16} /> Capture region
          </button>
          <button disabled={captureBusy} onClick={recordArea} className="snapvere-btn-secondary flex items-center gap-2 rounded-full px-3.5 py-2 text-sm transition hover:bg-elevated active:scale-95">
            <Video size={16} /> Record
          </button>
          <button disabled={captureBusy} onClick={captureFull} className="snapvere-btn-secondary flex items-center gap-2 rounded-full px-3.5 py-2 text-sm transition hover:bg-elevated active:scale-95">
            <Monitor size={16} /> {captureBusy ? "Capturing…" : "Full screen"}
          </button>
          <button onClick={openSettings} title="Settings" aria-label="Open settings" className="snapvere-btn-secondary grid h-9 w-9 place-items-center rounded-full transition hover:bg-elevated active:scale-95">
            <SettingsIcon size={18} />
          </button>
        </div>
      </header>

      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3 px-5 pb-3 pt-4">
        <div>
          <p className="snapvere-section-eyebrow">LOCAL GALLERY</p>
          <h1 className="snapvere-gallery-title">Your captures</h1>
        </div>
        <p className="text-xs text-muted" aria-live="polite">
          {loading ? "Updating…" : `${visible.length} ${visible.length === 1 ? "item" : "items"}`}
        </p>
      </div>
      {/* filter */}
      <div className="flex shrink-0 items-center gap-1 px-5 pb-3">
        {(["all", "images", "videos"] as Filter[]).map((f) => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            aria-pressed={filter === f}
            className={`rounded-full px-3 py-1 text-xs capitalize transition-colors ${
              filter === f ? "bg-accent text-white" : "text-muted hover:bg-elevated"
            }`}
          >
            {f}
          </button>
        ))}
      </div>

      {/* galerija */}
      <main className="min-h-0 flex-1 overflow-auto px-5 pb-5">
        {loading && items.length === 0 ? (
          <div className="grid h-full place-items-center text-muted">Loading…</div>
        ) : visible.length === 0 ? (
          <EmptyState onCapture={captureRegion} hasQuery={!!query.trim() || filter !== "all"} />
        ) : (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
            {visible.map((s) => (
              <Card
                key={s.id}
                shot={s}
                onOpen={() => setViewId(s.id)}
                onReveal={() => void reveal(s)}
                onDelete={() => void onDelete(s)}
              />
            ))}
          </div>
        )}
      </main>

      {viewId && (
        <Viewer
          items={visible}
          id={viewId}
          onNavigate={setViewId}
          onClose={closeViewer}
          onDelete={onDelete}
          onToast={setToast}
        />
      )}

      {toast && (
        <div role="status" aria-live="polite" className="pointer-events-none absolute bottom-5 left-1/2 z-50 -translate-x-1/2 rounded-full bg-elevated/95 px-4 py-2 text-sm shadow-card ring-1 ring-white/10 animate-pop">
          {toast}
        </div>
      )}
    </WindowFrame>
  );
}

function Card({
  shot,
  onOpen,
  onReveal,
  onDelete,
}: {
  shot: Screenshot;
  onOpen: () => void;
  onReveal: () => void;
  onDelete: () => void;
}) {
  const video = isVideo(shot);
  // MP4 se ne moze prikazati u <img>; bez thumbnaila ide placeholder.
  const src = shot.thumbPath ?? (video ? null : shot.filePath);
  return (
    <article
      role="group"
      aria-label={shot.name}
      className="snapvere-gallery-card group relative overflow-hidden outline-none transition-all duration-200 hover:-translate-y-0.5 hover:shadow-card focus-within:ring-2 focus-within:ring-accent"
    >
      {/* A real focusable button; utility buttons are siblings, never nested in it. */}
      <button
        type="button"
        onClick={onOpen}
        aria-label={`${video ? "Play recording" : "View screenshot"}: ${shot.name}`}
        className="absolute inset-0 z-[1] cursor-pointer rounded-[inherit] focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-accent"
      />
      <div className="pointer-events-none relative aspect-video w-full bg-black/30">
        {src ? (
          <img
            src={convertFileSrc(src)}
            alt={shot.name}
            className="h-full w-full object-cover"
            loading="lazy"
            draggable={false}
          />
        ) : (
          <div className="grid h-full w-full place-items-center text-muted">
            <Video size={26} />
          </div>
        )}
        {video && (
          <>
            <div className="pointer-events-none absolute inset-0 grid place-items-center opacity-0 transition-opacity duration-200 group-hover:opacity-100">
              <span className="grid h-11 w-11 place-items-center rounded-full bg-black/65 text-white shadow-card ring-1 ring-white/20 transition-transform duration-200 group-hover:scale-105">
                <Play size={18} fill="currentColor" className="ml-0.5" />
              </span>
            </div>
            <span className="pointer-events-none absolute bottom-1.5 left-1.5 flex items-center gap-1 rounded bg-black/70 px-1.5 py-0.5 font-mono text-[10px] uppercase text-white">
              <Play size={8} fill="currentColor" />
              {shot.format}
            </span>
          </>
        )}
      </div>
      <div className="pointer-events-none relative px-3 py-2">
        <div className="truncate text-[13px] font-medium">{shot.name}</div>
        <div className="font-mono text-[11px] text-muted">
          {shot.width}×{shot.height} · {fmtBytes(shot.sizeBytes)}
        </div>
      </div>
      <div
        className="absolute right-2 top-2 z-10 flex gap-1 opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100"
        onKeyDown={(e) => e.stopPropagation()}
      >
        <button
          onClick={(e) => {
            e.stopPropagation();
            onReveal();
          }}
          title="Show in folder"
          aria-label={`Show ${shot.name} in folder`}
          className="grid h-8 w-8 place-items-center rounded-full bg-black/55 text-white transition hover:bg-black/75"
        >
          <FolderOpen size={15} />
        </button>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete();
          }}
          title="Delete"
          aria-label={`Delete ${shot.name}`}
          className="grid h-8 w-8 place-items-center rounded-full bg-black/55 text-white transition hover:bg-danger"
        >
          <Trash2 size={15} />
        </button>
      </div>
    </article>
  );
}

function EmptyState({ onCapture, hasQuery }: { onCapture: () => void; hasQuery: boolean }) {
  return (
    <div className="grid h-full place-items-center text-center">
      <div className="max-w-sm">
        <div className="mx-auto mb-4 grid h-14 w-14 place-items-center rounded-2xl bg-elevated/60 ring-1 ring-white/10">
          <ImageOff size={26} className="text-muted" />
        </div>
        <h2 className="mb-1 text-lg font-semibold tracking-tight">{hasQuery ? "No results" : "No screenshots yet"}</h2>
        <p className="mb-5 text-sm text-muted">
          {hasQuery ? "Try a different term." : "Press the hotkey or button to capture your first screenshot."}
        </p>
        {!hasQuery && (
          <button onClick={onCapture} className="snapvere-btn-primary rounded-full bg-accent px-5 py-2.5 text-sm font-semibold text-white transition hover:brightness-110 active:scale-95">
            Capture region
          </button>
        )}
      </div>
    </div>
  );
}
