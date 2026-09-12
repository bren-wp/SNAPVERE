# Changelog

All notable SNAPVERE changes are documented here. Published release tags and assets are immutable; later documentation may clarify evidence boundaries but does not rewrite historical binaries.

## [0.1.1] - 2026-09-12

### Browser extensions become public release assets

- Adds official v0.1.1 packages for Google Chrome, Microsoft Edge, Opera and Mozilla Firefox.
- Browser extensions provide local-first visible-area, bounded full-page and selected-region PNG capture.
- Chromium variants use Manifest V3 service workers; Firefox uses the compatible Manifest V3 background-script model.
- All browser variants request only `activeTab`, `scripting`, `downloads` and `storage`; there is no `<all_urls>` or broad host permission.
- English and Croatian browser interfaces, local SNAPVERE icons and dark/violet product styling are included.

### Browser security, reproducibility and store readiness

- Cross-browser parity validation prevents shared runtime divergence between Chrome, Edge, Opera and Firefox.
- Chrome/Edge/Opera manifests must remain identical and Firefox may differ only in expected background/Gecko metadata.
- CI rejects remote runtime scripts, broad host access, unsafe dynamic-code patterns, inline runtime code and common development/cache artifacts.
- Browser ZIP packaging is deterministic: CI runs two clean package passes and requires byte-for-byte identical ZIP files and checksum manifests.
- Store listing metadata, permission justifications, privacy/data-practice declarations and EN/HR copy are validated against the actual manifests.
- Deterministic store screenshots/promo graphics and reviewer notes are generated/prepared for external publisher submission.
- Actual Chrome Web Store, Edge Add-ons, Opera Add-ons and Mozilla Add-ons publication remains an external authenticated review/signing process and is not inferred from repository CI.

### Version alignment

- Windows product/file/assembly version moves to `0.1.1` / `0.1.1.0`.
- Android moves to `versionName 0.1.1` / `versionCode 11`.
- Chrome, Edge, Opera and Firefox manifests move to `0.1.1` and canonical store metadata is aligned to the same release version.
- Windows and Android CI version gates are updated so they validate/package 0.1.1 rather than the previous 0.1.0 line.

### v0.1.1 release automation

- Adds a dedicated v0.1.1 release workflow with separate Android and browser validation/package jobs plus a Windows final release job.
- Android continues the transparent validated CI/debug-signed public APK path while still requiring debug/release lint, JVM tests, debug/release builds, signature/alignment and SHA-256 checks.
- Browser release packaging repeats manifest/privacy/parity/store-readiness validation and deterministic packaging on the exact release commit.
- Windows release validation re-runs audited x64 build/tests, x86 build, ARM64 cross-build, architecture payload integrity and x64/x86 Setup/Portable lifecycle/tray-first checks.
- The immutable `v0.1.1` tag is created only after Android, browser and Windows release gates pass.
- The public v0.1.1 contract contains exactly eight assets: Windows Setup/Portable, Android APK/source archive and four browser ZIP packages.
- SHA-256 is verified through artifact transfer and again against GitHub's published digest for all eight public assets.
- Historical v0.1.0 remains immutable with exactly its original four public Windows/Android assets.

### Documentation

- Updates English and Croatian root documentation for v0.1.1.
- Updates Android and browser-extension architecture/QA documentation.
- Adds v0.1.1 release notes and English/Croatian security/privacy/performance evidence documents.

## [0.1.0] - 2026-09-11

### Unified Windows + Android release

- Introduces the first SNAPVERE release contract containing Windows Setup, Windows Portable, a public Android APK and an Android source archive together.
- Public v0.1.0 assets are exactly `SNAPVERE-Setup.exe`, `SNAPVERE-Portable.exe`, `SNAPVERE.apk` and `SNAPVERE-Android-Source.zip`.
- Windows Setup/Portable remain universal hosts with x86, x64 and ARM64 native application payloads.
- Android moves to versionName `0.1.0` / versionCode `10`.

### Android stability and correctness

- Foreground-service notification initialization failure is contained and returned as a controlled capture failure instead of intentionally escaping service creation.
- Handler scheduling for task-hide and first-frame work is checked so a dead/rejected capture thread cannot leave a session stuck indefinitely.
- Five-second Activity-hide and seven-second first-frame bounds remain unchanged.
- Capture teardown is owner-aware: stale service teardown cannot clear global capture ownership belonging to another active owner.
- Image conversion now rewinds the ImageReader ByteBuffer and validates available bytes before bitmap copy.
- `RGBA_8888` input requires the expected 4-byte pixel stride and complete-pixel row padding; invalid OEM/provider layouts fail cleanly instead of risking corrupt copy semantics.
- Image/Bitmap release ordering is tightened so the acquired Image is closed before final success/failure cleanup completes.
- Conversion/provider and allocation failures are contained by the capture session recovery path.
- User-facing capture recovery errors are localized in English and Croatian instead of exposing raw provider exception messages.
- `CaptureBufferLayoutTest` adds regression coverage for unexpected pixel stride and partial-pixel row padding.

### Windows reliability

- Local preferences now contain filesystem policy/`SecurityException` failures while loading and best-effort temp cleanup.
- Recent capture discovery also contains filesystem security-policy failures and preserves any valid metadata already collected.
- Temporary PNG cleanup cannot replace the original capture failure when a security policy prevents deleting the staging file.
- Existing v0.0.9 tray protocol, package-integrity and lifecycle hardening remains in place.

### Android CI and public APK signing

- Android CI validates 0.1.0 versionCode/versionName in addition to the privacy/service manifest contract.
- CI runs `lintDebug`, `lintRelease`, JVM tests and debug/release builds, and verifies the debug APK signature, ZIP alignment and SHA-256.
- The published `SNAPVERE.apk` is the validated CI/debug-signed package built from the same 0.1.0 source that also passes release-variant lint/build validation.
- v0.1.0 does not require a private production Android keystore and is not represented as Google Play/production-signed.
- Because a future production signing identity may differ, moving from the v0.1.0 APK to a later production-signed channel may require uninstall/reinstall rather than an in-place update.
- The Android source ZIP is created with `git archive` from the exact validated release commit and structurally checked for expected source files and absence of generated build/cache content.

### v0.1.0 release automation

- A separate Android release job validates the manifest, lints/tests/builds both variants, verifies the CI/debug APK signature and alignment, generates `SNAPVERE-Android-Source.zip` and records transfer SHA-256 values.
- The Windows release job depends on successful Android validation, then re-runs audited Windows builds/tests, architecture payload integrity and x64/x86 Setup+Portable lifecycle/tray-first checks; real rendered WinUI QA remains a required normal-CI gate on the same release commit.
- Android assets are re-hashed after Actions artifact transfer before being admitted into the final release directory.
- The exact four-file public contract is enforced before the immutable tag is created.
- Post-publication verification checks exact asset names and compares GitHub's published SHA-256 digest for every asset with the locally validated value.

## [0.0.9] - 2026-09-11

### Windows capture, save and UX

- Region, Window and Full Screen capture can open the native Windows **Save As** picker after a PNG is safely created in the default SNAPVERE capture location.
- Save relocation uses asynchronous copy, destination-local staging, atomic replacement, PNG-only destination validation and Windows file-identity checks; cancellation preserves the already-completed PNG.
- About exposes the official SNAPVERE site, `info@snapvere.com`, Privacy, Terms and Brendigo destinations. Support falls back to copying the address when Windows has no mail client.
- The native notification-area host negotiates `NOTIFYICON_VERSION_4` after every icon add/re-add, preserves the tooltip through `NIF_SHOWTIP`, decodes the version-4 low-word callback event and supports keyboard select/context-menu activation.
- Region Capture remains Print Screen / `Ctrl+Shift+1`, Window Capture `Ctrl+Shift+2`, and Screen Capture `Ctrl+Shift+3`. Scrolling Capture does not reserve a shortcut until implemented.

### Windows package security and reliability

- NuGet audit is enabled for direct and transitive packages at `low` severity and above; `NU1901`–`NU1904` are build failures.
- GitHub Actions used by build/release automation are pinned to full commit SHAs; ordinary CI checkout does not persist repository credentials.
- Setup path-boundary logic correctly handles a protected directory itself, descendants, parents and sibling-prefix cases.
- Embedded ZIP extraction uses bounded random `.snapvere-<guid>.tmp` staging names, rejects duplicate destinations and retains absolute-path, traversal, entry-count and expanded-size protections.
- Portable reusable `%TEMP%` payload caches are verified against trusted architecture-specific SHA-256 manifests embedded in the Portable host. Missing, modified, unexpected or reparse-point content is rejected, transactionally rebuilt and verified again before launch.
- An initial direct-ZIP cache comparison design was rejected by lifecycle CI because redundant decompression could exceed the unchanged 60-second startup probe. Runtime verification was corrected to one sequential SHA-256 pass over cached files against the embedded manifest rather than weakening the timeout.
- Recent-capture enumeration removes a redundant per-file metadata refresh while preserving expected I/O/access-race handling without a resident watcher/cache.

### Android application

- Android 10+ native full-screen capture remains based on explicit, fresh MediaProjection consent for every capture; consent tokens are not cached or reused.
- A five-second Activity-hide guard and seven-second first-frame guard bound capture ownership so a stalled VirtualDisplay/ImageReader path cannot leave the foreground service or capture lock active indefinitely.
- MediaProjection, VirtualDisplay, ImageReader, acquired Image, callbacks and HandlerThread cleanup is exception-safe per resource and idempotent.
- `ImageReader.acquireLatestImage()` and MediaStore cleanup paths are guarded against framework/provider runtime failures.
- `CaptureBufferLayout` validates capture width, pixel stride, row stride and integer arithmetic before padded-bitmap allocation; JVM unit tests cover tight, padded, invalid and overflow cases.
- Capture, Open, Share, Delete, Website, Support, Privacy and Terms actions contain controlled error paths instead of allowing system/provider/intent failures to terminate the Activity.
- Latest-capture actions revalidate the MediaStore URI and clear stale local metadata when the image no longer exists.
- Paired actions stack vertically on narrow screens or at 1.25x+ font scale; buttons retain at least 52 dp touch height; system insets and scrolling remain supported.
- OEM `forceDark` is disabled because SNAPVERE already supplies a deliberate dark palette.
- English and Croatian recovery/status resources are updated together.
- The Android manifest contains no `INTERNET` permission, cleartext remains disabled, backup remains disabled and the MediaProjection service remains non-exported.

### Android CI and evidence

- Android CI validates the privacy/service manifest contract, `lintDebug`, `lintRelease`, `testDebugUnitTest`, debug and minified/shrunk release builds, APK signature, ZIP alignment and SHA-256 artifact generation.
- The Actions APK is debug-signed development/internal evidence, not a fabricated production Play Store signature.
- A green Android CI is build/lint/unit/package evidence and is not represented as physical runtime coverage of every OEM/device combination.

### Visual QA

- CI captures six real rendered Windows surfaces: Region, Window, Tray, Options, Language and About.
- Visual-QA provenance was hardened after a hosted-runner desktop could previously become a successful-main Region baseline.
- PR visual comparisons use a clean successful-main baseline; thresholds are not lowered to hide regressions.

### Release automation

- The v0.0.9 release workflow builds/validates x86, x64 and ARM64 payloads plus integrity manifests, produces exactly two universal public Windows executables, runs x64/x86 Setup+Portable lifecycle/tray-first checks, creates an immutable tag and verifies published GitHub asset SHA-256 digests.
- The first v0.0.9 publication attempt stopped before publication because missing-tag detection relied on PowerShell exception behavior while `gh api` returned a nonzero HTTP 404 result instead. No v0.0.9 GitHub Release was published by that attempt.
- Tag lookup was corrected fail-closed before the successful release.

## [0.0.8] - 2026-09-10

- Expanded capture-surface localization and dedicated Croatian strings for Region, Window, Options, Recent Captures, Language and About.
- English remained the canonical fallback for incomplete translation entries.
- Historical workflows were moved under `.github/release-archive/`; the v0.0.8 workflow used a dedicated main-branch trigger.
- Continued the two-universal-download x86/x64/ARM64 packaging contract.
- Release publication remained gated by x64 build/tests, x86 build, ARM64 cross-build, payload validation and x64/x86 lifecycle probes.

## [0.0.7] - 2026-09-10

- Introduced universal Setup and Portable hosts embedding x86, x64 and ARM64 payloads.
- Reduced public downloads to exactly `SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe`.
- Added a 28-language built-in selector with English canonical fallback and Croatian support.
- Added the SNAPVERE Commercial Software License Agreement for v0.0.7 and later.
- Preserved local settings, atomic writes, event-driven tray/hotkey behavior and package lifecycle validation.

## [0.0.6] - 2026-09-10

- Restored one consistent tray-first launch contract for normal, startup and Portable execution.
- Redesigned tray quick actions, Region/Window capture chrome, Options/Recent Captures, About and Setup/Uninstall surfaces.
- Added installed and Portable secondary-UI materialization probes.
- Applied the persisted **Include cursor on capture** setting to Region, Window and Screen workflows.
- Preserved same-Setup uninstall validation and constrained installation cleanup.

## [0.0.5] - 2026-09-10

- Temporarily activated Capture Center on manual launch while Windows startup used a background path.
- Added automated Setup UI materialization probing.
- Superseded by v0.0.6, which restored the consistent tray-first launch contract.

## [0.0.4] - 2026-09-10

- Introduced the production tray-first model with left-click Region Capture and branded WinUI right-click quick actions.
- Added real Options/Recent Captures, Language action, local preferences, Windows startup registration and include-cursor behavior.
- Added tray-first and secondary UI runtime probes and maintained SVG workflow references.
- Added startup-registration cleanup validation and preserved safe Setup/Portable lifecycle behavior.

## [0.0.3] - 2026-09-10

- Added production Window Capture using Windows.Graphics.Capture `CreateForWindow`, native top-level-window discovery and multi-monitor DPI-aware picker overlays.
- Added `Ctrl+Shift+2` Window Capture, tray integration and package runtime probes.
- Centralized package lifecycle validation and strengthened install/uninstall contract checks.

## [0.0.2] - 2026-09-09

- Stabilized WinUI startup through a conservative programmatic runtime coordinator.
- Added startup diagnostics, hidden-runtime `READY` probe and x64/x86 Setup/Portable lifecycle survival checks.
- Moved CI runtime validation to supported Windows Server 2022 for Windows App SDK 1.8.

## [0.0.1] - 2026-09-09

- Established the .NET 10 / WinUI 3 solution, capture domain, DPI/geometry model, GDI capture compatibility backend, PNG encoder and local capture persistence.
- Added Region and Screen workflows, global hotkeys, native tray integration, Recent Captures, x64/x86 packaging, per-user Setup and Portable launchers.
- Added guarded ZIP extraction, package security tests, installer lifecycle checks, SHA-256 release metadata, branding and core architecture/security documentation.
