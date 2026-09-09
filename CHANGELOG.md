# Changelog

All notable SNAPVERE changes are documented here. The project has not reached a production release yet.

## [Unreleased]

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
- Region workflow tests for negative desktop coordinates and display-change detection
- Windows GitHub Actions CI foundation
- canonical SNAPVERE vector symbol
- dark/light README logo assets
- architecture, branding, Region Capture, contribution and security documentation

### Changed

- capture persistence moved out of individual workflows into `CaptureFileWriter`
- DPI conversion now accepts sub-DIP pointer coordinates to avoid premature rounding
- unfinished History, Editor, Window and Scrolling workflows remain disabled rather than appearing functional

### Fixed

- solution test-project build configuration in CI
- xUnit global using required for test compilation
- WinUI `Application` type ambiguity introduced by the `Snapvere.Application` namespace
