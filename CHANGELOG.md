# SNAPVERE — Release history

SNAPVERE is a Windows screenshot and recording workspace published by **BRENDIGO**.

## v0.2.8 — November 13, 2026 (Tauri/Rust preview)

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
