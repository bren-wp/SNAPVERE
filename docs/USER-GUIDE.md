# SNAPVERE 0.1.1 User Guide

SNAPVERE is a local-first screenshot tool for Windows and modern browsers.

## Windows

SNAPVERE starts quietly in the notification area. Use the tray icon or shortcuts:

| Action | Shortcut |
| --- | --- |
| Region capture | Print Screen or Ctrl+Shift+1 |
| Window capture | Ctrl+Shift+2 |
| Screen capture | Ctrl+Shift+3 |

Region capture freezes the current monitor image so you can select, resize and annotate without the underlying page moving. Available tools include Pen, Line, Arrow, Box, Highlight, colors and Undo. Copy sends the result to the Windows clipboard; Save writes a PNG locally.

Captures are stored by default in `Pictures\SNAPVERE`.

## Browser extensions

Chrome, Edge, Opera and Firefox packages offer Visible area, Select region and Full page capture. Saved files always use the **SNAPVERE** filename prefix. Settings can control whether the browser asks where to save supported captures; the product brand cannot be changed from extension settings.

Full-page capture is deliberately bounded to protect memory and stability. Very large or highly dynamic pages can be rejected instead of allowing unbounded allocation.

## Privacy

Core capture processing is local. SNAPVERE does not require an account, does not upload screenshots automatically and does not include first-party analytics or telemetry in the capture runtime.

See [Privacy](PRIVACY.md) and [Troubleshooting](TROUBLESHOOTING.md).
