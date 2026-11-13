import type { ImageFormat } from "./types";

/** Shared screenshot export settings; kept pure for direct unit testing. */
export interface CapturePreferences {
  format: ImageFormat;
  quality: number;
}

export const DEFAULT_CAPTURE_PREFERENCES: Readonly<CapturePreferences> = {
  format: "png",
  quality: 92,
};

export function normalizeCapturePreferences(
  format: unknown,
  quality: unknown,
): CapturePreferences {
  const validFormat: ImageFormat =
    format === "jpg" || format === "webp" || format === "png" ? format : "png";
  const validQuality =
    typeof quality === "number" && Number.isFinite(quality)
      ? Math.max(1, Math.min(100, Math.round(quality)))
      : DEFAULT_CAPTURE_PREFERENCES.quality;
  return { format: validFormat, quality: validQuality };
}

/** Match the bytes written by the Rust encoder to the actual file extension. */
export function formatFromSavePath(path: string): ImageFormat | null {
  const extension = /\.([^.\\/]+)$/.exec(path.trim())?.[1]?.toLowerCase();
  if (extension === "jpg" || extension === "jpeg") return "jpg";
  if (extension === "png" || extension === "webp") return extension;
  return null;
}
