# SNAPVERE QA Matrix

This matrix documents the automated evidence used for the **SNAPVERE 0.1.1** product line. A green workflow is evidence for the checks listed here; it is not a claim that every device, graphics driver, browser page or OEM configuration has been manually tested.

| Area | Automated checks | Evidence boundary |
| --- | --- | --- |
| Windows source | NuGet audit, analyzers, x64 build, xUnit tests | Does not replace all real-world Windows configurations |
| Windows x86 | Restore/build, package payload, Setup/Portable lifecycle | Runtime evidence is on hosted Windows runner |
| Windows x64 | Build, tests, payload, Setup/Portable lifecycle, tray-first probes | Hosted runner cannot represent every driver/display stack |
| Windows ARM64 | Restore/build and payload/package validation | Cross-build evidence; not physical ARM64 runtime proof |
| Windows UI | Six real rendered UI surfaces, PR-to-main visual comparison | Screenshot comparison is not full accessibility/usability testing |
| Packaging | Architecture payload validation, integrity manifests, two-file public contract | Does not provide commercial code-signing reputation |
| Android manifest | Version, service/export, privacy and permission contract | Does not cover OEM policy differences |
| Android source | lintDebug, lintRelease, JVM unit tests | JVM tests do not emulate full MediaProjection runtime behavior |
| Android APK | Debug/release builds, signature, zip alignment | Public APK is CI/debug-signed, not Google Play production-signed |
| Android source ZIP | Tracked source archive validation, cache/build-output rejection | Archive validation does not publish to an app store |
| Browser manifests | MV3 contract, exact permissions, no host_permissions | Browser policy can still change externally |
| Browser source | JavaScript syntax, forbidden-pattern policy, EN/HR locale parity | Static checks are not a full browser GUI session |
| Browser parity | Chromium byte parity, normalized Firefox differences, icon hashes/dimensions | Expected browser-engine behavior can still differ |
| Browser packaging | Two independent package passes, byte-for-byte comparison, SHA-256 | Reproducible in the same CI environment |
| Browser store kit | Listing/privacy/permission metadata and deterministic graphics | Store approval/signing remains external |
| Product contract | Windows/Android/browser version parity, docs/index/release asset contract, Markdown links | Historical docs may intentionally reference older versions |
| Release | Exact eight public assets and local SHA-256 | Release checks do not imply third-party store publication |
| Post-publication | GitHub asset names/digests compared to locally validated files | Verifies GitHub release integrity at publication time |

## Windows workflow details

The Windows CI line validates the application across x86, x64 and ARM64 payload generation, then builds universal Setup/Portable hosts. PRs additionally compare rendered UI against the last successful `main` baseline when the baseline is available. Lifecycle tests validate extraction/install behavior and tray-first launch contracts on x64/x86 hosted Windows runners.

## Android workflow details

Android CI validates SDK/tool availability, privacy/service/version constraints, both lint variants, JVM tests and both debug/release builds. The public release process intentionally copies the validated debug-signed APK to `SNAPVERE.apk`, verifies its signature/alignment and records its digest. This signing limitation is documented rather than hidden.

## Browser workflow details

Extension CI validates source/manifest policy, parity and store metadata, regenerates deterministic store imagery and packages all four variants twice. Corresponding ZIPs must compare byte-for-byte and pass SHA-256 validation before upload.

## Product Contract CI

`eng/validate-product-contract.py` uses [`product-version.json`](../product-version.json) as the active product contract. It checks:

- .NET product/assembly/file version;
- Android versionCode/versionName/minSdk/targetSdk/compileSdk;
- Android English/Croatian string-key parity;
- Chrome/Edge/Opera/Firefox manifest version;
- browser store listing version and publication status declaration;
- exact v0.1.1 eight-file release contract;
- active README release links/assets;
- current documentation indexes;
- relative Markdown links in active documentation.

## Manual verification still valuable

Before a future release, real-device/manual testing is still useful for:

- physical ARM64 Windows hardware;
- mixed-DPI/multi-monitor configurations and unusual GPUs;
- multiple Android OEMs/display modes;
- current stable versions of all four browsers on representative dynamic pages;
- accessibility with keyboard, screen reader and high text scaling;
- external store installation/signing flows once publisher accounts are connected.

Automated checks reduce regressions; they do not justify unsupported claims of universal compatibility or zero defects.
