# Security Policy

## Scope

Security issues affecting SNAPVERE capture data, clipboard behavior, local preferences/history, native window targeting, temporary files, Setup/Portable extraction, uninstall cleanup, release integrity, dependency supply chain or future update mechanisms should be treated as sensitive.

SNAPVERE is a local-first Windows capture application. Core capture does not require telemetry, cloud upload, accounts, credentials or an external network service.

## v0.0.9 threat model

The v0.0.9 desktop codebase contains no WebView/WebView2, HTML renderer, JavaScript execution surface or first-party HTTP/socket client. Classic browser-style XSS is therefore not an applicable runtime attack surface in this version. If a future release introduces embedded web content, remote HTML, scripting or a network API, that changes the threat model and requires explicit origin, navigation, content and script-isolation review before shipping.

SNAPVERE does not claim to be a security boundary against arbitrary code that is already executing as the same Windows user. Such code can access user-writable files and can generally perform actions the user can perform. Security hardening therefore focuses on preventing SNAPVERE itself from introducing command execution, traversal, unsafe package extraction, hidden network behavior, destructive path mistakes or supply-chain regressions.

## Dependency and CI supply chain

Repository-wide .NET restore explicitly enables NuGet auditing for direct and transitive dependencies at `low` severity and above. `NU1901`, `NU1902`, `NU1903` and `NU1904` are treated as build errors, so a known NuGet vulnerability blocks CI/release validation until it is addressed or deliberately reviewed.

CI uses pinned full commit SHAs for third-party GitHub Actions instead of mutable major-version tags, and ordinary build checkouts do not persist repository credentials. Dependabot monitors NuGet and GitHub Actions dependencies on a scheduled basis.

No signing key, production credential or private token belongs in the repository or application payload.

## Sensitive information rules

SNAPVERE must not intentionally write the following to routine diagnostics:

- screenshot pixels or image contents;
- clipboard contents;
- OCR text;
- user file contents;
- credentials, API keys or signing material;
- private user capture history beyond paths/metadata needed by the local feature.

Startup diagnostics record stage and exception metadata only and remain under the current Windows account.

## Capture security boundaries

SNAPVERE does not attempt to bypass DRM, protected-content restrictions or Windows capture policy. A blocked, blank or protected capture is not authorization to weaken OS protections or introduce a bypass backend.

Window Capture snapshots eligible top-level windows before always-on-top picker overlays appear and geometrically hit-tests that frozen list. Self, tool, invisible, cloaked and invalid targets are filtered so SNAPVERE overlays are not selected as normal capture targets.

Caller cancellation must not be converted into hidden fallback work. Expected WGC compatibility/native/timeout failures may use the documented monitor GDI compatibility fallback; unexpected programming errors are allowed to surface.

## Local settings and capture history

Implemented preferences are stored at:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Writes use a temporary file followed by atomic replacement/move. Corrupt or unreadable settings fall back to safe defaults rather than executing data from the file.

Recent-capture discovery is local filesystem enumeration only. v0.0.9 avoids redundant per-file metadata refresh work and tolerates expected I/O/access races without adding a resident cache, watcher or polling loop.

**Start SNAPVERE with Windows** uses the current user's Windows `Run` key. Installed Setup uninstall removes the SNAPVERE Run value only when its command points exactly to the validated installed `Snapvere.exe`. A different/Portable registration is deliberately preserved.

## Package extraction security

Setup and Portable packages use guarded embedded ZIP extraction. Extraction must:

- reject absolute paths;
- reject parent traversal and destination escape;
- canonicalize target paths before writing;
- bound entry count and total expanded size;
- use bounded random staging filenames rather than extending an attacker-controlled or unusually long entry basename;
- write through private staging/versioned cache locations;
- avoid shell command construction for child process arguments;
- clean stale staging/cache data when safe.

Portable launch preparation is mutex-protected and a launcher must not report normal-startup success if the extracted child exits immediately.

A user-writable Portable extraction cache is not represented as tamper-proof against arbitrary code already running as that same Windows user. SNAPVERE does not use a sidecar hash as a false security boundary because same-user code could replace both the cache and sidecar. Published wrapper integrity and the release provenance checks remain the meaningful distribution boundary until Authenticode signing is introduced.

## Setup/uninstall security

Setup is per-user by default. Protected path-boundary checks must treat both a protected directory itself and all of its descendants as inside the boundary; sibling names that merely share a string prefix must not match. This prevents exact protected-root cases from bypassing a descendant-only prefix check.

Before destructive install-directory removal, Setup validates the SNAPVERE installation marker and expected application/maintenance files. An untrusted arbitrary directory must never be recursively removed merely because it was supplied as a command-line path or registry value.

Windows Installed apps invokes the installed:

```text
SNAPVERE-Setup.exe --uninstall
```

SNAPVERE intentionally does not create a separate `uninstall.exe`, `uninstaller.exe`, `unins000.exe` or `unins*.exe` payload.

User captures are outside the install directory and are preserved by uninstall.

## Runtime resource and background-work boundary

Tray and global-hotkey hosts use blocking Win32 message loops rather than periodic polling. Capture/D3D resources are created for capture work rather than kept active solely for tray residency. Security or telemetry work must not introduce hidden periodic network requests, high-frequency timers, file watchers or resident polling loops.

Performance claims in documentation must be tied to code/CI/runtime evidence; the project does not publish invented CPU or RAM percentages.

## Release integrity

The v0.0.9 release contract is exactly two public universal executables:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Each host embeds x86, x64 and ARM64 application payloads. Publication is gated by x64 build/tests, x86 build, ARM64 cross-build, package structure validation, rendered-UI validation, exact two-file enforcement and x64/x86 Setup/Portable lifecycle checks.

The release workflow creates an immutable `v0.0.9` tag only for the exact validated release commit and must never replace assets on an already-published release. SHA-256 values are recorded as release integrity metadata without adding a third public download.

v0.0.9 binaries are not represented as Authenticode-signed unless a real signing certificate and signing step are added and independently verified. SHA-256 proves byte identity, not publisher trust.

## Update security

v0.0.9 does **not** include an automatic updater or licensing network feature. A future updater must verify signed metadata plus artifact hash, architecture, expected origin and version direction before applying an update. TLS transport alone is not sufficient authenticity.

## Repository hygiene

Never commit:

- Authenticode/private signing keys;
- production API credentials or passwords;
- private user screenshots;
- `%LOCALAPPDATA%\SNAPVERE` settings/logs/history from real users;
- crash dumps containing user material;
- generated release package output unless it is an intentionally tracked public artifact;
- secrets embedded in workflow files or source.

## Reporting

Use GitHub private security reporting when disclosure could expose users or a practical exploit. General security/support contact is `info@snapvere.com`. Do not post sensitive proof-of-concept user data in a public issue.
