# SNAPVERE 0.1.1 Product Status

Active maintained product surfaces are **Windows** and **browser extensions**.

## Windows

Production implementation includes tray-first startup, Region/Window/Screen capture, frozen-frame selection, local annotation, clipboard and PNG save workflows, local settings, recent captures, diagnostics and x86/x64/ARM64 application payloads inside universal Setup and Portable packages.

Recent capture-path hardening removes redundant full-frame staging allocations from Region and Window overlay rendering and releases raw frozen monitor buffers after the corresponding UI bitmap is ready. These changes target peak memory without changing capture semantics.

## Browsers

Production source is maintained for Chrome, Edge, Opera and Firefox. Implemented capture modes are visible area, selected region and bounded full page. Brand identity is locked to SNAPVERE. Permissions are constrained to the validated extension contract and broad host access is not part of the maintained design.

GitHub release ZIPs are not represented as externally approved store listings unless that publication has actually happened.

## Packaging

The active 0.1.1 package contract contains:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`
- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

## Quality posture

CI covers Windows builds/tests, rendered WinUI visual QA, package construction, package-size budgets and x64/x86 lifecycle completion. Browser CI validates source/runtime behavior, permissions, locales, brand lock, cross-browser parity and deterministic packaging. Product Contract CI and CodeQL run independently.

These gates provide strong regression evidence; they are not a guarantee that every operating-system, driver or browser environment can never produce a platform-specific defect.

See [Performance & Stability](PERFORMANCE.md), [QA Matrix](QA-MATRIX.md) and the [current release](https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1).
