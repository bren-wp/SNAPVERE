# SNAPVERE 0.1.1 Architecture

## Windows

The Windows product is split into UI, application workflows, capture backends, imaging and shared infrastructure. The app is tray-first and uses dependency injection only for long-lived services actually needed by capture/settings workflows.

Screen capture prefers Windows.Graphics.Capture with Direct3D readback and uses a GDI compatibility path for expected acquisition failures. Region editing works against a frozen `CaptureFrame`. PNG encoding is local and cancellation-aware. Capture file writes are staged before final move.

Setup and Portable contain architecture-specific self-contained payloads for x86, x64 and ARM64. Integrity manifests protect portable payload reuse.

## Browsers

Each browser variant is a small Manifest V3/WebExtension package. The popup sends explicit capture commands to a background coordinator. A content script owns region selection and bounded full-page stitching. Capture tokens and sender tab/window identity prevent stale results from being accepted.

Full-page tiles are drawn incrementally into one bounded canvas and released after draw to reduce peak memory pressure.
