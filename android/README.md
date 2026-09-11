# SNAPVERE Android

Native Android companion for SNAPVERE with the same local-first privacy model as the Windows application. Screen capture starts only after explicit user action and Android's system MediaProjection consent.

## Implemented application

- Native Android application under `android/`
- Android 10+ (`minSdk 29`), compile/target SDK 36
- Full-screen capture through the official Android `MediaProjection` API
- Lifecycle handoff that waits for `MainActivity.onStop()` before creating the capture virtual display, so SNAPVERE is moved behind the screen being captured
- Foreground media-projection service only while one user-approved capture is active
- Single-active-capture guard and serialized teardown of MediaProjection, VirtualDisplay, ImageReader and capture thread resources
- PNG output through `MediaStore` into `Pictures/SNAPVERE`
- Latest-capture card with validated Open, Share and Delete actions
- Stale MediaStore URI detection so buttons are disabled instead of pointing at a missing image
- Native confirmation before deleting the latest capture
- User-initiated Website, Support, Privacy and Terms links
- Support fallback copies `info@snapvere.com` when no mail client is registered
- English and Croatian UI resources using Android's normal resource fallback model
- No account, telemetry, analytics, cloud upload, automatic background capture or `INTERNET` permission
- App backup/device-transfer disabled for SNAPVERE private state

## Dark UI / UX

The Android app shares the core dark SNAPVERE palette with the Windows distribution rather than maintaining a separate visual identity. Canonical desktop values live in `src/Snapvere.App/App.xaml`; Android mirrors the core tokens in `res/values/colors.xml`:

- canvas `#0B0D12`
- surface `#12151C`
- raised surface `#181C25`
- border `#2A3140`
- primary text `#F6F7FB`
- secondary text `#98A2B3`
- muted text `#727C90`
- violet accent `#7C6CFF`
- success `#45D6A2`

The interface is organized into five clear surfaces:

1. SNAPVERE identity/header with Android-local badge
2. capture card with live status and primary Capture action
3. latest-capture card with Open, Share and Delete
4. private-by-design explanation
5. About/support/legal actions

Buttons use a minimum 52 dp touch height, native ripple feedback and explicit disabled states. The page is system-inset aware and scrollable for small screens and larger text. Phone layouts keep compact horizontal spacing while tablet-class layouts use 48 dp horizontal padding. Status changes use an accessibility live region.

## Capture behavior

Each capture uses a fresh Android MediaProjection consent token. SNAPVERE starts the foreground service while the Activity is still visible, requests `moveTaskToBack(true)`, and starts the VirtualDisplay only after `MainActivity.onStop()` confirms that the SNAPVERE task is no longer visible. A bounded timeout is failure protection only; it never substitutes for the lifecycle handoff.

A process-local guard prevents overlapping capture sessions. Destruction atomically closes the session and serializes cleanup onto the capture handler when required, preventing a new capture from racing teardown of the previous one.

## APK and source-of-truth policy

Generated APK binaries are **not committed to the source tree**. The canonical development APK is the artifact produced by the final green GitHub Actions `Android CI` run for the exact source commit being evaluated.

CI publishes:

- `SNAPVERE-Android-0.0.9-debug.apk`
- `SNAPVERE-Android-0.0.9-debug.apk.sha256`

inside an artifact named `snapvere-android-apk-<commit-sha>`.

This avoids leaving a stale binary in the repository after source changes. The debug APK is an internal/development build. Production publication still requires a separately managed release-signing key; signing secrets are never committed to the repository.

## Build and validation

Pinned CI toolchain:

- JDK 17
- Android Gradle Plugin 8.10.1
- Gradle 8.11.1
- Android SDK 36
- Android SDK Build Tools 35.0.0

CI performs all of the following:

1. validates the manifest privacy/service contract (`INTERNET` forbidden, cleartext disabled, backup disabled, non-exported mediaProjection service required)
2. runs Android lint with warnings treated as errors
3. builds the debug APK
4. builds the minified/shrunk release variant as compile/shrinker evidence
5. verifies the debug APK with `apksigner`
6. verifies APK alignment with `zipalign`
7. computes SHA-256
8. uploads the APK and digest as a GitHub Actions artifact

Equivalent local build command for a configured Android toolchain:

```bash
gradle -p android --no-daemon clean lintDebug assembleDebug assembleRelease
```

The raw debug APK is written to:

```text
android/app/build/outputs/apk/debug/app-debug.apk
```

## Privacy and security

The manifest intentionally contains no `android.permission.INTERNET`. SNAPVERE does not fetch remote configuration, upload screenshots, send analytics, register a remote command channel or perform background capture. Website/support/legal actions are explicit user actions delegated to Android's browser/mail handlers.

The capture service is `android:exported="false"`, uses the `mediaProjection` foreground-service type and stops with the app task. Captures are stored through MediaStore without broad filesystem permissions.

## Scope

This Android milestone is a complete, polished full-screen capture application for the implemented Android capture model. Android does not expose the same top-level-window capture primitive used by SNAPVERE on Windows, so Windows-style Window Capture is not represented as implemented on Android. Region selection/annotation parity remains a distinct future feature rather than being falsely documented as complete.

For the detailed architecture, UI contract and QA evidence policy, see [`docs/ANDROID.md`](../docs/ANDROID.md) and [`docs/hr/ANDROID.md`](../docs/hr/ANDROID.md).
