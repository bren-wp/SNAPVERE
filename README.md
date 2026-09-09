# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture anything." src="assets/branding/readme/snapvere-logo-light.svg" width="520">
</picture>

**Screen capture for Windows — developed and published by Brendigo.**

SNAPVERE is a local-first Windows capture application focused on fast region selection, physical-pixel precision, dependable local PNG output and a clean path toward professional annotation, OCR, pin-to-screen and scrolling capture.

## Version 0.0.2

Version 0.0.2 hardens real Windows startup and packaging while preserving the working capture core. The primary application now starts through a conservative WinUI Capture Center that avoids the fragile custom MainWindow XAML/composition path found during release QA.

### Release downloads

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
- x64 and x86/32-bit release targets; ARM64 remains a source/build target
- Windows 10 version 1809 / build 17763 minimum platform target
- nullable reference types and strict .NET analyzers
- central NuGet package management
- xUnit automated tests
- GitHub Actions Windows CI and release packaging

## Architecture

```text
WinUI Capture Center / global hotkey / tray action
    ↓
Snapvere.Application workflow
    ↓
Snapvere.Capture acquisition + physical-pixel geometry
    ↓
CaptureFrame
    ↓
Snapvere.Imaging crop / encode
    ↓
CaptureFileWriter
    ↓
local atomic PNG
```

Release packaging is isolated from the application runtime:

```text
self-contained app publish
    ↓
validated ZIP payload
    ├── SNAPVERE Setup    → per-user install + Windows uninstall registration
    └── SNAPVERE Portable → private versioned temp cache + launch
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), [`docs/REGION-CAPTURE.md`](docs/REGION-CAPTURE.md) and [`docs/INSTALLATION.md`](docs/INSTALLATION.md).

## Implemented now

### Capture foundation

- native Win32 active-display discovery;
- physical virtual-desktop coordinates, including negative origins;
- 100/125/150/175/200% DPI conversion coverage;
- sub-DIP pointer conversion without premature rounding;
- validated BGRA8 capture frames;
- GDI monitor-capture compatibility backend with optional cursor rendering;
- deterministic PNG encoder;
- pixel-accurate BGRA8 cropper with stride support;
- collision-safe `Pictures\SNAPVERE` paths;
- temp-file + atomic-move persistence.

### Screen Capture

- primary-display screenshot;
- main Capture Center hides before acquisition;
- local PNG save;
- global `Ctrl+Shift+4` hotkey;
- tray Screen action.

### Region Capture

- frozen primary-display preview;
- borderless always-on-top selection overlay;
- four-sided dimming around the active region;
- reverse-direction drag normalization;
- drag-to-move selection;
- eight resize handles;
- physical-pixel W × H badge;
- Arrow 1 px and Shift + Arrow 10 px movement;
- Enter/double-click save and Esc cancel;
- final crop from the same frozen frame shown in the overlay;
- global `Ctrl+Shift+1` hotkey;
- tray Region action.

### Desktop integration

- stable programmatic WinUI Capture Center with standard Windows title bar;
- conflict-aware native global hotkey host;
- native system tray icon and menu;
- tray recovery after Windows Explorer restarts;
- recent local captures view;
- startup diagnostics under `%LOCALAPPDATA%\SNAPVERE\Logs`;
- explicit activated-window startup probe used by CI;
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
- x64/x86 Setup, activated-window, normal-launch, uninstall and Portable lifecycle validation in GitHub Actions;
- SHA-256 release checksums.

## Release QA

The v0.0.2 gate validates both x64 and x86 on the Windows Server 2022 GitHub runner used for WinUI CI. Each architecture must pass:

1. self-contained app publish;
2. Setup and Portable publish;
3. explicit license-gated silent Setup install;
4. activated WinUI `READY` probe;
5. normal application launch remaining alive through the startup window;
6. Setup-based uninstall and cleanup;
7. Portable activated-window probe and normal launch.

A failing runtime launch blocks release publication.

## Current limitations

The following remain intentionally deferred after 0.0.2:

- coordinated cross-monitor Region overlay;
- Windows.Graphics.Capture/D3D primary acquisition backend;
- Window Capture and smart window targeting;
- Scrolling Capture;
- clipboard Quick Actions;
- full annotation Editor;
- full History management and Pin to Screen;
- OCR;
- automatic updater.

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

SNAPVERE capture workflows are local-first. Screenshot pixels, local capture history and user files are not sent to an analytics or telemetry service by the 0.0.2 application.

## Security

Release packaging validates extraction destinations, bounds embedded archives, uses staged writes and publishes SHA-256 checksums. Setup validates a SNAPVERE installation marker before destructive cleanup. Private signing keys, production credentials, user screenshots, dumps and runtime data must never be committed.

The 0.0.2 executables are intentionally not Authenticode-signed. Use the published `SHA256SUMS.txt` for artifact integrity verification.

## Branding

Canonical vector sources are under `assets/branding/`. See [`docs/BRANDING.md`](docs/BRANDING.md).

## License

Source code is distributed under the Mozilla Public License 2.0. Product branding and trademarks are not granted by the source-code license.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md).

---

**SNAPVERE — Capture anything.**  
Developed and published by **Brendigo**.
