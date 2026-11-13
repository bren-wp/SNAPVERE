//! Snimanje ekrana — hvatanje frameova, enkodiranje i spremanje u galeriju.
//!
//! Sve se odvija na JEDNOJ radnoj dretvi ("snapvere-recorder"): D3D uređaj i Media
//! Foundation sink writer vezani su uz dretvu koja ih je stvorila, a jedna dretva
//! ujedno znači da nema kanala ni zaključavanja po frameu.
//!
//! Glavna dretva vidi samo atomike u `Ctl` (faza, brojači) i dvije zastavice
//! (stop/cancel), pa UI nikad ne čeka enkoder.

#[cfg(windows)]
mod audio;
#[cfg(windows)]
mod cursor;
#[cfg(windows)]
mod dxgi;
#[cfg(windows)]
mod mp4;

mod gifenc;

#[cfg(test)]
mod tests;

use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicBool, AtomicU64, AtomicU8, Ordering};
use std::sync::{Arc, Mutex};
use std::thread::JoinHandle;
use std::time::{Duration, Instant};

use tauri::{AppHandle, Emitter, Manager};

use crate::logging::log;
use crate::models::{
    CaptureSource, PixelRect, RecordFormat, RecordOptions, RecordPhase, RecordStatus, Region,
    Screenshot,
};
use crate::state::AppState;

const PHASE_IDLE: u8 = 0;
const PHASE_RECORDING: u8 = 1;
const PHASE_PAUSED: u8 = 2;
const PHASE_FINISHING: u8 = 3;

/// Što snimamo: monitor u fizičkim koordinatama + opcionalna regija unutar njega.
#[derive(Debug, Clone, Copy)]
pub struct RecordTarget {
    pub monitor_id: u32,
    pub monitor_x: i32,
    pub monitor_y: i32,
    pub monitor_w: u32,
    pub monitor_h: u32,
    pub region: Option<Region>,
}

/// Dijeljeno stanje između UI dretve i snimatelja. Samo atomici — bez zaključavanja.
pub struct Ctl {
    phase: AtomicU8,
    stop: AtomicBool,
    cancel: AtomicBool,
    elapsed_ms: AtomicU64,
    frames: AtomicU64,
    size_bytes: AtomicU64,
    width: u32,
    height: u32,
    format: RecordFormat,
    /// Područja koja se pikseliziraju u svakom frameu (koordinate snimke).
    /// Jedini ne-atomik ovdje; lock je nenapadnut i drži se mikrosekundu po frameu.
    blur: Mutex<Vec<PixelRect>>,
    /// Trenutak pokretanja — hotkey ne smije zaustaviti tek pokrenuto snimanje.
    started: Instant,
    /// Što se od zvuka STVARNO snima (postavlja recorder nakon otvaranja izvora).
    mic: AtomicBool,
    system_audio: AtomicBool,
}

impl Ctl {
    fn status(&self) -> RecordStatus {
        RecordStatus {
            phase: match self.phase.load(Ordering::Relaxed) {
                PHASE_RECORDING => RecordPhase::Recording,
                PHASE_PAUSED => RecordPhase::Paused,
                PHASE_FINISHING => RecordPhase::Finishing,
                _ => RecordPhase::Idle,
            },
            elapsed_ms: self.elapsed_ms.load(Ordering::Relaxed),
            frames: self.frames.load(Ordering::Relaxed),
            format: self.format,
            width: self.width,
            height: self.height,
            size_bytes: self.size_bytes.load(Ordering::Relaxed),
            mic: self.mic.load(Ordering::Relaxed),
            system_audio: self.system_audio.load(Ordering::Relaxed),
        }
    }
}

pub struct Session {
    pub ctl: Arc<Ctl>,
    #[allow(dead_code)] // drži se samo da dretva ne ostane "orphan" u ps listi
    join: Option<JoinHandle<()>>,
}

// ────────────────────────────── javno API ──────────────────────────────

/// Zagrij D3D11/DXGI i Media Foundation na pozadinskoj dretvi pri startu.
///
/// Prvi `D3D11CreateDevice` u procesu učitava driver DLL-ove i traje 300-500 ms —
/// bez ovoga bi svako PRVO snimanje kasnilo pola sekunde prije nego HUD išta pokaže.
/// Otvorena duplikacija se odmah pušta; ne držimo ništa trajno.
pub fn warm_up() {
    let _ = std::thread::Builder::new()
        .name("snapvere-warmup".into())
        .spawn(|| {
            let t = Instant::now();
            #[cfg(windows)]
            {
                if let Ok(monitors) = xcap::Monitor::all() {
                    let primary = monitors
                        .iter()
                        .find(|m| m.is_primary().unwrap_or(false))
                        .or_else(|| monitors.first());
                    if let Some(m) = primary {
                        let (x, y) = (m.x().unwrap_or(0), m.y().unwrap_or(0));
                        drop(dxgi::DxgiGrabber::new(x, y));
                    }
                }
                drop(mp4::MfSession::start());
            }
            log(&format!("zagrijavanje D3D/MF gotovo za {:?}", t.elapsed()));
        });
}

/// Je li snimanje u tijeku (ili na pauzi).
pub fn is_active(app: &AppHandle) -> bool {
    let state = app.state::<AppState>();
    let guard = crate::state::lock(&state.recorder);
    guard
        .as_ref()
        .map(|s| s.ctl.phase.load(Ordering::Relaxed) != PHASE_IDLE)
        .unwrap_or(false)
}

/// Koliko dugo aktivno snimanje traje (None kad se ne snima).
pub fn age(app: &AppHandle) -> Option<Duration> {
    let state = app.state::<AppState>();
    let guard = crate::state::lock(&state.recorder);
    guard
        .as_ref()
        .filter(|s| s.ctl.phase.load(Ordering::Relaxed) != PHASE_IDLE)
        .map(|s| s.ctl.started.elapsed())
}

pub fn status(app: &AppHandle) -> RecordStatus {
    let state = app.state::<AppState>();
    let guard = crate::state::lock(&state.recorder);
    guard
        .as_ref()
        .map(|s| s.ctl.status())
        .unwrap_or_else(RecordStatus::idle)
}

/// Pokreni snimanje. Vraća početni status (dimenzije, format) ili grešku ako
/// se izvor/enkoder nije dao otvoriti — greška stiže PRIJE nego HUD ode gore.
pub fn start(
    app: &AppHandle,
    target: RecordTarget,
    opts: RecordOptions,
) -> Result<RecordStatus, String> {
    if is_active(app) {
        return Err("Snimanje je već u tijeku".into());
    }

    let fps = opts.fps.clamp(5, 60);
    // Izlazne dimenzije moraju biti parne (NV12 uzorkuje kromu 2x2).
    let (ox, oy, ow, oh) = crop_rect(&target)?;

    let ctl = Arc::new(Ctl {
        phase: AtomicU8::new(PHASE_RECORDING),
        stop: AtomicBool::new(false),
        cancel: AtomicBool::new(false),
        elapsed_ms: AtomicU64::new(0),
        frames: AtomicU64::new(0),
        size_bytes: AtomicU64::new(0),
        width: ow,
        height: oh,
        format: opts.format,
        blur: Mutex::new(Vec::new()),
        started: Instant::now(),
        mic: AtomicBool::new(false),
        system_audio: AtomicBool::new(false),
    });

    let out_path = output_path(app, opts.format)?;

    // Handshake: enkoder i grabber se otvaraju NA radnoj dretvi (D3D/MF su thread-affine),
    // pa grešku otvaranja moramo dočekati ovdje da je korisnik vidi odmah.
    let (ready_tx, ready_rx) = std::sync::mpsc::channel::<Result<(), String>>();

    let thread_ctl = ctl.clone();
    let thread_app = app.clone();
    let thread_path = out_path.clone();
    let join = std::thread::Builder::new()
        .name("snapvere-recorder".into())
        .spawn(move || {
            let result = std::panic::catch_unwind(std::panic::AssertUnwindSafe(|| {
                run(
                    &thread_app,
                    &thread_ctl,
                    &thread_path,
                    target,
                    opts,
                    fps,
                    (ox, oy, ow, oh),
                    ready_tx,
                )
            }));
            let outcome = match result {
                Ok(r) => r,
                Err(_) => Err("snimanje: interna greška (panic uhvaćen)".to_string()),
            };
            finish_session(&thread_app, &thread_ctl, &thread_path, outcome);
        })
        .map_err(|e| format!("ne mogu pokrenuti dretvu snimanja: {e}"))?;

    // Otvaranje DXGI-ja + MF-a je ispod ~300 ms; timeout je samo zaštita od zaglavljivanja.
    match ready_rx.recv_timeout(Duration::from_secs(10)) {
        Ok(Ok(())) => {}
        Ok(Err(e)) => {
            ctl.phase.store(PHASE_IDLE, Ordering::SeqCst);
            return Err(e);
        }
        Err(_) => {
            ctl.cancel.store(true, Ordering::SeqCst);
            return Err("snimanje se nije pokrenulo na vrijeme".into());
        }
    }

    let state = app.state::<AppState>();
    *crate::state::lock(&state.recorder) = Some(Session {
        ctl: ctl.clone(),
        join: Some(join),
    });

    crate::tray::set_recording(app, true);
    let st = ctl.status();
    let _ = app.emit("snapvere:rec-state", &st);
    Ok(st)
}

/// Zaustavi snimanje i spremi datoteku. Vraća odmah — enkoder zatvara datoteku u
/// pozadini i javlja se preko `snapvere:rec-done`.
pub fn stop(app: &AppHandle) -> Result<(), String> {
    with_ctl(app, |ctl| {
        ctl.stop.store(true, Ordering::SeqCst);
        ctl.phase.store(PHASE_FINISHING, Ordering::SeqCst);
    })
}

/// Prekini snimanje i baci datoteku.
pub fn cancel(app: &AppHandle) -> Result<(), String> {
    with_ctl(app, |ctl| {
        ctl.cancel.store(true, Ordering::SeqCst);
        ctl.stop.store(true, Ordering::SeqCst);
        ctl.phase.store(PHASE_FINISHING, Ordering::SeqCst);
    })
}

/// Zamijeni skup redact pravokutnika. Prazan popis = ništa se ne pikselizira.
pub fn set_blur_regions(app: &AppHandle, rects: Vec<PixelRect>) -> Result<(), String> {
    with_ctl(app, |ctl| {
        *crate::state::lock(&ctl.blur) = rects;
    })
}

pub fn set_paused(app: &AppHandle, paused: bool) -> Result<RecordStatus, String> {
    with_ctl(app, |ctl| {
        let current = ctl.phase.load(Ordering::SeqCst);
        if paused && current == PHASE_RECORDING {
            ctl.phase.store(PHASE_PAUSED, Ordering::SeqCst);
            log(&format!(
                "snimanje: pauza na {} ms ({} frameova)",
                ctl.elapsed_ms.load(Ordering::Relaxed),
                ctl.frames.load(Ordering::Relaxed)
            ));
        } else if !paused && current == PHASE_PAUSED {
            ctl.phase.store(PHASE_RECORDING, Ordering::SeqCst);
            log("snimanje: nastavak nakon pauze");
        } else {
            log(&format!(
                "snimanje: zahtjev pauza={paused} ignoriran (faza {current})"
            ));
        }
    })?;
    let st = status(app);
    let _ = app.emit("snapvere:rec-state", &st);
    Ok(st)
}

fn with_ctl(app: &AppHandle, f: impl FnOnce(&Ctl)) -> Result<(), String> {
    let state = app.state::<AppState>();
    let guard = crate::state::lock(&state.recorder);
    let Some(session) = guard.as_ref() else {
        return Err("nema aktivnog snimanja".into());
    };
    f(&session.ctl);
    Ok(())
}

// ───────────────────────────── radna dretva ─────────────────────────────

/// Izvor frameova. BGRA, gusto pakiran, dimenzije cijelog monitora.
enum Source {
    #[cfg(windows)]
    Dxgi(Box<dxgi::DxgiGrabber>),
    /// Fallback: xcap BitBlt petlja (sporije, ali radi kad duplikacija nije dostupna —
    /// npr. RDP sesija ili zaključan output).
    Xcap {
        monitor: xcap::Monitor,
        buf: Vec<u8>,
        w: u32,
        h: u32,
    },
}

impl Source {
    fn open(target: &RecordTarget) -> Result<Self, String> {
        #[cfg(windows)]
        {
            match dxgi::DxgiGrabber::new(target.monitor_x, target.monitor_y) {
                Ok(g) => return Ok(Source::Dxgi(Box::new(g))),
                Err(e) => log(&format!("DXGI nedostupan ({e}); prelazim na xcap fallback")),
            }
        }

        let monitors = xcap::Monitor::all().map_err(|e| format!("xcap monitors: {e}"))?;
        let monitor = monitors
            .into_iter()
            .find(|m| m.id().map(|id| id == target.monitor_id).unwrap_or(false))
            .ok_or("monitor za snimanje nije pronađen")?;
        Ok(Source::Xcap {
            monitor,
            buf: vec![0u8; (target.monitor_w as usize) * (target.monitor_h as usize) * 4],
            w: target.monitor_w,
            h: target.monitor_h,
        })
    }

    fn grab(&mut self, timeout_ms: u32) -> Result<&[u8], String> {
        match self {
            #[cfg(windows)]
            Source::Dxgi(g) => g.grab(timeout_ms),
            Source::Xcap { monitor, buf, w, h } => {
                let img = monitor
                    .capture_image()
                    .map_err(|e| format!("xcap capture: {e}"))?;
                let iw = img.width().min(*w) as usize;
                let ih = img.height().min(*h) as usize;
                let src = img.as_raw();
                let sw = img.width() as usize;
                let dw = *w as usize;
                // RGBA → BGRA
                for y in 0..ih {
                    for x in 0..iw {
                        let s = (y * sw + x) * 4;
                        let d = (y * dw + x) * 4;
                        buf[d] = src[s + 2];
                        buf[d + 1] = src[s + 1];
                        buf[d + 2] = src[s];
                        buf[d + 3] = 255;
                    }
                }
                Ok(buf)
            }
        }
    }

    fn size(&self) -> (u32, u32) {
        match self {
            #[cfg(windows)]
            Source::Dxgi(g) => (g.width, g.height),
            Source::Xcap { w, h, .. } => (*w, *h),
        }
    }

    /// Ima li izvor stvarnu sliku (DXGI na statičnom ekranu prvo neko vrijeme nema).
    fn has_frame(&self) -> bool {
        match self {
            #[cfg(windows)]
            Source::Dxgi(g) => g.has_frame(),
            Source::Xcap { .. } => true,
        }
    }
}

/// Enkoder — MP4 na Windowsu, GIF svugdje.
enum Encoder {
    #[cfg(windows)]
    Mp4(Box<mp4::Mp4Encoder>),
    Gif(Box<gifenc::GifEncoder>),
}

impl Encoder {
    fn write(&mut self, bgra: &[u8], time_100ns: i64, dur_100ns: i64) -> Result<(), String> {
        match self {
            #[cfg(windows)]
            Encoder::Mp4(e) => e.write(bgra, time_100ns, dur_100ns),
            Encoder::Gif(e) => e.write(bgra),
        }
    }

    fn write_audio(&mut self, pcm: &[i16], time_100ns: i64, dur_100ns: i64) -> Result<(), String> {
        match self {
            #[cfg(windows)]
            Encoder::Mp4(e) => e.write_audio(pcm, time_100ns, dur_100ns),
            Encoder::Gif(_) => Ok(()),
        }
    }

    fn finish(&mut self) -> Result<(), String> {
        match self {
            #[cfg(windows)]
            Encoder::Mp4(e) => e.finish(),
            Encoder::Gif(e) => e.finish(),
        }
    }
}

/// Rezultat snimanja koji ide u `finish_session`.
struct Recorded {
    cancelled: bool,
    width: u32,
    height: u32,
    /// Prvi frame (RGBA) — izvor za thumbnail u galeriji.
    thumb: Option<image::RgbaImage>,
}

#[allow(clippy::too_many_arguments)]
fn run(
    app: &AppHandle,
    ctl: &Arc<Ctl>,
    path: &PathBuf,
    target: RecordTarget,
    opts: RecordOptions,
    fps: u32,
    crop: (u32, u32, u32, u32),
    ready: std::sync::mpsc::Sender<Result<(), String>>,
) -> Result<Recorded, String> {
    let (ox, oy, ow, oh) = crop;
    let t0 = Instant::now();

    // READY ide ODMAH: HUD se pojavi u par desetaka ms, a DXGI (~170 ms) i Media
    // Foundation (~180 ms) se dižu dok korisnik već vidi kontrole. Padne li nešto od
    // toga, `finish_session` javi grešku kroz `rec-done` i HUD se sam skloni.
    let _ = ready.send(Ok(()));

    let mut source = Source::open(&target)?;
    let (src_w, src_h) = source.size();
    let t_source = t0.elapsed();

    // MF sesija mora živjeti dulje od enkodera (Drop redoslijed je obrnut od deklaracije).
    #[cfg(windows)]
    let _mf = if matches!(opts.format, RecordFormat::Mp4) {
        Some(mp4::MfSession::start()?)
    } else {
        None
    };

    // Zvuk samo uz MP4 (GIF nema audio). Izvor koji se ne da otvoriti se preskače.
    #[cfg(windows)]
    let mixer = {
        let mut kinds = Vec::new();
        if matches!(opts.format, RecordFormat::Mp4) {
            if opts.mic {
                kinds.push(audio::Kind::Microphone);
            }
            if opts.system_audio {
                kinds.push(audio::Kind::SystemLoopback);
            }
        }
        if kinds.is_empty() {
            None
        } else {
            let m = audio::Mixer::start(&kinds);
            ctl.mic.store(
                m.kinds.contains(&audio::Kind::Microphone),
                Ordering::Relaxed,
            );
            ctl.system_audio.store(
                m.kinds.contains(&audio::Kind::SystemLoopback),
                Ordering::Relaxed,
            );
            if m.is_empty() {
                None
            } else {
                Some(m)
            }
        }
    };
    #[cfg(not(windows))]
    let mixer: Option<()> = None;

    let mut encoder = open_encoder(
        path,
        opts.format,
        ow,
        oh,
        fps,
        opts.quality,
        mixer.is_some(),
    )?;

    #[cfg(windows)]
    let mut painter = cursor::CursorPainter::new();
    log(&format!(
        "snimanje krenulo: {ow}x{oh} @ {fps}fps, format {:?}, izvor {src_w}x{src_h} (izvor {:?}, enkoder {:?})",
        opts.format,
        t_source,
        t0.elapsed() - t_source
    ));

    let interval = Duration::from_secs_f64(1.0 / fps as f64);
    let dur_100ns = (interval.as_nanos() / 100) as i64;
    let started = Instant::now();
    // Audio i video dijele sat od ovog trenutka: sve što je mikrofon uhvatio prije
    // starta bi inače stajalo ispred prvog framea kao konstantan zaostatak.
    #[cfg(windows)]
    if let Some(m) = &mixer {
        m.clear();
    }
    // Zvuk se vuče s ~120 ms zakašnjenja za satom videa — toliko WASAPI-ju treba da
    // paket stigne; bez margine bismo rupu popunili tišinom pa pravi zvuk gurnuli kasnije.
    const AUDIO_LAG: Duration = Duration::from_millis(120);
    let mut audio_written: i64 = 0; // frameova @ 48 kHz
    let mut paused_total = Duration::ZERO;
    let mut pause_started: Option<Instant> = None;
    let mut next_due = Instant::now();
    let mut last_tick = Instant::now();
    let mut last_time_100ns = -1i64;

    let mut frame_buf = vec![0u8; (ow as usize) * (oh as usize) * 4];
    let mut thumb: Option<image::RgbaImage> = None;
    let mut cancelled = false;
    // Dijagnostika: koliko obrada framea (grab+crop+kursor+blur+encode) stvarno traje.
    let mut work_total = Duration::ZERO;

    loop {
        if ctl.cancel.load(Ordering::Relaxed) {
            cancelled = true;
            break;
        }
        if ctl.stop.load(Ordering::Relaxed) {
            break;
        }

        // ── pauza ──
        if ctl.phase.load(Ordering::Relaxed) == PHASE_PAUSED {
            if pause_started.is_none() {
                pause_started = Some(Instant::now());
            }
            std::thread::sleep(Duration::from_millis(40));
            continue;
        }
        if let Some(p) = pause_started.take() {
            paused_total += p.elapsed();
            next_due = Instant::now();
            #[cfg(windows)]
            if let Some(m) = &mixer {
                m.clear();
            }
        }

        // ── tempo ──
        let now = Instant::now();
        if now < next_due {
            let wait = (next_due - now).min(Duration::from_millis(10));
            std::thread::sleep(wait);
            continue;
        }
        next_due += interval;
        if next_due < now {
            // Zaostali smo (težak frame, throttling) — preskoči rupu umjesto da juriš.
            next_due = now + interval;
        }

        // ── hvatanje ──
        let work_started = Instant::now();
        // Timeout ~0: tempo diktira sleep iznad, ne čekanje na novi frame. Ako se ekran
        // nije promijenio, DXGI vrati zadnji poznati frame — točno što želimo.
        let full = match source.grab(2) {
            Ok(f) => f,
            Err(e) => {
                log(&format!("snimanje: greška pri hvatanju framea — {e}"));
                break;
            }
        };
        crop_bgra(full, src_w, src_h, ox, oy, ow, oh, &mut frame_buf);
        if !source.has_frame() {
            // Statičan ekran odmah nakon starta: bolje kasniji prvi frame nego crni.
            continue;
        }

        #[cfg(windows)]
        if opts.capture_cursor {
            painter.draw(
                &mut frame_buf,
                ow,
                oh,
                target.monitor_x + ox as i32,
                target.monitor_y + oy as i32,
            );
        }

        // Redact ide POSLIJE kursora: ako je pokazivač nad zamagljenim poljem,
        // i on mora nestati. Destruktivno je — original nikad ne dođe do enkodera.
        {
            let rects = crate::state::lock(&ctl.blur);
            for r in rects.iter() {
                pixelate_bgra(&mut frame_buf, ow, oh, r);
            }
        }

        if thumb.is_none() {
            thumb = Some(bgra_to_rgba_image(&frame_buf, ow, oh));
        }

        // ── zapis ──
        let media = now.duration_since(started).saturating_sub(paused_total);
        let mut time_100ns = (media.as_nanos() / 100) as i64;
        // MF traži strogo rastuće timestampove.
        if time_100ns <= last_time_100ns {
            time_100ns = last_time_100ns + 1;
        }
        last_time_100ns = time_100ns;

        if let Err(e) = encoder.write(&frame_buf, time_100ns, dur_100ns) {
            log(&format!("snimanje: enkoder je odbio frame — {e}"));
            break;
        }

        #[cfg(windows)]
        if let Some(m) = &mixer {
            let due = ((media.saturating_sub(AUDIO_LAG)).as_secs_f64() * audio::RATE as f64) as i64;
            if let Err(e) = write_audio_until(&mut encoder, m, &mut audio_written, due) {
                log(&format!("snimanje: audio je odbijen — {e}"));
            }
        }

        let frames = ctl.frames.fetch_add(1, Ordering::Relaxed) + 1;
        ctl.elapsed_ms
            .store(media.as_millis() as u64, Ordering::Relaxed);
        work_total += work_started.elapsed();

        // ── HUD tick ~4x/s ──
        if last_tick.elapsed() >= Duration::from_millis(250) {
            last_tick = Instant::now();
            if let Ok(meta) = std::fs::metadata(path) {
                ctl.size_bytes.store(meta.len(), Ordering::Relaxed);
            }
            let _ = app.emit("snapvere:rec-tick", ctl.status());
        }

        // ── tvrdi limit trajanja ──
        if opts.max_seconds > 0 && media.as_secs() >= opts.max_seconds as u64 {
            log(&format!(
                "snimanje: dosegnut limit od {}s ({frames} frameova)",
                opts.max_seconds
            ));
            break;
        }
    }

    ctl.phase.store(PHASE_FINISHING, Ordering::SeqCst);
    let _ = app.emit("snapvere:rec-state", ctl.status());
    #[cfg(windows)]
    if let Some(m) = &mixer {
        // Rep: zvuk do zadnjeg framea (WASAPI ga već ima u redu, samo ga još nismo povukli).
        let end = ((last_time_100ns.max(0) as f64 / 1e7) * audio::RATE as f64) as i64;
        let _ = write_audio_until(&mut encoder, m, &mut audio_written, end);
    }
    let frames = ctl.frames.load(Ordering::Relaxed).max(1);
    log(&format!(
        "snimanje: prosječna obrada framea {:.1} ms (budžet {:.1} ms @ {fps} fps)",
        work_total.as_secs_f64() * 1000.0 / frames as f64,
        1000.0 / fps as f64
    ));
    encoder.finish()?;
    drop(encoder);

    Ok(Recorded {
        cancelled,
        width: ow,
        height: oh,
        thumb,
    })
}

/// Povuci iz miksera i zapiši sve audio frameove do `due` (frameovi @ 48 kHz).
#[cfg(windows)]
fn write_audio_until(
    encoder: &mut Encoder,
    mixer: &audio::Mixer,
    written: &mut i64,
    due: i64,
) -> Result<(), String> {
    let n = due - *written;
    if n <= 0 {
        return Ok(());
    }
    let pcm = mixer.pull(n as usize);
    let to_100ns = |frames: i64| frames * 10_000_000 / audio::RATE as i64;
    encoder.write_audio(&pcm, to_100ns(*written), to_100ns(n))?;
    *written += n;
    Ok(())
}

fn open_encoder(
    path: &Path,
    format: RecordFormat,
    w: u32,
    h: u32,
    fps: u32,
    quality: u8,
    with_audio: bool,
) -> Result<Encoder, String> {
    #[cfg(not(windows))]
    let _ = with_audio;
    match format {
        #[cfg(windows)]
        RecordFormat::Mp4 => Ok(Encoder::Mp4(Box::new(mp4::Mp4Encoder::new(
            path,
            w,
            h,
            fps,
            quality,
            with_audio.then_some(mp4::AudioSpec {
                sample_rate: audio::RATE,
                channels: audio::CHANNELS as u32,
            }),
        )?))),
        #[cfg(not(windows))]
        RecordFormat::Mp4 => Err("MP4 snimanje je zasad dostupno samo na Windowsu".into()),
        RecordFormat::Gif => Ok(Encoder::Gif(Box::new(gifenc::GifEncoder::new(
            path, w, h, fps,
        )?))),
    }
}

/// Zatvori sesiju: spremi u galeriju ili obriši datoteku, javi frontendu, očisti slot.
fn finish_session(
    app: &AppHandle,
    ctl: &Arc<Ctl>,
    path: &PathBuf,
    outcome: Result<Recorded, String>,
) {
    #[derive(serde::Serialize, Clone)]
    #[serde(rename_all = "camelCase")]
    struct Done {
        screenshot: Option<Screenshot>,
        error: Option<String>,
        cancelled: bool,
    }

    let payload = match outcome {
        Err(e) => {
            let _ = std::fs::remove_file(path);
            log(&format!("snimanje neuspješno: {e}"));
            Done {
                screenshot: None,
                error: Some(e),
                cancelled: false,
            }
        }
        Ok(rec) if rec.cancelled => {
            let _ = std::fs::remove_file(path);
            log("snimanje otkazano, datoteka obrisana");
            Done {
                screenshot: None,
                error: None,
                cancelled: true,
            }
        }
        Ok(rec) => {
            let frames = ctl.frames.load(Ordering::Relaxed);
            if frames == 0 {
                let _ = std::fs::remove_file(path);
                Done {
                    screenshot: None,
                    error: Some("Snimka nema nijedan frame".into()),
                    cancelled: false,
                }
            } else {
                match crate::commands::register_media(
                    app,
                    path,
                    rec.width,
                    rec.height,
                    CaptureSource::Recording,
                    ctl.format.ext(),
                    rec.thumb.as_ref(),
                ) {
                    Ok(shot) => {
                        log(&format!(
                            "snimka spremljena: {} ({frames} frameova, {} KB)",
                            shot.name,
                            shot.size_bytes / 1024
                        ));
                        Done {
                            screenshot: Some(shot),
                            error: None,
                            cancelled: false,
                        }
                    }
                    Err(e) => Done {
                        screenshot: None,
                        error: Some(e),
                        cancelled: false,
                    },
                }
            }
        }
    };

    ctl.phase.store(PHASE_IDLE, Ordering::SeqCst);
    ctl.size_bytes.store(
        std::fs::metadata(path).map(|m| m.len()).unwrap_or(0),
        Ordering::Relaxed,
    );

    // Oslobodi slot da sljedeće snimanje može krenuti. Drop vlastitog JoinHandlea
    // samo odspaja dretvu — dozvoljeno i kad se izvodi baš ta dretva.
    {
        let state = app.state::<AppState>();
        *crate::state::lock(&state.recorder) = None;
    }

    crate::tray::set_recording(app, false);
    let _ = app.emit("snapvere:rec-state", RecordStatus::idle());
    let _ = app.emit("snapvere:rec-done", payload);
}

// ─────────────────────────────── pomoćno ───────────────────────────────

/// Izlazni pravokutnik unutar monitora, poravnat na parne dimenzije (zahtjev NV12/H.264).
pub fn crop_rect(t: &RecordTarget) -> Result<(u32, u32, u32, u32), String> {
    let (mw, mh) = (t.monitor_w, t.monitor_h);
    if mw < 2 || mh < 2 {
        return Err("monitor prijavljuje nevaljanu veličinu".into());
    }
    let (x, y, w, h) = match t.region {
        Some(r) => {
            let x = r.x.max(0) as u32;
            let y = r.y.max(0) as u32;
            (
                x,
                y,
                r.width.min(mw.saturating_sub(x)),
                r.height.min(mh.saturating_sub(y)),
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
fn crop_bgra(src: &[u8], sw: u32, sh: u32, x: u32, y: u32, w: u32, h: u32, dst: &mut [u8]) {
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
fn pixelate_bgra(buf: &mut [u8], w: u32, h: u32, r: &PixelRect) {
    let x0 = r.x.min(w) as usize;
    let y0 = r.y.min(h) as usize;
    let x1 = (r.x.saturating_add(r.width)).min(w) as usize;
    let y1 = (r.y.saturating_add(r.height)).min(h) as usize;
    if x1 <= x0 || y1 <= y0 {
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

fn bgra_to_rgba_image(bgra: &[u8], w: u32, h: u32) -> image::RgbaImage {
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

/// Odredišna datoteka u galeriji, s vremenskim pečatom u imenu.
fn output_path(app: &AppHandle, format: RecordFormat) -> Result<PathBuf, String> {
    let dir = crate::commands::gallery_dir(app)?;
    std::fs::create_dir_all(&dir).map_err(|e| format!("ne mogu kreirati galeriju: {e}"))?;
    let name = format!(
        "Snapvere {}.{}",
        chrono::Local::now().format("%Y-%m-%d %H.%M.%S"),
        format.ext()
    );
    Ok(dir.join(name))
}
