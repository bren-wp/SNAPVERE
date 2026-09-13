# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

## Capture what matters. Keep it yours.

**SNAPVERE is a fast, local-first screenshot toolkit for Windows, Android and modern browsers — built for people who want useful capture tools without an account, analytics clutter or an automatic cloud workflow.**

Capture a precise region and annotate it on Windows. Grab a one-shot screen on Android. Save a visible area, selected region or bounded full page from your browser. Your core capture workflow stays local to your device.

**Current release: SNAPVERE 0.1.1**  
[Download v0.1.1](https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1) · [Hrvatski README](README.hr.md) · [User Guide](docs/USER-GUIDE.md) · [Official site](https://snapvere.com) · [Support](mailto:info@snapvere.com)

### Get SNAPVERE

| Platform | Recommended download | What you get |
| --- | --- | --- |
| **Windows** | **[SNAPVERE-Setup.exe](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Setup.exe)** | Installed tray-first app with x86/x64/ARM64 payloads |
| **Windows Portable** | **[SNAPVERE-Portable.exe](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Portable.exe)** | Single portable host; no traditional install required |
| **Android 10+** | **[SNAPVERE.apk](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE.apk)** | Native one-shot MediaProjection capture app |
| **Chrome** | **[SNAPVERE-Chrome.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Chrome.zip)** | Visible, region and bounded full-page capture |
| **Edge** | **[SNAPVERE-Edge.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Edge.zip)** | Chromium MV3 extension package |
| **Opera** | **[SNAPVERE-Opera.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Opera.zip)** | Chromium MV3 extension package |
| **Firefox** | **[SNAPVERE-Firefox.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Firefox.zip)** | Firefox-compatible MV3 WebExtension package |
| Android source | [SNAPVERE-Android-Source.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Android-Source.zip) | Validated tracked Android source tree |

> Browser ZIPs are published on GitHub for manual/developer-mode installation. They are **not** represented as approved listings in Chrome Web Store, Edge Add-ons, Opera Add-ons or Mozilla Add-ons until those external publisher/review processes actually happen. The public Android APK is transparently **CI/debug-signed**, not represented as Google Play production-signed.

---

## Why SNAPVERE?

### Fast when you need it, quiet when you do not

The Windows app is tray-first. It can live in the notification area instead of occupying your desktop with a permanent dashboard. Capture is available from the tray or global shortcuts when you need it.

### Useful capture modes, not just another Print Screen clone

Windows includes region, window and screen workflows plus a frozen-frame region editor with **Pen, Line, Arrow, Box, Highlight, colors, Undo, Copy and Save**. Browser packages add visible-area, selected-region and bounded full-page capture. Android provides a focused one-shot full-screen capture flow.

### Local-first by design

SNAPVERE does not require an account for core capture. The current Android app intentionally has **no `INTERNET` permission**. Browser extensions have no broad `<all_urls>` permission and no telemetry/analytics/cloud-upload runtime. Windows capture and PNG encoding are local.

### One product, three environments

Use the desktop app for fast capture and annotation, the Android companion for native mobile capture, and the browser extension when the content you need is inside a web page.

### Release engineering you can inspect

The project validates architecture payloads, tests Windows lifecycle behavior, runs Android lint/tests/builds, checks browser permission/source parity, packages browser ZIPs reproducibly and verifies SHA-256 release digests after publication.

---

## Windows — capture without breaking your flow

SNAPVERE for Windows is a **.NET 10 / WinUI 3 tray-first application**.

| Action | Primary input | Alternate |
| --- | --- | --- |
| Region Capture | Left-click tray or **Print Screen** | `Ctrl+Shift+1` |
| Window Capture | Tray → Capture window | `Ctrl+Shift+2` |
| Screen Capture | Tray → Capture screen | `Ctrl+Shift+3` |
| Settings / recent captures | Right-click tray | — |
| Language / About / Exit | Right-click tray | — |

### Region capture that is actually editable

Region Capture works on a frozen physical-pixel frame. Select, move and resize the region, then annotate locally with Pen, Line, Arrow, Box or Highlight. Copy the result or save it as PNG.

### Dedicated window capture

Window Capture uses native top-level-window discovery and Windows.Graphics.Capture where supported; it does not silently pretend a desktop crop is a true window capture.

### Resilient screen capture

Screen Capture prefers Windows.Graphics.Capture/Direct3D and retains a monitor fallback for expected acquisition failures. Multi-monitor and DPI conversion logic is tested as part of the desktop codebase.

### Local files you control

Completed captures are written locally, by default to:

```text
Pictures\SNAPVERE
```

The file writer uses a temporary-file + final move sequence so an interrupted encode is not presented as a completed PNG. Optional Save As uses the native picker; cancelling it preserves the already completed local capture.

### Universal public packages

`SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe` carry validated x86, x64 and ARM64 application payloads. Windows 10 1809/build 17763 is the minimum target; WGC-dependent paths require Windows 10 2004/build 19041 or later.

[Windows architecture](docs/ARCHITECTURE.md) · [Capture engine](docs/CAPTURE-ENGINE.md) · [Installation](docs/INSTALLATION.md)

---

## Android — one tap, one consent, one local capture

SNAPVERE for Android 10+ is a native Java 17 application using **MediaProjection + MediaStore**.

Every capture begins with a fresh Android system-consent prompt. SNAPVERE does not cache a projection token for silent future captures. After consent, the app moves its own task behind the previous content and saves one PNG into:

```text
Pictures/SNAPVERE
```

The latest readable capture can be **opened, shared or deleted** from the app. Stale MediaStore references are cleared instead of being shown as valid files.

### Android privacy posture

- no `android.permission.INTERNET`;
- no first-party telemetry or analytics SDK;
- no advertising SDK;
- no automatic cloud uploader;
- backup disabled;
- cleartext traffic disabled;
- non-exported MediaProjection foreground service;
- fresh system permission flow for each capture.

Version: **0.1.1** / `versionCode 11`.

[Android guide](docs/ANDROID.md) · [Android source/build](android/README.md) · [Privacy](docs/PRIVACY.md)

---

## Browser extensions — capture the page, not your privacy

SNAPVERE 0.1.1 includes standalone packages for **Chrome, Edge, Opera and Firefox**.

Choose one of three actions:

- **Capture visible area** — saves the current viewport;
- **Select region** — drag over exactly what you need;
- **Capture full page** — bounded controlled scrolling + local stitching.

The current permission contract is deliberately small:

```text
activeTab
scripting
downloads
storage
```

There is no `<all_urls>` and no broad `host_permissions` grant. The source includes no telemetry, analytics, ads, cloud uploader or remote runtime dependency.

Full-page capture has explicit tile/canvas/pixel bounds to avoid unbounded memory use. Dynamic pages, video/canvas content, sticky components and cross-origin frames can still behave differently from a static document; SNAPVERE documents that limitation rather than hiding it.

[Browser Extension Guide](docs/BROWSER-EXTENSIONS.md) · [Source Guide](ekstenzije/README.md) · [Extension Privacy](ekstenzije/PRIVACY.md)

---

## Privacy that is easy to understand

SNAPVERE's current core capture model is local-first:

- **Windows:** local capture/encoding; no first-party screenshot cloud-sync or telemetry uploader in the capture runtime.
- **Android:** no Internet permission; screenshots are stored through local MediaStore.
- **Browsers:** screenshots are processed locally; only the minimal extension permission set above is requested.

External actions remain explicit. If you choose Share on Android, Save As to a synced folder, or open an external website/email client, the destination application/service has its own behavior and policies.

Read the complete [Privacy documentation](docs/PRIVACY.md) and [Security Policy](SECURITY.md).

---

## Built with verification, not wishful thinking

A software project cannot honestly promise that no defect will ever exist. SNAPVERE instead uses multiple automated gates that make regressions visible before they become releases.

### Windows CI

- NuGet vulnerability audit and analyzer enforcement;
- x64 build + xUnit tests;
- x86 build and ARM64 cross-build/package validation;
- native payload/integrity validation;
- six real rendered UI surfaces;
- PR visual comparison to a successful `main` baseline;
- universal Setup + Portable build;
- exact public package contract;
- x64/x86 Setup/Portable lifecycle and tray-first probes.

### Android CI

- manifest privacy/service/version checks;
- SDK/tooling verification;
- `lintDebug` + `lintRelease`;
- JVM unit tests;
- debug + release builds;
- APK signature/alignment verification;
- validated Android source archive.

### Browser CI

- MV3 manifest and exact-permission validation;
- Chrome/Edge/Opera source parity and normalized Firefox differences;
- EN/HR locale parity;
- icon/hash/dimension checks;
- source syntax and forbidden-pattern policy;
- deterministic store graphics;
- store metadata/privacy validation;
- two independent ZIP builds that must match byte-for-byte.

### Product Contract CI

[`product-version.json`](product-version.json) is the canonical active version contract. A dedicated validator checks Windows, Android and browser version alignment, Android EN/HR resource parity, active documentation, exact release asset names and relative Markdown links.

See the full [QA Matrix](docs/QA-MATRIX.md) and [Product Status](docs/PRODUCT-STATUS.md).

---

## Release integrity — v0.1.1

The public v0.1.1 release contains **exactly eight SNAPVERE assets**:

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

The release workflow builds/validates the platform payloads, computes SHA-256 digests, validates Windows runtime/package contracts, creates/verifies the release tag, publishes the approved files and then compares the published GitHub digest of every asset with the locally validated value.

Historical releases stay historical: **v0.1.0 is not rewritten or retroactively given browser packages.**

[Release Notes 0.1.1](RELEASE_NOTES_0.1.1.md) · [Release Contract](docs/RELEASE-0.1.1.md) · [Versioning & Releases](docs/VERSIONING-RELEASES.md)

---

## Languages

English is the canonical fallback. The Windows application exposes **28 built-in language choices**, including Croatian. Android and browser extensions ship dedicated English and Croatian resources. No translation API is required for the runtime language system.

---

## Documentation

Not sure where to start? Use the [Documentation Hub](docs/README.md).

Recommended reading:

- [User Guide](docs/USER-GUIDE.md)
- [Installation](docs/INSTALLATION.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Privacy](docs/PRIVACY.md)
- [Product Status](docs/PRODUCT-STATUS.md)
- [QA Matrix](docs/QA-MATRIX.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Android](docs/ANDROID.md)
- [Browser Extensions](docs/BROWSER-EXTENSIONS.md)
- [Security & Performance 0.1.1](docs/SECURITY-PERFORMANCE-0.1.1.md)
- [Versioning & Releases](docs/VERSIONING-RELEASES.md)

---

## Technology

C# / .NET 10 · WinUI 3 · Windows App SDK 1.8 · Windows.Graphics.Capture · Direct3D 11 · Win32/DWM/GDI interop · Java 17 · Android MediaProjection/MediaStore · Manifest V3/WebExtensions · xUnit · JUnit 4 · GitHub Actions

---

## License, developer and support

**SNAPVERE 0.0.7 and later are distributed under the SNAPVERE Commercial Software License Agreement in [`LICENSE`](LICENSE).** SNAPVERE is the product brand. **Brendigo** is the developer and publisher.

Official site: **https://snapvere.com**  
Support: **info@snapvere.com**  
Developer: **https://brendigo.com**

---

### Ready to capture without the clutter?

**[Download SNAPVERE 0.1.1](https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1)** and choose the package for your platform.

**SNAPVERE — Capture. Edit. Done.**
