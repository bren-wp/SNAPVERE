# Changelog

All notable SNAPVERE changes are documented here.

## [Unreleased]

### Planned

- coordinated cross-monitor Region Capture
- Scrolling Capture
- richer annotation tools including text and blur/pixelate
- full History, Pin to Screen and OCR
- automatic update mechanism

## [0.0.3] - 2026-09-10

### Added

- production Window Capture workflow using Windows.Graphics.Capture `CreateForWindow`
- native top-level window discovery with DWM extended-frame bounds
- overlay-safe Z-order window hit testing that ignores SNAPVERE's own picker surfaces
- multi-monitor Window Capture picker with one DPI-aware frozen overlay per monitor
- Window Capture launcher action
- global `Ctrl+Shift+2` Window Capture shortcut
- Window Capture action in the native system tray menu
- installed and Portable Window-picker runtime probe for x64 and x86 CI
- centralized package lifecycle validation shared by CI and release automation
- explicit install-contract validation for Windows Installed apps registration

### Changed

- current CI/package version advances to 0.0.3
- capture launcher now presents Region, Window and Screen as implemented workflows
- tray and global-hotkey surfaces expose Window Capture only after its backend, picker and workflow became real
- production `IScreenCaptureService` prefers Windows.Graphics.Capture on Windows 10 version 2004 / build 19041 and later
- GDI remains the automatic monitor-capture compatibility fallback and supported path below build 19041
- Region editor visual tree remains programmatic to avoid fragile secondary Window XAML loading
- package lifecycle smoke tests now validate main-window, Region-editor and Window-picker WinUI materialization

### Fixed

- Window Capture hover targeting no longer resolves SNAPVERE's own always-on-top overlay instead of the window below it
- Window Capture unit tests no longer depend on unavailable xUnit `TestContext`
- generated COM interface accessibility and HRESULT helper issues in the WGC backend
- caller cancellation no longer risks becoming a fallback monitor-capture attempt
- Region editor runtime termination caused by a templated WinUI `ProgressRing` during packaged validation

### Security

- uninstall remains handled by the installed `SNAPVERE-Setup.exe --uninstall`; no standalone uninstaller binary is generated
- package QA rejects both `uninstall*.exe` and Inno-style `unins*.exe` files in the installation directory
- CI validates `UninstallString`, `QuietUninstallString`, install location and version metadata before uninstall
- uninstall QA confirms the Windows Installed apps registration is removed while screenshots are preserved
- release artifacts continue to publish SHA-256 checksums

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
