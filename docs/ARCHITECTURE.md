# SNAPVERE Architecture

## Status

The current `main` line is post-**v0.0.8** hardening. The latest published release is v0.0.8; later commits on `main` are not a new release until a separately validated release workflow publishes a new tag and assets.

Normal launch is tray-first: the application creates its WinUI capture coordinator without showing it, starts the global-hotkey and notification-area hosts, and remains available in the background. Region, Window and Screen Capture are implemented. Tray, Options/Recent Captures, Language and About are user-facing secondary surfaces created only when needed.

Published release tags and assets are treated as immutable. Development after v0.0.8 does not rewrite the v0.0.8 release.

## Design goals

SNAPVERE prioritizes capture latency, physical-pixel accuracy, mixed-DPI correctness, deterministic native resource cleanup, local-first privacy, predictable tray startup and a small understandable dependency surface.

## Project boundaries

### Snapvere.App

WinUI 3 composition root and presentation layer.

- `App` owns dependency injection, UI-thread command routing, runtime/visual probes and window lifetime.
- `CaptureCenterWindow` is retained as a hidden capture coordinator. It is not the normal-launch user experience.
- `TrayMenuWindow` is the compact branded right-click command surface.
- `OptionsWindow` exposes only implemented local preferences and recent captures.
- `LanguagePickerWindow` persists the selected built-in language locally.
- `AboutWindow` is a factual secondary product-information surface.
- `RegionCaptureWindow` owns interactive Region selection and inline annotations.
- `WindowTargetPicker` coordinates one frozen picker overlay per monitor.

Programmatic WinUI trees are preferred for these secondary windows because they have proven more stable than additional Window XAML resource paths in package/runtime probes.

### Snapvere.Application

UI-independent application workflows and persistence coordination.

- `RegionCaptureWorkflow`
- `WindowCaptureWorkflow`
- `ScreenCaptureWorkflow`
- `CaptureFileWriter`
- `CaptureHistoryService`
- `CapturePreferencesService`

Capture preferences are local-only and are consumed by workflows rather than by native backends directly.

### Snapvere.Domain

Stable pixel geometry and capture-domain value objects. Capture boundaries are expressed explicitly in physical pixels so logical WinUI coordinates cannot silently leak into backend geometry.

### Snapvere.Capture

Windows capture and desktop-integration primitives:

- monitor/window discovery;
- DPI conversion and virtual-desktop geometry;
- global hotkey host;
- WGC/D3D11 capture backend;
- GDI compatibility monitor backend;
- window Z-order filtering and targeting.

### Snapvere.Imaging

Deterministic image operations: BGRA8 crop, annotation rendering and PNG encoding. Region annotations are rendered into the resulting frame rather than existing only as visual overlay controls.

### Snapvere.Packaging / Snapvere.Setup / Snapvere.Portable

Guarded embedded-payload handling, universal architecture selection, per-user Setup lifecycle, Installed apps registration, same-Setup uninstall maintenance mode and single-file Portable extraction/launch.

### Snapvere.Shared

Small cross-cutting primitives including the built-in localization catalog and process-local language state. Product workflows and platform behavior remain in their owning layers.

## Tray-first startup architecture

```text
Snapvere.exe
    ↓
initialize diagnostics + DI services
    ↓
create hidden CaptureCenterWindow coordinator
    ↓
start Win32 global-hotkey host
    ↓
start Win32 notification-area icon host
    ↓
remain alive with coordinator hidden
```

Normal launch must not activate or flash the Capture Center.

The native tray message thread never manipulates WinUI controls directly. It raises a `TrayCommand`; `App` marshals that command through the WinUI `DispatcherQueue` before creating/activating flyouts, options windows or capture overlays.

## Tray command flow

```text
left-click tray
    → WM_LBUTTONUP
    → TrayCommand.RegionCapture
    → UI DispatcherQueue
    → Region Capture

right-click tray
    → TrayCommand.ShowMenu
    → UI DispatcherQueue
    → TrayMenuWindow
```

The tray service also handles notification icon lifetime and Explorer/taskbar recreation. Single-click behavior is protected from duplicate double-click activation.

## Monitor capture pipeline

```text
Region / Screen workflow
    ↓
ResilientScreenCaptureService
    ├── preferred: WindowsGraphicsCaptureService
    │   Windows.Graphics.Capture + D3D11 readback
    └── fallback: GdiScreenCaptureService
        BitBlt + GetDIBits
    ↓
CaptureFrame (physical BGRA8)
    ↓
optional Region crop + annotation render
    ↓
clipboard or CaptureFileWriter
    ↓
Pictures\SNAPVERE
```

WGC/D3D resources are created lazily when a capture is requested; the tray-first startup path does not initialize the GPU capture stack merely to stay resident.

Expected unsupported/platform/native/timeout monitor-capture failures may fall back to GDI. Caller cancellation and unexpected programmer failures are propagated rather than hidden behind fallback.

## Window Capture pipeline

```text
Window command / Ctrl+Shift+2
    ↓
snapshot capturable top-level windows in native Z-order
    ↓
freeze each display before overlays appear
    ↓
show one DPI-aware picker overlay per monitor
    ↓
geometric hit-test against frozen Z-order snapshot
    ↓
left-click selected HWND / Esc cancel
    ↓
WindowsGraphicsCaptureService.CreateForWindow
    ↓
CaptureFileWriter → Pictures\SNAPVERE
```

The picker does not depend on `WindowFromPoint` after SNAPVERE's always-on-top overlays exist, preventing the overlay itself from becoming the selected target.

## Region Capture pipeline

```text
tray left-click / Print Screen / Ctrl+Shift+1
    ↓
freeze primary display
    ↓
RegionCaptureWindow
    ↓
physical-pixel drag / move / eight-handle resize
    ↓
Pen / Line / Arrow / Box / Highlight annotations
    ↓
render annotations into selected frozen frame
    ↓
Copy or Save
```

The preview, selection crop and output all derive from the same frozen frame. The desktop is not recaptured after the user makes a selection.

## Coordinate systems

Windows virtual-desktop coordinates may be negative. Every display owns physical bounds plus effective DPI. WinUI pointer coordinates cross an explicit `DpiCoordinateTransformer` boundary before entering capture geometry.

Window picker overlays use each monitor's own transform. Region Capture remains intentionally single-display today; coordinated cross-monitor Region composition is still deferred.

## Capture frame contract

`CaptureFrame` is a validated BGRA8 buffer containing physical width/height, explicit stride, UTC timestamp and source identifier. Empty dimensions, invalid stride and undersized buffers are rejected before downstream work.

## Local settings and localization architecture

`CapturePreferencesService` stores implemented preferences under:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Current persisted state includes cursor composition preference and language code. Writes use a temporary file followed by an atomic replacement/move. Malformed or unreadable settings fall back to safe defaults.

`SnapvereLocalization` uses a built-in static catalog with English as the canonical fallback. Croatian contains dedicated strings for the current capture and secondary UI surfaces. Localization performs no network translation calls, file watching or background polling.

`StartupRegistrationService` owns the per-user Windows `Run` registration. Installed builds register the installed `Snapvere.exe`; Portable builds receive the stable Portable launcher path from the launcher so a startup entry never points at the temporary extraction cache.

## Resource lifetime

Native GDI objects, HWND/message hosts, tray icons, D3D devices/textures/frame pools, capture frames, file streams and temporary files require deterministic ownership. Long-lived tray/hotkey services must not retain full-resolution capture frames.

## Startup, runtime and visual probes

Package QA separates technical probes from the real normal-launch contract:

1. `READY` — explicitly activates the hidden coordinator only for a technical WinUI construction probe.
2. `TRAY_READY` — proves services, hotkey host and tray host initialized while the Capture Center remained hidden.
3. `REGION_OVERLAY_READY` — proves the Region editor materialized.
4. `WINDOW_OVERLAY_READY` — proves the Window picker materialized.
5. `SECONDARY_UI_READY` — sequentially materializes Tray, Options, Language and About surfaces.
6. normal-launch survival — proves installed and Portable tray-first processes remain alive rather than crashing immediately.

In addition to marker-based materialization, `eng/Capture-SnapvereVisualQa.ps1` launches the real x64 application and captures six rendered PNGs:

```text
region-capture.png
window-capture.png
tray-menu.png
options.png
language.png
about.png
```

The visual gate rejects a visually empty frame and unexpectedly small PNG output. It also writes a manifest containing surface title, pixel dimensions, byte size and SHA-256 digest. GitHub Actions uploads those files as a short-lived CI artifact.

The tray-only probe is the authoritative normal-startup model; visible-main-window activation is not a normal-launch requirement. A rendered-UI snapshot proves the surface actually painted on the hosted Windows runner, but it is not a substitute for end-user hardware testing or protected-content capture testing.

## Universal packaging architecture

Starting with v0.0.7, the public release contract contains exactly two user-facing executables:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Each host is built as an x86-compatible Windows executable and embeds native application payloads for x86, x64 and ARM64. At runtime the shared architecture resolver selects the compatible native payload; users do not choose an architecture-specific download.

Setup is per-user and owns install/update/repair-style replacement and uninstall through the same installed `SNAPVERE-Setup.exe`. Uninstall validates the installation marker before destructive removal and removes a Windows startup registration only when that registration points exactly to the validated installed `Snapvere.exe`.

Portable is a single-file launcher with a versioned temporary cache, bounded/path-safe extraction, mutex protection, stale-cache cleanup and child-startup validation. The launcher does not report normal-startup success if the child exits immediately.

CI cross-builds all three application payload architectures. Hosted x64 runners execute the universal Setup/Portable lifecycle with x64 and x86 payload selection. ARM64 is package/cross-build validated there but is not represented as a real ARM64 hardware runtime test.

## Security and privacy boundaries

Screenshot pixels, clipboard contents and user files are not routine log payloads. Core capture has no telemetry, cloud-upload or credential dependency. Startup diagnostics remain local. Package extraction is path-constrained and size-bounded, and uninstall never removes `Pictures\SNAPVERE` captures.

## Deliberately deferred

- coordinated cross-monitor Region selection/composition;
- text, blur/pixelate and numbered-step annotations;
- scrolling capture;
- expanded History/favorites/Pin to Screen;
- OCR;
- automatic updater;
- Authenticode signing.

Deferred functionality must remain absent from product UI until it is implemented and release-gated.
