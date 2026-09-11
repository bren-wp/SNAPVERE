# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

**Capture. Edit. Done. — fast, local-first screen capture for Windows and Android by Brendigo.**

[Hrvatski README](README.hr.md) · [Official product site](https://snapvere.com) · [Support](mailto:info@snapvere.com) · [Developer: Brendigo](https://brendigo.com)

SNAPVERE is a tray-first Windows screenshot application focused on fast Region, Window and Screen capture, lightweight annotation and local PNG output. English is the default product language; Croatian and more than 20 additional languages are available from the in-app Language picker.

> **v0.0.9 is the current release milestone being validated.** Published historical tags, releases and assets are immutable and are never rewritten by later development.

## Capture controls

| Action | Primary input | Alternate |
| --- | --- | --- |
| Region Capture | Left-click tray or **Print Screen** | `Ctrl+Shift+1` |
| Window Capture | Right-click tray → Capture window | `Ctrl+Shift+2` |
| Screen Capture | Right-click tray → Capture screen | `Ctrl+Shift+3` |
| Settings / recent captures | Right-click tray | — |
| Language | Tray language control or Options | — |
| About / Exit | Right-click tray | — |

Scrolling Capture does not reserve a global shortcut until that workflow is actually implemented.

## Save location

Region, Window and Screen captures are first completed safely as PNG files in the normal SNAPVERE capture location. After a successful save, SNAPVERE can open the native Windows **Save As** picker so the user chooses the final folder and filename.

Cancelling the picker keeps the completed PNG in `Pictures\SNAPVERE`, so a capture is not lost. Relocation uses asynchronous copying, destination-local staging, final atomic replacement, PNG-only destination validation and Windows file-identity checks. Alias or uncertain-identity cases preserve the source as a recovery copy. Clipboard-only Copy remains unchanged.

See [Save location](docs/SAVE-LOCATION.md).

## Region Capture

Implemented behavior includes a frozen frame, physical-pixel drag selection, move/resize, eight handles, live dimensions, Pen, Line, Arrow, Box and Highlight tools, four annotation colors, Undo, `Ctrl+Z`, `Ctrl+C`, Copy, Save, Enter/double-click save and Esc cancel. Annotations are rendered into the final PNG.

## Window Capture

SNAPVERE discovers visible top-level windows before overlays appear, uses DWM extended-frame bounds, filters SNAPVERE/tool/cloaked/invisible windows, renders DPI-aware frozen picker surfaces and performs the final window acquisition with Windows.Graphics.Capture `CreateForWindow`. Window Capture does not silently fall back to a screen crop.

## Screen Capture

Windows.Graphics.Capture + Direct3D 11 is preferred where supported. Expected monitor-acquisition failures can fall back to the resilient GDI monitor path. Capture resources are lazy and are not initialized merely because SNAPVERE is idle in the tray.

## Windows tray reliability and accessibility

The native notification-area host uses the modern `NOTIFYICON_VERSION_4` callback contract. SNAPVERE calls `NIM_SETVERSION` after every tray-icon add, including Explorer/taskbar recreation, preserves the normal tooltip with `NIF_SHOWTIP`, decodes callback events from the low word required by the version-4 protocol, and handles keyboard selection/context-menu notifications in addition to pointer input. Region capture remains debounced so duplicate activation notifications cannot start duplicate workflows.

## Android

SNAPVERE also ships a native Android 10+ companion built around Android's official MediaProjection model. It uses the same local-first privacy contract and the same core dark SNAPVERE design tokens as the Windows distribution: near-black canvas, layered dark surfaces, violet `#7C6CFF` accent and green `#45D6A2` success state.

The Android application provides user-approved full-screen capture, local PNG storage in `Pictures/SNAPVERE`, validated Open / Share / Delete actions for the latest capture, English and Croatian UI resources, and user-initiated Website / Support / Privacy / Terms actions. It requests no `INTERNET` permission and contains no account, telemetry, analytics or cloud-upload client.

For v0.0.9 the capture lifecycle is hardened with separate bounded task-hide and frame-delivery failure guards, exception-safe per-resource teardown, guarded `ImageReader.acquireLatestImage()`, validated row/pixel stride before bitmap allocation, safe MediaStore cleanup and deterministic release of the process-local capture lock. A stalled display producer therefore fails and cleans up instead of leaving capture permanently active.

The Android UI also stacks paired actions vertically on narrow screens or at 1.25x+ font scale, disables OEM `forceDark` transformation of the already-dark theme and contains system/provider failures for Capture, Open, Share, Delete, Website, Support, Privacy and Terms so those actions surface status instead of terminating the Activity.

Android CI validates the manifest privacy/service contract, debug and release lint, JVM unit tests, debug and release builds, APK signature, ZIP alignment and SHA-256, then publishes the installable debug APK as a GitHub Actions artifact for the exact source commit. Generated APK binaries are deliberately not committed to Git.

Android 14+ requires fresh user consent for each MediaProjection session and a single `createVirtualDisplay()` use per projection. SNAPVERE follows that contract and registers `MediaProjection.Callback.onStop()` for controlled resource release.

Android does not expose the same general top-level-window capture primitive used by SNAPVERE on Windows, so Windows-style Window Capture is not claimed on Android. See [Android application](docs/ANDROID.md) and [Android source/build guide](android/README.md).

## About and support

The About surface exposes user-initiated destinations for:

- official product site: `https://snapvere.com`;
- support: `info@snapvere.com`;
- Privacy: `https://snapvere.com/privacy`;
- Terms: `https://snapvere.com/terms`;
- developer: `https://brendigo.com`.

Support, Privacy and Terms use the shared localization catalog. If Windows has no registered mail client, the support action falls back to copying `info@snapvere.com` to the local clipboard. About content is scrollable so Windows text scaling does not make the new controls unreachable. The desktop application does not prefetch these destinations.

## Languages

English (`en`) is the canonical default and fallback. The built-in language catalog exposes 28 choices, including Croatian (`hr`). Languages without a dedicated translation for a particular string fall back to canonical English rather than displaying an unknown resource key.

Language preference is stored locally in:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

The localization layer uses static in-process tables only: no translation API, polling service, network dependency or background language worker.

## Options and defaults

Implemented preferences include **Start SNAPVERE with Windows**, **Include cursor on capture**, and **Language**. Windows startup is per-user and launches the same tray-first application without a dashboard window.

## Security posture for v0.0.9

Static review of the current desktop codebase found no WebView/WebView2, HTML renderer, JavaScript execution path or first-party HTTP/socket client. Browser-style XSS is therefore not an applicable runtime surface in this release line. This is not a claim that software can be guaranteed free of every vulnerability.

v0.0.9 hardening includes:

- NuGet audit for direct and transitive dependencies at `low` severity and above;
- `NU1901`–`NU1904` treated as build failures;
- GitHub Actions pinned to full commit SHAs in CI/release automation;
- non-persistent repository credentials for ordinary CI checkout;
- weekly Dependabot checks for NuGet and GitHub Actions;
- exact-or-descendant path-boundary validation for protected Setup paths;
- bounded random staging names for embedded ZIP extraction while retaining traversal and expanded-size protections;
- architecture-specific embedded SHA-256 manifests for reusable Portable payload-cache verification before execution;
- invalid/missing/modified/unexpected/reparse-point Portable cache content triggers transactional rebuild and revalidation;
- Android manifest CI forbids `INTERNET`, cleartext traffic and exported capture service regressions;
- Android frame/cleanup hardening prevents stuck MediaProjection ownership after timeout or cleanup faults;
- no telemetry, cloud-upload client, remote command channel or automatic updater in v0.0.9.

SNAPVERE is not represented as a sandbox against arbitrary malicious code already executing as the same Windows user. See [Security Policy](SECURITY.md) and [v0.0.9 security/performance hardening](docs/SECURITY-PERFORMANCE-0.0.9.md).

## Performance and stability

SNAPVERE is designed for low idle overhead and bounded failure behavior:

- tray and global-hotkey hosts block on Win32 message loops rather than periodic application polling;
- capture/D3D resources are created for capture work instead of remaining resident solely for tray operation;
- secondary windows are on-demand;
- recent-capture discovery is bounded and local; v0.0.9 removes a redundant per-file metadata refresh;
- Portable integrity validation performs one sequential SHA-256 read of cached files rather than re-decompressing the embedded payload merely to validate reuse;
- Android capture uses bounded handoff/frame waits and releases resources independently when a platform cleanup call fails;
- no telemetry worker, language network worker, file watcher or idle capture loop is added.

No fixed CPU/RAM percentage is promised because Windows version, DPI, monitor count, graphics drivers, Android OEM behavior and active capture/editor sessions materially affect resource use.

## Universal packaging

The public Windows release contract contains exactly two downloads:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Each host embeds native application payloads for x86, x64 and ARM64 and selects a compatible payload automatically. The application target remains Windows 10 version 1809 / build 17763 or later; WGC-dependent capture paths require Windows 10 version 2004 / build 19041 or later.

SNAPVERE intentionally installs no separate uninstaller executable. Windows Installed apps points to the installed `SNAPVERE-Setup.exe --uninstall`; the same Setup binary validates the installation before removal and preserves user screenshots.

## Automated QA

Windows GitHub Actions builds/tests x64 and x86 and cross-builds ARM64. The universal package gate additionally validates all three embedded payloads and integrity manifests, real rendered WinUI surfaces, the exact two-file public package contract, and x64/x86 Setup/Portable lifecycle and tray-first behavior.

The visual-QA pipeline captures Region, Window, Tray, Options, Language and About surfaces. v0.0.9 hardens capture provenance after a hosted runner desktop was discovered in an earlier successful-main Region baseline. Visual regression thresholds are not lowered to hide this issue.

Android GitHub Actions separately validates the privacy/service manifest contract, `lintDebug`, `lintRelease`, `testDebugUnitTest`, debug/release APK builds, APK signing, ZIP alignment and SHA-256 artifact generation. Capture-buffer layout arithmetic has direct JVM unit coverage.

ARM64 Windows validation on the hosted x64 runner is cross-build/package evidence, not a real ARM64 hardware runtime test. A green Android CI is compile/lint/unit/package evidence, not a claim of runtime testing on every physical OEM device.

## Architecture

```text
Tray / Print Screen / hotkeys
    ↓
Hidden WinUI runtime coordinator
    ↓
Snapvere.Application workflows
    ├── Region / Screen → resilient monitor capture
    └── Window → geometric picker → WGC CreateForWindow
    ↓
CaptureFrame (physical BGRA8 pixels)
    ↓
Crop / annotation render / PNG encode
    ↓
Safe default PNG → optional Save As relocation
                 └→ Windows Clipboard for Copy
```

Android uses a separate native flow:

```text
Explicit Capture tap
    ↓
Android MediaProjection consent
    ↓
Foreground mediaProjection service
    ↓
Activity hidden confirmation
    ↓
VirtualDisplay + ImageReader + bounded first-frame wait
    ↓
Stride validation / bitmap conversion
    ↓
MediaStore PNG → Open / Share / Delete
```

## Documentation

English documentation lives in [`docs/`](docs/) and Croatian documentation in [`docs/hr/`](docs/hr/).

Key documents: [Architecture](docs/ARCHITECTURE.md), [Tray UX](docs/TRAY-UX.md), [Capture engine](docs/CAPTURE-ENGINE.md), [Region Capture](docs/REGION-CAPTURE.md), [Window Capture](docs/WINDOW-CAPTURE.md), [Save location](docs/SAVE-LOCATION.md), [Settings](docs/SETTINGS.md), [Security/performance 0.0.9](docs/SECURITY-PERFORMANCE-0.0.9.md), [Installation](docs/INSTALLATION.md), [Branding](docs/BRANDING.md), [Image pipeline](docs/IMAGE-PIPELINE.md) and [Android application](docs/ANDROID.md).

## Technology

- C# / .NET 10
- WinUI 3 / Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11
- Win32 / DWM / GDI interoperability
- Java 17 / native Android APIs / MediaProjection / MediaStore
- deterministic builds, nullable/analyzer enforcement and central NuGet management
- xUnit + JUnit 4 + GitHub Actions

## Diagnostics

Local Windows startup diagnostics:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Screenshot pixels are not intentionally written to the startup log.

## License and ownership

**SNAPVERE 0.0.7 and later are distributed under the SNAPVERE Commercial Software License Agreement included in [`LICENSE`](LICENSE).** SNAPVERE is the product brand. Brendigo is the developer and publisher. The official product website is **snapvere.com** and support is **info@snapvere.com**.

---

**SNAPVERE — Capture. Edit. Done.**  
Developed and published by **Brendigo** · [snapvere.com](https://snapvere.com) · [info@snapvere.com](mailto:info@snapvere.com) · [brendigo.com](https://brendigo.com)
