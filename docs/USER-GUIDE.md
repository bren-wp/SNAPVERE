# SNAPVERE User Guide

This guide covers the user-facing workflows in **SNAPVERE 0.1.1** across Windows, Android and the four browser-extension packages.

## Choose the right SNAPVERE experience

| Platform | Best for | Public package |
| --- | --- | --- |
| Windows | Fast tray-first desktop capture, region editing, window/screen capture | `SNAPVERE-Setup.exe` or `SNAPVERE-Portable.exe` |
| Android 10+ | One-shot full-screen captures saved to the system Pictures collection | `SNAPVERE.apk` |
| Chrome | Visible area, full page and selected web-page region | `SNAPVERE-Chrome.zip` |
| Edge | Visible area, full page and selected web-page region | `SNAPVERE-Edge.zip` |
| Opera | Visible area, full page and selected web-page region | `SNAPVERE-Opera.zip` |
| Firefox | Visible area, full page and selected web-page region | `SNAPVERE-Firefox.zip` |

Current release: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

## Windows

### First launch

Install `SNAPVERE-Setup.exe` or run `SNAPVERE-Portable.exe`. SNAPVERE is tray-first: normal startup keeps the application in the Windows notification area rather than opening a permanent dashboard.

If the tray icon is hidden by Windows, open the notification-area overflow and pin SNAPVERE if desired.

### Region capture

Use **Print Screen**, `Ctrl+Shift+1`, or left-click the tray icon. SNAPVERE freezes the captured desktop frame and opens the region editor.

1. Drag to define the desired region.
2. Move or resize the selection if needed.
3. Use Pen, Line, Arrow, Box or Highlight for local annotations.
4. Use Undo if necessary.
5. Choose Copy or Save.

The completed PNG is created locally. The default capture directory is `Pictures\SNAPVERE`. When Save As is used, cancelling the picker does not destroy the already completed local capture.

### Window capture

Use `Ctrl+Shift+2` or the tray menu. SNAPVERE discovers eligible native top-level windows and uses the dedicated window-capture workflow rather than silently replacing it with a desktop crop.

Protected, minimized, hardware-overlay or otherwise non-capturable windows can still be restricted by Windows or the application being captured.

### Screen capture

Use `Ctrl+Shift+3` or the tray menu. The preferred path uses Windows.Graphics.Capture where available, with a resilient monitor fallback for expected acquisition failures.

### Settings and history

Open the tray menu for settings, recent captures, language selection, About and Exit. Windows includes 28 built-in language choices; English is the canonical fallback.

### Setup vs Portable

- **Setup** installs SNAPVERE for normal desktop use and supports the validated uninstall lifecycle.
- **Portable** is a single public executable that carries validated x86, x64 and ARM64 application payloads and selects the appropriate architecture at runtime.

## Android 10+

### Capture a screen

1. Open SNAPVERE.
2. Tap **Capture screen**.
3. Approve the Android system MediaProjection prompt.
4. SNAPVERE moves its task behind the previously visible content.
5. A one-shot capture is saved to `Pictures/SNAPVERE` through MediaStore.

A fresh system-consent flow is required for every capture. SNAPVERE does not cache or silently reuse a MediaProjection consent token.

### Latest capture actions

The app remembers the latest readable capture URI and exposes:

- **Open** — open the PNG with an installed image viewer;
- **Share** — send the PNG through Android's chooser;
- **Delete** — remove the latest capture after confirmation.

If a stored MediaStore URI becomes stale or unreadable, SNAPVERE clears the stale reference instead of treating it as a valid latest capture.

### Privacy behavior

The Android app has no `INTERNET` permission. It does not include a first-party analytics, telemetry, advertising or cloud-upload worker. System share/open actions are explicit user actions and can hand data to the app selected by the user.

The public v0.1.1 APK is CI/debug-signed. It is installable but is not represented as Google Play production-signed.

## Browser extensions

### Install from the GitHub package

Until external browser stores are actually published and approved, the public ZIP files are source/distribution packages for manual developer-mode installation. See [Installation](INSTALLATION.md) for browser-specific steps.

### Capture visible area

Open the SNAPVERE extension popup and choose **Capture visible area**. The current viewport is captured as a PNG and handed to the browser download flow.

### Capture a selected region

Choose **Select region**, drag across the desired web-page area and release. Press **Esc** to cancel. The crop is processed locally before download.

### Capture a full page

Choose **Capture full page**. SNAPVERE measures the document, scrolls through bounded positions, captures tiles, hides a bounded set of fixed/sticky elements after the first tile, stitches the image locally and restores page state.

For safety, the implementation rejects pages that exceed its tile/canvas/pixel limits. Highly dynamic pages, video, canvas content and some cross-origin frames can produce results that differ from a static document.

### Browser settings

The options page allows a local filename prefix and an “ask where to save” setting for visible and region captures. Full-page assembly uses a local page-side download because the final stitched image is constructed inside the content-script context.

## File naming

Windows captures use `SNAPVERE_yyyy-MM-dd_HHmmss.png` with a numeric suffix when required. Android captures add millisecond precision. Browser filenames use the configured prefix plus capture type and timestamp.

## If something does not work

Start with [Troubleshooting](TROUBLESHOOTING.md). For security/privacy behavior see [Privacy](PRIVACY.md). For the exact scope of automated validation see [QA Matrix](QA-MATRIX.md).

Support: **info@snapvere.com**
