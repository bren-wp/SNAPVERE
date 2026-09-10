# Settings / Options

## Status

SNAPVERE 0.0.6 provides a real secondary **Options / Preferences** surface. It is opened explicitly from the tray right-click menu and does not appear during normal startup.

Only implemented settings are shown. Unimplemented toggles are intentionally absent.

## Current preferences

### Start SNAPVERE with Windows

When enabled, SNAPVERE registers a per-user startup command under:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
Value: SNAPVERE
```

Installed build behavior:

```text
"%LOCALAPPDATA%\Programs\SNAPVERE\Snapvere.exe"
```

The exact install path can vary if the user chose another per-user installation directory.

Portable behavior:

The extracted child does not register its temporary-cache `Snapvere.exe`. `Snapvere.Portable` passes its stable original launcher path through the process environment, and `StartupRegistrationService` registers that original Portable EXE instead.

On Windows sign-in, startup follows the same tray-first contract as a manual launch: hotkeys/tray initialize and no Capture Center is shown. No separate `--background` launch command is required.

Setup uninstall removes the SNAPVERE `Run` value only when it exactly matches the validated installed `Snapvere.exe`. A different Portable or unrelated command is preserved.

### Include cursor on capture

When enabled, `CapturePreferencesService` records:

```json
{
  "IncludeCursorOnCapture": true
}
```

The hidden runtime/capture coordinator resolves the current preference when each capture begins, and the preference is applied to the final Region, Window and Screen capture workflows.

- WGC applies cursor state through the capture session where supported.
- GDI monitor fallback draws the Windows cursor only when visible and inside the captured display.
- Window picker background frames remain cursor-free because they are targeting surfaces, not final output.

## Local preference storage

Capture preferences are stored at:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Properties:

- current-user only;
- no cloud synchronization;
- no telemetry dependency;
- human-readable JSON;
- atomic temp-file + replacement/move write;
- malformed/unreadable file falls back to safe defaults.

The Windows startup registration is separate from `settings.json` because Windows itself owns sign-in launch behavior.

## Recent Captures section

The same secondary window can display **Recent captures**. This is not a fake history database; it is backed by the local capture directory and a bounded recent enumeration.

Current actions:

- view the recent list;
- refresh;
- open the capture folder;
- open an individual capture with Windows shell association.

The feature does not upload, index remotely or persist a cloud history.

## UX rules

- Options never opens automatically on startup.
- Capture buttons do not turn the Options window into a second Capture Center.
- Only real, functioning toggles are shown.
- Failed registry/file writes are surfaced as status feedback instead of pretending success.
- Programmatic WinUI is used to stay consistent with the stable tray/overlay window strategy.

## Settings intentionally not exposed yet

The following ideas remain deferred until fully implemented and tested:

- custom default save folder;
- capture notification toggle;
- configurable Region shortcut;
- open capture after save;
- copy after save;
- default annotation color;
- remembered annotation thickness;
- all-monitors/monitor-under-cursor capture defaults;
- update or licensing controls.

No placeholder toggle should be added for these features.

## QA

Unit tests cover `CapturePreferencesService` persistence, atomic temporary-file cleanup and corrupt-JSON fallback.

The strict package lifecycle validates Setup/Portable startup and uninstall behavior. Installer QA additionally establishes an installed `Run\SNAPVERE` registration before uninstall and requires the same Setup executable to remove that installed startup entry.

For both installed and Portable x64/x86 packages, the `SECONDARY_UI_READY` runtime probe now constructs and loads the tray flyout, Options and About surfaces in sequence. A compile-successful but non-renderable Options surface therefore blocks CI and release publication.
