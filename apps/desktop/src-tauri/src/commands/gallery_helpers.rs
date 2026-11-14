//! Scope gallery files strictly by registered id, with bounded image decode.
use super::*;

/// Asset scope iz tauri.conf.json pokriva samo $HOME/$PICTURE/$APPDATA. Galerija
/// spremljena drugdje (npr. `D:\Snimke` iz Settingsa) inače bi u pregledniku
/// pokazala prazan okvir — pa dopustimo upravo mape u kojima su naši zapisi.
pub(super) fn allow_media_dirs(app: &AppHandle, items: &[Screenshot]) {
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

pub(super) fn media_path(app: &AppHandle, id: &str) -> Result<PathBuf, String> {
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

const MAX_CLIPBOARD_FILE_BYTES: u64 = 200 * 1024 * 1024;

pub(super) fn can_copy_image(file_bytes: u64, width: u32, height: u32) -> bool {
    file_bytes <= MAX_CLIPBOARD_FILE_BYTES
        && width > 0
        && height > 0
        && u64::from(width) * u64::from(height) <= MAX_EDITOR_PIXELS
}

pub(super) fn validate_copy_image(path: &Path) -> Result<(), String> {
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
