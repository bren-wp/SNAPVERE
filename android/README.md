# SNAPVERE Android

Native Android companion for SNAPVERE. The Android app follows the same local-first privacy model as the Windows application: capture starts only after explicit user interaction and Android's system MediaProjection consent.

## Current implemented scope

- Native Android application under `android/`
- Android 10+ (`minSdk 29`), compiled/targeted against API 36
- Full-screen capture through the official Android `MediaProjection` API
- Foreground media-projection service only while a user-approved capture is being completed
- PNG output through `MediaStore` to `Pictures/SNAPVERE`
- Open and Share actions for the most recent capture
- English and Croatian UI resources with Android's normal fallback model
- No account, telemetry, analytics, cloud upload, background capture, `INTERNET` permission or cleartext traffic
- App backup/device-transfer disabled for SNAPVERE app-private state

## APK

The CI-verified internal/debug APK is committed at:

`android/SNAPVERE-Android-0.0.9-debug.apk`

The matching digest file is:

`android/SNAPVERE-Android-0.0.9-debug.apk.sha256`

Current committed APK SHA-256:

`f9df90450eb52a73cdfeccc1c12d4a5e197716f9c625cd0e867b6cf11d368bff`

The committed binary came directly from a successful GitHub Actions Android build after `lintDebug`, `assembleDebug`, APK signature verification and zip alignment verification completed successfully. Debug signing is intentionally treated as development/internal distribution only. A production release APK must use a separately managed release-signing key; signing secrets must never be committed to this repository.

## Build

The CI toolchain is pinned to:

- JDK 17
- Android Gradle Plugin 8.10.1
- Gradle 8.11.1
- Android SDK 36
- Android SDK Build Tools 35.0.0

From a configured Android build environment:

```bash
gradle -p android --no-daemon clean lintDebug assembleDebug
```

The raw Gradle output is written to `android/app/build/outputs/apk/debug/app-debug.apk`.

CI additionally verifies the generated APK with Android SDK `apksigner` and `zipalign`, then publishes the APK and SHA-256 digest as an Actions artifact.

## Privacy/security notes

SNAPVERE does not attempt to bypass Android screen-capture controls. Each capture uses a fresh system consent flow. The capture service is not exported and is declared with the `mediaProjection` foreground-service type. Captures are written through Android `MediaStore`; no broad filesystem permission is requested.

The Android CI workflow is read-only against repository contents during normal operation. The one-time write-capable artifact-import job used to place the explicitly requested verified APK in `android/` was removed immediately after that binary was committed.

## Next Android milestones

The first functional Android milestone intentionally focuses on reliable full-screen capture. Region selection, annotation tools, richer capture history, Android-specific settings, release signing, and device/emulator runtime QA should be added in later reviewable increments rather than being represented as complete before they are implemented and tested.
