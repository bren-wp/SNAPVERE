# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

**Capture. Edit. Done. — fast, local-first screen capture for Windows and Android by Brendigo.**

[Hrvatski README](README.hr.md) · [Official product site](https://snapvere.com) · [Support](mailto:info@snapvere.com) · [Developer: Brendigo](https://brendigo.com)

SNAPVERE 0.1.0 combines the production tray-first Windows capture application and the native Android companion under one validated release contract. Published historical tags/releases remain immutable.

## Windows capture

| Action | Primary input | Alternate |
| --- | --- | --- |
| Region Capture | Left-click tray or **Print Screen** | `Ctrl+Shift+1` |
| Window Capture | Right-click tray → Capture window | `Ctrl+Shift+2` |
| Screen Capture | Right-click tray → Capture screen | `Ctrl+Shift+3` |
| Settings / recent captures | Right-click tray | — |
| Language / About / Exit | Right-click tray | — |

Region Capture provides frozen-frame physical-pixel selection, resize/move handles, Pen, Line, Arrow, Box and Highlight tools, four annotation colors, Undo, Copy and Save. Window Capture uses native top-level-window discovery plus Windows.Graphics.Capture `CreateForWindow`; it does not silently substitute a screen crop. Screen Capture prefers Windows.Graphics.Capture/Direct3D and retains the resilient monitor fallback for expected acquisition failures.

Captures are safely completed as PNG files before optional relocation with the native Windows **Save As** picker. Cancelling Save As preserves the completed PNG in `Pictures\SNAPVERE`. See [Save location](docs/SAVE-LOCATION.md).

## Tray-first Windows UX

Normal startup keeps the app in the Windows notification area instead of opening a launcher dashboard. The native tray host uses the `NOTIFYICON_VERSION_4` contract, re-applies the version after Explorer/taskbar recreation, retains the standard tooltip and supports pointer plus keyboard activation. Region activation is debounced so duplicate notification events cannot start duplicate capture workflows.

Implemented preferences are **Start SNAPVERE with Windows**, **Include cursor on capture**, and **Language**. Recent Captures is local and bounded. The application contains no telemetry worker, cloud-upload client, remote command channel or automatic updater.

## Android 0.1.0

SNAPVERE for Android 10+ is a native Java 17 application built around Android MediaProjection and MediaStore. Every capture requires a fresh Android system-consent flow. Consent tokens are never cached/reused and SNAPVERE never records continuously in the background.

The implemented Android workflow provides:

- explicit full-screen capture;
- local PNG storage in `Pictures/SNAPVERE`;
- validated **Open / Share / Delete** actions for the latest capture;
- English and Croatian resources;
- user-initiated Website / Support / Privacy / Terms actions;
- responsive action stacking, large touch targets, system-inset handling and a deliberate dark theme;
- no account, telemetry, analytics, advertising SDK, cloud upload or `INTERNET` permission.

### Android stability hardening

0.1.0 adds stricter capture lifecycle and pixel-buffer handling:

- bounded five-second Activity-hide and seven-second first-frame guards;
- guarded Handler scheduling and `ImageReader.acquireLatestImage()`;
- controlled foreground-service initialization failure;
- per-resource exception-safe MediaProjection / VirtualDisplay / ImageReader / HandlerThread teardown;
- owner-aware process capture lock release so stale teardown cannot clear another active owner;
- `RGBA_8888` pixel-stride, row-stride, row-padding and buffer-size validation before bitmap copy;
- buffer rewind before copy and deterministic Image/Bitmap cleanup ordering;
- contained conversion/provider/allocation failures with localized recovery status rather than raw internal exception text.

Android CI runs `lintDebug`, `lintRelease`, JVM tests, debug/release builds, signature/alignment checks and SHA-256 generation. The CI APK is deliberately debug-signed development evidence; the public release APK uses the separate stable release-signing gate described below.

See [Android architecture and QA](docs/ANDROID.md) and [Android source/build guide](android/README.md).

## Privacy and security

Windows capture processing is local. Static review of the desktop runtime has no first-party HTTP/socket client, WebView/WebView2 or JavaScript execution path. Android's manifest intentionally has no `android.permission.INTERNET`; cleartext traffic and app backup are disabled and the MediaProjection service is non-exported.

0.1.0 keeps strict dependency/build controls:

- NuGet audit for direct/transitive dependencies at `low` severity and above;
- `NU1901`–`NU1904` as build failures;
- full-SHA-pinned GitHub Actions;
- deterministic/analyzer-enforced .NET builds;
- architecture-specific trusted SHA-256 manifests embedded in the Portable host;
- transactional Portable cache rebuild when content is missing, modified, unexpected or a reparse-point risk;
- Android manifest privacy/version/service validation and lint warnings as errors;
- release publication gated by exact asset names and post-publication GitHub digest verification.

This is not a claim that software can be guaranteed free of every vulnerability. See [Security Policy](SECURITY.md), [0.1.0 security/performance](docs/SECURITY-PERFORMANCE-0.1.0.md), and the historical [0.0.9 hardening report](docs/SECURITY-PERFORMANCE-0.0.9.md).

## Performance and stability

SNAPVERE remains event-driven while idle. Tray and hotkey hosts use native message loops rather than polling; capture/D3D resources are created for active capture work; secondary windows are on demand; recent-capture discovery is bounded; Android has no idle capture loop or network worker.

No fixed CPU/RAM percentage is promised because OS version, monitor count/DPI, graphics driver, Android OEM behavior, resolution and active editor/capture work materially change resource use.

## v0.1.0 public release contract

A valid v0.1.0 GitHub Release contains **exactly four public assets**:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

`SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe` embed native x86, x64 and ARM64 Windows payloads and select a compatible payload automatically. Windows targets Windows 10 version 1809/build 17763 or later; WGC-dependent paths require Windows 10 version 2004/build 19041 or later.

`SNAPVERE.apk` is the minified/shrunk Android release package. The release workflow only publishes it after ZIP alignment and verification against SNAPVERE's stable Android release signing identity. Required private signing values are GitHub secrets and are never committed. The workflow does **not** replace a missing release key with an ephemeral debug key.

`SNAPVERE-Android-Source.zip` is generated directly from the validated Git `android/` tree. It excludes generated build output, Gradle caches and signing material.

## Automated QA

### Windows

GitHub Actions:

- restores with NuGet audit;
- builds/tests x64;
- builds x86 and cross-builds ARM64;
- validates native payload structure and embedded integrity manifests;
- captures six real rendered surfaces: Region, Window, Tray, Options, Language and About;
- compares PR UI against the successful-main baseline;
- builds universal Setup and Portable;
- enforces package naming/shape;
- runs x64/x86 Setup+Portable lifecycle and tray-first probes.

ARM64 evidence on the hosted x64 runner is cross-build/package validation, not physical ARM64 runtime proof.

### Android

Android Actions validates the manifest privacy/service/version contract, SDK/tooling, debug/release lint, JVM tests, debug/release builds, debug APK signing/alignment and SHA-256. The v0.1.0 release workflow additionally verifies the stable release signature, validates the source archive, carries hashes through the Actions artifact transfer, and verifies all four published GitHub digests.

A green Android workflow is automated build/package evidence, not a claim of exhaustive runtime coverage across every physical OEM device.

## Release signing

Public Android v0.1.0 publication requires these repository secrets:

```text
SNAPVERE_ANDROID_KEYSTORE_BASE64
SNAPVERE_ANDROID_KEY_ALIAS
SNAPVERE_ANDROID_KEYSTORE_PASSWORD
SNAPVERE_ANDROID_KEY_PASSWORD
```

If any are absent/invalid, the release fails before the immutable tag is created. See [Android documentation](docs/ANDROID.md).

## Languages

English is the canonical default/fallback. Windows exposes 28 built-in language choices including Croatian. Android currently provides dedicated English and Croatian resources and follows normal Android fallback for other locales.

Windows language preference is stored locally in:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

No translation API or background language network service is used.

## Architecture

Windows:

```text
Tray / Print Screen / hotkeys
    ↓
Hidden WinUI runtime coordinator
    ↓
Region / Window / Screen workflows
    ↓
CaptureFrame (physical BGRA8 pixels)
    ↓
Crop / annotation / PNG encode
    ↓
Local PNG → optional Save As / Clipboard
```

Android:

```text
Explicit Capture tap
    ↓
MediaProjection consent
    ↓
Foreground mediaProjection service
    ↓
Activity hidden confirmation
    ↓
VirtualDisplay + ImageReader + bounded frame wait
    ↓
RGBA stride/buffer validation
    ↓
MediaStore PNG → Open / Share / Delete
```

## Documentation

English documentation lives in [`docs/`](docs/) and Croatian documentation in [`docs/hr/`](docs/hr/).

Key documents: [Architecture](docs/ARCHITECTURE.md), [Tray UX](docs/TRAY-UX.md), [Capture engine](docs/CAPTURE-ENGINE.md), [Region Capture](docs/REGION-CAPTURE.md), [Window Capture](docs/WINDOW-CAPTURE.md), [Save location](docs/SAVE-LOCATION.md), [Settings](docs/SETTINGS.md), [Installation](docs/INSTALLATION.md), [Android](docs/ANDROID.md), [0.1.0 security/performance](docs/SECURITY-PERFORMANCE-0.1.0.md), [Branding](docs/BRANDING.md), and [Image pipeline](docs/IMAGE-PIPELINE.md).

## Technology

- C# / .NET 10
- WinUI 3 / Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11
- Win32 / DWM / GDI interoperability
- Java 17 / Android MediaProjection / MediaStore
- deterministic builds, nullable/analyzer enforcement and central NuGet management
- xUnit + JUnit 4 + GitHub Actions

## Diagnostics

Windows startup diagnostics:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Screenshot pixels are not intentionally written to this log.

## License and ownership

**SNAPVERE 0.0.7 and later are distributed under the SNAPVERE Commercial Software License Agreement in [`LICENSE`](LICENSE).** SNAPVERE is the product brand. Brendigo is the developer and publisher. Official site: **snapvere.com**. Support: **info@snapvere.com**.

---

**SNAPVERE — Capture. Edit. Done.**  
Developed and published by **Brendigo** · [snapvere.com](https://snapvere.com) · [info@snapvere.com](mailto:info@snapvere.com) · [brendigo.com](https://brendigo.com)
