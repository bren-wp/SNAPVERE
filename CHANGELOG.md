# SNAPVERE — Release history

SNAPVERE is a Windows screenshot and recording workspace published by **BRENDIGO**.

## v0.2.9 — premium stability candidate (unreleased)

- Split the gallery viewer into image/video/error modules; extracted Settings controls and validated preference model.
- Expanded video speeds (including 0.75× and 1.25×), added keyboard-seek accessibility, repaired corrupted volume preferences and reduced playback CPU updates to 10 Hz.
- Added keyboard precision selection: arrows move by 1 px, Shift+arrow by 10 px, Alt+arrow resizes, with tested viewport clamping.
- Split native PNG editor validation and recording pixel-processing into isolated Rust modules; improved negative-coordinate capture and truncated-driver-frame guards.
- Serialized recording startup, guarded session cleanup against stale workers and gave recordings distinct filenames.
- Recovered from blocked WebView storage and corrupt settings, rolled back failed autostart changes and handled failed save-folder dialogs.
- Added a React error boundary with a recovery screen so a component render failure does not silently blank the WebView.
- Fixed a focus/keyboard collision between the video seek slider and gallery navigation; recovered from WebView localStorage getter failures before the player mounts.
- Removed `innerHTML` from consent/download flows to reduce web-script injection risk.
- Added detailed source/permission audit evidence and verified release metadata across Cargo, npm and Tauri.
- Removed the obsolete native fs/os plugin dependencies and prepared an isolated, manually reviewed dependency lockfile refresh (including React 19 migration checks), with RustSec and npm advisory gates.
- Reduced release supply-chain exposure: prevent Git checkout from persisting write-capable credentials and install npm packages with lifecycle scripts disabled.
- Added an explicit offline-app source guard rejecting unreviewed outbound HTTP/process-launch primitives, install-time npm lifecycle scripts and hidden WebView network requests. Reviewed the only public third-party script: consent-gated Google Analytics, not the native app.
- Retired obsolete branch-specific privileged workflows for earlier 0.2.4–0.2.6 releases. The active dependency refresh remains scoped to this open PR.

**Release gate:** this version remains an unmerged candidate until Windows Rust fmt/test/Clippy, portable+NSIS+MSI, hashes and device-level acceptance are complete. The public website still serves a separate WinUI edition.

## v0.2.8 — November 13, 2026 (Tauri/Rust preview)

- Added a versioned Brendigo commercial-use installer license with an acceptance page for NSIS/MSI, matching first-party website terms and an in-app About link; previously issued MIT and third-party open-source rights remain intact.
- Reduced peak memory usage for PNG saves by encoding borrowed RGBA pixels; bounded gallery search and tag input; eliminated duplicate tag-join results in SQLite with a correlated EXISTS query.
- Bounded image clipboard decoding by source size and pixel dimensions; added a one-click Copy file path option (also Ctrl+Shift+C) for images and recordings; corrected the web download guide to reflect opt-in autostart.
- Replaced the 600 ms fixed delay during Quit with a bounded wait for actual recorder finalization and SQLite registration, reducing truncated recordings on slower storage.
- Changed Save As to stage complete image bytes in the destination directory and rename only after a successful write, preventing truncation of existing files when an export fails.
- Added a CI contract check for every frontend-to-Rust IPC command, plus an audit report distinguishing verified static checks from device-level and third-party security review still outstanding.
- Hardened website and application CI to reject public GitHub links and verify the installer license and legal navigation.
- Removed unused Tauri filesystem and operating-system plugins and corresponding webview capabilities from runtime, reducing the native IPC attack surface.

- Fixed screenshot cropping at negative virtual-desktop coordinates and monitor edges, with Rust regression coverage.
- Connected fullscreen, region and editor Save operations to persisted image format and quality settings; Save As defaults to the configured extension, recognizes JPEG and rejects unsupported extensions. Added Node unit tests.
- Preserve the overlay and annotations when export fails. Clean up partial captures and generated thumbnails on disk or gallery database errors; never delete original recorded video on indexing failure.
- Made gallery search treat literal %, _ and backslash correctly instead of unintended SQL LIKE wildcards.
- Added stable, bounded SQLite pagination and a Load more action so galleries and search results no longer silently stop at 300/500 items. The pagination cursor remains correct after deletion or refresh, with duplicate prevention and frontend unit tests.
- Kept SQLite foreign keys enabled in the in-memory fallback, preserving referential integrity of tags.
- Propagated tag query errors rather than silently losing tags.
- Restricted environment-supplied WebView2 remote debugging flags to debug builds.
- Removed CI auto-commits of generated source/images and excessive quality workflow write permissions.
- Introduced non-mutating Rust formatting, strict Clippy gating, and a working TypeScript static-analysis lint command.
- Removed old release-note claims and distinguished the GitHub Tauri edition from the separately deployed .NET/WinUI website.

**Release type:** preview, pending native Windows hardware, DPI, multi-monitor, audio, install/upgrade and pixel-perfect reference validation. The TypeScript compiler's strict and unused-symbol checks are enforced; ESLint is not yet a locked dependency and must be added separately before claiming ESLint coverage.

## v0.2.7 — November 2026

- Improved Windows global shortcut rebinding with rollback and duplicate-key validation.
- Fixed dynamic version display, shortcut preferences and accessible Settings controls.
- Hidden gallery during full-screen capture, restoring the window afterward.
- Authorized newly saved files for immediate viewing in the editor.
- Replaced the old screenshot mockup with real source-rendered application previews.
- Aligned native and website navy/violet design tokens and regenerated matching favicon, ICO, ICNS and PNG assets.
- Added a visual check that rejects black or unloaded editor previews.
- Updated README, responsive-site review and Windows release QA.

**Validation requirements:** frontend/TypeScript build, npm audit, Rust tests, formatter, Windows Portable/NSIS/MSI packaging and SHA-256 verification. New binaries are not described as signed when no signing certificate is available.

## v0.2.6 — November 13, 2026

- Moved region screenshot transfer to in-memory RGBA with PNG fallback.
- Improved failure cleanup and tests for screenshot selection.
- Updated Tauri frontend/native dependencies and real screenshot documentation.
- Published Portable EXE, Setup EXE, MSI and SHA256SUMS.txt.

## Note on the public website

The separately deployed public snapvere.com currently serves a different implementation and its routing/assets are managed independently. Repository screenshots do not certify 1:1 identity with the deployed live site.
