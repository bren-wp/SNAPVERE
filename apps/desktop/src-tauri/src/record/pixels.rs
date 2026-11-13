//! Pure capture rectangle, BGRA crop and redact helpers.
use super::*;

/// Izlazni pravokutnik unutar monitora, poravnat na parne dimenzije (zahtjev NV12/H.264).
pub(super) fn crop_rect(t: &RecordTarget) -> Result<(u32, u32, u32, u32), String> {
    let (mw, mh) = (t.monitor_w, t.monitor_h);
    if mw < 2 || mh < 2 {
        return Err("monitor prijavljuje nevaljanu veličinu".into());
    }
    let (x, y, w, h) = match t.region {
        Some(r) => {
            // Actual overlap: negative coordinates shrink the selected area
            // instead of shifting a too-large recording to the monitor edge.
            let left = i64::from(r.x).clamp(0, i64::from(mw));
            let top = i64::from(r.y).clamp(0, i64::from(mh));
            let right = (i64::from(r.x) + i64::from(r.width)).clamp(0, i64::from(mw));
            let bottom = (i64::from(r.y) + i64::from(r.height)).clamp(0, i64::from(mh));
            if right <= left || bottom <= top {
                return Err("recording area is outside the monitor".into());
            }
            (
                left as u32,
                top as u32,
                (right - left) as u32,
                (bottom - top) as u32,
            )
        }
        None => (0, 0, mw, mh),
    };
    let w = (w / 2) * 2;
    let h = (h / 2) * 2;
    if w < 16 || h < 16 {
        return Err("područje snimanja je premalo (minimalno 16x16)".into());
    }
    Ok((x, y, w, h))
}

/// Kopiraj pravokutnik iz punog framea u izlazni buffer (oba BGRA, gusto pakirana).
#[allow(clippy::too_many_arguments)]
pub(super) fn crop_bgra(src: &[u8], sw: u32, sh: u32, x: u32, y: u32, w: u32, h: u32, dst: &mut [u8]) {
    let sw = sw as usize;
    let (x, y, w, h) = (x as usize, y as usize, w as usize, h as usize);
    let row = w * 4;
    for r in 0..h {
        let sy = y + r;
        let d = r * row;
        if sy >= sh as usize {
            dst[d..d + row].fill(0);
            continue;
        }
        let s = (sy * sw + x) * 4;
        if s + row <= src.len() {
            dst[d..d + row].copy_from_slice(&src[s..s + row]);
        } else {
            dst[d..d + row].fill(0);
        }
    }
}

/// Pikselizira pravokutnik u BGRA bufferu (prosjek po bloku).
///
/// Veličina bloka raste s veličinom snimke da redact izgleda jednako na 720p i 4K.
pub(super) fn pixelate_bgra(buf: &mut [u8], w: u32, h: u32, r: &PixelRect) {
    let x0 = r.x.min(w) as usize;
    let y0 = r.y.min(h) as usize;
    let x1 = (r.x.saturating_add(r.width)).min(w) as usize;
    let y1 = (r.y.saturating_add(r.height)).min(h) as usize;
    if x1 <= x0 || y1 <= y0 {
        return;
    }
    // A partial frame from a driver must never panic in privacy redaction.
    let Some(expected) = (w as usize)
        .checked_mul(h as usize)
        .and_then(|pixels| pixels.checked_mul(4))
    else {
        return;
    };
    if buf.len() < expected {
        return;
    }
    let stride = w as usize * 4;
    let block = (w.min(h) / 60).clamp(6, 32) as usize;

    let mut by = y0;
    while by < y1 {
        let ymax = (by + block).min(y1);
        let mut bx = x0;
        while bx < x1 {
            let xmax = (bx + block).min(x1);
            let (mut b, mut g, mut rr, mut n) = (0u32, 0u32, 0u32, 0u32);
            for y in by..ymax {
                let row = y * stride;
                for x in bx..xmax {
                    let p = row + x * 4;
                    b += buf[p] as u32;
                    g += buf[p + 1] as u32;
                    rr += buf[p + 2] as u32;
                    n += 1;
                }
            }
            if let Some(b_avg) = b.checked_div(n) {
                let (b, g, rr) = (b_avg as u8, (g / n) as u8, (rr / n) as u8);
                for y in by..ymax {
                    let row = y * stride;
                    for x in bx..xmax {
                        let p = row + x * 4;
                        buf[p] = b;
                        buf[p + 1] = g;
                        buf[p + 2] = rr;
                    }
                }
            }
            bx = xmax;
        }
        by = ymax;
    }
}

pub(super) fn bgra_to_rgba_image(bgra: &[u8], w: u32, h: u32) -> image::RgbaImage {
    let mut out = vec![0u8; bgra.len()];
    let (destination_pixels, _) = out.as_chunks_mut::<4>();
    let (source_pixels, _) = bgra.as_chunks::<4>();
    for (d, s) in destination_pixels.iter_mut().zip(source_pixels.iter()) {
        d[0] = s[2];
        d[1] = s[1];
        d[2] = s[0];
        d[3] = 255;
    }
    image::RgbaImage::from_raw(w, h, out).unwrap_or_else(|| image::RgbaImage::new(1, 1))
}

#[cfg(test)]
mod regression_tests {
    use super::*;

    fn target(region: Region) -> RecordTarget {
        RecordTarget {
            monitor_id: 0, monitor_x: 0, monitor_y: 0,
            monitor_w: 100, monitor_h: 80, region: Some(region),
        }
    }

    #[test]
    fn clips_negative_selection_instead_of_expanding() {
        let t = target(Region { monitor_id: 0, x: -8, y: -6, width: 50, height: 46 });
        assert_eq!(crop_rect(&t).expect("overlapping selection"), (0, 0, 42, 40));
    }

    #[test]
    fn outside_selection_returns_error_without_panic() {
        let t = target(Region { monitor_id: 0, x: -100, y: -100, width: 20, height: 20 });
        assert!(crop_rect(&t).is_err());
    }

    #[test]
    fn truncated_frame_does_not_crash_pixelation() {
        let mut frame = vec![0u8; 4];
        pixelate_bgra(&mut frame, 1920, 1080, &PixelRect {
            x: 0, y: 0, width: 50, height: 50,
        });
        assert_eq!(frame.len(), 4);
    }
}
