# SNAPVERE User Guide

SNAPVERE is a local-first capture tool for Windows and modern browsers. The current public release is v0.1.10.

## Windows

SNAPVERE starts quietly in the notification area. Use the tray icon or shortcuts:

| Action | Shortcut |
| --- | --- |
| Region capture | Print Screen or Ctrl+Shift+1 |
| Window capture | Ctrl+Shift+2 |
| Screen capture | Ctrl+Shift+3 |
| Screen recording | Tray menu Start/Stop action |

Region capture freezes the current monitor image so you can select, resize and annotate without the underlying page moving. Available tools include Pen, Line, Arrow, Box, Highlight, colors and Undo. Copy sends the result to the Windows clipboard; Save writes a PNG locally.

Screen recording records the primary Windows display to a local H.264 MP4 through Windows.Graphics.Capture. Recording starts and stops from the tray menu, follows the existing include-cursor preference, and shows active state in the tray surface. The initial mode is video-only: microphone and system audio are not claimed as supported.

Screenshots and completed recordings are stored by default in `Pictures\SNAPVERE`. A completed recording is published only after encoding finishes; temporary recording files are not intentionally exposed as completed captures.

## Browser extensions

Chrome, Edge, Opera and Firefox packages offer Visible area, Select region and Full page capture. Saved files always use the **SNAPVERE** filename prefix. Settings can control whether the browser asks where to save supported captures; the product brand cannot be changed from extension settings.

Full-page capture is deliberately bounded to protect memory and stability. Very large or highly dynamic pages can be rejected instead of allowing unbounded allocation.

Screen recording is currently a Windows application feature under validation. Browser extensions do not claim video recording support.

## Privacy

Core capture and recording processing is local. SNAPVERE does not require an account, does not upload screenshots or recordings automatically and does not include first-party analytics or telemetry in the capture runtime.

See [Privacy](PRIVACY.md), [QA Matrix](QA-MATRIX.md) and [Troubleshooting](TROUBLESHOOTING.md).
