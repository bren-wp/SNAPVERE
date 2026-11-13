//! Native validation for editor images and Save As destinations.
use super::*;

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

