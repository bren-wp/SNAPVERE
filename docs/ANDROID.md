# SNAPVERE Android architecture, UI and QA

## Product contract

SNAPVERE for Android is a native, local-first screen-capture application. It does not require an account and does not upload capture pixels. Every capture begins with explicit user interaction and a fresh Android MediaProjection consent flow.

Current release line: **0.1.1** (`versionCode 11`). Supported baseline:

- Android 10+ / API 29+;
- compileSdk / targetSdk 36;
- Java 17;
- Android Gradle Plugin 8.10.1;
- Gradle 8.11.1.

## User flow

1. User opens SNAPVERE.
2. The dark home surface shows capture readiness and the latest readable local capture.
3. User taps **Capture screen**.
4. Android displays the system MediaProjection consent UI.
5. After consent, SNAPVERE starts its `mediaProjection` foreground service while the Activity is still foreground.
6. SNAPVERE requests `moveTaskToBack(true)`.
7. `MainActivity.onStop()` signals that SNAPVERE is no longer visible.
8. Only then does the service create the VirtualDisplay and ImageReader.
9. The first completed frame is validated, converted from the RGBA image plane into a visible ARGB bitmap and written as PNG through MediaStore.
10. The latest capture URI/name is kept in private SharedPreferences for Open/Share/Delete convenience.
11. Image, projection, display, reader, callbacks and thread resources are released and the foreground service stops.

A five-second task-hide timeout and seven-second first-frame timeout bound stalled capture paths. Neither timeout starts another capture or bypasses Android consent.

Android 14+ requires fresh user consent for each MediaProjection session and one `createVirtualDisplay()` use per MediaProjection instance. SNAPVERE does not cache/reuse consent tokens and registers `MediaProjection.Callback.onStop()` for controlled teardown.

## Lifecycle hardening retained in 0.1.1

0.1.1 keeps the hardened service/capture behavior introduced in the 0.1.0 line:

- notification/service initialization failures become controlled capture failures;
- Handler scheduling failures cannot leave capture ownership indefinitely active;
- MediaProjection, VirtualDisplay and frame-acquisition failures return localized recovery messages;
- `ImageReader.acquireLatestImage()` remains guarded;
- the acquired `Image` is closed before final session cleanup is committed;
- bitmap conversion contains allocation/provider failures, including `OutOfMemoryError`;
- only the owning service instance may release process-local capture ownership;
- cleanup is idempotent and each Android resource is released independently.

## Capture-buffer validation

ImageReader uses `PixelFormat.RGBA_8888`. Before bitmap allocation/copy SNAPVERE validates:

- positive visible width;
- exact 4-byte RGBA pixel stride;
- positive row stride large enough for the visible row;
- complete-pixel row-padding alignment;
- overflow-safe padded-width/row arithmetic;
- `ByteBuffer.rewind()` before copy;
- at least `rowStride × height` available bytes.

`CaptureBufferLayout` JVM regression tests cover tight/padded rows, invalid dimensions, unexpected pixel stride, partial padding and overflow.

## Storage and UI

Captures are saved through MediaStore as `image/png` under:

```text
Pictures/SNAPVERE
```

No broad storage permission is requested. Pending-state finalization is checked and cleanup failures cannot replace the original save error.

The latest-capture UI revalidates its URI before Open, Share or Delete. Stale local metadata is removed instead of leaving broken actions visible.

The UI uses the SNAPVERE dark palette, disables OEM `forceDark`, respects system-bar insets and remains scrollable. Paired actions stack vertically on narrow displays or at Android font scale 1.25x+. Buttons retain at least 52 dp touch height.

## Manifest privacy contract

CI/release fails if the manifest adds `android.permission.INTERNET`. It also requires:

- `FOREGROUND_SERVICE_MEDIA_PROJECTION`;
- `CaptureService` with `android:exported="false"`;
- `CaptureService` with `android:foregroundServiceType="mediaProjection"`;
- cleartext disabled;
- app backup disabled;
- `versionName 0.1.1` / `versionCode 11`.

There is no telemetry, analytics SDK, advertising SDK, cloud-upload client, WebView or remote-command channel in the Android release line.

## Continuous integration APK

`.github/workflows/android-ci.yml` runs:

```text
clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease
```

Lint warnings are errors. CI validates the privacy/service/version contract, SDK 36 / Build Tools, debug APK signature, ZIP alignment and SHA-256, then uploads the versioned 0.1.1 debug APK and digest inside `snapvere-android-ci-apk-<commit-sha>`.

## Public 0.1.1 Android release

The public `SNAPVERE.apk` uses the validated CI/debug-signed package path. The release does **not** claim a private production keystore or Google Play signing identity.

The release workflow still requires:

- Android privacy/service/version validation;
- `lintDebug` and `lintRelease`;
- JVM unit tests;
- successful debug and release builds;
- a signed, non-empty debug APK;
- `apksigner` verification;
- ZIP-alignment verification;
- SHA-256 verification across Actions artifact transfer and GitHub publication.

This APK is installable but its signing identity is not a permanent production-upgrade contract. A future channel signed with another stable production key may require uninstall/reinstall.

The workflow also creates from the exact validated Git tree:

```text
SNAPVERE-Android-Source.zip
```

The archive contains tracked `android/` source/configuration and excludes generated build output, Gradle caches and signing material.

## v0.1.1 combined public asset contract

Android contributes two files to the eight-asset v0.1.1 release:

```text
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

The same GitHub Release also contains Windows Setup/Portable and the Chrome/Edge/Opera/Firefox ZIPs. SHA-256 values are checked during artifact transfer, recomputed for all final assets and compared with GitHub's published digests.

Historical v0.1.0 remains immutable with its original four-asset contract.

## Evidence boundaries

A green Android CI proves compilation, lint, JVM tests, debug/release builds, debug APK signature/alignment and artifact generation. A green v0.1.1 release additionally proves source-archive structure, cross-job digest transfer and final GitHub asset digest checks.

This is not exhaustive physical-device coverage across every OEM, Android skin, resolution or permission implementation.

## Platform parity boundary

Android 0.1.1 is complete for the implemented full-screen MediaProjection workflow. Windows-style top-level Window Capture and the desktop Region annotation editor are not claimed as Android features until separately implemented and device-tested.
