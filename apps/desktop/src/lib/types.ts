// TS ogledalo Rust modela (serde camelCase). Vidi src-tauri/src/models.rs.

export type ImageFormat = "png" | "jpg" | "webp";
export type CaptureSource = "region" | "fullscreen" | "window" | "recording";
export type CaptureAction = "copy" | "save" | "edit";

export interface MonitorInfo {
  id: number;
  name: string;
  x: number;
  y: number;
  width: number;
  height: number;
  scale: number;
  isPrimary: boolean;
}

export interface Region {
  monitorId: number;
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface Screenshot {
  id: string;
  filePath: string;
  thumbPath: string | null;
  name: string;
  format: string;
  width: number;
  height: number;
  sizeBytes: number;
  source: string;
  createdAt: number;
  isUploaded: boolean;
  tags: string[];
}

// ── Editor / overlay tipovi (frontend-only) ──
export type AnnotationTool =
  | "arrow"
  | "rect"
  | "ellipse"
  | "line"
  | "pen"
  | "marker"
  | "blur"
  | "text"
  | "step";

/** Alati u toolbaru = anotacijski alati + interaktivni (kursor, eyedropper). */
export type EditorTool = AnnotationTool | "select" | "eyedropper";

export interface Point {
  x: number;
  y: number;
}

export interface Annotation {
  id: string;
  tool: AnnotationTool;
  color: string;
  size: number;
  points: Point[]; // pen/marker: mnogo točaka; oblici: [start, end]; text/step: [pos]
  text?: string; // text sadržaj ili broj koraka
}

// ── snimanje ekrana ──

export type RecordFormat = "mp4" | "gif";
export type RecordPhase = "idle" | "recording" | "paused" | "finishing";

export interface RecordOptions {
  fps: number;
  format: RecordFormat;
  /** 1..100 — mapira se u bitrate (MP4). */
  quality: number;
  captureCursor: boolean;
  /** Tvrdi limit trajanja u sekundama; 0 = bez limita. */
  maxSeconds: number;
}

export interface RecordStatus {
  phase: RecordPhase;
  elapsedMs: number;
  frames: number;
  format: RecordFormat;
  width: number;
  height: number;
  sizeBytes: number;
  /** Što se od zvuka stvarno snima (izvor koji se nije otvorio je false). */
  mic: boolean;
  systemAudio: boolean;
}

/** Payload eventa `snapvere:rec-done`. */
export interface RecordDone {
  screenshot: Screenshot | null;
  error: string | null;
  cancelled: boolean;
}

/** Payload eventa `snapvere:capture` — overlay ga dobiva pri svakom capture-u. */
export interface CapturePayload {
  token: number;
  id: number;
  mode: "shot" | "record";
  x: number;
  y: number;
  width: number;
  height: number;
}

/** Pravokutnik u pikselima snimanog područja (Rust ga pikselizira po frameu). */
export interface PixelRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

/** Alat u sloju za crtanje preko snimke; "off" = sloj propušta klikove. */
export type RecTool = AnnotationTool | "off";

/** Poruka `snapvere:draw-tool` — HUD je izvor istine za alat, boju i debljinu. */
export interface DrawToolMsg {
  tool: RecTool;
  color?: string;
  size?: number;
}

/** Datoteka u galeriji je video kad joj je format mp4/gif iz snimanja. */
export function isVideo(s: Screenshot): boolean {
  return s.source === "recording" || s.format === "mp4";
}

/** Payload eventa `snapvere:rec-frame` — snimano područje u fizičkim px monitora. */
export interface RecFrameRect {
  x: number;
  y: number;
  width: number;
  height: number;
  /** Snima se cijeli monitor: nema zatamnjenja, samo rub. */
  full: boolean;
}
