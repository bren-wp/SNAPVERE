# SNAPVERE 0.0.9 security, stability and performance hardening

This document records the v0.0.9 hardening work across Windows and Android and, equally importantly, the limits of what the repository and CI actually prove.

## Security surface

Static review of the v0.0.9 desktop codebase found no WebView/WebView2, HTML renderer, JavaScript execution path or first-party HTTP/socket client. Browser-style XSS is therefore not an applicable attack surface for this version. This is not a claim that all software vulnerabilities are impossible.

External website and mail destinations are compile-time product destinations and open only after explicit user input. Opening SNAPVERE or About does not prefetch them.

The Android application likewise contains no first-party HTTP client and intentionally declares no `android.permission.INTERNET`. Its user-initiated Website, Support, Privacy and Terms actions delegate to Android browser/mail handlers. SNAPVERE does not contain a remote command channel, updater, cloud upload client, telemetry client or licensing network client in v0.0.9.

## Dependency and build-chain hardening

- NuGet auditing is explicitly enabled for direct and transitive packages.
- Audit severity starts at `low`.
- `NU1901`, `NU1902`, `NU1903` and `NU1904` are build errors, so a known advisory blocks CI and release validation.
- GitHub Actions used by CI/release are pinned to full commit SHAs rather than mutable major tags.
- Normal CI checkout does not persist repository credentials.
- Dependabot monitors NuGet and GitHub Actions dependencies weekly.
- Android CI runs both debug and release lint with warnings treated as errors, JVM unit tests, debug/release builds, APK signature validation, ZIP alignment and SHA-256 artifact generation.
- Android CI fails if `INTERNET` is introduced or if the non-exported MediaProjection foreground-service/privacy contract is weakened.

These controls reduce dependency and CI supply-chain risk. They do not replace publisher signing of the final Windows binaries or a privately managed Android production signing key.

## Package and filesystem hardening

Embedded ZIP extraction continues to reject absolute paths and parent traversal, canonicalizes destinations, limits entry count and expanded size, rejects duplicate file destinations, and writes through a staging file before replacement.

v0.0.9 uses a fixed bounded staging filename:

```text
.snapvere-<guid>.tmp
```

The user- or package-supplied destination basename is no longer extended with additional staging suffixes. This prevents a valid long NTFS filename from failing merely because an internal temporary filename would exceed the component limit.

The Portable host no longer treats the user-writable `%TEMP%` extraction cache as trusted merely because `.ready` and `Snapvere.exe` exist. CI generates an architecture-specific integrity manifest from the exact published payload before the universal Portable host is built. That embedded manifest records each expected relative path, file length and SHA-256 digest. Before launching a cached application, Portable rejects reparse points, missing or unexpected files and any file whose length or SHA-256 digest differs from the trusted manifest. An invalid cache is rebuilt transactionally and verified again before execution.

The first implementation compared cached files directly with a second decompression pass over the large embedded ZIP. Real package lifecycle CI showed that this could exceed the existing 60-second Portable startup-probe window. The implementation was corrected at the root cause: cache verification now performs one sequential hash read of extracted files against the embedded manifest rather than weakening the timeout.

Shared path-boundary logic treats the protected directory itself and its descendants as inside the boundary while rejecting parents and sibling names that merely share a prefix. Setup uses this logic when protecting Windows system paths and identifying its own installation.

## Capture-save hardening

The Windows save-location workflow writes a completed PNG to the safe default location before asking for a final destination. Cancellation therefore does not discard the capture.

Relocation uses a destination-local staging file, asynchronous copy, final atomic replacement, PNG-only destination validation and Windows file-identity checks. The source is deleted only when Windows positively identifies source and destination as different files. Alias or uncertain-identity cases preserve the source as a recovery copy.

## Windows tray hardening

The native notification-area host uses the modern `NOTIFYICON_VERSION_4` callback contract. After every successful icon add, including Explorer/taskbar recreation, the host calls `NIM_SETVERSION`. `NIF_SHOWTIP` preserves the normal tooltip under version 4.

Version-4 callback notifications are decoded from the low word of `lParam`; keyboard select and context-menu notifications are handled in addition to pointer events. Existing Region Capture debounce remains active so duplicate shell activation notifications cannot start overlapping capture workflows.

## Android capture lifecycle hardening

Every Android capture remains explicit and user-approved through a fresh MediaProjection session. Consent data is not cached or reused.

Two bounded failure guards prevent indefinite ownership:

- five seconds for the Activity-to-background handoff;
- seven seconds for first-frame delivery after VirtualDisplay creation.

If Android or a graphics driver never delivers a frame, the foreground capture service fails visibly and tears the session down rather than leaving capture permanently active.

Cleanup is idempotent and per-resource. Timeouts, handler callbacks, VirtualDisplay, ImageReader, MediaProjection callback, MediaProjection, acquired Image and HandlerThread are each cleared/released independently. A RuntimeException from one platform cleanup call does not prevent later resources or the global capture ownership flag from being released.

`ImageReader.acquireLatestImage()` is guarded. Every acquired `Image` is closed even when bitmap conversion or persistence fails. MediaStore cleanup of an incomplete PNG is best-effort and cannot mask the original persistence failure.

Before padded-bitmap allocation, `CaptureBufferLayout` validates width, pixel stride, minimum row bytes, row stride and integer overflow. JVM tests cover tight rows, padded rows, invalid dimensions/strides and overflowing arithmetic.

## Android UI/UX robustness

Capture, Open, Share, Delete, Website, Support, Privacy and Terms all contain explicit failure handling for unavailable system services, stale MediaStore URIs, missing intent handlers and provider errors. These failures update the local status surface instead of escaping as Activity-level crashes.

Action pairs automatically stack on narrow screens or at 1.25x+ font scale. The app remains scrollable, respects system-bar insets, keeps 52 dp minimum action height and disables OEM `forceDark` processing because the SNAPVERE palette is already deliberately dark.

## Performance and idle-resource policy

SNAPVERE's Windows tray and global-hotkey hosts use blocking Win32 `GetMessage` loops. They do not wake on a periodic application timer to poll for input. Capture and Direct3D resources are created for capture work instead of being kept alive solely for tray residency; secondary windows are on demand.

Recent-capture enumeration remains bounded and on demand. v0.0.9 removes an unnecessary explicit metadata refresh for every discovered PNG and tolerates expected directory access/disappearance races without adding a file watcher, database or resident cache.

Portable cache validation adds foreground work at launch because cached payload files are SHA-256 checked before execution. The validator reads cached files sequentially and does not re-decompress the embedded ZIP simply to re-derive expected bytes. No fixed launch-time claim is made; lifecycle CI remains the regression gate.

Android has no idle capture worker. MediaProjection, ImageReader and capture-thread resources exist only during a user-approved capture session, and bounded timeouts prevent a stalled session from becoming unintended background work.

The project deliberately does not publish invented RAM or CPU percentages. Working set and CPU depend on Windows version, DPI, monitor count, graphics driver, Android device/OEM behavior and whether a capture/editor session is active.

## Visual QA integrity

The pre-v0.0.9 visual-QA pipeline exposed a provenance race: a hosted runner desktop could be captured instead of the intended SNAPVERE Region overlay and then become a successful-main baseline. v0.0.9 hardens the probe so fallback capture is tied to the SNAPVERE overlay's native-window contract, transient render timing is retried, and capture provenance metadata is recorded.

Visual comparison thresholds are not relaxed to hide this problem. A clean final `main` baseline is required before release acceptance.

## Release automation integrity

The first v0.0.9 publication run passed source validation, builds, tests, payload/integrity-manifest checks, six rendered UI surfaces, exact two-file packaging and x64/x86 lifecycle checks, then stopped before publication because the tag lookup assumed a missing `gh api` ref would throw a PowerShell exception. `gh api` instead returned a nonzero exit code and HTTP 404 output, which was incorrectly treated as an existing-but-empty ref.

The corrected workflow is fail-closed:

- `gh` exit code is checked explicitly;
- only an actual `HTTP 404` is accepted as “tag does not exist”;
- all other lookup failures abort publication;
- existing tag/ref objects must contain usable SHAs;
- annotated-tag resolution checks its own API exit code;
- annotated-tag creation and tag-ref creation each check exit code and returned SHA before continuing.

That failed first attempt did not publish a GitHub Release. The definitive release must be retriggered from the final validated source after this correction.

## Release validation

The v0.0.9 Windows release is allowed to publish only after the release source itself passes:

- audited dependency restore;
- x64 build and unit tests;
- x86 build;
- ARM64 cross-build;
- native payload + integrity-manifest validation;
- six real rendered UI captures;
- exact two-file universal package contract;
- x64 and x86 Setup/Portable lifecycle and tray-first checks;
- immutable tag validation/creation;
- publication of exactly `SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe`;
- post-publication GitHub asset digest verification.

Android source changes separately require the manifest privacy/service contract, debug/release lint, JVM tests, debug/release builds, debug APK signature, ZIP alignment and SHA-256 artifact generation for the exact final source revision.

ARM64 validation on the hosted x64 runner remains cross-build/package evidence, not real ARM64 hardware runtime testing. A green Android CI is build/lint/unit/package evidence and is not represented as physical runtime coverage of every OEM/device combination.

## Remaining trust boundary

v0.0.9 Windows binaries are not described as Authenticode-signed unless a real signing certificate and verification step are added. SHA-256 release digests verify byte identity but do not independently authenticate the publisher.

The Android Actions APK is debug-signed and is not represented as a production Play Store/release-signed package. Production signing requires a separately managed private key that must not be committed to this repository.

SNAPVERE is not a sandbox against arbitrary malicious code already executing as the same OS user. The product's responsibility is not to introduce an additional remote execution path, unsafe package extraction, destructive path mistake, stuck capture ownership or hidden network channel of its own.
