# Changelog

All notable SNAPVERE changes are documented here.

## [Unreleased]

### Planned

- coordinated cross-monitor Region Capture
- Windows.Graphics.Capture/D3D primary backend
- Window Capture and smart targeting
- Scrolling Capture
- clipboard Quick Actions
- annotation Editor
- full History, Pin to Screen and OCR
- automatic update mechanism

## [0.0.1] - 2026-09-09

### Added

- .NET 10 / WinUI 3 solution foundation
- Windows App SDK stable package pin
- central package management and strict analyzer settings
- WinUI application shell
- capture domain geometry and validated BGRA8 frame contract
- native active-display discovery and virtual-desktop mapping
- explicit logical/physical DPI coordinate transformer
- 100/125/150/175/200% DPI and negative-coordinate unit tests
- GDI monitor-capture compatibility backend with optional cursor rendering
- deterministic PNG encoder
- pixel-accurate BGRA8 frame cropper with padded-stride support
- collision-safe `Pictures\SNAPVERE` capture paths
- shared temp-file + atomic-move PNG persistence
- working primary Screen Capture workflow
- freeze-frame primary-display Region Capture workflow
- borderless Region overlay with dimming, live dimensions and eight resize handles
- Region move, keyboard nudge, Enter/double-click commit and Esc cancel interactions
- conflict-aware `Ctrl+Shift+1` Region and `Ctrl+Shift+4` Screen global hotkeys
- native Windows system tray with Show, Region, Screen and Exit actions
- tray recovery after Windows Explorer restart
- local Recent Captures filesystem index
- x86/32-bit application target in addition to x64
- self-contained x64/x86 publish validation
- modern per-user SNAPVERE Setup executable with MPL 2.0 license acceptance
- Windows Installed apps uninstall registration using `SNAPVERE-Setup.exe --uninstall`
- optional Start menu/Desktop shortcuts
- single-file x64/x86 Portable launchers
- guarded embedded ZIP extraction with directory-traversal protection
- package extraction security unit tests
- installer lifecycle smoke tests for both x64 and x86
- release SHA-256 checksum generation
- GitHub Actions release automation for v0.0.1
- canonical SNAPVERE vector symbol and dark/light README assets
- architecture, branding, Region Capture, installation, contribution and security documentation

### Changed

- capture persistence moved out of individual workflows into `CaptureFileWriter`
- DPI conversion accepts sub-DIP pointer coordinates to avoid premature rounding
- main capture UI exposes implemented hotkeys and release version information
- Region overlay uses SNAPVERE branding and clearer keyboard guidance
- unfinished History, Editor, Window and Scrolling workflows remain disabled instead of appearing functional
- CI validates x64 build/test, x86 build, x64/x86 packaging, license-gated silent install and uninstall cleanup
- v0.0.1 release artifacts are intentionally unsigned; integrity is provided through published SHA-256 checksums

### Security

- installer/portable extraction rejects absolute paths and paths escaping the package destination
- embedded extraction is size/entry bounded and uses temporary files before final replacement
- installer writes and verifies a SNAPVERE installation marker before destructive uninstall cleanup
- uninstall uses the installed Setup executable and a constrained maintenance process rather than a shell command
- default installation is per-user and does not request elevation
- silent installation requires explicit `--accept-license`
- release workflow publishes SHA-256 checksums

### Fixed

- solution test-project build configuration in CI
- xUnit global using required for test compilation
- WinUI `Application` type ambiguity introduced by the `Snapvere.Application` namespace
- payload directory-entry suffix handling in release packaging
- GUI Setup lifecycle validation now waits for the process and checks the real exit code instead of relying on PowerShell `$LASTEXITCODE`
