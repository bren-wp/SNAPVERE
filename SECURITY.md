# SNAPVERE Security Policy

## Supported public line

The current public release is **SNAPVERE 0.1.1**: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

Security reports affecting the Windows application, Android application, Chrome/Edge/Opera/Firefox extensions, packaging, local capture data, settings/history, signing, release integrity or dependency supply chain are in scope.

Historical release `v0.1.0` remains immutable and correctly reflects the older four-asset Windows/Android product state. Current security documentation describes the 0.1.1 product line and post-release `main` hardening.

## Security and privacy model

SNAPVERE is a **local-first capture product**. Core screenshot capture does not require a SNAPVERE account, first-party cloud upload, analytics service or advertising service.

SNAPVERE is not a security boundary against arbitrary code already executing as the same OS/browser user. Hardening focuses on preventing SNAPVERE itself from introducing unsafe extraction, path traversal, destructive cleanup, stale capture ownership, corrupt image-buffer handling, overly broad browser permissions, hidden networking, remote runtime code or supply-chain/release regressions.

SNAPVERE does not attempt to bypass DRM, protected-content restrictions, browser privileged-page rules or OS capture policy. A blocked/blank capture is not authorization to weaken platform protections.

## Windows threat model

The Windows desktop runtime does not embed a WebView/WebView2/HTML/JavaScript application surface and does not include a first-party screenshot upload client. Capture, editing and PNG encoding are local.

### Capture boundaries

- Region Capture operates on a frozen captured frame with physical-pixel selection and local annotations.
- Window Capture discovers eligible top-level windows, freezes target metadata before picker overlays and uses Windows.Graphics.Capture where supported.
- Expected WGC/native/timeout failures can use the documented resilient monitor fallback; unexpected programming errors should not be silently converted into unrelated capture work.
- Screen capture does not claim the ability to bypass protected-content restrictions.

### Local file integrity

Capture PNG writes use a temporary file followed by final move, so a partially encoded image is not intentionally exposed as a completed capture. Failure to remove a staging file must not replace the original capture exception.

Windows preferences live at:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Settings writes use a staged/atomic replacement pattern. Corrupt/unreadable settings fall back to safe defaults through the implemented preference handling.

### Startup diagnostics

Windows startup diagnostics can be written under:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Routine diagnostics must not intentionally contain screenshot pixels, clipboard contents, credentials, private signing material or arbitrary user-document contents.

## Setup and Portable security

Setup/Portable embedded payload extraction must reject:

- absolute archive targets;
- parent traversal and destination escape;
- duplicate destinations;
- unsafe reparse-point content;
- excessive/invalid extraction state;
- unexpected cached payload files.

The Portable host carries trusted architecture-specific integrity manifests and validates expected paths, lengths and SHA-256 hashes before reusing writable extracted content. Invalid reusable cache is rebuilt/revalidated transactionally.

This protects the SNAPVERE extraction/reuse contract; it is not a sandbox against arbitrary same-user code modifying a live process.

### Uninstall safety

Before recursive installation-directory deletion, Setup validates the SNAPVERE installation marker and expected maintenance/application files. An arbitrary command-line/registry path must never become sufficient authority for recursive deletion.

Windows Installed apps invokes the same maintenance binary:

```text
SNAPVERE-Setup.exe --uninstall
```

No separate uninstaller binary is required. User screenshots under `Pictures\SNAPVERE` are outside the installation directory and are preserved by the install-removal contract.

## Android threat model

The Android manifest intentionally does **not** request `android.permission.INTERNET`. Cleartext traffic and application backup are disabled and `CaptureService` is non-exported with `foregroundServiceType="mediaProjection"`.

Website, support, privacy, terms, Open and Share actions are explicit user actions delegated to Android/external applications; their existence does not create a hidden SNAPVERE network client.

### MediaProjection consent and ownership

- every capture requires a fresh Android system MediaProjection consent flow;
- consent intents/tokens are not cached for silent reuse;
- a process-local single-active-capture guard prevents overlapping sessions;
- capture ownership is service-instance-aware so stale teardown does not clear another active owner;
- a five-second Activity-hide guard bounds the handoff before capture starts;
- a seven-second first-frame guard bounds frame acquisition.

### Resource and image-buffer safety

Cleanup is designed to be idempotent/per-resource. Failure while releasing one framework object should not intentionally prevent remaining MediaProjection, VirtualDisplay, ImageReader, callback, Image, HandlerThread or ownership cleanup.

For `RGBA_8888`, buffer validation requires:

- positive visible width;
- 4-byte pixel stride;
- positive/sufficient row stride;
- whole-pixel row padding;
- non-overflowing width/byte arithmetic;
- rewound ByteBuffer;
- enough bytes for `rowStride × height`.

JVM regression tests cover tight/padded layouts, invalid dimensions/stride, partial padding and overflow.

### Android storage

Captures use MediaStore under `Pictures/SNAPVERE` without broad filesystem permission. `IS_PENDING` finalization is checked and incomplete-item cleanup is best-effort without masking the original save error.

Latest-capture Open/Share/Delete revalidate the stored URI. Stale unreadable latest-capture references are removed.

## Browser-extension threat model

SNAPVERE 0.1.1 publicly ships Chrome, Edge, Opera and Firefox extension ZIPs.

The current extension permission set is exactly:

```text
activeTab
scripting
downloads
storage
```

There is no `<all_urls>` permission and no broad `host_permissions` entry. The extension does not attempt to bypass protected/internal browser pages.

### Runtime/source policy

The extension source contains no first-party telemetry, analytics SDK, ad SDK, cloud-upload client, remote runtime script or background network client. Extension HTML is checked for remote runtime resources, inline script/style blocks and inline event handlers.

Messages are restricted to explicit known types. Region completion validates the stored capture token plus sender tab/window identity. Capture-session state is bounded by a TTL and successful/error paths release the lock.

Full-page capture has explicit bounds on tile count, canvas dimensions and total pixel allocation. A content-side watchdog restores scroll/floating-element state if orchestration disappears.

### Browser CI hardening

Browser CI validates:

- exact permission allow-list and absence of broad host access;
- manifest/localization/runtime file policy;
- source parity across Chrome/Edge/Opera with only expected Firefox differences;
- icon dimensions/hash parity;
- behavioral background smoke tests for download/lock/error paths;
- store metadata/privacy declarations against actual manifests;
- reproducible ZIP packages and SHA-256 integrity.

These checks are not represented as exhaustive manual GUI/runtime testing of every browser/page combination.

## Dependency and CI supply chain

Repository-wide .NET restore enables NuGet auditing for direct and transitive dependencies at `low` severity and above. `NU1901`–`NU1904` are build errors.

GitHub Actions used by the validated workflows are pinned to full commit SHAs. Ordinary CI checkout does not persist repository credentials where not required. Dependabot configuration monitors relevant dependency/action surfaces.

Android lint warnings are treated as errors and both debug/release variants are built. Browser source is dependency-light and release packages contain only approved extension source/assets.

No private signing key, production credential, password or private token belongs in Git source, documentation examples or generated source archives.

## Product/version contract security

[`product-version.json`](product-version.json) is the canonical active product contract for 0.1.1. `eng/validate-product-contract.py` verifies that:

- Windows product/assembly/file versions align;
- Android versionName/versionCode/SDK contract aligns;
- Android EN/HR resource keys remain in parity;
- all four browser manifests/store metadata align;
- the exact eight-file public release contract is unchanged;
- active README/current docs point at the current release;
- relative Markdown documentation links resolve.

This prevents documentation/version drift from silently changing security or signing claims.

## Android release signing

The public v0.1.1 `SNAPVERE.apk` is intentionally the validated **CI/debug-signed** APK. The release workflow also builds the release variant, but no private production/Google Play keystore is required or claimed for the public package.

The signing identity provides package-signature integrity for that published APK, but it is **not** represented as a stable Google Play production publisher identity. A future channel using a different stable production key can require uninstall/reinstall and must document that transition explicitly.

`SNAPVERE-Android-Source.zip` must not contain keystores, secrets, generated build output or Gradle caches.

## Windows code signing

Public Windows executables are not represented as Authenticode-signed unless a real certificate/signing gate is introduced and verified. SHA-256 proves byte identity, not publisher identity or reputation.

## Browser-store signing/publication

The public v0.1.1 browser ZIPs are GitHub release assets. They are **not** represented as Chrome Web Store, Edge Add-ons, Opera Add-ons or Mozilla Add-ons approved/signed packages unless real external publisher/review status supports that claim.

Firefox AMO signing and all store-side certification remain external to the current repository credentials/workflows.

## v0.1.1 release integrity

The public v0.1.1 release contract is exactly:

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

The release workflow:

1. validates/builds Android and records transfer hashes;
2. validates and reproducibly packages all four browser variants;
3. audits/builds/tests Windows and constructs x86/x64/ARM64 payloads;
4. validates architecture integrity manifests;
5. builds universal Setup and Portable;
6. re-verifies transferred Android/browser files;
7. enforces the exact eight-file release directory;
8. validates x64/x86 Setup/Portable runtime contracts;
9. creates/verifies the exact `v0.1.1` tag only after gates pass;
10. publishes the approved files;
11. compares GitHub-reported SHA-256 digests with locally validated values.

Already published v0.1.1 assets/tags are treated as immutable historical output. Post-release `main` hardening must use a future version for changed release binaries instead of replacing v0.1.1 assets.

## Update security

SNAPVERE 0.1.1 has no first-party automatic binary updater or licensing-network feature. A future updater must authenticate update metadata/artifacts, validate origin/hash/architecture/version direction and define rollback behavior. TLS alone is not sufficient publisher authenticity.

## Repository hygiene

Never commit:

- Authenticode/private Android/browser-store signing keys;
- production credentials/passwords/tokens;
- private screenshots or real-user application data;
- crash dumps containing sensitive user material;
- generated private signing output;
- secrets embedded in workflow/source files.

## Reporting a vulnerability

Use GitHub private security reporting when disclosure could expose users or a practical exploit. General security/support contact: **info@snapvere.com**.

Do not post sensitive proof-of-concept user data or private screenshots in a public issue.

For the wider privacy model see [Privacy](docs/PRIVACY.md), and for the validation boundary see [QA Matrix](docs/QA-MATRIX.md).
