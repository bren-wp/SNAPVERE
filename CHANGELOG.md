# Changelog

All notable SNAPVERE changes are documented here. Published release tags and assets are immutable; later documentation may clarify evidence boundaries but does not rewrite historical binaries.

## [Unreleased]

### Window picker failure fidelity

- Stop treating a Window Capture overlay-render failure as an ordinary user cancellation.
- Propagate render failures through the picker completion task so all monitor overlays still close in the existing `finally` path and the capture coordinator surfaces the existing localized Window Capture failure feedback.
- Preserve genuine Escape/window-close cancellation as a null selection rather than conflating it with technical failures.

### Recording callback teardown hardening

- Replace the disposable recording-frame semaphore with a generation-based async pulse signal so Stop/failure/dispose cannot race `SemaphoreSlim.Dispose()` against a pending `WaitAsync()`.
- Wake every waiter from the current recording generation during teardown while keeping later frame waits on a fresh unsignaled generation.
- Synchronize the first recording timestamp and failure publication across MediaStreamSource callbacks.
- Contain deferral-completion failures inside the recording failure path instead of allowing an `async void` callback exception to escape process-level handling.
- Add focused concurrency regression tests for multi-waiter wakeup, fresh generations and repeated pulse/wait cycles.

### Deferred shutdown integrity

- Make an Exit request sticky while any capture is active so no new capture can start after shutdown has already been requested.
- When Exit is requested during screen recording, request the existing graceful recording stop instead of leaving the user to stop recording manually.
- Continue the deferred shutdown automatically after active capture cleanup completes, without disposing capture services underneath an in-flight workflow.
- Refresh EN/HR shutdown feedback and tray-lifecycle documentation, with regression coverage for the sticky lifecycle gate.

## [0.1.12] - 2026-09-20

### Responsive completion

- Make Setup release its normal minimum size when the active work area is smaller, then reflow license, path, shortcut, status and action controls without forcing horizontal overflow.
- Prevent the post-install Launch option from overlapping Finish on compact Setup layouts and keep uninstall messaging/cards width-responsive.
- Add tray compact reflow that collapses shortcut hints and reduces branding/padding when the work area forces a narrow flyout.
- Add extra 280 px popup and 420/320 px Options breakpoints with stacked navigation/actions and long-copy wrapping across Chrome, Edge, Opera and Firefox.
- Make Setup text widths follow the compact-layout policy so long localized copy cannot keep desktop widths after the rest of the installer has reflowed.
- Extend Windows and browser responsive regression contracts so these narrow-layout behaviors are enforced by CI.

## [0.1.11] - 2026-09-19

### Responsive UI hardening

- Clamp DPI-scaled Windows tray, Settings, Language, About and capture-feedback windows to the active monitor work area instead of allowing high-DPI sizes to exceed the usable screen.
- Add a pure work-area sizing policy with regression tests for normal, clamped, tiny and invalid geometry.
- Make tray actions vertically scrollable when the work area cannot fit the full command surface.
- Make Settings preferences vertically scrollable and move trailing controls below card copy on narrow windows.
- Make About shortcut copy wrap and capture-feedback messages scroll on constrained heights.
- Improve Setup narrow-window behavior by shrinking the content card/path controls and reducing the minimum window footprint while retaining AutoScroll and lifecycle behavior.
- Remove the browser popup's fixed 340 px desktop minimum, add a 280 px safe floor plus a 320 px compact breakpoint, and enforce responsive popup/options contracts across Chrome, Edge, Opera and Firefox.


## [0.1.10] - 2026-09-19

### Local screen recording

- Add real local-first primary-display screen recording for Windows using Windows.Graphics.Capture and H.264 MP4 output.
- Add tray Start/Stop recording controls, active recording status and cursor-preference integration.
- Keep the initial recording mode video-only; microphone and system audio are not claimed as supported.
- Bound recording policy to 30 FPS, 4–32 Mbps and source displays up to 7680x4320, with even encoded dimensions.
- Keep frame retention bounded to the latest pending frame plus encoder-owned in-flight frames.
- Publish MP4 files atomically through SNAPVERE-owned temporary files with collision-safe final names and stale-temp cleanup.
- Reject zero-frame or zero-byte recording output so rapid Start/Stop cannot publish an empty recording as success.
- Include completed MP4 recordings in Recent captures while rejecting unrelated file extensions.
- Add unit coverage for encoding policy, atomic recording publication, collisions, failure cleanup, completion validation and history integration.


## [0.1.9] - 2026-09-19

### Capture-safe shutdown lifecycle

- Prevent tray Exit from disposing the application service graph while a Region, Window or Screen capture is still active.
- Serialize capture start and shutdown start through a thread-safe lifecycle gate so a shutdown request cannot race with a new capture.
- Show a clear EN/HR status message when Exit is requested during an active capture instead of silently tearing down capture dependencies.
- Add regression coverage proving active capture blocks shutdown until completion and that shutdown blocks later capture starts.


## [0.1.8] - 2026-09-19

### Startup diagnostics UI privacy

- Stop exposing the full local diagnostics-log path in the fatal startup dialog.
- Keep exception type, message and stack details confined to the bounded local diagnostics log.
- Centralize the startup failure copy in the shared layer so it can be regression-tested without invoking Win32 UI.
- Add unit coverage that rejects filesystem-looking paths and exception/stack wording in the user-facing startup failure message.


## [0.1.7] - 2026-09-19

### Capture persistence failure recovery

- Classify expected local PNG persistence failures as access denied, storage full or generic write failure instead of letting save errors look like capture-engine failures.
- Preserve the original technical exception only as an inner diagnostic detail while Region, Window and Screen surfaces show stable localized recovery copy.
- Keep cancellation, capture-engine/programming failures, staged temp writes, atomic publication, filename-collision retry and stale-temp cleanup behavior unchanged.
- Add regression coverage for Win32 disk-full codes, access/security failures, generic I/O failures, sanitized wrapper messages, invalid save targets and EN/HR fallback copy.

### Browser active-tab side-effect ownership

- Bind capture ownership to the last-focused browser window as well as the initiating tab/window IDs, aborting before page mutation when focus moves to another browser window.
- Revalidate the initiating tab before Region/Full Page content-script injection and again before page-mutating Region start, Full Page preparation, scroll, floating-element hiding and final assembly actions.
- Add token-scoped `REGION_CLEANUP` so a tab switch detected immediately after Region overlay creation removes only the overlay owned by that capture session before releasing its lock.
- Expand shared behavioral smoke tests to prove early tab switches prevent injection/side effects, Region races clean up the old overlay, Full Page races do not scroll the stale tab, and all four browser variants retain source parity.

### Explorer tray recovery

- Replace the one-shot `TaskbarCreated` tray-icon restore attempt with a bounded non-blocking retry schedule so short Explorer notification-area races do not leave a healthy SNAPVERE process without its icon.
- Keep retries on the native tray message loop through a one-shot timer/post-message handoff instead of sleeping or blocking tray input.
- Invalidate stale timer callbacks with a generation token so a callback from an older recovery cycle cannot re-add or disturb an icon after a later recovery already succeeded.
- Add unit coverage for the bounded 250 ms / 500 ms / 1 s / 2 s retry policy and hard stop after five total attempts.

### Local shell error containment

- Centralize the expected Windows local-file/folder shell failure taxonomy so tray and Options actions no longer drift in which policy, permission and shell-handler failures they contain.
- Contain `SecurityException` and `InvalidOperationException` from tray **Open capture folder** alongside the existing I/O/access/Win32 failures instead of allowing an expected local Windows restriction to escape the UI callback.
- Add unit coverage for the bounded expected taxonomy while leaving unrelated programming exceptions outside the containment policy.

### Second-launch activation

- Replace silent duplicate-launch dismissal with a per-user activation signal that wakes the existing tray-first instance and surfaces Preferences without creating another app process.
- Make the Portable duplicate fast path signal the same existing instance before returning, preserving the no-rehash/no-reextract optimization.
- Extend x64/x86 installed and Portable lifecycle validation so a second launch must prove both single-process integrity and delivery of the activation request.
- Refresh active EN/HR tray-lifecycle guidance and remove the stale 0.1.2 document heading.

### Portable startup error containment

- Stop rendering raw Portable launcher exception messages in user-visible dialogs.
- Keep technical exception details in the local bounded startup diagnostic log while returning stable, actionable error categories to the user.
- Add Product Contract and unit-test regression coverage that rejects direct `exception.Message` exposure and verifies sensitive local paths stay out of Portable failure dialogs.
- Refresh EN/HR troubleshooting documentation and remove stale version-specific 0.1.2 headings from active guidance.

### Capture persistence cleanup

- Remove only stale SNAPVERE-owned atomic-write temp files before a new capture, using an ownership-shaped filename check and a 24-hour age threshold.
- Preserve recent/in-flight temp files and similarly named non-SNAPVERE files, and keep cleanup best-effort so a locked stale temp cannot block a new capture.
- Add unit coverage for stale/recent/foreign/locked temp-file boundaries.

### Windows install-target ownership

- Refuse to replace an existing non-empty install target unless it is empty or carries the exact SNAPVERE installation marker together with SNAPVERE application files.
- Revalidate target ownership immediately before directory replacement so payload extraction does not leave a broad replacement race window.
- Add regression coverage for missing, empty, owned, unowned, invalid-marker and marker-only target directories.

### Windows uninstall recoverability

- Keep Windows Installed Apps metadata, startup registration and SNAPVERE-owned shortcuts intact until installation-directory deletion has actually succeeded.
- Move registration/shortcut cleanup into successful deferred cleanup instead of unregistering the product before the maintenance process removes files.
- Stop creating the SNAPVERE Start menu directory as a side effect of uninstall path lookup and remove the product folder when it becomes empty.
- Add a real package-lifecycle regression probe that intentionally locks an installed file, requires uninstall failure, verifies registration metadata is preserved, repairs the installation and then completes a normal uninstall.
- Reject malformed deferred-cleanup requests without a positive parent PID and serialize maintenance cleanup with the same Setup mutex used by normal install/uninstall operations.
- Add lifecycle coverage proving invalid cleanup requests return `87` and cleanup loses safely with `1618` when another Setup operation owns the mutex.

## [0.1.6] - 2026-09-18

### Browser message ownership and error containment

- Separate extension-page capture commands from tab-owned capture-session callbacks in the browser background runtime.
- Reject `CAPTURE_VISIBLE`, `CAPTURE_REGION` and `CAPTURE_FULL` when they originate from an injected tab script or another extension identity.
- Preserve region token/tab/window ownership checks and stop returning raw internal error messages in background message responses.
- Add cross-browser behavioral regression coverage for wrong-sender command attempts while preserving byte-identical shared runtime files.

### Setup security, lifecycle and accessibility

- Require an exact installation-marker header during uninstall validation instead of accepting arbitrary strings that merely start with the trusted marker prefix.
- Reject installer paths whose existing directory chain traverses a symbolic link or reparse-point directory before Setup mutates the target.
- Sanitize silent/deferred install and uninstall failures instead of returning raw exception messages.
- Fail closed when Setup cannot verify a same-installation running SNAPVERE process.
- Prevent closing the interactive wizard while an active file operation is still completing, and make progress callbacks safe against UI disposal.
- Avoid duplicate `SNAPVERE\\SNAPVERE` folder selection, improve keyboard/accessibility metadata and position the wizard on the active monitor with scrollable narrow-screen fallback.
- Add regression tests for marker matching, install-folder normalization and normal directory-chain validation.
- Refresh EN/HR installation documentation, including the existing silent install/uninstall contract.

## [0.1.5] - 2026-09-18

### Reliability and concurrency

- Make Windows PNG publication collision-safe when simultaneous captures share the same timestamp by allocating the visible filename at the atomic move boundary and retrying deterministic suffixes without re-encoding.
- Make browser capture-lock mutation owner-safe so stale cleanup cannot remove a newer capture lock.
- Prevent stale asynchronous Recent-capture searches from overwriting newer browser UI state.
- Guard repeated Open, Open downloads folder and Settings-save activation while the corresponding browser action is in flight or cooling down.

### Browser correctness, permissions and UI

- Add the scoped `downloads.open` permission required by the existing user-triggered Recent > Open action, while retaining no broad host permissions or `<all_urls>` access.
- Match `downloads.showDefaultFolder()` to its actual no-callback API shape and keep duplicate folder-open activation bounded.
- Improve narrow-layout behavior, disabled states, metadata truncation and `prefers-reduced-motion` handling consistently across Chrome, Edge, Opera and Firefox.
- Move the macOS Full Page default from system-reserved `Command+Shift+3` to `Command+Shift+7`, while keeping Windows/Linux `Ctrl+Shift+3` unchanged.
- Expand behavioral regression coverage for Recent ordering, duplicate actions and capture-lock interleavings.

### Windows UX and resilience

- Contain expected local security/shell/filesystem failures in Settings/Recent instead of allowing them to escape UI handlers.
- Keep language selection consistent with the actually persisted preference after a failed write and announce status changes through accessibility live regions.
- Harden About link/clipboard fallback handling and treat missing shell handlers as controlled failures.
- Clamp the tray flyout fallback to the Windows virtual desktop when monitor work-area lookup is unavailable.
- Truncate unusually long Recent filenames/metadata without losing the complete filename tooltip.

### Documentation

- Align browser permission, security, QA and contribution documentation with the implemented post-v0.1.4 behavior.
- Remove stale active-document version labels while preserving immutable historical release records.

## [0.1.4] - 2026-09-18

### CI and release reliability

- Derive the Windows package/lifecycle version from the canonical `product-version.json` contract instead of a hand-maintained CI constant.
- Fail Product Contract CI if standard CI reintroduces a hardcoded release version or if the active release workflow/trigger drifts from the canonical version.
- Archive the completed v0.1.3 release workflow and add the controlled v0.1.4 publication workflow.

### Version and documentation alignment

- Move Windows product/assembly/file versions and Chrome, Edge, Opera and Firefox package versions to 0.1.4 together.
- Correct stale versioning-document title/examples and align current EN/HR product documentation and release links.
- Preserve the exact six-package release contract and existing local-first permission/privacy boundaries.

## [0.1.3] - 2026-09-17

### Windows UX and recovery

- Wire tray Settings and Recent captures into production behavior.
- Surface sanitized user-facing feedback for busy, unsupported and failed Region/Window/Screen capture actions without exposing raw exceptions.

### Browser UX, accessibility and capture reliability

- Add Settings/Recent captures parity, Open/Refresh/Open-downloads-folder actions and stale deleted/moved download filtering across Chrome, Edge, Opera and Firefox.
- Add keyboard-accessible ARIA tab navigation for Settings/Recent panels.
- Add functional Region/Visible/Full Page keyboard commands that reuse the existing serialized capture/lock pipeline.
- Add localized action badge/title feedback for hotkey-triggered busy, unsupported-page, active-tab-changed, full-page-too-large and generic capture failures.
- Harden popup localization-key validation and Settings-opening failure feedback.

### Validation and packaging

- Expand behavioral command/failure-feedback coverage and lock command IDs, shortcut mappings and EN/HR localization references in CI.
- Preserve the exact six-package Windows/browser release contract, deterministic browser packaging and local-first permission/privacy boundaries.

## [0.1.2] - 2026-09-16

### Active product boundary

- The maintained product surface is Windows plus Chrome, Edge, Opera and Firefox.
- The retired mobile application, its CI/tooling and active product-contract entries were removed from the maintained source tree. Historical release records remain unchanged.
- `product-version.json` defines exactly six maintained release packages: Windows Setup/Portable and four browser ZIPs.

### Windows reliability, performance and user-facing cleanup

- Hardened GDI fallback bitmap readback lifecycle and reparse-point-safe embedded payload extraction.
- Added per-user single-instance handling and fast duplicate Portable launch rejection before expensive payload validation.
- Moved PNG compression off the WinUI thread and added a final cancellation check immediately before atomic publication.
- Reduced idle startup work by deferring capture-service creation until capture is requested.
- Reduced Region and Window Capture peak memory by removing redundant full-frame staging buffers and releasing frozen monitor frames earlier.
- Sanitized interactive Setup failures so raw technical exception details are not exposed in user-facing error copy.
- Removed raw Region Capture exception details from the UI while retaining bounded local diagnostics for technical recovery evidence.
- Removed the dead CaptureHistory delete API/surface and stale localization/development-only copy that no longer represented product behavior.
- Corrected hotkey wording and removed no-op preference writes so unchanged settings do not trigger unnecessary persistence work.
- Cleaned Settings, About, Tray and Language user-facing copy so internal/technical implementation text does not leak into normal UI.

### Browser reliability and privacy

- Revalidate the initiating tab immediately before and after `captureVisibleTab` so tab switches cannot feed another tab's pixels into the capture workflow.
- Revalidate full-page session ownership after asynchronous canvas encoding and before download publication.
- Surface region-capture failures in-page after the popup closes.
- Lock the visible product identity and saved filename prefix to SNAPVERE, while keeping permissions limited to `activeTab`, `scripting`, `downloads` and `storage`.
- Stitch full-page captures incrementally into one bounded canvas and release decoded tile resources promptly.

### CI, security and packaging

- Product Contract CI enforces the active Windows/browser contract, version alignment, exact package names and documentation links.
- CodeQL workflow actions are pinned and run with reduced permissions, concurrency control and bounded execution.
- Windows CI enforces real x64/x86 Setup and Portable lifecycle completion rather than relying on ambiguous PowerShell exit-state behavior.
- Added universal package-size regression budgets and retained x86, x64 and ARM64 payload validation plus rendered WinUI visual QA.
- Release preparation preserves immutable-tag semantics, exact package contracts and SHA-256 verification rather than weakening gates for publication.

### Documentation and branding

- Rebuilt the English and Croatian READMEs as product landing pages with adaptive SNAPVERE branding, live CI badges, feature/download sections and evidence-based privacy/security language.
- Added real Windows UI screenshots produced by the validated visual-QA pipeline rather than marketing mockups.
- Added English/Croatian Performance & Stability documentation and expanded branding, product-status and QA evidence documentation.
- `RELEASES.md` is the canonical detailed release-history source; no version-specific root release-notes file is introduced for 0.1.2.

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
