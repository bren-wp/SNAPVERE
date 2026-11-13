//! Testovi za lanac snimanja.
//!
//! Čisti testovi (pretvorbe boja, geometrija) rade svugdje. Testovi koji traže
//! stvarni desktop (DXGI + Media Foundation) su `#[ignore]` — pokreni ih ručno:
//!
//! ```text
//! cargo test --release -- --ignored --nocapture
//! ```

use super::*;

#[test]
fn crop_rect_poravnava_na_parne_dimenzije() {
    let t = RecordTarget {
        monitor_id: 1,
        monitor_x: 0,
        monitor_y: 0,
        monitor_w: 1920,
        monitor_h: 1080,
        region: Some(Region {
            monitor_id: 1,
            x: 11,
            y: 7,
            width: 301,
            height: 199,
        }),
    };
    let (x, y, w, h) = crop_rect(&t).expect("regija je valjana");
    assert_eq!((x, y), (11, 7));
    assert_eq!(w % 2, 0, "H.264/NV12 traže parnu širinu");
    assert_eq!(h % 2, 0, "H.264/NV12 traže parnu visinu");
    assert_eq!((w, h), (300, 198));
}

#[test]
fn crop_rect_obrezuje_regiju_izvan_monitora() {
    let t = RecordTarget {
        monitor_id: 1,
        monitor_x: 0,
        monitor_y: 0,
        monitor_w: 800,
        monitor_h: 600,
        region: Some(Region {
            monitor_id: 1,
            x: 700,
            y: 500,
            width: 999,
            height: 999,
        }),
    };
    let (x, y, w, h) = crop_rect(&t).expect("regija se obrezuje, ne odbija");
    assert_eq!((x, y), (700, 500));
    assert_eq!((w, h), (100, 100));
}

#[test]
fn crop_rect_odbija_premalu_regiju() {
    let t = RecordTarget {
        monitor_id: 1,
        monitor_x: 0,
        monitor_y: 0,
        monitor_w: 1920,
        monitor_h: 1080,
        region: Some(Region {
            monitor_id: 1,
            x: 0,
            y: 0,
            width: 8,
            height: 8,
        }),
    };
    assert!(crop_rect(&t).is_err());
}

#[test]
fn crop_rect_bez_regije_uzima_cijeli_monitor() {
    let t = RecordTarget {
        monitor_id: 1,
        monitor_x: -1920,
        monitor_y: 0,
        monitor_w: 1366,
        monitor_h: 768,
        region: None,
    };
    assert_eq!(crop_rect(&t).unwrap(), (0, 0, 1366, 768));
}

#[test]
fn crop_bgra_kopira_ispravan_pravokutnik() {
    // 4x4 izvor, svaki piksel nosi svoj indeks u B kanalu.
    let (sw, sh) = (4u32, 4u32);
    let mut src = vec![0u8; (sw * sh * 4) as usize];
    for i in 0..(sw * sh) as usize {
        src[i * 4] = i as u8;
        src[i * 4 + 3] = 255;
    }
    let mut dst = vec![0u8; 2 * 2 * 4];
    crop_bgra(&src, sw, sh, 1, 1, 2, 2, &mut dst);

    // Očekujemo piksele (1,1), (2,1), (1,2), (2,2) → indeksi 5, 6, 9, 10.
    assert_eq!(dst[0], 5);
    assert_eq!(dst[4], 6);
    assert_eq!(dst[8], 9);
    assert_eq!(dst[12], 10);
}

#[test]
fn crop_bgra_izvan_granica_daje_crno_umjesto_panica() {
    let (sw, sh) = (4u32, 4u32);
    let src = vec![200u8; (sw * sh * 4) as usize];
    let mut dst = vec![7u8; 2 * 4 * 4];
    // Traži 2x4 blok koji počinje na y=3 → zadnja 3 retka su izvan izvora.
    crop_bgra(&src, sw, sh, 0, 3, 2, 4, &mut dst);
    assert_eq!(&dst[0..8], &[200u8; 8], "prvi redak postoji");
    assert!(
        dst[8..].iter().all(|&b| b == 0),
        "retci izvan izvora su crni"
    );
}

#[test]
fn pixelate_izravnava_blokove_i_ne_dira_okolinu() {
    // 32x32 BGRA, svaki piksel drugačiji (B = x, G = y) — nakon redacta unutar
    // pravokutnika svi pikseli jednog bloka moraju biti identični.
    let (w, h) = (32u32, 32u32);
    let mut buf = vec![255u8; (w * h * 4) as usize];
    for y in 0..h as usize {
        for x in 0..w as usize {
            let p = (y * w as usize + x) * 4;
            buf[p] = x as u8;
            buf[p + 1] = y as u8;
            buf[p + 2] = 7;
        }
    }
    let before = buf.clone();

    let rect = PixelRect {
        x: 8,
        y: 8,
        width: 16,
        height: 16,
    };
    pixelate_bgra(&mut buf, w, h, &rect);

    let px = |b: &[u8], x: usize, y: usize| {
        let p = (y * w as usize + x) * 4;
        (b[p], b[p + 1], b[p + 2])
    };

    // Izvan pravokutnika ništa se nije promijenilo.
    assert_eq!(px(&buf, 0, 0), px(&before, 0, 0));
    assert_eq!(px(&buf, 31, 31), px(&before, 31, 31));
    assert_eq!(px(&buf, 7, 8), px(&before, 7, 8));

    // Unutar pravokutnika susjedni pikseli istog bloka su izjednačeni…
    assert_eq!(px(&buf, 8, 8), px(&buf, 9, 9));
    // …i sadržaj je stvarno promijenjen (nije samo prekopiran).
    assert_ne!(px(&buf, 8, 8), px(&before, 8, 8));
}

#[test]
fn pixelate_ignorira_pravokutnik_izvan_slike() {
    let (w, h) = (8u32, 8u32);
    let mut buf = vec![3u8; (w * h * 4) as usize];
    let before = buf.clone();
    // Potpuno izvan granica i nulte širine — oboje mora proći bez panica.
    pixelate_bgra(
        &mut buf,
        w,
        h,
        &PixelRect {
            x: 99,
            y: 99,
            width: 10,
            height: 10,
        },
    );
    pixelate_bgra(
        &mut buf,
        w,
        h,
        &PixelRect {
            x: 2,
            y: 2,
            width: 0,
            height: 4,
        },
    );
    assert_eq!(buf, before);
}

/// GIF put ne treba ni desktop ni Media Foundation — pokriva ga obični test.
#[test]
fn gif_enkoder_daje_valjanu_animaciju() {
    let path = std::env::temp_dir().join("snapvere-gif-test.gif");
    let _ = std::fs::remove_file(&path);

    let (w, h) = (64u32, 48u32);
    {
        let mut enc = gifenc::GifEncoder::new(&path, w, h, 10).expect("GIF enkoder");
        let mut frame = vec![0u8; (w * h * 4) as usize];
        for i in 0..6 {
            // Svaki frame druga boja — kvantizator tako mora proizvesti različit sadržaj.
            for p in frame.chunks_exact_mut(4) {
                p[0] = (i * 40) as u8; // B
                p[1] = 80;
                p[2] = (255 - i * 40) as u8; // R
                p[3] = 255;
            }
            enc.write(&frame).expect("GIF frame");
        }
        enc.finish().expect("GIF finish");
    }

    let bytes = std::fs::read(&path).expect("GIF datoteka");
    assert_eq!(&bytes[0..6], b"GIF89a", "nedostaje GIF89a zaglavlje");
    assert_eq!(*bytes.last().unwrap(), 0x3B, "nedostaje GIF trailer (0x3B)");
    assert!(
        bytes.len() > 300,
        "GIF je sumnjivo malen: {} B",
        bytes.len()
    );

    let _ = std::fs::remove_file(&path);
}

/// Skaliranje mora čuvati omjer i nikad ne povećavati mali izlaz.
#[test]
fn gif_skalira_samo_prevelike_frameove() {
    let mut small = gifenc::GifEncoder::new(
        &std::env::temp_dir().join("snapvere-gif-small.gif"),
        800,
        600,
        10,
    )
    .expect("GIF enkoder");
    small.finish().ok();
    let _ = std::fs::remove_file(std::env::temp_dir().join("snapvere-gif-small.gif"));

    let path = std::env::temp_dir().join("snapvere-gif-big.gif");
    let mut big = gifenc::GifEncoder::new(&path, 2560, 1440, 10).expect("GIF enkoder");
    let frame = vec![120u8; (2560 * 1440 * 4) as usize];
    big.write(&frame).expect("GIF frame");
    big.finish().expect("GIF finish");

    // 2560x1440 → 960x540 (omjer 16:9 očuvan).
    let bytes = std::fs::read(&path).expect("GIF datoteka");
    let width = u16::from_le_bytes([bytes[6], bytes[7]]);
    let height = u16::from_le_bytes([bytes[8], bytes[9]]);
    assert_eq!((width, height), (960, 540));
    let _ = std::fs::remove_file(&path);
}

#[cfg(windows)]
mod windows_only {
    use super::*;
    use std::time::{Duration, Instant};

    /// BT.601 studio: puna bijela → Y 235, neutralna kroma.
    #[test]
    fn bgra_to_nv12_bijela_i_crna() {
        let (w, h) = (2u32, 2u32);
        let mut out = vec![0u8; (w * h * 3 / 2) as usize];

        let white = vec![255u8; (w * h * 4) as usize];
        super::super::mp4::bgra_to_nv12_for_test(&white, w, h, &mut out);
        assert!(
            (out[0] as i32 - 235).abs() <= 1,
            "bijela Y ≈ 235, dobiveno {}",
            out[0]
        );
        assert!((out[4] as i32 - 128).abs() <= 1, "bijela U ≈ 128");
        assert!((out[5] as i32 - 128).abs() <= 1, "bijela V ≈ 128");

        let mut black = vec![0u8; (w * h * 4) as usize];
        for p in black.chunks_exact_mut(4) {
            p[3] = 255;
        }
        super::super::mp4::bgra_to_nv12_for_test(&black, w, h, &mut out);
        assert!(
            (out[0] as i32 - 16).abs() <= 1,
            "crna Y ≈ 16, dobiveno {}",
            out[0]
        );
    }

    /// Čista crvena mora dati visok V i nizak U — hvata zamjenu B/R kanala.
    #[test]
    fn bgra_to_nv12_crvena_ne_zamjenjuje_kanale() {
        let (w, h) = (2u32, 2u32);
        let mut out = vec![0u8; (w * h * 3 / 2) as usize];
        let mut red = vec![0u8; (w * h * 4) as usize];
        for p in red.chunks_exact_mut(4) {
            p[2] = 255; // R u BGRA
            p[3] = 255;
        }
        super::super::mp4::bgra_to_nv12_for_test(&red, w, h, &mut out);
        let (u, v) = (out[4] as i32, out[5] as i32);
        assert!(v > 200, "crvena mora imati visok V, dobiveno {v}");
        assert!(u < 110, "crvena mora imati nizak U, dobiveno {u}");
    }

    /// Sporа, očito točna referenca — paralelna verzija mora dati bajt-za-bajt isto.
    fn reference_nv12(bgra: &[u8], w: usize, h: usize) -> Vec<u8> {
        let mut out = vec![0u8; w * h * 3 / 2];
        for y in 0..h {
            for x in 0..w {
                let p = (y * w + x) * 4;
                let (b, g, r) = (bgra[p] as i32, bgra[p + 1] as i32, bgra[p + 2] as i32);
                out[y * w + x] = (((66 * r + 129 * g + 25 * b + 128) >> 8) + 16) as u8;
            }
        }
        for by in (0..h).step_by(2) {
            for bx in (0..w).step_by(2) {
                let (mut sb, mut sg, mut sr) = (0, 0, 0);
                for (dy, dx) in [(0, 0), (0, 1), (1, 0), (1, 1)] {
                    let p = ((by + dy) * w + bx + dx) * 4;
                    sb += bgra[p] as i32;
                    sg += bgra[p + 1] as i32;
                    sr += bgra[p + 2] as i32;
                }
                let (b, g, r) = (sb / 4, sg / 4, sr / 4);
                let o = w * h + (by / 2) * w + bx;
                out[o] = (((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128).clamp(0, 255) as u8;
                out[o + 1] = (((112 * r - 94 * g - 18 * b + 128) >> 8) + 128).clamp(0, 255) as u8;
            }
        }
        out
    }

    /// Velika slika ide paralelnim putem (trake po dretvama) — rezultat mora biti
    /// identičan serijskoj referenci, uključujući zadnju traku koja nije puna.
    #[test]
    fn bgra_to_nv12_paralelno_jednako_serijskom() {
        // 640x362: > 100k piksela (paralelni put), visina nije djeljiva brojem dretvi.
        let (w, h) = (640usize, 362usize);
        let mut bgra = vec![0u8; w * h * 4];
        let mut seed = 0x1234_5678u32;
        for px in bgra.chunks_exact_mut(4) {
            seed = seed.wrapping_mul(1_664_525).wrapping_add(1_013_904_223);
            px[0] = (seed >> 24) as u8;
            px[1] = (seed >> 16) as u8;
            px[2] = (seed >> 8) as u8;
            px[3] = 255;
        }
        let mut out = vec![0u8; w * h * 3 / 2];
        super::super::mp4::bgra_to_nv12_for_test(&bgra, w as u32, h as u32, &mut out);
        assert_eq!(out, reference_nv12(&bgra, w, h));
    }

    fn primary() -> (i32, i32, u32, u32) {
        let monitors = xcap::Monitor::all().expect("xcap monitori");
        let m = monitors
            .iter()
            .find(|m| m.is_primary().unwrap_or(false))
            .or_else(|| monitors.first())
            .expect("bar jedan monitor");
        (
            m.x().unwrap(),
            m.y().unwrap(),
            m.width().unwrap(),
            m.height().unwrap(),
        )
    }

    /// Puni lanac: DXGI duplikacija → crop → kursor → H.264 → MP4 na disku.
    #[test]
    #[ignore = "treba stvarni desktop; pokreni s: cargo test --release -- --ignored"]
    fn dxgi_u_mp4_daje_reproducibilnu_datoteku() {
        let (mx, my, mw, mh) = primary();
        println!("primarni monitor: {mw}x{mh} @ ({mx},{my})");

        let _mf = super::super::mp4::MfSession::start().expect("Media Foundation se pokrenuo");
        let mut grabber =
            super::super::dxgi::DxgiGrabber::new(mx, my).expect("DXGI duplikacija otvorena");
        let (gw, gh) = (grabber.width, grabber.height);

        // Snimamo gornju lijevu četvrtinu — time se testira i crop put.
        let ow = ((mw / 2) / 2) * 2;
        let oh = ((mh / 2) / 2) * 2;
        let fps = 20u32;

        let path = std::env::temp_dir().join("snapvere-test.mp4");
        let _ = std::fs::remove_file(&path);
        let mut enc =
            super::super::mp4::Mp4Encoder::new(&path, ow, oh, fps, 80, None).expect("MP4 enkoder");
        let mut painter = super::super::cursor::CursorPainter::new();

        let mut frame = vec![0u8; (ow as usize) * (oh as usize) * 4];
        let interval = Duration::from_millis((1000 / fps) as u64);
        let started = Instant::now();
        let frames = 40; // ~2 sekunde

        for i in 0..frames {
            let full = grabber.grab(30).expect("grab");
            crop_bgra(full, gw, gh, 0, 0, ow, oh, &mut frame);
            painter.draw(&mut frame, ow, oh, mx, my);
            let t = (i as i64) * (interval.as_nanos() / 100) as i64;
            enc.write(&frame, t, (interval.as_nanos() / 100) as i64)
                .expect("write frame");
            std::thread::sleep(interval / 2);
        }
        enc.finish().expect("finalize");
        drop(enc);

        let meta = std::fs::metadata(&path).expect("datoteka postoji");
        println!(
            "snimljeno {frames} frameova {ow}x{oh} u {:?}, {} KB → {}",
            started.elapsed(),
            meta.len() / 1024,
            path.display()
        );
        assert!(
            meta.len() > 8 * 1024,
            "MP4 je sumnjivo malen: {} B",
            meta.len()
        );

        // MP4 mora početi ftyp boxom, inače ga playeri neće otvoriti.
        let head = std::fs::read(&path).expect("čitanje");
        assert_eq!(
            &head[4..8],
            b"ftyp",
            "nema ftyp boxa — kontejner nije zatvoren"
        );
        assert!(
            head.windows(4).any(|w| w == b"moov"),
            "nema moov boxa — Finalize() nije prošao"
        );

        // Za ručnu vizualnu provjeru: SNAPVERE_KEEP_TEST_MP4=1 ostavi datoteku na disku.
        if std::env::var_os("SNAPVERE_KEEP_TEST_MP4").is_none() {
            let _ = std::fs::remove_file(&path);
        }
    }

    /// Kursor se mora dekodirati (ili uredno odustati) bez panica i bez izlaska iz buffera.
    #[test]
    #[ignore = "treba stvarni desktop; pokreni s: cargo test --release -- --ignored"]
    fn kursor_se_crta_unutar_granica() {
        let mut painter = super::super::cursor::CursorPainter::new();
        let (w, h) = (64u32, 64u32);
        let mut frame = vec![0u8; (w * h * 4) as usize];
        // Origin namjerno pomaknut tako da kursor pada djelomično izvan framea.
        for origin in [(0, 0), (-30, -30), (10_000, 10_000)] {
            painter.draw(&mut frame, w, h, origin.0, origin.1);
        }
        assert_eq!(frame.len(), (w * h * 4) as usize);
    }
}
