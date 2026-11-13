// Typed wrapperi oko Tauri `invoke`. Jedina dodirna točka frontenda i Rust jezgre.
// Tauri v2 automatski mapira camelCase (JS) → snake_case (Rust) argumente.

import { invoke } from "@tauri-apps/api/core";
import type {
  CaptureAction,
  ImageFormat,
  MonitorInfo,
  PixelRect,
  RecFrameRect,
  RecordOptions,
  RecordStatus,
  Region,
  Screenshot,
} from "./types";

export const ipc = {
  // ── capture ──
  listMonitors: () => invoke<MonitorInfo[]>("list_monitors"),
  beginRegionCapture: (mode: "shot" | "record" = "shot") =>
    invoke<void>("begin_region_capture", { mode }),
  getFrozenFrame: (monitorId: number) =>
    invoke<string>("get_frozen_frame", { monitorId }),
  getFrozenRgba: (monitorId: number) =>
    invoke<ArrayBuffer>("get_frozen_rgba", { monitorId }),
  finishRegionCapture: (
    region: Region,
    action: CaptureAction,
    format: ImageFormat,
    quality: number,
  ) =>
    invoke<Screenshot | null>("finish_region_capture", {
      region,
      action,
      format,
      quality,
    }),
  cancelCapture: () => invoke<void>("cancel_capture"),
  /** Overlay se prikazuje cloakan (DWM) — otkrij ga kad je novi frame iscrtan. */
  revealOverlay: () => invoke<void>("reveal_overlay"),
  captureFullscreen: (format: ImageFormat, quality: number) =>
    invoke<Screenshot>("capture_fullscreen", { format, quality }),

  // ── snimanje ekrana ──
  startRecording: (
    region: Region | null,
    monitorId?: number,
    options?: Partial<RecordOptions>,
  ) =>
    invoke<RecordStatus>("start_recording", { region, monitorId, options }),
  stopRecording: () => invoke<void>("stop_recording"),
  cancelRecording: () => invoke<void>("cancel_recording"),
  pauseRecording: (paused: boolean) =>
    invoke<RecordStatus>("pause_recording", { paused }),
  recordingStatus: () => invoke<RecordStatus>("recording_status"),
  closeRecHud: () => invoke<void>("close_rec_hud"),
  /** Snimano područje za okvir (null izvan snimanja). */
  recFrameRect: () => invoke<RecFrameRect | null>("rec_frame_rect"),
  /** true = sloj za crtanje hvata miš; false = klikovi prolaze kroz njega. */
  setDrawInteractive: (interactive: boolean) =>
    invoke<void>("set_draw_interactive", { interactive }),
  /** Redact zone koje Rust pikselizira u svakom frameu snimke. */
  setBlurRegions: (rects: PixelRect[]) =>
    invoke<void>("set_blur_regions", { rects }),

  // ── editor ──
  saveEditedImage: (base64Png: string, format: ImageFormat, quality: number) =>
    invoke<Screenshot>("save_edited_image", { base64Png, format, quality }),
  saveImageAs: (base64Png: string, path: string, format: ImageFormat, quality: number) =>
    invoke<void>("save_image_as", { base64Png, path, format, quality }),
  copyBase64ToClipboard: (base64Png: string) =>
    invoke<void>("copy_base64_to_clipboard", { base64Png }),

  // ── galerija ──
  listScreenshots: (limit?: number, offset?: number) =>
    invoke<Screenshot[]>("list_screenshots", { limit, offset }),
  searchScreenshots: (query: string, limit?: number, offset?: number) =>
    invoke<Screenshot[]>("search_screenshots", { query, limit, offset }),
  deleteScreenshot: (id: string) =>
    invoke<void>("delete_screenshot", { id }),
  addTag: (id: string, tag: string) => invoke<void>("add_tag", { id, tag }),

  // ── preglednik (po id-u zapisa, ne po putanji) ──
  /** Otvori u zadanom programu sustava. */
  openMedia: (id: string) => invoke<void>("open_media", { id }),
  /** Pokaži datoteku u Exploreru. */
  revealMedia: (id: string) => invoke<void>("reveal_media", { id }),
  copyMediaImage: (id: string) => invoke<void>("copy_media_image", { id }),

  // ── sustav ──
  showHome: () => invoke<void>("show_home"),
  updateHotkey: (accelerator: string) =>
    invoke<void>("update_hotkey", { accelerator }),
  updateRecordHotkey: (accelerator: string) =>
    invoke<void>("update_record_hotkey", { accelerator }),
  /** [capture, record] — stvarno registrirani hotkeyi (default može pasti na fallback). */
  activeHotkeys: () => invoke<[string | null, string | null]>("active_hotkeys"),
  logFilePath: () => invoke<string | null>("log_file_path"),
  openCaptureFolder: () => invoke<void>("open_capture_folder"),
  openDataFolder: () => invoke<void>("open_data_folder"),
};
