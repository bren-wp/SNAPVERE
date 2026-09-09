# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture anything." src="assets/branding/readme/snapvere-logo-light.svg" width="520">
</picture>

**Screen capture for Windows — developed and published by Brendigo.**

SNAPVERE is a local-first Windows capture application focused on fast region selection, physical-pixel precision, inline annotation, dependable local PNG output and a clean path toward window targeting, scrolling capture, OCR and richer history workflows.

## Current main

The current development branch keeps the hardened 0.0.2 Setup/Portable lifecycle while advancing the capture experience:

- compact tray-first Capture Center;
- `Print Screen` as the preferred Region Capture shortcut;
- `Ctrl+Shift+1` as an independent Region fallback when Print Screen is owned by Windows or another app;
- inline Region tools for Pen, Line, Arrow, Box and Highlight;
- Copy and Save directly from the Region editor;
- Windows.Graphics.Capture + D3D11 as the preferred monitor-acquisition backend on Windows 10 2004 / build 19041 and later;
- GDI retained as an automatic compatibility fallback and for supported older Windows builds.

## Version 0.0.2 packaging

GitHub Releases publish:

- `SNAPVERE-0.0.2-Setup-x64.exe`
- `SNAPVERE-0.0.2-Portable-x64.exe`
- `SNAPVERE-0.0.2-Setup-x86.exe`
- `SNAPVERE-0.0.2-Portable-x86.exe`
- raw x64/x86 self-contained ZIP payloads
- `SHA256SUMS.txt`

`x86` is the 32-bit Windows architecture commonly called `x32`; duplicate x32 binaries are not published.

See [`docs/INSTALLATION.md`](docs/INSTALLATION.md) for Setup, Portable, platform and uninstall behavior.

## Technology

- C# / .NET 10 LTS (`10.0.12`, SDK `10.0.401`)
- WinUI 3
- Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11 preferred acquisition path
- native GDI compatibility fallback
- x64 and x86/32-bit release targets; ARM64 remains a source/build target
- Windows 10 version 1809 / build 17763 minimum application target
- WGC monitor path enabled from Windows 10 version 2004 / build 19041
- nullable reference types and strict .NET analyzers
- central NuGet package management
- xUnit automated tests
- GitHub Actions Windows CI and release packaging

## Architecture

```text
WinUI compact Capture Center / Print Screen / fallback hotkey / tray action
    ↓
Snapvere.Application workflow
    ↓
IScreenCaptureService
    ↓
ResilientScreenCaptureService
    ├── preferred: WindowsGraphicsCaptureService (WGC + D3D11)
    └── fallback: GdiScreenCaptureService
    ↓
CaptureFrame (physical BGRA8 pixels)
    ↓
Snapvere.Imaging crop / annotation render / encode
    ↓
CaptureFileWriter or Clipboard
    ↓
local PNG / Windows clipboard
```

Release packaging remains isolated from the capture runtime:

```text
self-contained app publish
    ↓
validated ZIP payload
    ├── SNAPVERE Setup    → per-user install + Windows uninstall registration
    └── SNAPVERE Portable → private versioned temp cache + launch
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), [`docs/CAPTURE-ENGINE.md`](docs/CAPTURE-ENGINE.md), [`docs/REGION-CAPTURE.md`](docs/REGION-CAPTURE.md) and [`docs/INSTALLATION.md`](docs/INSTALLATION.md).

## Implemented now

### Capture engine

- native Win32 display discovery;
- physical virtual-desktop coordinates, including negative origins;
- 100/125/150/175/200% DPI conversion coverage;
- sub-DIP pointer conversion without premature rounding;
- validated BGRA8 capture frames;
- Windows.Graphics.Capture monitor acquisition through a free-threaded Direct3D 11 frame pool;
- CPU staging-texture readback with row-pitch handling;
- two-second WGC frame timeout and blank-frame/dimension validation;
- automatic GDI fallback for expected unsupported/platform/native failures;
- caller cancellation never converted into fallback work;
- deterministic PNG encoder;
- pixel-accurate BGRA8 cropper with stride support;
- collision-safe `Pictures\SNAPVERE` paths;
- temp-file + atomic-move persistence.

### Region Capture

- `Print Screen` preferred global shortcut;
- `Ctrl+Shift+1` independent fallback shortcut;
- tray and launcher Region actions;
- frozen primary-display preview;
- borderless always-on-top selection/editor overlay;
- four-sided dimming around the active region;
- reverse-direction drag normalization;
- drag-to-move selection;
- eight resize handles;
- physical-pixel `W × H` badge;
- Arrow 1 px and Shift+Arrow 10 px movement;
- Pen, Line, Arrow, Box and Highlight annotation tools;
- annotation undo;
- Copy to Windows clipboard;
- Save to local PNG;
- Enter/double-click save and Esc cancel;
- final output generated from the same frozen frame shown in the editor.

### Screen Capture

- primary-display screenshot;
- Capture Center hides before acquisition;
- local PNG save;
- global `Ctrl+Shift+4` hotkey;
- tray Screen action.

### Desktop integration

- compact 560×620 capture-first WinUI launcher;
- Hide to tray action;
- native system tray icon and menu;
- tray recovery after Windows Explorer restarts;
- conflict-aware native global hotkey host;
- Print Screen conflict reporting that preserves the `Ctrl+Shift+1` fallback;
- four-item recent local capture view;
- Open capture folder action;
- startup diagnostics under `%LOCALAPPDATA%\SNAPVERE\Logs`;
- explicit activated-window and Region-editor runtime probes used by CI;
- unavailable future features are not presented as working functionality.

### Setup and Portable

- x64 and x86 self-contained application payloads;
- single-file Setup executable per architecture;
- Mozilla Public License 2.0 shown before interactive installation;
- per-user default installation without elevation;
- optional Start menu/Desktop shortcuts;
- Windows Installed apps registration;
- uninstall through installed `SNAPVERE-Setup.exe --uninstall`, without a separate `uninstall.exe`;
- installation marker validation before destructive uninstall cleanup;
- screenshots preserved when uninstalling;
- single-file Portable launcher per architecture;
- guarded payload extraction that blocks absolute paths and directory traversal;
- bounded archive extraction and versioned portable cache;
- SHA-256 release checksums.

## Automated QA

The Windows CI gate validates both x64 and x86 on Windows Server 2022. Each architecture must pass build/publish plus Setup and Portable lifecycle checks. The lifecycle probe verifies application startup and real Region-editor materialization; unit tests cover capture geometry, image processing and preferred/fallback orchestration.

The hosted runner is not treated as proof of an end-user desktop WGC screenshot. Native WGC acquisition is compiled and wired as the preferred production path, while expected acquisition failures are designed to fall back to GDI.

## Current limitations

The following remain intentionally deferred:

- coordinated cross-monitor Region overlay and freeze composition;
- Window Capture and smart window/control targeting;
- Scrolling Capture;
- richer editor features such as text, blur/pixelate and numbered steps;
- full History management, favorites and Pin to Screen;
- OCR;
- automatic updater;
- Authenticode signing for public release artifacts.

## Build

Use Windows with the .NET SDK defined in `global.json` and Windows application development prerequisites.

```powershell
dotnet restore SNAPVERE.sln
dotnet build SNAPVERE.sln -c Release -p:Platform=x64
dotnet test tests/Snapvere.UnitTests/Snapvere.UnitTests.csproj -c Release -p:Platform=x64
```

32-bit app build:

```powershell
dotnet restore src/Snapvere.App/Snapvere.App.csproj -r win-x86
dotnet build src/Snapvere.App/Snapvere.App.csproj -c Release -r win-x86 -p:Platform=x86
```

## Privacy

SNAPVERE capture workflows are local-first. Screenshot pixels, local capture history and user files are not sent to an analytics or telemetry service by the current application.

## Security

Release packaging validates extraction destinations, bounds embedded archives, uses staged writes and publishes SHA-256 checksums. Setup validates a SNAPVERE installation marker before destructive cleanup. Private signing keys, production credentials, user screenshots, dumps and runtime data must never be committed.

Current executables are intentionally not Authenticode-signed. Use the published `SHA256SUMS.txt` for artifact integrity verification.

## Branding

Canonical vector sources are under `assets/branding/`. See [`docs/BRANDING.md`](docs/BRANDING.md).

## License

Source code is distributed under the Mozilla Public License 2.0. Product branding and trademarks are not granted by the source-code license.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md).

---

**SNAPVERE — Capture anything.**  
Developed and published by **Brendigo**.
