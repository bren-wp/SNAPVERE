import React, { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import {
  MousePointer2,
  ArrowUpRight,
  Square,
  Circle,
  Minus,
  PenTool,
  Highlighter,
  EyeOff,
  Type,
  ListOrdered,
  Pipette,
  Undo2,
  Redo2,
  Copy,
  Download,
  Save,
  X,
  Monitor as MonitorIcon,
  Mic,
  MicOff,
  Volume2,
  VolumeX,
} from "lucide-react";
import { load as loadStore } from "@tauri-apps/plugin-store";
import { save as saveDialog } from "@tauri-apps/plugin-dialog";
import { listen } from "@tauri-apps/api/event";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { convertFileSrc } from "@tauri-apps/api/core";
import { ipc } from "../lib/ipc";
import { renderAnnotations } from "../lib/canvas";
import type {
  Annotation,
  AnnotationTool,
  CapturePayload,
  EditorTool,
  Point,
} from "../lib/types";

interface Rect {
  x: number;
  y: number;
  w: number;
  h: number;
}
type Dir = "nw" | "n" | "ne" | "e" | "se" | "s" | "sw" | "w";
type DragOp = { type: "move" | "resize"; dir: Dir; sx: number; sy: number; start: Rect };

const TOOLS: { id: EditorTool; icon: typeof Square; label: string }[] = [
  { id: "select", icon: MousePointer2, label: "Move / resize frame" },
  { id: "arrow", icon: ArrowUpRight, label: "Arrow" },
  { id: "rect", icon: Square, label: "Rectangle" },
  { id: "ellipse", icon: Circle, label: "Ellipse" },
  { id: "line", icon: Minus, label: "Line" },
  { id: "pen", icon: PenTool, label: "Pen" },
  { id: "marker", icon: Highlighter, label: "Marker" },
  { id: "step", icon: ListOrdered, label: "Numbered step" },
  { id: "blur", icon: EyeOff, label: "Blur / redact" },
  { id: "text", icon: Type, label: "Text" },
];

const SWATCHES = [
  "#FF453A", "#FF9F0A", "#FFD60A", "#30D158", "#40C8E0",
  "#0A84FF", "#BF5AF2", "#FF375F", "#FFFFFF", "#1C1C1E",
];

const MIN = 12;
/** Koliko cloakan overlay čeka nakon iscrtanog framea da ga DWM preuzme (ms). */
const REVEAL_DELAY_MS = 34;
const sleep = (ms: number) => new Promise<void>((r) => setTimeout(r, ms));
/** Dva animation framea = DOM i canvas su predani kompozitoru. Timeout je osigurač:
 *  cloakan prozor ne smije ostati nevidljiv ako rAF iz nekog razloga ne stigne. */
const painted = () =>
  new Promise<void>((resolve) => {
    let done = false;
    const finish = () => {
      if (!done) { done = true; resolve(); }
    };
    requestAnimationFrame(() => requestAnimationFrame(finish));
    setTimeout(finish, 200);
  });
let counter = 0;
const uid = () => `a${++counter}`;
const textFont = (size: number) => Math.round(size * 3.5 + 8);
const cursorFor = (d: Dir) =>
  d === "nw" || d === "se" ? "nwse-resize" : d === "ne" || d === "sw" ? "nesw-resize" : d === "n" || d === "s" ? "ns-resize" : "ew-resize";

export default function Overlay() {
  const [imgReady, setImgReady] = useState(false);
  const [phase, setPhase] = useState<"select" | "edit">("select");
  /** "shot" = snimka zaslona s anotacijama, "record" = odabir područja za video. */
  const [mode, setMode] = useState<"shot" | "record">("shot");
  const [monId, setMonId] = useState(0);
  const [starting, setStarting] = useState(false);
  // Zvuk se bira po snimci, a izbor se pamti u settings.json (isti ključevi kao u Settings).
  const [mic, setMic] = useState(false);
  const [sysAudio, setSysAudio] = useState(false);
  const [gifFormat, setGifFormat] = useState(false);
  const [sel, setSel] = useState<Rect | null>(null);
  const [cursor, setCursor] = useState({ x: 0, y: 0 });

  const [tool, setTool] = useState<EditorTool>("arrow");
  const [color, setColor] = useState("#FF453A");
  const [size, setSize] = useState(6);
  const [popover, setPopover] = useState<null | "color" | "size">(null);

  const [annotations, setAnnotations] = useState<Annotation[]>([]);
  const [redo, setRedo] = useState<Annotation[]>([]);
  const [textEdit, setTextEdit] = useState<Point | null>(null);
  const [textValue, setTextValue] = useState("");
  const [toast, setToast] = useState<string | null>(null);

  const frozenRef = useRef<HTMLCanvasElement>(null);
  const frozenCtxRef = useRef<CanvasRenderingContext2D | null>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const loupeRef = useRef<HTMLCanvasElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const dragStart = useRef<Point | null>(null);
  const drawing = useRef(false);
  const draft = useRef<Annotation | null>(null);
  const dragOp = useRef<DragOp | null>(null);
  const prevTool = useRef<EditorTool>("arrow");
  const barRef = useRef<HTMLDivElement>(null);
  const [barW, setBarW] = useState(640);
  /** Fizički px po CSS px — u record modu nema zamrznutog canvasa iz kojeg bi se izveo omjer. */
  const scaleRef = useRef(1);
  /** Token zadnjeg capture-a — handler starijeg (drugi brzi PrintScreen) ne smije prikazati prozor. */
  const latestToken = useRef(0);

  const ratio = () =>
    frozenRef.current && frozenRef.current.width ? frozenRef.current.width / window.innerWidth : 1;

  // Povuci zamrznuti frame (monitora `id`) kao sirove RGBA bajtove i ubaci ga u frozen canvas.
  // Učitaj zamrznuti frame preko asset protokola (nativno, brzo) i iscrtaj u frozen canvas.
  const loadFrozen = useCallback((path: string, token: number) => {
    return new Promise<void>((resolve) => {
      const img = new Image();
      img.crossOrigin = "anonymous"; // asset protokol šalje CORS → canvas ostaje čist (export radi)
      img.onload = () => {
        const c = frozenRef.current;
        if (c) {
          c.width = img.naturalWidth;
          c.height = img.naturalHeight;
          const ctx = c.getContext("2d", { willReadFrequently: true })!;
          ctx.drawImage(img, 0, 0);
          frozenCtxRef.current = ctx;
          setImgReady(true);
        }
        resolve();
      };
      img.onerror = () => resolve();
      img.src = `${convertFileSrc(path)}?t=${token}`;
    });
  }, []);

  // Event iz Rusta: novi capture (payload = monitor id) → reset + povuci svjež frame.
  useEffect(() => {
    const un = listen<CapturePayload>("snapvere:capture", async (e) => {
      const token = e.payload.token;
      latestToken.current = token;
      setMode(e.payload.mode === "record" ? "record" : "shot");
      setMonId(e.payload.id);
      setStarting(false);
      setPhase("select");
      setSel(null);
      setAnnotations([]);
      setRedo([]);
      setTextEdit(null);
      setTextValue("");
      setTool("arrow");
      setPopover(null);
      draft.current = null;
      drawing.current = false;
      dragOp.current = null;
      setImgReady(false);
      const w = getCurrentWindow();
      scaleRef.current = await w.scaleFactor().catch(() => 1);

      if (e.payload.mode === "record") {
        // Record mode ne zamrzava ekran: prozor je proziran, ispod je živi desktop,
        // pa nema ni screenshota ni učitavanja — overlay se pojavi odmah.
        document.body.classList.add("rec-live");
        void loadStore("settings.json", { autoSave: true, defaults: {} })
          .then(async (st) => {
            setMic((await st.get<boolean>("recordMic")) ?? false);
            setSysAudio((await st.get<boolean>("recordSystemAudio")) ?? false);
            setGifFormat(((await st.get<string>("recordFormat")) ?? "mp4") === "gif");
          })
          .catch(() => undefined);
        const c = frozenRef.current;
        if (c) {
          c.width = 1;
          c.height = 1;
        }
        frozenCtxRef.current = null;
      } else {
        document.body.classList.remove("rec-live");
        await loadFrozen(e.payload.path, e.payload.token);
      }
      if (latestToken.current !== token) return; // u međuvremenu je stigao noviji capture
      // Rust je prozor cloakao: show() pusti Chromium da iscrta NOVI frame dok ga DWM
      // još ne prikazuje. Bez toga bi ekran ~50 ms pokazivao staru sliku (često s drugog
      // monitora), crninu ili šarene smetnje — tek onda otkrivamo prozor.
      await w.show();
      await painted();
      await sleep(REVEAL_DELAY_MS);
      // Drugi PrintScreen / Esc u tih ~70 ms već je sakrio prozor — ne fokusiraj ga natrag.
      if (latestToken.current !== token || !(await w.isVisible())) return;
      await ipc.revealOverlay();
      await w.setAlwaysOnTop(true);
      await w.setFocus();
    });
    return () => {
      void un.then((f) => f());
    };
  }, [loadFrozen]);

  useLayoutEffect(() => {
    if (barRef.current) setBarW(barRef.current.offsetWidth);
  }, [phase, popover, tool]);

  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 1600);
    return () => clearTimeout(t);
  }, [toast]);

  const pixelHex = (ax: number, ay: number): string => {
    const ctx = frozenCtxRef.current;
    const c = frozenRef.current;
    if (!ctx || !c) return "#000000";
    const r = ratio();
    const px = Math.max(0, Math.min(Math.round(ax * r), c.width - 1));
    const py = Math.max(0, Math.min(Math.round(ay * r), c.height - 1));
    try {
      const d = ctx.getImageData(px, py, 1, 1).data;
      return "#" + [d[0], d[1], d[2]].map((v) => v.toString(16).padStart(2, "0")).join("");
    } catch {
      // Tainted canvas (asset protokol bez CORS-a) — bez ovoga bi throw iz rendera
      // srušio cijeli overlay u bijeli ekran.
      return "#000000";
    }
  };

  // ── redraw anotacija (apsolutne CSS koordinate) ──
  const redraw = useCallback(() => {
    const c = canvasRef.current;
    const fz = frozenRef.current;
    if (!c || !fz || !sel) return;
    const r = ratio();
    const W = Math.max(1, Math.round(sel.w * r));
    const H = Math.max(1, Math.round(sel.h * r));
    if (c.width !== W) c.width = W;
    if (c.height !== H) c.height = H;
    const ctx = c.getContext("2d")!;
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, W, H);
    ctx.setTransform(r, 0, 0, r, -sel.x * r, -sel.y * r);
    renderAnnotations(ctx, annotations, draft.current, { frozen: fz, originX: 0, originY: 0, scale: r });
  }, [annotations, sel]);

  useEffect(() => {
    if (phase === "edit" && mode === "shot") redraw();
  }, [phase, mode, annotations, sel, redraw]);

  // ── loupe ──
  const loupeVisible =
    mode === "shot" && (phase === "select" || (phase === "edit" && tool === "eyedropper"));
  useEffect(() => {
    if (!loupeVisible || !imgReady) return;
    const c = loupeRef.current;
    const fz = frozenRef.current;
    if (!c || !fz) return;
    const ctx = c.getContext("2d")!;
    const Z = 8;
    const span = Math.round(c.width / Z);
    const r = ratio();
    const cx = Math.round(cursor.x * r);
    const cy = Math.round(cursor.y * r);
    ctx.imageSmoothingEnabled = false;
    ctx.clearRect(0, 0, c.width, c.height);
    ctx.drawImage(fz, cx - span / 2, cy - span / 2, span, span, 0, 0, c.width, c.height);
    const mid = c.width / 2;
    ctx.strokeStyle = "rgba(10,132,255,0.95)";
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(mid, 0); ctx.lineTo(mid, c.height);
    ctx.moveTo(0, mid); ctx.lineTo(c.width, mid);
    ctx.stroke();
    ctx.strokeStyle = "rgba(255,255,255,0.95)";
    ctx.strokeRect(mid - Z / 2, mid - Z / 2, Z, Z);
  }, [cursor, loupeVisible, imgReady]);

  // ── drag-op (move / resize selekcije) ──
  useEffect(() => {
    const move = (e: MouseEvent) => {
      const op = dragOp.current;
      if (!op) return;
      const dx = e.clientX - op.sx;
      const dy = e.clientY - op.sy;
      setSel(op.type === "move" ? clampRect({ ...op.start, x: op.start.x + dx, y: op.start.y + dy }) : applyResize(op.dir, op.start, dx, dy));
    };
    const up = () => {
      dragOp.current = null;
    };
    window.addEventListener("mousemove", move);
    window.addEventListener("mouseup", up);
    return () => {
      window.removeEventListener("mousemove", move);
      window.removeEventListener("mouseup", up);
    };
  }, []);

  const startDrag = (e: React.MouseEvent, type: "move" | "resize", dir: Dir) => {
    if (!sel) return;
    e.stopPropagation();
    dragOp.current = { type, dir, sx: e.clientX, sy: e.clientY, start: sel };
  };

  // Robustan fokus inline text inputa (autoFocus zna zakazati u borderless overlayu).
  useEffect(() => {
    if (!textEdit) return;
    const focus = () => inputRef.current?.focus();
    focus();
    const r = requestAnimationFrame(focus);
    const t1 = setTimeout(focus, 30);
    const t2 = setTimeout(focus, 90);
    return () => {
      cancelAnimationFrame(r);
      clearTimeout(t1);
      clearTimeout(t2);
    };
  }, [textEdit]);

  // ── tipkovnica ──
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        if (textEdit) { setTextEdit(null); setTextValue(""); }
        else void ipc.cancelCapture();
        return;
      }
      if (mode === "record") {
        if (e.key === "Enter") {
          e.preventDefault();
          void beginRecording(phase !== "edit" || !sel);
        }
        return; // u record modu nema anotacijskih prečaca
      }
      if (phase !== "edit" || textEdit) return;
      const k = e.key.toLowerCase();
      if (e.ctrlKey && k === "z") { e.preventDefault(); undo(); }
      else if (e.ctrlKey && (k === "y" || (e.shiftKey && k === "z"))) { e.preventDefault(); doRedo(); }
      else if (e.ctrlKey && k === "c") { e.preventDefault(); void finish("copy"); }
      else if (e.ctrlKey && k === "s") { e.preventDefault(); void finish("save"); }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  });

  // ── selekcija (phase select) ──
  const onSelDown = (e: React.MouseEvent) => {
    if (phase !== "select") return;
    dragStart.current = { x: e.clientX, y: e.clientY };
    setSel({ x: e.clientX, y: e.clientY, w: 0, h: 0 });
  };
  const onSelMove = (e: React.MouseEvent) => {
    if (phase === "select" || tool === "eyedropper") setCursor({ x: e.clientX, y: e.clientY });
    if (phase !== "select" || !dragStart.current) return;
    const s = dragStart.current;
    setSel({ x: Math.min(s.x, e.clientX), y: Math.min(s.y, e.clientY), w: Math.abs(e.clientX - s.x), h: Math.abs(e.clientY - s.y) });
  };
  const onSelUp = () => {
    if (phase !== "select") return;
    dragStart.current = null;
    if (sel && sel.w > 6 && sel.h > 6) setPhase("edit");
    else setSel(null);
  };

  // ── crtanje (phase edit) ──
  const onCanvasDown = (e: React.MouseEvent) => {
    if (textEdit) { commitText(); return; } // klik drugdje potvrđuje otvoreni tekst
    if (tool === "select" || tool === "eyedropper") return;
    setPopover(null);
    const p = { x: e.clientX, y: e.clientY };
    if (tool === "text") { setTextEdit(p); setTextValue(""); return; }
    if (tool === "step") {
      const n = annotations.filter((a) => a.tool === "step").length + 1;
      commit({ id: uid(), tool: "step", color, size, points: [p], text: String(n) });
      return;
    }
    drawing.current = true;
    draft.current = {
      id: uid(),
      tool: tool as AnnotationTool,
      color,
      size,
      points: tool === "pen" || tool === "marker" ? [p] : [p, p],
    };
    redraw();
  };
  const onCanvasMove = (e: React.MouseEvent) => {
    if (!drawing.current || !draft.current) return;
    const p = { x: e.clientX, y: e.clientY };
    if (tool === "pen" || tool === "marker") draft.current.points.push(p);
    else draft.current.points[1] = p;
    redraw();
  };
  const onCanvasUp = () => {
    if (!drawing.current || !draft.current) return;
    drawing.current = false;
    commit(draft.current);
    draft.current = null;
  };

  const commit = (a: Annotation) => { setAnnotations((p) => [...p, a]); setRedo([]); };
  const undo = () =>
    setAnnotations((p) => {
      if (!p.length) return p;
      setRedo((r) => [...r, p[p.length - 1]]);
      return p.slice(0, -1);
    });
  const doRedo = () =>
    setRedo((r) => {
      if (!r.length) return r;
      setAnnotations((p) => [...p, r[r.length - 1]]);
      return r.slice(0, -1);
    });

  const commitText = () => {
    const v = textValue.trim();
    if (v && textEdit) commit({ id: uid(), tool: "text", color, size: textFont(size), points: [textEdit], text: v });
    setTextEdit(null);
    setTextValue("");
  };

  const pickColor = (e: React.MouseEvent) => {
    const hex = pixelHex(e.clientX, e.clientY);
    setColor(hex);
    setTool(prevTool.current === "eyedropper" ? "arrow" : prevTool.current);
    setToast(`Color ${hex.toUpperCase()}`);
  };

  const selectTool = (id: EditorTool) => {
    if (id !== "eyedropper") prevTool.current = id;
    setTool(id);
    setPopover(null);
  };

  // ── export → kopiraj / spremi / spremi kao ──
  const exportPng = (): string => {
    const fz = frozenRef.current!;
    const r = ratio();
    const ex = document.createElement("canvas");
    ex.width = Math.round(sel!.w * r);
    ex.height = Math.round(sel!.h * r);
    const ctx = ex.getContext("2d")!;
    ctx.drawImage(fz, sel!.x * r, sel!.y * r, sel!.w * r, sel!.h * r, 0, 0, ex.width, ex.height);
    ctx.setTransform(r, 0, 0, r, -sel!.x * r, -sel!.y * r);
    renderAnnotations(ctx, annotations, null, { frozen: fz, originX: 0, originY: 0, scale: r });
    return ex.toDataURL("image/png").split(",")[1];
  };

  const closeSoon = () => setTimeout(() => void ipc.cancelCapture(), 650);

  // ── snimanje ekrana ──
  /** Selekcija u FIZIČKIM pikselima monitora — backend crop očekuje baš to. */
  const selToRegion = () => {
    if (!sel) return null;
    const r = scaleRef.current;
    return {
      monitorId: monId,
      x: Math.round(sel.x * r),
      y: Math.round(sel.y * r),
      width: Math.round(sel.w * r),
      height: Math.round(sel.h * r),
    };
  };

  const setAudio = useCallback(async (key: "recordMic" | "recordSystemAudio", v: boolean) => {
    if (key === "recordMic") setMic(v);
    else setSysAudio(v);
    try {
      const st = await loadStore("settings.json", { autoSave: true, defaults: {} });
      await st.set(key, v);
    } catch (e) {
      console.error(e);
    }
  }, []);

  const beginRecording = useCallback(
    async (fullScreen: boolean) => {
      if (starting) return;
      setStarting(true);
      try {
        const region = fullScreen ? null : selToRegion();
        await ipc.startRecording(region, monId);
        // Backend sakriva overlay i diže HUD; ovdje nema više posla.
      } catch (err) {
        console.error(err);
        setToast(String(err));
        setStarting(false);
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [sel, monId, starting],
  );

  const finish = useCallback(
    async (action: "copy" | "save") => {
      if (!sel || !frozenRef.current) return;
      try {
        const b64 = exportPng();
        if (action === "copy") { await ipc.copyBase64ToClipboard(b64); setToast("Copied to clipboard"); }
        else { await ipc.saveEditedImage(b64, "png", 100); setToast("Saved to gallery"); }
        closeSoon();
      } catch (err) {
        console.error(err);
        await ipc.cancelCapture();
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [sel, annotations],
  );

  const saveAs = async () => {
    if (!sel) return;
    try {
      const path = await saveDialog({
        defaultPath: "Snapvere.png",
        filters: [
          { name: "PNG", extensions: ["png"] },
          { name: "JPEG", extensions: ["jpg"] },
          { name: "WebP", extensions: ["webp"] },
        ],
      });
      if (!path) return;
      const fmt = path.toLowerCase().endsWith(".jpg") ? "jpg" : path.toLowerCase().endsWith(".webp") ? "webp" : "png";
      await ipc.saveImageAs(exportPng(), path, fmt, 92);
      setToast("Saved");
      closeSoon();
    } catch (err) {
      console.error(err);
    }
  };

  // ── layout toolbara ──
  const barPos = (() => {
    if (!sel) return { left: 0, top: 0 };
    const margin = 12;
    const half = Math.min(barW, window.innerWidth - 24) / 2;
    const cx = Math.max(half + margin, Math.min(sel.x + sel.w / 2, window.innerWidth - half - margin));
    let top: number;
    if (sel.y + sel.h + 66 < window.innerHeight) top = sel.y + sel.h + 14;
    else if (sel.y - 64 > 0) top = sel.y - 60;
    else top = window.innerHeight - 72;
    return { left: cx, top };
  })();

  const handles: { dir: Dir; left: number; top: number }[] = sel
    ? [
        { dir: "nw", left: sel.x, top: sel.y },
        { dir: "n", left: sel.x + sel.w / 2, top: sel.y },
        { dir: "ne", left: sel.x + sel.w, top: sel.y },
        { dir: "e", left: sel.x + sel.w, top: sel.y + sel.h / 2 },
        { dir: "se", left: sel.x + sel.w, top: sel.y + sel.h },
        { dir: "s", left: sel.x + sel.w / 2, top: sel.y + sel.h },
        { dir: "sw", left: sel.x, top: sel.y + sel.h },
        { dir: "w", left: sel.x, top: sel.y + sel.h / 2 },
      ]
    : [];

  const loupePos = (() => {
    const off = 22;
    const sizePx = 150;
    let left = cursor.x + off;
    let top = cursor.y + off;
    if (left + sizePx > window.innerWidth) left = cursor.x - off - sizePx;
    if (top + sizePx > window.innerHeight) top = cursor.y - off - sizePx;
    return { left, top };
  })();

  const hex = loupeVisible && imgReady ? pixelHex(cursor.x, cursor.y) : "#000000";

  return (
    <div
      className="fixed inset-0 select-none"
      style={{ cursor: phase === "select" ? "crosshair" : "default" }}
      onMouseDown={onSelDown}
      onMouseMove={onSelMove}
      onMouseUp={onSelUp}
    >
      <canvas ref={frozenRef} className="pointer-events-none absolute inset-0 h-full w-full" />


      {/* Dim — 4 pravokutnika oko selekcije (jeftino za prepaint, za razliku od 9999px box-shadowa) */}
      {!sel ? (
        <div className={`pointer-events-none absolute inset-0 ${mode === "record" ? "bg-black/10" : "bg-black/20"}`} />
      ) : (
        <>
          <div className="pointer-events-none absolute left-0 right-0 top-0 bg-black/40" style={{ height: Math.max(0, sel.y) }} />
          <div className="pointer-events-none absolute bottom-0 left-0 right-0 bg-black/40" style={{ top: sel.y + sel.h }} />
          <div className="pointer-events-none absolute bg-black/40" style={{ left: 0, top: sel.y, width: Math.max(0, sel.x), height: sel.h }} />
          <div className="pointer-events-none absolute bg-black/40" style={{ left: sel.x + sel.w, right: 0, top: sel.y, height: sel.h }} />
        </>
      )}

      {!sel && (
        <div className="lg-chip pointer-events-none absolute left-1/2 top-7 -translate-x-1/2 text-sm text-white animate-fade">
          {mode === "record" ? (
            <>
              Drag to select the recording area &nbsp;·&nbsp;
              <kbd className="font-mono">Enter</kbd> full screen &nbsp;·&nbsp;
              <kbd className="font-mono">Esc</kbd> to exit
            </>
          ) : (
            <>
              Drag to select &nbsp;·&nbsp; <kbd className="font-mono">Esc</kbd> to exit
            </>
          )}
        </div>
      )}

      {/* Selekcijski rub */}
      {sel && (
        <div
          className="pointer-events-none absolute"
          style={{ left: sel.x, top: sel.y, width: sel.w, height: sel.h, outline: "1.5px solid rgba(10,132,255,0.95)", borderRadius: 2 }}
        >
          {(phase === "select" || dragOp.current) && (
            <div className="lg-chip absolute -top-8 left-0 text-xs text-white">
              {Math.round(sel.w)} × {Math.round(sel.h)}
            </div>
          )}
        </div>
      )}

      {/* Annotation canvas */}
      {sel && phase === "edit" && mode === "shot" && (
        <canvas
          ref={canvasRef}
          onMouseDown={onCanvasDown}
          onMouseMove={onCanvasMove}
          onMouseUp={onCanvasUp}
          onMouseLeave={onCanvasUp}
          className="absolute z-10"
          style={{
            left: sel.x, top: sel.y, width: sel.w, height: sel.h,
            cursor: tool === "select" ? "default" : tool === "text" ? "text" : "crosshair",
            pointerEvents: tool === "select" ? "none" : "auto",
          }}
        />
      )}

      {/* Move layer — pomak povlačenjem unutar okvira (uvijek u record modu) */}
      {sel && phase === "edit" && (mode === "record" || tool === "select") && (
        <div
          className="absolute z-20"
          style={{ left: sel.x, top: sel.y, width: sel.w, height: sel.h, cursor: "move" }}
          onMouseDown={(e) => startDrag(e, "move", "nw")}
        />
      )}

      {/* Move trake po rubu — povuci rub da pomakneš okvir (u svakom alatu) */}
      {sel &&
        phase === "edit" &&
        [
          { k: "t", s: { left: sel.x - 6, top: sel.y - 6, width: sel.w + 12, height: 12 } },
          { k: "b", s: { left: sel.x - 6, top: sel.y + sel.h - 6, width: sel.w + 12, height: 12 } },
          { k: "l", s: { left: sel.x - 6, top: sel.y + 6, width: 12, height: Math.max(0, sel.h - 12) } },
          { k: "r", s: { left: sel.x + sel.w - 6, top: sel.y + 6, width: 12, height: Math.max(0, sel.h - 12) } },
        ].map((b) => (
          <div
            key={b.k}
            className="absolute z-20"
            style={{ ...b.s, cursor: "move" }}
            onMouseDown={(e) => startDrag(e, "move", "nw")}
          />
        ))}

      {/* Eyedropper layer (pick s bilo kojeg mjesta) */}
      {sel && phase === "edit" && tool === "eyedropper" && (
        <div
          className="absolute inset-0 z-20"
          style={{ cursor: "crosshair" }}
          onMouseMove={(e) => setCursor({ x: e.clientX, y: e.clientY })}
          onMouseDown={pickColor}
        />
      )}

      {/* Resize handles */}
      {sel && phase === "edit" &&
        handles.map((h) => (
          <div
            key={h.dir}
            className="absolute z-50 h-3 w-3 -translate-x-1/2 -translate-y-1/2 rounded-full bg-white shadow-[0_1px_4px_rgba(0,0,0,0.5)] ring-1 ring-accent"
            style={{ left: h.left, top: h.top, cursor: cursorFor(h.dir) }}
            onMouseDown={(e) => startDrag(e, "resize", h.dir)}
          />
        ))}

      {/* Inline text editor */}
      {sel && textEdit && (
        <input
          ref={inputRef}
          autoFocus
          value={textValue}
          onChange={(e) => setTextValue(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") commitText();
            else if (e.key === "Escape") { setTextEdit(null); setTextValue(""); }
            e.stopPropagation();
          }}
          placeholder="Type…"
          className="absolute z-40 border-0 bg-black/25 p-0 outline-none placeholder:text-white/45"
          style={{
            left: textEdit.x, top: textEdit.y, color, caretColor: color,
            fontSize: textFont(size), fontWeight: 600,
            fontFamily: '-apple-system, "SF Pro Display", Inter, sans-serif',
            minWidth: 90, outline: "1px dashed rgba(255,255,255,0.65)", borderRadius: 4,
          }}
        />
      )}

      {/* Magnifier loupe */}
      {loupeVisible && imgReady && (
        <div className="pointer-events-none absolute z-40 animate-fade" style={{ left: loupePos.left, top: loupePos.top }}>
          <div className="overflow-hidden rounded-2xl ring-1 ring-white/40 shadow-[0_10px_30px_rgba(0,0,0,0.5)]">
            <canvas ref={loupeRef} width={120} height={120} className="block" />
          </div>
          <div className="lg-chip mt-1.5 inline-flex items-center gap-2 text-[11px] text-white">
            <span className="font-mono">{Math.round(cursor.x)}, {Math.round(cursor.y)}</span>
            <span className="inline-block h-3 w-3 rounded-sm ring-1 ring-white/40" style={{ background: hex }} />
            <span className="font-mono uppercase">{hex}</span>
          </div>
        </div>
      )}

      {/* Toast */}
      {toast && (
        <div className="lg-chip pointer-events-none absolute left-1/2 top-7 z-50 -translate-x-1/2 text-sm text-white animate-pop">
          {toast}
        </div>
      )}


      {/* Liquid-glass toolbar — dvije grupe: alati | akcije */}
      {sel && phase === "edit" && mode === "shot" && (
        <div ref={barRef} className="absolute z-30 flex -translate-x-1/2 items-start gap-2" style={{ left: barPos.left, top: barPos.top }} onMouseDown={(e) => e.stopPropagation()}>
          {popover === "color" && (
            <div className="lg lg-pop animate-pop left-0 w-[228px]" style={{ bottom: "calc(100% + 10px)" }}>
              <div className="grid grid-cols-5 gap-2">
                {SWATCHES.map((c) => (
                  <button key={c} onClick={() => { setColor(c); setPopover(null); }} className="h-7 w-7 rounded-full border-2 transition-transform hover:scale-110" style={{ background: c, borderColor: color === c ? "#fff" : "transparent" }} />
                ))}
              </div>
              <div className="mt-3 flex items-center justify-between">
                <label className="flex items-center gap-2 text-xs text-white/80">
                  Custom
                  <input type="color" value={color} onChange={(e) => setColor(e.target.value)} className="h-7 w-9 cursor-pointer rounded bg-transparent" />
                </label>
                <button onClick={() => selectTool("eyedropper")} className="flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs text-white hover:bg-white/15">
                  <Pipette size={14} /> Eyedropper
                </button>
              </div>
            </div>
          )}
          {popover === "size" && (
            <div className="lg lg-pop animate-pop left-0 w-[208px]" style={{ bottom: "calc(100% + 10px)" }}>
              <div className="mb-2 flex items-center justify-center gap-3">
                {[3, 6, 10, 16].map((s) => (
                  <button key={s} onClick={() => setSize(s)} className="grid h-8 w-8 place-items-center rounded-full hover:bg-white/10">
                    <span className="rounded-full bg-white" style={{ width: s, height: s }} />
                  </button>
                ))}
              </div>
              <input type="range" min={2} max={28} value={size} onChange={(e) => setSize(+e.target.value)} className="w-full" />
            </div>
          )}

          {/* Grupa 1 — alati */}
          <div className="lg lg-bar animate-pop">
            {TOOLS.map((t) => (
              <button key={t.id} title={t.label} onClick={() => selectTool(t.id)} className={`lg-btn ${tool === t.id ? "is-active" : ""}`}>
                <t.icon size={16} strokeWidth={2} />
              </button>
            ))}
            <span className="lg-sep" />
            <button title="Color" onClick={() => setPopover(popover === "color" ? null : "color")} className="lg-btn">
              <span className="h-4 w-4 rounded-full border border-white/40" style={{ background: color }} />
            </button>
            <button title="Thickness" onClick={() => setPopover(popover === "size" ? null : "size")} className="lg-btn">
              <span className="rounded-full bg-current" style={{ width: Math.min(16, size + 2), height: Math.min(16, size + 2) }} />
            </button>
            <span className="lg-sep" />
            <button title="Undo (Ctrl+Z)" onClick={undo} disabled={!annotations.length} className="lg-btn">
              <Undo2 size={16} />
            </button>
            <button title="Redo (Ctrl+Y)" onClick={doRedo} disabled={!redo.length} className="lg-btn">
              <Redo2 size={16} />
            </button>
          </div>

          {/* Grupa 2 — akcije */}
          <div className="lg lg-bar animate-pop">
            <button title="Copy (Ctrl+C)" onClick={() => finish("copy")} className="lg-btn">
              <Copy size={16} />
            </button>
            <button title="Save as…" onClick={saveAs} className="lg-btn">
              <Save size={16} />
            </button>
            <button title="Save to gallery (Ctrl+S)" onClick={() => finish("save")} className="lg-btn">
              <Download size={16} />
            </button>
            <button title="Cancel (Esc)" onClick={() => ipc.cancelCapture()} className="lg-btn">
              <X size={16} />
            </button>
          </div>
        </div>
      )}
      {/* Record toolbar — jedina radnja je pokretanje snimanja */}
      {sel && phase === "edit" && mode === "record" && (
        <div
          ref={barRef}
          className="absolute z-30 flex -translate-x-1/2 items-center gap-2"
          style={{ left: barPos.left, top: barPos.top }}
          onMouseDown={(e) => e.stopPropagation()}
        >
          <div className="lg lg-bar animate-pop">
            <button
              title="Start recording (Enter)"
              onClick={() => beginRecording(false)}
              disabled={starting}
              className="flex items-center gap-2 rounded-full bg-danger px-4 py-1.5 text-sm font-medium text-white transition hover:brightness-110 active:scale-95 disabled:opacity-50"
            >
              <span className="h-2.5 w-2.5 rounded-full bg-white" />
              {starting ? "Starting…" : "Record"}
            </button>
            <span className="lg-sep" />
            <button
              title="Record the whole screen"
              onClick={() => beginRecording(true)}
              disabled={starting}
              className="lg-btn"
            >
              <MonitorIcon size={16} />
            </button>
            <span className="px-1 font-mono text-[11px] text-white/70">
              {Math.round(sel.w)}×{Math.round(sel.h)}
            </span>
            <span className="lg-sep" />
            {/* zvuk: bira se po snimci; GIF nema audio pa su gumbi ugašeni */}
            <button
              title={gifFormat ? "GIF has no audio" : mic ? "Microphone: on" : "Microphone: off"}
              onClick={() => void setAudio("recordMic", !mic)}
              disabled={gifFormat}
              className={`lg-btn ${mic && !gifFormat ? "is-active" : ""}`}
            >
              {mic && !gifFormat ? <Mic size={16} /> : <MicOff size={16} />}
            </button>
            <button
              title={gifFormat ? "GIF has no audio" : sysAudio ? "System audio: on" : "System audio: off"}
              onClick={() => void setAudio("recordSystemAudio", !sysAudio)}
              disabled={gifFormat}
              className={`lg-btn ${sysAudio && !gifFormat ? "is-active" : ""}`}
            >
              {sysAudio && !gifFormat ? <Volume2 size={16} /> : <VolumeX size={16} />}
            </button>
            <span className="lg-sep" />
            <button title="Cancel (Esc)" onClick={() => ipc.cancelCapture()} className="lg-btn">
              <X size={16} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function clampRect(r: Rect): Rect {
  const W = window.innerWidth;
  const H = window.innerHeight;
  const w = Math.max(MIN, Math.min(r.w, W));
  const h = Math.max(MIN, Math.min(r.h, H));
  const x = Math.max(0, Math.min(r.x, W - w));
  const y = Math.max(0, Math.min(r.y, H - h));
  return { x, y, w, h };
}

function applyResize(dir: Dir, s: Rect, dx: number, dy: number): Rect {
  let { x, y, w, h } = s;
  if (dir.includes("w")) { x = s.x + dx; w = s.w - dx; }
  if (dir.includes("e")) { w = s.w + dx; }
  if (dir.includes("n")) { y = s.y + dy; h = s.h - dy; }
  if (dir.includes("s")) { h = s.h + dy; }
  if (w < MIN) { if (dir.includes("w")) x = s.x + s.w - MIN; w = MIN; }
  if (h < MIN) { if (dir.includes("n")) y = s.y + s.h - MIN; h = MIN; }
  return clampRect({ x, y, w, h });
}
