//! Ucrtavanje pokazivača miša u snimku (Windows).
//!
//! DXGI Desktop Duplication ne uključuje kursor u frame — isporučuje ga odvojeno kao
//! "pointer shape". Umjesto parsiranja tog formata čitamo aktualni HCURSOR preko GDI-ja
//! i alpha-blendamo ga u frame. Bitmapa se kešira po HCURSOR handleu, pa je po frameu
//! ostane samo blend nekoliko stotina piksela.
//!
//! Sve je best-effort: ako bilo koji korak padne, frame ide bez kursora.

#![cfg(windows)]

use std::collections::HashMap;

use windows::Win32::Foundation::POINT;
use windows::Win32::Graphics::Gdi::{
    DeleteObject, GetDC, GetDIBits, GetObjectW, ReleaseDC, BITMAP, BITMAPINFO, BITMAPINFOHEADER,
    BI_RGB, DIB_RGB_COLORS, HBITMAP, HDC, HGDIOBJ,
};
use windows::Win32::UI::WindowsAndMessaging::{
    GetCursorInfo, GetIconInfo, CURSORINFO, CURSOR_SHOWING, HICON, ICONINFO,
};

/// Dekodirana slika kursora: premultiplied-free BGRA + hotspot.
#[derive(Clone)]
struct CursorBitmap {
    w: i32,
    h: i32,
    hot_x: i32,
    hot_y: i32,
    /// BGRA, top-down, gusto pakiran.
    px: Vec<u8>,
}

/// Keš dekodiranih kursora. Sustav reciklira HCURSOR handleove, pa je keš malen i stabilan.
#[derive(Default)]
pub struct CursorPainter {
    cache: HashMap<isize, Option<CursorBitmap>>,
}

impl CursorPainter {
    pub fn new() -> Self {
        Self::default()
    }

    /// Ucrtaj kursor u BGRA frame (`frame_w` x `frame_h`, gusto pakiran).
    ///
    /// `origin_x/y` su desktop koordinate gornjeg-lijevog ugla framea, pa se pozicija
    /// kursora s ekrana prevede u koordinate framea. Regija se obrezuje po rubovima.
    pub fn draw(
        &mut self,
        frame: &mut [u8],
        frame_w: u32,
        frame_h: u32,
        origin_x: i32,
        origin_y: i32,
    ) {
        let Some((bmp, pos)) = self.current() else { return };
        let fw = frame_w as i32;
        let fh = frame_h as i32;

        // Gornji-lijevi ugao slike kursora u koordinatama framea.
        let x0 = pos.x - origin_x - bmp.hot_x;
        let y0 = pos.y - origin_y - bmp.hot_y;

        for row in 0..bmp.h {
            let fy = y0 + row;
            if fy < 0 || fy >= fh {
                continue;
            }
            for col in 0..bmp.w {
                let fx = x0 + col;
                if fx < 0 || fx >= fw {
                    continue;
                }
                let s = ((row * bmp.w + col) * 4) as usize;
                let a = bmp.px[s + 3] as u32;
                if a == 0 {
                    continue;
                }
                let d = ((fy * fw + fx) * 4) as usize;
                if a == 255 {
                    frame[d..d + 3].copy_from_slice(&bmp.px[s..s + 3]);
                    continue;
                }
                // src-over blend po kanalu (BGRA, alpha nije premultiplicirana).
                for c in 0..3 {
                    let src = bmp.px[s + c] as u32;
                    let dst = frame[d + c] as u32;
                    frame[d + c] = ((src * a + dst * (255 - a)) / 255) as u8;
                }
            }
        }
    }

    /// Trenutni kursor (iz keša ili svježe dekodiran) + pozicija na ekranu.
    fn current(&mut self) -> Option<(CursorBitmap, POINT)> {
        let mut ci = CURSORINFO {
            cbSize: std::mem::size_of::<CURSORINFO>() as u32,
            ..Default::default()
        };
        unsafe { GetCursorInfo(&mut ci) }.ok()?;
        if ci.flags != CURSOR_SHOWING || ci.hCursor.is_invalid() {
            return None;
        }

        let key = ci.hCursor.0 as isize;
        if !self.cache.contains_key(&key) {
            let decoded = unsafe { decode_cursor(HICON(ci.hCursor.0)) };
            if decoded.is_none() {
                crate::logging::log("kursor: dekodiranje nije uspjelo, snimam bez pokazivača");
            }
            // Keširaj i neuspjeh — inače pokušavamo dekodirati svaki frame.
            self.cache.insert(key, decoded);
            if self.cache.len() > 64 {
                self.cache.clear();
            }
        }
        let bmp = self.cache.get(&key)?.clone()?;
        Some((bmp, ci.ptScreenPos))
    }
}

/// RAII za HDC koji smo posudili od sustava.
struct ScreenDc(HDC);
impl Drop for ScreenDc {
    fn drop(&mut self) {
        unsafe { ReleaseDC(None, self.0) };
    }
}

/// RAII za GDI objekt koji GetIconInfo vraća u vlasništvo pozivatelju.
struct OwnedBitmap(HBITMAP);
impl Drop for OwnedBitmap {
    fn drop(&mut self) {
        if !self.0.is_invalid() {
            let _ = unsafe { DeleteObject(HGDIOBJ(self.0 .0)) };
        }
    }
}

/// Pretvori HICON/HCURSOR u BGRA bitmapu.
///
/// Dva slučaja: kolor kursor (hbmColor, 32bpp s alfom) i monokromatski (samo hbmMask,
/// dvostruke visine — gornja polovica AND maska, donja XOR maska).
unsafe fn decode_cursor(icon: HICON) -> Option<CursorBitmap> {
    let mut ii = ICONINFO::default();
    GetIconInfo(icon, &mut ii).ok()?;
    let mask = OwnedBitmap(ii.hbmMask);
    let color = OwnedBitmap(ii.hbmColor);

    let dc = ScreenDc(GetDC(None));
    if dc.0.is_invalid() {
        return None;
    }

    let hot_x = ii.xHotspot as i32;
    let hot_y = ii.yHotspot as i32;

    if !color.0.is_invalid() {
        let (w, h) = bitmap_size(color.0)?;
        let mut px = dib_bgra(dc.0, color.0, w, h)?;

        // Neki kolor kursori imaju svugdje alpha=0 i oslanjaju se na AND masku.
        if px.chunks_exact(4).all(|p| p[3] == 0) {
            let and = mono_rows(dc.0, mask.0, w, h, 0, h)?;
            for (i, p) in px.chunks_exact_mut(4).enumerate() {
                p[3] = if and[i] { 0 } else { 255 };
            }
        }
        return Some(CursorBitmap { w, h, hot_x, hot_y, px });
    }

    // Monokromatski kursor: maska je 2x visine.
    let (w, full_h) = bitmap_size(mask.0)?;
    let h = full_h / 2;
    if h <= 0 {
        return None;
    }
    let and = mono_rows(dc.0, mask.0, w, full_h, 0, h)?;
    let xor = mono_rows(dc.0, mask.0, w, full_h, h, h)?;

    let mut px = vec![0u8; (w * h * 4) as usize];
    for i in 0..(w * h) as usize {
        let transparent = and[i];
        let set = xor[i];
        let o = i * 4;
        match (transparent, set) {
            // AND=1, XOR=0 → prozirno
            (true, false) => px[o + 3] = 0,
            // AND=1, XOR=1 → invertirano; nemamo pozadinu pa uzmimo bijelo (čitljivo na tamnom)
            (true, true) => {
                px[o] = 255;
                px[o + 1] = 255;
                px[o + 2] = 255;
                px[o + 3] = 255;
            }
            // AND=0 → neproziran piksel: XOR bira crno/bijelo
            (false, s) => {
                let v = if s { 255 } else { 0 };
                px[o] = v;
                px[o + 1] = v;
                px[o + 2] = v;
                px[o + 3] = 255;
            }
        }
    }
    Some(CursorBitmap { w, h, hot_x, hot_y, px })
}

fn bitmap_size(hbm: HBITMAP) -> Option<(i32, i32)> {
    let mut bm = BITMAP::default();
    let n = unsafe {
        GetObjectW(
            HGDIOBJ(hbm.0),
            std::mem::size_of::<BITMAP>() as i32,
            Some(&mut bm as *mut _ as *mut _),
        )
    };
    if n == 0 || bm.bmWidth <= 0 || bm.bmHeight <= 0 {
        return None;
    }
    Some((bm.bmWidth, bm.bmHeight))
}

fn header(w: i32, h: i32, bits: u16) -> BITMAPINFO {
    BITMAPINFO {
        bmiHeader: BITMAPINFOHEADER {
            biSize: std::mem::size_of::<BITMAPINFOHEADER>() as u32,
            biWidth: w,
            biHeight: -h, // negativno = top-down, isti smjer kao naši frameovi
            biPlanes: 1,
            biBitCount: bits,
            biCompression: BI_RGB.0,
            ..Default::default()
        },
        ..Default::default()
    }
}

/// Pročitaj 32bpp BGRA piksele bitmape.
unsafe fn dib_bgra(dc: HDC, hbm: HBITMAP, w: i32, h: i32) -> Option<Vec<u8>> {
    let mut bi = header(w, h, 32);
    let mut px = vec![0u8; (w * h * 4) as usize];
    let ok = GetDIBits(
        dc,
        hbm,
        0,
        h as u32,
        Some(px.as_mut_ptr() as *mut _),
        &mut bi,
        DIB_RGB_COLORS,
    );
    (ok != 0).then_some(px)
}

/// Pročitaj 1bpp masku visine `h_total` i vrati `rows` redaka počevši od `y_off`
/// kao booleane (true = bit postavljen). Kod monokromatskog kursora maska je dvostruke
/// visine: pojas na 0 je AND maska, pojas na `h` je XOR maska.
unsafe fn mono_rows(
    dc: HDC,
    hbm: HBITMAP,
    w: i32,
    h_total: i32,
    y_off: i32,
    rows: i32,
) -> Option<Vec<bool>> {
    // 1bpp DIB: redovi su poravnati na 4 bajta.
    let stride = (((w + 31) / 32) * 4) as usize;
    let mut bi = header(w, h_total, 1);
    let mut raw = vec![0u8; stride * h_total as usize];
    let ok = GetDIBits(
        dc,
        hbm,
        0,
        h_total as u32,
        Some(raw.as_mut_ptr() as *mut _),
        &mut bi,
        DIB_RGB_COLORS,
    );
    if ok == 0 {
        return None;
    }

    let rows = rows.min(h_total - y_off).max(0);
    let mut out = Vec::with_capacity((w * rows) as usize);
    for y in 0..rows {
        let row = &raw[(y + y_off) as usize * stride..];
        for x in 0..w as usize {
            let byte = row[x / 8];
            out.push((byte >> (7 - (x % 8))) & 1 == 1);
        }
    }
    Some(out)
}
