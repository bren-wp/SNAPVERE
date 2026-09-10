# Settings / Options

## Status

SNAPVERE 0.0.7 provides a real secondary **Options / Preferences** surface opened explicitly from the tray. It never reintroduces the former Capture Center as a normal startup window.

Only implemented settings are shown. Placeholder toggles are intentionally absent.

## Current preferences

### Start SNAPVERE with Windows

The setting controls the current-user startup value:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
Value: SNAPVERE
```

Installed builds point to the stable installed `Snapvere.exe`. Portable builds register the stable original `SNAPVERE-Portable.exe` launcher rather than a versioned executable inside the extraction cache.

Normal SNAPVERE launch is tray-first, so Windows startup uses the same command contract. Disabling startup removes the value only when it matches the current SNAPVERE launch path.

Interactive Setup enables this option by default, but the user can clear the checkbox before installation.

### Include cursor on capture

`CapturePreferencesService` persists whether the final capture should include the cursor when the active backend supports cursor composition.

- WGC applies cursor state where supported;
- GDI monitor fallback draws the Windows cursor only when appropriate;
- picker/reference frames are not treated as final output.

### Language

English (`en`) is the canonical default and fallback. The in-app language picker currently exposes 28 built-in choices, including Croatian (`hr`) and more than 20 additional languages.

Language selection is local-only. The localization system uses static in-process resources and does not call a translation API, run a polling worker or require network access.

## Local preference storage

Preferences are stored in:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Current persisted fields include cursor preference and language code. Properties:

- current-user only;
- no cloud synchronization;
- no telemetry dependency;
- human-readable JSON;
- atomic temporary-file + move replacement;
- malformed/unreadable files fall back to safe defaults;
- unsupported language codes normalize to English.

Windows startup registration remains separate because Windows owns sign-in launch behavior.

## Recent Captures

The same secondary surface provides a bounded view of real files in the local capture directory.

Implemented actions:

- refresh recent captures;
- open the capture folder;
- open an individual capture through Windows shell association.

There is no fake history database or remote index.

## UX rules

- Options never opens automatically on startup.
- Language is available from both Tray and Options.
- Only functioning settings are exposed.
- Registry/file failures are surfaced as status feedback.
- Settings changes do not add resident timers or background polling.
- The graphite/violet/cyan visual system is shared with Tray, Region, About and Setup.

## Deferred settings

Not exposed until implemented and tested:

- custom default save folder;
- configurable Region shortcut;
- open-after-save/copy-after-save policy;
- remembered annotation defaults;
- monitor-under-cursor/all-monitors defaults;
- automatic update controls.

## QA

Unit tests cover settings persistence, atomic temp-file cleanup, corrupt JSON fallback, supported language normalization and unsupported language fallback. Runtime QA materializes Options through the `SECONDARY_UI_READY` probe for installed and Portable x64/x86 package paths.
