# SNAPVERE v0.0.4

SNAPVERE 0.0.4 changes the desktop experience to a true **tray-first capture workflow**.

## Highlights

- Normal application startup no longer opens the Capture Center.
- Left-click the SNAPVERE notification-area icon to start **Region Capture immediately**.
- Right-click the tray icon to open the new branded SNAPVERE WinUI command flyout.
- The tray flyout exposes Region, Window and Screen Capture plus the local capture folder, Options/Recent captures, About and Exit.
- New violet shard/feather SNAPVERE identity across tray and repository branding.
- README now documents the actual tray-first interaction using repository-maintained SVG UI illustrations.
- Installed and Portable package QA now includes a dedicated **tray-only startup probe** that verifies the hotkey/tray hosts initialize without activating the Capture Center.

## Capture

- Print Screen or tray left-click: Region Capture.
- Ctrl+Shift+1: independent Region fallback.
- Ctrl+Shift+2: Window Capture with the multi-monitor frozen picker and Windows.Graphics.Capture `CreateForWindow`.
- Ctrl+Shift+4: primary Screen Capture.
- Region annotations: Pen, Line, Arrow, Box, Highlight, colors and Undo.
- Copy or Save directly from the Region overlay.

## Packaging and uninstall

Public assets are self-contained x64 and x86 builds.

The uninstall contract is unchanged and intentionally strict: **there is no separate uninstall executable**. Windows Installed apps invokes the installed `SNAPVERE-Setup.exe --uninstall`. CI rejects `uninstall*.exe` and Inno-style `unins*.exe` payloads.

Setup and Portable release binaries are unsigned by design for this release. `SHA256SUMS.txt` is included for integrity verification.

## Release QA

Publication is blocked unless x64 and x86 pass build/test and the full package lifecycle: license-gated Setup, install contract, activated-window probe, tray-only startup probe, Region-editor probe, Window-picker probe, normal installed startup, Setup-based uninstall and Portable equivalents.

See the README and `docs/INSTALLATION.md` for details.
