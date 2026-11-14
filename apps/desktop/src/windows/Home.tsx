import { useCallback, useEffect, useMemo, useRef, useState } from "react";
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
  Video,
} from "lucide-react";
import { ipc } from "../lib/ipc";
import { isVideo } from "../lib/types";
import { mergeUniqueById } from "../lib/galleryPaging";
import type { RecordDone, Screenshot } from "../lib/types";
import WindowFrame from "../components/WindowFrame";
import Viewer from "../components/Viewer";
import CaptureCard from "../components/home/CaptureCard";
import EmptyGallery from "../components/home/EmptyGallery";

type Filter = "all" | "images" | "videos";
const PAGE_SIZE = 120;

export default function Home() {
  const [items, setItems] = useState<Screenshot[]>([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [hasMore, setHasMore] = useState(false);
  const [filter, setFilter] = useState<Filter>("all");
  const [toast, setToast] = useState<string | null>(null);
  const [captureBusy, setCaptureBusy] = useState(false);
  /** Otvoren u pregledniku (id, ne indeks — lista se osvježava ispod njega). */
  const [viewId, setViewId] = useState<string | null>(null);
  const galleryRequest = useRef(0);
  const pagingBusy = useRef(false);
  const pageOffset = useRef(0);

  const load = useCallback(async (q: string) => {
    const request = ++galleryRequest.current;
    setLoading(true);
    try {
      const res = q.trim()
        ? await ipc.searchScreenshots(q, PAGE_SIZE, 0)
        : await ipc.listScreenshots(PAGE_SIZE, 0);
      if (request === galleryRequest.current) {
        pageOffset.current = res.length;
        setItems(res);
        setHasMore(res.length === PAGE_SIZE);
      }
    } catch (e) {
      console.error(e);
      if (request === galleryRequest.current) setToast("Could not load the gallery");
    } finally {
      if (request === galleryRequest.current) setLoading(false);
    }
  }, []);

  const loadMore = useCallback(async () => {
    if (pagingBusy.current || loading || !hasMore) return;
    pagingBusy.current = true;
    const request = galleryRequest.current;
    const offset = pageOffset.current;
    setLoadingMore(true);
    try {
      const res = query.trim()
        ? await ipc.searchScreenshots(query, PAGE_SIZE, offset)
        : await ipc.listScreenshots(PAGE_SIZE, offset);
      if (request !== galleryRequest.current) return;
      pageOffset.current = offset + res.length;
      setItems((previous) => mergeUniqueById(previous, res));
      setHasMore(res.length === PAGE_SIZE);
    } catch (error) {
      if (request === galleryRequest.current) {
        setToast(`Could not load older captures: ${String(error)}`);
      }
    } finally {
      pagingBusy.current = false;
      setLoadingMore(false);
    }
  }, [hasMore, loading, query]);

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
            onChange={(e) => {
              setQuery(e.target.value);
              setHasMore(false);
              pageOffset.current = 0;
            }}
            placeholder="Search by name or tag…"
            maxLength={128}
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
          {loading ? "Updating…" : `${visible.length}${hasMore ? "+" : ""} ${visible.length === 1 ? "item" : "items"} loaded`}
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
          <EmptyGallery onCapture={captureRegion} hasQuery={!!query.trim() || filter !== "all"} />
        ) : (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
            {visible.map((s) => (
              <CaptureCard
                key={s.id}
                shot={s}
                onOpen={() => setViewId(s.id)}
                onReveal={() => void reveal(s)}
                onDelete={() => void onDelete(s)}
              />
            ))}
          </div>
        )}
        {hasMore && (
          <div className="flex justify-center pt-5">
            <button
              type="button"
              onClick={() => void loadMore()}
              disabled={loadingMore || loading}
              aria-label="Load older captures"
              className="snapvere-btn-secondary rounded-full px-5 py-2.5 text-sm font-semibold transition hover:bg-elevated disabled:opacity-60"
            >
              {loadingMore ? "Loading older captures…" : "Load more captures"}
            </button>
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
