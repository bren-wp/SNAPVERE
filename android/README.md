# SNAPVERE Android

Native Android companion for SNAPVERE with the same local-first privacy model as the Windows application.

Current release line: **0.1.1** (`versionCode 11`). Screen capture starts only after explicit user action and Android's MediaProjection consent.

## Implemented application

- Native Android 10+ application under `android/`;
- compile/target SDK 36, Java 17;
- full-screen capture through Android MediaProjection;
- fresh system consent for every capture; consent intents/tokens are never reused;
- foreground `mediaProjection` service only while one user-approved capture is active;
- Activity-to-background handoff before VirtualDisplay creation;
- five-second task-hide guard and seven-second first-frame guard;
- single-active-capture ownership with stale-teardown protection;
- exception-safe MediaProjection / VirtualDisplay / ImageReader / Image / HandlerThread cleanup;
- validated RGBA pixel stride, row stride, row-padding alignment and available buffer size before bitmap copy;
- PNG output through MediaStore into `Pictures/SNAPVERE`;
- validated Open / Share / Delete actions for the latest capture;
- English and Croatian UX/recovery resources;
- no account, telemetry, analytics, cloud upload, automatic background capture or `INTERNET` permission;
- app backup/device transfer disabled for SNAPVERE private state.

## 0.1.1 release version

`android/app/build.gradle` defines:

```text
versionCode 11
versionName '0.1.1'
```

0.1.1 preserves the hardened capture path established in 0.1.0 while moving the Android package into the new combined Windows/Android/browser release contract.

## Capture-path stability

The application keeps these validated controls:

- foreground-service initialization faults are converted into controlled capture failures;
- rejected Handler scheduling cannot leave capture ownership indefinitely active;
- only the owning service instance may release the global capture guard;
- acquired Images are closed before final completion cleanup;
- the pixel buffer is rewound and its declared byte requirement is validated before copy;
- `RGBA_8888` capture rejects unexpected pixel stride or partial-pixel row padding;
- conversion/provider/allocation failures remain inside capture-session recovery;
- user-facing recovery text is localized rather than exposing raw provider exception details.

`CaptureBufferLayoutTest` covers tight/padded layouts, invalid dimensions, unexpected pixel stride, partial padding and overflow.

## Development CI

Generated APKs are not committed to Git. `.github/workflows/android-ci.yml` runs:

```bash
gradle -p android --no-daemon clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease
```

CI also validates the manifest privacy/service/version contract, SDK 36 / Build Tools availability, debug APK signature, ZIP alignment and SHA-256.

For the 0.1.1 source line the CI artifact naming follows the versioned Android build produced from the same tracked source commit.

## Public v0.1.1 APK

The public `SNAPVERE.apk` deliberately uses the validated CI/debug-signed APK path. No private production keystore is required or claimed.

The release workflow still builds **both** debug and release variants and requires:

- `lintDebug` and `lintRelease`;
- JVM unit tests;
- successful debug and release builds;
- a non-empty signed debug APK;
- `apksigner` verification;
- ZIP-alignment verification;
- SHA-256 transfer and publication checks.

The published file is:

```text
SNAPVERE.apk
```

This package is installable but is **not** represented as Google Play/production-signed. If a future Android distribution channel uses a different stable production key, Android may require uninstall/reinstall before installing that differently signed package.

## Android source release asset

The validated Git commit is archived with `git archive` and published as:

```text
SNAPVERE-Android-Source.zip
```

The archive contains tracked Android source/configuration and excludes generated build output, Gradle caches and signing material.

## v0.1.1 combined release contract

Android contributes these two files to the eight-asset v0.1.1 GitHub Release:

```text
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

The complete release also contains Windows Setup/Portable and four browser-extension ZIPs. SHA-256 values are checked during artifact transfer, recomputed for all final assets and compared with GitHub's published asset digests.

Historical v0.1.0 remains unchanged with its original four-asset contract.

## Privacy and security

The manifest intentionally has no `android.permission.INTERNET`. Cleartext is disabled, backup is disabled and `CaptureService` is non-exported with `foregroundServiceType="mediaProjection"`. Captures are stored through MediaStore without broad filesystem permission.

External website/mail/legal destinations open only after explicit user action. SNAPVERE does not add a first-party network client merely because those buttons exist.

## Scope boundary

Android 0.1.1 is complete for the implemented full-screen MediaProjection workflow. Windows-style general top-level Window Capture and the desktop Region annotation editor are not represented as Android features.

A green CI/release workflow proves the executed source/build/lint/unit/package/signature/digest automation. It is not exhaustive runtime coverage across every physical OEM device.

See [`../docs/ANDROID.md`](../docs/ANDROID.md), [`../docs/hr/ANDROID.md`](../docs/hr/ANDROID.md) and [`../docs/SECURITY-PERFORMANCE-0.1.1.md`](../docs/SECURITY-PERFORMANCE-0.1.1.md).
