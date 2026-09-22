# SNAPVERE User Guide

SNAPVERE is a local-first capture tool for Windows and modern browsers. The current public release is v0.1.12.

## Windows

SNAPVERE starts quietly in the notification area. Use the tray icon or shortcuts:

| Action | Shortcut |
| --- | --- |
| Region capture | Print Screen or Ctrl+Shift+1 |
| Window capture | Ctrl+Shift+2 |
| Screen capture | Ctrl+Shift+3 |
| Screen recording | Tray menu Start/Stop action |

### Region capture

Region capture freezes the selected display before the editor appears, so the underlying desktop cannot move underneath the selection. Drag to create a region, then move or resize it inside the frozen frame. Pen, Line, Arrow, Box, Highlight, color choices and Undo are available directly in the editor.

**Copy** renders the selected region and sends it to the Windows clipboard. **Save** writes a PNG locally. `Enter` saves and `Esc` cancels. Selection geometry is clamped to the frozen frame, and empty or invalid selections are rejected before encoding.

### Screen recording

Screen recording records the primary Windows display to a local H.264 MP4 through Windows.Graphics.Capture. Recording starts and stops from the tray menu, follows the existing include-cursor preference, and shows active state in the tray surface. The initial mode is video-only: microphone and system audio are not claimed as supported.

Screenshots and completed recordings are stored by default in `Pictures\SNAPVERE`. A completed recording is published only after encoding finishes; temporary recording files are not intentionally exposed as completed captures.

### Windows settings

Settings expose only implemented local preferences: start SNAPVERE with Windows, include the cursor in supported capture modes and choose the UI language. Preferences are stored for the current Windows account under `%LOCALAPPDATA%\SNAPVERE`. Recent captures remain filesystem-backed rather than using a database or cloud history.

## Browser extensions

Chrome, Edge, Opera and Firefox packages offer Visible area, Select region and Full page capture. Saved files always use the **SNAPVERE** filename prefix. Settings can control whether the browser asks where to save supported visible/region captures; the product brand and filename prefix are fixed and are not user-customizable.

The browser region flow uses a per-capture token. After the popup closes, the page selection overlay owns completion of that request and surfaces a localized transient error when the background crop/download cannot finish. No setting enables telemetry, automatic cloud upload or remote runtime code.

Full-page capture is deliberately bounded to protect memory and stability. Very large or highly dynamic pages can be rejected instead of allowing unbounded allocation.

Screen recording is currently a Windows application feature under validation. Browser extensions do not claim video recording support.

## Privacy

Core capture and recording processing is local. SNAPVERE does not require an account, does not upload screenshots or recordings automatically and does not include first-party analytics or telemetry in the capture runtime.

See [Privacy](PRIVACY.md), [QA Matrix](QA-MATRIX.md) and [Troubleshooting](TROUBLESHOOTING.md).
