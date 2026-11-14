/** Lightweight, validated local player preferences. No native permissions required. */
export type VolumePreference = { volume: number; muted: boolean };
export const PLAYBACK_SPEEDS: readonly number[] = [1, 1.25, 1.5, 2, 0.5, 0.75];

export function normalizeVolume(value: unknown): VolumePreference {
  if (!value || typeof value !== "object") return { volume: 1, muted: false };
  const stored = value as { volume?: unknown; muted?: unknown };
  const volume = typeof stored.volume === "number" && Number.isFinite(stored.volume)
    ? Math.max(0, Math.min(1, stored.volume))
    : 1;
  return { volume, muted: stored.muted === true };
}

export function readVolume(store?: Pick<Storage, "getItem">): VolumePreference {
  try {
    // Access localStorage inside try: WebView may throw a SecurityError just
    // when resolving the storage getter, before getItem() can be called.
    const source = store ?? localStorage;
    return normalizeVolume(JSON.parse(source.getItem("snapvere-volume") ?? "null"));
  } catch {
    return { volume: 1, muted: false };
  }
}
