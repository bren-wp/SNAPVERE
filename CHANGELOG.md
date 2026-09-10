# Changelog

All notable SNAPVERE changes are documented here.

## [Unreleased]

### Added

- real rendered x64 WinUI visual-QA capture for Region, Window, Tray, Options, Language and About surfaces
- GitHub Actions visual-QA artifact containing six PNGs plus a manifest with dimensions, byte sizes and SHA-256 digests
- regression coverage for dedicated Croatian secondary-UI strings, canonical English fallback and formatted localization placeholders

### Changed

- remaining Options, Recent Captures, Language Picker and About user-facing copy now resolves through the shared `SnapvereLocalization` catalog
- Croatian now has dedicated translations for the current secondary product surfaces while other incomplete languages retain canonical English fallback
- living README, architecture, installation, settings, tray and branding documentation now describes the v0.0.8/post-release universal packaging and visual-QA contract

### Fixed

- localization tests no longer assume that a canonical English value must differ from its resource key (for example `Startup`)
- stale documentation that still described `main` as a 0.0.4/0.0.7 development line or the package model as separate x64/x86 outputs

### Planned

- coordinated cross-monitor Region Capture
- monitor-under-cursor and all-monitors Screen Capture options
- Scrolling Capture
- richer annotation tools including text, ellipse, blur/pixelate and numbered steps
- expanded History, Pin to Screen and OCR
- automatic update mechanism
- Authenticode signing

## [0.0.8] - 2026-09-10

### Changed

- Region Capture uses shared process-local language state for its title, tool names, tooltips, resize accessibility labels, annotation color labels, capture guidance, Copy / Save / Close actions and working-state text
- Window Capture uses the same language state for its title, guidance and keyboard accessibility label
- Croatian includes dedicated strings for the newly localized capture surfaces and generic Region Capture failure states
- English remains the canonical fallback for capture-surface strings not yet translated in another selected language
- historical v0.0.4 through v0.0.7 release workflow definitions are retained under `.github/release-archive/` instead of remaining active workflows
- the v0.0.8 release workflow is scoped to its dedicated `.github/release-triggers/v0.0.8` path on `main`

### Packaging

- the release continues the v0.0.7 two-download contract with exactly `SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe`
- each public executable embeds x86, x64 and ARM64 application payloads and selects a compatible native payload at runtime
- Windows 10 version 1809 / build 17763 remains the application minimum; WGC-dependent paths require build 19041 or later

### Reliability and privacy

- capture-surface localization remains static, local and event-driven with no translation network service, polling worker, file watcher or telemetry
- Region Capture geometry, annotation rendering, WGC/GDI capture engines and tray-first startup contract are unchanged by this release
- release publication is gated by x64 build/tests, x86 build, ARM64 cross-build, payload validation, exact two-file packaging and x64/x86 Setup/Portable lifecycle probes
- ARM64 validation on the hosted x64 runner is cross-build/package validation, not a real ARM64 hardware runtime test
- release binaries are not represented as Authenticode-signed; SHA-256 values are integrity metadata only

## [0.0.7] - 2026-09-10

### Added

- universal public Setup and Portable hosts that embed x86, x64 and ARM64 native application payloads
- built-in language selector exposing 28 choices with English as the canonical default/fallback and Croatian among the supported selections
- shared universal architecture resolver for x86/x32, x64/AMD64 and ARM64 payload selection
- commercial SNAPVERE license contract for v0.0.7 and later

### Changed

- public GitHub releases contain exactly two user-facing downloads: `SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe`
- users no longer choose architecture-specific x86/x64 downloads; the host selects a compatible embedded payload
- Tray, Region, Options, Language, About and Setup surfaces use the graphite/violet/cyan SNAPVERE visual system
- interactive Setup defaults Start menu shortcut, Desktop icon and Start-with-Windows to On while keeping them user-selectable
- language preference is stored locally in `%LOCALAPPDATA%\SNAPVERE\settings.json`

### Reliability, security and performance

- obsolete Demo packaging branches and duplicate launcher behavior were removed
- ZIP extraction retains path-traversal and bounded extraction protections
- settings writes remain local and atomic
- startup, shortcut and uninstall lifecycle is validated in CI
- tray/hotkey idle behavior remains event-driven and localization adds no polling thread or network worker
- capture/D3D resources remain lazy rather than initializing merely for tray residency
- publication requires x64 build/test, x86 build, ARM64 cross-build, all three payloads, exact two-file output, x64/x86 universal lifecycle, commercial-license acceptance, startup/shortcut state and same-Setup uninstall validation

## [0.0.6] - 2026-09-10

### Added

- redesigned premium tray quick-action flyout using the graphite + violet/indigo/cyan SNAPVERE identity
- redesigned Region Capture selection chrome and horizontal floating annotation/action toolbar
- redesigned Window Capture target highlight/title surface
- redesigned Options / Recent Captures and About secondary windows
- redesigned Setup/Uninstall interface while preserving the existing installer contract
- installed and Portable `SECONDARY_UI_READY` runtime probe that materializes tray flyout, Options and About
- dedicated v0.0.6 release workflow using the current package and tray-first QA scripts

### Changed

- normal manual launch is tray-first again and no longer activates the legacy Capture Center
- the former `CaptureCenterWindow` is reduced to a hidden runtime/capture coordinator rather than a product dashboard
- Windows startup uses the same plain installed/Portable executable command as normal tray-first launch
- Region, Window and Screen capture resolve the persisted `Include cursor on capture` preference at execution time
- setup branding, spacing, progress treatment and product messaging now match the rest of the v0.0.6 UI
- release QA now gates secondary product surfaces in addition to startup, Region and Window probes

### Fixed

- v0.0.5 manual-launch behavior that reopened the Capture Center contrary to the intended tray-first UX
- `Include cursor on capture` being persisted by the settings surface but ignored by the hidden capture coordinator
- Setup custom-control compile failures caused by WinForms analyzer serialization requirements and inaccessible rounded-path helpers
- startup/uninstall command mismatch introduced by the temporary `--background` startup path
- release automation dependency on the removed `Test-SnapvereInteractiveLaunch.ps1` script

### Security

- no telemetry, account, cloud-upload or network dependency was introduced by the redesign
- same-Setup uninstall validation and constrained install-directory cleanup remain mandatory
- uninstall still removes `Run\SNAPVERE` only when it exactly matches the validated installed executable
- x64/x86 publication is gated by build/test, Setup/Portable lifecycle, tray-first, Region, Window, secondary-UI and uninstall validation
- release assets are constrained to the expected seven-file set and publish SHA-256 checksums

## [0.0.5] - 2026-09-10

### Changed

- manual application launches activated the Capture Center while Windows startup used a separate `--background` tray-only path
- Setup received an automated UI materialization probe

### Superseded

- v0.0.6 restores one consistent tray-first launch contract for normal, startup and Portable execution and supersedes v0.0.5 for normal downloads

## [0.0.4] - 2026-09-10

### Added

- true tray-first normal startup that keeps the Capture Center hidden
- left-click notification-area action that starts Region Capture immediately
- branded programmatic WinUI tray flyout for right-click quick actions
- separate Recent captures and Options / Preferences tray actions
- real secondary `OptionsWindow` rather than routing the tray back to the capture dashboard
- implemented `Start SNAPVERE with Windows` per-user preference
- implemented `Include cursor on capture` preference applied by Region, Window and Screen workflows
- local atomic `CapturePreferencesService` storage under `%LOCALAPPDATA%\SNAPVERE\settings.json`
- real Recent Captures section with refresh, open capture and open-folder actions
- stable Portable-launcher path handoff for Windows startup registration
- dedicated installed and Portable `TRAY_READY` runtime probe
- repository-maintained tray-first, tray-menu and Region-editor SVG workflow illustrations
- refreshed violet SNAPVERE shard/feather identity, README wordmarks and branded tray surfaces
- `docs/TRAY-UX.md`, `docs/WINDOW-CAPTURE.md` and `docs/SETTINGS.md`
- v0.0.4 release automation and package metadata

### Changed

- the notification-area icon is the primary persistent application surface instead of an always-open launcher
- normal application launch initializes the hotkey/tray hosts without activating the Capture Center
- right-click tray interaction uses SNAPVERE's branded WinUI command surface instead of a generic native text popup
- tray event dispatch is routed through the application WinUI `DispatcherQueue`
- `CaptureCenterWindow` remains a hidden capture coordinator; capture actions belong to tray/hotkeys/overlays
- WGC/D3D monitor acquisition is documented as the preferred supported backend with GDI compatibility fallback
- documentation now reflects the real Window Capture multi-monitor picker, tray startup model, local settings, uninstall startup cleanup and current limitations
- README explicitly identifies repository SVGs as workflow illustrations rather than pixel-identical runtime screenshots
- product tagline for current branding is `Capture. Edit. Done.`

### Fixed

- Portable validation recognizes the tray-only startup probe
- normal package QA explicitly validates that the application remains alive in hidden tray-first mode
- left-click/double-click tray messages are debounced so a double-click does not trigger duplicate Region captures
- tray Options no longer opens the old visible capture-launcher dashboard
- installed Windows startup registration is removed by Setup uninstall when it points exactly to the validated installed `Snapvere.exe`
- Portable Windows startup registration no longer risks pointing to a temporary extracted child executable
- stale architecture/security documentation that still described older 0.0.1/0.0.2 backend/startup behavior

### Security

- no telemetry, cloud-upload or network dependency was added by the tray-first redesign
- settings remain local and are written through an atomic temp-file replacement path
- Setup uninstall remains owned by the same installed `SNAPVERE-Setup.exe --uninstall`
- uninstall removes `Run\SNAPVERE` only when it exactly matches the validated installed executable, preserving unrelated/Portable registrations
- package lifecycle now creates an installed startup registration and requires uninstall to remove it
- CI continues to reject standalone `uninstall*.exe` and Inno-style `unins*.exe` payloads
- x64/x86 release publication remains gated by build/test, install, tray, Region, Window, Portable and uninstall lifecycle validation
- release artifacts continue to publish SHA-256 checksums

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
