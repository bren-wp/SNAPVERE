# SNAPVERE 0.1.1 — Security, Privacy and Performance

This document records the automated security/privacy/performance evidence for the SNAPVERE 0.1.1 release line. It complements, rather than replaces, [`SECURITY.md`](../SECURITY.md).

## Release scope

v0.1.1 contains Windows, Android and browser-extension deliverables. A valid public GitHub Release contains exactly eight files: two universal Windows executables, the Android APK and source archive, and four browser ZIP packages.

Historical v0.1.0 assets remain immutable and are not rewritten by the 0.1.1 release process.

## Windows controls

- .NET restore enables NuGet audit for direct and transitive dependencies at `low` severity and above.
- `NU1901`–`NU1904` are treated as build errors.
- Nullable analysis and .NET analyzers remain enabled with warnings treated as errors.
- Release payloads are built separately for x86, x64 and ARM64.
- Each native payload receives a SHA-256 integrity manifest.
- Universal Setup and Portable hosts are rebuilt from validated payloads.
- Portable cache contents are checked against trusted embedded integrity metadata before launch.
- x64 and x86 Setup/Portable lifecycle and tray-first runtime probes must pass before the release tag can be created.
- Normal CI captures real rendered Region, Window, Tray, Options, Language and About surfaces and performs PR visual-regression checks against the successful-main baseline.

ARM64 remains cross-build/package evidence on hosted x64 runners; it is not represented as physical ARM64 runtime coverage.

## Android controls

- Android version is `0.1.1` / `versionCode 11`.
- `android.permission.INTERNET` is forbidden by the release contract.
- Cleartext traffic and application backup are disabled.
- `CaptureService` remains non-exported and restricted to the `mediaProjection` foreground-service type.
- Every capture requires a fresh MediaProjection consent flow; tokens are not reused for continuous/background recording.
- Release automation runs `lintDebug`, `lintRelease`, JVM tests, debug build and release build.
- The public APK is verified with `apksigner`, checked for ZIP alignment and hashed with SHA-256.
- The public v0.1.1 APK uses the validated CI/debug signing identity and is not represented as Google Play/production-signed.
- The Android source ZIP is generated from the exact tracked release tree and checked to exclude build/cache material.

## Browser-extension controls

All Chrome, Edge, Opera and Firefox packages use version 0.1.1 and share the same local-first capture runtime except for the expected Firefox manifest background/Gecko metadata.

Release validation enforces:

- exact permissions: `activeTab`, `scripting`, `downloads`, `storage`;
- no `<all_urls>` and no broad `host_permissions`;
- no remote runtime scripts, telemetry endpoints, `eval` or `new Function`;
- EN/HR locale parity;
- referenced runtime assets and exact 16/32/48/128 icon dimensions;
- byte-identical shared source across browser variants;
- deterministic ZIP construction in two independent packaging passes;
- byte-for-byte reproducibility and SHA-256 verification;
- package cleanliness (no common build/cache/source-map artifacts);
- store metadata/privacy declarations matched against the actual manifests;
- deterministic listing/promo image generation.

Screenshot pixels are processed locally by the extension. The extension does not provide a cloud uploader, analytics, advertising SDK, account system or background network client.

## Release integrity

The v0.1.1 release workflow does not create the `v0.1.1` tag until Android, browser and Windows release gates have succeeded.

Before publication it requires exactly:

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

SHA-256 is calculated for every final asset. After GitHub Release publication, GitHub's published digest for every asset is compared with the locally validated hash. Existing release assets are not silently replaced.

## Performance model

SNAPVERE intentionally avoids permanent capture/network polling:

- Windows tray/hotkey processing is event-driven;
- Direct3D/capture resources are created for active work instead of being continuously active;
- Android has no idle screen-recording loop or background network worker;
- browser-extension capture logic starts after explicit user action and full-page work is bounded by tile/canvas/pixel limits.

No fixed CPU/RAM percentage is promised. Resource use depends on screen/page dimensions, monitor count and DPI, graphics driver, browser engine, Android OEM implementation and the active capture/edit workload.

## Evidence boundary

Green CI/release automation proves the source/build/test/package/signature/digest checks that are explicitly executed. It is not a guarantee that the software is free from every vulnerability or defect, and it is not exhaustive runtime coverage of every Windows machine, Android device/OEM, browser build or web application.

External browser-store publication/review is outside the GitHub Release pipeline and must not be inferred from a successful v0.1.1 GitHub Release.
