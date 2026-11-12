//! Animirani GIF izlaz.
//!
//! Cross-platform put (radi i kad Media Foundation nije dostupan) i namjerno ponuđen
//! izbor: GIF se lijepi izravno u Slack/GitHub/Jira bez playera. Cijena je veličina,
//! pa frameove skaliramo na razumnu širinu i kvantiziramo na 256 boja po frameu.

use std::fs::File;
use std::io::{BufWriter, Write};
use std::path::Path;

use gif::{DisposalMethod, Encoder, Frame, Repeat};

use crate::logging::log;

/// Najveća širina GIF-a; iznad ovoga datoteka postaje neupotrebljivo velika.
const MAX_WIDTH: u32 = 960;

pub struct GifEncoder {
    /// `Option` da ga `finish()` može konzumirati: `Encoder::into_inner` zapisuje
    /// trailer i vraća writer, pa grešku zatvaranja vidimo umjesto da je Drop proguta.
    enc: Option<Encoder<BufWriter<File>>>,
    /// Izlazne dimenzije (mogu biti manje od ulaznih zbog skaliranja).
    out_w: u32,
    out_h: u32,
    src_w: u32,
    src_h: u32,
    delay_cs: u16,
    /// Reupotrebljivi RGBA buffer za kvantizaciju.
    rgba: Vec<u8>,
}

impl GifEncoder {
    pub fn new(path: &Path, w: u32, h: u32, fps: u32) -> Result<Self, String> {
        let (out_w, out_h) = fit(w, h);
        let file = File::create(path).map_err(|e| format!("GIF create: {e}"))?;
        let mut enc = Encoder::new(BufWriter::new(file), out_w as u16, out_h as u16, &[])
            .map_err(|e| format!("GIF encoder: {e}"))?;
        enc.set_repeat(Repeat::Infinite)
            .map_err(|e| format!("GIF repeat: {e}"))?;

        // GIF kašnjenje je u stotinkama sekunde; ispod 2 (50 fps) preglednici throttlaju.
        let delay_cs = ((100.0 / fps.max(1) as f64).round() as u16).max(2);

        log(&format!(
            "GIF enkoder: {w}x{h} → {out_w}x{out_h}, delay {delay_cs}cs → {}",
            path.display()
        ));

        Ok(Self {
            enc: Some(enc),
            out_w,
            out_h,
            src_w: w,
            src_h: h,
            delay_cs,
            rgba: vec![0u8; (out_w as usize) * (out_h as usize) * 4],
        })
    }

    /// Dodaj jedan BGRA frame.
    pub fn write(&mut self, bgra: &[u8]) -> Result<(), String> {
        self.fill_rgba(bgra);
        // Razdvojene posudbe polja — `from_rgba_speed` traži &mut rgba dok držimo &mut enc.
        let Self { enc, out_w, out_h, delay_cs, rgba, .. } = self;
        let Some(enc) = enc.as_mut() else {
            return Err("GIF enkoder je već zatvoren".into());
        };
        // speed 10 = razuman kompromis kvalitete i brzine NeuQuant kvantizacije.
        let mut frame = Frame::from_rgba_speed(*out_w as u16, *out_h as u16, rgba, 10);
        frame.delay = *delay_cs;
        frame.dispose = DisposalMethod::Any;
        enc.write_frame(&frame)
            .map_err(|e| format!("GIF write_frame: {e}"))
    }

    /// Zapiši GIF trailer i flushaj buffer. Bez toga zadnji kB ostane u BufWriteru.
    pub fn finish(&mut self) -> Result<(), String> {
        let Some(enc) = self.enc.take() else {
            return Ok(());
        };
        let mut w = enc.into_inner().map_err(|e| format!("GIF trailer: {e}"))?;
        w.flush().map_err(|e| format!("GIF flush: {e}"))
    }

    /// BGRA (izvorna rezolucija) → RGBA (izlazna rezolucija), nearest-neighbour.
    fn fill_rgba(&mut self, bgra: &[u8]) {
        let sw = self.src_w as usize;
        let sh = self.src_h as usize;
        let ow = self.out_w as usize;
        let oh = self.out_h as usize;

        if ow == sw && oh == sh {
            for (d, s) in self.rgba.chunks_exact_mut(4).zip(bgra.chunks_exact(4)) {
                d[0] = s[2];
                d[1] = s[1];
                d[2] = s[0];
                d[3] = 255;
            }
            return;
        }

        for y in 0..oh {
            let sy = y * sh / oh;
            let srow = sy * sw * 4;
            let drow = y * ow * 4;
            for x in 0..ow {
                let sx = x * sw / ow;
                let s = srow + sx * 4;
                let d = drow + x * 4;
                self.rgba[d] = bgra[s + 2];
                self.rgba[d + 1] = bgra[s + 1];
                self.rgba[d + 2] = bgra[s];
                self.rgba[d + 3] = 255;
            }
        }
    }
}

/// Skaliraj na najviše `MAX_WIDTH` uz očuvanje omjera (parne dimenzije radi urednosti).
fn fit(w: u32, h: u32) -> (u32, u32) {
    if w <= MAX_WIDTH || w == 0 {
        return (w.max(1), h.max(1));
    }
    let nh = ((h as u64 * MAX_WIDTH as u64) / w as u64).max(1) as u32;
    (MAX_WIDTH, nh)
}
