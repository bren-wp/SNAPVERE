# SNAPVERE 0.0.8

SNAPVERE 0.0.8 is a capture-surface localization and release-maintenance release built on the tray-first universal packaging introduced in 0.0.7.

## Capture UI localization

- Region Capture now uses the shared process-local language state for its window title, tool names, tooltips, resize accessibility labels, color labels, capture hint, Copy / Save / Close actions and working-state text.
- Window Capture now uses the same language state for its title, guidance and keyboard accessibility label.
- Croatian includes complete strings for these newly localized capture surfaces.
- Generic Region Capture failures for access denial, I/O or clipboard failure, invalid selection and an unexpected capture failure are localized in Croatian.
- English remains the canonical fallback when a selected language does not yet define a particular capture-surface string.
- Localization remains local and event-driven: no network translation service, polling worker, file watcher or telemetry is introduced.

## Release automation maintenance

- Historical v0.0.4 through v0.0.7 release workflow definitions are retained under `.github/release-archive/` instead of the active `.github/workflows/` directory.
- This keeps their source and Git history available while preventing obsolete version-specific release workflows from registering on ordinary pushes.
- The v0.0.8 release workflow is scoped to the dedicated `.github/release-triggers/v0.0.8` path on `main`; unrelated future `main` pushes do not activate it.
- Previously published tags, releases and assets are not rewritten by this maintenance.

## Two universal downloads

The public v0.0.8 GitHub Release contains exactly:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`

Each executable embeds x86, x64 and ARM64 application payloads and selects a compatible payload for the current Windows architecture.

The application target remains Windows 10 version 1809 / build 17763 or later. WGC-dependent capture paths require Windows 10 version 2004 / build 19041 or later.

## Functional scope

This release does not change Region Capture pixel geometry, annotation rendering, the WGC/GDI capture engines or the tray-first startup contract. Normal startup remains tray-only rather than opening a large launcher window.

## Validation gate

Publication is blocked unless:

- x64 build and unit tests pass;
- x86 build passes;
- ARM64 cross-build passes;
- all three native payload archives contain `Snapvere.exe`;
- exactly two public EXE files are produced;
- x64 and x86 universal Setup/Portable lifecycle probes pass;
- tray-first startup and Region, Window, Options and About runtime probes materialize;
- the v0.0.8 tag is absent or already points to the exact validated release commit;
- the published GitHub Release contains exactly the two expected universal executables.

ARM64 build/package validation on the hosted x64 runner is not represented as a real ARM64 hardware runtime test. The binaries are not represented as Authenticode-signed software; SHA-256 digests are integrity metadata only.
