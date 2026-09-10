# SNAPVERE v0.0.4

SNAPVERE 0.0.4 changes the desktop experience to a true **tray-first capture workflow** and replaces the old visible capture-launcher path with compact quick actions plus real secondary preferences/history surfaces.

## Highlights

- Normal application startup no longer opens the Capture Center.
- Left-click the SNAPVERE notification-area icon to start **Region Capture immediately**.
- Print Screen starts Region Capture when the key is available; `Ctrl+Shift+1` remains the independent fallback.
- Right-click the tray icon to open the branded SNAPVERE WinUI quick-actions flyout.
- The flyout exposes Region, Window and Screen Capture, Open Capture Folder, Recent captures, Options / Preferences, About and Exit.
- Added a real Options / Preferences window instead of routing the tray back to a capture dashboard.
- Added real **Start SNAPVERE with Windows** and **Include cursor on capture** preferences.
- Recent Captures is available as a real local list with refresh/open/folder actions.
- Installed and Portable startup QA includes a dedicated **tray-only startup probe** proving hotkey/tray hosts initialize with the Capture Center hidden.
- Refreshed violet SNAPVERE shard/feather identity remains consistent across tray and repository product surfaces.

## Capture

### Region

- tray left-click / Print Screen / `Ctrl+Shift+1` entry;
- frozen primary-display frame;
- physical-pixel drag, move and eight-handle resize;
- dimension badge;
- Move, Pen, Line, Arrow, Box and Highlight tools;
- annotation colors and Undo;
- `Ctrl+Z`, `Ctrl+C`, Enter and Esc editor behavior;
- annotations rendered into the resulting clipboard image/PNG;
- Copy or Save directly from the frozen Region overlay.

### Window

- `Ctrl+Shift+2` / tray command;
- native top-level window discovery and DWM bounds;
- candidate Z-order frozen before overlays appear;
- self/invisible/cloaked/tool target filtering;
- one frozen DPI-aware picker overlay per monitor;
- negative-coordinate and spanning-window highlight handling;
- left-click capture / Esc cancel;
- WGC `CreateForWindow` final acquisition;
- atomic local PNG persistence and Recent Captures integration.

### Screen

- `Ctrl+Shift+4` / tray command;
- primary-display capture;
- WGC/D3D11 preferred backend on supported systems;
- expected compatibility/native failures can use GDI fallback;
- caller cancellation does not become fallback work.

## Preferences

### Start SNAPVERE with Windows

Uses the current user's Windows `Run` registration. Installed builds point to the installed `Snapvere.exe`.

Portable builds register the original Portable launcher rather than a temporary extracted child path.

Setup uninstall removes the startup value only when it exactly matches the validated installed `Snapvere.exe`, preventing removal of a different Portable registration.

### Include cursor on capture

Stored locally in `%LOCALAPPDATA%\SNAPVERE\settings.json` and applied by Region, Window and Screen workflows where supported by the active backend.

Settings writes use an atomic temporary-file replacement strategy and corrupt settings fall back to safe defaults.

## Packaging and uninstall

Public release assets are self-contained x64 and x86 builds.

The uninstall contract remains intentionally strict: **there is no separate uninstall executable**. Windows Installed apps invokes the installed:

```text
SNAPVERE-Setup.exe --uninstall
```

CI rejects `uninstall*.exe` and Inno-style `unins*.exe` payloads and now also verifies installed Windows startup registration cleanup.

User captures under `Pictures\SNAPVERE` are preserved.

Setup and Portable binaries are unsigned for this release; `SHA256SUMS.txt` is published for integrity verification.

## Release QA

Publication is blocked unless x64 and x86 pass:

- restore/build + unit tests;
- self-contained application publish;
- Setup and Portable creation;
- explicit silent-license gate;
- Installed apps/uninstall contract;
- activated-window construction probe;
- tray-only `TRAY_READY` probe;
- Region `REGION_OVERLAY_READY` probe;
- Window `WINDOW_OVERLAY_READY` probe;
- normal installed tray-first process survival;
- installed startup Run-value cleanup during same-Setup uninstall;
- Portable equivalents and normal child-process survival.

The release workflow must publish exactly the expected x64/x86 Setup, Portable and ZIP files plus `SHA256SUMS.txt` before SNAPVERE v0.0.4 is considered complete.

See the README and `docs/` for implementation-specific details.
