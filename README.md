# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture anything." src="assets/branding/readme/snapvere-logo-light.svg" width="520">
</picture>

**Premium Screen Capture for Windows — developed and published by Brendigo.**

SNAPVERE is a local-first Windows capture application focused on fast region selection, precise mixed-DPI geometry, dependable clipboard/save workflows, professional annotation, privacy-safe redaction, OCR, pin-to-screen and robust scrolling capture.

## Development status

SNAPVERE is under active development. The current codebase contains a working primary-display screenshot workflow, atomic local PNG persistence and the first freeze-frame Region Capture overlay. Features are listed as implemented only when working code exists; planned capabilities remain disabled in the UI until their real workflow exists.

## Technology

- C# / .NET 10 LTS (`10.0.12`, SDK `10.0.401`)
- WinUI 3
- Windows App SDK 1.8 stable line
- Windows 11 x64 and ARM64 targets
- Nullable reference types, .NET analyzers and central package management
- xUnit automated tests
- GitHub Actions on Windows

## Architecture

Capture acquisition, application orchestration, image processing and WinUI presentation are separate projects.

```text
WinUI action / future hotkey
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

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) and [`docs/REGION-CAPTURE.md`](docs/REGION-CAPTURE.md).

## Implemented now

### Production foundation

- strict compiler/analyzer configuration;
- central NuGet package management;
- x64/ARM64 application targets;
- per-monitor-v2 DPI manifest;
- WinUI shell with System/Light/Dark-ready resources;
- GitHub Actions restore → Release x64 build → test pipeline;
- canonical SNAPVERE SVG symbol and README brand assets.

### Capture and geometry

- native Win32 active-display discovery;
- primary monitor selection;
- physical virtual-desktop coordinates, including negative X/Y origins;
- 100/125/150/175/200% DPI conversion coverage;
- sub-DIP XAML pointer conversion without premature integer rounding;
- validated BGRA8 `CaptureFrame` contract;
- GDI monitor-capture compatibility backend with optional cursor rendering;
- pixel-accurate BGRA8 cropper that respects source stride.

### Screen Capture

- real primary-display capture;
- PNG encoding without an external imaging dependency;
- collision-safe file naming;
- temp-file + atomic-move persistence;
- default save folder `Pictures\SNAPVERE`.

### Region Capture

- main-window hide before freeze acquisition;
- frozen primary-display preview;
- borderless always-on-top selection overlay;
- four-sided dimming around the active selection;
- reverse-direction drag normalization;
- drag-to-move existing selection;
- eight resize handles;
- physical-pixel W × H badge;
- arrow-key 1 px movement;
- Shift + Arrow 10 px movement;
- Enter/double-click save and Esc cancel;
- final crop from the same frozen frame shown in the overlay;
- shared atomic PNG writer with Screen Capture.

## Current limitations

The following are not yet complete and are intentionally not exposed as finished capabilities:

- cross-monitor Region Capture over one coordinated virtual-desktop overlay;
- Windows.Graphics.Capture/D3D primary backend;
- Window Capture;
- Scrolling Capture;
- clipboard output;
- global hotkeys and tray workflow;
- Quick Actions;
- annotation editor;
- History and Pin to Screen;
- OCR;
- installer/portable packaging, updater and licensing foundation.

## Roadmap

1. Harden Region Capture on mixed-DPI multi-monitor layouts
2. Add Windows.Graphics.Capture/D3D primary acquisition with GDI fallback
3. Implement global hotkeys and tray workflow
4. Implement Window Capture and smart target selection
5. Add clipboard and Quick Actions
6. Build the non-destructive annotation editor
7. Add History and Pin to Screen
8. Add local OCR with compatible fallback
9. Implement scrolling-capture stitching
10. Add installer, portable packaging, signed updates and licensing foundation
11. Complete accessibility, HDR, mixed-DPI hardening and release QA

## Build

Use Windows 11 with Visual Studio Build Tools/Visual Studio including Windows application development prerequisites and the .NET SDK defined in `global.json`.

```powershell
dotnet restore SNAPVERE.sln
dotnet build SNAPVERE.sln -c Release -p:Platform=x64
dotnet test tests/Snapvere.UnitTests/Snapvere.UnitTests.csproj -c Release -p:Platform=x64
```

## Privacy

SNAPVERE is designed to capture, edit, OCR, save and retain history locally. Screenshot pixels, OCR text, clipboard contents and user files must not be sent to third parties without an explicit user action.

## Security

Private signing keys, production credentials, user screenshots, dumps and local runtime data must never be committed. Update and licensing infrastructure will use signed verifiable data rather than trusting transport alone.

## Branding

SNAPVERE branding is maintained from canonical vector sources under `assets/branding/`. See [`docs/BRANDING.md`](docs/BRANDING.md).

## License

Source code is currently distributed under the repository's Mozilla Public License 2.0. Product branding and trademarks are not granted by the source-code license.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md).

---

**SNAPVERE** — Capture anything.  
Developed and published by **Brendigo**.
