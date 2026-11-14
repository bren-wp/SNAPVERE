//! Native validation for editor images and Save As destinations.
use super::*;

/// Only one native file picker may be open across editor and overlay windows.
/// The guard is held by the background worker and released even on failure.
static SAVE_AS_BUSY: AtomicBool = AtomicBool::new(false);

pub(super) struct SaveAsGuard;

impl SaveAsGuard {
    pub(super) fn acquire() -> Result<Self, String> {
        SAVE_AS_BUSY
            .compare_exchange(false, true, Ordering::AcqRel, Ordering::Relaxed)
            .map(|_| Self)
            .map_err(|_| "Another Save As dialog is already open".to_string())
    }
}

impl Drop for SaveAsGuard {
    fn drop(&mut self) {
        SAVE_AS_BUSY.store(false, Ordering::Release);
    }
}

/// Custom IPC commands are available to multiple Tauri windows by default.
/// Exports are allowed only from the editor and screenshot-selection webviews.
pub(super) fn validate_export_origin(label: &str) -> Result<(), String> {
    if !matches!(label, "editor" | "overlay") {
        return Err("Save As is only available from the editor or screenshot overlay".into());
    }
    Ok(())
}

// Validate PNG canvas exports before allocating a full-size RGBA frame.
// Legitimate 8K desktop captures still fit; forged dimensions and overly large
// IPC messages cannot force unbounded decompression.
const MAX_EDITOR_BASE64_LEN: usize = 192 * 1024 * 1024;
pub(super) const MAX_EDITOR_PIXELS: u64 = 50_000_000;

pub(super) fn validate_editor_png_header(bytes: &[u8]) -> Result<(), String> {
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

pub(super) fn decode_editor_png(encoded: &str) -> Result<image::RgbaImage, String> {
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

/// The OS picker lets the user choose PNG, JPEG or WebP, independently
/// of the default format in Settings. Infer the actual encoder from the
/// selected extension, never from an untrusted IPC-supplied path.
pub(super) fn format_from_export_path(path: &Path) -> Result<ImageFormat, String> {
    let ext = path
        .extension()
        .and_then(|v| v.to_str())
        .ok_or("Save As requires a supported image extension")?;
    if ext.eq_ignore_ascii_case("png") {
        Ok(ImageFormat::Png)
    } else if ext.eq_ignore_ascii_case("jpg") || ext.eq_ignore_ascii_case("jpeg") {
        Ok(ImageFormat::Jpg)
    } else if ext.eq_ignore_ascii_case("webp") {
        Ok(ImageFormat::Webp)
    } else {
        Err("Save As supports PNG, JPEG and WebP only".into())
    }
}

pub(super) fn validate_export_path(path: &str, format: ImageFormat) -> Result<PathBuf, String> {
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

#[cfg(test)]
mod export_security_tests {
    use super::*;

    #[test]
    fn a_second_save_as_cannot_open_another_native_picker() {
        let first = SaveAsGuard::acquire().expect("first picker");
        assert!(SaveAsGuard::acquire().is_err());
        drop(first);
        assert!(SaveAsGuard::acquire().is_ok());
    }

    #[test]
    fn save_as_is_restricted_to_the_editor_window() {
        assert!(validate_export_origin("editor").is_ok());
        assert!(validate_export_origin("overlay").is_ok());
        for label in ["home", "settings", "overlay-evil", "recdraw", "recbar", "editor-evil", ""] {
            assert!(validate_export_origin(label).is_err(), "{label}");
        }
    }

    #[test]
    fn export_destination_requires_an_absolute_matching_image_extension() {
        let png = std::env::temp_dir().join("snapvere-export.png");
        let jpeg = std::env::temp_dir().join("snapvere-export.jpeg");
        let png_path = png.to_string_lossy();
        let jpeg_path = jpeg.to_string_lossy();

        assert!(validate_export_path(&png_path, ImageFormat::Png).is_ok());
        assert!(validate_export_path(&jpeg_path, ImageFormat::Jpg).is_ok());
        assert_eq!(format_from_export_path(&jpeg).unwrap(), ImageFormat::Jpg);
        assert_eq!(format_from_export_path(&png).unwrap(), ImageFormat::Png);
        assert!(format_from_export_path(&std::env::temp_dir().join("unsafe.exe")).is_err());
        assert!(validate_export_path(&png_path, ImageFormat::Webp).is_err());
        assert!(validate_export_path("relative-image.png", ImageFormat::Png).is_err());
        assert!(validate_export_path("relative-image.exe", ImageFormat::Png).is_err());
    }
}
