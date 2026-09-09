# Changelog

All notable SNAPVERE changes are documented here.

## [Unreleased]

### Added

- `Print Screen` global shortcut for Region Capture
- independent `Ctrl+Shift+1` Region fallback when Print Screen is already registered by Windows or another application
- compact tray-first Capture Center with Region/Screen actions, four recent captures, Open folder and Hide to tray
- inline Region annotation tools: Pen, Line, Arrow, Box and Highlight
- Region Copy to Windows clipboard in addition to local PNG Save
- installed and Portable Region-editor runtime probe for x64 and x86 CI
- Windows.Graphics.Capture + Direct3D 11 monitor backend
- Microsoft.Windows.CsWin32 generated D3D interop
- resilient preferred/fallback screen-capture orchestration tests

### Changed

- production `IScreenCaptureService` now prefers Windows.Graphics.Capture on Windows 10 version 2004 / build 19041 and later
- GDI is retained as automatic compatibility fallback and as the supported path for Windows 10 builds below 19041
- caller cancellation no longer risks being converted into a fallback capture attempt
- Region editor visual tree is created programmatically instead of depending on a secondary Window XAML resource
- Region capture is presented as the primary SNAPVERE workflow rather than a large dashboard-style launcher

### Fixed

- Region editor runtime termination caused by a templated WinUI `ProgressRing` during visual-tree materialization on packaged x64/x86 CI runs
- generated COM interface accessibility errors in the WGC backend
- WGC HRESULT helper compilation inside the capture service
- Windows platform analyzer warning for cursor-capture support by guarding WGC monitor acquisition to build 19041+

### Planned

- coordinated cross-monitor Region Capture
- Window Capture and smart targeting
- Scrolling Capture
- richer annotation tools including text and blur/pixelate
- full History, Pin to Screen and OCR
- automatic update mechanism

## [0.0.2] - 2026-09-09

### Added

- stable programmatic `CaptureCenterWindow` used as the production startup window
- startup diagnostics under `%LOCALAPPDATA%\SNAPVERE\Logs\startup.log`
- activated-window `READY` probe for installed and Portable builds
- explicit stage logging around window activation, global hotkey startup and tray startup
- CI normal-launch gates that require the application to remain alive after activation
- full x64/x86 Setup + launch + uninstall + Portable lifecycle validation on Windows Server 2022

### Changed

- application startup no longer instantiates the former complex `MainWindow` composition path
- the primary window uses the standard Windows title bar and conservative WinUI controls for improved runtime compatibility
- WinUI runtime validation runs on `windows-2022`, a supported Windows App SDK 1.8 server target
- release metadata, documentation and package names advance to 0.0.2

### Fixed

- startup failure caused by the former `MainWindow.xaml` `Application.LoadComponent` path (`0x802B000A`)
- subsequent deferred WinUI runtime termination (`0xC000027B`) observed on the unsupported Windows Server 2025 GitHub runner
- release QA distinguishes successful window activation from a process that crashes immediately after activation
- x64 and x86 installed and Portable builds pass the same normal-startup survival gate on the supported CI platform

### Security

- release requires explicit silent license acceptance
- installer cleanup remains constrained by the SNAPVERE installation marker
- Portable extraction traversal and expanded-size protections remain enabled
- release artifacts publish SHA-256 checksums

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
- GUI Setup lifecycle validation waits for the process and checks the real exit code instead of relying on PowerShell `$LASTEXITCODE`
