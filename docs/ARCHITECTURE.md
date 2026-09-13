# SNAPVERE Architecture

## Status

The current public release is **SNAPVERE 0.1.1**. The product has three deliberately separate runtime surfaces:

- a tray-first Windows capture application;
- a native Android 10+ companion;
- standalone browser extensions for Chrome, Edge, Opera and Firefox.

The platforms share product identity, local-first privacy principles, version/release governance and documentation, but they do **not** pretend to be one cross-platform codebase. Each runtime uses the native capture and lifecycle model appropriate to its platform.

Published historical tags/releases remain immutable. Post-release maintenance on `main` does not move or rewrite `v0.1.1`.

## Design goals

SNAPVERE prioritizes capture latency, physical-pixel correctness, mixed-DPI handling, bounded resource ownership, deterministic cleanup, local-first processing, minimal permissions, predictable startup/lifecycle behavior and a small understandable dependency surface.

The active cross-platform version contract is [`product-version.json`](../product-version.json). Product Contract CI validates that Windows, Android, browser manifests/store metadata and active documentation stay aligned to 0.1.1.

# Windows architecture

## Project boundaries

### Snapvere.App

WinUI 3 composition root and presentation layer.

- `App` owns dependency injection, UI-thread routing, runtime/visual probes and window lifetime.
- `CaptureCenterWindow` is the hidden capture coordinator, not a normal launcher dashboard.
- `TrayMenuWindow`, `OptionsWindow`, `LanguagePickerWindow` and `AboutWindow` are on-demand surfaces.
- `RegionCaptureWindow` owns interactive region selection and annotations.
- `WindowTargetPicker` coordinates DPI-aware target overlays.

Programmatic WinUI trees remain preferred for secondary windows because CI/runtime probes can materialize them without extra XAML resource-loading dependencies.

### Snapvere.Application

UI-independent workflows and local persistence:

- `RegionCaptureWorkflow`
- `WindowCaptureWorkflow`
- `ScreenCaptureWorkflow`
- `CaptureFileWriter`
- `CaptureHistoryService`
- `CapturePreferencesService`

Preferences and capture history remain local. The current line retains filesystem-policy/security handling in preference load, history enumeration and temporary-file cleanup so secondary local I/O failures do not unnecessarily terminate the UI path.

### Snapvere.Domain

Physical-pixel capture geometry and value objects without UI dependencies.

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

## Tray-first startup

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

Native tray/hotkey threads do not manipulate WinUI controls directly. Commands are marshalled through WinUI `DispatcherQueue`. The tray host negotiates `NOTIFYICON_VERSION_4`, handles Explorer/taskbar recreation and supports pointer/keyboard activation.

## Capture pipelines

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

WGC/D3D resources are created lazily for capture work. Caller cancellation and unexpected programming failures are not hidden behind unrelated fallback behavior.

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

Preview, selection and output derive from the same frozen frame.

## Coordinates and frame contract

Virtual-desktop coordinates may be negative. Display geometry uses physical bounds/effective DPI. WinUI logical pointer positions cross an explicit DPI conversion boundary before capture geometry.

`CaptureFrame` is validated BGRA8 data containing physical dimensions, stride, UTC timestamp and source identifier. Empty dimensions, invalid stride and undersized buffers are rejected before downstream processing.

## Local state

Preferences live at:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Writes use temporary-file + atomic move. Malformed/unreadable state falls back to safe defaults. Localization is static/in-process; English is the canonical fallback. Windows startup registration is per-user and owned by `StartupRegistrationService`.

# Android architecture

Android source lives under `android/` and is intentionally independent from the WinUI/.NET boundary.

Core components:

- `MainActivity` — responsive native home UI, explicit Capture/Open/Share/Delete and support/legal actions;
- `CaptureService` — foreground `mediaProjection` service owning one approved capture session;
- `CaptureBufferLayout` — pure validated RGBA row-layout arithmetic;
- Android resources — dark theme, English/Croatian strings and local vector/icon assets.

The Android manifest intentionally keeps `INTERNET` absent, cleartext disabled, backup disabled and `CaptureService` non-exported with `foregroundServiceType="mediaProjection"`.

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

Every capture uses a fresh consent token/projection instance. A five-second task-hide guard and seven-second first-frame guard bound ownership. Handler scheduling and image acquisition are checked. Resource teardown is idempotent/per-resource and capture ownership is service-instance-aware to prevent stale teardown races.

The RGBA path requires a 4-byte pixel stride, sufficient row stride, whole-pixel padding, overflow-safe arithmetic and enough ByteBuffer bytes for `rowStride × height`. JVM tests cover valid and invalid layouts.

## Android UI/UX boundary

Android shares SNAPVERE's dark identity but uses native Android layout behavior. The surface is vertically scrollable, system-inset aware, keeps at least 52 dp touch targets and stacks paired actions vertically on narrow displays or at font scale >= 1.25x.

System/provider failures for Capture/Open/Share/Delete/Website/Support/Privacy/Terms are converted into visible recovery state where practical instead of exposing raw internal exception text.

# Browser-extension architecture

Browser source lives under `ekstenzije/` in four self-contained variants:

```text
ekstenzije/chrome
ekstenzije/edge
ekstenzije/opera
ekstenzije/firefox
```

Chrome, Edge and Opera use Manifest V3 service-worker backgrounds. Firefox uses a Firefox-compatible MV3 `background.scripts` declaration. Shared runtime source is deliberately kept byte-identical across variants except for the expected manifest/Gecko metadata difference.

Core runtime pieces:

- `background.js` — capture orchestration, durable capture lock, visible capture, region/full-page coordination and download initiation;
- `capture.js` — page overlay/selection, full-page tile collection/stitching, local crop/Blob output and page-state restoration;
- `popup.*` — three user capture actions and status UI;
- `options.*` — local filename-prefix and save-location preferences;
- `_locales/en` + `_locales/hr` — dedicated locale catalogs;
- local 16/32/48/128 PNG icons.

## Browser capture flows

### Visible area

```text
popup request
    ↓
background acquires bounded storage-backed capture lock
    ↓
active tab/window validation
    ↓
browser captureVisibleTab API
    ↓
local PNG download
    ↓
release capture lock
```

### Region

```text
popup request
    ↓
persist token + tab/window identity
    ↓
inject local capture.js
    ↓
user selects region / Esc cancels
    ↓
overlay removed before screenshot
    ↓
viewport screenshot + local crop
    ↓
PNG download + cleanup
```

### Full page

The content helper measures the document, builds a bounded viewport tile plan, scrolls with paint settling, temporarily suppresses a bounded set of fixed/sticky elements after the first tile, receives local screenshot tiles, assembles them into a bounded canvas, downloads a local Blob and restores page state.

Explicit limits bound tile count, canvas dimension and total pixels. A watchdog restores page state if orchestration disappears unexpectedly.

## Browser permission boundary

The current extension contract requests exactly:

```text
activeTab
scripting
downloads
storage
```

There is no `<all_urls>` or broad `host_permissions` grant. Privileged/internal browser pages can remain unavailable for capture and are surfaced as controlled unsupported-page errors.

# Resource lifetime and local-first boundary

Heavy capture resources are demand-driven on all runtimes:

- Windows does not keep full-resolution/D3D capture resources resident solely for tray presence;
- Android creates projection/display/reader/thread resources only for an explicitly approved session;
- browser full-page tile/session state exists only for a bounded capture and is cleaned through success/error/watchdog paths.

No platform adds a first-party telemetry worker, automatic screenshot cloud-upload worker or continuous background capture loop in the current product contract.

# QA architecture

## Windows

Technical probes include `READY`, `TRAY_READY`, `REGION_OVERLAY_READY`, `WINDOW_OVERLAY_READY`, `SECONDARY_UI_READY` and normal-launch survival. CI renders six x64 UI surfaces and validates x86/x64/ARM64 payload construction plus Setup/Portable x64/x86 lifecycle.

ARM64 evidence on hosted x64 CI is cross-build/package evidence, not physical ARM64 runtime proof.

## Android

CI validates manifest privacy/version/service requirements, `lintDebug`, `lintRelease`, JVM tests, debug/release builds, APK signature/alignment and SHA-256. The public 0.1.1 APK remains transparently CI/debug-signed, not represented as Google Play production-signed.

## Browser extensions

Browser CI validates manifest/permission/source policy, cross-browser parity, EN/HR locales, icon dimensions, behavioral background smoke paths, store-readiness metadata and reproducible double packaging with SHA-256 verification.

Behavioral VM smoke tests exercise the real `background.js` state machine but are not represented as exhaustive browser GUI testing.

## Product contract

Product Contract CI cross-checks platform versions, Android EN/HR resource keys, browser metadata, release asset names, current-documentation version alignment and relative Markdown links.

# Packaging and public release architecture

## Windows

`SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe` each embed x86, x64 and ARM64 native payloads and select the compatible architecture automatically. Setup owns per-user install/update/uninstall. Portable uses a versioned cache with bounded/path-safe extraction, integrity verification, mutex protection and child-startup validation.

## Android

The public v0.1.1 `SNAPVERE.apk` is the validated CI/debug-signed APK. The release also publishes `SNAPVERE-Android-Source.zip`, generated from the validated tracked Android tree without build/cache/signing secrets.

## Browser extensions

The v0.1.1 release publishes deterministic ZIPs for Chrome, Edge, Opera and Firefox. GitHub release publication is separate from external browser-store review/signing and does not imply store approval.

## v0.1.1 public assets

The public GitHub Release contains exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

All final assets received locally calculated SHA-256 values and post-publication GitHub digest verification in the release workflow.

# Security/privacy boundary

Screenshot pixels, clipboard contents and user files are not routine diagnostic payloads. Capture is local-first. Windows package extraction is path-constrained and integrity-checked. Uninstall preserves `Pictures\SNAPVERE`. Android has no first-party Internet permission. Browser extensions have no broad host permission or first-party telemetry/cloud-upload runtime.

See [Security Policy](../SECURITY.md), [Privacy](PRIVACY.md), [Security & Performance 0.1.1](SECURITY-PERFORMANCE-0.1.1.md), [Android](ANDROID.md), [Browser Extensions](BROWSER-EXTENSIONS.md) and [QA Matrix](QA-MATRIX.md).

# Deliberately deferred

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

Browser distribution:

- external Chrome Web Store / Edge Add-ons / Opera Add-ons / Mozilla Add-ons publication and signing until authenticated publisher workflows are available.

Deferred functionality stays out of production UI/marketing until it is actually implemented and validated.
