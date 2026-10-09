# SNAPVERE 0.1.32 Product Status

Active maintained product surfaces are **Windows** and **browser extensions**.

## Windows

Production implementation includes tray-first startup, Region/Window/Screen capture, local primary-display screen recording, frozen-frame selection, local annotation, clipboard, PNG and MP4 persistence workflows, local settings, recent captures, diagnostics and x86/x64/ARM64 application payloads inside universal Setup and Portable packages, plus a standard x64 MSI distribution.

SNAPVERE 0.1.32 is a production branding and usability release. A shared `SnapvereBrand` layer locks the supplied premium identity across the visible Windows product: Obsidian `#070912`, Surface `#111526`, Slate `#161B2E`, Violet `#7655F6`, Lavender `#A48BFF`, Ice `#80E1E5`, the viewfinder-plus-lightning mark, compact spacing and consistent action hierarchy. Tray keeps the four primary capture/recording actions prominent while secondary utilities move to compact footer actions. Settings uses a branded navigation rail, Region/Window overlays share the same chrome, and recording/secondary windows and Setup use the same visual system.

Normal startup remains tray-first and hidden; v0.1.32 does not add a blocking splash screen or a permanent dashboard. Screen recording remains primary-display video only, so microphone/system-audio controls are not presented as implemented features. The existing diagnostic redaction and local-first privacy boundaries remain in force.

## Browsers

Production source is maintained for Chrome, Edge, Opera and Firefox. Implemented capture modes are visible area, selected region and bounded full page. All four variants now use the same supplied 16/32/48/128 PNG icon set, premium popup hierarchy and premium Options layout. The popup keeps only the three real capture modes plus direct access to Recent captures and Settings. Browser source parity remains enforced.

The validated permission contract remains `activeTab`, `scripting`, `downloads`, `downloads.open` and `storage`; broad host access is not part of the maintained design. Existing privacy boundaries remain active: session-scoped capture ownership metadata is preferred where supported, persistent local storage is reserved for settings when possible, Recent discovery is bounded to SNAPVERE candidates, and click-time download revalidation plus sender/tab ownership checks remain enforced.

GitHub release ZIPs are not represented as externally approved store listings unless that publication has actually happened.

## Packaging

The active 0.1.32 package contract contains:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Setup.msi`
- `SNAPVERE-Portable.exe`
- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

## Quality posture

CI covers Windows builds/tests, rendered WinUI visual QA, package construction, package-size budgets, x64/x86 Setup/Portable lifecycle completion and MSI database/install/repair/upgrade/uninstall lifecycle QA. Browser CI validates source/runtime behavior, permissions, locales, brand lock, cross-browser parity and deterministic packaging. The Windows UI contract now locks the premium palette/layout fragments, while browser validation locks the shared premium CSS tokens and popup hierarchy. Product Contract CI and CodeQL run independently.

These gates provide regression evidence; they are not a guarantee that every operating-system, driver or browser environment can never produce a platform-specific defect.

The release-preparation target is **v0.1.32**. It must not be treated as published until the GitHub release workflow completes after a green merge.

See [Performance & Stability](PERFORMANCE.md), [QA Matrix](QA-MATRIX.md) and [Release history](../RELEASES.md).
