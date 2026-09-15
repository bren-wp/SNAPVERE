# SNAPVERE 0.1.1 Image Pipeline

SNAPVERE's Windows image pipeline keeps capture acquisition, pixel transformation, annotation, PNG encoding and durable file publication as separate responsibilities. The shared frame contract uses BGRA8 pixels, explicit dimensions and stride, capture time and source identity.

## CaptureFrame boundary

Every producer returns a `CaptureFrame`. Before downstream work continues, the frame is validated so dimensions, stride and pixel-buffer length are internally consistent. Workflows validate again at important trust boundaries rather than assuming a native backend or transformed frame is valid.

This contract is intentionally independent of the acquisition backend. Region, screen and window workflows can therefore share cropping, annotation, encoding and file-publication components.

## Cropping

`CaptureFrameCropper` normalizes the requested rectangle, rejects empty regions and requires the complete crop to remain inside the source frame. Destination stride is calculated as `width × 4` with checked arithmetic.

The cropper copies only visible BGRA rows into a tightly packed destination buffer. The returned frame preserves the original capture time and source identifier while replacing size, stride and pixel data with the cropped region.

## Annotation

The Windows region editor renders Pen, Line, Arrow, Box and Highlight operations over the selected frame. Annotation state belongs to the local editing workflow; the original capture acquisition layer does not upload or remotely process those pixels.

Pointer-time drawing avoids unnecessary cloning of the in-progress point collection. Final rendering is performed against validated frame geometry so annotation coordinates stay bounded to the selected image surface.

## PNG encoding

`PngCaptureEncoder` writes a standards-compatible RGBA PNG stream from the BGRA capture frame. It emits the PNG signature and `IHDR`, compressed `IDAT` and `IEND` chunks with CRC-32 values.

BGRA-to-RGBA conversion and DEFLATE compression are CPU-bound, so that phase is scheduled away from the WinUI thread with `Task.Run`. The encoder reuses the `MemoryStream` backing buffer through `GetBuffer()` rather than creating a second full compressed copy before writing `IDAT`.

Each source row is validated through the frame contract, transformed into one reusable scanline and compressed with `CompressionLevel.Fastest`. Cancellation is checked while rows are processed and again before the compressed data is written to the destination stream.

## Durable local publication

`CaptureFileWriter` resolves the local capture directory and an available final name, then creates a unique hidden-style temporary file in the same directory. The encoder writes to that temp file with asynchronous I/O and the stream is flushed before publication.

The final `File.Move` is the commit boundary. Cancellation is checked immediately before the move. If encoding, flushing or cancellation fails, SNAPVERE performs best-effort temp-file cleanup and rethrows the original failure.

Because the temp file and final file are in the same directory, the design avoids deliberately presenting an incomplete encode under the final capture name. Cleanup failure is not allowed to replace the original capture error.

## Memory and responsiveness posture

The pipeline avoids several avoidable full-frame or compressed-buffer copies, but screenshot processing still necessarily allocates memory proportional to image dimensions. Large multi-monitor captures and large annotated regions can therefore use significant memory even when the implementation is functioning correctly.

Current performance hardening includes direct BGRA writes into Region/Window overlay bitmaps, earlier release of frozen Window Capture monitor frames, avoiding an extra PNG `ToArray()` copy in the clipboard path and keeping compression off the UI thread.

See [Performance & Stability](PERFORMANCE.md) for the current evidence boundary rather than treating these implementation choices as a fixed memory guarantee.

## Regression evidence

The unit suite covers crop bounds and row copying, annotation rendering, PNG dimensions and decoded RGBA pixel content, successful atomic PNG publication, cancellation without a published final file and cleanup behavior around the shared writer.

Windows CI executes those tests on the x64 test path and separately builds x86 and ARM64 application payloads. Package CI also validates public executable size budgets so image/runtime changes cannot silently cause unbounded package growth.

Related documents: [Capture Engine](CAPTURE-ENGINE.md), [Region Capture](REGION-CAPTURE.md), [Window Capture](WINDOW-CAPTURE.md), [Performance & Stability](PERFORMANCE.md), [QA Matrix](QA-MATRIX.md).
