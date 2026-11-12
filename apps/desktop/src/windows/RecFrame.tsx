import { useEffect, useState } from "react";
import { listen } from "@tauri-apps/api/event";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { ipc } from "../lib/ipc";
import type { RecFrameRect, RecordPhase, RecordStatus } from "../lib/types";

/**
 * Okvir snimanja — prozor preko CIJELOG monitora koji zatamni sve oko snimanog
 * područja i nacrta rub oko njega, pa se u svakom trenutku vidi što točno ide u video.
 *
 * Prozor je izuzet iz capture-a (`WDA_EXCLUDEFROMCAPTURE`) pa dim i rub nikad ne
 * završe u snimci, i click-through pa ne smeta radu. Sve što je ovdje je čista
 * vizualna povratna informacija; stanje (faza) stiže istim eventima kao HUD-u.
 */
export default function RecFrame() {
  const [rect, setRect] = useState<RecFrameRect | null>(null);
  const [phase, setPhase] = useState<RecordPhase>("recording");
  const [scale, setScale] = useState(1);

  useEffect(() => {
    // Prozor se seli po monitorima; DPI se čita pri svakom novom okviru.
    const refreshScale = () =>
      void getCurrentWindow().scaleFactor().then(setScale).catch(() => undefined);
    refreshScale();

    const subs = [
      listen<RecFrameRect>("snapvere:rec-frame", (e) => {
        setRect(e.payload);
        setPhase("recording");
        refreshScale();
      }),
      listen<RecordStatus>("snapvere:rec-state", (e) => setPhase(e.payload.phase)),
      listen<RecordStatus>("snapvere:rec-tick", (e) => setPhase(e.payload.phase)),
    ];
    // Ako je event prošao prije nego se sloj učitao (lijeno stvoren prozor), povuci stanje.
    void ipc
      .recFrameRect()
      .then((r) => r && setRect(r))
      .catch(() => undefined);
    return () => {
      subs.forEach((s) => void s.then((un) => un()));
    };
  }, []);

  if (!rect) return null;

  // Fizički px → CSS px (prozor je cijeli monitor, pa je 0,0 = kut monitora).
  const x = rect.x / scale;
  const y = rect.y / scale;
  const w = rect.width / scale;
  const h = rect.height / scale;

  const paused = phase === "paused";
  const finishing = phase === "finishing" || phase === "idle";
  const edge = finishing
    ? "rgba(255,255,255,0.45)"
    : paused
      ? "#FFD60A"
      : "#FF453A";
  const dim = "rgba(0,0,0,0.45)";

  return (
    <div className="pointer-events-none fixed inset-0 select-none overflow-hidden">
      {/* Dim — 4 pravokutnika oko područja; kod cijelog ekrana nema ničega za zatamniti. */}
      {!rect.full && (
        <>
          <div className="absolute left-0 right-0 top-0" style={{ height: Math.max(0, y), background: dim }} />
          <div className="absolute bottom-0 left-0 right-0" style={{ top: y + h, background: dim }} />
          <div className="absolute" style={{ left: 0, top: y, width: Math.max(0, x), height: h, background: dim }} />
          <div className="absolute" style={{ left: x + w, right: 0, top: y, height: h, background: dim }} />
        </>
      )}

      {/* Rub — izvan područja kod regije (ne pokriva ni piksel sadržaja), unutra kod cijelog ekrana. */}
      <div
        className="absolute"
        style={{
          left: x,
          top: y,
          width: w,
          height: h,
          outline: `2px solid ${edge}`,
          outlineOffset: rect.full ? -2 : 0,
          boxShadow: rect.full ? undefined : `0 0 0 1px rgba(0,0,0,0.35), 0 0 18px ${edge}55`,
          borderRadius: rect.full ? 0 : 2,
          transition: "outline-color 160ms ease, box-shadow 160ms ease",
        }}
      />

      {/* Pauza — područje se lagano zatamni i dobije oznaku; nijedan frame se ne hvata. */}
      {paused && (
        <div
          className="absolute grid place-items-center"
          style={{ left: x, top: y, width: w, height: h, background: "rgba(0,0,0,0.28)" }}
        >
          <div className="flex items-center gap-2 rounded-full bg-black/80 px-4 py-2 text-[13px] font-semibold text-white ring-1 ring-white/15 shadow-[0_10px_30px_rgba(0,0,0,0.5)]">
            <span className="h-2.5 w-2.5 rounded-full" style={{ background: "#FFD60A" }} />
            Paused
          </div>
        </div>
      )}
    </div>
  );
}
