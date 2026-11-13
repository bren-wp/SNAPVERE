# SNAPVERE — Product & UI specification

**Product:** SNAPVERE  
**Publisher / maintainer:** BRENDIGO · https://brendigo.com  
**Official product domain:** https://snapvere.com  
**Current published desktop package:** v0.2.5  
**Current source implementation:** Tauri 2 + Rust + React/TypeScript; this is not the separate WinUI implementation presently displayed on the public domain.

This document describes the **current codebase**, not an aspirational feature list. Availability must be checked against actual source, native tests and Windows release artifacts.

## Product principles

- Fast screenshot capture and optional screen recording with no mandatory user account.
- Local-first file storage and a SQLite gallery.
- Reliable screen selection: the desktop must remain visible behind the selection overlay.
- Honest affordances: no controls for unimplemented upload or window-capture features.
- Consistent SNAPVERE visual language in the Windows app, the repository website and release materials.
- Official branding throughout shipped resources; third-party licence obligations remain applicable.

## Implemented interface surfaces

| Window | Purpose | Important behavior |
|---|---|---|
| Home | Searchable capture library | Region capture, full-screen capture, recording start, settings, filters, media cards |
| Overlay | Select screenshot or recording region | Real frozen desktop in screenshot mode; transparent live desktop in recording mode; Esc cancels |
| Editor | Image annotations | Draw shapes, text, blur/redact, undo/redo and export to disk/clipboard |
| Viewer | Screenshot/MP4 preview | Image navigation, zoom, media controls, reveal/delete/copy actions |
| Settings | Seven tabs | General, Hotkeys, Capture, Recording, Save, Privacy, About |
| RecBar | Recording HUD | Pause/resume, stop, annotation tools, sound controls |
| RecDraw | Recording annotations | Separate composited drawing layer |
| RecFrame | Recording boundary | Frame and recording perimeter display |
| Website | Marketing/download/support information | Home, download center and privacy; responsive layouts |

These components have different window constraints. In particular, applying the standard app header to the transparent overlay, recdraw, recframe or recording HUD would break capture geometry and is not permitted.

## Brand & UI tokens

The canonical SNAPVERE dark palette and shape values are stored under the `--brand-*` variables in both `site/styles.css` and `apps/desktop/src/styles.css`. The CI brand check requires them to match.

- Dark base: `#0a0a0b`
- Content surface: `#141416`; elevated surface: `#1a1a1d`
- Main accent: `#09bfd9`; highlight: `#55e0e5`
- Main text: `#f5f5f7`; secondary text: `#a1a1a6`
- Cards: 18px radius; primary pill controls: fully rounded
- SNAPVERE wordmark and product silhouette shared with the website vector favicon

Dark and light desktop themes must preserve readable contrast and keyboard focus. Reduced-motion preferences disable unnecessary animations. Use tested responsive layouts at the main window's minimum dimensions and on 100/125/150/200% system scaling.

## Screenshot capture invariants

1. Hotkey/tray/button requests run screenshot acquisition on a background Rust thread.
2. The cursor's monitor is captured before overlay visibility changes.
3. The current RGBA frame stays in memory and is returned by the scoped Tauri IPC command `get_frozen_rgba`.
4. Packet format: little-endian 32-bit width and height followed by exactly `4 × width × height` RGBA bytes.
5. The overlay validates packet length and dimensions before drawing it to its canvas.
6. A PNG data URL generated from the same memory frame is the fallback for incompatible WebView2 binary IPC.
7. If both display routes fail, report a visible error, cancel capture and release frozen memory — **never show a black overlay as a successful capture**.
8. The overlay is hidden and frozen frames released on Escape, cancellation, completion and server-side presentation failures.
9. No `frozen.png` temporary image is required or written for selection.
10. Recording region selection remains live and transparent; screenshot-mode pixel loading must not disable it.

A fully black image captured by the operating system from protected, secure or exclusive-fullscreen content cannot always be prevented in application code. Such conditions should be diagnosed separately from an image-transfer failure.

## Release and distribution

Windows release artifacts: `Snapvere-Portable.exe`, `Snapvere-Setup.exe`, `Snapvere-Setup.msi`, `SHA256SUMS.txt`. The website's download mirror is kept first-party, with SHA-256 verification. A Git branch update alone does not deploy the live website or create a new release.

The current unsigned preview packages may show Windows SmartScreen warnings. Do not claim code signing, Windows production field validation or zero visual deviation without the corresponding evidence.

## Feature boundaries

Active-window screenshot capture, remote storage, password-protected share links, account services and production cloud upload are **not supported shipping features in this codebase**. Do not show nonfunctional controls or describe them as available in marketing material.

## Quality gates

- `npm ci`, `npm run build` and a production/development dependency audit.
- Rust formatting, unit tests, native Windows build, and portable/NSIS/MSI packaging.
- Shared brand/token parity, first-party website links and SHA checksums.
- Chromium-rendered screenshot previews: Home, compact Home, all Settings tabs, Editor and desktop/mobile pages.
- Chromium in-memory frame visibility regression check for screenshot region selection.
- Manual Windows 10/11 field testing of capture, multi-monitor geometry, DPI, WebView2/driver differences, hotkeys, recording audio and actual physical device performance before release acceptance.

**Brand and legal:** The app and marketing identity must show SNAPVERE and BRENDIGO exclusively as the product/publisher identities. Copyright and licence notices associated with third-party materials may not be falsely reassigned or suppressed.
