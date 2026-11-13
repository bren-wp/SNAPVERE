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
        log(&format!(
            "hotkey '{accel}' iz postavki nije dostupan — koristim default"
        ));
        None
    }
}

/// Registriraj korisnikove hotkeye iz postavki, a ako ih nema (ili su zauzeti) prve
/// slobodne defaulte. Vraća (capture, record) za log.
pub fn register_defaults(app: &AppHandle) -> (Option<String>, Option<String>) {
    let capture = stored_hotkey(app, "hotkey").or_else(|| {
        DEFAULT_HOTKEYS
            .iter()
            .find(|hk| is_free(app, hk))
            .map(|s| s.to_string())
    });
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

/// Only one registration sequence may touch the shared global shortcut registry.
/// Updates arrive from two different Settings controls and must be serialized.
static REBIND: Mutex<()> = Mutex::new(());

/// Register both handlers, or return the first error without claiming success.
fn register_pair(
    app: &AppHandle,
    capture: Option<&str>,
    record: Option<&str>,
) -> Result<(), String> {
    let gs = app.global_shortcut();
    if let Some(accel) = capture {
        gs.on_shortcut(accel, move |app, _shortcut, event| {
            if event.state() == ShortcutState::Pressed {
                commands::dispatch_region_capture(app, CaptureMode::Shot);
            }
        })
        .map_err(|e| format!("capture '{accel}': {e}"))?;
    }
    if let Some(accel) = record {
        gs.on_shortcut(accel, move |app, _shortcut, event| {
            if event.state() == ShortcutState::Pressed {
                commands::dispatch_quick_record(app);
            }
        })
        .map_err(|e| format!("record '{accel}': {e}"))?;
    }
    Ok(())
}

fn validate_pair(capture: Option<&str>, record: Option<&str>) -> Result<(), String> {
    if let (Some(a), Some(b)) = (capture, record) {
        if a.eq_ignore_ascii_case(b) {
            return Err("Capture and recording cannot share the same hotkey".into());
        }
    }
    Ok(())
}

/// Run with REBIND locked. A failed registration restores the previously
/// working bindings instead of leaving the user without any global hotkeys.
fn apply_unlocked(
    app: &AppHandle,
    capture: Option<String>,
    record: Option<String>,
) -> Result<(), String> {
    let capture = capture.filter(|v| !v.trim().is_empty());
    let record = record.filter(|v| !v.trim().is_empty());
    validate_pair(capture.as_deref(), record.as_deref())?;
    let previous = current();
    let gs = app.global_shortcut();
    gs.unregister_all()
        .map_err(|e| format!("Could not clear old shortcuts: {e}"))?;

    match register_pair(app, capture.as_deref(), record.as_deref()) {
        Ok(()) => {
            if let Ok(mut active) = ACTIVE.lock() {
                *active = (capture, record);
            }
            Ok(())
        }
        Err(error) => {
            // A partial new registration must not survive a failed rebind.
            let _ = gs.unregister_all();
            match register_pair(app, previous.0.as_deref(), previous.1.as_deref()) {
                Ok(()) => Err(format!("{error}. Previous shortcuts restored.")),
                Err(rollback) => {
                    let _ = gs.unregister_all();
                    if let Ok(mut active) = ACTIVE.lock() {
                        *active = (None, None);
                    }
                    Err(format!(
                        "{error}. Previous shortcuts could not be restored: {rollback}"
                    ))
                }
            }
        }
    }
}

/// (Re)register both shortcuts, preserving the previous set on failure.
pub fn apply(
    app: &AppHandle,
    capture: Option<String>,
    record: Option<String>,
) -> Result<(), String> {
    let _guard = REBIND
        .lock()
        .map_err(|_| "Shortcut registry is unavailable".to_string())?;
    apply_unlocked(app, capture, record)
}

fn current() -> (Option<String>, Option<String>) {
    ACTIVE.lock().map(|g| g.clone()).unwrap_or((None, None))
}

/// Update screenshot hotkey without racing a recording hotkey update.
#[tauri::command]
pub fn update_hotkey(app: AppHandle, accelerator: String) -> Result<(), String> {
    if accelerator.trim().is_empty() {
        return Err("Capture hotkey cannot be empty".into());
    }
    let _guard = REBIND
        .lock()
        .map_err(|_| "Shortcut registry is unavailable".to_string())?;
    let (_, record) = current();
    apply_unlocked(&app, Some(accelerator), record)
}

/// Update recording hotkey without racing a screenshot hotkey update.
#[tauri::command]
pub fn update_record_hotkey(app: AppHandle, accelerator: String) -> Result<(), String> {
    if accelerator.trim().is_empty() {
        return Err("Recording hotkey cannot be empty".into());
    }
    let _guard = REBIND
        .lock()
        .map_err(|_| "Shortcut registry is unavailable".to_string())?;
    let (capture, _) = current();
    apply_unlocked(&app, capture, Some(accelerator))
}

#[cfg(test)]
mod shortcut_validation_tests {
    use super::validate_pair;

    #[test]
    fn independent_capture_and_record_keys_are_allowed() {
        assert!(validate_pair(Some("PrintScreen"), Some("Ctrl+Shift+R")).is_ok());
    }

    #[test]
    fn the_same_shortcut_cannot_be_assigned_twice() {
        assert!(validate_pair(Some("Ctrl+Shift+R"), Some("ctrl+shift+r")).is_err());
    }

    #[test]
    fn absent_shortcut_does_not_conflict_with_registered_shortcut() {
        assert!(validate_pair(None, Some("Ctrl+Shift+R")).is_ok());
    }
}

/// Frontend command: trenutno aktivni hotkeyi (Settings ih prikazuje kao stvarno stanje).
#[tauri::command]
pub fn active_hotkeys() -> (Option<String>, Option<String>) {
    current()
}
