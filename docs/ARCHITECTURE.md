# SNAPVERE Architecture

## Status

The current release line is **0.1.0**. SNAPVERE consists of a tray-first Windows capture application and a separate native Android 10+ companion. Published historical tags/releases are immutable and are not rewritten by later development.

Windows normal launch creates a hidden WinUI capture coordinator, global-hotkey host and notification-area host, then remains tray-first. Region, Window and Screen Capture are implemented. Tray, Options/Recent Captures, Language and About are secondary surfaces created on demand.

Android implements explicit user-approved full-screen capture through MediaProjection and MediaStore. It is not a port of the Windows top-level-window capture engine and does not claim Windows-only capabilities that Android does not provide.

## Design goals

SNAPVERE prioritizes capture latency, physical-pixel accuracy, mixed-DPI correctness, deterministic native resource cleanup, local-first privacy, bounded failure behavior, predictable tray/service startup and a small understandable dependency surface.

## Windows project boundaries

### Snapvere.App

WinUI 3 composition root and presentation layer.

- `App` owns dependency injection, UI-thread routing, runtime/visual probes and window lifetime.
- `CaptureCenterWindow` remains a hidden capture coordinator, not the normal-launch UI.
- `TrayMenuWindow`, `OptionsWindow`, `LanguagePickerWindow` and `AboutWindow` are on-demand user surfaces.
- `RegionCaptureWindow` owns interactive Region selection/annotation.
- `WindowTargetPicker` coordinates DPI-aware target overlays.

Programmatic WinUI trees remain preferred for secondary windows because they are exercised by package/runtime probes without additional XAML resource-loading dependencies.

### Snapvere.Application

UI-independent Windows workflows/persistence:

- `RegionCaptureWorkflow`
- `WindowCaptureWorkflow`
- `ScreenCaptureWorkflow`
- `CaptureFileWriter`
- `CaptureHistoryService`
- `CapturePreferencesService`

Preferences and capture history remain local. 0.1.0 additionally contains filesystem-policy/security failures in load/enumeration/temp-cleanup paths rather than allowing secondary local I/O cleanup faults to terminate the UI path.

### Snapvere.Domain

Physical-pixel capture geometry/value objects without UI dependencies.

### Snapvere.Capture

Windows acquisition/desktop integration:

- monitor/window discovery;
- DPI and virtual-desktop geometry;
- global hotkey host;
- Windows.Graphics.Capture / D3D11;
- GDI monitor compatibility backend;
- native window Z-order filtering and targeting.

### Snapvere.Imaging

Deterministic BGRA8 crop, annotation rendering and PNG encoding.

### Snapvere.Packaging / Snapvere.Setup / Snapvere.Portable

Guarded embedded-payload handling, architecture selection, per-user Setup lifecycle, same-Setup uninstall and Portable extraction/launch. Portable reusable cache content is verified against trusted architecture-specific SHA-256 manifests embedded in the host before execution.

### Snapvere.Shared

Small shared primitives such as localization and process-local language state.

## Windows tray-first startup

```text
Snapvere.exe
    ↓
startup diagnostics + DI
    ↓
hidden CaptureCenterWindow coordinator
    ↓
Win32 global-hotkey host
    ↓
Win32 notification-area icon host
    ↓
coordinator stays hidden while app remains resident
```

Native tray/hotkey threads do not manipulate WinUI controls directly. Commands are marshalled through WinUI `DispatcherQueue`. The tray host uses `NOTIFYICON_VERSION_4`, handles Explorer/taskbar recreation and retains pointer/keyboard activation support.

## Windows capture pipelines

### Region / Screen

```text
command
    ↓
ResilientScreenCaptureService
    ├── Windows.Graphics.Capture + D3D11
    └── expected monitor-acquisition failure → GDI fallback
    ↓
CaptureFrame (physical BGRA8)
    ↓
optional crop + annotation render
    ↓
CaptureFileWriter / Clipboard
```

WGC/D3D resources are created lazily for capture work. Caller cancellation and unexpected programming failures are not hidden by unrelated fallback work.

### Window

```text
Window command / Ctrl+Shift+2
    ↓
snapshot eligible top-level windows in Z-order
    ↓
freeze displays before overlays
    ↓
DPI-aware picker overlays
    ↓
geometric hit test against frozen snapshot
    ↓
WindowsGraphicsCaptureService.CreateForWindow
    ↓
local PNG
```

The picker does not depend on `WindowFromPoint` after SNAPVERE overlays exist, preventing self-selection.

### Region editor

```text
tray / Print Screen / Ctrl+Shift+1
    ↓
frozen frame
    ↓
physical-pixel selection / move / eight-handle resize
    ↓
Pen / Line / Arrow / Box / Highlight
    ↓
render annotations into selected frame
    ↓
Copy or Save
```

Preview/selection/output derive from the same frozen frame.

## Windows coordinates and frame contract

Virtual-desktop coordinates may be negative. Display geometry uses physical bounds/effective DPI. WinUI logical pointer positions cross an explicit DPI conversion boundary before capture geometry.

`CaptureFrame` is validated BGRA8 data containing physical dimensions, stride, UTC timestamp and source identifier. Empty dimensions, invalid stride and undersized buffers are rejected before downstream processing.

## Windows local state

Preferences live at:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Writes use temporary-file + atomic move. Malformed/unreadable state falls back to safe defaults. Localization is static/in-process; English is canonical fallback. Windows startup registration is per-user and owned by `StartupRegistrationService`.

## Android architecture

Android source lives under `android/` and is intentionally independent from the WinUI/.NET application boundary.

Core components:

- `MainActivity` — responsive native home UI, explicit Capture/Open/Share/Delete and support/legal actions;
- `CaptureService` — foreground `mediaProjection` service that owns one approved capture session;
- `CaptureBufferLayout` — pure validated RGBA row-layout arithmetic;
- Android resources — dark theme, English/Croatian strings and native vector/icon assets.

Android manifest policy keeps `INTERNET` absent, cleartext disabled, backup disabled and `CaptureService` non-exported with `foregroundServiceType="mediaProjection"`.

## Android capture pipeline

```text
explicit Capture tap
    ↓
Android MediaProjection consent
    ↓
start foreground CaptureService
    ↓
move Activity behind target screen
    ↓
MainActivity.onStop() confirms hidden state
    ↓
VirtualDisplay + RGBA_8888 ImageReader
    ↓
bounded first-frame wait
    ↓
validate pixel stride / row stride / padding / buffer length
    ↓
bitmap conversion
    ↓
MediaStore PNG → Pictures/SNAPVERE
    ↓
Open / Share / Delete
```

Each capture uses a fresh consent token/projection instance. A five-second task-hide guard and seven-second first-frame guard bound ownership. Handler scheduling and image acquisition are checked. Resource teardown is idempotent/per-resource and capture ownership is service-instance-aware to prevent stale teardown races.

The RGBA buffer path requires a 4-byte pixel stride, sufficient row stride, whole-pixel padding, overflow-safe arithmetic and enough ByteBuffer bytes for `rowStride × height`. JVM unit tests cover valid and invalid layouts.

## Android UI/UX boundary

The Android surface shares SNAPVERE's dark identity but uses native Android layout behavior. It is vertically scrollable, system-inset aware, keeps at least 52 dp touch targets and stacks paired actions vertically on narrow displays or font scale >= 1.25x.

System/provider failures for Capture/Open/Share/Delete/Website/Support/Privacy/Terms are converted into visible recovery state where practical instead of raw exception text escaping the Activity.

## Resource lifetime

Heavy capture resources are demand-driven on both platforms. Windows does not keep full-resolution frames/D3D capture resources resident solely for tray operation. Android creates MediaProjection, VirtualDisplay, ImageReader and capture thread only for an explicit approved session and stops them after success/failure.

Neither platform adds a telemetry worker, cloud-upload worker or continuous capture loop.

## Runtime and visual QA

Windows technical probes include `READY`, `TRAY_READY`, `REGION_OVERLAY_READY`, `WINDOW_OVERLAY_READY`, `SECONDARY_UI_READY` and normal-launch survival. Visual QA captures six rendered x64 surfaces:

```text
region-capture.png
window-capture.png
tray-menu.png
options.png
language.png
about.png
```

CI rejects empty/unexpectedly small output and records dimensions, byte sizes and SHA-256.

Android CI validates manifest privacy/version/service requirements, `lintDebug`, `lintRelease`, JVM tests, debug/release builds, debug APK signature/alignment and SHA-256. Public-release automation additionally validates stable Android release signing and source-archive structure.

## Packaging and public release architecture

### Windows

`SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe` each embed x86, x64 and ARM64 native payloads and select the compatible architecture automatically. Setup owns per-user install/update/uninstall. Portable uses a versioned cache with bounded/path-safe extraction, integrity verification, mutex protection and child-startup validation.

Hosted x64 CI executes universal x64/x86 package lifecycle. ARM64 is cross-build/package validation, not physical ARM64 runtime proof.

### Android

The public APK is built from the minified/shrunk release variant, ZIP-aligned and signed with SNAPVERE's stable private release identity. Signing material remains outside Git source. Release fails before tag creation if signing cannot be verified.

`SNAPVERE-Android-Source.zip` is generated directly from the exact validated `android/` Git tree and excludes generated build/cache output and signing secrets.

### v0.1.0 release assets

The final GitHub Release must contain exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

All four assets receive locally calculated SHA-256 values and post-publication GitHub digest verification before the release is accepted.

## Security/privacy boundary

Screenshot pixels, clipboard contents and user files are not routine diagnostic payloads. Capture is local-first. Package extraction is path-constrained/size-bounded. Uninstall preserves `Pictures\SNAPVERE`. Android contains no first-party network capture path.

See `SECURITY.md`, `docs/SECURITY-PERFORMANCE-0.1.0.md` and `docs/ANDROID.md`.

## Deliberately deferred

Windows:

- coordinated cross-monitor Region composition;
- text, blur/pixelate and numbered-step annotations;
- scrolling capture;
- expanded history/favorites/pin-to-screen;
- OCR;
- automatic updater;
- Authenticode signing.

Android:

- Windows-style arbitrary top-level Window Capture;
- Region-selection/annotation parity with the desktop editor.

Deferred functionality stays out of production UI/documentation until implemented and release-gated.
