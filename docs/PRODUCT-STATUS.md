# SNAPVERE Product Status

Last aligned with the public **SNAPVERE 0.1.1** release and the post-release `main` maintenance line.

## Public release

Official release: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

The v0.1.1 release is published and contains exactly eight validated public assets:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`
- `SNAPVERE.apk`
- `SNAPVERE-Android-Source.zip`
- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

The release workflow validates local SHA-256 values and then compares them with GitHub's published asset digests.

## Windows status

**Implemented and release-packaged:** tray-first launch, region capture/editor, window capture, screen capture, settings, recent-capture workflow, language selection, Setup, Portable and universal x86/x64/ARM64 payload packaging.

**Automated evidence:** x64 build/tests, x86 build, ARM64 cross-build/package validation, native payload validation, rendered UI snapshots, PR visual comparison, Setup/Portable package contract, x64/x86 lifecycle and tray-first probes.

**Important boundary:** ARM64 is validated as a cross-build and packaged payload on hosted x64 CI. This is not a claim of exhaustive physical ARM64 hardware runtime testing. Public Windows executables are not represented as having a commercial Authenticode reputation/signing identity.

## Android status

**Implemented and release-packaged:** Android 10+ native Java capture app, fresh MediaProjection consent per capture, local MediaStore PNG save, latest-capture Open/Share/Delete, EN/HR resources, privacy/about actions and bounded capture cleanup/timeouts.

**Automated evidence:** privacy/service/version manifest checks, SDK/tooling check, lintDebug/lintRelease, JVM unit tests, debug/release build, APK signature/alignment verification, source archive validation and release digest checks.

**Important boundary:** public `SNAPVERE.apk` is CI/debug-signed. It is not represented as Google Play production-signed, and no Google Play publication is claimed.

## Browser-extension status

**Implemented and release-packaged:** Chrome, Edge, Opera and Firefox packages with visible-area, region and bounded full-page capture, EN/HR locales, local options, local processing and minimal permission contract.

**Automated evidence:** manifest validation, exact permission allow-list, source parity, icon dimensions, locale parity, syntax/policy checks, deterministic store graphics, store metadata/privacy validation, reproducible double packaging and SHA-256 validation.

**Important boundary:** GitHub ZIP publication does not mean the extensions are listed, signed or approved in Chrome Web Store, Microsoft Edge Add-ons, Opera Add-ons or Mozilla Add-ons. External authenticated publisher accounts and store review/signing remain separate.

## Privacy status

- Windows: screenshot processing and first-party capture flow are local; no first-party telemetry/cloud-upload worker is part of the capture runtime.
- Android: no `android.permission.INTERNET`; backups and cleartext are disabled; MediaProjection service is non-exported.
- Browser: no telemetry, analytics, advertising SDK, remote runtime dependency, `<all_urls>` or broad host permissions in the current extension contract.

See [Privacy](PRIVACY.md) and [Security Policy](../SECURITY.md).

## Documentation status

Active documentation is version-aligned through [`product-version.json`](../product-version.json) and `eng/validate-product-contract.py`. Product Contract CI checks current-version consistency, Android EN/HR resource-key parity, browser version parity, exact release asset names and relative Markdown links.

Historical release notes stay historical and may correctly describe older version numbers/features.

## Support and issue reporting

See [Troubleshooting](TROUBLESHOOTING.md) before reporting an issue. Include platform/version/architecture, exact capture mode and reproducible steps.

Support: **info@snapvere.com**
