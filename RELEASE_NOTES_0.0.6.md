# SNAPVERE v0.0.6

SNAPVERE 0.0.6 restores the intended tray-first product contract and completes the visual/runtime cleanup started in the 0.0.4 line. It supersedes v0.0.5 for normal downloads.

## Highlights

- Normal installed and Portable launches stay in the Windows notification area instead of opening the legacy Capture Center.
- Left-click tray remains the direct Region Capture action; right-click opens the redesigned compact quick-action flyout.
- Region Capture now uses the redesigned floating annotation/action toolbar and refreshed selection chrome.
- Window Capture picker, Options / Recent Captures, About and Setup/Uninstall surfaces use the same graphite + violet/indigo/cyan SNAPVERE visual system.
- The former visible Capture Center is reduced to a hidden runtime/capture coordinator and is no longer a product surface.

## Fixed

- `Include cursor on capture` is now consumed by the real Region, Window and Screen workflows instead of only being persisted by the settings UI.
- Windows startup registration uses the same tray-first executable command as a normal launch, keeping uninstall matching deterministic.
- Setup custom controls were corrected for strict WinForms analyzers and runtime-safe rendering.
- The release pipeline no longer relies on the removed interactive-launch QA script.

## Runtime and package validation

Both x64 and x86 release packages must pass:

- restore, build and unit tests;
- self-contained app, Setup and Portable publication;
- silent-license enforcement;
- Setup UI materialization;
- installed-app/uninstall contract validation;
- tray-only `TRAY_READY` startup validation;
- Region editor `REGION_OVERLAY_READY` validation;
- Window picker `WINDOW_OVERLAY_READY` validation;
- tray flyout + Options + About `SECONDARY_UI_READY` validation;
- normal installed and Portable tray-first process-survival checks;
- installed startup-registration cleanup during same-Setup uninstall;
- SHA-256 release manifest integrity.

## Packaging

The public release contains Setup, Portable and self-contained ZIP payloads for x64 and x86, plus `SHA256SUMS.txt`. No separate standalone uninstaller executable is installed; the installed `SNAPVERE-Setup.exe --uninstall` remains the maintenance/removal entry point.

The binaries remain unsigned in this release. Verify `SHA256SUMS.txt` when package integrity assurance is required.
