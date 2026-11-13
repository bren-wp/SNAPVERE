# SNAPVERE — Release history

SNAPVERE is a Windows screenshot and recording workspace published by **BRENDIGO** at https://snapvere.com.

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
