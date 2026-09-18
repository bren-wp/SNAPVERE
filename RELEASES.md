# SNAPVERE Releases

This file is the **canonical detailed release history and release-notes source for SNAPVERE**.

- Current public release: **v0.1.4**
- Current release page: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.4
- Machine-readable active version contract: [`product-version.json`](product-version.json)
- Concise engineering change history: [`CHANGELOG.md`](CHANGELOG.md)

## Release-document policy

Starting with the post-v0.1.1 maintenance line, SNAPVERE keeps detailed release notes in this file instead of creating a new root `RELEASE_NOTES_<version>.md` for every release.

For every future release:

1. add the new version section **above** the previous release section;
2. update `product-version.json` and all platform version sources together;
3. update `CHANGELOG.md` with the concise engineering summary;
4. update current EN/HR product documentation and download links;
5. validate the release with Product Contract CI plus the platform-specific CI/release gates;
6. publish a new immutable tag/release instead of rewriting an older release.

Historical sections below intentionally describe what was true **at the time of that version**. Old shortcuts, package layouts, licensing terms, signing status, supported features and known limitations are historical facts and must not be silently rewritten to match the current product.

Published GitHub tags, release descriptions and binary assets remain immutable historical artifacts. Consolidating repository documentation does **not** alter an already-published release.

---

## Unreleased

No post-v0.1.4 release changes are documented yet.

---

## v0.1.4 — 2026-09-18

SNAPVERE 0.1.4 is a maintenance release focused on **release correctness and version-contract reliability** for the maintained Windows + Chrome/Edge/Opera/Firefox product line. Capture behavior, local-first privacy boundaries and the exact six-package public contract remain unchanged from 0.1.3.

### CI and version-contract hardening

- Standard Windows CI no longer carries a hand-maintained hardcoded `SNAPVERE_VERSION`; the package/lifecycle job resolves the version from the canonical `product-version.json` contract and checks Windows/browser version parity before building public package candidates.
- Product Contract CI now rejects a reintroduced hardcoded standard-CI version and requires the active version-specific release workflow, trigger path and release environment to match the canonical product contract.
- This removes the class of stale CI-version mismatch that could cause otherwise-correct Setup/Portable lifecycle validation to test against the previous release number.

### Release automation and documentation

- The successful v0.1.3 publication workflow is archived as historical release automation and v0.1.4 receives its own main-only controlled publication workflow.
- Versioning documentation is corrected so its title, current-release examples and development-channel example all describe the active release instead of retaining stale 0.1.2 wording.
- Windows assembly/product/file versions and all four browser manifest/store-package versions move together to 0.1.4.

### Public packages

The v0.1.4 GitHub Release contains exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

Publication remains gated by audited Windows builds/tests, x86/x64 lifecycle and tray-first validation, ARM64 cross-build evidence, deterministic browser packaging, exact package-name enforcement and SHA-256 verification before and after GitHub Release publication. No new browser permissions, telemetry, analytics, cloud upload or tracking are introduced.

---

## v0.1.3 — 2026-09-17

SNAPVERE 0.1.3 promotes the post-v0.1.2 reliability and UX work into the public **Windows + Chrome/Edge/Opera/Firefox** release line while preserving the local-first privacy model and exact six-package release contract.

### Windows UX and failure recovery

- Tray **Settings** and **Recent captures** are fully wired instead of presentation-only surfaces.
- User-triggered Region, Window and Screen capture failures now receive sanitized feedback for busy, unsupported and capture-failure states without exposing raw exception text.
- Existing tray-first startup, capture-folder access, language switching, About, Exit, global shortcuts, x86/x64 payloads and ARM64 build coverage remain intact.

### Browser capture parity and accessibility

- Chrome, Edge, Opera and Firefox now expose Settings and Recent captures with the same EN/HR UX, including Open, Refresh and **Open downloads folder** actions using the existing downloads permission.
- Recent-capture views ignore completed download records whose local file no longer exists, avoiding stale **Open** actions after a file has been removed or moved.
- Settings/Recent navigation follows the ARIA keyboard tab pattern with roving `tabIndex`, Arrow Left/Right, Home and End behavior.
- Browser capture hotkeys are now functional: `Ctrl+Shift+1` Region, `Ctrl+Shift+2` Visible Area and `Ctrl+Shift+3` Full Page, with Command+Shift equivalents on macOS. They reuse the existing serialized capture pipeline and capture lock rather than introducing a second implementation.
- Hotkey-triggered capture failures now surface localized, non-invasive action badge/title feedback for capture busy, unsupported pages, active-tab changes, oversized full-page captures and generic region/capture failures. Raw exceptions and stack traces remain internal.
- Opening browser Settings provides progress and localized failure feedback when the options page cannot be opened.

### CI and contract hardening

- Behavioral browser tests activate real command handlers and cover visible, region-lock/cleanup, full-page completion, unknown-command no-op and localized hotkey-failure feedback paths.
- Extension validation locks the expected command IDs, default/macOS shortcuts and localization keys, and also catches popup localization-reference drift.
- Chrome, Edge, Opera and Firefox shared source parity remains enforced; Firefox retains only required Gecko differences.
- No new extension permission, broad host access, telemetry, analytics, cloud upload, tracking or remote runtime dependency is introduced.

### Public packages

The v0.1.3 GitHub Release contains exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

Publication re-runs the audited Windows/browser build gates, validates x64/x86 lifecycle completion, verifies deterministic browser ZIPs and checks SHA-256 digests before and after GitHub Release publication. Browser-store publication remains a separate external publisher/reviewer process.

---

## v0.1.2 — 2026-09-16

SNAPVERE 0.1.2 consolidates the post-v0.1.1 maintenance line into the maintained **Windows + browser extensions** product boundary. Historical Android facts in v0.1.1 and earlier sections remain unchanged.

### Active product boundary and release contract

- The maintained product surface is Windows plus Chrome, Edge, Opera and Firefox.
- `product-version.json` declares only `windows` and `browsers` as supported platforms.
- The public release contract contains exactly six packages:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

- Browser store publication remains an external publisher/reviewer process. Repository CI prepares and validates the packages and store-submission metadata but does not claim Chrome Web Store, Microsoft Edge Add-ons, Opera Add-ons or Mozilla Add-ons publication without external confirmation.

### Windows reliability, security and performance hardening

- Corrected the GDI fallback readback lifecycle by restoring the original DC bitmap before `GetDIBits`, improving correctness and resource cleanup.
- Hardened embedded payload extraction against destination reparse-point/junction escapes in addition to lexical ZIP traversal checks.
- Added a per-user desktop single-instance guard and disposed the temporary Windows identity/token wrapper after SID lookup.
- Portable duplicate launch now probes the same single-instance mutex before expensive cache verification/extraction while the child process remains the final race-safe owner.
- CPU-heavy PNG conversion/compression now runs off the WinUI thread; cancellation is checked again immediately before the final atomic file move.
- Normal tray startup defers capture-service construction until the user requests a capture, reducing idle startup work.
- Region Capture writes frozen BGRA rows directly into the destination bitmap, avoids an extra clipboard `ToArray()` copy and avoids cloning draft annotation points on every pointer move.
- Window Capture writes frozen monitor data directly into overlay bitmaps and releases raw frozen frames as soon as each overlay has loaded them, reducing multi-monitor peak memory.
- Setup interactive error handling no longer exposes raw technical exception details to users.
- Region Capture no longer exposes raw `InvalidOperationException.Message`; technical details are retained only in a bounded local diagnostic log while the user receives stable localized recovery copy.
- No-op preference writes are avoided, stale localization/development copy was removed, hotkey wording was corrected and the dead CaptureHistory delete surface was removed.

### Browser state-machine, privacy and memory hardening

- Chrome, Edge, Opera and Firefox revalidate the initiating tab/window immediately before and after every `captureVisibleTab` call, preventing a tab-switch race from admitting pixels from another active tab.
- Full-page assembly revalidates the capture session after asynchronous canvas encoding and before creating a download.
- Region-capture failures are surfaced through a transient accessible in-page status after the popup has closed instead of failing silently.
- The visible product name, wordmark and saved filename prefix are locked to **SNAPVERE**; legacy custom filename-prefix values are ignored.
- Full-page stitching draws decoded tiles incrementally into one bounded destination canvas and releases image resources promptly instead of retaining an image set for the full capture.
- The permission contract remains exactly `activeTab`, `scripting`, `downloads` and `storage`, without `<all_urls>` or broad host permissions.

### CI, security and release evidence hardening

- Product Contract CI validates the Windows/browser platform boundary, current version, exact six-package contract, brand/file-prefix lock, store status and active documentation links.
- CodeQL checkout/init/analyze actions are pinned to audited commit SHAs; workflow credentials/permissions, concurrency and time bounds are tightened.
- Windows package CI records explicit architecture completion markers so x64 and x86 Setup/Portable lifecycle scripts must both actually finish successfully.
- Universal package-size regression budgets guard application payloads and public Setup/Portable executables.
- Existing x64 tests, x86 build, ARM64 cross-build, native payload validation, rendered WinUI visual QA and Setup/Portable lifecycle gates remain required.
- Release publication uses immutable-tag protection, transfer SHA validation, exact asset-name validation and post-publication digest verification; published tags and historical releases are never force-moved or rewritten.

### Documentation, branding and real product imagery

- Rebuilt the English and Croatian root READMEs as product landing pages with adaptive SNAPVERE dark/light logos, live CI badges, concise value proposition, capture shortcuts, download matrix, privacy/security positioning and documentation navigation.
- Added real tray, Settings and Window Capture screenshots generated by the validated Windows visual-QA pipeline. These are product renders, not design mockups.
- Added English/Croatian Performance & Stability documentation covering idle-runtime and Region/Window memory hardening.
- Expanded Branding, Product Status, QA Matrix and documentation indexes so marketing claims remain separated from automated evidence and external publication/signing status.
- `RELEASES.md` remains the canonical detailed release-history source; no new root `RELEASE_NOTES_<version>.md` file is introduced.

---

## v0.1.1 — 2026-09-12

SNAPVERE 0.1.1 promoted the browser-extension work completed after 0.1.0 into the public release line while preserving the validated Windows and Android applications.

### Browser extensions

The release added public source-ready browser packages for:

- Google Chrome;
- Microsoft Edge;
- Opera;
- Mozilla Firefox.

All four variants provide user-initiated capture of the visible viewport, bounded full-page capture through local scrolling/stitching and rectangular region capture. Screenshot pixels are processed locally in the browser and saved as PNG files. The extension source contains no telemetry, analytics, advertising SDK, cloud-upload client or remote runtime dependency.

Chromium-family builds use Manifest V3 service workers. Firefox uses a compatible Manifest V3 WebExtension background-script model. All variants request only `activeTab`, `scripting`, `downloads` and `storage`, with no `<all_urls>` or broad host permission. English and Croatian extension interfaces are included.

### Browser package integrity and store readiness

The 0.1.1 browser release path requires:

- manifest, permission and privacy validation;
- Chrome/Edge/Opera/Firefox runtime-source parity checks;
- JavaScript syntax validation;
- deterministic ZIP packaging performed twice;
- byte-for-byte package reproducibility;
- package-content validation and SHA-256 verification;
- store listing metadata, permission justifications and privacy declarations validated against the actual manifests;
- deterministic store screenshot/promo generation for submission preparation.

Store publication remains a separate external process requiring authenticated publisher accounts, review/certification and, where applicable, store signing. The v0.1.1 GitHub Release does **not** claim Chrome Web Store, Microsoft Edge Add-ons, Opera Add-ons or Mozilla Add-ons approval.

### Windows

- Product/file/assembly version moved to 0.1.1 / 0.1.1.0.
- Tray-first Windows behavior, Region/Window/Screen capture, local PNG workflow and universal x86/x64/ARM64 packaging remained intact.
- NuGet audit, analyzers/warnings-as-errors, architecture payload integrity manifests, rendered UI QA and x64/x86 Setup/Portable lifecycle probes remained validation gates.

### Android

- Android version moved to `0.1.1` / `versionCode 11`.
- The local-first MediaProjection workflow kept no `INTERNET` permission, no background continuous recording, backup disabled, cleartext disabled and a non-exported capture service.
- The public `SNAPVERE.apk` continued to use the validated CI/debug signing identity. It is installable but is not represented as Google Play/production-signed.
- Debug/release lint, JVM tests, debug/release builds, APK signature/alignment checks and SHA-256 validation remained release gates.

### Public release files

A valid v0.1.1 GitHub Release contains exactly these eight assets:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

Historical v0.1.0 remains immutable with its original four Windows/Android assets. Browser packages start as public release assets with v0.1.1 and are not retroactively attached to v0.1.0.

### Validation boundary

Automation validated source versions, Windows builds/tests/packages, Android lint/tests/builds/signature/alignment, browser manifest/privacy/parity/reproducibility, exact public asset names and SHA-256 digests before and after GitHub publication.

ARM64 Windows evidence remained cross-build/package evidence on hosted x64 runners. A green pipeline was not represented as exhaustive runtime coverage across every Windows hardware configuration, Android OEM/device, website or browser build.

---

## v0.1.0 — 2026-09-11

SNAPVERE 0.1.0 was the first release line that packaged the validated Windows desktop application and native Android companion together under one release contract.

### Windows

- Kept the tray-first Region, Window and Screen capture workflows.
- Kept universal Windows Setup and Portable hosts embedding x86, x64 and ARM64 application payloads.
- Preserved payload-integrity verification and transactional Portable-cache recovery before execution.
- Hardened local preferences, capture history and temporary-file cleanup against filesystem-policy/security failures so secondary cleanup failures could not replace the original capture result.
- Continued NuGet auditing, analyzer enforcement, real rendered WinUI QA and x64/x86 Setup + Portable lifecycle validation.

### Android

- Version moved to `0.1.0` / `versionCode 10`.
- Every capture remained one explicit user-approved MediaProjection session; there was no background/continuous recording.
- `INTERNET` remained absent, cleartext disabled, backup disabled and the capture service non-exported.
- Foreground-service initialization/provider failures were converted to controlled capture failures.
- Handler scheduling, bounded task-hide/frame-delivery timeouts and capture ownership teardown were hardened.
- Stale service teardown could not clear ownership belonging to another active service instance.
- RGBA pixel stride, row stride, padding alignment and available buffer bytes were validated before bitmap copy.
- ImageReader buffers were rewound before copy and acquired Images were closed before final cleanup.
- Conversion/provider/allocation failures, including `OutOfMemoryError`, were contained by recoverable capture-session handling.
- English and Croatian recovery/status copy and JVM regression coverage were expanded.

### Public release files

A valid v0.1.0 GitHub Release contains exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

The Windows executables are universal x86/x64/ARM64 hosts. The public APK is the validated CI/debug-signed package produced from the same 0.1.0 source that also passes release-variant lint/build checks. It is not represented as Google Play/production-signed. The Android source ZIP is generated from the validated Git tree rather than a dirty working directory.

Because the APK uses the CI/debug signing identity, it does not establish a stable production-signing lineage. A future differently signed Android channel may require uninstall/reinstall.

### Release validation

Before the immutable v0.1.0 tag could be created, automation required audited Windows restore/build/test, x86 and ARM64 builds, architecture payload integrity manifests, rendered WinUI QA, universal Setup/Portable generation, x64/x86 lifecycle/tray-first tests, Android manifest/privacy/version validation, lint, JVM tests, debug/release builds, APK signature/alignment verification, Android source-archive validation and SHA-256 transfer/publication checks.

---

## v0.0.9 — 2026-09-11

SNAPVERE 0.0.9 was a security, reliability, performance and workflow-quality milestone for the tray-first Windows application and native Android companion.

### Android stability and UI/UX

- Every Android capture still required fresh MediaProjection consent; consent tokens were not cached or reused.
- The Activity-to-background handoff remained bounded to five seconds and a seven-second first-frame timeout prevented stalled capture ownership.
- MediaProjection, VirtualDisplay, ImageReader, acquired Image, handler callbacks and HandlerThread teardown became exception-safe per resource.
- `ImageReader.acquireLatestImage()` and MediaStore cleanup paths were guarded against provider/runtime failures.
- Row stride, pixel stride and overflow-sensitive arithmetic were validated before bitmap allocation/copy, with JVM unit coverage for valid and invalid layouts.
- Capture/Open/Share/Delete/Website/Support/Privacy/Terms actions gained controlled failure paths.
- Action pairs stacked on narrow displays or at larger font scale, OEM `forceDark` was disabled and English/Croatian recovery resources were maintained together.
- Android still requested no `INTERNET` permission, disabled backup and cleartext, and kept the MediaProjection service non-exported.

Android CI validated manifest privacy/service behavior, `lintDebug`, `lintRelease`, JVM tests, debug/release builds, APK signature, ZIP alignment and SHA-256. Its APK remained development/CI-debug signed rather than production-store signed.

### Windows tray reliability and accessibility

- The notification-area host adopted `NOTIFYICON_VERSION_4` after every successful icon add/re-add.
- `NIF_SHOWTIP` retained the normal tooltip.
- Version-4 callbacks were decoded correctly and keyboard select/context-menu notifications were supported.
- Existing Region Capture debounce prevented duplicate activation from starting duplicate workflows.

### Save As workflow

Region, Window and Full Screen captures could open the native Windows Save As picker after a PNG was safely created. Cancelling preserved the completed file in `Pictures\SNAPVERE`. Relocation used asynchronous copy, destination-local staging, atomic replacement, PNG-only validation and file-identity protections.

### About, support and legal destinations

About exposed `https://snapvere.com`, `info@snapvere.com`, Privacy, Terms and `https://brendigo.com`. Missing mail-handler fallback copied the support address locally. Opening About did not create background networking.

### Security and package hardening

- NuGet auditing was enabled for direct/transitive dependencies at low severity and above, with `NU1901`–`NU1904` as build errors.
- GitHub Actions were pinned to full commit SHAs; normal CI checkout did not persist repository credentials.
- Dependabot monitored NuGet and Actions dependencies.
- Setup path-boundary handling and ZIP extraction staging/duplicate-destination defenses were tightened.
- Portable caches were validated against trusted embedded architecture-specific SHA-256 manifests, rejecting reparse points, missing/unexpected files and hash/length mismatches before execution.
- A slower redundant ZIP-comparison design was rejected by lifecycle CI and replaced with one sequential hash pass rather than weakening timeouts.
- No WebView/WebView2, HTML/JavaScript execution surface, first-party HTTP/socket client, telemetry, cloud upload or remote command channel was introduced.

### Performance and QA

Tray/hotkey hosts remained event-driven, capture/D3D resources on-demand and Android capture work bounded. Visual QA provenance was hardened after a hosted-runner desktop was discovered in a prior baseline; thresholds were not lowered to hide the issue.

### Release automation

An initial v0.0.9 publication attempt stopped before publication because missing-tag detection mishandled a `gh api` HTTP 404 path. The workflow was corrected to fail closed, explicitly distinguish only HTTP 404 as tag absence and validate existing annotated tags before reuse.

The public Windows release contained exactly `SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe`, each embedding x86/x64/ARM64 payloads. ARM64 remained cross-build/package evidence rather than physical ARM64 runtime evidence. Windows binaries were not represented as Authenticode-signed.

---

## v0.0.8 — 2026-09-10

SNAPVERE 0.0.8 focused on capture-surface localization and release maintenance while retaining the universal tray-first packaging introduced in 0.0.7.

### Localization

- Region Capture moved its window title, tool names, tooltips, resize accessibility labels, color labels, hints, Copy/Save/Close actions and working-state text to shared process-local localization.
- Window Capture used the same language state for title, guidance and keyboard accessibility text.
- Croatian received complete strings for the newly localized capture surfaces and common capture failures.
- English remained the fallback for missing strings; no translation network service, polling worker, watcher or telemetry was added.

### Release automation

Historical v0.0.4–v0.0.7 workflows were moved under `.github/release-archive/` so they remained available as history without registering as active workflows. The v0.0.8 workflow used a dedicated release-trigger path on `main`.

### Public downloads and validation

The public release contained exactly `SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe`, each embedding x86/x64/ARM64 payloads. Validation required x64 build/tests, x86 build, ARM64 cross-build, valid native payloads, exactly two public EXEs, x64/x86 Setup/Portable lifecycle tests and tray/Region/Window/Options/About runtime probes. ARM64 was not claimed as a physical hardware runtime test and binaries were not claimed as Authenticode-signed.

---

## v0.0.7 — 2026-09-10

SNAPVERE 0.0.7 was a packaging, UI consistency, localization and reliability release and the first line with the two-universal-download model.

### Universal packaging

The public GitHub Release contained exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Each executable embedded x86, x64 and ARM64 native payloads and automatically selected the compatible architecture. Users no longer chose separate architecture downloads.

### UI, branding and languages

Tray, Region, Options, Language, About and Setup shared the graphite/violet/cyan SNAPVERE visual system. SNAPVERE was the product brand and Brendigo the developer/publisher. English was canonical fallback, while the built-in selector exposed 28 language choices including Croatian. Language preference stayed local.

### Setup defaults and license

Interactive Setup defaulted Start menu shortcut, Desktop icon and Start with Windows to On while keeping them user-selectable. Silent Setup used the same defaults after explicit license acceptance.

Starting with 0.0.7, SNAPVERE was distributed under the SNAPVERE Commercial Software License Agreement in `LICENSE`; earlier releases remained governed by the terms shipped with those releases.

### Reliability and validation

Obsolete demo packaging paths were removed, universal architecture selection was centralized, safe ZIP extraction/local atomic settings were retained and lifecycle validation covered commercial-license acceptance, default shortcuts/startup registration, same-Setup uninstall and runtime UI probes. Hosted x64 ARM64 validation remained cross-build/package evidence only.

---

## v0.0.6 — 2026-09-10

SNAPVERE 0.0.6 restored the intended tray-first contract after the temporary 0.0.5 startup behavior and completed the visual/runtime cleanup started in the 0.0.4 line.

### Highlights

- Normal installed and Portable launches remained in the notification area instead of opening the legacy Capture Center.
- Tray left-click started Region Capture and right-click opened compact quick actions.
- Region Capture used the redesigned annotation/action toolbar and refreshed selection chrome.
- Window picker, Options/Recent Captures, About and Setup/Uninstall used one visual system.
- The former visible Capture Center was reduced to a hidden runtime/capture coordinator.

### Fixes and validation

`Include cursor on capture` became connected to real Region, Window and Screen workflows. Startup registration used the same tray-first command contract, and Setup controls were corrected for strict analyzers/runtime rendering. x64/x86 package validation covered build/tests, self-contained publishing, license enforcement, Setup UI, install/uninstall, tray/Region/Window/secondary UI probes, startup survival and SHA-256 integrity.

The historical public packaging still included Setup, Portable and self-contained ZIP payloads for x64/x86 plus `SHA256SUMS.txt`. No separate uninstaller binary was installed and binaries remained unsigned.

---

## v0.0.5 — 2026-09-10

SNAPVERE 0.0.5 was a short-lived patch release that temporarily changed user-visible startup behavior.

### Historical behavior

- Manual launches opened the SNAPVERE Capture Center instead of starting silently in the tray.
- Windows startup remained tray-first/hidden through an explicit `--background` argument.
- Portable launches preserved the same manual-versus-background distinction.
- Setup gained an automated UI materialization probe.

x64/x86 validation covered build/tests, Setup/Portable generation, install/uninstall and startup-registration cleanup, visible main-window creation for manual launch, hidden background launch, interactive Setup and SHA-256 integrity.

v0.0.6 superseded this behavior and restored one consistent tray-first normal-launch model. This section intentionally retains the 0.0.5 behavior as historical fact.

---

## v0.0.4 — 2026-09-10

SNAPVERE 0.0.4 introduced a true tray-first capture workflow and real secondary preferences/history surfaces.

### Tray and capture UX

- Normal startup no longer opened the Capture Center.
- Tray left-click and Print Screen / `Ctrl+Shift+1` entered Region Capture.
- Right-click opened branded quick actions.
- Options / Preferences and Recent Captures became real secondary surfaces.
- Start with Windows and Include cursor settings were implemented.

### Region

Region used a frozen primary-display frame, physical-pixel selection, move/eight-handle resize, dimension badge, Move/Pen/Line/Arrow/Box/Highlight tools, annotation colors, Undo and Copy/Save actions.

### Window

Window Capture used top-level-window discovery/DWM bounds, frozen candidate Z-order, DPI-aware picker overlays, self/invisible/cloaked/tool-window filtering and WGC `CreateForWindow`, with local PNG persistence and Recent Captures integration.

### Screen

At this historical version the documented Screen shortcut was `Ctrl+Shift+4`. Monitor capture preferred WGC/D3D11 where supported with controlled GDI compatibility fallback for expected monitor-capture failures.

### Packaging

The release still used architecture-specific x64/x86 self-contained assets. Same-Setup uninstall remained mandatory, standalone uninstaller executables were rejected and user screenshots were preserved. Setup/Portable binaries were unsigned and `SHA256SUMS.txt` was provided for integrity verification.

---

## v0.0.3 — 2026-09-10

SNAPVERE v0.0.3 introduced production Window Capture and further hardened Windows Setup/Portable lifecycle validation.

### Window Capture

- Window Capture was available from the then-current Capture Center, tray or `Ctrl+Shift+2`.
- Visible top-level windows were snapshotted in native Z-order before picker overlays appeared.
- Frozen borderless picker surfaces were created per monitor for mixed-DPI/negative-coordinate layouts.
- DWM extended-frame bounds were used for highlighting and SNAPVERE's own picker windows were excluded.
- WGC `CreateForWindow` performed final HWND acquisition and the PNG used the shared atomic persistence path.

Region retained Print Screen / `Ctrl+Shift+1`, annotations and Copy/Save. The historical Screen shortcut remained `Ctrl+Shift+4`.

### Setup, packages and QA

Windows Installed apps used the installed `SNAPVERE-Setup.exe --uninstall`; no separate uninstaller was shipped. x64/x86 release validation covered build/tests, publish, license acceptance, install/uninstall registration, WinUI/Region/Window probes and installed/Portable process survival.

Historical downloads were architecture-specific Setup, Portable and ZIP files for x64 and x86 plus `SHA256SUMS.txt`. Executables were not Authenticode-signed.

At that point cross-monitor Region composition, Scrolling Capture, richer editor tools, full History/favorites/Pin to Screen, OCR, automatic updates and Authenticode signing were still deliberately deferred.

---

## v0.0.2 — 2026-09-09

SNAPVERE v0.0.2 was a startup-stability and release-QA update for the initial local-first Windows capture foundation.

### Startup stabilization

The main startup path moved to a conservative programmatic WinUI Capture Center with a standard Windows title bar, replacing a more complex `MainWindow` XAML/composition path that had failed packaged runtime validation.

Release QA had identified:

- a former `MainWindow.xaml` load failure around `Application.LoadComponent`;
- hosted `windows-latest` moving to Windows Server 2025 and producing a runtime failure outside the then-supported Windows App SDK server target.

The release removed the fragile startup path and moved WinUI launch gates to `windows-2022`.

### Capture and packages

Region capture, primary Screen capture, `Ctrl+Shift+1` Region, the historical `Ctrl+Shift+4` Screen shortcut, tray actions, local recent-capture discovery and atomic PNG persistence remained connected.

Both x64 and x86 went through self-contained app publishing, Setup/Portable generation, explicit license acceptance, activated WinUI READY probing, normal launch survival, uninstall cleanup and Portable validation.

Historical downloads were separate x64/x86 Setup, Portable and self-contained ZIP files plus `SHA256SUMS.txt`. Executables were intentionally unsigned.

Window Capture, cross-monitor Region, Scrolling Capture, richer editor/history/OCR/update functionality and the WGC/D3D primary monitor backend were still deferred at that version.

---

## v0.0.1 — 2026-09-09

SNAPVERE v0.0.1 was the first public Windows release.

### Downloads

The first release published separate architecture-specific packages:

- `SNAPVERE-0.0.1-Setup-x64.exe`;
- `SNAPVERE-0.0.1-Portable-x64.exe`;
- `SNAPVERE-0.0.1-Setup-x86.exe`;
- `SNAPVERE-0.0.1-Portable-x86.exe`;
- raw self-contained application ZIP packages for advanced/manual deployment.

`x32` and `x86` referred to the same 32-bit Windows architecture.

### Installer

The Setup application was a self-contained per-user installer. The historical 0.0.1 installer used **Mozilla Public License 2.0** terms, required explicit acceptance (including `--accept-license` for silent installation), registered normal Windows Installed apps metadata and used the installed Setup executable for uninstall rather than a standalone uninstaller. User screenshots under `Pictures\SNAPVERE` were preserved during uninstall.

### Implemented functionality

- primary-display Screen Capture;
- freeze-frame Region Capture;
- physical-pixel region selection;
- drag/move/eight resize handles and keyboard nudge;
- Enter/double-click save and Esc cancel;
- PNG output under `Pictures\SNAPVERE` with collision-safe names and atomic persistence;
- recent local capture list;
- `Ctrl+Shift+1` Region and the historical `Ctrl+Shift+4` Screen shortcut;
- native system tray actions;
- per-monitor-v2 DPI awareness and negative-coordinate/mixed-DPI geometry handling;
- x64/x86 self-contained Setup/Portable packaging and lifecycle validation;
- SHA-256 release checksums.

### Privacy, security and limitations

The first capture workflow was local-first and did not require accounts, analytics or telemetry. Package extraction rejected absolute/traversal paths, bounded expanded content and used staging before replacement. Release executables were intentionally not Authenticode-signed.

At v0.0.1 Region Capture was primary-display-only, the newer WGC/D3D primary acquisition backend was not enabled, and Window Capture, Scrolling Capture, full History, annotation Editor, OCR, Pin to Screen and updater were intentionally unavailable rather than exposed as fake placeholders.

---

**SNAPVERE — Capture what matters. Keep it yours.**  
Developed and published by **Brendigo**.