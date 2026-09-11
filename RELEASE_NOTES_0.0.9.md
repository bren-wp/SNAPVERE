# SNAPVERE 0.0.9

SNAPVERE 0.0.9 is a security, reliability and workflow-quality milestone for the tray-first Windows capture application.

## Save captures where you want

- Region, Window and Full Screen captures can open the native Windows **Save As** picker after the PNG is safely created.
- The generated SNAPVERE filename and default capture directory are suggested automatically.
- Cancelling the picker keeps the completed capture in `Pictures\SNAPVERE`; the screenshot is not discarded.
- Relocation to another folder is asynchronous and uses destination-local staging plus final atomic replacement.
- PNG-only destination validation, Windows file-identity checks and recovery-copy behavior protect alias, hard-link and uncertain-identity cases.
- Copy-to-clipboard behavior remains unchanged.

## About, support and legal destinations

The About window now exposes:

- official product site: `https://snapvere.com`
- support: `info@snapvere.com`
- Privacy: `https://snapvere.com/privacy`
- Terms: `https://snapvere.com/terms`
- developer: `https://brendigo.com`

Support, Privacy and Terms labels are localized through the shared language catalog. Croatian uses `Podrška`, `Privatnost` and `Uvjeti`. If Windows has no registered mail client, the support action falls back to copying `info@snapvere.com` to the local clipboard and visibly confirms the address. The central About card is vertically scrollable so increased Windows text scaling does not make these controls unreachable.

The `/privacy` and `/terms` paths are stable product-owned canonical URLs. Their website deployment is tracked separately and the desktop application does not prefetch them.

## Security hardening

- NuGet auditing is explicitly enabled for direct and transitive dependencies at `low` severity and above.
- `NU1901` through `NU1904` are treated as build errors so known package vulnerabilities block validation.
- GitHub Actions used by CI/release are pinned to full commit SHAs rather than mutable major tags.
- Normal CI checkout does not persist repository credentials.
- Dependabot monitors NuGet and GitHub Actions dependencies weekly.
- Shared path-boundary logic now treats a protected directory itself and its descendants correctly, closing the exact-Windows-directory validation gap in Setup.
- Embedded ZIP extraction uses bounded random staging filenames so valid long NTFS names cannot overflow because of an internal temporary suffix.
- Embedded ZIP extraction rejects duplicate file destinations in addition to the existing absolute-path, traversal, expanded-size and entry-count protections.
- CI generates architecture-specific integrity manifests from the exact published x86/x64/ARM64 payloads and embeds them in the universal Portable host. Before executing a reusable `%TEMP%` cache, Portable rejects reparse points, missing/unexpected files and any file whose length or SHA-256 differs from the trusted embedded manifest; invalid caches are rebuilt transactionally and verified again.
- The first direct-ZIP comparison design was rejected by real lifecycle CI because redundant decompression could exceed the existing startup-probe window. The fix preserves the timeout and integrity gate while switching to a single sequential SHA-256 pass over cached files.
- v0.0.9 contains no WebView/WebView2, HTML/JavaScript execution surface, first-party HTTP/socket client, telemetry client, cloud-upload client or remote command channel.

The project does not claim that any software can be guaranteed free of every vulnerability. SNAPVERE is also not a sandbox against arbitrary malicious code already executing as the same Windows user.

## Performance and stability

- Tray and global-hotkey hosts remain event-driven through blocking Win32 message loops; no periodic application polling timer is added.
- Capture/D3D resources remain on-demand rather than resident solely for tray operation.
- Recent-capture enumeration avoids an unnecessary explicit metadata refresh for every PNG and tolerates expected filesystem disappearance/access races without a watcher or resident cache.
- Portable cache validation intentionally performs a foreground sequential SHA-256 read of cached payload files at Portable launch; it does not re-decompress the embedded ZIP solely to derive expected bytes, and no fixed launch-time claim is made without measurement.
- No fixed CPU/RAM percentage is advertised; resource use depends on Windows, DPI, monitor topology, drivers and whether a capture/editor session is active.

## UX consistency

Published capture shortcuts are:

- Region: **Print Screen** or `Ctrl+Shift+1`
- Window: `Ctrl+Shift+2`
- Full Screen: `Ctrl+Shift+3`

Scrolling Capture does not reserve a global shortcut until that workflow actually exists.

## Visual QA integrity

The rendered-UI probe was hardened after a hosted runner desktop was discovered in a successful-main Region baseline. v0.0.9 does not lower visual regression thresholds to hide that problem. Probe timing and fallback ownership checks are tightened, and capture provenance metadata is recorded in the visual-QA manifest.

## Two universal downloads

The v0.0.9 public release contains exactly:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`

Each executable embeds x86, x64 and ARM64 application payloads and selects a compatible payload at runtime.

## Release validation

Publication is blocked unless the v0.0.9 source itself passes audited dependency restore, x64 build/tests, x86 build, ARM64 cross-build, payload validation, real rendered UI capture, exact two-file packaging, and x64/x86 universal Setup/Portable lifecycle plus tray-first checks.

ARM64 validation on the hosted x64 runner is cross-build/package validation, not a real ARM64 hardware runtime test. The binaries are not represented as Authenticode-signed unless a real signing step is introduced and independently verified. SHA-256 values published with the release prove byte identity, not publisher trust.
