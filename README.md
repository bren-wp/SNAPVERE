# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

**Capture. Edit. Done. — local-first screen capture for Windows, Android and modern browsers by Brendigo.**

[Hrvatski README](README.hr.md) · [Official product site](https://snapvere.com) · [Support](mailto:info@snapvere.com) · [Developer: Brendigo](https://brendigo.com)

Current release: **SNAPVERE 0.1.1**.

0.1.1 keeps the validated tray-first Windows application and native Android companion from the 0.1.0 line and promotes the Chrome, Edge, Opera and Firefox extensions into the public GitHub Release contract. Historical published tags/releases remain immutable.

## Windows

SNAPVERE for Windows is a tray-first .NET 10 / WinUI 3 application. Normal startup lives in the notification area instead of opening a launcher dashboard.

| Action | Primary input | Alternate |
| --- | --- | --- |
| Region Capture | Left-click tray or **Print Screen** | `Ctrl+Shift+1` |
| Window Capture | Right-click tray → Capture window | `Ctrl+Shift+2` |
| Screen Capture | Right-click tray → Capture screen | `Ctrl+Shift+3` |
| Settings / recent captures | Right-click tray | — |
| Language / About / Exit | Right-click tray | — |

Region Capture provides frozen-frame physical-pixel selection, resize/move handles, Pen, Line, Arrow, Box and Highlight tools, colors, Undo, Copy and Save. Window Capture uses native top-level-window discovery with Windows.Graphics.Capture rather than silently substituting a screen crop. Screen Capture prefers Windows.Graphics.Capture/Direct3D and retains a resilient monitor fallback for expected acquisition failures.

Captures are completed locally as PNG files before optional relocation through the native **Save As** picker. Cancelling Save As preserves the completed PNG in `Pictures\SNAPVERE`.

The public Windows files are universal hosts containing x86, x64 and ARM64 application payloads:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Windows targets Windows 10 1809/build 17763 or later. WGC-dependent capture paths require Windows 10 2004/build 19041 or later.

## Android 0.1.1

SNAPVERE for Android 10+ is a native Java 17 application built on MediaProjection and MediaStore.

Version: **0.1.1** / `versionCode 11`.

Every capture requires a fresh Android system-consent flow. Consent tokens are not cached or reused and SNAPVERE does not continuously record in the background.

The Android workflow provides:

- explicit full-screen capture;
- local PNG storage in `Pictures/SNAPVERE`;
- validated **Open / Share / Delete** actions for the latest capture;
- English and Croatian resources;
- user-initiated Website / Support / Privacy / Terms actions;
- responsive controls, large touch targets and system-inset handling;
- no account, telemetry, analytics, advertising SDK, cloud upload or `INTERNET` permission.

The capture path keeps bounded Activity-hide/first-frame guards, owner-aware capture ownership, exception-safe MediaProjection/VirtualDisplay/ImageReader cleanup and RGBA stride/buffer validation.

The public `SNAPVERE.apk` for v0.1.1 continues to use the validated CI/debug signing identity. It is installable, but it is **not** represented as Google Play/production-signed. A future Android channel using a different production signing identity may require uninstall/reinstall.

The release also contains `SNAPVERE-Android-Source.zip`, generated directly from the validated tracked Android tree without build caches or signing material.

See [Android architecture and QA](docs/ANDROID.md) and [Android source/build guide](android/README.md).

## Browser extensions 0.1.1

v0.1.1 is the first SNAPVERE public release to include browser-extension packages for:

- Google Chrome;
- Microsoft Edge;
- Opera;
- Mozilla Firefox.

The extensions support visible-area capture, bounded full-page capture using controlled scrolling/stitching, and rectangular region capture. Screenshot pixels remain local and are saved as PNG files.

Chromium-family builds use Manifest V3 service workers. Firefox uses a compatible Manifest V3 WebExtension background-script model. All variants request only:

- `activeTab`;
- `scripting`;
- `downloads`;
- `storage`.

There is no `<all_urls>` or broad host permission. The source contains no telemetry, analytics, advertising SDK, cloud uploader or remote runtime dependency.

Public browser assets:

```text
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

The packages are generated reproducibly in two independent packaging passes and must match byte-for-byte before publication. Store metadata/privacy declarations are validated against the real manifests. See [Browser Extensions](docs/BROWSER-EXTENSIONS.md), [extension source guide](ekstenzije/README.md) and [browser privacy policy](ekstenzije/PRIVACY.md).

A GitHub release does **not** imply publication or approval in Chrome Web Store, Edge Add-ons, Opera Add-ons or Mozilla Add-ons. Those stores require external authenticated publisher accounts and their own review/certification/signing processes.

## v0.1.1 public release contract

A valid v0.1.1 GitHub Release contains **exactly eight public assets**:

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

The release workflow computes SHA-256 for every asset, creates the immutable `v0.1.1` tag only after validation gates pass, publishes only the approved names and then compares GitHub's published digest for every asset with the locally validated value.

The historical **v0.1.0** release remains unchanged with exactly its original four Windows/Android assets. Browser packages are not retroactively attached to v0.1.0.

## Privacy and security

SNAPVERE is local-first:

- Windows capture processing is local and the desktop runtime has no first-party telemetry/cloud-upload worker;
- Android intentionally has no `android.permission.INTERNET`, cleartext and backup are disabled, and the MediaProjection service is non-exported;
- browser extensions do not transmit screenshots off-device and do not request broad site access.

Build/release controls include:

- NuGet audit for direct/transitive dependencies at `low` severity and above;
- `NU1901`–`NU1904` as build failures;
- nullable/analyzer enforcement and deterministic .NET builds;
- full-SHA-pinned GitHub Actions;
- architecture-specific SHA-256 manifests embedded in the Portable host;
- transactional Portable cache validation/rebuild;
- Android privacy/version/service/lint/signature/alignment checks;
- browser manifest/permission/source/parity/store-metadata checks;
- exact release-asset and post-publication digest verification.

See [Security Policy](SECURITY.md) and [0.1.1 security/performance](docs/SECURITY-PERFORMANCE-0.1.1.md).

## Automated QA

### Windows

GitHub Actions:

- restores with NuGet audit;
- builds/tests x64;
- builds x86 and cross-builds ARM64;
- validates native payload structure and embedded integrity manifests;
- captures six real rendered UI surfaces;
- compares PR UI against the successful-main baseline;
- builds universal Setup and Portable;
- enforces the public package contract;
- runs x64/x86 Setup+Portable lifecycle and tray-first probes.

ARM64 evidence on hosted x64 runners is cross-build/package validation, not physical ARM64 runtime proof.

### Android

Android CI/release validates manifest privacy/service/version, SDK/tooling, `lintDebug`, `lintRelease`, JVM unit tests, debug/release builds, public APK signature/alignment, Android source archive structure and SHA-256 transfer/publication integrity.

A green Android workflow is build/package evidence, not exhaustive physical-device/OEM runtime coverage.

### Browsers

Browser CI/release validates all four variants, source parity, exact permission allow-list, locales, icon dimensions, source syntax/policy, store metadata/privacy declarations, deterministic store graphics, reproducible ZIP output, package cleanliness and SHA-256 integrity.

A green browser workflow is static/package evidence, not exhaustive manual GUI coverage of every browser build or web application.

## Performance and stability

SNAPVERE remains event-driven while idle. Windows tray/hotkey hosts use native message loops rather than polling; capture/D3D resources are created only for active capture work. Android has no idle capture loop or network worker. Browser extensions activate capture logic only after user action.

No fixed CPU/RAM percentage is promised because OS/browser version, monitor count/DPI, graphics driver, Android OEM behavior, page complexity, resolution and active capture/editor work materially affect resource use.

## Languages

English is the canonical default/fallback. Windows exposes 28 built-in language choices including Croatian. Android and browser extensions include dedicated English and Croatian resources.

No translation API or background language network service is used.

## Documentation

English documentation lives in [`docs/`](docs/) and Croatian documentation in [`docs/hr/`](docs/hr/).

Key documents:

- [Architecture](docs/ARCHITECTURE.md)
- [Capture engine](docs/CAPTURE-ENGINE.md)
- [Region Capture](docs/REGION-CAPTURE.md)
- [Window Capture](docs/WINDOW-CAPTURE.md)
- [Installation](docs/INSTALLATION.md)
- [Android](docs/ANDROID.md)
- [Browser Extensions](docs/BROWSER-EXTENSIONS.md)
- [0.1.1 Security & Performance](docs/SECURITY-PERFORMANCE-0.1.1.md)
- [Branding](docs/BRANDING.md)
- [Release Notes 0.1.1](RELEASE_NOTES_0.1.1.md)

## Technology

- C# / .NET 10
- WinUI 3 / Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11
- Win32 / DWM / GDI interoperability
- Java 17 / Android MediaProjection / MediaStore
- Manifest V3 / WebExtensions
- deterministic builds and reproducible browser packaging
- xUnit + JUnit 4 + GitHub Actions

## Diagnostics

Windows startup diagnostics:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Screenshot pixels are not intentionally written to this log.

## License and ownership

**SNAPVERE 0.0.7 and later are distributed under the SNAPVERE Commercial Software License Agreement in [`LICENSE`](LICENSE).** SNAPVERE is the product brand. Brendigo is the developer and publisher.

Official site: **snapvere.com** · Support: **info@snapvere.com**.

---

**SNAPVERE — Capture. Edit. Done.**
