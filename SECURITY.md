# Security Policy

## Scope

Security issues affecting SNAPVERE capture data, clipboard behavior, local preferences/history, native window targeting, Android MediaProjection/MediaStore handling, temporary files, Setup/Portable extraction, uninstall cleanup, release integrity, dependency supply chain or future update mechanisms should be treated as sensitive.

SNAPVERE is a local-first Windows and Android capture product. Core capture does not require telemetry, cloud upload, accounts, credentials or a first-party network service.

## v0.0.9 threat model

The v0.0.9 desktop codebase contains no WebView/WebView2, HTML renderer, JavaScript execution surface or first-party HTTP/socket client. Classic browser-style XSS is therefore not an applicable runtime attack surface in this version. If a future release introduces embedded web content, remote HTML, scripting or a network API, that changes the threat model and requires explicit origin, navigation, content and script-isolation review before shipping.

The Android application intentionally declares no `android.permission.INTERNET` and contains no telemetry, analytics, cloud upload or remote-command client. Website, support and legal destinations are delegated to OS browser/mail handlers only after explicit user input.

SNAPVERE does not claim to be a security boundary against arbitrary code already executing as the same OS user. Such code can access user-writable files and can generally perform actions the user can perform. Hardening therefore focuses on preventing SNAPVERE itself from introducing command execution, traversal, unsafe package extraction, hidden network behavior, destructive path mistakes, stuck capture ownership or supply-chain regressions.

## Dependency and CI supply chain

Repository-wide .NET restore explicitly enables NuGet auditing for direct and transitive dependencies at `low` severity and above. `NU1901`, `NU1902`, `NU1903` and `NU1904` are build errors, so a known NuGet vulnerability blocks CI/release validation until addressed or deliberately reviewed.

CI uses pinned full commit SHAs for third-party GitHub Actions instead of mutable major-version tags, and ordinary build checkouts do not persist repository credentials. Dependabot monitors NuGet and GitHub Actions dependencies on a scheduled basis.

Android CI treats lint warnings as errors, runs local JVM tests, builds debug and release variants and verifies the debug APK signature/alignment. Its manifest contract fails if `INTERNET`, cleartext traffic, app backup or exported capture-service regressions are introduced.

No signing key, production credential or private token belongs in the repository or application payload.

## Sensitive information rules

SNAPVERE must not intentionally write the following to routine diagnostics:

- screenshot pixels or image contents;
- clipboard contents;
- OCR text;
- user file contents;
- credentials, API keys or signing material;
- private user capture history beyond paths/metadata needed by the local feature.

Windows startup diagnostics record stage and exception metadata only and remain under the current Windows account.

## Capture security boundaries

SNAPVERE does not attempt to bypass DRM, protected-content restrictions or OS capture policy. A blocked, blank or protected capture is not authorization to weaken OS protections or introduce a bypass backend.

Windows Window Capture snapshots eligible top-level windows before always-on-top picker overlays appear and geometrically hit-tests that frozen list. Self, tool, invisible, cloaked and invalid targets are filtered so SNAPVERE overlays are not selected as normal capture targets.

Caller cancellation must not become hidden fallback work. Expected WGC compatibility/native/timeout failures may use the documented monitor GDI compatibility fallback; unexpected programming errors are allowed to surface.

Android capture requires fresh MediaProjection consent for each capture session. Consent tokens are not cached or reused. A five-second Activity-hide guard and seven-second first-frame guard bound session ownership. Timeout/failure cleanup releases capture resources rather than leaving a foreground projection indefinitely active.

## Android resource and storage safety

`CaptureService` has a process-local single-active-capture guard. Cleanup is idempotent and per-resource so a RuntimeException from one Android framework/vendor cleanup call does not prevent the remaining MediaProjection, VirtualDisplay, ImageReader, Image, handler-thread or ownership cleanup.

Image acquisition is guarded, acquired `Image` objects are always closed, and capture-buffer width/stride arithmetic is validated before bitmap allocation. JVM tests cover valid padding and invalid/overflowing row-layout cases.

Captures are stored through MediaStore under `Pictures/SNAPVERE` without broad storage permission. An incomplete pending item is cleaned on failure when possible. A stale latest-capture URI is disabled/removed from private preferences rather than being treated as a trusted live object.

## Local settings and capture history

Implemented Windows preferences are stored at:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Writes use a temporary file followed by atomic replacement/move. Corrupt or unreadable settings fall back to safe defaults rather than executing data from the file.

Recent-capture discovery is local filesystem enumeration only. v0.0.9 avoids redundant per-file metadata refresh work and tolerates expected I/O/access races without adding a resident cache, watcher or polling loop.

**Start SNAPVERE with Windows** uses the current user's Windows `Run` key. Installed Setup uninstall removes the SNAPVERE Run value only when its command points exactly to the validated installed `Snapvere.exe`. A different/Portable registration is deliberately preserved.

Android latest-capture URI/name metadata is stored in private SharedPreferences only for local Open/Share/Delete convenience.

## Package extraction security

Setup and Portable packages use guarded embedded ZIP extraction. Extraction must:

- reject absolute paths;
- reject parent traversal and destination escape;
- canonicalize target paths before writing;
- bound entry count and total expanded size;
- reject duplicate file destinations;
- use bounded random staging filenames rather than extending an attacker-controlled or unusually long entry basename;
- write through private staging/versioned cache locations;
- avoid shell command construction for child process arguments;
- clean stale staging/cache data when safe.

Portable launch preparation is mutex-protected and a launcher must not report normal-startup success if the extracted child exits immediately.

A user-writable Portable extraction cache is not trusted merely because marker/executable files exist. The expected content manifest is generated from the exact CI publish output and embedded inside the Portable host next to the corresponding architecture payload. On cache reuse, SNAPVERE verifies expected relative paths, lengths and SHA-256 digests, rejects reparse points and unexpected/missing files, and transactionally rebuilds an invalid cache before re-verifying it.

The integrity manifest is not a writable sidecar in the cache and is not presented as protection against arbitrary same-user code modifying the already-running process. Its purpose is to prevent the launcher from silently trusting persistent user-writable extracted content between executions. Distribution authenticity remains a separate boundary until publisher signing is introduced.

## Setup/uninstall security

Setup is per-user by default. Protected path-boundary checks must treat both a protected directory itself and all descendants as inside the boundary; sibling names that merely share a string prefix must not match.

Before destructive install-directory removal, Setup validates the SNAPVERE installation marker and expected application/maintenance files. An untrusted arbitrary directory must never be recursively removed merely because it was supplied as a command-line path or registry value.

Windows Installed apps invokes the installed:

```text
SNAPVERE-Setup.exe --uninstall
```

SNAPVERE intentionally does not create a separate `uninstall.exe`, `uninstaller.exe`, `unins000.exe` or `unins*.exe` payload. User captures are outside the install directory and are preserved by uninstall.

## Runtime resource and background-work boundary

Windows tray and global-hotkey hosts use blocking Win32 message loops rather than periodic polling. Capture/D3D resources are created for capture work rather than kept active solely for tray residency.

The Windows tray host negotiates `NOTIFYICON_VERSION_4` after each icon add/re-add, decodes its documented event field and keeps native handles deterministically owned. A UI subscriber exception is contained so it cannot terminate the native tray message loop.

Android has no idle capture worker. MediaProjection, ImageReader and capture-thread resources exist only for an explicitly approved capture session and have bounded failure cleanup.

Security or telemetry work must not introduce hidden periodic network requests, high-frequency timers, file watchers or resident polling loops. Performance claims must be tied to code/CI/runtime evidence; the project does not publish invented CPU or RAM percentages.

## Release integrity

The v0.0.9 public Windows release contract is exactly two universal executables:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Each host embeds x86, x64 and ARM64 application payloads. Publication is gated by x64 build/tests, x86 build, ARM64 cross-build, payload + integrity-manifest validation, rendered-UI validation, exact two-file enforcement and x64/x86 Setup/Portable lifecycle/tray-first checks.

The release workflow creates an immutable `v0.0.9` tag only for the exact validated release commit and must never replace assets on an already-published release. Missing-tag detection explicitly checks the GitHub CLI exit code and accepts only an actual HTTP 404 as absence; other API failures abort publication. Tag object/ref creation also fails closed on API errors or missing SHAs.

Post-publication validation requires exactly the two expected assets and matches GitHub-reported SHA-256 asset digests against the local hashes generated from the release candidates.

Android CI artifacts are separate development/internal artifacts and do not expand the exact two-file Windows GitHub Release contract.

v0.0.9 Windows binaries are not represented as Authenticode-signed unless a real signing certificate and signing step are added and independently verified. SHA-256 proves byte identity, not publisher trust. Android CI APKs are debug-signed and are not represented as production Play Store signing.

## Update security

v0.0.9 does **not** include an automatic updater or licensing network feature. A future updater must verify signed metadata plus artifact hash, architecture, expected origin and version direction before applying an update. TLS transport alone is not sufficient authenticity.

## Repository hygiene

Never commit:

- Authenticode/private Android signing keys;
- production API credentials or passwords;
- private user screenshots;
- `%LOCALAPPDATA%\SNAPVERE` settings/logs/history from real users;
- crash dumps containing user material;
- generated release package output unless intentionally tracked as a public artifact;
- secrets embedded in workflow files or source.

## Reporting

Use GitHub private security reporting when disclosure could expose users or a practical exploit. General security/support contact is `info@snapvere.com`. Do not post sensitive proof-of-concept user data in a public issue.
