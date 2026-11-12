//! Hvatanje ekrana i obrada slike.
//!
//! ⚠️ NAPOMENA o `xcap`: API se mijenja između verzija. Ako u tvojoj pinanoj verziji
//! getteri (`id()`, `width()`, …) vraćaju `XCapResult<T>` umjesto `T`, dodaj `?` na njih.
//! Logika i pipeline ostaju isti — ovo je jedina točka koju treba uskladiti s verzijom.

use std::io::Cursor;

use anyhow::{anyhow, Result};
use base64::Engine;
use image::codecs::png::{CompressionType, FilterType as PngFilterType, PngEncoder};
use image::{imageops::FilterType, DynamicImage, ImageEncoder, RgbaImage};
use xcap::Monitor;

use crate::models::{ImageFormat, MonitorInfo, Region};

fn monitor_info(m: &Monitor) -> Result<MonitorInfo> {
    Ok(MonitorInfo {
        id: m.id()?,
        name: m.name()?,
        x: m.x()?,
        y: m.y()?,
        width: m.width()?,
        height: m.height()?,
        scale: m.scale_factor()?,
        is_primary: m.is_primary()?,
    })
}

/// Popis svih monitora (bez hvatanja slike).
pub fn list_monitors() -> Result<Vec<MonitorInfo>> {
    let monitors = Monitor::all().map_err(|e| anyhow!("xcap monitors: {e}"))?;
    monitors.iter().map(monitor_info).collect()
}

/// Monitor koji sadrži točku (x,y) (poziciju miša). Fallback: primarni, pa prvi.
fn monitor_containing(x: i32, y: i32) -> Result<Monitor> {
    let monitors = Monitor::all().map_err(|e| anyhow!("xcap monitors: {e}"))?;
    monitors
        .iter()
        .find(|m| {
            let mx = m.x().unwrap_or(0);
            let my = m.y().unwrap_or(0);
            let mw = m.width().unwrap_or(0) as i32;
            let mh = m.height().unwrap_or(0) as i32;
            x >= mx && x < mx + mw && y >= my && y < my + mh
        })
        .or_else(|| monitors.iter().find(|m| m.is_primary().unwrap_or(false)))
        .or_else(|| monitors.first())
        .cloned()
        .ok_or_else(|| anyhow!("nema dostupnog monitora"))
}

/// Samo geometrija monitora pod mišem — bez hvatanja slike. Record-mode overlay
/// pokazuje živi desktop, pa mu screenshot ne treba (a to je ~300 ms uštede).
pub fn monitor_at(x: i32, y: i32) -> Result<MonitorInfo> {
    monitor_info(&monitor_containing(x, y)?)
}

/// Uhvati JEDAN monitor — onaj koji sadrži točku (x,y) (poziciju miša). Fallback: primarni, pa prvi.
/// Jedan monitor = jedan capture + jedan transfer = brzo i malo RAM-a, bez paralelnih dretvi.
pub fn capture_at(x: i32, y: i32) -> Result<(MonitorInfo, RgbaImage)> {
    let m = monitor_containing(x, y)?;
    let info = monitor_info(&m)?;
    let img = m.capture_image().map_err(|e| anyhow!("capture: {e}"))?;
    Ok((info, img))
}

/// Uhvati cijeli primarni ekran (Capture Full Screen).
pub fn capture_primary() -> Result<RgbaImage> {
    let monitors = Monitor::all().map_err(|e| anyhow!("xcap monitors: {e}"))?;
    let primary = monitors
        .into_iter()
        .find(|m| m.is_primary().unwrap_or(false))
        .ok_or_else(|| anyhow!("nema primarnog monitora"))?;
    primary
        .capture_image()
        .map_err(|e| anyhow!("capture primary: {e}"))
}

/// Izreži pravokutnik iz slike (clamp na granice slike).
pub fn crop(img: &RgbaImage, region: &Region) -> RgbaImage {
    let x = region.x.max(0) as u32;
    let y = region.y.max(0) as u32;
    let w = region.width.min(img.width().saturating_sub(x)).max(1);
    let h = region.height.min(img.height().saturating_sub(y)).max(1);
    image::imageops::crop_imm(img, x, y, w, h).to_image()
}

/// Encode slike u traženi format. `quality` 1..=100 (ignorira se za PNG).
pub fn encode(img: &RgbaImage, format: ImageFormat, quality: u8) -> Result<Vec<u8>> {
    match format {
        ImageFormat::Png => {
            let mut buf = Cursor::new(Vec::new());
            DynamicImage::ImageRgba8(img.clone()).write_to(&mut buf, image::ImageFormat::Png)?;
            Ok(buf.into_inner())
        }
        ImageFormat::Jpg => {
            let rgb = DynamicImage::ImageRgba8(img.clone()).to_rgb8();
            let mut buf = Cursor::new(Vec::new());
            let enc =
                image::codecs::jpeg::JpegEncoder::new_with_quality(&mut buf, quality.clamp(1, 100));
            enc.write_image(
                rgb.as_raw(),
                rgb.width(),
                rgb.height(),
                image::ExtendedColorType::Rgb8,
            )?;
            Ok(buf.into_inner())
        }
        ImageFormat::Webp => {
            // webp crate: lossy s zadanom kvalitetom.
            let encoder = webp::Encoder::from_rgba(img.as_raw(), img.width(), img.height());
            let mem = encoder.encode(quality.clamp(1, 100) as f32);
            Ok(mem.to_vec())
        }
    }
}

/// Smanji sliku zadržavajući omjer (za thumbnaile galerije).
pub fn thumbnail(img: &RgbaImage, max_dim: u32) -> RgbaImage {
    let (w, h) = (img.width(), img.height());
    if w <= max_dim && h <= max_dim {
        return img.clone();
    }
    let scale = max_dim as f32 / w.max(h) as f32;
    let nw = ((w as f32 * scale).round() as u32).max(1);
    let nh = ((h as f32 * scale).round() as u32).max(1);
    image::imageops::resize(img, nw, nh, FilterType::Triangle)
}

/// Brzi (lossless) PNG encode — minimalna kompresija, za frozen frame koji mora
/// nastati što prije. Pikseli su identični originalu (PNG je lossless), pa export ostaje točan.
pub fn encode_png_fast(img: &RgbaImage) -> Result<Vec<u8>> {
    let mut buf = Cursor::new(Vec::new());
    PngEncoder::new_with_quality(&mut buf, CompressionType::Fast, PngFilterType::NoFilter)
        .write_image(
            img.as_raw(),
            img.width(),
            img.height(),
            image::ExtendedColorType::Rgba8,
        )?;
    Ok(buf.into_inner())
}

/// PNG data-URL frozen frame-a (za prikaz u overlay web viewu). Data URL je CORS-clean
/// pa canvas getImageData (export, eyedropper/lupa HEX) radi bez taintanja.
pub fn to_png_data_url(img: &RgbaImage) -> Result<String> {
    let png = encode_png_fast(img)?;
    let b64 = base64::engine::general_purpose::STANDARD.encode(png);
    Ok(format!("data:image/png;base64,{b64}"))
}
