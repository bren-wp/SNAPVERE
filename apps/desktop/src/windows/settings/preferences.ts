import type { ImageFormat, RecordFormat } from "../../lib/types";

export interface AppSettings {
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

export const DEFAULTS: AppSettings = {
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


// Never trust persisted user preferences: partial writes and older builds can
// leave incompatible types, invalid enum variants or non-finite numbers.
export function normalizeSettings(input: unknown): AppSettings {
  if (!input || typeof input !== "object") return { ...DEFAULTS };
  const value = input as Record<string, unknown>;
  const next: AppSettings = { ...DEFAULTS };
  if (value.theme === "dark" || value.theme === "light") next.theme = value.theme;
  if (typeof value.hotkey === "string" && value.hotkey.length <= 100 && value.hotkey.trim()) next.hotkey = value.hotkey;
  if (typeof value.recordHotkey === "string" && value.recordHotkey.length <= 100 && value.recordHotkey.trim()) next.recordHotkey = value.recordHotkey;
  if (value.format === "png" || value.format === "jpg" || value.format === "webp") next.format = value.format;
  if (value.recordFormat === "mp4" || value.recordFormat === "gif") next.recordFormat = value.recordFormat;
  if (typeof value.quality === "number" && Number.isFinite(value.quality)) next.quality = Math.round(Math.max(50, Math.min(100, value.quality)));
  if (typeof value.recordQuality === "number" && Number.isFinite(value.recordQuality)) next.recordQuality = Math.round(Math.max(30, Math.min(100, value.recordQuality)));
  if ([15, 24, 30, 60].includes(value.recordFps as number)) next.recordFps = value.recordFps as number;
  if ([0, 300, 1800, 7200].includes(value.recordMaxSeconds as number)) next.recordMaxSeconds = value.recordMaxSeconds as number;
  for (const key of ["recordCursor", "recordMic", "recordSystemAudio"] as const) {
    if (typeof value[key] === "boolean") next[key] = value[key];
  }
  if (typeof value.saveFolder === "string" && value.saveFolder.length > 0
    && value.saveFolder.length <= 1024 && !value.saveFolder.includes("\u0000")) {
    next.saveFolder = value.saveFolder;
  }
  return next;
}
