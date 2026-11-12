//! System tray ikona i quick menu.
//!
//! VAŽNO: tray se kreira ISKLJUČIVO ovdje (`TrayIconBuilder`). NE definiraj
//! `app.trayIcon` u `tauri.conf.json` — Tauri v2 ne spaja dvije registracije po
//! `id`-u, pa bi jedan proces nacrtao dvije identične ikone ("opet dva upaljena").

use std::sync::OnceLock;

use tauri::{
    menu::{Menu, MenuEvent, MenuItem, PredefinedMenuItem},
    tray::{MouseButton, MouseButtonState, TrayIcon, TrayIconBuilder, TrayIconEvent},
    AppHandle, Wry,
};

use crate::commands::{self, CaptureMode};
use crate::models::ImageFormat;
use crate::record;

/// Stavke kojima mijenjamo stanje dok snimanje traje.
struct RecordItems {
    start: MenuItem<Wry>,
    start_screen: MenuItem<Wry>,
    stop: MenuItem<Wry>,
}

static RECORD_ITEMS: OnceLock<RecordItems> = OnceLock::new();

pub fn build_tray(app: &AppHandle) -> tauri::Result<()> {
    let capture_region =
        MenuItem::with_id(app, "capture_region", "Capture Region", true, Some("PrintScreen"))?;
    let capture_full =
        MenuItem::with_id(app, "capture_full", "Capture Full Screen", true, None::<&str>)?;
    let sep1 = PredefinedMenuItem::separator(app)?;

    let record_region = MenuItem::with_id(
        app,
        "record_region",
        "Record Area…",
        true,
        Some("CmdOrCtrl+Shift+R"),
    )?;
    let record_screen =
        MenuItem::with_id(app, "record_screen", "Record This Screen", true, None::<&str>)?;
    let record_stop =
        MenuItem::with_id(app, "record_stop", "Stop Recording", false, None::<&str>)?;
    let sep2 = PredefinedMenuItem::separator(app)?;

    let open_gallery = MenuItem::with_id(app, "open_gallery", "Gallery", true, None::<&str>)?;
    let open_settings = MenuItem::with_id(app, "open_settings", "Settings", true, None::<&str>)?;
    let sep3 = PredefinedMenuItem::separator(app)?;
    let quit = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)?;

    let menu = Menu::with_items(
        app,
        &[
            &capture_region,
            &capture_full,
            &sep1,
            &record_region,
            &record_screen,
            &record_stop,
            &sep2,
            &open_gallery,
            &open_settings,
            &sep3,
            &quit,
        ],
    )?;

    let _ = RECORD_ITEMS.set(RecordItems {
        start: record_region,
        start_screen: record_screen,
        stop: record_stop,
    });

    let mut builder = TrayIconBuilder::with_id("snapvere-tray")
        .menu(&menu)
        .show_menu_on_left_click(false)
        .tooltip("Snapvere")
        .on_menu_event(on_menu_event)
        .on_tray_icon_event(on_tray_event);

    if let Some(icon) = app.default_window_icon() {
        builder = builder.icon(icon.clone());
    }

    builder.build(app)?;
    Ok(())
}

/// Prebaci tray u "snima se" stanje (poziva se iz recordera).
pub fn set_recording(app: &AppHandle, active: bool) {
    if let Some(items) = RECORD_ITEMS.get() {
        let _ = items.start.set_enabled(!active);
        let _ = items.start_screen.set_enabled(!active);
        let _ = items.stop.set_enabled(active);
    }
    if let Some(tray) = app.tray_by_id("snapvere-tray") {
        let _ = tray.set_tooltip(Some(if active {
            "Snapvere — snimanje u tijeku"
        } else {
            "Snapvere"
        }));
    }
}

fn on_menu_event(app: &AppHandle, event: MenuEvent) {
    match event.id().as_ref() {
        "capture_region" => commands::dispatch_region_capture(app, CaptureMode::Shot),
        "capture_full" => commands::dispatch_capture_fullscreen(app, ImageFormat::Png, 100),
        "record_region" => commands::dispatch_region_capture(app, CaptureMode::Record),
        "record_screen" => commands::dispatch_record_fullscreen(app),
        "record_stop" => {
            let _ = record::stop(app);
        }
        "open_gallery" => {
            let _ = commands::show_home(app.clone());
        }
        "open_settings" => {
            let _ = commands::open_settings(app);
        }
        "quit" => {
            // Snimanje u tijeku mora uredno zatvoriti datoteku prije izlaza.
            if record::is_active(app) {
                let _ = record::stop(app);
                std::thread::sleep(std::time::Duration::from_millis(600));
            }
            app.exit(0);
        }
        _ => {}
    }
}

fn on_tray_event(tray: &TrayIcon, event: TrayIconEvent) {
    if let TrayIconEvent::Click {
        button: MouseButton::Left,
        button_state: MouseButtonState::Up,
        ..
    } = event
    {
        let _ = commands::show_home(tray.app_handle().clone());
    }
}
