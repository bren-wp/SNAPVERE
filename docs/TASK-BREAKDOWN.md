# SNAPVERE — Verification matrix and outstanding work

**Publisher:** BRENDIGO · https://brendigo.com  
**Product:** https://snapvere.com

This file replaces the outdated initial-project scaffolding task list. It documents engineering verification rather than claiming future features are complete.

## Verified in source / CI

| Area | Status | Evidence / verification |
|---|---|---|
| Site assets and first-party downloads | Implemented | SHA-256 manifest verification and website path checks |
| App and web visual tokens | Implemented | `tools/check_brand_tokens.py` |
| Brand identity in tracked paths/content | Implemented | CI search and `tools/check_capture_pipeline.py` |
| Home, Settings, Editor, website previews | Implemented | Playwright-generated images in `docs/screenshots` |
| Screenshot frozen-frame binary transfer | Implemented in development PR | Tauri IPC, header/pixel validation and PNG memory fallback |
| Black-overlay error handling | Implemented in development PR | Cancel and report failure rather than display an empty canvas |
| Windows package build | Published v0.2.5 | Native build workflow; three binaries and checksum manifest |
| Recording subsystem and HUD | Implemented | Rust/React source, release tests; live Windows device QA still required |

## Acceptance tests still requiring real Windows hardware

- Region capture over Chrome, Edge, desktop wallpaper, regular applications and multi-monitor configurations.
- Confirm rendered selection matches the desktop visually at 100%, 125%, 150% and 200% DPI, including negative virtual monitor coordinates.
- Confirm no black rectangle is shown when selecting a browser page; test the in-memory fallback under older WebView2 versions.
- Test full-screen capture, screen recording, pause/resume, microphone/system audio and short/long recording finalization.
- Confirm overlay/HUD remain above the intended monitor but do not appear inside screenshots or recordings.
- Check keyboard navigation, focus outlines, 200%-400% zoom and accessibility for every primary window.
- Compare each shipping screen against the approved visual reference at matching viewport dimensions; measure and correct differences before describing the result as pixel-perfect.
- Confirm first-party website deployment and versioned downloads after merge; branch screenshots and released packages are separate from live deployment.

## Excluded from shipping UI

No placeholder active-window capture, incomplete private upload buttons, mock donation features, unverified cloud upload claims or stale version banners.

## Release policy

Do not merge without passing relevant CI jobs. Publish a fresh set of Portable EXE / Setup EXE / MSI assets with checksums after a validated release revision. Confirm Windows behavior on physical hardware before marking the build production stable.

The live public site currently presents a separate release technology and version. Reconcile public-product metadata, licensing and technical capability before replacing the live site with repository HTML.
