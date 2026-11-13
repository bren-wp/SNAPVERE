import { useCallback, useEffect, useRef, useState } from "react";
import { emit, listen } from "@tauri-apps/api/event";
import {
  ArrowUpRight,
  Circle,
  Eraser,
  EyeOff,
  Highlighter,
  ListOrdered,
  Mic,
  Minus,
  MousePointer2,
  Pause,
  PenTool,
  Play,
  Square,
  Type,
  Undo2,
  Volume2,
  X,
} from "lucide-react";
import { ipc } from "../lib/ipc";
import type { DrawToolMsg, RecordStatus, RecTool } from "../lib/types";

/**
 * Plutajuća traka koja stoji iznad svega dok snimanje traje.
 *
 * Vlastiti prozor je namjeran: HUD mora preživjeti zatvaranje galerije i — bitnije —
 * izuzet je iz capture-a (`WDA_EXCLUDEFROMCAPTURE`), pa toolbar nikad ne završi u
 * snimci. Sami potezi se crtaju u zasebnom prozoru `recdraw` koji JEST snimljen;
 * ovdje se samo bira alat i šalje preko `snapvere:draw-*` eventa.
 */

const TOOLS: { id: RecTool; icon: typeof Square; label: string }[] = [
  { id: "off", icon: MousePointer2, label: "Click through (drawing off)" },
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
  "#09BFD9", "#BF5AF2", "#FF375F", "#FFFFFF", "#1C1C1E",
];

export default function RecBar() {
  const [status, setStatus] = useState<RecordStatus | null>(null);
  const [busy, setBusy] = useState(false);
  const [tool, setTool] = useState<RecTool>("off");
  const [color, setColor] = useState("#FF453A");
  const [size, setSize] = useState(6);
  const [palette, setPalette] = useState(false);
  const closing = useRef(false);

  // Tick iz Rusta (~4x/s) je izvor istine za tajmer — nema drifta u JS-u.
  useEffect(() => {
    const subs = [
      listen<RecordStatus>("snapvere:rec-tick", (e) => setStatus(e.payload)),
      listen<RecordStatus>("snapvere:rec-state", (e) => {
        setStatus(e.payload);
        // Novo snimanje (prozor HUD-a živi između snimanja, komponenta se ne remounta):
        // otključaj Stop/Odbaci — inače bi nakon prvog snimanja zauvijek ostali mrtvi.
        if (e.payload.phase === "recording") closing.current = false;
      }),
      listen("snapvere:rec-done", () => {
        closing.current = true;
        // Sljedeće snimanje kreće bez alata i palete — sloj za crtanje je ionako
        // click-through nakon zatvaranja, pa bi "zalijepljen" alat samo zbunjivao.
        setTool("off");
        setPalette(false);
        void emit("snapvere:draw-tool", { tool: "off" });
        void ipc.closeRecHud();
      }),
      // Sloj za crtanje može sam ugasiti alat (Esc) — traka mora pratiti.
      listen<DrawToolMsg>("snapvere:draw-tool", (e) => setTool(e.payload.tool)),
    ];
    // Prvi status odmah, da traka ne bljesne prazna.
    void ipc.recordingStatus().then(setStatus).catch(() => undefined);
    return () => {
      subs.forEach((s) => void s.then((un) => un()));
    };
  }, []);

  const sendTool = useCallback((t: RecTool, c: string, s: number) => {
    void emit("snapvere:draw-tool", { tool: t, color: c, size: s });
  }, []);

  const pickTool = (t: RecTool) => {
    const next = t === tool && t !== "off" ? "off" : t;
    setTool(next);
    setPalette(false);
    sendTool(next, color, size);
  };

  const paused = status?.phase === "paused";
  const finishing = status?.phase === "finishing";

  const togglePause = useCallback(async () => {
    if (busy || finishing) return;
    setBusy(true);
    try {
      setStatus(await ipc.pauseRecording(!paused));
    } catch (e) {
      console.error(e);
    } finally {
      setBusy(false);
    }
  }, [busy, finishing, paused]);

  const stop = useCallback(async () => {
    if (closing.current) return;
    closing.current = true;
    try {
      await ipc.stopRecording();
    } catch (e) {
      console.error(e);
      void ipc.closeRecHud();
    }
  }, []);

  const discard = useCallback(async () => {
    if (closing.current) return;
    closing.current = true;
    try {
      await ipc.cancelRecording();
    } catch (e) {
      console.error(e);
      void ipc.closeRecHud();
    }
  }, []);

  // Namjerno bez tipkovničkih prečaca: HUD je WS_EX_NOACTIVATE (nikad nema fokus), pa
  // tipke koje korisnik pritisne u svojoj aplikaciji ne mogu slučajno zaustaviti snimanje.

  return (
    <div
      data-tauri-drag-region
      className="relative flex h-full w-full cursor-grab select-none items-center gap-1.5 rounded-2xl bg-black/85 px-3 text-white shadow-[0_10px_40px_rgba(0,0,0,0.55)] ring-1 ring-white/15 backdrop-blur-xl active:cursor-grabbing"
    >
      {/* stanje + tajmer */}
      <span
        className={`h-2.5 w-2.5 shrink-0 rounded-full ${
          finishing ? "bg-white/50" : paused ? "bg-warn" : "animate-rec-pulse bg-danger"
        }`}
      />
      <span className="mr-1 min-w-[50px] font-mono text-[15px] font-semibold tabular-nums">
        {fmtTime(status?.elapsedMs ?? 0)}
      </span>

      <span className="h-6 w-px shrink-0 bg-white/15" />

      {/* alati za crtanje — potezi idu u prozor recdraw, koji JEST snimljen */}
      {TOOLS.map((t) => (
        <button
          key={t.id}
          title={t.label}
          onClick={() => pickTool(t.id)}
          className={`grid h-8 w-8 shrink-0 place-items-center rounded-lg transition ${
            tool === t.id ? "bg-accent text-white" : "hover:bg-white/15"
          }`}
        >
          <t.icon size={15} strokeWidth={2} />
        </button>
      ))}

      <button
        title="Colour"
        onClick={() => setPalette((v) => !v)}
        className="grid h-8 w-8 shrink-0 place-items-center rounded-lg transition hover:bg-white/15"
      >
        <span
          className="h-4 w-4 rounded-full border border-white/40"
          style={{ background: color }}
        />
      </button>

      <button
        title="Undo"
        onClick={() => void emit("snapvere:draw-undo")}
        className="grid h-8 w-8 shrink-0 place-items-center rounded-lg transition hover:bg-white/15"
      >
        <Undo2 size={15} />
      </button>
      <button
        title="Clear all drawings"
        onClick={() => void emit("snapvere:draw-clear")}
        className="grid h-8 w-8 shrink-0 place-items-center rounded-lg transition hover:bg-white/15"
      >
        <Eraser size={15} />
      </button>

      <span className="h-6 w-px shrink-0 bg-white/15" />

      {/* kontrole snimanja */}
      <button
        onClick={togglePause}
        disabled={busy || finishing}
        title={paused ? "Resume" : "Pause"}
        className="grid h-8 w-8 shrink-0 place-items-center rounded-full transition hover:bg-white/15 active:scale-95 disabled:opacity-40"
      >
        {paused ? <Play size={15} /> : <Pause size={15} />}
      </button>
      <button
        onClick={stop}
        disabled={finishing}
        title="Stop and save"
        className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-danger transition hover:brightness-110 active:scale-95 disabled:opacity-40"
      >
        <Square size={13} fill="currentColor" />
      </button>
      <button
        onClick={discard}
        disabled={finishing}
        title="Discard"
        className="grid h-8 w-8 shrink-0 place-items-center rounded-full transition hover:bg-white/15 active:scale-95 disabled:opacity-40"
      >
        <X size={15} />
      </button>

      <span className="ml-auto min-w-0 shrink overflow-hidden whitespace-nowrap pl-1 text-right font-mono text-[10px] leading-tight text-white/45">
        {finishing ? (
          "Saving…"
        ) : (
          <>
            {status?.width ?? 0}×{status?.height ?? 0}
            <br />
            <span className="inline-flex items-center gap-1">
              {(status?.format ?? "mp4").toUpperCase()}
              {status?.mic && <Mic size={10} className="text-accent" />}
              {status?.systemAudio && <Volume2 size={10} className="text-accent" />}
              {status?.sizeBytes ? ` · ${fmtBytes(status.sizeBytes)}` : ""}
            </span>
          </>
        )}
      </span>

      {/* paleta — iznad trake, izvan snimanog područja jer je cijeli HUD izuzet */}
      {palette && (
        <div className="absolute bottom-[calc(100%+8px)] right-3 rounded-2xl bg-black/90 p-3 shadow-[0_10px_40px_rgba(0,0,0,0.6)] ring-1 ring-white/15 animate-pop">
          <div className="grid grid-cols-5 gap-2">
            {SWATCHES.map((c) => (
              <button
                key={c}
                onClick={() => {
                  setColor(c);
                  setPalette(false);
                  sendTool(tool, c, size);
                }}
                className="h-6 w-6 rounded-full border-2 transition-transform hover:scale-110"
                style={{ background: c, borderColor: color === c ? "#fff" : "transparent" }}
              />
            ))}
          </div>
          <div className="mt-3 flex items-center gap-2">
            <span className="text-[11px] text-white/70">Size</span>
            <input
              type="range"
              min={2}
              max={28}
              value={size}
              onChange={(e) => {
                const s = +e.target.value;
                setSize(s);
                sendTool(tool, color, s);
              }}
              className="w-28"
            />
            <span className="w-5 text-right font-mono text-[11px] text-white/70">{size}</span>
          </div>
        </div>
      )}
    </div>
  );
}

function fmtTime(ms: number) {
  const total = Math.floor(ms / 1000);
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  const mm = String(m).padStart(2, "0");
  const ss = String(s).padStart(2, "0");
  return h > 0 ? `${h}:${mm}:${ss}` : `${mm}:${ss}`;
}

function fmtBytes(n: number) {
  if (n < 1024 * 1024) return `${(n / 1024).toFixed(0)} KB`;
  return `${(n / 1024 / 1024).toFixed(1)} MB`;
}
