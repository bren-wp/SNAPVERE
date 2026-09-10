# Capture Engine

## Status

SNAPVERE keeps capture acquisition behind explicit service contracts. Region and Screen Capture use a resilient monitor backend that prefers Windows.Graphics.Capture (WGC) and falls back to GDI for expected compatibility failures. Window Capture uses WGC directly because it targets a real HWND and must not silently degrade into a screen crop.

Presentation code does not call native capture APIs directly. `Snapvere.Application` workflows own capture orchestration and persistence.

## Supported Windows policy

The application minimum remains Windows 10 version 1809 / build 17763.

- **17763–19040:** GDI monitor compatibility path. Window Capture is unavailable because the production HWND/WGC path requires the newer capture contract used by SNAPVERE.
- **19041 and later:** WGC/D3D11 is preferred for Region/Screen monitor acquisition with GDI fallback; WGC Window Capture is available.

The application therefore does not raise its minimum OS simply because newer capture capabilities are unavailable on older supported Windows builds.

## Monitor backend policy

```text
IScreenCaptureService
    ↓
ResilientScreenCaptureService
    ├── preferred → WindowsGraphicsCaptureService
    │               Windows.Graphics.Capture + Direct3D 11
    └── fallback  → GdiScreenCaptureService
                    BitBlt + GetDIBits
```

The WGC/D3D backend is lazy. Tray-first application startup does not initialize the D3D device, capture item or frame pool; those resources are created only when a capture workflow requests them.

Fallback is deliberately narrow. Expected unsupported/platform/native/timeout acquisition failures may use GDI. Caller-requested cancellation is propagated and never converted into a second capture attempt. Invalid arguments and other programming failures are not broadly hidden by `catch (Exception)` fallback logic.

## WGC monitor acquisition

The monitor backend:

1. checks the supported Windows/build policy;
2. verifies `GraphicsCaptureSession.IsSupported()`;
3. resolves the native monitor handle from physical display bounds;
4. creates a D3D11 device with BGRA support;
5. projects the device to WinRT `IDirect3DDevice`;
6. creates a monitor `GraphicsCaptureItem` through native interop;
7. creates a free-threaded frame pool;
8. applies the requested cursor-capture state where supported;
9. starts the capture session;
10. waits for a bounded first frame;
11. obtains the `ID3D11Texture2D`;
12. copies to a CPU-readable staging texture;
13. maps row-pitched GPU memory into a BGRA8 byte buffer;
14. validates dimensions/content contract;
15. returns a validated `CaptureFrame`.

D3D/COM/WinRT objects are scoped to the capture operation and released when their ownership ends. Full-resolution frames are not retained by the long-lived tray/hotkey services.

## GDI compatibility acquisition

`GdiScreenCaptureService` captures a physical monitor rectangle with `BitBlt`, converts it to top-down 32-bit BGRA through `GetDIBits`, optionally draws the Windows cursor with hotspot correction, and normalizes alpha.

GDI is a monitor fallback, not a fake Window Capture implementation. A screen crop cannot reliably reproduce an occluded window and therefore is not substituted for a failed HWND capture.

## Window Capture

Window Capture uses the same D3D11/readback machinery but creates its capture item from the chosen HWND through `IGraphicsCaptureItemInterop.CreateForWindow`.

Before picker overlays exist, `Win32WindowDiscovery` snapshots visible capturable top-level windows in native Z-order. The discovery layer filters SNAPVERE's own process and unsuitable targets including invisible, cloaked, tool and invalid/empty windows. DWM extended-frame bounds are preferred when available.

`WindowTargetPicker` then freezes one desktop frame per active display and creates one DPI-aware overlay per monitor. Hover hit-testing is performed geometrically against the frozen Z-order list; it does not ask Windows which always-on-top picker window is under the pointer after the overlays have appeared.

A shared selected target is clipped/highlighted on every monitor it intersects, so windows spanning displays remain visually coherent even with mixed DPI or negative virtual-desktop coordinates.

After left-click selection, `WindowCaptureWorkflow` captures the HWND through WGC and persists the validated frame with `CaptureFileWriter`. Esc cancels without substituting another backend.

## Region Capture

`RegionCaptureWorkflow` freezes the primary display through `IScreenCaptureService`. The editor converts pointer/DIP interaction to physical pixels, crops the same frozen `CaptureFrame`, applies annotations in the image pipeline and then copies or saves the rendered result.

The desktop is not recaptured after selection. This preserves what the user saw in the frozen overlay and avoids time-of-selection drift.

## Screen Capture

`ScreenCaptureWorkflow` captures the primary display and writes the validated frame directly through the atomic PNG writer. Current public UI does not expose all-monitors or monitor-under-cursor choices because those product options are not yet implemented and release-gated.

## Cursor preference

Cursor capture is now a real local preference exposed in **Options / Preferences**.

`CapturePreferencesService` stores `IncludeCursorOnCapture` in `%LOCALAPPDATA%\SNAPVERE\settings.json`. Region, Window and Screen workflows combine the explicit request with that user preference before invoking the final capture backend.

WGC uses `GraphicsCaptureSession.IsCursorCaptureEnabled` where supported. GDI draws the cursor only when Windows reports it visible and its screen coordinates fall within the captured monitor. Window picker background/frozen targeting frames intentionally remain cursor-free because they are interaction surfaces, not final capture output.

## CaptureFrame contract

Every backend returns a validated `CaptureFrame` containing:

- physical width and height;
- explicit stride;
- BGRA8 pixel buffer;
- UTC capture timestamp;
- source identifier.

Validation rejects empty dimensions, invalid stride and undersized buffers before crop, annotation, clipboard or PNG operations.

## Persistence

`CaptureFileWriter` owns durable PNG persistence. Output is encoded to a temporary/staging path and then atomically moved to a collision-safe final filename under `Pictures\SNAPVERE`.

Recent Captures enumerates a bounded recent subset rather than scanning unrelated locations. Uninstall deliberately preserves capture files.

## Failure semantics

For resilient monitor capture, expected compatibility/acquisition failures may fall back to GDI, including supported `PlatformNotSupportedException`, timeout, COM/native and invalid WGC acquisition states defined by the service policy.

`OperationCanceledException` caused by the caller token propagates immediately. Window Capture surfaces WGC/platform acquisition failure rather than changing the meaning of the requested target.

## Protected content

SNAPVERE does not attempt to bypass DRM or operating-system capture restrictions. Protected, blocked or blank content is handled according to normal backend failure behavior; no implementation is designed to defeat Windows protection mechanisms.

## Automated validation

CI/release QA validates:

- x64 and x86 compilation;
- unit tests for geometry, persistence, backend fallback/cancellation and Window targeting;
- self-contained publish;
- Setup and Portable packaging;
- tray-first startup probe;
- Region editor materialization;
- Window picker materialization;
- normal installed and Portable startup survival;
- uninstall contract and cleanup.

Hosted CI runtime probes prove that product surfaces and package lifecycle materialize correctly. They are not treated as proof that an arbitrary protected or interactive end-user desktop can be captured on the hosted runner.
