//! Logiranje i hvatanje panika.
//!
//! U release buildu (`windows_subsystem = "windows"`) nema konzole, pa se `println!`
//! gubi. Bez traga u datoteci "aplikacija se samo ugasi" je nedijagnosticirljivo.
//! Ovdje postavljamo:
//!   1. log datoteku u `app.path().app_data_dir()/logs/snapvere.log`,
//!   2. panic hook koji zapiše poruku + lokaciju prije nego dretva umre.

use std::fs::{self, OpenOptions};
use std::io::Write;
use std::path::PathBuf;
use std::sync::OnceLock;

static LOG_PATH: OnceLock<Option<PathBuf>> = OnceLock::new();

/// Maksimalna veličina log datoteke; preko toga se rotira u `.1`.
const MAX_LOG_BYTES: u64 = 512 * 1024;

/// Postavi putanju log datoteke (zove se jednom, iz `setup`).
pub fn init(app_data_dir: &std::path::Path) {
    let dir = app_data_dir.join("logs");
    let path = fs::create_dir_all(&dir)
        .ok()
        .map(|_| dir.join("snapvere.log"));

    // Rotacija — log ne smije rasti bez granice.
    if let Some(p) = &path {
        if fs::metadata(p)
            .map(|m| m.len() > MAX_LOG_BYTES)
            .unwrap_or(false)
        {
            let _ = fs::rename(p, p.with_extension("log.1"));
        }
    }

    let _ = LOG_PATH.set(path);
    log(&format!(
        "──────── Snapvere {} start ────────",
        env!("CARGO_PKG_VERSION")
    ));
}

/// Zapiši redak u log (i na stdout u debug buildu). Nikad ne paničari.
pub fn log(msg: &str) {
    #[cfg(debug_assertions)]
    println!("[snapvere] {msg}");

    let Some(Some(path)) = LOG_PATH.get() else {
        return;
    };
    let stamp = chrono::Local::now().format("%Y-%m-%d %H:%M:%S%.3f");
    if let Ok(mut f) = OpenOptions::new().create(true).append(true).open(path) {
        let _ = writeln!(f, "{stamp}  {msg}");
    }
}

/// Putanja log datoteke (za "Open log" u Settings → About).
pub fn log_path() -> Option<PathBuf> {
    LOG_PATH.get().and_then(|p| p.clone())
}

/// Instaliraj panic hook. Panic u radnoj dretvi (capture, snimanje) se sada
/// zapiše i — jer je `panic = "unwind"` — ne ruši cijeli proces.
pub fn install_panic_hook() {
    let previous = std::panic::take_hook();
    std::panic::set_hook(Box::new(move |info| {
        let location = info
            .location()
            .map(|l| format!("{}:{}", l.file(), l.line()))
            .unwrap_or_else(|| "<nepoznato>".into());
        let payload = info
            .payload()
            .downcast_ref::<&str>()
            .map(|s| (*s).to_string())
            .or_else(|| info.payload().downcast_ref::<String>().cloned())
            .unwrap_or_else(|| "<bez poruke>".into());
        let thread = std::thread::current()
            .name()
            .unwrap_or("<unnamed>")
            .to_string();

        log(&format!("PANIC [{thread}] @ {location}: {payload}"));
        previous(info);
    }));
}

/// Pokreni zatvaranje koje smije paničariti; panic postaje `Err` umjesto pada procesa.
/// Koristi se oko svakog poziva prema `xcap`/DXGI/`image` — to su C/FFI rubovi
/// gdje panic dolazi iz tuđeg koda i ne smije ubiti aplikaciju.
pub fn guard<T>(what: &str, f: impl FnOnce() -> Result<T, String>) -> Result<T, String> {
    match std::panic::catch_unwind(std::panic::AssertUnwindSafe(f)) {
        Ok(r) => {
            if let Err(e) = &r {
                log(&format!("{what}: greška — {e}"));
            }
            r
        }
        Err(_) => {
            let msg = format!("{what}: interna greška (panic uhvaćen, aplikacija nastavlja)");
            log(&msg);
            Err(msg)
        }
    }
}
