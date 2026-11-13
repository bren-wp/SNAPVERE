//! MP4 / H.264 enkoder preko Windows Media Foundation Sink Writera.
//!
//! Media Foundation je već u sustavu, koristi hardverski enkoder (Quick Sync / NVENC /
//! AMF) kad postoji i ne traži nikakav vanjski binary — bitno jer Snapvere ostaje mali
//! i bez licencnih repova (za razliku od bundlanja ffmpeg-a ili OpenH264 binarya).
//!
//! Ulaz je NV12, ne RGB32. Razlog: kod RGB32 ulaza smjer redaka ovisi o predznaku
//! `MF_MT_DEFAULT_STRIDE` pa se snimka zna okrenuti naopako, a sink writer mora
//! ubaciti dodatni Video Processor MFT. NV12 je uvijek top-down i H.264 enkoder ga
//! prima izravno.

#![cfg(windows)]

use std::path::Path;

use windows::core::HSTRING;
use windows::Win32::Media::MediaFoundation as mf;
use windows::Win32::Media::MediaFoundation::{
    eAVEncH264VProfile_High, IMFSinkWriter, MFCreateAttributes, MFCreateMediaType,
    MFCreateMemoryBuffer, MFCreateSample, MFCreateSinkWriterFromURL, MFMediaType_Video, MFShutdown,
    MFStartup, MFVideoFormat_H264, MFVideoFormat_NV12, MFVideoInterlace_Progressive,
    MFSTARTUP_NOSOCKET, MF_MT_AVG_BITRATE, MF_MT_FRAME_RATE, MF_MT_FRAME_SIZE,
    MF_MT_INTERLACE_MODE, MF_MT_MAJOR_TYPE, MF_MT_MPEG2_PROFILE, MF_MT_PIXEL_ASPECT_RATIO,
    MF_MT_SUBTYPE, MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, MF_SINK_WRITER_DISABLE_THROTTLING,
    MF_VERSION,
};
use windows::Win32::System::Com::{CoInitializeEx, CoUninitialize, COINIT_MULTITHREADED};

use crate::logging::log;

/// Media Foundation runtime vezan uz jednu dretvu. Drop uredno gasi MF i COM.
pub struct MfSession {
    com: bool,
}

impl MfSession {
    /// Pokreni MF na TRENUTNOJ dretvi. Mora se držati živ dok traje enkodiranje.
    pub fn start() -> Result<Self, String> {
        // RPC_E_CHANGED_MODE znači da je dretva već u drugom apartmanu — to je u redu,
        // samo onda ne smijemo zvati CoUninitialize.
        let hr = unsafe { CoInitializeEx(None, COINIT_MULTITHREADED) };
        let com = hr.is_ok();
        unsafe { MFStartup(MF_VERSION, MFSTARTUP_NOSOCKET) }
            .map_err(|e| format!("MFStartup: {e}"))?;
        Ok(Self { com })
    }
}

impl Drop for MfSession {
    fn drop(&mut self) {
        unsafe {
            let _ = MFShutdown();
            if self.com {
                CoUninitialize();
            }
        }
    }
}

pub struct Mp4Encoder {
    writer: IMFSinkWriter,
    stream: u32,
    /// AAC stream, ako se snima zvuk.
    audio_stream: Option<u32>,
    w: u32,
    h: u32,
    /// Reupotrebljiv NV12 buffer — bez ovoga alociramo ~3 MB po frameu.
    nv12: Vec<u8>,
    finalized: bool,
}

/// PCM16 stereo koji ide u AAC (128 kbps).
pub struct AudioSpec {
    pub sample_rate: u32,
    pub channels: u32,
}

impl Mp4Encoder {
    /// `w`/`h` moraju biti parni (NV12 uzorkuje kromu 2x2). `audio` dodaje AAC stream.
    pub fn new(
        path: &Path,
        w: u32,
        h: u32,
        fps: u32,
        quality: u8,
        audio: Option<AudioSpec>,
    ) -> Result<Self, String> {
        if w % 2 != 0 || h % 2 != 0 || w == 0 || h == 0 {
            return Err(format!("nevaljane dimenzije za H.264: {w}x{h}"));
        }
        let bitrate = bitrate_for(w, h, fps, quality);

        unsafe {
            // Hardverski enkoder + bez throttlinga (mi diktiramo tempo, ne sink writer).
            let mut attrs = None;
            MFCreateAttributes(&mut attrs, 2).map_err(|e| format!("MFCreateAttributes: {e}"))?;
            let attrs = attrs.ok_or("MFCreateAttributes nije vratio atribute")?;
            attrs
                .SetUINT32(&MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1)
                .map_err(|e| e.to_string())?;
            attrs
                .SetUINT32(&MF_SINK_WRITER_DISABLE_THROTTLING, 1)
                .map_err(|e| e.to_string())?;

            let url = HSTRING::from(path.as_os_str());
            let writer = MFCreateSinkWriterFromURL(&url, None, &attrs)
                .map_err(|e| format!("MFCreateSinkWriterFromURL({}): {e}", path.display()))?;

            // ── izlazni tip: H.264 ──
            let out = MFCreateMediaType().map_err(|e| e.to_string())?;
            out.SetGUID(&MF_MT_MAJOR_TYPE, &MFMediaType_Video)
                .map_err(|e| e.to_string())?;
            out.SetGUID(&MF_MT_SUBTYPE, &MFVideoFormat_H264)
                .map_err(|e| e.to_string())?;
            out.SetUINT32(&MF_MT_AVG_BITRATE, bitrate)
                .map_err(|e| e.to_string())?;
            out.SetUINT32(&MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive.0 as u32)
                .map_err(|e| e.to_string())?;
            out.SetUINT32(&MF_MT_MPEG2_PROFILE, eAVEncH264VProfile_High.0 as u32)
                .map_err(|e| e.to_string())?;
            out.SetUINT64(&MF_MT_FRAME_SIZE, pack(w, h))
                .map_err(|e| e.to_string())?;
            out.SetUINT64(&MF_MT_FRAME_RATE, pack(fps, 1))
                .map_err(|e| e.to_string())?;
            out.SetUINT64(&MF_MT_PIXEL_ASPECT_RATIO, pack(1, 1))
                .map_err(|e| e.to_string())?;
            let stream = writer
                .AddStream(&out)
                .map_err(|e| format!("AddStream: {e}"))?;

            // ── ulazni tip: NV12 ──
            let inp = MFCreateMediaType().map_err(|e| e.to_string())?;
            inp.SetGUID(&MF_MT_MAJOR_TYPE, &MFMediaType_Video)
                .map_err(|e| e.to_string())?;
            inp.SetGUID(&MF_MT_SUBTYPE, &MFVideoFormat_NV12)
                .map_err(|e| e.to_string())?;
            inp.SetUINT32(&MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive.0 as u32)
                .map_err(|e| e.to_string())?;
            inp.SetUINT64(&MF_MT_FRAME_SIZE, pack(w, h))
                .map_err(|e| e.to_string())?;
            inp.SetUINT64(&MF_MT_FRAME_RATE, pack(fps, 1))
                .map_err(|e| e.to_string())?;
            inp.SetUINT64(&MF_MT_PIXEL_ASPECT_RATIO, pack(1, 1))
                .map_err(|e| e.to_string())?;
            writer
                .SetInputMediaType(stream, &inp, None)
                .map_err(|e| format!("SetInputMediaType(NV12): {e}"))?;

            // ── zvuk: PCM16 ulaz → AAC-LC izlaz ──
            let audio_stream = match audio {
                Some(a) => {
                    let out = MFCreateMediaType().map_err(|e| e.to_string())?;
                    out.SetGUID(&MF_MT_MAJOR_TYPE, &mf::MFMediaType_Audio)
                        .map_err(|e| e.to_string())?;
                    out.SetGUID(&MF_MT_SUBTYPE, &mf::MFAudioFormat_AAC)
                        .map_err(|e| e.to_string())?;
                    out.SetUINT32(&mf::MF_MT_AUDIO_BITS_PER_SAMPLE, 16)
                        .map_err(|e| e.to_string())?;
                    out.SetUINT32(&mf::MF_MT_AUDIO_SAMPLES_PER_SECOND, a.sample_rate)
                        .map_err(|e| e.to_string())?;
                    out.SetUINT32(&mf::MF_MT_AUDIO_NUM_CHANNELS, a.channels)
                        .map_err(|e| e.to_string())?;
                    // 16000 B/s = 128 kbps; AAC enkoder prihvaća 12000/16000/20000/24000.
                    out.SetUINT32(&mf::MF_MT_AUDIO_AVG_BYTES_PER_SECOND, 16_000)
                        .map_err(|e| e.to_string())?;
                    let s = writer
                        .AddStream(&out)
                        .map_err(|e| format!("AddStream(AAC): {e}"))?;

                    let inp = MFCreateMediaType().map_err(|e| e.to_string())?;
                    inp.SetGUID(&MF_MT_MAJOR_TYPE, &mf::MFMediaType_Audio)
                        .map_err(|e| e.to_string())?;
                    inp.SetGUID(&MF_MT_SUBTYPE, &mf::MFAudioFormat_PCM)
                        .map_err(|e| e.to_string())?;
                    inp.SetUINT32(&mf::MF_MT_AUDIO_BITS_PER_SAMPLE, 16)
                        .map_err(|e| e.to_string())?;
                    inp.SetUINT32(&mf::MF_MT_AUDIO_SAMPLES_PER_SECOND, a.sample_rate)
                        .map_err(|e| e.to_string())?;
                    inp.SetUINT32(&mf::MF_MT_AUDIO_NUM_CHANNELS, a.channels)
                        .map_err(|e| e.to_string())?;
                    let block = a.channels * 2;
                    inp.SetUINT32(&mf::MF_MT_AUDIO_BLOCK_ALIGNMENT, block)
                        .map_err(|e| e.to_string())?;
                    inp.SetUINT32(&mf::MF_MT_AUDIO_AVG_BYTES_PER_SECOND, a.sample_rate * block)
                        .map_err(|e| e.to_string())?;
                    inp.SetUINT32(&mf::MF_MT_ALL_SAMPLES_INDEPENDENT, 1)
                        .map_err(|e| e.to_string())?;
                    writer
                        .SetInputMediaType(s, &inp, None)
                        .map_err(|e| format!("SetInputMediaType(PCM): {e}"))?;
                    Some(s)
                }
                None => None,
            };

            writer
                .BeginWriting()
                .map_err(|e| format!("BeginWriting: {e}"))?;

            log(&format!(
                "MP4 enkoder: {w}x{h} @ {fps}fps, {} kbps → {}",
                bitrate / 1000,
                path.display()
            ));

            Ok(Self {
                writer,
                stream,
                audio_stream,
                w,
                h,
                nv12: vec![0u8; (w as usize * h as usize * 3) / 2],
                finalized: false,
            })
        }
    }

    /// Zapiši jedan BGRA frame. `time`/`duration` su u 100-ns jedinicama.
    pub fn write(&mut self, bgra: &[u8], time_100ns: i64, dur_100ns: i64) -> Result<(), String> {
        bgra_to_nv12(bgra, self.w, self.h, &mut self.nv12);
        let len = self.nv12.len() as u32;

        unsafe {
            let buffer = MFCreateMemoryBuffer(len).map_err(|e| e.to_string())?;
            let mut dst: *mut u8 = std::ptr::null_mut();
            buffer
                .Lock(&mut dst, None, None)
                .map_err(|e| e.to_string())?;
            std::ptr::copy_nonoverlapping(self.nv12.as_ptr(), dst, len as usize);
            buffer.Unlock().map_err(|e| e.to_string())?;
            buffer.SetCurrentLength(len).map_err(|e| e.to_string())?;

            let sample = MFCreateSample().map_err(|e| e.to_string())?;
            sample.AddBuffer(&buffer).map_err(|e| e.to_string())?;
            sample
                .SetSampleTime(time_100ns)
                .map_err(|e| e.to_string())?;
            sample
                .SetSampleDuration(dur_100ns)
                .map_err(|e| e.to_string())?;
            self.writer
                .WriteSample(self.stream, &sample)
                .map_err(|e| format!("WriteSample: {e}"))?;
        }
        Ok(())
    }

    /// Zapiši blok PCM16 uzoraka (interleaved). `time`/`duration` u 100-ns jedinicama.
    pub fn write_audio(
        &mut self,
        pcm: &[i16],
        time_100ns: i64,
        dur_100ns: i64,
    ) -> Result<(), String> {
        let Some(stream) = self.audio_stream else {
            return Ok(());
        };
        if pcm.is_empty() {
            return Ok(());
        }
        let len = (pcm.len() * 2) as u32;
        unsafe {
            let buffer = MFCreateMemoryBuffer(len).map_err(|e| e.to_string())?;
            let mut dst: *mut u8 = std::ptr::null_mut();
            buffer
                .Lock(&mut dst, None, None)
                .map_err(|e| e.to_string())?;
            std::ptr::copy_nonoverlapping(pcm.as_ptr() as *const u8, dst, len as usize);
            buffer.Unlock().map_err(|e| e.to_string())?;
            buffer.SetCurrentLength(len).map_err(|e| e.to_string())?;

            let sample = MFCreateSample().map_err(|e| e.to_string())?;
            sample.AddBuffer(&buffer).map_err(|e| e.to_string())?;
            sample
                .SetSampleTime(time_100ns)
                .map_err(|e| e.to_string())?;
            sample
                .SetSampleDuration(dur_100ns)
                .map_err(|e| e.to_string())?;
            self.writer
                .WriteSample(stream, &sample)
                .map_err(|e| format!("WriteSample(audio): {e}"))?;
        }
        Ok(())
    }

    /// Zatvori MP4 (moov atom). Bez ovoga datoteka nije reproducibilna.
    pub fn finish(&mut self) -> Result<(), String> {
        if self.finalized {
            return Ok(());
        }
        self.finalized = true;
        unsafe { self.writer.Finalize() }.map_err(|e| format!("Finalize: {e}"))
    }
}

impl Drop for Mp4Encoder {
    fn drop(&mut self) {
        // Sigurnosna mreža: ako je snimanje prekinuto greškom, ipak zatvori kontejner.
        if !self.finalized {
            if let Err(e) = self.finish() {
                log(&format!("MP4 finalize u dropu: {e}"));
            }
        }
    }
}

/// MF pakira parove (širina/visina, brojnik/nazivnik) u jedan u64: high | low.
fn pack(high: u32, low: u32) -> u64 {
    ((high as u64) << 32) | low as u64
}

/// Ciljani bitrate. ~0.1 bita po pikselu po frameu na kvaliteti 80 daje oko
/// 6 Mbps za 1080p30 — usporedivo s macOS screen recordingom.
fn bitrate_for(w: u32, h: u32, fps: u32, quality: u8) -> u32 {
    let q = quality.clamp(1, 100) as f64 / 100.0;
    let bpp = 0.04 + q * 0.10;
    let raw = w as f64 * h as f64 * fps as f64 * bpp;
    (raw as u32).clamp(1_000_000, 40_000_000)
}

/// Testni pristup pretvorbi boja — pogrešno zamijenjeni B/R kanali inače se vide
/// tek kao neispravna boja u gotovoj snimci.
#[cfg(test)]
pub(crate) fn bgra_to_nv12_for_test(bgra: &[u8], w: u32, h: u32, out: &mut [u8]) {
    bgra_to_nv12(bgra, w, h, out)
}

/// BGRA → NV12 (BT.601, studio raspon 16-235). Krominancija se uzorkuje po 2x2 bloku.
///
/// Paralelizirano po trakama redaka: 1440p @ 60 fps je ~220 Mpx/s, što je na jednoj
/// jezgri cijela jezgra samo za konverziju — a to se osjeti kao lag u UI-ju. Trake su
/// parne visine jer kroma pokriva 2x2 blok. Dimenzije su parne (provjera u `new`).
fn bgra_to_nv12(bgra: &[u8], w: u32, h: u32, out: &mut [u8]) {
    let wi = w as usize;
    let hi = h as usize;
    let (y_plane, uv_plane) = out.split_at_mut(wi * hi);

    let threads = std::thread::available_parallelism()
        .map(|n| n.get())
        .unwrap_or(2)
        .clamp(1, 8);

    // Ispod ~100k piksela paralelizacija košta više nego što vraća.
    if threads == 1 || wi * hi < 100_000 {
        convert_rows(bgra, wi, 0, y_plane, uv_plane);
        return;
    }

    let mut band = hi.div_ceil(threads);
    if band % 2 == 1 {
        band += 1;
    }
    let band = band.max(2);

    std::thread::scope(|s| {
        let y_bands = y_plane.chunks_mut(band * wi);
        let uv_bands = uv_plane.chunks_mut((band / 2) * wi);
        for (i, (yb, uvb)) in y_bands.zip(uv_bands).enumerate() {
            let row0 = i * band;
            s.spawn(move || convert_rows(bgra, wi, row0, yb, uvb));
        }
    });
}

/// Pretvori redove `row0..row0+rows` (rows = yb.len()/wi, paran broj) u zadane Y i UV trake.
fn convert_rows(bgra: &[u8], wi: usize, row0: usize, yb: &mut [u8], uvb: &mut [u8]) {
    let rows = yb.len() / wi;
    let stride = wi * 4;

    // Luma — svaki piksel.
    for r in 0..rows {
        let src = &bgra[(row0 + r) * stride..(row0 + r + 1) * stride];
        let dst = &mut yb[r * wi..(r + 1) * wi];
        for (d, p) in dst.iter_mut().zip(src.chunks_exact(4)) {
            let b = p[0] as i32;
            let g = p[1] as i32;
            let rr = p[2] as i32;
            *d = (((66 * rr + 129 * g + 25 * b + 128) >> 8) + 16) as u8;
        }
    }

    // Kroma — prosjek 2x2 bloka, pa pretvorba (jeftinije i mekše od uzimanja jednog piksela).
    for r in (0..rows).step_by(2) {
        let top = &bgra[(row0 + r) * stride..(row0 + r + 1) * stride];
        let bot = &bgra[(row0 + r + 1) * stride..(row0 + r + 2) * stride];
        let uv = &mut uvb[(r / 2) * wi..(r / 2 + 1) * wi];
        for x in (0..wi).step_by(2) {
            let o = x * 4;
            let b = (top[o] as i32 + top[o + 4] as i32 + bot[o] as i32 + bot[o + 4] as i32) / 4;
            let g =
                (top[o + 1] as i32 + top[o + 5] as i32 + bot[o + 1] as i32 + bot[o + 5] as i32) / 4;
            let rr =
                (top[o + 2] as i32 + top[o + 6] as i32 + bot[o + 2] as i32 + bot[o + 6] as i32) / 4;
            uv[x] = (((-38 * rr - 74 * g + 112 * b + 128) >> 8) + 128).clamp(0, 255) as u8;
            uv[x + 1] = (((112 * rr - 94 * g - 18 * b + 128) >> 8) + 128).clamp(0, 255) as u8;
        }
    }
}
