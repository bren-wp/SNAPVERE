# SNAPVERE 0.1.21 Product Status

Active maintained product surfaces are **Windows** and **browser extensions**.

## Windows

Production implementation includes tray-first startup, Region/Window/Screen capture, local primary-display screen recording, frozen-frame selection, local annotation, clipboard, PNG and MP4 persistence workflows, local settings, recent captures, diagnostics and x86/x64/ARM64 application payloads inside universal Setup and Portable packages.

SNAPVERE 0.1.21 retains local-first primary-display screen recording through Windows.Graphics.Capture with H.264 MP4 encoding and makes recording state explicit: Settings exposes separate compact Start and Stop controls, while active recording uses a compact borderless elapsed-time controller with one dedicated Stop action. Secondary Settings, recording-controller, Tray, About and Language windows now roll back stale ownership when WinUI activation fails so later user actions can create a clean surface instead of reusing an invalid window. Setup separates license acceptance from installation options and clears its busy lifecycle before Finish after a successful install or uninstall. Responsive high-DPI behavior remains across Tray, Settings, Language, About, capture feedback and Setup. The initial recording mode is video-only; system audio and microphone capture are not claimed as supported.

## Browsers

Production source is maintained for Chrome, Edge, Opera and Firefox. Implemented capture modes are visible area, selected region and bounded full page. Brand identity is locked to SNAPVERE. The validated permission contract is `activeTab`, `scripting`, `downloads`, `downloads.open` and `storage`; `downloads.open` is used only by the explicit Recent > Open action, after click-time revalidation confirms that the selected download still exists, is complete and still matches the SNAPVERE capture contract. Broad host access is not part of the maintained design. SNAPVERE 0.1.21 also hardens capture-lock ownership, Recent async ordering, duplicate action handling, browser API compatibility and reduced-motion/responsive UI behavior, and moves the macOS Full Page default away from the system-reserved Command+Shift+3 shortcut.

Region capture now carries the selection-time viewport dimensions through the background-to-crop pipeline and fails closed if the browser viewport changes before local PNG cropping, preventing a valid selection from being mapped against different geometry. Settings also locks Save As together with the Save action while browser-local persistence is pending, preventing displayed state from racing the persisted value.

GitHub release ZIPs are not represented as externally approved store listings unless that publication has actually happened. SNAPVERE 0.1.21 also tightens browser message sender and active-tab ownership checks before privileged capture/download work.

## Packaging

The active 0.1.21 package contract contains:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`
- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

## Quality posture

CI covers Windows builds/tests, rendered WinUI visual QA, package construction, package-size budgets and x64/x86 lifecycle completion. Browser CI validates source/runtime behavior, permissions, locales, brand lock, cross-browser parity and deterministic packaging. Product Contract CI and CodeQL run independently.

These gates provide strong regression evidence; they are not a guarantee that every operating-system, driver or browser environment can never produce a platform-specific defect.

The public release is **v0.1.21**. Later `main` hardening remains source state only until a future version is explicitly packaged and published.

See [Performance & Stability](PERFORMANCE.md), [QA Matrix](QA-MATRIX.md) and the [current release](https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.21).
