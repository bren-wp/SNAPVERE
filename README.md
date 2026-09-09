# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture anything." src="assets/branding/readme/snapvere-logo-light.svg" width="520">
</picture>

**Premium Screen Capture for Windows — developed and published by Brendigo.**

SNAPVERE is a local-first Windows capture application focused on fast region selection, precise mixed-DPI geometry, dependable clipboard/save workflows, professional annotation, privacy-safe redaction, OCR, pin-to-screen and robust scrolling capture.

## Development status

SNAPVERE is under active development. The repository currently contains the production foundation, WinUI 3 application shell, capture domain contracts, DPI coordinate transformation, initial unit tests, CI, and the first canonical brand assets. Features are listed as implemented only when working code exists; planned features are not presented as finished.

## Technology

- C# / .NET 10 LTS (`10.0.12`, SDK `10.0.401`)
- WinUI 3
- Windows App SDK `1.8.11` (`Microsoft.WindowsAppSDK` `1.8.260804001`)
- Windows 11 x64 and ARM64 targets
- Nullable reference types, .NET analyzers and central package management
- xUnit test foundation
- GitHub Actions on Windows

## Architecture

The capture engine, image pipeline and UI are separate projects. UI code does not own screen-capture geometry or frame contracts.

```text
Capture source
    ↓
Snapvere.Capture
    ↓
CaptureFrame / domain geometry
    ↓
Snapvere.Imaging
    ↓
Clipboard / Save / Editor / OCR
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Current implemented foundation

- production solution and project structure
- strict compiler/analyzer configuration
- x64/ARM64 application target configuration
- WinUI shell and navigation foundation
- System/Light/Dark-ready resource foundation
- per-monitor-v2 DPI application manifest
- capture mode/domain models
- validated BGRA8 `CaptureFrame` contract
- logical ↔ physical DPI coordinate transformation
- tests for 100%, 125%, 150%, 200%, negative coordinates and mixed-axis DPI
- Windows CI restore/build/test workflow
- canonical SNAPVERE SVG symbol and dark/light README logos

## Roadmap

1. Native display discovery and virtual desktop mapping
2. Windows.Graphics.Capture full-screen/monitor capture
3. Freeze-frame region overlay and precise selection handles
4. Clipboard and PNG/JPEG/WebP encoding
5. Global hotkeys and tray workflow
6. Quick Actions
7. Non-destructive annotation editor
8. History and Pin to Screen
9. Local OCR with compatible fallback
10. Scrolling capture stitching
11. Installer, portable packaging, signed updates and licensing foundation
12. Accessibility, HDR, mixed-DPI hardening and release QA

## Build

Use Windows 11 with Visual Studio Build Tools/Visual Studio including Windows application development prerequisites and .NET SDK defined in `global.json`.

```powershell
dotnet restore SNAPVERE.sln
dotnet build SNAPVERE.sln -c Release -p:Platform=x64
dotnet test tests/Snapvere.UnitTests/Snapvere.UnitTests.csproj -c Release -p:Platform=x64
```

## Privacy

SNAPVERE is designed to capture, edit, OCR, save and retain history locally. No screenshot pixels, OCR text, clipboard contents or user files should be sent to third parties without an explicit user action.

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
