# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

**Capture. Edit. Done. — fast, local-first screen capture for Windows by Brendigo.**

[Hrvatski README](README.hr.md) · [Official product site](https://snapvere.com) · [Developer: Brendigo](https://brendigo.com)

SNAPVERE is a tray-first Windows screenshot application focused on fast Region, Window and Screen capture, lightweight annotation and local PNG output. English is the default product language; Croatian and more than 20 additional languages are available from the in-app Language picker.

> The current published release is **v0.0.8**. Changes on `main` after the v0.0.8 release are unreleased hardening work. Published tags, releases and assets are treated as immutable and are not rewritten by later development.

## Product UI

The repository UI references below are the visual contract for the real WinUI application. The tray flyout and Region editor implementation are maintained against these layouts instead of the former Capture Center design.

### Tray-first Region Capture

Left-click the SNAPVERE tray icon or press **Print Screen** to start Region Capture immediately.

![SNAPVERE tray-first Region Capture reference](docs/images/tray-first-region.svg)

### Branded tray menu

Right-click the tray icon for capture actions, local files, settings, language, About and Exit.

![SNAPVERE tray menu UI reference](docs/images/tray-menu.svg)

### Region editor

The editor works directly over the frozen capture frame with physical-pixel selection, eight resize handles, a vertical tool rail and a separate Copy / Save / Close action bar.

![SNAPVERE Region editor UI reference](docs/images/region-editor.svg)

The SVG files above are maintained UI reference artwork, not synthetic claims of a Windows screenshot. Current CI also launches the real x64 WinUI application and captures six rendered PNG surfaces — Region, Window, Tray, Options, Language and About — before universal packaging is allowed to pass. Those PNGs plus a manifest containing dimensions, byte sizes and SHA-256 digests are uploaded as a short-lived GitHub Actions visual-QA artifact. Repository screenshots are committed only when they come from a reproducible real application capture path.

## Capture controls

| Action | Primary input | Alternate |
| --- | --- | --- |
| Region Capture | Left-click tray or **Print Screen** | `Ctrl+Shift+1` |
| Window Capture | Right-click tray → Capture window | `Ctrl+Shift+2` |
| Screen Capture | Right-click tray → Capture screen | `Ctrl+Shift+4` |
| Settings / recent captures | Right-click tray | — |
| Language | Tray language control or Options | — |
| About / Exit | Right-click tray | — |

## Region Capture

Implemented behavior includes a frozen frame, physical-pixel drag selection, move/resize, eight handles, live dimensions, Pen, Line, Arrow, Box and Highlight tools, four annotation colors, Undo, `Ctrl+Z`, `Ctrl+C`, Copy, Save, Enter/double-click save and Esc cancel. Annotations are rendered into the final PNG.

The editor UI follows the graphite/violet SNAPVERE reference: vertical tools beside the selection and a separate action bar below it. Text, blur/pixelate, ellipse and numbered-step tools stay absent until they are implemented and tested.

## Window Capture

SNAPVERE discovers visible top-level windows before overlays appear, uses DWM extended-frame bounds, filters SNAPVERE/tool/cloaked/invisible windows, renders DPI-aware frozen picker surfaces and performs the final window acquisition with Windows.Graphics.Capture `CreateForWindow`. Window Capture does not silently fall back to a screen crop.

## Screen Capture

Windows.Graphics.Capture + Direct3D 11 is preferred where supported. Expected monitor-acquisition failures can fall back to the resilient GDI monitor path. Capture resources are lazy and are not initialized merely because SNAPVERE is idle in the tray.

## Languages

English (`en`) is the canonical default and fallback. The built-in language catalog currently exposes 28 language choices, including Croatian (`hr`), German, French, Spanish, Italian, Portuguese, Dutch, Polish, Czech, Slovak, Slovenian, Hungarian, Romanian, Bulgarian, Greek, Swedish, Danish, Norwegian, Finnish, Estonian, Latvian, Lithuanian, Ukrainian, Turkish, Japanese, Korean and Simplified Chinese.

Croatian includes dedicated text for current capture and secondary product surfaces. Languages without a dedicated translation for a particular string fall back to canonical English instead of displaying an unknown resource key.

Language preference is stored locally in:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

The localization layer uses static in-process tables only: no translation API, polling service, network dependency or background language worker.

## Options and defaults

Implemented preferences include:

- **Start SNAPVERE with Windows**;
- **Include cursor on capture**;
- **Language**.

Setup defaults are intentionally user-friendly but opt-out:

- Start menu shortcut: **On**;
- Desktop icon: **On**;
- Start SNAPVERE with Windows: **On**;
- Launch after installation: **On** on the completion screen.

Users can turn the optional defaults off in Setup. Windows startup is per-user and launches the normal tray-first application without a dashboard window.

## Universal packaging

From **v0.0.7 onward**, the public release contract contains exactly two downloads:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

There is no architecture-specific download list and no Demo executable. Each public host is built to start on 32-bit Windows and embeds native application payloads for:

- x86 / x32 / 32-bit Windows;
- x64 / AMD64 Windows;
- ARM64 Windows.

At runtime the host selects a compatible native payload automatically. Users do not need to decide whether to download x86 or x64.

This does **not** mean every historical Windows release is supported. The application target remains Windows 10 version 1809 / build 17763 or later; WGC-dependent capture paths require Windows 10 version 2004 / build 19041 or later. Current Windows 10/11 x86, x64 and ARM64 architecture handling is part of the release contract.

### Same-Setup uninstall

SNAPVERE intentionally installs no separate uninstaller executable. Windows Installed apps points to the installed copy of:

```text
SNAPVERE-Setup.exe --uninstall
```

The same Setup binary owns install/update/remove, validates an installation marker before recursive deletion and preserves screenshots under `Pictures\SNAPVERE`.

## Performance and privacy

SNAPVERE is designed for low idle overhead:

- tray/hotkey operation is event-driven rather than timer-polled;
- capture/D3D resources are lazy;
- language support is static and local;
- settings are small and atomically written;
- capture history is local and queried on demand;
- no account, telemetry, cloud upload or screenshot analytics are required by current capture workflows.

No fixed RAM/CPU number is promised because Windows version, DPI, display count, graphics drivers and an active capture session materially affect working set and CPU usage. CI protects against functional regressions; performance work avoids adding periodic idle tasks.

## Automated QA

GitHub Actions builds/tests x64 and x86 and cross-builds ARM64. The universal package gate also verifies:

- exactly two public EXE outputs;
- all three embedded native payloads contain `Snapvere.exe`;
- x64 and x86 Setup/Portable runtime lifecycle;
- explicit commercial-license acceptance for silent Setup;
- default Desktop shortcut and current-user startup registration;
- same-Setup uninstall and cleanup;
- tray-first startup;
- Region editor materialization;
- Window picker materialization;
- Tray / Options / Language / About materialization;
- six real rendered x64 WinUI PNG snapshots: Region, Window, Tray, Options, Language and About;
- a visual-QA manifest containing each captured surface's dimensions, byte size and SHA-256 digest;
- unit tests for payload architecture selection, safe ZIP extraction and language/settings fallback.

A visually empty or unexpectedly small rendered UI snapshot fails CI. ARM64 is cross-built and package-validated on the hosted x64 runner; that runner is not treated as a real ARM64 runtime device.

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
Pictures\SNAPVERE or Windows Clipboard
```

## Documentation

English documentation lives in [`docs/`](docs/) and Croatian documentation in [`docs/hr/`](docs/hr/).

Key documents: [Architecture](docs/ARCHITECTURE.md), [Tray UX](docs/TRAY-UX.md), [Capture engine](docs/CAPTURE-ENGINE.md), [Region Capture](docs/REGION-CAPTURE.md), [Window Capture](docs/WINDOW-CAPTURE.md), [Multi-monitor](docs/MULTI-MONITOR.md), [Settings](docs/SETTINGS.md), [Installation](docs/INSTALLATION.md), [Branding](docs/BRANDING.md) and [Image pipeline](docs/IMAGE-PIPELINE.md).

## Technology

- C# / .NET 10
- WinUI 3 / Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11
- Win32 / DWM / GDI interoperability
- deterministic builds, nullable/analyzer enforcement and central NuGet management
- xUnit + GitHub Actions

## Diagnostics

Local startup diagnostics:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Screenshot pixels are not written to the startup log.

## License and ownership

**SNAPVERE 0.0.7 and later are distributed under the SNAPVERE Commercial Software License Agreement included in [`LICENSE`](LICENSE).** SNAPVERE is the product brand. Brendigo is the developer and publisher. The official product website is **snapvere.com** and the developer website is **brendigo.com**.

Earlier published versions remain governed by the license distributed with those versions; changing the current repository license does not retroactively revoke rights already granted for historical releases.

---

**SNAPVERE — Capture. Edit. Done.**  
Developed and published by **Brendigo** · [snapvere.com](https://snapvere.com) · [brendigo.com](https://brendigo.com)
