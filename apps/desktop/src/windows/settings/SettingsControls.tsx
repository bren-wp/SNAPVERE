import React, { useEffect, useState } from "react";

// ── UI primitivci ──
export function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="max-w-2xl">
      <h1 className="mb-5 text-xl font-semibold">{title}</h1>
      <div className="space-y-4">{children}</div>
    </div>
  );
}
export function Row({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-4 border-b border-border/60 pb-4">
      <div className="min-w-0">
        <div className="text-sm">{label}</div>
        {hint && <div className="mt-1 text-xs leading-snug text-muted">{hint}</div>}
      </div>
      <div className="shrink-0">{children}</div>
    </div>
  );
}
export function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      onClick={() => onChange(!checked)}
      role="switch"
      aria-label={label}
      aria-checked={checked}
      className={`inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors duration-200 ${checked ? "bg-accent" : "bg-border"}`}
    >
      <span
        className={`inline-block h-5 w-5 rounded-full bg-white shadow-[0_1px_3px_rgba(0,0,0,0.4)] transition-transform duration-200 ${
          checked ? "translate-x-[1.375rem]" : "translate-x-0.5"
        }`}
      />
    </button>
  );
}
export function Segmented({ value, options, onChange }: { value: string; options: { v: string; l: string }[]; onChange: (v: string) => void }) {
  return (
    <div className="flex rounded-lg border border-border p-0.5">
      {options.map((o) => (
        <button
          key={o.v}
          onClick={() => onChange(o.v)}
          className={`rounded-md px-3 py-1 text-sm transition-colors ${value === o.v ? "bg-accent text-white" : "text-muted hover:text-text"}`}
        >
          {o.l}
        </button>
      ))}
    </div>
  );
}
/**
 * Polje za snimanje kombinacije.
 *
 * Pored bindanja služi i kao dijagnostika: ako tipkovnica kombinaciju obrađuje
 * interno (klasično za `Fn`), ovdje NE stigne nijedan event — i to korisniku
 * eksplicitno kažemo umjesto da polje šuti.
 */
export function HotkeyInput({ label, value, onChange }: { label: string; value: string; onChange: (accel: string) => void }) {
  const [capturing, setCapturing] = useState(false);
  const [silent, setSilent] = useState(false);

  // Ako 4 s nakon fokusa ništa nije stiglo, kombinacija vjerojatno ne dolazi do Windowsa.
  useEffect(() => {
    if (!capturing) {
      setSilent(false);
      return;
    }
    const t = setTimeout(() => setSilent(true), 4000);
    return () => clearTimeout(t);
  }, [capturing]);

  return (
    <div className="flex flex-col items-end gap-1">
      <input
        readOnly
        aria-label={label}
        value={capturing ? "Press a combination…" : value}
        onFocus={() => setCapturing(true)}
        onBlur={() => setCapturing(false)}
        onKeyDown={(e) => {
          e.preventDefault();
          setSilent(false);
          const accel = buildAccelerator(e);
          if (accel) {
            onChange(accel);
            setCapturing(false);
            e.currentTarget.blur();
          }
        }}
        className="w-52 cursor-pointer rounded-lg border border-border bg-bg px-3 py-1.5 text-center font-mono text-sm outline-none focus:border-accent"
      />
      {silent && (
        <span className="max-w-52 text-right text-[11px] leading-snug text-warn">
          Nothing arrived. Your keyboard is handling that combination itself — Windows never
          sees it, so no app can bind it.
        </span>
      )}
    </div>
  );
}

/**
 * KeyboardEvent → Tauri accelerator.
 *
 * Koristi `e.code` (fizička tipka) gdje god može: `e.key` ovisi o rasporedu i o
 * modifikatorima (Shift+2 daje `"`, AltGr mijenja slovo), a globalni hotkey se
 * registrira po fizičkoj tipki. Pokriva i F13–F24 te medijske tipke jer upravo
 * njih Fn-sloj na mnogim tipkovnicama šalje umjesto ničega.
 */
function buildAccelerator(e: React.KeyboardEvent): string | null {
  if (["Control", "Shift", "Alt", "Meta"].includes(e.key)) return null;

  const mods: string[] = [];
  if (e.ctrlKey) mods.push("Ctrl");
  if (e.shiftKey) mods.push("Shift");
  if (e.altKey) mods.push("Alt");
  if (e.metaKey) mods.push("Super");

  const key = keyFromEvent(e);
  if (!key) return null;
  return [...mods, key].join("+");
}

function keyFromEvent(e: React.KeyboardEvent): string | null {
  const code = e.code;

  if (/^Key[A-Z]$/.test(code)) return code.slice(3);
  if (/^Digit[0-9]$/.test(code)) return code.slice(5);
  if (/^Numpad[0-9]$/.test(code)) return `Num${code.slice(6)}`;
  if (/^F([1-9]|1[0-9]|2[0-4])$/.test(code)) return code;

  const byCode: Record<string, string> = {
    Space: "Space",
    Enter: "Enter",
    NumpadEnter: "Enter",
    Tab: "Tab",
    Backspace: "Backspace",
    Delete: "Delete",
    Insert: "Insert",
    Home: "Home",
    End: "End",
    PageUp: "PageUp",
    PageDown: "PageDown",
    ArrowUp: "Up",
    ArrowDown: "Down",
    ArrowLeft: "Left",
    ArrowRight: "Right",
    PrintScreen: "PrintScreen",
    ScrollLock: "ScrollLock",
    Pause: "Pause",
    Minus: "-",
    Equal: "=",
    BracketLeft: "[",
    BracketRight: "]",
    Backslash: "\\",
    Semicolon: ";",
    Quote: "'",
    Comma: ",",
    Period: ".",
    Slash: "/",
    Backquote: "`",
  };
  if (byCode[code]) return byCode[code];

  // Medijske / preglednik tipke nemaju koristan `code` — tu je `key` jedini izvor.
  const k = e.key;
  if (k.startsWith("Media") || k.startsWith("Audio") || k.startsWith("Browser")) return k;
  if (k.length === 1) return k.toUpperCase();
  if (k.startsWith("Arrow")) return k.slice(5);
  if (k === " ") return "Space";
  if (k === "Unidentified" || k === "Dead") return null;
  return k;
}
