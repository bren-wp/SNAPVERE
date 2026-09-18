<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE" src="assets/branding/readme/snapvere-logo-light.svg" width="420">
</picture>

### Capture. Edit. Done.

**Fast, local-first screenshot capture for Windows and modern browsers.**  
No account. No screenshot telemetry. No automatic cloud upload.

[![Windows CI](https://github.com/bren-wp/SNAPVERE/actions/workflows/ci.yml/badge.svg)](https://github.com/bren-wp/SNAPVERE/actions/workflows/ci.yml)
[![Extensions CI](https://github.com/bren-wp/SNAPVERE/actions/workflows/extensions-ci.yml/badge.svg)](https://github.com/bren-wp/SNAPVERE/actions/workflows/extensions-ci.yml)
[![Product Contract](https://github.com/bren-wp/SNAPVERE/actions/workflows/product-contract-ci.yml/badge.svg)](https://github.com/bren-wp/SNAPVERE/actions/workflows/product-contract-ci.yml)
[![CodeQL](https://github.com/bren-wp/SNAPVERE/actions/workflows/codeql.yml/badge.svg)](https://github.com/bren-wp/SNAPVERE/actions/workflows/codeql.yml)

[Website](https://snapvere.com) · [Download v0.1.4](https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.4) · [Documentation](docs/README.md) · [Croatian](README.hr.md)

</div>

---

## Why SNAPVERE

SNAPVERE 0.1.4 is built for people who want screenshot tools that stay focused on the job: capture the right pixels, annotate quickly, copy or save locally, and get out of the way.

| | What you get |
| --- | --- |
| ⚡ **Fast capture** | Region, window and screen capture on Windows, plus visible-area, region and bounded full-page capture in browsers. |
| ✏️ **Built-in annotation** | Pen, Line, Arrow, Box and Highlight tools directly in the Windows region workflow. |
| 🖥️ **Native Windows workflow** | Tray-first operation, global shortcuts, recent captures, local settings and DPI-aware multi-monitor handling. |
| 🔒 **Local-first by design** | Core capture processing stays local; no account is required for capture and no first-party screenshot telemetry is built into the capture runtime. |
| 📦 **Portable or installed** | Universal Windows Setup and Portable packages carry x86, x64 and ARM64 application payloads. |
| 🌐 **Browser coverage** | Chrome, Edge, Opera and Firefox share the same locked SNAPVERE brand and bounded capture behavior. |

0.1.4 also tightens the user-facing Windows experience: internal exception details stay in bounded local diagnostics instead of appearing in Settings, Language, About, Setup or Region Capture surfaces; the tray shortcut labels match the registered hotkeys; and repeated unchanged preference writes are avoided.

Current `main` additionally contains **unreleased post-0.1.4 reliability hardening**: collision-safe concurrent PNG publication, owner-safe browser capture locks, stale Recent-load suppression, guarded browser/Windows actions, multi-monitor tray fallback clamping, secondary-window recovery/accessibility fixes and responsive/reduced-motion browser UI polish. These source changes do not retroactively modify the published v0.1.4 binaries.

## Windows capture workflow

SNAPVERE runs tray-first instead of keeping a permanent dashboard open.

| Action | Shortcut | Result |
| --- | --- | --- |
| **Region Capture** | `Print Screen` or `Ctrl+Shift+1` | Freeze the desktop, select a region, resize it, annotate, copy or save. |
| **Window Capture** | `Ctrl+Shift+2` | Pick a visible window from the frozen desktop and capture it cleanly. |
| **Screen Capture** | `Ctrl+Shift+3` | Capture the active display workflow immediately. |

Windows captures are stored by default in `Pictures\SNAPVERE`. PNG saving uses staging before the final move so an interrupted encode is not intentionally exposed as a completed capture.

## Built for real multi-monitor desktops

SNAPVERE uses native display geometry and DPI conversion instead of assuming every monitor has the same scale. Current memory hardening removes redundant full-frame staging allocations from Region and Window Capture paths and releases frozen monitor buffers as soon as their UI bitmap is ready.

Read the implementation-focused notes in [Performance & Stability](docs/PERFORMANCE.md), [Window Capture](docs/WINDOW-CAPTURE.md) and [Multi-monitor](docs/MULTI-MONITOR.md).

## Browser extensions

<img src="ekstenzije/chrome/icons/icon-128.png" alt="SNAPVERE browser extension icon" width="96" align="right">

The Chrome, Edge, Opera and Firefox variants provide:

- visible-area capture;
- selected-region capture;
- bounded full-page capture;
- local PNG download;
- English and Croatian UI;
- fixed SNAPVERE product name, wordmark and saved-file prefix.

The permission contract is exactly `activeTab`, `scripting`, `downloads`, `downloads.open` and `storage`, without broad host access. `downloads.open` is used only after the user explicitly chooses **Open** for a completed SNAPVERE item in Recent captures. Full-page capture decodes and draws tiles into one bounded destination canvas and releases tile resources immediately instead of retaining an unbounded image set.

Browser ZIP packages are release packages for manual installation. External store approval is not claimed unless an actual store listing exists.

## See SNAPVERE in action

These are **real rendered Windows surfaces captured by SNAPVERE's visual-QA pipeline** from the same validated codebase — not design mockups.

<table>
<tr>
<td width="38%" valign="top"><img src="assets/branding/readme/screenshots/tray-menu.png" alt="SNAPVERE tray menu"><br><strong>Tray-first workflow</strong><br>Capture region, window or screen without keeping a permanent dashboard open.</td>
<td width="62%" valign="top"><img src="assets/branding/readme/screenshots/options.png" alt="SNAPVERE Settings window"><br><strong>Local settings</strong><br>Startup, cursor and language preferences remain focused and local to the Windows account.</td>
</tr>
</table>

<img src="assets/branding/readme/screenshots/window-capture.png" alt="SNAPVERE Window Capture overlay" width="760">

**Window Capture** — a frozen desktop target-selection surface with clear visual focus before capture.

## Downloads

Current release: **SNAPVERE 0.1.4**<br>
Release page: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.4

| Platform | Package |
| --- | --- |
| Windows Setup | `SNAPVERE-Setup.exe` |
| Windows Portable | `SNAPVERE-Portable.exe` |
| Chrome | `SNAPVERE-Chrome.zip` |
| Edge | `SNAPVERE-Edge.zip` |
| Opera | `SNAPVERE-Opera.zip` |
| Firefox | `SNAPVERE-Firefox.zip` |

The active product contract contains these six maintained packages.

## Quality you can inspect

SNAPVERE does not treat a successful compile as sufficient evidence. CI currently validates:

- x64 build plus the unit-test suite;
- x86 build and ARM64 cross-build;
- real rendered WinUI screenshots for Region, Window, Tray, Options, Language and About surfaces;
- visual comparison against the last successful `main` baseline;
- universal Setup/Portable construction;
- exact public package contract and package-size regression budgets;
- real x64/x86 Setup and Portable lifecycle completion;
- browser manifest, permission, runtime, brand-lock, locale and cross-browser parity rules;
- bounded browser full-page memory design and deterministic extension packages;
- Product Contract CI and CodeQL for C#, JavaScript/TypeScript, Python and GitHub Actions.

These gates reduce regression risk; they are not a claim that Windows, drivers or browsers can never fail.

## Privacy & security posture

Core screenshot processing is local. SNAPVERE does not require a user account for capture, does not add first-party screenshot analytics to the capture runtime and does not automatically upload captures to a cloud service.

For the exact boundaries and security model, read [Privacy](docs/PRIVACY.md), [Security Policy](SECURITY.md), [Architecture](docs/ARCHITECTURE.md) and [QA Matrix](docs/QA-MATRIX.md).

## Documentation

| Guide | Purpose |
| --- | --- |
| [User Guide](docs/USER-GUIDE.md) | Everyday Windows and browser usage. |
| [Installation](docs/INSTALLATION.md) | Setup, Portable and browser package installation. |
| [Performance & Stability](docs/PERFORMANCE.md) | Memory, lifecycle and regression-hardening notes. |
| [Window Capture](docs/WINDOW-CAPTURE.md) | Native window discovery, frozen picker and WGC acquisition. |
| [Image Pipeline](docs/IMAGE-PIPELINE.md) | Frame validation, crop/annotation, PNG encode and atomic publication. |
| [Tray & Lifecycle](docs/TRAY-LIFECYCLE.md) | Singleton, native tray, Explorer recovery and package lifecycle. |
| [Browser Extensions](docs/BROWSER-EXTENSIONS.md) | Browser architecture, permissions and behavior. |
| [Region Capture](docs/REGION-CAPTURE.md) | Selection and annotation workflow. |
| [Multi-monitor](docs/MULTI-MONITOR.md) | DPI and desktop-layout behavior. |
| [Troubleshooting](docs/TROUBLESHOOTING.md) | Recovery guidance and diagnostics. |
| [Product Status](docs/PRODUCT-STATUS.md) | Maintained product surfaces and evidence boundaries. |
| [Branding](docs/BRANDING.md) | Product identity and asset usage. |

## Brand & publisher

**SNAPVERE** is the product brand. **Brendigo** is the developer and publisher.

Official site: https://snapvere.com  
Support: info@snapvere.com  
Publisher: https://brendigo.com

Historical release facts remain in [RELEASES.md](RELEASES.md); active documentation describes the currently maintained Windows/browser product surface.
