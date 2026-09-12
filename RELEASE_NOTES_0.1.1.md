# SNAPVERE 0.1.1

SNAPVERE 0.1.1 promotes the browser-extension work completed after 0.1.0 into the public release line while preserving the validated Windows and Android capture applications.

## What is new

### Browser extensions

0.1.1 adds public source-ready browser packages for:

- Google Chrome;
- Microsoft Edge;
- Opera;
- Mozilla Firefox.

All four variants provide user-initiated capture of the visible viewport, bounded full-page capture through local scrolling/stitching, and rectangular region capture. Screenshot pixels are processed locally in the browser and saved as PNG files. The extension source contains no telemetry, analytics, advertising SDK, cloud-upload client or remote runtime dependency.

Chromium-family builds use Manifest V3 service workers. Firefox uses a compatible Manifest V3 WebExtension background-script model. All variants request only `activeTab`, `scripting`, `downloads` and `storage`, with no `<all_urls>` or broad host permission.

English and Croatian extension interfaces are included.

### Browser package integrity and store readiness

The browser release path now requires:

- manifest/permission/privacy validation;
- Chrome/Edge/Opera/Firefox runtime-source parity checks;
- JavaScript syntax validation;
- deterministic ZIP packaging performed twice;
- byte-for-byte package reproducibility;
- package-content validation;
- SHA-256 verification;
- store listing metadata, permission justifications and privacy declarations validated against the actual manifests;
- deterministic store screenshot/promo generation for submission preparation.

Store publication itself remains a separate external process that requires authenticated publisher accounts, review/certification and, where applicable, store signing. This GitHub Release does not claim Chrome Web Store, Edge Add-ons, Opera Add-ons or Mozilla Add-ons approval.

## Windows

- Product/file/assembly version is 0.1.1.
- Tray-first Windows behavior, Region/Window/Screen capture, local PNG workflow and universal x86/x64/ARM64 packaging remain intact.
- NuGet audit, analyzer/warnings-as-errors policy, architecture payload integrity manifests, rendered UI QA and x64/x86 Setup/Portable lifecycle probes remain required validation gates.

## Android

- Android version is 0.1.1 / `versionCode 11`.
- The local-first MediaProjection workflow and privacy contract remain unchanged: no `INTERNET` permission, no background continuous recording, backup disabled, cleartext disabled and the capture service non-exported.
- The public `SNAPVERE.apk` continues to use the validated CI/debug signing identity. It is installable but is not represented as Google Play/production-signed.
- Debug/release lint, JVM tests, debug/release builds, APK signature/alignment checks and SHA-256 validation remain release gates.

## Public release files

A valid v0.1.1 GitHub Release contains exactly these eight public assets:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`
- `SNAPVERE.apk`
- `SNAPVERE-Android-Source.zip`
- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

The historical v0.1.0 release remains immutable with its original four Windows/Android assets. Browser packages are introduced as public release assets starting with v0.1.1 and are not retroactively attached to v0.1.0.

## Release validation boundary

Automation validates source versions, Windows builds/tests/packages, Android lint/tests/builds/signature/alignment, browser manifest/privacy/parity/reproducibility, exact public asset names and SHA-256 digests before and after GitHub publication.

ARM64 Windows remains cross-build/package evidence on hosted x64 runners. A green CI/release pipeline is not a claim of exhaustive runtime coverage across every Windows hardware configuration, Android OEM/device, web application or browser build.

## SHA-256 digests

The release workflow appends the validated SHA-256 digest for every one of the eight published assets to these release notes at publication time.
