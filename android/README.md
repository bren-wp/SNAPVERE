# SNAPVERE Android

Native Android companion for SNAPVERE with the same local-first privacy model as the Windows application. Current release line: **0.1.0** (`versionCode 10`). Screen capture starts only after explicit user action and Android's MediaProjection consent.

## Implemented application

- Native Android 10+ application under `android/`
- compile/target SDK 36, Java 17
- full-screen capture through Android MediaProjection
- fresh system consent for every capture; consent intents/tokens are never reused
- foreground `mediaProjection` service only while one user-approved capture is active
- Activity-to-background handoff before VirtualDisplay creation
- five-second task-hide guard and seven-second first-frame guard
- single-active-capture ownership with stale-teardown protection
- exception-safe MediaProjection / VirtualDisplay / ImageReader / Image / HandlerThread cleanup
- validated RGBA pixel stride, row stride, row-padding alignment and available buffer size before bitmap copy
- PNG output through MediaStore into `Pictures/SNAPVERE`
- validated Open / Share / Delete actions for the latest capture
- user-initiated Website / Support / Privacy / Terms actions with controlled fallback states
- English and Croatian UX/recovery resources
- no account, telemetry, analytics, cloud upload, automatic background capture or `INTERNET` permission
- app backup/device transfer disabled for SNAPVERE private state

## 0.1.0 stability changes

0.1.0 tightens the capture path around OEM/provider edge cases:

- foreground-service initialization faults are contained and surfaced as capture failure rather than intentionally escaping service creation;
- rejected Handler scheduling cannot leave capture ownership indefinitely active;
- acquired Images are closed before final completion cleanup;
- the pixel buffer is rewound and its declared byte requirement is verified before copy;
- `RGBA_8888` capture rejects an unexpected pixel stride or partial-pixel row padding;
- conversion/provider failures and allocation failure are contained by the capture session cleanup path;
- only the owning service instance may release the global capture guard;
- user-facing recovery text is localized instead of exposing internal provider exception messages.

`CaptureBufferLayoutTest` covers tight/padded layouts, invalid dimensions, unexpected pixel stride, partial padding and overflow.

## UI / UX

The Android app shares SNAPVERE's dark product identity: near-black canvas, layered surfaces, violet accent and green success state. OEM `forceDark` is disabled.

The interface contains the identity header, primary Capture card, accessibility-aware status, Latest Capture actions, Private by Design explanation and About/support/legal actions. It is scrollable and system-inset aware. Paired actions stack vertically on narrow screens or when Android font scale reaches 1.25x. Buttons retain at least 52 dp touch height and explicit disabled states.

## Development CI

Generated APKs are not committed to Git. `.github/workflows/android-ci.yml` runs:

```bash
gradle -p android --no-daemon clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease
```

The workflow also validates the manifest privacy/service/version contract, SDK 36 / Build Tools availability, debug APK signature, ZIP alignment and SHA-256.

CI uploads:

```text
SNAPVERE-Android-0.1.0-debug.apk
SNAPVERE-Android-0.1.0-debug.apk.sha256
```

inside `snapvere-android-ci-apk-<commit-sha>`.

## Public v0.1.0 APK

For v0.1.0 the public `SNAPVERE.apk` deliberately uses the same CI/debug-signed APK path that is already validated by Android CI. No private production keystore or repository signing secret is required.

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

This package is installable, but it is **not** represented as Google Play/production-signed. Its signing identity is not a supported long-term production upgrade contract. If a later Android channel uses a different stable production key, Android may require uninstall/reinstall before installing that differently signed package.

## Android source release asset

The same validated Git commit is archived directly with `git archive` and published as:

```text
SNAPVERE-Android-Source.zip
```

The archive contains tracked Android source/configuration and excludes generated build output, Gradle caches and signing material.

## v0.1.0 release contract

The final GitHub Release is accepted only when it contains exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

SHA-256 values are checked during Android artifact transfer, recomputed for all final assets and compared with GitHub's published asset digests.

## Privacy and security

The manifest intentionally has no `android.permission.INTERNET`. Cleartext is disabled, backup is disabled and `CaptureService` is non-exported with `foregroundServiceType="mediaProjection"`. Captures are stored through MediaStore without broad filesystem permission.

External website/mail/legal destinations are opened only after explicit user action. SNAPVERE does not add a first-party network client merely because those buttons exist.

## Scope boundary

Android 0.1.0 is complete for the implemented full-screen MediaProjection workflow. Windows-style general top-level Window Capture and the desktop Region annotation editor are not falsely represented as Android features.

A green CI/release workflow proves source/build/lint/unit/package/signature/digest automation. It is not a claim of exhaustive runtime coverage across every physical OEM device.

See [`../docs/ANDROID.md`](../docs/ANDROID.md) and [`../docs/hr/ANDROID.md`](../docs/hr/ANDROID.md) for the detailed architecture and QA contract.
