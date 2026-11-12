//! Serde modeli dijeljeni između Rust jezgre i frontenda (preko IPC-a).
//! Polja su `camelCase` da odgovaraju TypeScript tipovima u `src/lib/types.ts`.

use serde::{Deserialize, Serialize};

/// Izlazni format slike.
#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "lowercase")]
pub enum ImageFormat {
    Png,
    Jpg,
    Webp,
}

impl ImageFormat {
    pub fn ext(&self) -> &'static str {
        match self {
            ImageFormat::Png => "png",
            ImageFormat::Jpg => "jpg",
            ImageFormat::Webp => "webp",
        }
    }
}

impl Default for ImageFormat {
    fn default() -> Self {
        ImageFormat::Png
    }
}

/// Izvor capture-a.
#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum CaptureSource {
    Region,
    Fullscreen,
    Window,
    Recording,
}

impl CaptureSource {
    pub fn as_str(&self) -> &'static str {
        match self {
            CaptureSource::Region => "region",
            CaptureSource::Fullscreen => "fullscreen",
            CaptureSource::Window => "window",
            CaptureSource::Recording => "recording",
        }
    }
}

/// Informacije o jednom monitoru (šalju se overlayu da zna gdje crtati).
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct MonitorInfo {
    pub id: u32,
    pub name: String,
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
    pub scale: f32,
    pub is_primary: bool,
}

/// Pravokutna regija u koordinatama jednog monitora (fizički pikseli).
#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Region {
    pub monitor_id: u32,
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
}

/// Akcija koju korisnik odabere u overlay floating toolbaru.
#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum CaptureAction {
    Copy,
    Save,
    Edit,
    Upload, // Faza 3
}

/// Zapis screenshota u lokalnoj galeriji.
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Screenshot {
    pub id: String,
    pub file_path: String,
    pub thumb_path: Option<String>,
    pub name: String,
    pub format: String,
    pub width: u32,
    pub height: u32,
    pub size_bytes: u64,
    pub source: String,
    pub created_at: i64, // unix ms
    pub is_uploaded: bool,
    pub tags: Vec<String>,
}

// ───────────────────────────── snimanje ekrana ─────────────────────────────

/// Kontejner/kodek snimke.
///
/// `Mp4` koristi Windows Media Foundation (H.264, hardverski enkoder kad postoji) —
/// isti format koji daje macOS Cmd+Shift+5, pa se datoteka otvara svugdje.
/// `Gif` je cross-platform fallback (i namjerno dostupan svima) za kratke isječke.
#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "lowercase")]
pub enum RecordFormat {
    Mp4,
    Gif,
}

impl RecordFormat {
    pub fn ext(&self) -> &'static str {
        match self {
            RecordFormat::Mp4 => "mp4",
            RecordFormat::Gif => "gif",
        }
    }
}

impl Default for RecordFormat {
    fn default() -> Self {
        // Na ne-Windows platformama MP4 enkoder još ne postoji → GIF je default.
        if cfg!(windows) {
            RecordFormat::Mp4
        } else {
            RecordFormat::Gif
        }
    }
}

/// Opcije snimanja koje šalje frontend.
#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct RecordOptions {
    /// Ciljani broj sličica u sekundi (5..=60).
    pub fps: u32,
    pub format: RecordFormat,
    /// 1..=100 — mapira se u bitrate (MP4) odnosno rezoluciju/paletu (GIF).
    pub quality: u8,
    /// Ucrtaj pokazivač miša u snimku.
    pub capture_cursor: bool,
    /// Tvrdi limit trajanja u sekundama (0 = bez limita). Štiti od punjenja diska.
    pub max_seconds: u32,
    /// Snimaj mikrofon (zadani capture uređaj). Samo MP4.
    #[serde(default)]
    pub mic: bool,
    /// Snimaj sistemski zvuk (WASAPI loopback zadanog izlaza). Samo MP4.
    #[serde(default)]
    pub system_audio: bool,
}

impl Default for RecordOptions {
    fn default() -> Self {
        Self {
            fps: 24,
            format: RecordFormat::default(),
            quality: 80,
            capture_cursor: true,
            max_seconds: 30 * 60,
            mic: false,
            system_audio: false,
        }
    }
}

/// Faza u kojoj se snimanje nalazi (dijeli se s frontendom za HUD).
#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "lowercase")]
pub enum RecordPhase {
    Idle,
    Recording,
    Paused,
    /// Snimanje je stalo, enkoder još zatvara datoteku (može potrajati kod GIF-a).
    Finishing,
}

/// Pravokutnik u pikselima SNIMANOG područja (0,0 = gornji lijevi kut snimke).
///
/// Koristi se za redact tijekom snimanja: sloj za crtanje ne može pročitati piksele
/// desktopa ispod sebe, pa pikselizaciju radi Rust nad svakim frameom.
#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PixelRect {
    pub x: u32,
    pub y: u32,
    pub width: u32,
    pub height: u32,
}

/// Snimano područje u fizičkim pikselima MONITORA — okvir (`recframe`) po njemu
/// zatamni ostatak ekrana i nacrta rub. `full` = snima se cijeli monitor (nema dima).
#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct RecFrameRect {
    pub x: u32,
    pub y: u32,
    pub width: u32,
    pub height: u32,
    pub full: bool,
}

/// Stanje snimanja koje HUD prikazuje (i poll-a nakon reconnecta).
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct RecordStatus {
    pub phase: RecordPhase,
    /// Trajanje snimljenog materijala u milisekundama (pauza se ne broji).
    pub elapsed_ms: u64,
    pub frames: u64,
    pub format: RecordFormat,
    pub width: u32,
    pub height: u32,
    /// Veličina datoteke do sada, u bajtovima (0 dok enkoder ne flusha).
    pub size_bytes: u64,
    /// Što se od zvuka stvarno snima (izvor koji se nije dao otvoriti ovdje je false).
    pub mic: bool,
    pub system_audio: bool,
}

impl RecordStatus {
    pub fn idle() -> Self {
        Self {
            phase: RecordPhase::Idle,
            elapsed_ms: 0,
            frames: 0,
            format: RecordFormat::default(),
            width: 0,
            height: 0,
            size_bytes: 0,
            mic: false,
            system_audio: false,
        }
    }
}
