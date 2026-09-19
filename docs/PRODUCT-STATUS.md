# SNAPVERE 0.1.10 Product Status

Active maintained product surfaces are **Windows** and **browser extensions**.

## Windows

Production implementation includes tray-first startup, Region/Window/Screen capture, local primary-display screen recording, frozen-frame selection, local annotation, clipboard, PNG and MP4 persistence workflows, local settings, recent captures, diagnostics and x86/x64/ARM64 application payloads inside universal Setup and Portable packages.

SNAPVERE 0.1.10 adds local-first primary-display screen recording through Windows.Graphics.Capture with H.264 MP4 encoding, tray Start/Stop controls, cursor-preference integration, bounded pending/in-flight frame ownership, 30 FPS and 4–32 Mbps policy bounds, an 8K source envelope, zero-frame/zero-byte rejection and atomic publication of completed MP4 files. The initial mode is video-only; system audio and microphone capture are not claimed as supported. Existing capture-safe shutdown, collision-safe PNG publication, shell-error containment, language persistence, tray recovery and Setup hardening remain in place.

## Browsers

Production source is maintained for Chrome, Edge, Opera and Firefox. Implemented capture modes are visible area, selected region and bounded full page. Brand identity is locked to SNAPVERE. The validated permission contract is `activeTab`, `scripting`, `downloads`, `downloads.open` and `storage`; `downloads.open` is used only by the explicit Recent > Open action. Broad host access is not part of the maintained design. SNAPVERE 0.1.10 also hardens capture-lock ownership, Recent async ordering, duplicate action handling, browser API compatibility and reduced-motion/responsive UI behavior, and moves the macOS Full Page default away from the system-reserved Command+Shift+3 shortcut.

GitHub release ZIPs are not represented as externally approved store listings unless that publication has actually happened. SNAPVERE 0.1.10 also tightens browser message sender and active-tab ownership checks before privileged capture/download work.

## Packaging

The active 0.1.10 package contract contains:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`
- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

## Quality posture

CI covers Windows builds/tests, rendered WinUI visual QA, package construction, package-size budgets and x64/x86 lifecycle completion. Browser CI validates source/runtime behavior, permissions, locales, brand lock, cross-browser parity and deterministic packaging. Product Contract CI and CodeQL run independently.

These gates provide strong regression evidence; they are not a guarantee that every operating-system, driver or browser environment can never produce a platform-specific defect.

The public release is **v0.1.10**. Later `main` hardening remains source state only until a future version is explicitly packaged and published.

See [Performance & Stability](PERFORMANCE.md), [QA Matrix](QA-MATRIX.md) and the [current release](https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.10).
