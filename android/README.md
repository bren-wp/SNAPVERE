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

CI builds an installable debug APK named:

`SNAPVERE-Android-0.0.9-debug.apk`

The requested generated APK is kept at the root of this directory after a successful build and verification. Debug signing is intentionally treated as development/internal distribution only. A production release APK must use a separately managed release-signing key; signing secrets must never be committed to this repository.

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

## Privacy/security notes

SNAPVERE does not attempt to bypass Android screen-capture controls. Each capture uses a fresh system consent flow. The capture service is not exported and is declared with the `mediaProjection` foreground-service type. Captures are written through Android `MediaStore`; no broad filesystem permission is requested.

## Next Android milestones

The first functional Android milestone intentionally focuses on reliable full-screen capture. Region selection, annotation tools, richer capture history, Android-specific settings, release signing, and device/emulator runtime QA should be added in later reviewable increments rather than being represented as complete before they are implemented and tested.
