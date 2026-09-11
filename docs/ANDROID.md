# SNAPVERE Android architecture, UI and QA

## Product contract

SNAPVERE for Android is a native, local-first screen-capture application. It does not require an account and does not upload capture pixels. Every capture begins with explicit user interaction and a fresh Android MediaProjection consent flow.

Current release line: **0.1.0** (`versionCode 10`). Supported baseline:

- Android 10+ / API 29+
- compileSdk / targetSdk 36
- Java 17
- Android Gradle Plugin 8.10.1
- Gradle 8.11.1

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
10. The URI/name of the latest capture are kept in private SharedPreferences for Open/Share/Delete convenience.
11. Image, projection, display, reader, callbacks and thread resources are released and the foreground service stops.

A five-second task-hide timeout prevents the handoff from remaining active forever. After VirtualDisplay creation, a separate seven-second first-frame timeout bounds a stalled driver/ImageReader path. Neither timeout starts another capture or bypasses Android consent.

Android 14+ requires fresh user consent for each MediaProjection capture session and one `createVirtualDisplay()` use per MediaProjection instance. SNAPVERE does not cache or reuse consent tokens and registers `MediaProjection.Callback.onStop()` for controlled teardown.

## 0.1.0 lifecycle hardening

The service treats platform/provider failures as controlled capture failures rather than allowing them to escape the service lifecycle where practical.

- Notification-channel initialization failure is recorded and converted into a capture failure during start instead of intentionally escaping `onCreate()`.
- Handler scheduling is checked: rejected task-hide/frame work fails the session instead of leaving capture ownership stuck.
- MediaProjection, VirtualDisplay and frame acquisition failures return localized recovery messages.
- `ImageReader.acquireLatestImage()` remains guarded.
- Image conversion/save work closes the acquired `Image` before final service cleanup is committed.
- Bitmap conversion catches allocation/provider failures, including `OutOfMemoryError`, and tears down the capture session.
- Process-local capture ownership is released only by the service instance that owns it, preventing a stale teardown from clearing another active owner.
- Cleanup remains idempotent and each Android resource is released independently so one cleanup exception cannot block later cleanup.

## Capture-buffer validation

The ImageReader is created as `PixelFormat.RGBA_8888`. Before bitmap allocation/copy SNAPVERE validates:

- visible width is positive;
- RGBA pixel stride is exactly 4 bytes;
- row stride is positive and large enough for the visible row;
- row padding is aligned to a complete RGBA pixel;
- visible-row and padded-width arithmetic cannot overflow;
- the ByteBuffer is rewound before copy;
- the buffer contains at least `rowStride × height` declared bytes.

The pure `CaptureBufferLayout` helper has JVM regression tests covering tight rows, padded rows, invalid dimensions, unexpected pixel stride, partial padding and overflow.

## Storage

Captures are saved through Android MediaStore as `image/png` under:

```text
Pictures/SNAPVERE
```

No broad storage permission is requested. MediaStore pending-state finalization is checked. If writing or finalization fails, cleanup of the incomplete item is best-effort and cannot mask the original failure.

The latest-capture UI validates that its stored URI is still readable before enabling Open, Share or Delete. Stale URI metadata is removed instead of leaving broken actions visible.

## Dark UI and responsive UX

The Android distribution shares the core dark SNAPVERE palette with Windows. OEM `forceDark` is disabled because the app already supplies an intentional dark palette.

The home surface contains:

- SNAPVERE identity/header and Android/private/local badge;
- primary Capture card with live accessibility status;
- Latest Capture card with Open / Share / Delete;
- Private by Design card;
- About/support/legal actions;
- version/platform footer.

The page is vertically scrollable, respects system-bar insets and uses wider horizontal padding on tablet-class widths. Paired actions stack vertically on narrow displays or at Android font scale 1.25x+, preventing clipped labels. Buttons keep at least 52 dp touch height, native ripple feedback and explicit enabled/disabled state.

0.1.0 also refreshes English and Croatian capture/privacy/recovery copy so system/provider faults are described as actionable user-facing states rather than raw exception text from the platform.

## Action reliability

All user-facing actions have explicit failure paths:

- **Capture** handles unavailable MediaProjection/service/launcher paths without intentionally leaving the primary action stuck disabled.
- **Open** and **Share** revalidate the MediaStore URI immediately before delegation.
- **Delete** confirms first and handles stale/provider failures.
- **Website**, **Privacy** and **Terms** are explicit user-initiated external intents with visible failure state.
- **Support** tries `mailto:` first and then copies `info@snapvere.com`; missing handlers/services are contained.
- capture-result receiver registration/unregistration is guarded against lifecycle edge cases.

These actions do not add an `INTERNET` permission or first-party networking client.

## Manifest privacy contract

CI fails if the Android manifest adds `android.permission.INTERNET`. It also checks that:

- `FOREGROUND_SERVICE_MEDIA_PROJECTION` is declared;
- `CaptureService` remains `android:exported="false"`;
- `CaptureService` remains `android:foregroundServiceType="mediaProjection"`;
- cleartext traffic remains disabled;
- app backup remains disabled;
- versionName/versionCode match the 0.1.0 release contract.

There is no telemetry, analytics SDK, advertising SDK, cloud-upload client, WebView or remote command channel in this Android release line.

## Continuous integration APK

`.github/workflows/android-ci.yml` validates every Android PR/change on `main` by running:

```text
clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease
```

Lint warnings are errors. CI validates the privacy/service/version contract, confirms SDK 36 / Build Tools, verifies the debug APK signature and ZIP alignment, computes SHA-256, and uploads:

```text
SNAPVERE-Android-0.1.0-debug.apk
SNAPVERE-Android-0.1.0-debug.apk.sha256
```

inside the Actions artifact `snapvere-android-ci-apk-<commit-sha>`.

## Public 0.1.0 Android release

For v0.1.0 the public `SNAPVERE.apk` uses the same CI/debug-signed Android package path that is already validated by Android CI. The release path does **not** require a private production keystore or repository signing secret.

The release workflow still builds both debug and release variants and requires:

- Android privacy/service/version validation;
- `lintDebug` and `lintRelease`;
- JVM unit tests;
- successful debug and release builds;
- a signed, non-empty debug APK;
- `apksigner` verification;
- ZIP-alignment verification;
- SHA-256 verification across the Actions artifact transfer and final publication.

The verified debug-signed package is published as:

```text
SNAPVERE.apk
```

This APK is installable but is not represented as a Google Play/production-signed package. Its signing identity is not a supported long-term production update lineage. If a later Android channel uses a different stable production key, Android may require uninstall/reinstall before installing the differently signed package.

The workflow also creates directly from the validated Git tree:

```text
SNAPVERE-Android-Source.zip
```

The source ZIP contains tracked `android/` source/configuration only and is structurally tested before publication. Generated `build/` output, Gradle caches and signing material are not included.

## v0.1.0 public asset contract

A valid GitHub Release contains exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

The release workflow carries SHA-256 values across the Android Actions artifact transfer, recomputes final release hashes and verifies GitHub's published asset digests after publication.

## Evidence boundaries

A green Android CI proves compilation, debug/release lint, JVM tests, debug/release builds, debug APK signature/alignment and artifact generation. A green v0.1.0 release job additionally proves the public CI/debug-signed APK signature/alignment, source-archive structure, Windows package lifecycle checks and release-asset digest checks. Neither is a claim of exhaustive physical-device testing across every OEM, Android skin, resolution or permission implementation.

## Platform parity boundary

Android 0.1.0 is complete for its implemented full-screen MediaProjection workflow. Android does not expose the same general top-level-window capture primitive used by SNAPVERE on Windows, so Windows-style Window Capture is not claimed on Android. Region-selection/annotation parity remains a separate future capability until implemented and device-tested.
