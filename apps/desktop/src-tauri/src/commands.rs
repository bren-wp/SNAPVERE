//! Tauri komande — jedini most između frontenda i jezgre.
//! Frontend ih zove preko `invoke()` (vidi `src/lib/ipc.ts`).
//!
//! Pravilo dretvi: NIŠTA što dira ekran (xcap, DXGI, enkoder) ne smije se izvoditi
//! na glavnoj dretvi ni u callbacku globalnog hotkeya. Sinkrone Tauri komande rade
//! na glavnoj dretvi, a hotkey callback na dretvi listenera — grafički capture na
//! bilo kojoj od njih zna blokirati petlju poruka ili paničariti u tuđem FFI kodu,
//! što je uz `panic = "abort"` značilo trenutni nestanak aplikacije.

use std::fs;
use std::io::Write;
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicBool, Ordering};

use base64::Engine;
use chrono::{Local, Utc};
use tauri::ipc::Response;
use tauri::{
    AppHandle, Emitter, Manager, PhysicalPosition, PhysicalSize, State, WebviewUrl,
    WebviewWindowBuilder,
};
use tauri_plugin_clipboard_manager::ClipboardExt;
use tauri_plugin_opener::OpenerExt;
use tauri_plugin_store::StoreExt;
use uuid::Uuid;

use crate::capture;
use crate::logging::{self, log};
use crate::models::{
    CaptureAction, CaptureSource, ImageFormat, MonitorInfo, PixelRect, RecFrameRect, RecordOptions,
    RecordStatus, Region, Screenshot,
};
use crate::record::{self, RecordTarget};
use crate::state::{lock, AppState};

fn estr<E: std::fmt::Display>(e: E) -> String {
    e.to_string()
}

/// Capture je "u letu" — sprječava da drugi pritisak hotkeya krene prije nego prvi
/// završi. Bez ovoga se overlay zna repozicionirati usred učitavanja framea.
static CAPTURE_BUSY: AtomicBool = AtomicBool::new(false);

/// Način rada overlaya: obična snimka zaslona ili odabir područja za snimanje videa.
#[derive(Debug, Clone, Copy, PartialEq, Eq, serde::Serialize, serde::Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum CaptureMode {
    Shot,
    Record,
}

impl CaptureMode {
    fn as_str(&self) -> &'static str {
        match self {
            CaptureMode::Shot => "shot",
            CaptureMode::Record => "record",
        }
    }
}

// ──────────────────────────────── putanje ────────────────────────────────

/// Folder galerije. Poštuje `saveFolder` iz Settingsa kad je apsolutna putanja,
/// inače Slike/Snapvere.
pub(crate) fn gallery_dir(app: &AppHandle) -> Result<PathBuf, String> {
    if let Some(custom) = configured_folder(app) {
        return Ok(custom);
    }
    let base = app.path().picture_dir().map_err(estr)?;
    Ok(base.join("Snapvere"))
}

/// Pročitaj korisnički odabran folder iz store-a. Sve greške → None (fallback).
fn configured_folder(app: &AppHandle) -> Option<PathBuf> {
    let store = app.store("settings.json").ok()?;
    let value = store.get("saveFolder")?;
    let s = value.as_str()?.trim().to_string();
    if s.is_empty() {
        return None;
    }
    let p = PathBuf::from(s);
    // Default vrijednost iz UI-ja ("Pictures/Snapvere") je relativna — nju ignoriramo.
    p.is_absolute().then_some(p)
}

fn thumbs_dir(app: &AppHandle) -> Result<PathBuf, String> {
    let base = app.path().app_data_dir().map_err(estr)?;
    Ok(base.join("thumbnails"))
}

// ──────────────────────────── interni helperi ────────────────────────────

/// Create new media exclusively; never overwrite existing captures.
/// Remove the newly created file if an interrupted write leaves partial bytes.
fn write_new_capture(path: &Path, bytes: &[u8]) -> Result<(), String> {
    let mut file = fs::OpenOptions::new()
        .write(true)
        .create_new(true)
        .open(path)
        .map_err(estr)?;
    if let Err(error) = file.write_all(bytes) {
        drop(file);
        if let Err(cleanup) = fs::remove_file(path) {
            log(&format!(
                "incomplete capture cleanup {}: {cleanup}",
                path.display()
            ));
        }
        return Err(format!("Could not write capture: {error}"));
    }
    Ok(())
}

/// Spremi sliku u galeriju (datoteka + thumbnail + DB zapis) i vrati zapis.
fn save_to_gallery(
    app: &AppHandle,
    state: &State<AppState>,
    img: &image::RgbaImage,
    source: CaptureSource,
    format: ImageFormat,
    quality: u8,
) -> Result<Screenshot, String> {
    let bytes = capture::encode(img, format, quality).map_err(estr)?;

    let dir = gallery_dir(app)?;
    fs::create_dir_all(&dir).map_err(estr)?;

    let id = Uuid::new_v4().to_string();
    let name = format!("Snapvere {}", Local::now().format("%Y-%m-%d %H.%M.%S"));
    // Stable display name, unique on-disk filename: never overwrite captures in the same second.
    let path = dir.join(format!("{name}-{id}.{}", format.ext()));
    write_new_capture(&path, &bytes)?;

    let thumb_path = write_thumbnail(app, &id, img);

    let shot = Screenshot {
        id,
        file_path: path.to_string_lossy().into_owned(),
        thumb_path,
        name,
        format: format.ext().to_string(),
        width: img.width(),
        height: img.height(),
        size_bytes: bytes.len() as u64,
        source: source.as_str().to_string(),
        created_at: Utc::now().timestamp_millis(),
        is_uploaded: false,
        tags: Vec::new(),
    };

    if let Err(error) = lock(&state.db).insert(&shot) {
        // Clean up only newly generated media; never touch existing user files.
        if let Some(thumb) = &shot.thumb_path {
            if let Err(cleanup) = fs::remove_file(thumb) {
                log(&format!("orphan thumbnail cleanup {thumb}: {cleanup}"));
            }
        }
        if let Err(cleanup) = fs::remove_file(&path) {
            log(&format!(
                "orphan capture cleanup {}: {cleanup}",
                path.display()
            ));
        }
        return Err(format!("Could not index capture in gallery: {error}"));
    }
    allow_media_dirs(app, std::slice::from_ref(&shot));
    Ok(shot)
}

/// Upiši u galeriju datoteku koja je VEĆ na disku (snimka videa). Thumbnail se
/// generira iz prvog framea koji je snimatelj sačuvao.
pub(crate) fn register_media(
    app: &AppHandle,
    path: &Path,
    width: u32,
    height: u32,
    source: CaptureSource,
    ext: &str,
    thumb: Option<&image::RgbaImage>,
) -> Result<Screenshot, String> {
    let size_bytes = fs::metadata(path).map(|m| m.len()).map_err(estr)?;
    let id = Uuid::new_v4().to_string();
    let name = path
        .file_stem()
        .map(|s| s.to_string_lossy().into_owned())
        .unwrap_or_else(|| format!("Snapvere {}", Local::now().format("%Y-%m-%d %H.%M.%S")));

    let thumb_path = thumb.and_then(|img| write_thumbnail(app, &id, img));

    let shot = Screenshot {
        id,
        file_path: path.to_string_lossy().into_owned(),
        thumb_path,
        name,
        format: ext.to_string(),
        width,
        height,
        size_bytes,
        source: source.as_str().to_string(),
        created_at: Utc::now().timestamp_millis(),
        is_uploaded: false,
        tags: Vec::new(),
    };

    let state = app.state::<AppState>();
    if let Err(error) = lock(&state.db).insert(&shot) {
        // Keep the original recording on disk even if gallery indexing fails.
        if let Some(thumb) = &shot.thumb_path {
            if let Err(cleanup) = fs::remove_file(thumb) {
                log(&format!("orphan thumbnail cleanup {thumb}: {cleanup}"));
            }
        }
        return Err(format!(
            "Could not index recording: {error}. Original file retained at {}",
            path.display()
        ));
    }
    allow_media_dirs(app, std::slice::from_ref(&shot));
    Ok(shot)
}

/// Thumbnail (WebP, max 480px). Neuspjeh nije fatalan — galerija onda pokaže original.
fn write_thumbnail(app: &AppHandle, id: &str, img: &image::RgbaImage) -> Option<String> {
    let tdir = thumbs_dir(app).ok()?;
    fs::create_dir_all(&tdir).ok()?;
    let thumb = capture::thumbnail(img, 480);
    let bytes = capture::encode(&thumb, ImageFormat::Webp, 80).ok()?;
    let path = tdir.join(format!("{id}.webp"));
    fs::write(&path, &bytes).ok()?;
    Some(path.to_string_lossy().into_owned())
}

fn copy_rgba_to_clipboard(app: &AppHandle, img: &image::RgbaImage) -> Result<(), String> {
    let image = tauri::image::Image::new(img.as_raw(), img.width(), img.height());
    app.clipboard().write_image(&image).map_err(estr)
}

/// Trenutna pozicija miša u fizičkim px (Windows). Fallback (0,0).
fn cursor_pos() -> (i32, i32) {
    #[cfg(windows)]
    {
        use windows::Win32::Foundation::POINT;
        use windows::Win32::UI::WindowsAndMessaging::GetCursorPos;
        let mut p = POINT::default();
        if unsafe { GetCursorPos(&mut p) }.is_ok() {
            return (p.x, p.y);
        }
    }
    (0, 0)
}

/// Izuzmi prozor iz SVAKOG snimanja/capture-a ekrana (Windows 10 2004+).
///
/// Bez ovoga bi HUD s tajmerom i sam overlay završili u snimci cijelog monitora —
/// macOS svoju traku skriva na isti način.
#[cfg(windows)]
fn exclude_from_capture(win: &tauri::WebviewWindow) {
    use windows::Win32::Foundation::HWND;
    use windows::Win32::UI::WindowsAndMessaging::{
        SetWindowDisplayAffinity, WDA_EXCLUDEFROMCAPTURE,
    };
    let Ok(handle) = win.hwnd() else { return };
    // Tauri vraća HWND iz svoje verzije `windows` cratea; oba su newtype nad *mut c_void.
    let hwnd = HWND(handle.0 as _);
    if unsafe { SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE) }.is_err() {
        log("WDA_EXCLUDEFROMCAPTURE nije podržan na ovoj verziji Windowsa");
    }
}

#[cfg(not(windows))]
fn exclude_from_capture(_win: &tauri::WebviewWindow) {}

/// DWM "cloak": prozor je za sustav i WebView2 normalno prikazan (Chromium crta),
/// ali ga DWM ne komponira na ekran.
///
/// Overlay se između dva capture-a samo sakrije, a pri ponovnom `show()` DWM
/// 3-4 framea pokazuje ZADNJU sliku koju je prozor imao — stari frozen frame (često
/// s drugog monitora), crninu ili, ovisno o driveru, šarene smetnje. Zato se overlay
/// prikazuje cloakan i otkriva tek kad Chromium iscrta novi frame (`reveal_overlay`).
#[cfg(windows)]
fn set_cloaked(win: &tauri::WebviewWindow, cloaked: bool) {
    use windows::Win32::Foundation::HWND;
    use windows::Win32::Graphics::Dwm::{DwmSetWindowAttribute, DWMWA_CLOAK};
    let Ok(handle) = win.hwnd() else { return };
    let hwnd = HWND(handle.0 as _);
    let value: i32 = cloaked.into(); // Win32 BOOL
    let _ = unsafe {
        DwmSetWindowAttribute(
            hwnd,
            DWMWA_CLOAK,
            &value as *const i32 as *const _,
            std::mem::size_of::<i32>() as u32,
        )
    };
}

#[cfg(not(windows))]
fn set_cloaked(_win: &tauri::WebviewWindow, _cloaked: bool) {}

/// Digni prozor na vrh topmost sloja BEZ aktivacije.
///
/// Oba prozora snimanja su always-on-top; među takvima pobjeđuje zadnji aktivirani.
/// Čim sloj za crtanje dobije fokus, digne se IZNAD HUD-a — i ako regija pokriva dno
/// ekrana, svaki klik na "Stop" postane potez olovkom. Tauri `set_always_on_top`
/// ne pomaže (flag je već postavljen, pa ne zove SetWindowPos).
#[cfg(windows)]
pub fn raise_topmost(win: &tauri::WebviewWindow) {
    use windows::Win32::Foundation::HWND;
    use windows::Win32::UI::WindowsAndMessaging::{
        SetWindowPos, HWND_TOPMOST, SWP_NOACTIVATE, SWP_NOMOVE, SWP_NOSIZE,
    };
    let Ok(handle) = win.hwnd() else { return };
    let hwnd = HWND(handle.0 as _);
    let _ = unsafe {
        SetWindowPos(
            hwnd,
            Some(HWND_TOPMOST),
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE,
        )
    };
}

#[cfg(not(windows))]
pub fn raise_topmost(win: &tauri::WebviewWindow) {
    let _ = win.set_always_on_top(true);
}

/// HUD mora ostati klikabilan što god se dogodilo sa slojem za crtanje.
pub fn raise_hud(app: &AppHandle) {
    if let Some(hud) = app.get_webview_window("recbar") {
        if hud.is_visible().unwrap_or(false) {
            raise_topmost(&hud);
        }
    }
}

/// Sakrij (ne zatvaraj!) overlay — ostaje "topao" za sljedeći capture.
fn hide_overlays(app: &AppHandle) {
    if let Some(win) = app.get_webview_window("overlay") {
        let _ = win.hide();
    }
}

/// Pred-kreiraj JEDAN skriveni overlay prozor ("overlay") ako ne postoji.
/// Webview se učita unaprijed → prikaz na hotkey je instant; pozicioniramo ga po monitoru pri capture-u.
///
/// Prozor je transparentan: u shot modu CSS ga čini crnim i preko njega ide zamrznuti
/// frame (ništa se ne mijenja), a u record modu propušta živi desktop pa odabir
/// područja ne treba nikakav screenshot.
pub fn ensure_overlay(app: &AppHandle) -> Result<(), String> {
    if app.get_webview_window("overlay").is_some() {
        return Ok(());
    }
    let win =
        WebviewWindowBuilder::new(app, "overlay", WebviewUrl::App("index.html#overlay".into()))
            .decorations(false)
            .always_on_top(true)
            .skip_taskbar(true)
            .resizable(false)
            .shadow(false)
            .transparent(true)
            .visible(false)
            .build()
            .map_err(estr)?;
    exclude_from_capture(&win);
    Ok(())
}

/// Pred-kreiraj HUD i sloj za crtanje (skrivene). Stvaranje WebView2 prozora traje
/// ~0.5 s po prozoru — kad bi se radilo tek na klik "Record", snimanje bi već teklo
/// sekundu prije nego korisnik vidi ikakvu kontrolu.
pub fn ensure_rec_windows(app: &AppHandle) -> Result<(), String> {
    if app.get_webview_window("recbar").is_none() {
        let win =
            WebviewWindowBuilder::new(app, "recbar", WebviewUrl::App("index.html#recbar".into()))
                .title("Snapvere — Recording")
                .decorations(false)
                .always_on_top(true)
                .skip_taskbar(true)
                .resizable(false)
                .shadow(false)
                .transparent(true)
                // WS_EX_NOACTIVATE: gumbi rade na klik, ali tipkovnica NIKAD ne prelazi na
                // HUD — inače Enter/brojke pritisnute "u svojoj aplikaciji" završe ovdje.
                .focusable(false)
                .inner_size(REC_HUD_W as f64, REC_HUD_H as f64)
                .visible(false)
                .build()
                .map_err(estr)?;
        exclude_from_capture(&win);
    }
    if app.get_webview_window("recdraw").is_none() {
        let win =
            WebviewWindowBuilder::new(app, "recdraw", WebviewUrl::App("index.html#recdraw".into()))
                .title("Snapvere — Draw")
                .decorations(false)
                .always_on_top(true)
                .skip_taskbar(true)
                .resizable(false)
                .shadow(false)
                .transparent(true)
                .focused(false)
                .visible(false)
                .build()
                .map_err(estr)?;
        let _ = win.set_ignore_cursor_events(true);
    }
    if app.get_webview_window("recframe").is_none() {
        // Okvir snimanja: cijeli monitor, zatamni sve OKO snimanog područja i nacrta rub.
        // Izuzet je iz capture-a (dim ne smije završiti u videu) i propušta sve klikove.
        let win = WebviewWindowBuilder::new(
            app,
            "recframe",
            WebviewUrl::App("index.html#recframe".into()),
        )
        .title("Snapvere — Frame")
        .decorations(false)
        .always_on_top(true)
        .skip_taskbar(true)
        .resizable(false)
        .shadow(false)
        .transparent(true)
        .focusable(false)
        .visible(false)
        .build()
        .map_err(estr)?;
        let _ = win.set_ignore_cursor_events(true);
        exclude_from_capture(&win);
    }
    Ok(())
}

/// Traka nosi tajmer, alate za crtanje i kontrole snimanja.
/// Širina je zbroj svih gumba (10 alata + boja/undo/clear + pauza/stop/odbaci + info):
/// preuska traka tiho odreže desne gumbe — flex djeca su `shrink-0`.
const REC_HUD_W: u32 = 820;
const REC_HUD_H: u32 = 58;

fn open_editor_window(app: &AppHandle, file_path: &str) -> Result<(), String> {
    // Newly saved gallery images may not have been listed yet, so their
    // file:// asset URLs would otherwise be blocked in the editor WebView.
    // Permit the exact saved file, never an arbitrary directory.
    let image = Path::new(file_path);
    if !image.is_file() {
        return Err("Saved screenshot is missing; cannot open editor".into());
    }
    app.asset_protocol_scope()
        .allow_file(image)
        .map_err(|e| format!("Cannot authorize screenshot for editor: {e}"))?;
    // Putanju proslijedimo base64-encoded u hashu (izbjegava probleme s razmacima/\).
    let enc = base64::engine::general_purpose::STANDARD.encode(file_path);
    let url = format!("index.html#editor?p={enc}");
    if let Some(existing) = app.get_webview_window("editor") {
        let _ = existing.close();
    }
    WebviewWindowBuilder::new(app, "editor", WebviewUrl::App(url.into()))
        .title("Snapvere — Editor")
        .inner_size(1120.0, 760.0)
        .min_inner_size(800.0, 560.0)
        .center()
        .build()
        .map_err(estr)?;
    Ok(())
}

// ──────────────────────────────── komande ────────────────────────────────

#[tauri::command]
pub fn list_monitors() -> Result<Vec<MonitorInfo>, String> {
    logging::guard("list_monitors", || capture::list_monitors().map_err(estr))
}

#[derive(serde::Serialize, Clone)]
struct CapturePayload {
    token: i64,
    id: u32,
    mode: &'static str,
    /// Fizička pozicija/veličina monitora — overlay ih šalje natrag pri pokretanju snimanja.
    x: i32,
    y: i32,
    width: u32,
    height: u32,
}

/// Pokreni odabir područja. Vraća ODMAH — pravi capture ide na radnu dretvu, a
/// overlay se javi kad je frame spreman (`snapvere:capture`).
#[tauri::command]
pub fn begin_region_capture(app: AppHandle, mode: Option<CaptureMode>) -> Result<(), String> {
    dispatch_region_capture(&app, mode.unwrap_or(CaptureMode::Shot));
    Ok(())
}

/// Zajednička ulazna točka za hotkey, tray i frontend.
pub fn dispatch_region_capture(app: &AppHandle, mode: CaptureMode) {
    if record::is_active(app) {
        let _ = app.emit("snapvere:toast", "Snimanje je u tijeku");
        return;
    }
    // Drugi pritisak dok je overlay već gore = odustani. Prije je ponovno hvatao
    // ekran (uključujući sam overlay) — beskorisno i sporo.
    if let Some(win) = app.get_webview_window("overlay") {
        if win.is_visible().unwrap_or(false) {
            let _ = win.hide();
            lock(&app.state::<AppState>().frozen).clear();
            return;
        }
    }
    // compare_exchange umjesto load+store: dva brza pritiska hotkeya inače prođu oba.
    if CAPTURE_BUSY
        .compare_exchange(false, true, Ordering::SeqCst, Ordering::SeqCst)
        .is_err()
    {
        return;
    }

    let app = app.clone();
    let spawned = std::thread::Builder::new()
        .name("snapvere-capture".into())
        .spawn(move || {
            let result = logging::guard("region capture", || region_capture_blocking(&app, mode));
            CAPTURE_BUSY.store(false, Ordering::SeqCst);
            if let Err(e) = result {
                let _ = app.emit("snapvere:toast", format!("Capture nije uspio: {e}"));
            }
        });

    if spawned.is_err() {
        CAPTURE_BUSY.store(false, Ordering::SeqCst);
        log("ne mogu pokrenuti capture dretvu");
    }
}

/// Capture the monitor under the cursor directly into memory.
/// Never write a transient frozen.png to disk or depend on asset:// grants:
/// the overlay receives the pixels over a scoped Tauri IPC command.
fn region_capture_blocking(app: &AppHandle, mode: CaptureMode) -> Result<(), String> {
    let (cx, cy) = cursor_pos();
    let t = std::time::Instant::now();
    let token = Utc::now().timestamp_millis();

    // Recording uses a transparent overlay and the live desktop.
    // Screenshots retain only a memory image until the selection is finished.
    let info = if mode == CaptureMode::Record {
        let info = capture::monitor_at(cx, cy).map_err(estr)?;
        lock(&app.state::<AppState>().frozen).clear();
        info
    } else {
        let (info, img) = capture::capture_at(cx, cy).map_err(estr)?;
        let state = app.state::<AppState>();
        let mut frozen = lock(&state.frozen);
        frozen.clear();
        frozen.frames.insert(info.id, img);
        info
    };

    // If the WebView cannot receive the frame, report the error; do not
    // silently leave frozen pixels in memory or show an unusable overlay.
    let present = (|| -> Result<(), String> {
        ensure_overlay(app)?;
        let win = app
            .get_webview_window("overlay")
            .ok_or_else(|| "Overlay window unavailable".to_string())?;
        set_cloaked(&win, true);
        win.set_position(PhysicalPosition::new(info.x, info.y))
            .map_err(estr)?;
        win.set_size(PhysicalSize::new(info.width, info.height))
            .map_err(estr)?;
        win.emit(
            "snapvere:capture",
            CapturePayload {
                token,
                id: info.id,
                mode: mode.as_str(),
                x: info.x,
                y: info.y,
                width: info.width,
                height: info.height,
            },
        )
        .map_err(estr)
    })();
    if let Err(error) = present {
        hide_overlays(app);
        lock(&app.state::<AppState>().frozen).clear();
        return Err(error);
    }
    log(&format!(
        "capture {}x{} za {:?}, mode {}",
        info.width,
        info.height,
        t.elapsed(),
        mode.as_str()
    ));
    Ok(())
}

/// Otkrij overlay koji je frontend prikazao cloakan (vidi `set_cloaked`).
#[tauri::command]
pub fn reveal_overlay(app: AppHandle) {
    if let Some(win) = app.get_webview_window("overlay") {
        set_cloaked(&win, false);
    }
}

/// Vrati zamrznuti frame monitora kao PNG data-URL (za prikaz u overlayu).
#[tauri::command]
pub fn get_frozen_frame(state: State<AppState>, monitor_id: u32) -> Result<String, String> {
    let frozen = lock(&state.frozen);
    let frame = frozen
        .frames
        .get(&monitor_id)
        .or_else(|| frozen.frames.values().next())
        .ok_or_else(|| format!("no frozen frame for monitor {monitor_id}"))?;
    capture::to_png_data_url(frame).map_err(estr)
}

/// Vrati zamrznuti frame kao SIROVE RGBA bajtove (header: width,height kao u32 LE, pa pikseli).
/// Binarni IPC bez PNG/base64 → overlay se pojavi gotovo instantno (ključno za brzinu).
#[tauri::command]
pub fn get_frozen_rgba(state: State<AppState>, monitor_id: u32) -> Response {
    let frozen = lock(&state.frozen);
    // Jedan frame u igri — uzmi po id-u, ili prvi dostupni (robusno na nepodudaranje id-a).
    let Some(frame) = frozen
        .frames
        .get(&monitor_id)
        .or_else(|| frozen.frames.values().next())
    else {
        return Response::new(Vec::new());
    };
    Response::new(frame_to_rgba_packet(frame))
}

/// Binary response format: 8 bytes of little-endian width and height, then
/// exactly width * height * 4 bytes of RGBA pixels. Never transmit file paths.
fn frame_to_rgba_packet(frame: &image::RgbaImage) -> Vec<u8> {
    let raw = frame.as_raw();
    let mut out = Vec::with_capacity(8 + raw.len());
    out.extend_from_slice(&frame.width().to_le_bytes());
    out.extend_from_slice(&frame.height().to_le_bytes());
    out.extend_from_slice(raw);
    out
}

/// Završi capture regije odabranom akcijom. Vraća zapis ako je nešto spremljeno.
#[tauri::command]
pub fn finish_region_capture(
    app: AppHandle,
    state: State<AppState>,
    region: Region,
    action: CaptureAction,
    format: ImageFormat,
    quality: u8,
) -> Result<Option<Screenshot>, String> {
    // Always close the overlay and release frozen frames, even on invalid
    // selections or when the capture frame has already disappeared.
    let cropped = {
        let frozen = lock(&state.frozen);
        match frozen
            .frames
            .get(&region.monitor_id)
            .or_else(|| frozen.frames.values().next())
        {
            Some(frame) => capture::crop(frame, &region).map_err(estr),
            None => Err("frozen frame missing".to_string()),
        }
    };
    hide_overlays(&app);
    lock(&state.frozen).clear();
    let cropped = cropped?;

    match action {
        CaptureAction::Copy => {
            copy_rgba_to_clipboard(&app, &cropped)?;
            Ok(None)
        }
        CaptureAction::Save => {
            let shot = save_to_gallery(
                &app,
                &state,
                &cropped,
                CaptureSource::Region,
                format,
                quality,
            )?;
            Ok(Some(shot))
        }
        CaptureAction::Edit => {
            let shot = save_to_gallery(
                &app,
                &state,
                &cropped,
                CaptureSource::Region,
                ImageFormat::Png,
                100,
            )?;
            open_editor_window(&app, &shot.file_path)?;
            Ok(Some(shot))
        }
    }
}

/// Odustani od capture-a: sakrij overlay, oslobodi frozen frameove.
#[tauri::command]
pub fn cancel_capture(app: AppHandle, state: State<AppState>) -> Result<(), String> {
    hide_overlays(&app);
    lock(&state.frozen).clear();
    Ok(())
}

/// Capture cijelog primarnog ekrana → spremi u galeriju.
#[tauri::command]
pub async fn capture_fullscreen(
    app: AppHandle,
    format: ImageFormat,
    quality: u8,
) -> Result<Screenshot, String> {
    let handle = app.clone();
    tauri::async_runtime::spawn_blocking(move || {
        logging::guard("capture_fullscreen", || {
            let img = capture::capture_primary().map_err(estr)?;
            let state = handle.state::<AppState>();
            save_to_gallery(
                &handle,
                &state,
                &img,
                CaptureSource::Fullscreen,
                format,
                quality,
            )
        })
    })
    .await
    .map_err(|e| format!("capture task: {e}"))?
}

/// Tray put za full-screen capture — na radnoj dretvi, bez blokiranja menija.
pub fn dispatch_capture_fullscreen(app: &AppHandle) {
    // Tray captures run without a webview: read the same persisted settings
    // that Home and the annotation editor use before starting the worker.
    let (format, quality) = match app.store("settings.json") {
        Ok(store) => {
            let chosen_format = store.get("format");
            let format = match chosen_format.as_ref().and_then(|v| v.as_str()) {
                Some("jpg") => ImageFormat::Jpg,
                Some("webp") => ImageFormat::Webp,
                _ => ImageFormat::Png,
            };
            let quality = store
                .get("quality")
                .and_then(|v| v.as_u64())
                .map(|q| q.clamp(1, 100) as u8)
                .unwrap_or(92);
            (format, quality)
        }
        Err(_) => (ImageFormat::Png, 92),
    };
    let app = app.clone();
    let _ = std::thread::Builder::new()
        .name("snapvere-capture-full".into())
        .spawn(move || {
            let res = logging::guard("capture_fullscreen", || {
                let img = capture::capture_primary().map_err(estr)?;
                let state = app.state::<AppState>();
                save_to_gallery(
                    &app,
                    &state,
                    &img,
                    CaptureSource::Fullscreen,
                    format,
                    quality,
                )
            });
            match res {
                Ok(shot) => {
                    let _ = app.emit("snapvere:shot-saved", shot);
                }
                Err(e) => {
                    let _ = app.emit("snapvere:toast", format!("Capture nije uspio: {e}"));
                }
            }
        });
}

// Validate PNG canvas exports before allocating a full-size RGBA frame.
// Legitimate 8K desktop captures still fit; forged dimensions and overly large
// IPC messages cannot force unbounded decompression.
const MAX_EDITOR_BASE64_LEN: usize = 192 * 1024 * 1024;
const MAX_EDITOR_PIXELS: u64 = 50_000_000;

fn validate_editor_png_header(bytes: &[u8]) -> Result<(), String> {
    if bytes.len() < 24 || !bytes.starts_with(b"\x89PNG\r\n\x1a\n") || &bytes[12..16] != b"IHDR" {
        return Err("Editor export must be a valid PNG image".into());
    }
    let width = u32::from_be_bytes(bytes[16..20].try_into().map_err(estr)?);
    let height = u32::from_be_bytes(bytes[20..24].try_into().map_err(estr)?);
    let pixels = u64::from(width) * u64::from(height);
    if pixels == 0 || pixels > MAX_EDITOR_PIXELS {
        return Err("Editor image exceeds supported pixel dimensions".into());
    }
    Ok(())
}

fn decode_editor_png(encoded: &str) -> Result<image::RgbaImage, String> {
    let encoded = encoded.trim();
    if encoded.len() > MAX_EDITOR_BASE64_LEN {
        return Err("Editor PNG is too large to import safely".into());
    }
    let bytes = base64::engine::general_purpose::STANDARD
        .decode(encoded)
        .map_err(estr)?;
    validate_editor_png_header(&bytes)?;
    let image =
        image::load_from_memory_with_format(&bytes, image::ImageFormat::Png).map_err(estr)?;
    Ok(image.to_rgba8())
}

fn validate_export_path(path: &str, format: ImageFormat) -> Result<PathBuf, String> {
    let target = PathBuf::from(path);
    if !target.is_absolute() {
        return Err("Save As requires an absolute destination path".into());
    }
    let ext = target
        .extension()
        .and_then(|v| v.to_str())
        .ok_or("Save As requires a supported image extension")?;
    let matches = match format {
        ImageFormat::Png => ext.eq_ignore_ascii_case("png"),
        ImageFormat::Jpg => ext.eq_ignore_ascii_case("jpg") || ext.eq_ignore_ascii_case("jpeg"),
        ImageFormat::Webp => ext.eq_ignore_ascii_case("webp"),
    };
    if !matches {
        return Err("Image format must match the Save As file extension".into());
    }
    Ok(target)
}

/// Spremi sliku iz editora (base64 PNG s canvasa) u galeriju.
#[tauri::command]
pub fn save_edited_image(
    app: AppHandle,
    state: State<AppState>,
    base64_png: String,
    format: ImageFormat,
    quality: u8,
) -> Result<Screenshot, String> {
    let img = decode_editor_png(&base64_png)?;
    save_to_gallery(&app, &state, &img, CaptureSource::Region, format, quality)
}

/// Spremi sliku (base64 PNG s canvasa) na proizvoljnu putanju iz "Save As" dialoga.
#[tauri::command]
pub fn save_image_as(
    base64_png: String,
    path: String,
    format: ImageFormat,
    quality: u8,
) -> Result<(), String> {
    let target = validate_export_path(&path, format)?;
    let img = decode_editor_png(&base64_png)?;
    let out = capture::encode(&img, format, quality).map_err(estr)?;
    fs::write(&target, out).map_err(estr)?;
    Ok(())
}

/// Kopiraj base64 PNG (iz editora) u clipboard.
#[tauri::command]
pub fn copy_base64_to_clipboard(app: AppHandle, base64_png: String) -> Result<(), String> {
    let img = decode_editor_png(&base64_png)?;
    copy_rgba_to_clipboard(&app, &img)
}

// ─────────────────────────── snimanje ekrana ───────────────────────────

/// Pokreni snimanje. `region` je u koordinatama monitora (fizički px); bez njega
/// snima se cijeli monitor pod mišem.
/// Async jer `record::start` čeka da radna dretva otvori DXGI + Media Foundation.
/// Na glavnoj dretvi bi to zamrznulo UI na par stotina milisekundi.
#[tauri::command]
pub async fn start_recording(
    app: AppHandle,
    region: Option<Region>,
    monitor_id: Option<u32>,
    options: Option<RecordOptions>,
) -> Result<RecordStatus, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let opts = options.unwrap_or_else(|| record_options(&app));
        start_recording_inner(&app, region, monitor_id, opts)
    })
    .await
    .map_err(|e| format!("start_recording task: {e}"))?
}

fn start_recording_inner(
    app: &AppHandle,
    region: Option<Region>,
    monitor_id: Option<u32>,
    opts: RecordOptions,
) -> Result<RecordStatus, String> {
    let target = resolve_target(app, region, monitor_id)?;
    let (ox, oy, ow, oh) = record::crop_rect(&target)?;

    // Overlay se sklanja TEK nakon uspješnog starta — ako DXGI ili enkoder padnu,
    // korisnik grešku vidi u overlayu koji je još pred njim. Overlay je ionako
    // izuzet iz capture-a (`exclude_from_capture`), pa ne može upasti u snimku.
    let status = record::start(app, target, opts)?;
    hide_overlays(app);
    lock(&app.state::<AppState>().frozen).clear();

    // Okvir ide prvi (najniže), pa sloj za crtanje, pa HUD — HUD mora ostati na vrhu.
    if let Err(e) = show_rec_frame(app, &target, (ox, oy, ow, oh)) {
        log(&format!(
            "okvir snimanja se nije otvorio: {e} — snimanje ipak radi"
        ));
    }
    if let Err(e) = show_rec_draw(app, &target, (ox, oy, ow, oh)) {
        log(&format!(
            "sloj za crtanje se nije otvorio: {e} — snimanje ipak radi"
        ));
    }

    // HUD je udobnost, ne uvjet. Snimanje već teče — ako se traka ne da otvoriti
    // (npr. transparentni prozor nije podržan), stat ćemo hotkeyem ili preko traya.
    if let Err(e) = show_rec_hud(app, &target) {
        log(&format!(
            "HUD snimanja se nije otvorio: {e} — snimanje ipak radi"
        ));
        let _ = app.emit(
            "snapvere:toast",
            "Recording started (controls unavailable — press the hotkey again to stop)",
        );
    }
    Ok(status)
}

/// Opcije snimanja iz Settings store-a; svaka greška → razumni default.
fn record_options(app: &AppHandle) -> RecordOptions {
    let mut o = RecordOptions::default();
    let Ok(store) = app.store("settings.json") else {
        return o;
    };
    if let Some(v) = store.get("recordFps").and_then(|v| v.as_u64()) {
        o.fps = (v as u32).clamp(5, 60);
    }
    if let Some(v) = store.get("recordQuality").and_then(|v| v.as_u64()) {
        o.quality = v.clamp(1, 100) as u8;
    }
    if let Some(v) = store.get("recordCursor").and_then(|v| v.as_bool()) {
        o.capture_cursor = v;
    }
    if let Some(v) = store.get("recordMaxSeconds").and_then(|v| v.as_u64()) {
        o.max_seconds = v.min(4 * 3600) as u32;
    }
    if let Some(v) = store.get("recordMic").and_then(|v| v.as_bool()) {
        o.mic = v;
    }
    if let Some(v) = store.get("recordSystemAudio").and_then(|v| v.as_bool()) {
        o.system_audio = v;
    }
    if let Some(v) = store
        .get("recordFormat")
        .and_then(|v| v.as_str().map(str::to_owned))
    {
        o.format = match v.as_str() {
            "gif" => crate::models::RecordFormat::Gif,
            _ => crate::models::RecordFormat::default(),
        };
    }
    o
}

/// Hotkey put: ako se snima — stani; inače otvori odabir područja u record modu.
pub fn dispatch_quick_record(app: &AppHandle) {
    let app = app.clone();
    let _ = std::thread::Builder::new()
        .name("snapvere-rec-start".into())
        .spawn(move || {
            if let Some(age) = record::age(&app) {
                // Isti hotkey je i "stop" — ali drugi pritisak unutar 1.5 s od starta je
                // gotovo uvijek nestrpljivo ponavljanje, ne namjera. Ignoriraj ga.
                if age < std::time::Duration::from_millis(1500) {
                    log("hotkey: snimanje je tek krenulo, ignoriram stop");
                    return;
                }
                let _ = record::stop(&app);
                return;
            }
            dispatch_region_capture(&app, CaptureMode::Record);
        });
}

/// Tray put: odmah snimaj cijeli monitor pod mišem, bez odabira područja.
pub fn dispatch_record_fullscreen(app: &AppHandle) {
    let app = app.clone();
    let _ = std::thread::Builder::new()
        .name("snapvere-rec-full".into())
        .spawn(move || {
            if record::is_active(&app) {
                let _ = record::stop(&app);
                return;
            }
            let opts = record_options(&app);
            if let Err(e) = start_recording_inner(&app, None, None, opts) {
                let _ = app.emit("snapvere:toast", format!("Snimanje nije krenulo: {e}"));
                log(&format!("dispatch_record_fullscreen: {e}"));
            }
        });
}

#[tauri::command]
pub fn stop_recording(app: AppHandle) -> Result<(), String> {
    record::stop(&app)
}

#[tauri::command]
pub fn cancel_recording(app: AppHandle) -> Result<(), String> {
    record::cancel(&app)
}

#[tauri::command]
pub fn pause_recording(app: AppHandle, paused: bool) -> Result<RecordStatus, String> {
    record::set_paused(&app, paused)
}

#[tauri::command]
pub fn recording_status(app: AppHandle) -> RecordStatus {
    record::status(&app)
}

/// Odredi monitor za snimanje: eksplicitni id, inače onaj pod mišem.
fn resolve_target(
    app: &AppHandle,
    region: Option<Region>,
    monitor_id: Option<u32>,
) -> Result<RecordTarget, String> {
    let _ = app;
    let monitors = capture::list_monitors().map_err(estr)?;
    let wanted = monitor_id.or_else(|| region.map(|r| r.monitor_id));

    let m = wanted
        .and_then(|id| monitors.iter().find(|m| m.id == id))
        .or_else(|| {
            let (cx, cy) = cursor_pos();
            monitors.iter().find(|m| {
                cx >= m.x && cx < m.x + m.width as i32 && cy >= m.y && cy < m.y + m.height as i32
            })
        })
        .or_else(|| monitors.iter().find(|m| m.is_primary))
        .or_else(|| monitors.first())
        .ok_or("nema dostupnog monitora")?;

    Ok(RecordTarget {
        monitor_id: m.id,
        monitor_x: m.x,
        monitor_y: m.y,
        monitor_w: m.width,
        monitor_h: m.height,
        region,
    })
}

/// Mala uvijek-na-vrhu traka s tajmerom i kontrolama, na dnu snimanog monitora.
fn show_rec_hud(app: &AppHandle, target: &RecordTarget) -> Result<(), String> {
    ensure_rec_windows(app)?;
    let win = app
        .get_webview_window("recbar")
        .ok_or("HUD prozor nedostaje")?;

    // Dno-centar snimanog monitora, malo iznad ruba.
    let x = target.monitor_x + (target.monitor_w as i32 - REC_HUD_W as i32) / 2;
    let y = target.monitor_y + target.monitor_h as i32 - REC_HUD_H as i32 - 48;
    let _ = win.set_position(PhysicalPosition::new(x, y));
    let _ = win.show();
    raise_topmost(&win);
    Ok(())
}

/// Prozirni sloj za crtanje, točno preko snimanog područja.
///
/// Za razliku od HUD-a, ovaj prozor NAMJERNO nije izuzet iz capture-a: DXGI hvata
/// kompozitirani desktop, pa sve nacrtano završi u videu bez ijedne linije koda za
/// kompozitiranje u Rustu — isti canvas renderer kao kod screenshota.
fn show_rec_draw(
    app: &AppHandle,
    target: &RecordTarget,
    crop: (u32, u32, u32, u32),
) -> Result<(), String> {
    let (ox, oy, ow, oh) = crop;
    let x = target.monitor_x + ox as i32;
    let y = target.monitor_y + oy as i32;

    ensure_rec_windows(app)?;
    let win = app
        .get_webview_window("recdraw")
        .ok_or("prozor za crtanje nedostaje")?;
    // Čist sloj za novo snimanje — anotacije prošlog ne smiju ostati.
    let _ = win.emit("snapvere:draw-clear", ());

    let _ = win.set_position(PhysicalPosition::new(x, y));
    let _ = win.set_size(PhysicalSize::new(ow, oh));
    // Dok nije odabran alat sloj mora propuštati klikove — inače se ne može
    // raditi ništa drugo dok snimanje traje.
    let _ = win.set_ignore_cursor_events(true);
    let _ = win.show();
    Ok(())
}

/// Okvir snimanja preko CIJELOG monitora: zatamni sve oko snimanog područja i
/// nacrta rub oko njega, pa korisnik u svakom trenutku vidi što točno ulazi u video.
///
/// Prozor je izuzet iz capture-a i click-through, pa ne utječe ni na snimku ni na rad.
fn show_rec_frame(
    app: &AppHandle,
    target: &RecordTarget,
    crop: (u32, u32, u32, u32),
) -> Result<(), String> {
    let (ox, oy, ow, oh) = crop;
    let rect = RecFrameRect {
        x: ox,
        y: oy,
        width: ow,
        height: oh,
        full: target.region.is_none(),
    };
    *lock(&app.state::<AppState>().rec_frame) = Some(rect);

    ensure_rec_windows(app)?;
    let win = app
        .get_webview_window("recframe")
        .ok_or("prozor okvira nedostaje")?;
    let _ = win.set_position(PhysicalPosition::new(target.monitor_x, target.monitor_y));
    let _ = win.set_size(PhysicalSize::new(target.monitor_w, target.monitor_h));
    let _ = win.set_ignore_cursor_events(true);
    // Event ide PRIJE show-a: prvi prikaz već nosi točan okvir, bez bljeska praznog sloja.
    let _ = win.emit("snapvere:rec-frame", rect);
    let _ = win.show();
    Ok(())
}

/// Snimano područje za okvir — prozor ga povuče pri učitavanju ako je event već prošao.
#[tauri::command]
pub fn rec_frame_rect(state: State<AppState>) -> Option<RecFrameRect> {
    *lock(&state.rec_frame)
}

/// Propuštaj klikove kroz sloj za crtanje (true = sloj hvata miš).
#[tauri::command]
pub fn set_draw_interactive(app: AppHandle, interactive: bool) -> Result<(), String> {
    let Some(win) = app.get_webview_window("recdraw") else {
        return Ok(());
    };
    win.set_ignore_cursor_events(!interactive).map_err(estr)?;
    if interactive {
        let _ = win.set_focus();
    }
    // Fokus na sloj ga digne iznad HUD-a — vrati HUD na vrh.
    raise_hud(&app);
    Ok(())
}

/// Postavi redact područja koja Rust pikselizira u svakom frameu.
#[tauri::command]
pub fn set_blur_regions(app: AppHandle, rects: Vec<PixelRect>) -> Result<(), String> {
    record::set_blur_regions(&app, rects)
}

/// Sakrij UI snimanja (HUD + sloj za crtanje). Zove ga frontend kad snimanje završi.
#[tauri::command]
pub fn close_rec_hud(app: AppHandle) -> Result<(), String> {
    for label in ["recbar", "recdraw", "recframe"] {
        if let Some(win) = app.get_webview_window(label) {
            let _ = win.hide();
        }
    }
    *lock(&app.state::<AppState>().rec_frame) = None;
    // Sljedeće snimanje kreće s čistim slojem.
    if let Some(win) = app.get_webview_window("recdraw") {
        let _ = win.set_ignore_cursor_events(true);
    }
    Ok(())
}

// ──────────────────────────────── galerija ────────────────────────────────

#[tauri::command]
pub fn list_screenshots(
    app: AppHandle,
    state: State<AppState>,
    limit: Option<u32>,
    offset: Option<u32>,
) -> Result<Vec<Screenshot>, String> {
    let items = lock(&state.db)
        .list(limit.unwrap_or(200), offset.unwrap_or(0))
        .map_err(estr)?;
    allow_media_dirs(&app, &items);
    Ok(items)
}

#[tauri::command]
pub fn search_screenshots(
    app: AppHandle,
    state: State<AppState>,
    query: String,
    limit: Option<u32>,
    offset: Option<u32>,
) -> Result<Vec<Screenshot>, String> {
    let items = {
        let db = lock(&state.db);
        let limit = limit.unwrap_or(120).min(500);
        let offset = offset.unwrap_or(0);
        if query.trim().is_empty() {
            db.list(limit, offset).map_err(estr)?
        } else {
            db.search(query.trim(), limit, offset).map_err(estr)?
        }
    };
    allow_media_dirs(&app, &items);
    Ok(items)
}

/// Asset scope iz tauri.conf.json pokriva samo $HOME/$PICTURE/$APPDATA. Galerija
/// spremljena drugdje (npr. `D:\Snimke` iz Settingsa) inače bi u pregledniku
/// pokazala prazan okvir — pa dopustimo upravo mape u kojima su naši zapisi.
fn allow_media_dirs(app: &AppHandle, items: &[Screenshot]) {
    let scope = app.asset_protocol_scope();
    for shot in items {
        for path_str in std::iter::once(shot.file_path.as_str()).chain(shot.thumb_path.as_deref()) {
            let path = Path::new(path_str);
            if !scope.is_allowed(path) {
                if let Err(e) = scope.allow_file(path) {
                    log(&format!("asset scope {}: {e}", path.display()));
                }
            }
        }
    }
}

// ─────────────────────────────── preglednik ───────────────────────────────
// Komande primaju id zapisa, ne putanju: webview tako ne može otvoriti ni
// kopirati ništa što nije u galeriji.

fn media_path(app: &AppHandle, id: &str) -> Result<PathBuf, String> {
    let state = app.state::<AppState>();
    let shot = lock(&state.db)
        .get(id)
        .map_err(estr)?
        .ok_or("This item is no longer in the gallery")?;
    let path = PathBuf::from(shot.file_path);
    if !path.exists() {
        return Err("The file was moved or deleted".into());
    }
    Ok(path)
}

/// Otvori datoteku u zadanom programu sustava (Photos, Media Player…).
#[tauri::command]
pub fn open_media(app: AppHandle, id: String) -> Result<(), String> {
    let path = media_path(&app, &id)?;
    app.opener()
        .open_path(path.to_string_lossy(), None::<&str>)
        .map_err(estr)
}

/// Pokaži datoteku u Exploreru (označenu).
#[tauri::command]
pub fn reveal_media(app: AppHandle, id: String) -> Result<(), String> {
    let path = media_path(&app, &id)?;
    app.opener().reveal_item_in_dir(&path).map_err(estr)
}

const MAX_CLIPBOARD_FILE_BYTES: u64 = 200 * 1024 * 1024;

fn can_copy_image(file_bytes: u64, width: u32, height: u32) -> bool {
    file_bytes <= MAX_CLIPBOARD_FILE_BYTES
        && width > 0
        && height > 0
        && u64::from(width) * u64::from(height) <= MAX_EDITOR_PIXELS
}

fn validate_copy_image(path: &Path) -> Result<(), String> {
    let bytes = fs::metadata(path).map_err(estr)?.len();
    if bytes > MAX_CLIPBOARD_FILE_BYTES {
        return Err("Clipboard source exceeds safe file size (200 MB)".into());
    }
    let (width, height) = image::image_dimensions(path).map_err(estr)?;
    if !can_copy_image(bytes, width, height) {
        return Err("Clipboard image exceeds supported dimensions".into());
    }
    Ok(())
}

/// Kopiraj sliku iz galerije u clipboard. Dekodiranje velikog PNG-a traje — ne na
/// glavnoj dretvi.
#[tauri::command]
pub async fn copy_media_image(app: AppHandle, id: String) -> Result<(), String> {
    let path = media_path(&app, &id)?;
    tauri::async_runtime::spawn_blocking(move || {
        validate_copy_image(&path)?;
        let img = image::open(&path).map_err(estr)?.to_rgba8();
        copy_rgba_to_clipboard(&app, &img)
    })
    .await
    .map_err(|e| format!("copy task: {e}"))?
}

#[tauri::command]
pub fn delete_screenshot(state: State<AppState>, id: String) -> Result<(), String> {
    // Only delete the path from an existing gallery record, never an arbitrary webview argument.
    let shot = lock(&state.db)
        .get(&id)
        .map_err(estr)?
        .ok_or("This capture is no longer in the gallery")?;
    match fs::remove_file(&shot.file_path) {
        Ok(()) => {}
        Err(e) if e.kind() == std::io::ErrorKind::NotFound => {}
        Err(e) => return Err(format!("Could not delete capture file: {e}")),
    }
    lock(&state.db).delete(&id).map_err(estr)?;
    if let Some(thumb) = shot.thumb_path {
        if let Err(e) = fs::remove_file(&thumb) {
            if e.kind() != std::io::ErrorKind::NotFound {
                log(&format!("Could not delete thumbnail {}: {e}", thumb));
            }
        }
    }
    Ok(())
}

#[tauri::command]
pub fn add_tag(state: State<AppState>, id: String, tag: String) -> Result<(), String> {
    lock(&state.db).add_tag(&id, tag.trim()).map_err(estr)
}

/// Otvori glavni prozor / galeriju (tray, hotkey, single-instance).
/// Na zatvaranje (X) prozor se SAMO sakrije (vidi `on_window_event` u lib.rs),
/// pa ga ovdje obično samo prikažemo. Sigurnosna mreža: ako je "home" ipak
/// nestao (npr. stari build koji ga je uništavao na X), ponovo ga kreiramo —
/// galerija se tako UVIJEK može otvoriti.
#[tauri::command]
pub fn show_home(app: AppHandle) -> Result<(), String> {
    if let Some(win) = app.get_webview_window("home") {
        let _ = win.unminimize();
        let _ = win.show();
        let _ = win.set_focus();
        return Ok(());
    }
    WebviewWindowBuilder::new(&app, "home", WebviewUrl::App("index.html".into()))
        .title("Snapvere")
        .inner_size(1120.0, 740.0)
        .min_inner_size(880.0, 560.0)
        .center()
        .build()
        .map_err(estr)?;
    Ok(())
}

/// Otvori Settings prozor (tray i Home ga dijele).
pub fn open_settings(app: &AppHandle) -> Result<(), String> {
    if let Some(win) = app.get_webview_window("settings") {
        let _ = win.unminimize();
        let _ = win.show();
        let _ = win.set_focus();
        return Ok(());
    }
    WebviewWindowBuilder::new(
        app,
        "settings",
        WebviewUrl::App("index.html#settings".into()),
    )
    .title("Snapvere — Settings")
    .inner_size(880.0, 680.0)
    .center()
    .build()
    .map_err(estr)?;
    Ok(())
}

/// Putanja do log datoteke — Settings → About nudi "Open log".
#[tauri::command]
pub fn log_file_path() -> Option<String> {
    logging::log_path().map(|p| p.to_string_lossy().into_owned())
}

/** Open the configured capture directory, with no caller-controlled path. */
#[tauri::command]
pub fn open_capture_folder(app: AppHandle) -> Result<(), String> {
    let dir = gallery_dir(&app)?;
    fs::create_dir_all(&dir).map_err(estr)?;
    app.opener()
        .open_path(dir.to_string_lossy(), None::<&str>)
        .map_err(estr)
}

/** Open Snapvere's data directory, without arbitrary filesystem IPC access. */
#[tauri::command]
pub fn open_data_folder(app: AppHandle) -> Result<(), String> {
    let dir = app.path().app_data_dir().map_err(estr)?;
    fs::create_dir_all(&dir).map_err(estr)?;
    app.opener()
        .open_path(dir.to_string_lossy(), None::<&str>)
        .map_err(estr)
}

#[cfg(test)]
mod export_safety_tests {
    use super::*;

    fn header(width: u32, height: u32) -> Vec<u8> {
        let mut bytes = vec![0; 24];
        bytes[..8].copy_from_slice(b"\x89PNG\r\n\x1a\n");
        bytes[12..16].copy_from_slice(b"IHDR");
        bytes[16..20].copy_from_slice(&width.to_be_bytes());
        bytes[20..24].copy_from_slice(&height.to_be_bytes());
        bytes
    }

    #[test]
    fn clipboard_limits_guard_against_decode_memory_exhaustion() {
        assert!(can_copy_image(123_456, 3840, 2160));
        assert!(!can_copy_image(123_456, 0, 2160));
        assert!(!can_copy_image(123_456, 100_000, 100_000));
        assert!(!can_copy_image(MAX_CLIPBOARD_FILE_BYTES + 1, 100, 100));
    }

    #[test]
    fn reject_malformed_and_oversized_editor_png() {
        assert!(validate_editor_png_header(b"not a png").is_err());
        assert!(validate_editor_png_header(&header(0, 100)).is_err());
        assert!(validate_editor_png_header(&header(60_000, 60_000)).is_err());
        assert!(validate_editor_png_header(&header(3840, 2160)).is_ok());
        assert!(decode_editor_png("invalid base64").is_err());
    }

    #[test]
    fn save_as_requires_matching_extension_and_absolute_path() {
        let base = std::env::temp_dir();
        let jpg = base.join("capture.JPEG");
        let exe = base.join("capture.exe");
        assert!(validate_export_path(&jpg.to_string_lossy(), ImageFormat::Jpg).is_ok());
        assert!(validate_export_path(&jpg.to_string_lossy(), ImageFormat::Png).is_err());
        assert!(validate_export_path(&exe.to_string_lossy(), ImageFormat::Png).is_err());
        assert!(validate_export_path("capture.png", ImageFormat::Png).is_err());
    }

    #[test]
    fn exclusive_capture_write_never_overwrites_existing_media() {
        let path = std::env::temp_dir().join(format!("snapvere-test-{}.png", Uuid::new_v4()));
        write_new_capture(&path, b"original").expect("create new capture");
        assert!(write_new_capture(&path, b"replacement").is_err());
        assert_eq!(fs::read(&path).expect("read"), b"original");
        fs::remove_file(&path).expect("cleanup");
    }
}

#[cfg(test)]
mod frozen_capture_transport_tests {
    use super::frame_to_rgba_packet;
    use image::{Rgba, RgbaImage};

    #[test]
    fn rgba_packet_keeps_dimensions_and_pixel_colors() {
        let mut image = RgbaImage::new(2, 1);
        image.put_pixel(0, 0, Rgba([9, 191, 217, 255]));
        image.put_pixel(1, 0, Rgba([232, 42, 87, 255]));
        let packet = frame_to_rgba_packet(&image);
        assert_eq!(&packet[..4], &2u32.to_le_bytes());
        assert_eq!(&packet[4..8], &1u32.to_le_bytes());
        assert_eq!(&packet[8..], &[9, 191, 217, 255, 232, 42, 87, 255]);
        assert_eq!(packet.len(), 8 + 2 * 4);
    }

    #[test]
    fn rgba_packet_empty_image_has_only_header() {
        let packet = frame_to_rgba_packet(&RgbaImage::new(0, 0));
        assert_eq!(packet, vec![0u8; 8]);
    }
}
