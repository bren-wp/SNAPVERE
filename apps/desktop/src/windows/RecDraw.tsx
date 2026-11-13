import { useCallback, useEffect, useRef, useState } from "react";
import { emit, listen } from "@tauri-apps/api/event";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { ipc } from "../lib/ipc";
import { renderAnnotations } from "../lib/canvas";
import type { Annotation, DrawToolMsg, Point, RecTool } from "../lib/types";

/**
 * Prozirni sloj za crtanje preko snimanog područja.
 *
 * Namjerno NIJE izuzet iz capture-a (za razliku od HUD-a): DXGI hvata kompozitirani
 * desktop, pa sve nacrtano ovdje završi u videu bez ijedne linije kompozitiranja u
 * Rustu. Alati i renderer su isti kao kod screenshota (`lib/canvas.ts`).
 *
 * Blur je iznimka. Webview ne može čitati piksele desktopa ispod sebe, pa se ovdje
 * crta samo isprekidani okvir, a stvarnu pikselizaciju radi Rust nad svakim frameom
 * (`set_blur_regions`) — što usput znači da redact prati živi sadržaj, ne zamrznuti.
 */

let counter = 0;
const uid = () => `r${++counter}`;
const textFont = (size: number) => Math.round(size * 3.5 + 8);

/** Alati koji se crtaju povlačenjem od točke do točke. */
const DRAG_TOOLS: RecTool[] = ["arrow", "rect", "ellipse", "line", "blur"];

export default function RecDraw() {
  const [tool, setTool] = useState<RecTool>("off");
  const [color, setColor] = useState("#09BFD9");
  const [size, setSize] = useState(6);
  const [annotations, setAnnotations] = useState<Annotation[]>([]);
  const [textEdit, setTextEdit] = useState<Point | null>(null);
  const [textValue, setTextValue] = useState("");
  const [scale, setScale] = useState(1);

  const canvasRef = useRef<HTMLCanvasElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const drawing = useRef(false);
  const draft = useRef<Annotation | null>(null);
  /** Pending rAF — više mousemove eventa po frameu smije proizvesti samo jedan redraw. */
  const rafRef = useRef(0);
  /** Prazan izvor: blur anotacije nikad ne idu u `renderAnnotations`. */
  const blurStub = useRef<HTMLCanvasElement>(document.createElement("canvas"));

  useEffect(() => {
    void getCurrentWindow().scaleFactor().then(setScale).catch(() => undefined);
  }, []);

  // ── kontrole stižu iz HUD-a (on je izuzet iz snimke, pa toolbar nije u videu) ──
  useEffect(() => {
    const subs = [
      listen<DrawToolMsg>("snapvere:draw-tool", (e) => {
        const { tool: t, color: c, size: s } = e.payload;
        setTool(t);
        if (c) setColor(c);
        if (s) setSize(s);
        setTextEdit(null);
        setTextValue("");
        draft.current = null;
        drawing.current = false;
        // Kad alat nije odabran sloj mora propuštati klikove — inače korisnik
        // ne može raditi ništa drugo dok snimanje traje.
        void ipc.setDrawInteractive(t !== "off");
      }),
      listen("snapvere:draw-undo", () => setAnnotations((p) => p.slice(0, -1))),
      listen("snapvere:draw-clear", () => setAnnotations([])),
    ];
    return () => {
      subs.forEach((s) => void s.then((un) => un()));
    };
  }, []);

  // ── redact zone idu u Rust; on ih pikselizira u svakom frameu ──
  useEffect(() => {
    const rects = annotations
      .filter((a) => a.tool === "blur" && a.points.length > 1)
      .map((a) => {
        const [p0, p1] = a.points;
        return {
          x: Math.round(Math.min(p0.x, p1.x) * scale),
          y: Math.round(Math.min(p0.y, p1.y) * scale),
          width: Math.round(Math.abs(p1.x - p0.x) * scale),
          height: Math.round(Math.abs(p1.y - p0.y) * scale),
        };
      })
      .filter((r) => r.width > 1 && r.height > 1);
    void ipc.setBlurRegions(rects).catch(() => undefined);
  }, [annotations, scale]);

  // ── crtanje ──
  const redraw = useCallback(() => {
    const c = canvasRef.current;
    if (!c) return;
    const cssW = window.innerWidth;
    const cssH = window.innerHeight;
    const W = Math.max(1, Math.round(cssW * scale));
    const H = Math.max(1, Math.round(cssH * scale));
    if (c.width !== W) c.width = W;
    if (c.height !== H) c.height = H;

    // desynchronized: canvas smije ići na ekran mimo glavnog compositor koraka → manji lag olovke.
    const ctx = c.getContext("2d", { desynchronized: true });
    if (!ctx) return;
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, W, H);
    ctx.setTransform(scale, 0, 0, scale, 0, 0);

    const all = draft.current ? [...annotations, draft.current] : annotations;
    const strokes = all.filter((a) => a.tool !== "blur");
    renderAnnotations(ctx, strokes, null, {
      frozen: blurStub.current,
      originX: 0,
      originY: 0,
      scale,
    });

    // Redact: samo tanki okvir kao živa povratna informacija. Piksele briše Rust,
    // pa prezenter i dalje vidi sadržaj, a gledatelj snimke ne.
    for (const a of all) {
      if (a.tool !== "blur" || a.points.length < 2) continue;
      const [p0, p1] = a.points;
      ctx.save();
      ctx.setLineDash([6, 4]);
      ctx.lineWidth = 1.5;
      ctx.strokeStyle = "rgba(255,255,255,0.85)";
      ctx.strokeRect(
        Math.min(p0.x, p1.x),
        Math.min(p0.y, p1.y),
        Math.abs(p1.x - p0.x),
        Math.abs(p1.y - p0.y),
      );
      ctx.restore();
    }
  }, [annotations, scale]);

  useEffect(redraw, [redraw]);

  /** Redraw spojen na animacijski frame — miš zna slati više eventa po frameu. */
  const scheduleRedraw = useCallback(() => {
    if (rafRef.current) return;
    rafRef.current = requestAnimationFrame(() => {
      rafRef.current = 0;
      redraw();
    });
  }, [redraw]);

  const commit = (a: Annotation) => setAnnotations((p) => [...p, a]);

  const onDown = (e: React.MouseEvent) => {
    if (tool === "off") return;
    if (textEdit) {
      commitText();
      return;
    }
    const p = { x: e.clientX, y: e.clientY };
    if (tool === "text") {
      // Default akcija mousedowna na nefokusabilnom divu blura aktivni element —
      // a to je upravo input koji React mounta u ovom handleru. Bez ovoga tipke idu u <body>.
      e.preventDefault();
      setTextEdit(p);
      setTextValue("");
      return;
    }
    if (tool === "step") {
      const n = annotations.filter((a) => a.tool === "step").length + 1;
      commit({ id: uid(), tool: "step", color, size, points: [p], text: String(n) });
      return;
    }
    drawing.current = true;
    draft.current = {
      id: uid(),
      tool,
      color,
      size,
      points: DRAG_TOOLS.includes(tool) ? [p, p] : [p],
    } as Annotation;
    redraw();
  };

  const onMove = (e: React.MouseEvent) => {
    if (!drawing.current || !draft.current) return;
    const p = { x: e.clientX, y: e.clientY };
    if (tool === "pen" || tool === "marker") draft.current.points.push(p);
    else draft.current.points[1] = p;
    scheduleRedraw();
  };

  const onUp = () => {
    if (!drawing.current || !draft.current) return;
    drawing.current = false;
    const a = draft.current;
    draft.current = null;
    // Ispusti slučajni klik koji nije stvarni potez.
    const [p0, p1] = a.points;
    const tiny = DRAG_TOOLS.includes(a.tool) && p1 && Math.hypot(p1.x - p0.x, p1.y - p0.y) < 4;
    if (tiny) redraw();
    else commit(a);
  };

  const commitText = () => {
    const v = textValue.trim();
    if (v && textEdit) {
      commit({
        id: uid(),
        tool: "text",
        color,
        size: textFont(size),
        points: [textEdit],
        text: v,
      });
    }
    setTextEdit(null);
    setTextValue("");
  };

  // Robustan fokus (kao u Overlayu): prozor se aktivira tek na klik, pa WebView2 zna
  // preuzeti fokus nakon što je React već fokusirao input.
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

  // Esc = brzo van iz crtanja (sloj opet propušta klikove); HUD se sinkronizira preko eventa.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key !== "Escape" || textEdit) return;
      void emit("snapvere:draw-tool", { tool: "off" });
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [textEdit]);

  const cursor =
    tool === "off" ? "default" : tool === "text" ? "text" : "crosshair";

  return (
    <div
      className="fixed inset-0 select-none"
      style={{ background: "transparent", cursor }}
      onMouseDown={onDown}
      onMouseMove={onMove}
      onMouseUp={onUp}
      onMouseLeave={onUp}
    >
      <canvas
        ref={canvasRef}
        className="pointer-events-none absolute inset-0 h-full w-full"
      />

      {textEdit && (
        <input
          ref={inputRef}
          autoFocus
          value={textValue}
          onChange={(e) => setTextValue(e.target.value)}
          // Klik u sam input (pomak kursora) ne smije doći do sloja — tamo bi potvrdio tekst.
          onMouseDown={(e) => e.stopPropagation()}
          onKeyDown={(e) => {
            if (e.key === "Enter") commitText();
            else if (e.key === "Escape") {
              setTextEdit(null);
              setTextValue("");
            }
            e.stopPropagation();
          }}
          placeholder="Type…"
          className="absolute z-40 border-0 bg-black/25 p-0 outline-none placeholder:text-white/45"
          style={{
            left: textEdit.x,
            top: textEdit.y,
            color,
            caretColor: color,
            fontSize: textFont(size),
            fontWeight: 600,
            fontFamily: '-apple-system, "SF Pro Display", Inter, sans-serif',
            minWidth: 90,
            outline: "1px dashed rgba(255,255,255,0.65)",
            borderRadius: 4,
          }}
        />
      )}
    </div>
  );
}
