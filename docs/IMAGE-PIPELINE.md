# Image Pipeline

## Status

PNG encoding from validated BGRA8 capture frames is implemented without an external imaging dependency. JPEG and WebP remain planned.

## Input contract

The image pipeline consumes `CaptureFrame` instances after capture validation. It does not know about XAML controls, monitor handles or capture windows.

## PNG encoder

`PngCaptureEncoder` writes a standards-based PNG stream:

1. PNG signature
2. `IHDR` with 8-bit RGBA color type
3. zlib-compressed scanlines in `IDAT`
4. `IEND`
5. CRC32 for every chunk

BGRA source pixels are converted to RGBA during scanline construction. Alpha is preserved so later freeform capture and editor composition can produce transparent exports.

The initial encoder uses filter type 0 and `CompressionLevel.Fastest`, prioritizing low capture-to-save latency. Adaptive PNG filters can be introduced after profiling rather than adding complexity speculatively.

## Memory behavior

The current implementation buffers compressed `IDAT` data in memory. This is acceptable for the first monitor-capture path but must be profiled for multi-monitor and long scrolling captures. A chunked IDAT writer is the planned hardening path if peak memory becomes material.

## Tests

The PNG test creates a known 2×1 BGRA frame, encodes it, parses the PNG chunk stream, validates IHDR dimensions, decompresses IDAT through `ZLibStream`, and verifies the exact resulting RGBA scanline.

## Planned formats

- JPEG with configurable quality and explicit alpha flattening
- WebP using a production-supported Windows/local codec path
- BMP for compatibility
- TIFF/PDF export only when justified by user workflows

PNG remains the default lossless screenshot format.
