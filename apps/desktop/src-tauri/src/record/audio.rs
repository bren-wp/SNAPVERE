//! Hvatanje zvuka preko WASAPI-ja: mikrofon (capture endpoint) i sistemski zvuk
//! (render endpoint u loopback modu). Svaki izvor ima svoju dretvu koja puni red
//! u zajedničkom formatu (48 kHz, stereo, f32); `Mixer::pull` ih zbraja u PCM16
//! koji ide u AAC stream MP4 enkodera.
//!
//! Satovi uređaja nisu isti kao sat videa, pa se ne vežemo na njihove timestampove:
//! video petlja vuče točno onoliko frameova koliko je proteklo medijskog vremena,
//! a manjak se popuni tišinom (loopback bez reprodukcije često ne šalje ništa).

use std::collections::VecDeque;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};
use std::time::Duration;

use windows::Win32::Media::Audio::{
    eCapture, eConsole, eRender, IAudioCaptureClient, IAudioClient, IMMDeviceEnumerator,
    MMDeviceEnumerator, AUDCLNT_BUFFERFLAGS_SILENT, AUDCLNT_SHAREMODE_SHARED,
    AUDCLNT_STREAMFLAGS_LOOPBACK, WAVEFORMATEX, WAVEFORMATEXTENSIBLE, WAVE_FORMAT_PCM,
};
use windows::Win32::Media::KernelStreaming::WAVE_FORMAT_EXTENSIBLE;
use windows::Win32::Media::Multimedia::{KSDATAFORMAT_SUBTYPE_IEEE_FLOAT, WAVE_FORMAT_IEEE_FLOAT};
use windows::Win32::System::Com::{
    CoCreateInstance, CoInitializeEx, CoTaskMemFree, CoUninitialize, CLSCTX_ALL,
    COINIT_MULTITHREADED,
};

use crate::logging::log;

/// Format u koji se sve svodi (AAC enkoder prima 44.1/48 kHz, 1-2 kanala, PCM16).
pub const RATE: u32 = 48_000;
pub const CHANNELS: usize = 2;
/// Više od ovoga u redu = zaostatak (npr. nakon pauze) → odbaci najstarije.
const MAX_QUEUED_FRAMES: usize = RATE as usize * 2;

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Kind {
    Microphone,
    SystemLoopback,
}

impl Kind {
    fn label(self) -> &'static str {
        match self {
            Kind::Microphone => "mikrofon",
            Kind::SystemLoopback => "sistemski zvuk",
        }
    }
}

/// Jedan red uzoraka (interleaved stereo f32 @ 48 kHz) koji puni dretva za hvatanje.
struct Queue {
    samples: Mutex<VecDeque<f32>>,
}

/// Zbraja sve aktivne izvore u jedan PCM16 stereo tok.
pub struct Mixer {
    queues: Vec<Arc<Queue>>,
    stop: Arc<AtomicBool>,
    threads: Vec<std::thread::JoinHandle<()>>,
    pub kinds: Vec<Kind>,
}

impl Mixer {
    /// Otvori tražene izvore. Izvor koji se ne da otvoriti (nema mikrofona, nema
    /// uređaja za reprodukciju) se preskače uz log — snimanje ide dalje bez njega.
    pub fn start(kinds: &[Kind]) -> Self {
        let stop = Arc::new(AtomicBool::new(false));
        let mut queues = Vec::new();
        let mut threads = Vec::new();
        let mut opened = Vec::new();

        for &kind in kinds {
            let queue = Arc::new(Queue { samples: Mutex::new(VecDeque::new()) });
            let (ready_tx, ready_rx) = std::sync::mpsc::channel::<Result<(), String>>();
            let q = Arc::clone(&queue);
            let st = Arc::clone(&stop);
            let handle = match std::thread::Builder::new()
                .name(format!("snapvere-audio-{kind:?}"))
                .spawn(move || capture_thread(kind, q, st, ready_tx))
            {
                Ok(h) => h,
                Err(e) => {
                    log(&format!("audio: dretva za {} nije pokrenuta — {e}", kind.label()));
                    continue;
                }
            };

            match ready_rx.recv_timeout(Duration::from_secs(3)) {
                Ok(Ok(())) => {
                    queues.push(queue);
                    threads.push(handle);
                    opened.push(kind);
                }
                Ok(Err(e)) => log(&format!("audio: {} nije dostupan — {e}", kind.label())),
                Err(_) => log(&format!("audio: {} se nije javio u 3 s", kind.label())),
            }
        }

        Self { queues, stop, threads, kinds: opened }
    }

    pub fn is_empty(&self) -> bool {
        self.queues.is_empty()
    }

    /// Odbaci sve što čeka u redovima (poravnanje sa satom videa, izlazak iz pauze).
    pub fn clear(&self) {
        for q in &self.queues {
            crate::state::lock(&q.samples).clear();
        }
    }

    /// Izvuci točno `frames` stereo frameova kao PCM16. Manjak = tišina.
    pub fn pull(&self, frames: usize) -> Vec<i16> {
        let n = frames * CHANNELS;
        let mut mix = vec![0f32; n];
        for q in &self.queues {
            let mut samples = crate::state::lock(&q.samples);
            for slot in mix.iter_mut().take(n) {
                match samples.pop_front() {
                    Some(s) => *slot += s,
                    None => break,
                }
            }
        }
        mix.iter()
            .map(|s| (s.clamp(-1.0, 1.0) * i16::MAX as f32) as i16)
            .collect()
    }
}

impl Drop for Mixer {
    fn drop(&mut self) {
        self.stop.store(true, Ordering::Relaxed);
        for t in self.threads.drain(..) {
            let _ = t.join();
        }
    }
}

// ────────────────────────────── dretva za hvatanje ──────────────────────────────

/// Format izvora kako ga WASAPI daje (mix format uređaja).
struct SourceFormat {
    rate: u32,
    channels: usize,
    float: bool,
    bits: u16,
}

fn capture_thread(
    kind: Kind,
    queue: Arc<Queue>,
    stop: Arc<AtomicBool>,
    ready: std::sync::mpsc::Sender<Result<(), String>>,
) {
    unsafe {
        let _ = CoInitializeEx(None, COINIT_MULTITHREADED);
    }
    let (client, capture, fmt) = match open(kind) {
        Ok(v) => v,
        Err(e) => {
            let _ = ready.send(Err(e));
            unsafe { CoUninitialize() };
            return;
        }
    };
    let _ = ready.send(Ok(()));

    let mut resampler = Resampler::new(fmt.rate);
    let mut scratch: Vec<f32> = Vec::new();

    while !stop.load(Ordering::Relaxed) {
        unsafe {
            let mut packet = capture.GetNextPacketSize().unwrap_or(0);
            while packet > 0 {
                let mut data: *mut u8 = std::ptr::null_mut();
                let mut frames = 0u32;
                let mut flags = 0u32;
                if capture
                    .GetBuffer(&mut data, &mut frames, &mut flags, None, None)
                    .is_err()
                {
                    break;
                }
                scratch.clear();
                if flags & AUDCLNT_BUFFERFLAGS_SILENT.0 as u32 != 0 || data.is_null() {
                    scratch.resize(frames as usize * CHANNELS, 0.0);
                } else {
                    to_stereo_f32(data, frames as usize, &fmt, &mut scratch);
                }
                let _ = capture.ReleaseBuffer(frames);

                let out = resampler.process(&scratch);
                let mut q = crate::state::lock(&queue.samples);
                q.extend(out.iter().copied());
                let over = q.len().saturating_sub(MAX_QUEUED_FRAMES * CHANNELS);
                if over > 0 {
                    q.drain(..over);
                }
                packet = capture.GetNextPacketSize().unwrap_or(0);
            }
        }
        std::thread::sleep(Duration::from_millis(5));
    }

    unsafe {
        let _ = client.Stop();
        drop(capture);
        drop(client);
        CoUninitialize();
    }
}

fn open(kind: Kind) -> Result<(IAudioClient, IAudioCaptureClient, SourceFormat), String> {
    unsafe {
        let enumerator: IMMDeviceEnumerator = CoCreateInstance(&MMDeviceEnumerator, None, CLSCTX_ALL)
            .map_err(|e| format!("MMDeviceEnumerator: {e}"))?;
        let flow = match kind {
            Kind::Microphone => eCapture,
            Kind::SystemLoopback => eRender,
        };
        let device = enumerator
            .GetDefaultAudioEndpoint(flow, eConsole)
            .map_err(|e| format!("nema zadanog uređaja: {e}"))?;
        let client: IAudioClient = device
            .Activate(CLSCTX_ALL, None)
            .map_err(|e| format!("IAudioClient: {e}"))?;

        let fmt_ptr = client.GetMixFormat().map_err(|e| format!("GetMixFormat: {e}"))?;
        let fmt = read_format(fmt_ptr);

        let flags = match kind {
            Kind::SystemLoopback => AUDCLNT_STREAMFLAGS_LOOPBACK,
            Kind::Microphone => 0,
        };
        // 200 ms buffera: dovoljno da 5 ms polling nikad ne izgubi paket.
        let init = client.Initialize(AUDCLNT_SHAREMODE_SHARED, flags, 2_000_000, 0, fmt_ptr, None);
        CoTaskMemFree(Some(fmt_ptr as *const _));
        init.map_err(|e| format!("Initialize: {e}"))?;

        let capture: IAudioCaptureClient = client
            .GetService()
            .map_err(|e| format!("IAudioCaptureClient: {e}"))?;
        client.Start().map_err(|e| format!("Start: {e}"))?;

        log(&format!(
            "audio: {} otvoren — {} Hz, {} kan., {}",
            kind.label(),
            fmt.rate,
            fmt.channels,
            if fmt.float { "float" } else { "PCM" }
        ));
        Ok((client, capture, fmt))
    }
}

unsafe fn read_format(p: *const WAVEFORMATEX) -> SourceFormat {
    let f = &*p;
    let tag = f.wFormatTag as u32;
    let float = if tag == WAVE_FORMAT_EXTENSIBLE {
        // WAVEFORMATEXTENSIBLE je packed(1) → polje se ne smije referencirati, samo kopirati.
        let ext = std::ptr::read_unaligned(p as *const WAVEFORMATEXTENSIBLE);
        let sub = ext.SubFormat; // kopija po vrijednosti; `==` bi uzeo referencu na packed polje
        sub == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
    } else {
        tag == WAVE_FORMAT_IEEE_FLOAT
    };
    let _ = WAVE_FORMAT_PCM; // ostali tagovi se tretiraju kao cjelobrojni PCM
    SourceFormat {
        rate: f.nSamplesPerSec,
        channels: (f.nChannels as usize).max(1),
        float,
        bits: f.wBitsPerSample,
    }
}

/// Pretvori sirovi WASAPI paket u interleaved stereo f32 (izvorni sample rate).
unsafe fn to_stereo_f32(data: *const u8, frames: usize, fmt: &SourceFormat, out: &mut Vec<f32>) {
    let ch = fmt.channels;
    let read = |i: usize| -> f32 {
        // i = indeks uzorka (frame * ch + kanal)
        match (fmt.float, fmt.bits) {
            (true, 32) => *(data as *const f32).add(i),
            (false, 16) => *(data as *const i16).add(i) as f32 / 32768.0,
            (false, 32) => *(data as *const i32).add(i) as f32 / 2_147_483_648.0,
            (false, 24) => {
                let b = data.add(i * 3);
                let v = ((*b.add(2) as i32) << 24 | (*b.add(1) as i32) << 16 | (*b as i32) << 8) >> 8;
                v as f32 / 8_388_608.0
            }
            (false, 8) => (*data.add(i) as f32 - 128.0) / 128.0,
            _ => 0.0,
        }
    };
    out.reserve(frames * CHANNELS);
    for f in 0..frames {
        let base = f * ch;
        match ch {
            1 => {
                let m = read(base);
                out.push(m);
                out.push(m);
            }
            _ => {
                out.push(read(base));
                out.push(read(base + 1));
            }
        }
    }
}

/// Linearni resampler stereo toka na 48 kHz. Za 48 kHz izvore je prolaz bez kopije.
struct Resampler {
    ratio: f64, // ulaznih frameova po izlaznom frameu
    pos: f64,   // pozicija u ulaznom toku (u frameovima), prenosi se između paketa
    tail: [f32; 2],
    has_tail: bool,
}

impl Resampler {
    fn new(src_rate: u32) -> Self {
        Self { ratio: src_rate as f64 / RATE as f64, pos: 0.0, tail: [0.0; 2], has_tail: false }
    }

    fn process(&mut self, input: &[f32]) -> Vec<f32> {
        if (self.ratio - 1.0).abs() < 1e-9 {
            return input.to_vec();
        }
        let in_frames = input.len() / CHANNELS;
        if in_frames == 0 {
            return Vec::new();
        }
        // Zalijepi zadnji frame prošlog paketa ispred ovog da interpolacija nema šav.
        let mut buf: Vec<f32> = Vec::with_capacity(input.len() + 2);
        if self.has_tail {
            buf.extend_from_slice(&self.tail);
        }
        buf.extend_from_slice(input);
        let frames = buf.len() / CHANNELS;

        let mut out = Vec::with_capacity((frames as f64 / self.ratio) as usize * CHANNELS + 2);
        while self.pos + 1.0 < frames as f64 {
            let i = self.pos as usize;
            let t = (self.pos - i as f64) as f32;
            for c in 0..CHANNELS {
                let a = buf[i * CHANNELS + c];
                let b = buf[(i + 1) * CHANNELS + c];
                out.push(a + (b - a) * t);
            }
            self.pos += self.ratio;
        }
        // Zadrži zadnji frame kao rep za sljedeći paket i pomakni poziciju.
        let last = frames - 1;
        self.tail = [buf[last * CHANNELS], buf[last * CHANNELS + 1]];
        self.has_tail = true;
        self.pos -= last as f64;
        out
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn resampler_48k_je_prolaz() {
        let mut r = Resampler::new(48_000);
        let input: Vec<f32> = (0..20).map(|i| i as f32).collect();
        assert_eq!(r.process(&input), input);
    }

    #[test]
    fn resampler_96k_prepolovi_i_zadrzi_kontinuitet() {
        let mut r = Resampler::new(96_000);
        // 2 paketa po 100 stereo frameova rampe → izlaz ~100 frameova ukupno, monotono rastuć.
        let mk = |start: usize| -> Vec<f32> {
            (0..100).flat_map(|f| [(start + f) as f32, -((start + f) as f32)]).collect()
        };
        let a = r.process(&mk(0));
        let b = r.process(&mk(100));
        let total = (a.len() + b.len()) / CHANNELS;
        assert!((95..=101).contains(&total), "izlaz {total} frameova");
        let left: Vec<f32> = a.iter().chain(b.iter()).step_by(2).copied().collect();
        assert!(left.windows(2).all(|w| w[1] > w[0]), "šav između paketa");
    }

    #[test]
    fn mixer_bez_izvora_daje_tisinu() {
        let m = Mixer { queues: vec![], stop: Arc::new(AtomicBool::new(false)), threads: vec![], kinds: vec![] };
        let out = m.pull(10);
        assert_eq!(out.len(), 20);
        assert!(out.iter().all(|&s| s == 0));
    }

    #[test]
    fn mixer_zbraja_izvore_i_klipa() {
        let q1 = Arc::new(Queue { samples: Mutex::new(VecDeque::from(vec![0.5f32; 4])) });
        let q2 = Arc::new(Queue { samples: Mutex::new(VecDeque::from(vec![0.75f32; 2])) });
        let m = Mixer { queues: vec![q1, q2], stop: Arc::new(AtomicBool::new(false)), threads: vec![], kinds: vec![] };
        let out = m.pull(2);
        // prva dva uzorka: 0.5+0.75 = 1.25 → klip na 1.0; druga dva: samo 0.5 (q2 prazan)
        assert_eq!(out[0], i16::MAX);
        assert_eq!(out[1], i16::MAX);
        assert_eq!(out[2], (0.5 * i16::MAX as f32) as i16);
        assert_eq!(out[3], (0.5 * i16::MAX as f32) as i16);
    }
}
