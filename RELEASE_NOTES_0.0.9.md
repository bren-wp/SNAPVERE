# SNAPVERE 0.0.9

SNAPVERE 0.0.9 is a security, reliability, performance and workflow-quality milestone for the tray-first Windows application and the native Android companion.

## Android stability and UI/UX hardening

- Every Android capture still requires a fresh user-approved MediaProjection session; consent tokens are not cached or reused.
- The Activity-to-background handoff remains bounded to five seconds and a new seven-second first-frame timeout prevents a stalled VirtualDisplay/ImageReader path from leaving the foreground service or process-local capture lock active indefinitely.
- MediaProjection, VirtualDisplay, ImageReader, acquired Image, handler callbacks and HandlerThread teardown are now exception-safe per resource, so one vendor/API cleanup failure cannot prevent the rest of the session from being released.
- `ImageReader.acquireLatestImage()` and MediaStore cleanup paths are guarded against runtime/provider failures.
- Capture row stride, pixel stride and integer arithmetic are validated before padded-bitmap allocation; the pure layout helper has JVM unit coverage for tight rows, padded rows, invalid dimensions/strides and overflow cases.
- Capture, Open, Share, Delete, Website, Support, Privacy and Terms actions now have controlled failure paths rather than allowing Android service/provider/intent errors to escape the Activity.
- Support still prefers `mailto:info@snapvere.com`, then falls back to copying the address locally; clipboard-service failure is also handled.
- Action pairs stack vertically on narrow displays or at 1.25x+ font scale so labels and touch targets remain usable.
- OEM `forceDark` is disabled because SNAPVERE already supplies a deliberate dark palette.
- English and Croatian recovery/status resources were updated together.
- The Android manifest still has no `INTERNET` permission, app backup remains disabled, cleartext traffic remains disabled and the MediaProjection service remains non-exported.

Android CI now runs the manifest privacy/service contract, `lintDebug`, `lintRelease`, `testDebugUnitTest`, debug and minified/shrunk release builds, APK signature validation, ZIP alignment and SHA-256 artifact generation. The CI APK is debug-signed development/internal evidence; production store signing requires a separately managed private release key and is not fabricated by this repository.

## Windows tray reliability and accessibility

- The native notification-area host now opts into the Microsoft `NOTIFYICON_VERSION_4` callback contract after every successful icon add, including taskbar/Explorer recreation.
- The normal SNAPVERE tooltip is retained with `NIF_SHOWTIP`.
- Version-4 callback notifications are decoded from the low word of `lParam` as required by the shell contract.
- Keyboard select and keyboard context-menu notifications are handled alongside pointer input.
- Existing Region Capture debounce remains in place so duplicate activation notifications cannot start duplicate workflows.

## Save captures where you want

- Region, Window and Full Screen captures can open the native Windows **Save As** picker after the PNG is safely created.
- The generated SNAPVERE filename and default capture directory are suggested automatically.
- Cancelling the picker keeps the completed capture in `Pictures\SNAPVERE`; the screenshot is not discarded.
- Relocation to another folder is asynchronous and uses destination-local staging plus final atomic replacement.
- PNG-only destination validation, Windows file-identity checks and recovery-copy behavior protect alias, hard-link and uncertain-identity cases.
- Copy-to-clipboard behavior remains unchanged.

## About, support and legal destinations

The About window exposes:

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
- Shared path-boundary logic treats a protected directory itself and its descendants correctly, closing the exact-Windows-directory validation gap in Setup.
- Embedded ZIP extraction uses bounded random staging filenames so valid long NTFS names cannot overflow because of an internal temporary suffix.
- Embedded ZIP extraction rejects duplicate file destinations in addition to the existing absolute-path, traversal, expanded-size and entry-count protections.
- CI generates architecture-specific integrity manifests from the exact published x86/x64/ARM64 payloads and embeds them in the universal Portable host. Before executing a reusable `%TEMP%` cache, Portable rejects reparse points, missing/unexpected files and any file whose length or SHA-256 differs from the trusted embedded manifest; invalid caches are rebuilt transactionally and verified again.
- The first direct-ZIP comparison design was rejected by real lifecycle CI because redundant decompression could exceed the existing startup-probe window. The fix preserves the timeout and integrity gate while switching to a single sequential SHA-256 pass over cached files.
- Android capture/session cleanup is bounded and fail-safe without adding a network permission, telemetry or remote control path.
- v0.0.9 contains no WebView/WebView2, HTML/JavaScript execution surface, first-party HTTP/socket client, telemetry client, cloud-upload client or remote command channel.

The project does not claim that any software can be guaranteed free of every vulnerability. SNAPVERE is also not a sandbox against arbitrary malicious code already executing as the same Windows user.

## Performance and stability

- Tray and global-hotkey hosts remain event-driven through blocking Win32 message loops; no periodic application polling timer is added.
- Capture/D3D resources remain on-demand rather than resident solely for tray operation.
- Recent-capture enumeration avoids an unnecessary explicit metadata refresh for every PNG and tolerates expected filesystem disappearance/access races without a watcher or resident cache.
- Portable cache validation intentionally performs a foreground sequential SHA-256 read of cached payload files at Portable launch; it does not re-decompress the embedded ZIP solely to derive expected bytes.
- Android capture work has explicit failure bounds and resource cleanup; no idle capture worker or polling loop is added.
- No fixed CPU/RAM percentage or launch-time number is advertised without measurement because Windows version, DPI, monitor topology, graphics drivers and Android OEM behavior materially affect resource use.

## UX consistency

Published Windows capture shortcuts are:

- Region: **Print Screen** or `Ctrl+Shift+1`
- Window: `Ctrl+Shift+2`
- Full Screen: `Ctrl+Shift+3`

Scrolling Capture does not reserve a global shortcut until that workflow actually exists.

Android keeps one explicit **Capture screen** entry point, then delegates system consent to Android before the app moves behind the target screen. Open, Share and Delete remain user-controlled actions for the latest local PNG.

## Visual QA integrity

The rendered-UI probe was hardened after a hosted runner desktop was discovered in a successful-main Region baseline. v0.0.9 does not lower visual regression thresholds to hide that problem. Probe timing and fallback ownership checks are tightened, and capture provenance metadata is recorded in the visual-QA manifest.

## Release automation hardening

The first v0.0.9 publication attempt intentionally stopped before publishing because the immutable-tag lookup treated `gh api`'s non-throwing HTTP 404 output incorrectly. No v0.0.9 GitHub Release was published by that run.

The release workflow now explicitly checks `gh` exit codes and distinguishes only an HTTP 404 as “tag does not exist.” Other API failures stop publication. Existing annotated-tag objects are validated before reuse; annotated-tag creation and tag-ref creation also require successful exit codes and usable returned SHAs. This keeps the immutable tag fail-closed instead of guessing from an empty response.

## Two universal Windows downloads

The v0.0.9 public Windows release contains exactly:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`

Each executable embeds x86, x64 and ARM64 application payloads and selects a compatible payload at runtime. Android CI artifacts remain separate from this exact two-file Windows release contract.

## Release validation

Publication is blocked unless the final v0.0.9 source passes audited dependency restore, x64 build/tests, x86 build, ARM64 cross-build, payload/integrity-manifest validation, real rendered UI capture, exact two-file Windows packaging, and x64/x86 universal Setup/Portable lifecycle plus tray-first checks. Android source changes separately require its manifest privacy/service gate, debug/release lint, JVM tests, debug/release builds, APK signature, alignment and SHA-256 artifact validation.

ARM64 validation on the hosted x64 runner is cross-build/package validation, not a real ARM64 hardware runtime test. A green Android CI is build/lint/unit/package evidence rather than a claim that every physical OEM/device combination was exercised. The Windows binaries are not represented as Authenticode-signed unless a real signing step is introduced and independently verified. SHA-256 values published with the release prove byte identity, not publisher trust.
