import { useCallback, useEffect, useMemo, useState } from "react";
import { convertFileSrc } from "@tauri-apps/api/core";
import { WebviewWindow } from "@tauri-apps/api/webviewWindow";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { ask } from "@tauri-apps/plugin-dialog";
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
  /** Otvoren u pregledniku (id, ne indeks — lista se osvježava ispod njega). */
  const [viewId, setViewId] = useState<string | null>(null);

  const load = useCallback(async (q: string) => {
    setLoading(true);
    try {
      const res = q.trim() ? await ipc.searchScreenshots(q) : await ipc.listScreenshots(300, 0);
      setItems(res);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const t = setTimeout(() => void load(query), 200);
    return () => clearTimeout(t);
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
    await ipc.deleteScreenshot(s.id, s.filePath);
    setItems((prev) => prev.filter((x) => x.id !== s.id));
    return true;
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
    await getCurrentWindow().hide();
    await ipc.beginRegionCapture();
  };
  const captureFull = async () => {
    await ipc.captureFullscreen("png", 100);
    await load(query);
  };
  const recordArea = async () => {
    await getCurrentWindow().hide();
    await ipc.beginRegionCapture("record");
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
      <header className="flex shrink-0 items-center gap-3 px-5 py-3">
        <div className="relative flex-1 max-w-md">
          <Search size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-muted" />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search by name or tag…"
            className="w-full rounded-full bg-elevated/80 py-2 pl-10 pr-4 text-sm outline-none ring-1 ring-white/10 transition focus:ring-2 focus:ring-accent"
          />
        </div>
        <div className="ml-auto flex items-center gap-2">
          <button onClick={captureRegion} className="flex items-center gap-2 rounded-full bg-accent px-4 py-2 text-sm font-medium text-white shadow-glow transition hover:brightness-110 active:scale-95">
            <Crop size={16} /> Capture region
          </button>
          <button onClick={recordArea} className="flex items-center gap-2 rounded-full px-3.5 py-2 text-sm ring-1 ring-white/10 transition hover:bg-elevated active:scale-95">
            <Video size={16} /> Record
          </button>
          <button onClick={captureFull} className="flex items-center gap-2 rounded-full px-3.5 py-2 text-sm ring-1 ring-white/10 transition hover:bg-elevated active:scale-95">
            <Monitor size={16} /> Full screen
          </button>
          <button onClick={openSettings} title="Settings" className="grid h-9 w-9 place-items-center rounded-full ring-1 ring-white/10 transition hover:bg-elevated active:scale-95">
            <SettingsIcon size={18} />
          </button>
        </div>
      </header>

      {/* filter */}
      <div className="flex shrink-0 items-center gap-1 px-5 pb-2">
        {(["all", "images", "videos"] as Filter[]).map((f) => (
          <button
            key={f}
            onClick={() => setFilter(f)}
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
        <div className="pointer-events-none absolute bottom-5 left-1/2 z-50 -translate-x-1/2 rounded-full bg-elevated/95 px-4 py-2 text-sm shadow-card ring-1 ring-white/10 animate-pop">
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
    <div
      role="button"
      tabIndex={0}
      onClick={onOpen}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onOpen();
        }
      }}
      title={video ? "Play" : "View"}
      className="group relative cursor-pointer overflow-hidden rounded-2xl bg-elevated/60 outline-none ring-1 ring-white/10 transition-all duration-200 hover:-translate-y-0.5 hover:shadow-card hover:ring-white/20 focus-visible:ring-2 focus-visible:ring-accent"
    >
      <div className="relative aspect-video w-full bg-black/30">
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
      <div className="px-3 py-2">
        <div className="truncate text-[13px] font-medium">{shot.name}</div>
        <div className="font-mono text-[11px] text-muted">
          {shot.width}×{shot.height} · {fmtBytes(shot.sizeBytes)}
        </div>
      </div>
      <div
        className="absolute right-2 top-2 flex gap-1 opacity-0 transition-opacity group-hover:opacity-100"
        onKeyDown={(e) => e.stopPropagation()}
      >
        <button
          onClick={(e) => {
            e.stopPropagation();
            onReveal();
          }}
          title="Show in folder"
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
          className="grid h-8 w-8 place-items-center rounded-full bg-black/55 text-white transition hover:bg-danger"
        >
          <Trash2 size={15} />
        </button>
      </div>
    </div>
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
          <button onClick={onCapture} className="rounded-full bg-accent px-5 py-2.5 text-sm font-medium text-white shadow-glow transition hover:brightness-110 active:scale-95">
            Capture region
          </button>
        )}
      </div>
    </div>
  );
}
