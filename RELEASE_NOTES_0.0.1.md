# SNAPVERE v0.0.1

First public SNAPVERE release for Windows.

## Downloads

Choose the architecture that matches Windows:

- `SNAPVERE-0.0.1-Setup-x64.exe` — recommended installer for 64-bit Windows
- `SNAPVERE-0.0.1-Portable-x64.exe` — single-file portable launcher for 64-bit Windows
- `SNAPVERE-0.0.1-Setup-x86.exe` — installer for 32-bit/x86 Windows
- `SNAPVERE-0.0.1-Portable-x86.exe` — single-file portable launcher for 32-bit/x86 Windows

`x32` and `x86` refer to the same 32-bit Windows architecture, so SNAPVERE publishes one x86 build rather than duplicating identical binaries.

Raw self-contained application ZIP packages are also included for advanced/manual deployment.

## Installer

The SNAPVERE Setup application is a self-contained per-user installer:

- no administrator elevation is required for the default install location;
- Mozilla Public License 2.0 terms are shown and must be accepted before interactive installation;
- Start menu shortcut is enabled by default;
- Desktop shortcut is optional;
- Windows Installed apps receives a normal uninstall registration;
- uninstall is handled through the installed SNAPVERE Setup executable with `--uninstall` — no separate `uninstall.exe` is installed;
- user screenshots under `Pictures\SNAPVERE` are preserved during uninstall.

Silent installation requires explicit license acceptance:

```text
SNAPVERE-0.0.1-Setup-x64.exe --silent --accept-license
```

Silent uninstall:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

## Implemented in 0.0.1

- primary-display Screen Capture;
- freeze-frame Region Capture;
- precise physical-pixel region selection;
- drag, move, eight resize handles and keyboard nudge;
- Enter/double-click save and Esc cancel;
- local PNG output to `Pictures\SNAPVERE`;
- collision-safe filenames and atomic file persistence;
- recent local capture list;
- global `Ctrl+Shift+1` Region hotkey;
- global `Ctrl+Shift+4` Screen hotkey;
- native Windows system tray actions for Show, Region, Screen and Exit;
- per-monitor-v2 DPI awareness;
- negative-coordinate and mixed-DPI geometry handling;
- x64 and x86/32-bit builds;
- self-contained Setup and Portable packaging;
- SHA-256 checksums for release assets.

## Privacy and security

SNAPVERE 0.0.1 is local-first. The capture workflow does not require an account, analytics service or telemetry connection. Packaging extraction rejects absolute paths and directory traversal and uses staging/temp files before replacing final files.

Verify downloaded release files against `SHA256SUMS.txt` when integrity matters.

## Known limitations

This is the first release and intentionally keeps unfinished functionality disabled:

- Region Capture currently operates on the primary display rather than one coordinated cross-monitor overlay;
- the modern Windows.Graphics.Capture/D3D primary backend is not yet enabled; 0.0.1 uses the existing compatibility monitor-capture backend;
- Window Capture is not yet enabled;
- Scrolling Capture is not yet enabled;
- full History, annotation Editor, OCR, Pin to Screen and updater are not yet enabled;
- release executables are not Authenticode-signed yet.

These limitations are shown as unavailable rather than exposing placeholder functionality.

---

**SNAPVERE — Capture anything.**  
Developed and published by **Brendigo**.
