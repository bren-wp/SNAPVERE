# SNAPVERE Architecture

This document describes the actively maintained Windows and browser architecture. Release-specific historical behavior remains in [RELEASES.md](../RELEASES.md).

## Windows boundaries

The Windows product is split into UI, application workflows, capture backends, imaging and shared infrastructure. The app is tray-first and uses dependency injection for long-lived services needed by capture, settings and lifecycle workflows.

UI surfaces coordinate user intent and bounded feedback. Application workflows own capture sequencing and persistence policy. Capture backends own Windows acquisition. Imaging owns crop, annotation and PNG work. Shared infrastructure owns cross-cutting policies such as localization, lifecycle gates and shell-action failure classification.

## Capture engine

Screen capture prefers Windows.Graphics.Capture with Direct3D readback when the platform supports it. Expected platform, timeout or native acquisition failures can use the GDI compatibility monitor backend; caller cancellation is never converted into fallback work.

Captured frames carry validated physical-pixel dimensions, stride and BGRA8 data. Region editing operates on a frozen `CaptureFrame`; region geometry is clamped before crop and annotation. PNG encoding performs channel conversion and DEFLATE compression away from the WinUI thread. File output stages temporary data and publishes through a final move so incomplete output is not intentionally exposed as a completed capture.

Window Capture freezes the visible desktop before picker overlays are shown. Native window discovery supplies target geometry and z-order, while one overlay per display converts pointer coordinates using that display's DPI. Picker rendering failures propagate as failures rather than being reported as user cancellation.

Screen recording uses Windows.Graphics.Capture plus MediaStreamSource and MediaTranscoder to produce local H.264 MP4 output. Frame ownership is bounded to the latest pending frame plus encoder-owned in-flight frames, and recording teardown uses non-disposable async pulse coordination so Stop or failure cleanup cannot dispose a wait primitive under an active callback.

## Multi-monitor and DPI

Windows capture works in physical-pixel coordinates. Mixed DPI, monitors positioned left or above the primary display and negative virtual-desktop coordinates are normal topology, not error conditions.

Display discovery supplies physical bounds and per-monitor DPI. Region and Window surfaces are positioned against those native bounds and convert between logical UI coordinates and physical capture pixels only at explicit boundaries. Tests cover geometry and policy behavior; platform or driver failures remain controlled failure paths rather than crash assumptions.

## Persistence and local state

PNG and MP4 publication is local-first and staged before final publication. Recent captures are discovered from the local capture directory without a resident watcher or database. Preferences are local to the Windows account and are written atomically.

Setup and Portable contain architecture-specific self-contained payloads for x86, x64 and ARM64. Integrity manifests protect Portable payload reuse, while Setup validates installation ownership and path boundaries before replacing files.

## Browsers

Each browser variant is a small Manifest V3/WebExtension package. The popup sends explicit capture commands to a background coordinator. A content script owns region selection and bounded full-page stitching. Capture tokens plus sender tab and window identity prevent stale or foreign results from being accepted.

Full-page tiles are drawn incrementally into one bounded destination canvas and decoded tile resources are released after drawing to reduce peak memory pressure. Options state is loaded from extension-local storage before Settings controls become interactive, preventing initial-load and save races.

The maintained permission contract is intentionally bounded and contains no broad host access. Browser capture remains local-first and does not add remote runtime code, telemetry or automatic cloud upload.

## Related implementation guides

- [Window Capture](WINDOW-CAPTURE.md)
- [Image Pipeline](IMAGE-PIPELINE.md)
- [Tray & Lifecycle](TRAY-LIFECYCLE.md)
- [Performance & Stability](PERFORMANCE.md)
- [QA Matrix](QA-MATRIX.md)
