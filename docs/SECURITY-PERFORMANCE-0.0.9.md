# SNAPVERE 0.0.9 security, stability and performance hardening

This document records the v0.0.9 hardening work and, equally importantly, the limits of what the repository and CI actually prove.

## Security surface

Static review of the v0.0.9 desktop codebase found no WebView/WebView2, HTML renderer, JavaScript execution path or first-party HTTP/socket client. Browser-style XSS is therefore not an applicable attack surface for this version. This is not a claim that all software vulnerabilities are impossible.

External website and mail destinations in the About surface are compile-time product destinations and are opened only after an explicit user click. Opening SNAPVERE or the About window does not prefetch them.

SNAPVERE does not contain a remote command channel, updater, cloud upload client, telemetry client or licensing network client in v0.0.9.

## Dependency and build-chain hardening

- NuGet auditing is explicitly enabled for direct and transitive packages.
- Audit severity starts at `low`.
- `NU1901`, `NU1902`, `NU1903` and `NU1904` are build errors, so a known advisory blocks CI and release validation.
- GitHub Actions used by CI are pinned to full commit SHAs rather than mutable major tags.
- Normal CI checkout does not persist repository credentials.
- Dependabot monitors NuGet and GitHub Actions dependencies weekly.

These controls reduce dependency and CI supply-chain risk. They do not replace publisher signing of the final Windows binaries.

## Package and filesystem hardening

Embedded ZIP extraction continues to reject absolute paths and parent traversal, canonicalizes destinations, limits entry count and expanded size, rejects duplicate file destinations, and writes through a staging file before replacement.

v0.0.9 changes the staging filename to a fixed bounded form:

```text
.snapvere-<guid>.tmp
```

The user- or package-supplied destination basename is no longer extended with additional staging suffixes. This prevents a valid long NTFS filename from failing merely because the temporary filename would exceed the component limit.

The Portable host no longer treats the user-writable `%TEMP%` extraction cache as trusted merely because `.ready` and `Snapvere.exe` exist. Before launching the extracted application it compares every expected payload file byte-for-byte with the embedded architecture-specific ZIP, rejects missing or unexpected files and reparse points, and rebuilds an invalid cache transactionally from the embedded payload. The rebuilt cache is verified again before execution. This reduces persistence/tampering risk in the reusable Portable cache without claiming to sandbox malicious code already running as the same Windows user.

Shared path-boundary logic now treats the protected directory itself and its descendants as inside the boundary while rejecting parents and sibling names that only share a prefix. Setup uses this logic when rejecting the Windows system directory and when identifying its own installed files/processes.

## Capture-save hardening

The save-location work included in the v0.0.9 milestone writes a completed PNG to the safe default location before asking for a final destination. Cancellation therefore does not discard the capture.

Relocation uses a destination-local staging file, asynchronous copy, final atomic replacement, PNG-only destination validation and Windows file-identity checks. The source is deleted only when Windows positively identifies source and destination as different files. Alias or uncertain-identity cases preserve the source as a recovery copy.

## UI and UX correctness

- Region Capture remains available from Print Screen and `Ctrl+Shift+1`.
- Window Capture uses `Ctrl+Shift+2`.
- Full Screen Capture uses `Ctrl+Shift+3` in v0.0.9.
- No global hotkey is reserved for Scrolling Capture until a real scrolling workflow exists.
- About exposes the official product site, support email, Privacy and Terms destinations.
- About content is vertically scrollable so increased Windows text scaling does not make the support/legal controls unreachable.
- If Windows has no registered mail client, the support action can fall back to copying `info@snapvere.com` locally to the clipboard.

## Performance and idle-resource policy

SNAPVERE's tray and global-hotkey hosts use blocking Win32 `GetMessage` loops. They do not wake on a periodic application timer to poll for input.

Capture and Direct3D resources are created for capture work instead of being kept active solely for tray residency. Secondary windows are created on demand.

Recent-capture enumeration remains bounded and on demand. v0.0.9 removes an unnecessary explicit metadata refresh for every discovered PNG and tolerates expected directory access/disappearance races without adding a file watcher, database or resident cache.

Portable cache validation adds foreground work when the public Portable executable is launched because the reusable extracted payload is compared with its embedded source before execution. No startup-time claim is made here; CI lifecycle validation must remain green, and real launch-time benchmarking is separate from static review.

The project deliberately does not publish invented RAM or CPU percentages. Working set and CPU depend on Windows version, DPI, monitor count, graphics driver and whether a capture/editor session is active. The performance contract is architectural: no unnecessary periodic idle loop, no telemetry worker, no language/network worker and no always-live capture GPU pipeline.

## Visual QA integrity

The pre-v0.0.9 visual-QA pipeline exposed a provenance race: a hosted runner desktop could be captured instead of the intended SNAPVERE region overlay and then become a successful-main baseline. v0.0.9 hardens the probe so fallback capture is tied to the SNAPVERE overlay's native window contract, transient render timing is retried, and the manifest records capture provenance metadata.

Visual comparison thresholds are not relaxed to hide this problem. A clean main baseline must be established before the v0.0.9 feature set is accepted.

## Release validation

The v0.0.9 release is allowed to publish only after the release source itself passes:

- audited dependency restore;
- x64 build and unit tests;
- x86 build;
- ARM64 cross-build;
- native payload validation;
- real rendered UI capture;
- exact two-file universal package contract;
- x64 and x86 Setup/Portable lifecycle and tray-first checks;
- exact immutable release-tag validation;
- post-publication verification that the GitHub Release contains exactly the two expected executables.

ARM64 validation on the hosted x64 runner remains cross-build/package evidence, not a claim of real ARM64 hardware runtime testing.

## Remaining trust boundary

v0.0.9 binaries are not described as Authenticode-signed unless a real signing certificate and verification step are added. SHA-256 release digests verify byte identity but do not independently authenticate the publisher.

Likewise, SNAPVERE is not a sandbox against arbitrary malicious code already executing as the same Windows user. The product's responsibility is not to introduce an additional remote execution path, unsafe package extraction, destructive path mistake or hidden network channel of its own.
