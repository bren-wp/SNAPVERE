//! Globalni hotkeys (registracija + rebind).
//!
//! Dvije akcije: snimka zaslona i snimanje ekrana. Plugin nema "ukloni jedan
//! shortcut" koji bi bio siguran uz rebind, pa pri svakoj promjeni odjednom
//! ponovno registriramo oba — stanje držimo ovdje.
//!
//! NAPOMENA o "Fn+…": Fn je hardverski modifikator na samoj tipkovnici. Windows
//! nikad ne vidi "Fn+R" — vidi samo tipku koju kombinacija proizvede (npr. Fn+P
//! šalje PrintScreen). Zato se hotkey za snimanje veže na pravu kombinaciju
//! (default Ctrl+Shift+R) koja se u Settingsima može promijeniti.

use std::sync::Mutex;

use tauri::AppHandle;
use tauri_plugin_global_shortcut::{GlobalShortcutExt, ShortcutState};

use crate::commands::{self, CaptureMode};
use crate::logging::log;

/// Preferirani hotkeyi za capture regije, redom. PrintScreen često zauzme Windows
/// Snipping Tool ili drugi screenshot alat — tada padamo na prvi slobodni fallback.
pub const DEFAULT_HOTKEYS: &[&str] = &[
    "PrintScreen",
    "CmdOrCtrl+Shift+1",
    "CmdOrCtrl+Shift+S",
    "Alt+Shift+S",
];

/// Preferirani hotkeyi za snimanje ekrana. `CmdOrCtrl+Shift+5` je namjerno u listi —
/// isti prst-memory kao macOS Cmd+Shift+5.
pub const DEFAULT_RECORD_HOTKEYS: &[&str] = &[
    "CmdOrCtrl+Shift+R",
    "CmdOrCtrl+Shift+5",
    "Alt+Shift+R",
    "CmdOrCtrl+Alt+R",
];

/// Trenutno aktivni akceleratori (capture, record). `None` = nijedan nije prošao.
static ACTIVE: Mutex<(Option<String>, Option<String>)> = Mutex::new((None, None));

/// Hotkey koji je korisnik spremio u Settings (store `settings.json`), ako postoji i
/// ako ga sustav dopušta. Bez ovoga bi se svaki restart vratio na default kombinaciju.
fn stored_hotkey(app: &AppHandle, key: &str) -> Option<String> {
    use tauri_plugin_store::StoreExt;
    let store = app.store("settings.json").ok()?;
    let accel = store.get(key)?.as_str()?.trim().to_string();
    if accel.is_empty() {
        return None;
    }
    if is_free(app, &accel) {
        Some(accel)
    } else {
        log(&format!("hotkey '{accel}' iz postavki nije dostupan — koristim default"));
        None
    }
}

/// Registriraj korisnikove hotkeye iz postavki, a ako ih nema (ili su zauzeti) prve
/// slobodne defaulte. Vraća (capture, record) za log.
pub fn register_defaults(app: &AppHandle) -> (Option<String>, Option<String>) {
    let capture = stored_hotkey(app, "hotkey")
        .or_else(|| DEFAULT_HOTKEYS.iter().find(|hk| is_free(app, hk)).map(|s| s.to_string()));
    let record = stored_hotkey(app, "recordHotkey")
        .filter(|hk| Some(hk) != capture.as_ref())
        .or_else(|| {
            DEFAULT_RECORD_HOTKEYS
                .iter()
                .find(|hk| Some(hk.to_string()) != capture && is_free(app, hk))
                .map(|s| s.to_string())
        });

    if let Err(e) = apply(app, capture.clone(), record.clone()) {
        log(&format!("hotkeys: {e}"));
    }
    (capture, record)
}

/// Probna registracija: je li kombinacija slobodna u sustavu.
fn is_free(app: &AppHandle, accel: &str) -> bool {
    let gs = app.global_shortcut();
    match gs.register(accel) {
        Ok(()) => {
            let _ = gs.unregister(accel);
            true
        }
        Err(_) => false,
    }
}

/// (Re)registriraj oba hotkeya. Prazan/None = ta akcija nema hotkey.
pub fn apply(
    app: &AppHandle,
    capture: Option<String>,
    record: Option<String>,
) -> Result<(), String> {
    let gs = app.global_shortcut();
    let _ = gs.unregister_all();

    let mut errors = Vec::new();

    if let Some(accel) = capture.as_deref().filter(|s| !s.trim().is_empty()) {
        let res = gs.on_shortcut(accel, move |app, _shortcut, event| {
            if event.state() == ShortcutState::Pressed {
                commands::dispatch_region_capture(app, CaptureMode::Shot);
            }
        });
        if let Err(e) = res {
            errors.push(format!("capture '{accel}': {e}"));
        }
    }

    if let Some(accel) = record.as_deref().filter(|s| !s.trim().is_empty()) {
        let res = gs.on_shortcut(accel, move |app, _shortcut, event| {
            if event.state() == ShortcutState::Pressed {
                // Isti hotkey pokreće i zaustavlja snimanje.
                commands::dispatch_quick_record(app);
            }
        });
        if let Err(e) = res {
            errors.push(format!("record '{accel}': {e}"));
        }
    }

    if let Ok(mut active) = ACTIVE.lock() {
        *active = (capture, record);
    }

    if errors.is_empty() {
        Ok(())
    } else {
        Err(errors.join("; "))
    }
}

fn current() -> (Option<String>, Option<String>) {
    ACTIVE.lock().map(|g| g.clone()).unwrap_or((None, None))
}

/// Frontend command: promijeni hotkey za snimku zaslona.
#[tauri::command]
pub fn update_hotkey(app: AppHandle, accelerator: String) -> Result<(), String> {
    let (_, record) = current();
    apply(&app, Some(accelerator), record)
}

/// Frontend command: promijeni hotkey za snimanje ekrana.
#[tauri::command]
pub fn update_record_hotkey(app: AppHandle, accelerator: String) -> Result<(), String> {
    let (capture, _) = current();
    apply(&app, capture, Some(accelerator))
}

/// Frontend command: trenutno aktivni hotkeyi (Settings ih prikazuje kao stvarno stanje).
#[tauri::command]
pub fn active_hotkeys() -> (Option<String>, Option<String>) {
    current()
}
