# SNAPVERE

**Capture. Edit. Done.**

SNAPVERE 0.1.1 is a fast, local-first screenshot toolkit for **Windows, Chrome, Edge, Opera and Firefox**. It is designed for direct capture workflows without an account, first-party analytics, automatic cloud upload or a permanent desktop dashboard.

Current release: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

## Downloads

| Platform | Package |
| --- | --- |
| Windows Setup | `SNAPVERE-Setup.exe` |
| Windows Portable | `SNAPVERE-Portable.exe` |
| Chrome | `SNAPVERE-Chrome.zip` |
| Edge | `SNAPVERE-Edge.zip` |
| Opera | `SNAPVERE-Opera.zip` |
| Firefox | `SNAPVERE-Firefox.zip` |

The active product contract contains only these six packages. Browser ZIPs are release packages for manual installation; external store approval is not claimed unless an actual store listing exists.

## Windows

The Windows app is tray-first and supports:

- Region capture — Print Screen or `Ctrl+Shift+1`;
- Window capture — `Ctrl+Shift+2`;
- Screen capture — `Ctrl+Shift+3`;
- frozen-frame region selection and resize;
- Pen, Line, Arrow, Box and Highlight annotations;
- Copy and local PNG Save;
- local settings, recent captures and language selection;
- universal x86, x64 and ARM64 payloads.

Captures are stored by default in `Pictures\SNAPVERE`. PNG writes use staging before final move so an interrupted encode is not intentionally exposed as a completed capture.

## Browser extensions

Chrome, Edge, Opera and Firefox provide:

- Capture visible area;
- Select region;
- bounded full-page capture.

The permission contract is exactly `activeTab`, `scripting`, `downloads` and `storage`, without broad host access. The visible product name, wordmark and saved capture prefix are fixed to **SNAPVERE** and cannot be changed in extension settings.

Full-page tiles are decoded, drawn into one bounded destination canvas and released immediately. Explicit tile/canvas/pixel limits prevent unbounded memory growth. Active-tab ownership is checked before and after frame capture so a tab switch cannot silently save content from the wrong tab.

## Stability and performance

SNAPVERE uses bounded capture state, cancellation-aware local workflows, self-contained package validation and package-size regression budgets. Windows PNG compression runs off the WinUI thread and avoids an unnecessary full copy of the compressed buffer. Browser full-page capture avoids retaining a second set of decoded tile images.

No software can truthfully guarantee that a platform or driver will never fail. SNAPVERE instead uses controlled error handling, resource cleanup and automated regression gates so failures do not become silent corruption or uncontrolled resource use.

## Verification

Windows CI builds/tests x64, builds x86 and ARM64, captures rendered UI snapshots, builds universal Setup/Portable, validates x64/x86 package lifecycle and enforces package-size budgets.

Browser CI validates permissions, locked branding, EN/HR resources, cross-browser parity, runtime capture behavior, bounded memory design, deterministic package output and store-readiness metadata. Product Contract CI and CodeQL run independently.

## Privacy

Core capture processing is local. SNAPVERE does not require a user account for capture and does not include first-party screenshot telemetry or automatic cloud upload in the capture runtime.

Read [User Guide](docs/USER-GUIDE.md), [Privacy](docs/PRIVACY.md), [Troubleshooting](docs/TROUBLESHOOTING.md), [Product Status](docs/PRODUCT-STATUS.md), [QA Matrix](docs/QA-MATRIX.md) and [Security Policy](SECURITY.md).

## Publisher and support

SNAPVERE is the product brand. **Brendigo** is the developer and publisher.

Official site: https://snapvere.com  
Support: info@snapvere.com  
Publisher: https://brendigo.com

Historical release facts are retained in [RELEASES.md](RELEASES.md); active product documentation describes the currently maintained Windows/browser product surface.
