# SNAPVERE v0.0.3

SNAPVERE v0.0.3 adds production Window Capture and further hardens the Windows Setup/Portable release lifecycle.

## Window Capture

Window Capture is now an implemented user-facing workflow instead of a deferred placeholder.

- Start it from the Capture Center, the tray menu, or `Ctrl+Shift+2`.
- SNAPVERE snapshots visible top-level windows in native Z-order before its picker appears.
- A frozen, borderless picker surface is created per monitor so mixed-DPI and negative-coordinate desktop layouts remain in physical-pixel space.
- Hovering highlights the real DWM extended-frame bounds of the target window.
- SNAPVERE's own picker windows are excluded from targeting.
- Left click confirms the target; Esc cancels without creating a file.
- The selected HWND is captured with Windows.Graphics.Capture `CreateForWindow` and the shared Direct3D 11 readback path.
- The PNG is persisted through the same temporary-file + atomic-move writer used by other capture workflows and appears in Recent Captures.

Window Capture requires Windows 10 version 2004 / build 19041 or later. SNAPVERE does not fake Window Capture with a desktop crop when WGC is unavailable because that would not reliably capture occluded window content.

## Region and Screen Capture

The existing capture workflows remain available:

- `Print Screen` or `Ctrl+Shift+1` — Region Capture;
- Region annotation tools: Pen, Line, Arrow, Box and Highlight;
- Region Copy to the Windows clipboard or Save to local PNG;
- `Ctrl+Shift+4` — primary Screen Capture;
- WGC/D3D11 preferred monitor acquisition with GDI compatibility fallback for expected monitor-capture failures.

## Setup and uninstall

v0.0.3 keeps SNAPVERE's single-maintenance-executable model.

Windows Installed apps registers the installed `SNAPVERE-Setup.exe --uninstall`. There is intentionally no separate `uninstall.exe`, `unins000.exe`, `uninstall*.exe` or Inno-style `unins*.exe` payload.

The same Setup executable handles installation and maintenance/uninstall. Uninstall removes application files, shortcuts and Windows uninstall registration while preserving screenshots under `Pictures\SNAPVERE`.

## Release validation

Both x64 and x86 packages must pass the release gate before publication:

1. Release build and unit tests;
2. self-contained application publish;
3. Setup and Portable executable generation;
4. rejection of silent Setup without explicit MPL 2.0 acceptance;
5. silent installation with `--accept-license`;
6. Windows Installed apps contract validation, including the same-Setup uninstall handler and absence of standalone uninstaller executables;
7. activated WinUI main-window READY probe;
8. Region editor runtime-materialization probe;
9. Window Capture picker runtime-materialization probe;
10. normal installed launch survival check;
11. Setup-based silent uninstall and removal of the Installed apps registration;
12. Portable main-window, Region-editor and Window-picker probes;
13. normal Portable launch survival check.

The hosted CI probes validate that the production WinUI surfaces materialize in installed and Portable builds. They are not treated as proof of a real interactive end-user desktop WGC screenshot, which remains subject to the Windows desktop/GPU session and protected-content policy.

## Downloads

The release contains:

- `SNAPVERE-0.0.3-Setup-x64.exe`
- `SNAPVERE-0.0.3-Portable-x64.exe`
- `SNAPVERE-0.0.3-Setup-x86.exe`
- `SNAPVERE-0.0.3-Portable-x86.exe`
- `SNAPVERE-0.0.3-x64.zip`
- `SNAPVERE-0.0.3-x86.zip`
- `SHA256SUMS.txt`

The app payloads are self-contained; a separate .NET runtime or Windows App SDK runtime installation is not required for these published packages.

## Integrity and signing

v0.0.3 executables are intentionally not Authenticode-signed. Verify downloaded artifacts with the included `SHA256SUMS.txt` when integrity checking is required.

## Still intentionally deferred

The following are not represented as finished in v0.0.3:

- coordinated cross-monitor Region Capture;
- Scrolling Capture;
- richer editor tools such as text, blur/pixelate and numbered steps;
- full History management, favorites and Pin to Screen;
- OCR;
- automatic updates;
- Authenticode signing.

**SNAPVERE — Capture anything.**  
Developed and published by **Brendigo**.
