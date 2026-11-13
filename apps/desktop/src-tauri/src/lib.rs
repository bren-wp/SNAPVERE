//! Snapvere — desktop jezgra (Tauri v2).
//! Postavlja plugine, lokalnu bazu, tray i globalne hotkeye, te izlaže komande frontendu.

mod capture;
mod commands;
mod db;
mod logging;
mod models;
mod record;
mod shortcuts;
mod state;
mod tray;

use tauri::Manager;

use db::Db;
use logging::log;
use state::AppState;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    // Panic hook prije svega ostalog — inače prvi panic prođe bez traga.
    logging::install_panic_hook();

    // Neki WebView2/GPU driveri ruše borderless overlay webview (exit 0xC..., "Edge se pojavi").
    // Isključi GPU akceleraciju u WebView2 → stabilno na svim mašinama.
    // Diagnostic flags may expose WebView2's remote debugger. Only use them in
    // debug builds; production captures must never inherit such endpoints.
    let webview_args = if cfg!(debug_assertions) {
        match std::env::var("SNAPVERE_WEBVIEW_ARGS") {
            Ok(extra) if !extra.trim().is_empty() => format!("--disable-gpu {}", extra.trim()),
            _ => "--disable-gpu".to_owned(),
        }
    } else {
        "--disable-gpu".to_owned()
    };
    std::env::set_var("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", webview_args);

    let app = tauri::Builder::default()
        // ── single-instance MORA biti prvi: druga instanca se ugasi (samo jedna tray ikona) ──
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            // pokušaj pokretanja druge instance → pokaži galeriju prve, druga se gasi
            let _ = commands::show_home(app.clone());
        }))
        // ── ostali plugini ───────────────────────────────────────
        .plugin(tauri_plugin_os::init())
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_clipboard_manager::init())
        .plugin(tauri_plugin_store::Builder::default().build())
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .plugin(tauri_plugin_autostart::init(
            tauri_plugin_autostart::MacosLauncher::LaunchAgent,
            None::<Vec<&str>>,
        ))
        // ── setup: log, baza, tray, hotkeyi ──────────────────────
        .setup(|app| {
            let data_dir = app.path().app_data_dir()?;
            std::fs::create_dir_all(&data_dir)?;
            logging::init(&data_dir);

            // Baza koja se ne da otvoriti (zaključana datoteka, oštećen WAL) NE smije
            // srušiti pokretanje — capture i snimanje rade i bez povijesti.
            let db_path = data_dir.join("snapvere.db");
            let db = match Db::open(&db_path) {
                Ok(db) => db,
                Err(e) => {
                    log(&format!(
                        "baza {}: {e} — prelazim na in-memory",
                        db_path.display()
                    ));
                    Db::open_in_memory().map_err(|e| e.to_string())?
                }
            };
            app.manage(AppState::new(db));

            // Privacy-friendly default: do not register Windows startup automatically.
            // Users can opt in from Settings, and the saved preference is respected.
            sync_autostart(app.handle());

            if let Err(e) = tray::build_tray(app.handle()) {
                log(&format!("tray: {e}"));
            }

            let (capture_hk, record_hk) = shortcuts::register_defaults(app.handle());
            log(&format!(
                "hotkeyi — capture: {}, snimanje: {}",
                capture_hk.as_deref().unwrap_or("<nijedan slobodan>"),
                record_hk.as_deref().unwrap_or("<nijedan slobodan>")
            ));

            // Pred-kreiraj (skrivene) overlay, HUD i sloj za crtanje da capture i
            // snimanje budu instant — stvaranje WebView2 prozora traje ~0.5 s po komadu.
            if let Err(e) = commands::ensure_overlay(app.handle()) {
                log(&format!("pred-kreiranje overlaya: {e}"));
            }
            if let Err(e) = commands::ensure_rec_windows(app.handle()) {
                log(&format!("pred-kreiranje prozora snimanja: {e}"));
            }
            record::warm_up();

            Ok(())
        })
        // ── komande dostupne frontendu ───────────────────────────
        .invoke_handler(tauri::generate_handler![
            commands::list_monitors,
            commands::begin_region_capture,
            commands::reveal_overlay,
            commands::get_frozen_frame,
            commands::get_frozen_rgba,
            commands::finish_region_capture,
            commands::cancel_capture,
            commands::capture_fullscreen,
            commands::save_edited_image,
            commands::save_image_as,
            commands::copy_base64_to_clipboard,
            commands::start_recording,
            commands::stop_recording,
            commands::cancel_recording,
            commands::pause_recording,
            commands::recording_status,
            commands::set_draw_interactive,
            commands::set_blur_regions,
            commands::close_rec_hud,
            commands::rec_frame_rect,
            commands::list_screenshots,
            commands::search_screenshots,
            commands::delete_screenshot,
            commands::add_tag,
            commands::open_media,
            commands::reveal_media,
            commands::copy_media_image,
            commands::copy_media_path,
            commands::show_home,
            commands::log_file_path,
            commands::open_capture_folder,
            commands::open_data_folder,
            shortcuts::update_hotkey,
            shortcuts::update_record_hotkey,
            shortcuts::active_hotkeys,
        ])
        // ── window lifecycle: X na glavnom prozoru SAKRIJE (tray app), ne uništi ──
        // Bez ovoga Tauri uništi "home" pri zatvaranju, pa ga show_home (tray klik,
        // hotkey, single-instance) više ne može vratiti. Pravi izlaz = tray "Quit".
        .on_window_event(|window, event| {
            // Klik na sloj za crtanje ga aktivira i digne iznad HUD-a → HUD odmah natrag na vrh,
            // inače "Stop" ispod njega postane nedostupan (i svaki klik crta).
            if let tauri::WindowEvent::Focused(true) = event {
                if window.label() == "recdraw" {
                    commands::raise_hud(window.app_handle());
                }
            }
            if let tauri::WindowEvent::CloseRequested { api, .. } = event {
                match window.label() {
                    "home" => {
                        let _ = window.hide();
                        api.prevent_close();
                    }
                    // UI snimanja se ne zatvara ručno — X bi ostavio snimanje bez kontrola.
                    "recbar" | "recdraw" | "recframe" => {
                        api.prevent_close();
                    }
                    _ => {}
                }
            }
        })
        .build(tauri::generate_context!());

    match app {
        Ok(app) => app.run(|app_handle, event| {
            if let tauri::RunEvent::ExitRequested { api, code, .. } = event {
                // `code: None` = zatvoren zadnji prozor. Snapvere je tray aplikacija i
                // mora preživjeti — bez ovoga se proces ugasi i hotkey prestane raditi.
                if code.is_none() {
                    api.prevent_exit();
                    return;
                }
                // Programski izlaz (tray → Quit): pusti snimatelja da zatvori datoteku.
                if record::is_active(app_handle) {
                    let _ = record::stop(app_handle);
                    // H.264/GIF finalization and SQLite registration may exceed
                    // 600 ms on slow disks: wait for the worker, not a fixed guess.
                    let _ = record::wait_for_finalize(
                        app_handle,
                        std::time::Duration::from_secs(12),
                    );
                }
                log("izlaz iz aplikacije");
            }
        }),
        Err(e) => {
            log(&format!("FATALNO: aplikacija se nije pokrenula — {e}"));
            eprintln!("[snapvere] greška pri pokretanju aplikacije: {e}");
        }
    }
}

/// Uskladi Run unos (HKCU Run / LaunchAgent) sa željom korisnika. Zove se pri svakom
/// pokretanju: `enable()` upisuje putanju TRENUTNOG exe-a, pa unos prati aplikaciju i
/// nakon updatea/preseljenja. Dev build se preskače da ne upiše target/debug u registar.
fn sync_autostart(app: &tauri::AppHandle) {
    use tauri_plugin_autostart::ManagerExt;
    use tauri_plugin_store::StoreExt;

    if cfg!(debug_assertions) {
        return;
    }
    let wants = app
        .store("settings.json")
        .ok()
        .and_then(|s| s.get("autostart"))
        .and_then(|v| v.as_bool())
        .unwrap_or(false);

    let al = app.autolaunch();
    let result = if wants {
        al.enable()
    } else if al.is_enabled().unwrap_or(false) {
        al.disable()
    } else {
        Ok(())
    };
    match result {
        Ok(()) => log(&format!(
            "autostart: {}",
            if wants { "uključen" } else { "isključen" }
        )),
        Err(e) => log(&format!("autostart: {e}")),
    }
}
