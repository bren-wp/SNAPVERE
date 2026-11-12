//! Globalno stanje aplikacije, dijeljeno preko Tauri `State`.

use std::collections::HashMap;
use std::sync::{Mutex, MutexGuard};

use image::RgbaImage;

use crate::db::Db;
use crate::models::RecFrameRect;
use crate::record::Session;

/// Zaključaj mutex i oporavi se od "poisoned" stanja.
///
/// Ovo je namjerno: ako je neka dretva paničarila držeći lock, standardni
/// `.lock().unwrap()` bi paničario i u SVAKOJ sljedećoj dretvi — jedan lokalni
/// problem trajno bi srušio aplikaciju. Podaci iza ovih lockova su cache i
/// handleovi koje uvijek možemo pregaziti, pa je nastavak rada ispravan izbor.
pub fn lock<T>(m: &Mutex<T>) -> MutexGuard<'_, T> {
    match m.lock() {
        Ok(g) => g,
        Err(poisoned) => {
            crate::logging::log("mutex je bio poisoned — nastavljam s oporavljenim stanjem");
            poisoned.into_inner()
        }
    }
}

/// Zamrznuti frameovi svih monitora u trenutku capture-a.
/// Drže se u memoriji samo dok je overlay otvoren, pa se oslobađaju.
#[derive(Default)]
pub struct FrozenFrames {
    pub frames: HashMap<u32, RgbaImage>,
}

impl FrozenFrames {
    pub fn clear(&mut self) {
        self.frames.clear();
    }
}

/// Aplikacijsko stanje registrirano u Tauri builderu.
pub struct AppState {
    /// Zamrznuti capture po monitor id-u.
    pub frozen: Mutex<FrozenFrames>,
    /// Lokalna SQLite baza (povijest, tagovi).
    pub db: Mutex<Db>,
    /// Aktivna sesija snimanja ekrana (None kad se ne snima).
    pub recorder: Mutex<Option<Session>>,
    /// Područje koje se trenutno snima (za okvir `recframe`); None izvan snimanja.
    /// Drži se i ovdje, ne samo u eventu, da prozor okvira može povući stanje ako se
    /// učita nakon što je snimanje već krenulo.
    pub rec_frame: Mutex<Option<RecFrameRect>>,
}

impl AppState {
    pub fn new(db: Db) -> Self {
        Self {
            frozen: Mutex::new(FrozenFrames::default()),
            db: Mutex::new(db),
            recorder: Mutex::new(None),
            rec_frame: Mutex::new(None),
        }
    }
}
