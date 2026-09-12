# SNAPVERE 0.1.1 release contract

This document is the repository-side checklist for the SNAPVERE 0.1.1 GitHub Release.

## Version matrix

| Component | Version |
| --- | --- |
| Windows product/file/assembly | 0.1.1 / 0.1.1.0 |
| Android | versionName 0.1.1 / versionCode 11 |
| Chrome extension | 0.1.1 |
| Edge extension | 0.1.1 |
| Opera extension | 0.1.1 |
| Firefox extension | 0.1.1 |

## Exact public asset contract

A valid v0.1.1 release contains exactly eight files:

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

No store-submission graphics, CI checksum manifests, intermediate payload ZIPs, signing material or build caches belong in the public GitHub Release.

## Required validation before tag creation

- Windows audited restore/build/test on x64 plus x86 and ARM64 build validation.
- Architecture-specific native payload and SHA-256 integrity-manifest validation.
- Universal Windows Setup/Portable generation and x64/x86 lifecycle/tray-first probes.
- Android privacy/service/version checks, `lintDebug`, `lintRelease`, JVM tests and debug/release builds.
- Android public APK signature/alignment and SHA-256 checks.
- Android source archive structural validation.
- Chrome/Edge/Opera/Firefox manifest, permission, source-policy and locale checks.
- Cross-browser source parity and exact icon-dimension checks.
- Store metadata/privacy contract validation.
- Two independent browser packaging passes with byte-for-byte reproducibility.
- Browser package SHA-256 verification.
- Cross-job Android/browser transfer-digest verification.
- Exact eight-file final release directory validation.

Only after these gates succeed may the workflow create or verify the immutable `v0.1.1` tag.

## Publication verification

After release creation, automation requires:

- `v0.1.1` is not draft or prerelease;
- exactly the eight approved asset names are published;
- GitHub provides a digest for every asset;
- every published GitHub SHA-256 digest matches the locally validated value.

Existing release assets are never silently replaced.

## Signing boundary

The public Android `SNAPVERE.apk` uses the validated CI/debug signing identity. It is installable but is not represented as Google Play/production-signed.

Browser ZIP packages are GitHub distribution artifacts. Publication/signing/approval in Chrome Web Store, Microsoft Edge Add-ons, Opera Add-ons or Mozilla Add-ons remains an external publisher process and is not implied by this release.

## Historical boundary

The historical v0.1.0 tag/release remains immutable and keeps exactly its original four Windows/Android assets. Browser ZIP packages start with v0.1.1 and must not be retroactively attached to v0.1.0.
