# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

**Fast, local-first screen capture for Windows — developed and published by Brendigo.**

SNAPVERE is designed to stay out of the way. Start it once and it lives in the Windows notification area instead of keeping a launcher window open.

## Tray-first workflow

**Left-click the SNAPVERE tray icon → Region Capture starts immediately.** There is no intermediate dashboard.

![SNAPVERE tray-first Region Capture](docs/images/tray-first-region.svg)

**Right-click the tray icon → open the branded SNAPVERE command menu** for Region, Window and Screen Capture, the capture folder, Options/Recent captures, About and Exit.

![SNAPVERE branded tray menu](docs/images/tray-menu.svg)

The Region editor exists only while a capture is active. Selection, annotation, Copy and Save happen directly over the frozen screen.

![SNAPVERE Region Capture editor](docs/images/region-editor.svg)

> The SVGs above are repository UI illustrations of the implemented workflow, maintained with the product sources. They are not claims of pixel-identical Windows screenshots across themes, DPI levels or Windows versions.

## Capture controls

| Action | Primary input | Fallback / alternate |
| --- | --- | --- |
| Region Capture | **Left-click tray icon** or **Print Screen** | `Ctrl+Shift+1` |
| Window Capture | Right-click tray → Capture window | `Ctrl+Shift+2` |
| Screen Capture | Right-click tray → Capture screen | `Ctrl+Shift+4` |
| Tray menu | **Right-click tray icon** | — |
| Options / recent captures | Right-click tray → Options & recent captures | — |
| Exit | Right-click tray → Exit SNAPVERE | — |

If Windows or another application owns Print Screen, the independent `Ctrl+Shift+1` Region shortcut remains available.

## Current main / v0.0.4 line

The current development line builds on v0.0.3 and adds the tray-first interaction model and refreshed product identity:

- normal startup stays hidden in the notification area;
- left-clicking the tray icon starts Region Capture immediately;
- right-clicking opens the branded WinUI tray flyout rather than a generic native text menu;
- refreshed violet SNAPVERE shard/feather identity for tray and repository surfaces;
- explicit tray-only runtime probe for installed and Portable packages;
- Region, Window and Screen Capture remain available without keeping a launcher open;
- Options/Recent and About open only when explicitly requested.

## Implemented capture stack

### Region Capture

- frozen primary-display preview;
- physical-pixel drag selection with reverse-drag normalization;
- drag-to-move and eight resize handles;
- `W × H` pixel badge;
- Arrow 1 px / Shift+Arrow 10 px movement;
- Pen, Line, Arrow, Box and Highlight tools;
- four annotation colors and Undo;
- Copy to Windows clipboard;
- Save to local PNG;
- Enter/double-click save and Esc cancel;
- final output produced from the same frozen frame shown by the editor.

### Window Capture

- visible top-level window discovery;
- DWM extended-frame bounds;
- SNAPVERE/self/tool/cloaked-window filtering;
- stable Z-order snapshot before picker overlays appear;
- overlay-safe geometric hit testing;
- one frozen DPI-aware picker surface per monitor;
- negative virtual-desktop coordinate support;
- coordinated highlight for windows spanning monitors;
- hover title and physical size feedback;
- left-click capture / Esc cancel;
- Windows.Graphics.Capture `CreateForWindow` acquisition;
- atomic local PNG save and recent-capture refresh.

### Screen Capture

- primary-display capture;
- Windows.Graphics.Capture + Direct3D 11 preferred acquisition on Windows 10 build 19041+;
- automatic GDI compatibility fallback for expected unsupported/native WGC failures;
- caller cancellation never becomes fallback work;
- optional cursor path;
- local atomic PNG save.

## Architecture

```text
Tray left-click / Print Screen / capture hotkeys / branded tray menu
    ↓
Snapvere.Application workflows
    ├── Region / Screen → ResilientScreenCaptureService
    │                    ├── preferred: WindowsGraphicsCaptureService
    │                    └── fallback: GdiScreenCaptureService
    │
    └── Window → WindowTargetPicker
                 └── WindowsGraphicsCaptureService.CreateForWindow
    ↓
CaptureFrame (physical BGRA8 pixels)
    ↓
Snapvere.Imaging crop / annotation render / PNG encode
    ↓
CaptureFileWriter or Windows Clipboard
    ↓
Pictures\SNAPVERE / clipboard
```

The tray and hotkey hosts run independently from capture acquisition. The normal WinUI launcher is not activated during tray-first startup.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), [`docs/CAPTURE-ENGINE.md`](docs/CAPTURE-ENGINE.md), [`docs/REGION-CAPTURE.md`](docs/REGION-CAPTURE.md) and [`docs/INSTALLATION.md`](docs/INSTALLATION.md).

## Packaging

Public release builds provide native Windows packages for x64 and x86/32-bit. `x32` is another informal name for x86; duplicate binaries are not published.

Each release publishes:

- `SNAPVERE-<version>-Setup-x64.exe`
- `SNAPVERE-<version>-Portable-x64.exe`
- `SNAPVERE-<version>-Setup-x86.exe`
- `SNAPVERE-<version>-Portable-x86.exe`
- x64/x86 self-contained ZIP payloads
- `SHA256SUMS.txt`

### Uninstall contract

SNAPVERE intentionally does **not** install a separate `uninstall.exe`, `unins000.exe` or Inno-style `unins*.exe`. Windows Installed apps points back to the installed **`SNAPVERE-Setup.exe --uninstall`**. The same Setup binary owns install, maintenance/update and removal.

Screenshots under `Pictures\SNAPVERE` are preserved when the application is uninstalled.

## Automated QA

GitHub Actions validates x64 and x86 on `windows-2022`. The package lifecycle gate checks:

- strict build + unit tests;
- self-contained application publish;
- Setup and Portable EXE generation;
- explicit license acceptance for silent Setup;
- Installed apps uninstall registration and no standalone uninstaller payload;
- legacy activated-window startup probe;
- **tray-only startup probe with the Capture Center left hidden**;
- Region-editor runtime materialization;
- Window-picker runtime materialization;
- normal installed and Portable process survival;
- Setup-based uninstall cleanup.

Hosted CI is not treated as proof of capturing arbitrary protected end-user desktop content. WGC/native capture is production-wired while UI and package surfaces are validated independently.

## Platform and technology

- C# / .NET 10 LTS
- WinUI 3 / Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11
- native Win32/DWM/GDI interoperability
- Windows 10 version 1809 / build 17763 minimum application target
- WGC monitor and Window Capture paths enabled from Windows 10 version 2004 / build 19041
- x64 and x86 public packages; ARM64 remains a source/build target
- strict nullable/analyzer settings and deterministic builds
- central NuGet management
- xUnit tests
- GitHub Actions CI + release automation

## Privacy

SNAPVERE is local-first. Current capture workflows do not upload screenshot pixels, recent-capture history or user files to analytics or telemetry services. Files are saved locally unless the user explicitly copies or shares them elsewhere.

## Current limitations

Intentionally deferred rather than exposed as fake UI:

- coordinated cross-monitor Region freeze composition;
- Scrolling Capture;
- text, blur/pixelate and numbered-step annotations;
- full History management, favorites and Pin to Screen;
- OCR;
- automatic updater;
- Authenticode signing.

## Build

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

## Diagnostics

Startup diagnostics are local at:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Capture pixels are not written to the startup log.

## Branding

Canonical vector identity lives under `assets/branding/`. Product documentation visuals live under `docs/images/`. See [`docs/BRANDING.md`](docs/BRANDING.md).

## License

Source code is distributed under the Mozilla Public License 2.0. Product branding and trademarks are not granted by the source-code license.

---

**SNAPVERE — Capture. Edit. Done.**  
Developed and published by **Brendigo**.
