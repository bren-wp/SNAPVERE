import React, { useEffect, useState } from "react";
import { SlidersHorizontal, Keyboard, Crop, Video, FolderOpen, ShieldCheck, Info } from "lucide-react";
import { load, type Store } from "@tauri-apps/plugin-store";
import { enable, disable, isEnabled } from "@tauri-apps/plugin-autostart";
import { open as openDialog } from "@tauri-apps/plugin-dialog";
import { openPath, openUrl } from "@tauri-apps/plugin-opener";
import { ipc } from "../lib/ipc";
import type { ImageFormat, RecordFormat } from "../lib/types";
import WindowFrame from "../components/WindowFrame";
import { version as appVersion } from "../../package.json";

type Tab =
  | "general"
  | "hotkeys"
  | "capture"
  | "recording"
  | "save"
  | "privacy"
  | "about";

interface AppSettings {
  theme: "dark" | "light";
  hotkey: string;
  recordHotkey: string;
  format: ImageFormat;
  quality: number;
  recordFormat: RecordFormat;
  recordFps: number;
  recordQuality: number;
  recordCursor: boolean;
  recordMic: boolean;
  recordSystemAudio: boolean;
  recordMaxSeconds: number;
  saveFolder: string;
}

const DEFAULTS: AppSettings = {
  theme: "dark",
  hotkey: "PrintScreen",
  recordHotkey: "Ctrl+Shift+R",
  format: "png",
  quality: 92,
  recordFormat: "mp4",
  recordFps: 24,
  recordQuality: 80,
  recordCursor: true,
  recordMic: false,
  recordSystemAudio: false,
  recordMaxSeconds: 1800,
  saveFolder: "Pictures/Snapvere",
};

const TABS: { id: Tab; label: string; icon: typeof SlidersHorizontal }[] = [
  { id: "general", label: "General", icon: SlidersHorizontal },
  { id: "hotkeys", label: "Hotkeys", icon: Keyboard },
  { id: "capture", label: "Capture", icon: Crop },
  { id: "recording", label: "Recording", icon: Video },
  { id: "save", label: "Save", icon: FolderOpen },
  { id: "privacy", label: "Privacy", icon: ShieldCheck },
  { id: "about", label: "About", icon: Info },
];

export default function Settings() {
  const [tab, setTab] = useState<Tab>("general");
  const [s, setS] = useState<AppSettings>(DEFAULTS);
  const [store, setStore] = useState<Store | null>(null);
  const [autostart, setAutostart] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      const st = await load("settings.json", { autoSave: true, defaults: {} });
      setStore(st);
      const loaded = { ...DEFAULTS };
      for (const k of Object.keys(DEFAULTS) as (keyof AppSettings)[]) {
        const v = await st.get(k);
        if (v !== undefined && v !== null) (loaded as Record<string, unknown>)[k] = v;
      }
      // Ako je zeljeni hotkey zauzet, jezgra registrira fallback — prikazi STVARNO stanje.
      try {
        const [capture, record] = await ipc.activeHotkeys();
        if (capture) loaded.hotkey = prettyAccel(capture);
        if (record) loaded.recordHotkey = prettyAccel(record);
      } catch {
        /* noop */
      }
      setS(loaded);
      try {
        setAutostart(await isEnabled());
      } catch {
        /* noop */
      }
    })().catch((error) => setNotice(`Could not load settings: ${String(error)}`));
  }, []);

  function set<K extends keyof AppSettings>(key: K, val: AppSettings[K]) {
    setS((prev) => ({ ...prev, [key]: val }));
    void store?.set(key, val).catch((error) => setNotice(`Could not save ${key}: ${String(error)}`));
  }

  // Save a hotkey only after the operating system has accepted it. On failure,
  // the native service attempts to restore the previous binding.
  const applyHotkey = async (key: "hotkey" | "recordHotkey", accelerator: string) => {
    setStatus(null);
    try {
      if (key === "hotkey") await ipc.updateHotkey(accelerator);
      else await ipc.updateRecordHotkey(accelerator);
      const [capture, recording] = await ipc.activeHotkeys();
      const active = key === "hotkey" ? capture : recording;
      set(key, prettyAccel(active ?? accelerator));
      setNotice(null);
      setStatus(`${key === "hotkey" ? "Screenshot" : "Recording"} shortcut updated.`);
    } catch (error) {
      const current = await ipc.activeHotkeys().catch(() => null);
      if (current) {
        const previous = key === "hotkey" ? current[0] : current[1];
        if (previous) setS((state) => ({ ...state, [key]: prettyAccel(previous) }));
      }
      setNotice(`Shortcut was not changed: ${String(error)}`);
    }
  };

  const applyTheme = (theme: "dark" | "light") => {
    set("theme", theme);
    document.documentElement.setAttribute("data-theme", theme);
    localStorage.setItem("snapvere-theme", theme);
  };

  const toggleAutostart = async (on: boolean) => {
    try {
      on ? await enable() : await disable();
      setAutostart(on);
      // Remember the explicit user preference. Portable mode never auto-enables startup.
      await store?.set("autostart", on);
    } catch (e) {
      setNotice(`Could not change startup preference: ${String(e)}`);
    }
  };

  const pickFolder = async () => {
    const dir = await openDialog({ directory: true, multiple: false });
    if (typeof dir === "string") set("saveFolder", dir);
  };

  return (
    <WindowFrame title="Settings">
      <div className="snapvere-settings-layout flex min-h-0 flex-1">
        {/* tabovi */}
        <nav aria-label="Settings sections" className="snapvere-settings-sidebar flex w-48 shrink-0 flex-col gap-1 overflow-y-auto border-r border-white/10 bg-elevated/20 p-3">
        <div className="snapvere-section-eyebrow px-2 pb-2 pt-2">PREFERENCES</div>
        {TABS.map((t) => (
          <button
            key={t.id}
            onClick={() => { setTab(t.id); setNotice(null); setStatus(null); }}
            aria-current={tab === t.id ? "page" : undefined}
            aria-label={`${t.label} settings`}
            className={`flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-left text-sm transition-colors focus-visible:ring-2 focus-visible:ring-accent ${
              tab === t.id ? "bg-accent text-white" : "text-muted hover:bg-elevated hover:text-text"
            }`}
          >
            <t.icon size={17} strokeWidth={1.8} aria-hidden="true" className="shrink-0" />
            <span>{t.label}</span>
          </button>
        ))}
      </nav>

      <div className="snapvere-settings-content min-h-0 flex-1 overflow-auto p-6">
        {notice && <p role="alert" className="mb-4 rounded-xl border border-danger/40 bg-danger/10 p-3 text-sm">{notice}</p>}
        {status && <p role="status" aria-live="polite" className="mb-4 rounded-xl border border-accent/30 bg-accent/10 p-3 text-sm">{status}</p>}
        {tab === "general" && (
          <Section title="General">
            <Row label="Theme" hint="Light or dark interface scheme.">
              <Segmented
                value={s.theme}
                options={[
                  { v: "dark", l: "Dark" },
                  { v: "light", l: "Light" },
                ]}
                onChange={(v) => applyTheme(v as "dark" | "light")}
              />
            </Row>
            <Row
              label="Launch on startup"
              hint="Optional. Start Snapvere when you sign in (off by default)."
            >
              <Toggle label="Launch on startup" checked={autostart} onChange={toggleAutostart} />
            </Row>
          </Section>
        )}

        {tab === "hotkeys" && (
          <Section title="Hotkeys">
            <Row label="Region capture" hint="Freeze the screen and pick an area.">
              <HotkeyInput
                label="Screenshot shortcut"
                value={s.hotkey}
                onChange={(accel) => void applyHotkey("hotkey", accel)}
              />
            </Row>
            <Row
              label="Screen recording"
              hint="Press once to pick an area, again to stop the recording."
            >
              <HotkeyInput
                label="Recording shortcut"
                value={s.recordHotkey}
                onChange={(accel) => void applyHotkey("recordHotkey", accel)}
              />
            </Row>
            <p className="mt-3 text-xs leading-relaxed text-muted">
              Click a field, then press the combination. Changes apply immediately.
              <br />
              <span className="text-muted/80">
                Note: <kbd>Fn</kbd> is handled inside the keyboard itself, so Windows never sees
                it. It can only be part of a shortcut when the key it produces is a real key —
                for example Fn+P sending <kbd>PrintScreen</kbd>. Pick a normal combination for
                recording.
              </span>
            </p>
          </Section>
        )}

        {tab === "capture" && (
          <Section title="Capture">
            <Row label="Default format" hint="Format new screenshots are saved in.">
              <Segmented
                value={s.format}
                options={[
                  { v: "png", l: "PNG" },
                  { v: "jpg", l: "JPG" },
                  { v: "webp", l: "WebP" },
                ]}
                onChange={(v) => set("format", v as ImageFormat)}
              />
            </Row>
            <Row label={`Quality (${s.quality})`}>
              <input
                type="range"
                min={50}
                max={100}
                value={s.quality}
                disabled={s.format === "png"}
                onChange={(e) => set("quality", +e.target.value)}
                className="w-48"
              />
            </Row>
            {s.format === "png" && <p className="text-xs text-muted">PNG is lossless — quality doesn't apply.</p>}
          </Section>
        )}

        {tab === "recording" && (
          <Section title="Recording">
            <Row label="Format" hint="MP4 (H.264) plays everywhere; GIF drops straight into chat.">
              <Segmented
                value={s.recordFormat}
                options={[
                  { v: "mp4", l: "MP4" },
                  { v: "gif", l: "GIF" },
                ]}
                onChange={(v) => set("recordFormat", v as RecordFormat)}
              />
            </Row>
            <Row label="Frame rate" hint="Higher is smoother; 24 is plenty for UI walkthroughs.">
              <Segmented
                value={String(s.recordFps)}
                options={[
                  { v: "15", l: "15" },
                  { v: "24", l: "24" },
                  { v: "30", l: "30" },
                  { v: "60", l: "60" },
                ]}
                onChange={(v) => set("recordFps", Number(v))}
              />
            </Row>
            <Row label={`Quality (${s.recordQuality})`} hint="Drives the video bitrate.">
              <input
                type="range"
                min={30}
                max={100}
                value={s.recordQuality}
                disabled={s.recordFormat === "gif"}
                onChange={(e) => set("recordQuality", +e.target.value)}
                className="w-48"
              />
            </Row>
            <Row label="Include the mouse cursor">
              <Toggle
                label="Include the mouse cursor"
                checked={s.recordCursor}
                onChange={(v) => set("recordCursor", v)}
              />
            </Row>
            <Row
              label="Record microphone"
              hint="Default input device. You can also flip this per recording, right in the capture bar."
            >
              <Toggle
                label="Record microphone"
                checked={s.recordMic}
                onChange={(v) => set("recordMic", v)}
              />
            </Row>
            <Row
              label="Record system audio"
              hint="Whatever your speakers play — app sounds, calls, music. MP4 only."
            >
              <Toggle
                label="Record system audio"
                checked={s.recordSystemAudio}
                onChange={(v) => set("recordSystemAudio", v)}
              />
            </Row>
            <Row
              label="Stop after"
              hint="A safety limit so a forgotten recording cannot fill the disk."
            >
              <Segmented
                value={String(s.recordMaxSeconds)}
                options={[
                  { v: "300", l: "5 min" },
                  { v: "1800", l: "30 min" },
                  { v: "7200", l: "2 h" },
                  { v: "0", l: "Never" },
                ]}
                onChange={(v) => set("recordMaxSeconds", Number(v))}
              />
            </Row>
            {s.recordFormat === "gif" && (
              <p className="text-xs text-muted">
                GIF is capped at 960px wide and 256 colours per frame — great for short clips,
                large for long ones.
              </p>
            )}
          </Section>
        )}

        {tab === "save" && (
          <Section title="Save">
            <Row label="Folder">
              <div className="flex items-center gap-2">
                <code className="max-w-xs truncate rounded bg-elevated px-2 py-1 text-xs">{s.saveFolder}</code>
                <button onClick={pickFolder} className="rounded-lg border border-border px-3 py-1.5 text-sm hover:bg-elevated">
                  Change
                </button>
              </div>
            </Row>
          </Section>
        )}

        {tab === "privacy" && (
          <Section title="Privacy & local storage">
            <div className="rounded-2xl border border-accent/25 bg-accent/5 p-5">
              <h2 className="text-base font-semibold">Your captures stay on your computer</h2>
              <p className="mt-2 text-sm leading-relaxed text-muted">
                Screenshots, recordings, preferences and the local gallery database stay on your device.
                Snapvere does not need an account or an upload to capture the screen.
              </p>
              <div className="mt-4 flex flex-wrap gap-3">
                <button type="button" onClick={async () => {
                  try { await ipc.openCaptureFolder(); }
                  catch (error) { setNotice(`Cannot open capture folder: ${String(error)}`); }
                }} className="rounded-xl border border-border bg-elevated px-4 py-2 text-sm font-medium hover:border-accent focus-visible:ring-2 focus-visible:ring-accent">
                  Open capture folder
                </button>
                <button type="button" onClick={async () => {
                  try { await ipc.openDataFolder(); }
                  catch (error) { setNotice(`Cannot open data folder: ${String(error)}`); }
                }} className="rounded-xl border border-border bg-elevated px-4 py-2 text-sm font-medium hover:border-accent focus-visible:ring-2 focus-visible:ring-accent">
                  Open local data
                </button>
              </div>
            </div>
            <p className="mt-4 text-xs leading-relaxed text-muted">
              Use the gallery to review or delete individual captures. Closing Snapvere does not delete files.
            </p>
          </Section>
        )}

        {tab === "about" && (
          <Section title="About">
            <p className="text-sm text-muted">Snapvere · version {appVersion}</p>
            <p className="mt-2 text-sm text-muted">
              Secure capture and screen recording by Brendigo.
            </p>
            <div className="mt-3 flex flex-wrap gap-3 text-sm">
              <button
                type="button"
                onClick={() => void openUrl("https://brendigo.com").catch(console.error)}
                className="font-medium text-accent underline-offset-4 hover:underline focus-visible:underline"
                aria-label="Open author Brendigo website"
              >
                Author: Brendigo
              </button>
              <button
                type="button"
                onClick={() => void openUrl("https://snapvere.com").catch(console.error)}
                className="font-medium text-accent underline-offset-4 hover:underline focus-visible:underline"
                aria-label="Open official Snapvere website"
              >
                Official website: snapvere.com
              </button>
            </div>
            <div className="mt-5 rounded-xl border border-border/60 bg-elevated/20 p-4">
              <div className="text-sm font-medium">Commercial license agreement</div>
              <p className="mt-2 text-xs leading-relaxed text-muted">
                Professional and commercial use are permitted. Existing open-source
                licenses remain in effect. No account or activation key is required.
              </p>
              <button
                type="button"
                onClick={() => void openUrl("https://snapvere.com/license/").catch((error) => setNotice(`Cannot open license: ${String(error)}`))}
                className="mt-3 rounded-lg border border-border px-3 py-1.5 text-sm font-medium hover:bg-elevated focus-visible:ring-2 focus-visible:ring-accent"
              >
                Read license terms
              </button>
            </div>
            <div className="mt-6 rounded-xl border border-border/60 p-4">
              <div className="mb-1 text-sm font-medium">Diagnostics</div>
              <p className="mb-3 text-xs text-muted">
                If something crashes or a recording fails, the log has the reason.
              </p>
              <button
                onClick={async () => {
                  const path = await ipc.logFilePath();
                  if (path) await openPath(path);
                }}
                className="rounded-lg border border-border px-3 py-1.5 text-sm hover:bg-elevated"
              >
                Open log file
              </button>
            </div>
          </Section>
        )}
      </div>
      </div>
    </WindowFrame>
  );
}

/** "CmdOrCtrl+Shift+R" je Taurijev zapis; korisniku pokazujemo tipku koju stvarno pritišće. */
function prettyAccel(accel: string): string {
  const isMac = navigator.userAgent.includes("Mac");
  return accel.replace(/CmdOrCtrl/gi, isMac ? "Cmd" : "Ctrl").replace(/Super/g, isMac ? "Cmd" : "Win");
}

// ── UI primitivci ──
function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="max-w-2xl">
      <h1 className="mb-5 text-xl font-semibold">{title}</h1>
      <div className="space-y-4">{children}</div>
    </div>
  );
}
function Row({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
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
function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
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
function Segmented({ value, options, onChange }: { value: string; options: { v: string; l: string }[]; onChange: (v: string) => void }) {
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
function HotkeyInput({ label, value, onChange }: { label: string; value: string; onChange: (accel: string) => void }) {
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
