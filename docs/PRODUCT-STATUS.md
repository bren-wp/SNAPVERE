# SNAPVERE 0.1.30 Product Status

Active maintained product surfaces are **Windows** and **browser extensions**.

## Windows

Production implementation includes tray-first startup, Region/Window/Screen capture, local primary-display screen recording, frozen-frame selection, local annotation, clipboard, PNG and MP4 persistence workflows, local settings, recent captures, diagnostics and x86/x64/ARM64 application payloads inside universal Setup and Portable packages, plus a standard x64 MSI distribution.

SNAPVERE 0.1.30 focuses on privacy/security hardening. Windows and Portable startup diagnostics now pass persisted exception text through a bounded redactor for local/UNC paths, URLs/file URIs, e-mail addresses and common credential/token patterns while preserving exception type/HRESULT evidence. Existing capture, recording, responsive overlay and Setup lifecycle behavior remains unchanged. The initial recording mode is video-only; system audio and microphone capture are not claimed as supported.

## Browsers

Production source is maintained for Chrome, Edge, Opera and Firefox. Implemented capture modes are visible area, selected region and bounded full page. Brand identity is locked to SNAPVERE. The validated permission contract is `activeTab`, `scripting`, `downloads`, `downloads.open` and `storage`; `downloads.open` is used only by the explicit Recent > Open action, after click-time revalidation confirms that the selected download still exists, is complete and still matches the SNAPVERE capture contract. Broad host access is not part of the maintained design. The maintained browser runtime also retains capture-lock ownership hardening, Recent async ordering, duplicate-action containment, browser API compatibility and reduced-motion/responsive UI behavior, with the macOS Full Page default kept away from the system-reserved Command+Shift+3 shortcut.

Region capture now carries the selection-time viewport dimensions through the background-to-crop pipeline and fails closed if the browser viewport changes before local PNG cropping, preventing a valid selection from being mapped against different geometry. Settings also locks Save As together with the Save action while browser-local persistence is pending, preventing displayed state from racing the persisted value.

GitHub release ZIPs are not represented as externally approved store listings unless that publication has actually happened. Browser message sender and active-tab ownership checks remain enforced before privileged capture/download work.

## Packaging

The active 0.1.30 package contract contains:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Setup.msi`
- `SNAPVERE-Portable.exe`
- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

## Quality posture

CI covers Windows builds/tests, rendered WinUI visual QA, package construction, package-size budgets, x64/x86 Setup/Portable lifecycle completion and MSI database/install/repair/upgrade/uninstall lifecycle QA. Browser CI validates source/runtime behavior, permissions, locales, brand lock, cross-browser parity and deterministic packaging. Product Contract CI and CodeQL run independently.

These gates provide strong regression evidence; they are not a guarantee that every operating-system, driver or browser environment can never produce a platform-specific defect.

The public release is **v0.1.30**. Later `main` hardening remains source state only until a future version is explicitly packaged and published.

See [Performance & Stability](PERFORMANCE.md), [QA Matrix](QA-MATRIX.md) and the [current release](https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.30).
