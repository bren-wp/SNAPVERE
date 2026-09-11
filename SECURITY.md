# Security Policy

## Scope

Security issues affecting SNAPVERE capture data, clipboard behavior, local preferences/history, native window targeting, Android MediaProjection/MediaStore handling, temporary files, Setup/Portable extraction, uninstall cleanup, Android signing, release integrity, dependency supply chain or future update mechanisms should be treated as sensitive.

SNAPVERE is a local-first Windows and Android capture product. Core capture does not require telemetry, cloud upload, accounts, credentials or a first-party network service.

## 0.1.0 threat model

The desktop runtime has no WebView/WebView2, HTML renderer, JavaScript execution surface or first-party HTTP/socket client. Browser-style XSS is therefore not an applicable current runtime surface. A future embedded-web/network feature would require a new origin/navigation/content/script review before shipping.

Android intentionally declares no `android.permission.INTERNET` and contains no telemetry, analytics, cloud upload or remote-command client. Website, support and legal destinations are delegated to OS handlers only after explicit user input.

SNAPVERE is not a security boundary against arbitrary code already executing as the same OS user. Hardening focuses on preventing SNAPVERE itself from introducing traversal, unsafe extraction, hidden networking, destructive path mistakes, stuck capture ownership, corrupt image-buffer handling or supply-chain/release regressions.

## Dependency and CI supply chain

Repository-wide .NET restore enables NuGet auditing for direct and transitive dependencies at `low` severity and above. `NU1901`–`NU1904` are build errors.

CI/release actions are pinned to full commit SHAs and ordinary CI checkout does not persist repository credentials. Dependabot monitors NuGet and GitHub Actions dependencies.

Android CI treats lint warnings as errors, runs JVM tests, builds debug/release variants and verifies the development APK signature/alignment. Its manifest contract fails if `INTERNET`, cleartext, backup, exported capture service or version regressions are introduced.

No private signing key, production credential or private token belongs in Git source or generated source archives.

## Sensitive information rules

Routine diagnostics must not intentionally include:

- screenshot pixels/image contents;
- clipboard contents;
- OCR/user file text;
- credentials/API keys/signing material;
- private capture history beyond paths/metadata needed by the local feature.

Windows startup diagnostics contain stage/exception metadata and stay under the current account.

## Capture security boundaries

SNAPVERE does not attempt to bypass DRM, protected-content restrictions or OS capture policy. A blocked/blank protected capture is not authorization to weaken OS protection.

Windows Window Capture freezes eligible top-level-window metadata before picker overlays, uses geometric hit testing and filters self/tool/invisible/cloaked/invalid targets. Expected WGC compatibility/native/timeout failures may use the documented resilient monitor fallback; unexpected programming errors are not silently converted into unrelated capture work.

Android requires fresh MediaProjection consent for every capture. Consent tokens are never cached/reused. Five-second Activity-hide and seven-second first-frame guards bound capture ownership.

## Android resource and buffer safety

`CaptureService` uses a process-local single-active-capture guard. 0.1.0 additionally makes capture ownership service-instance-aware so stale teardown does not clear another active owner.

Cleanup is idempotent/per-resource. A framework/vendor failure while releasing one object does not intentionally prevent remaining MediaProjection, VirtualDisplay, ImageReader, callback, Image, HandlerThread or ownership cleanup.

The service also checks Handler scheduling, guards `acquireLatestImage()`, closes acquired Image objects before final completion cleanup and contains conversion/provider/allocation failures within the session recovery path.

For `RGBA_8888`, capture-buffer validation requires:

- positive visible width;
- 4-byte pixel stride;
- positive/sufficient row stride;
- whole-pixel row padding;
- non-overflowing width arithmetic;
- rewound ByteBuffer;
- enough bytes for `rowStride × height`.

JVM regression tests cover tight/padded layouts, invalid dimensions, unexpected pixel stride, partial padding and overflow.

## Android storage and actions

Captures use MediaStore under `Pictures/SNAPVERE` without broad storage permission. Pending-state finalization is checked; incomplete-item cleanup is best-effort and cannot mask the original save failure.

Latest-capture actions revalidate the stored URI. Open/Share/Delete and Website/Support/Privacy/Terms have controlled failure paths. External destinations are user initiated and do not create a SNAPVERE networking client.

## Local Windows settings and history

Windows preferences live at:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Writes use temporary-file + atomic move. Corrupt/unreadable settings fall back to safe defaults. 0.1.0 also contains filesystem-policy `SecurityException` during preference load/temp cleanup and recent-capture discovery.

Capture PNG writes use local staging followed by atomic move. Failure to remove a staging file because of I/O/access/security policy never replaces the original capture exception.

**Start SNAPVERE with Windows** remains per-user. Setup uninstall removes the SNAPVERE Run value only when it identifies the validated installed executable; unrelated/Portable registration is preserved.

Android latest URI/name metadata is private SharedPreferences convenience state only.

## Package extraction security

Setup/Portable embedded ZIP extraction must reject absolute paths, parent traversal, destination escape, duplicate destinations, excessive entry counts/expanded size and unsafe staging. Child process arguments are not constructed as shell commands.

Portable cached payloads are not trusted merely because marker/executable files exist. The trusted expected manifest is embedded in the Portable host with the architecture payload. Reuse validation checks expected paths, lengths, SHA-256, missing/unexpected content and reparse points, then transactionally rebuilds/revalidates an invalid cache.

The embedded manifest protects against silently trusting persistent writable extracted content between executions; it is not represented as a sandbox against arbitrary same-user code modifying a running process.

## Setup/uninstall safety

Protected path-boundary validation treats a protected directory itself and descendants as inside the boundary while sibling-prefix paths do not match.

Before recursive install-directory removal, Setup validates the SNAPVERE installation marker and expected maintenance/application files. An arbitrary path from command line/registry must never be recursively deleted merely because it was supplied.

Windows Installed apps invokes:

```text
SNAPVERE-Setup.exe --uninstall
```

No separate uninstaller binary is installed. User screenshots live outside the install directory and are preserved.

## Runtime/background-work boundary

Windows tray/global-hotkey hosts use blocking native message loops, not periodic polling. Capture/D3D resources are created for capture work rather than kept resident solely for tray presence. Secondary UI is on demand.

Android has no idle capture worker. MediaProjection/ImageReader/VirtualDisplay/HandlerThread resources exist only for an explicitly approved capture and are bounded by failure cleanup.

Security or telemetry work must not add hidden periodic network requests, high-frequency timers, file watchers or polling loops merely to claim monitoring.

## Android release signing

The development Actions APK is debug-signed evidence only. The public `SNAPVERE.apk` in v0.1.0 must be the minified/shrunk release variant aligned and signed with SNAPVERE's stable private Android release identity.

The release workflow requires these secrets, managed outside the repository:

```text
SNAPVERE_ANDROID_KEYSTORE_BASE64
SNAPVERE_ANDROID_KEY_ALIAS
SNAPVERE_ANDROID_KEYSTORE_PASSWORD
SNAPVERE_ANDROID_KEY_PASSWORD
```

If signing material is missing/invalid, release automation fails before tag creation. It never falls back to an ephemeral debug identity for the public package.

`SNAPVERE-Android-Source.zip` is generated from the validated Git tree and must not contain keystores, secrets, generated build output or Gradle caches.

## Release integrity

The v0.1.0 public release contract is exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

Windows publication remains gated by audited build/tests, x86 build, ARM64 cross-build, payload/integrity-manifest validation, six rendered UI surfaces and x64/x86 Setup+Portable lifecycle/tray-first probes.

Android publication is gated by manifest/version/privacy checks, lint, JVM tests, debug/release builds, release APK alignment/signature verification and Android source-archive validation.

The workflow transfers Android candidates through a GitHub Actions artifact with recorded SHA-256 values and re-verifies those hashes before admitting them to the final release directory.

The immutable `v0.1.0` tag is created only for the exact validated commit and only after all pre-publication gates pass. Missing-tag detection accepts only an actual HTTP 404 as absence; other GitHub API errors abort. Existing annotated tags must resolve to the exact commit.

Post-publication validation requires exactly the four expected assets and compares each GitHub-reported SHA-256 digest with the locally validated candidate hash. Existing release assets are never replaced by the workflow.

Windows binaries are not represented as Authenticode-signed unless a real certificate/signing gate is introduced and verified. SHA-256 establishes byte identity, not publisher identity. Android APK publisher identity is provided by its stable release key.

## Update security

0.1.0 has no automatic updater or licensing network feature. A future updater must authenticate metadata/artifacts, validate hashes/architecture/origin/version direction and define rollback behavior; TLS alone is not sufficient publisher authenticity.

## Repository hygiene

Never commit:

- Authenticode/private Android signing keys;
- production credentials/passwords/tokens;
- private screenshots or real-user app data;
- crash dumps containing user material;
- generated release package output unless intentionally tracked as a public artifact;
- secrets embedded in workflow/source files.

## Reporting

Use GitHub private security reporting when disclosure could expose users or a practical exploit. General security/support contact: `info@snapvere.com`. Do not post sensitive proof-of-concept user data in a public issue.
