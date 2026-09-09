# SNAPVERE Architecture

## Status

The 0.0.2 capture foundation is implemented and release-gated. Screen Capture, primary-display Region Capture, local PNG persistence, recent capture discovery, global hotkeys, tray integration, Setup and Portable packaging are working. Planned features remain explicitly separated from implemented functionality.

## Design goals

SNAPVERE prioritizes capture latency, physical-pixel accuracy, mixed-DPI correctness, deterministic native resource cleanup, local-first privacy, predictable Windows startup and a small understandable dependency surface.

## Project boundaries

### Snapvere.App
WinUI 3 composition root and presentation layer. `CaptureCenterWindow` is the production startup shell. It uses a standard Windows title bar and programmatic WinUI controls to minimize startup resource/parser dependencies. Region selection remains isolated in `RegionCaptureWindow`.

### Snapvere.Application
Application workflows and persistence coordination. It connects capture acquisition, crop/encode operations, file naming, atomic writes and filesystem-backed capture history without depending on WinUI.

### Snapvere.Domain
Stable value objects and product-domain types. Pixel geometry is represented explicitly to avoid accidental mixing of logical and physical coordinates.

### Snapvere.Capture
Display discovery, physical/logical DPI transforms, capture contracts, Region selection geometry, global hotkeys and the current Windows capture backend. The production 0.0.2 acquisition path is the GDI compatibility backend; Windows.Graphics.Capture/D3D remains the intended future primary backend.

### Snapvere.Imaging
Deterministic PNG encoding, pixel-accurate cropping and image-pipeline primitives. UI-independent by design.

### Snapvere.Packaging / Snapvere.Setup / Snapvere.Portable
Embedded payload validation, per-user Setup lifecycle, uninstall maintenance mode and single-file Portable extraction/launch. Packaging code is isolated from capture/runtime logic.

### Snapvere.Shared
Small cross-cutting primitives only. It must not become a dumping ground for unrelated helpers.

## Current capture pipeline

```text
Capture Center / hotkey / tray command
        ↓
Snapvere.Application workflow
        ↓
Win32 display discovery + physical pixel geometry
        ↓
GDI compatibility acquisition
        ↓
CaptureFrame (validated BGRA8 + stride)
        ↓
optional exact Region crop
        ↓
deterministic PNG encoder
        ↓
CaptureFileWriter
        ↓
temporary file + atomic move
        ↓
Pictures\SNAPVERE
```

## Region Capture pipeline

```text
prepare primary-display frozen frame
        ↓
hide Capture Center
        ↓
RegionCaptureWindow preview
        ↓
selection geometry in physical pixels
        ↓
move / resize / keyboard nudge
        ↓
commit exact rectangle
        ↓
crop the same frozen frame
        ↓
atomic PNG save
```

The selected output therefore comes from the exact frozen source shown during selection rather than a second screen acquisition.

## Coordinate systems

All capture boundaries are ultimately expressed in physical pixels. Presentation may use logical coordinates, but every boundary crossing uses an explicit DPI transform. Virtual desktop coordinates may be negative and no code assumes the primary monitor starts at the virtual origin.

`DpiCoordinateTransformer` and geometry tests cover 100%, 125%, 150%, 175%, 200%, mixed-axis DPI, negative coordinates, reverse drags, minimum Region sizes and physical-pixel keyboard movement.

## Capture frame contract

`CaptureFrame` represents a validated BGRA8 buffer with dimensions, stride, timestamp and optional source id. Validation rejects empty dimensions, undersized stride and undersized buffers before frames enter downstream processing.

## Resource lifetime

Native GDI objects, monitor/device handles, hotkey windows, tray windows, icon resources, file streams and temporary files require deterministic ownership. Long-lived desktop integration services must not retain full-resolution capture frames.

Future Windows.Graphics.Capture/Direct3D objects will follow the same deterministic ownership rule when that backend is introduced.

## Threading

UI-thread work is limited to WinUI presentation and APIs that require it. Native global hotkeys and tray integration use isolated message threads. Capture and persistence workflows expose asynchronous boundaries where useful without moving WinUI objects across apartments.

## Startup architecture

The application initializes diagnostics before WinUI composition, constructs dependency injection services, creates `CaptureCenterWindow`, activates it, then starts global hotkey and tray hosts.

Release QA has two distinct startup checks:

1. an activated-window READY probe, proving the WinUI main window was actually constructed and activated;
2. a normal-launch survival gate, proving the process remains alive after deferred rendering and desktop-integration startup.

This distinction prevents a package from being published merely because process creation succeeded.

## Security and privacy boundaries

Screenshot pixels, clipboard contents, OCR text and file contents are sensitive. They are not valid structured-log payloads. Startup diagnostics record stage and exception metadata only. Temporary package extraction is path-constrained and size-bounded. Network functionality for future licensing/update services remains architecturally separate from user image data.

## Planned next implementation

1. Windows.Graphics.Capture/D3D primary acquisition backend with compatibility fallback
2. coordinated multi-monitor Region Capture
3. Window Capture and smart targeting
4. clipboard Quick Actions
5. annotation Editor
6. scrolling capture
7. OCR, Pin to Screen and expanded History management
8. signed update pipeline
