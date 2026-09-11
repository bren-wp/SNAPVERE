# SNAPVERE Android architecture, UI and QA

## Product contract

SNAPVERE for Android is a native, local-first screen-capture application. It does not require an account and it does not upload capture pixels. Every capture begins with explicit user interaction and a fresh Android MediaProjection consent flow.

Supported baseline:

- Android 10+ / API 29+
- compileSdk / targetSdk 36
- Java 17
- Android Gradle Plugin 8.10.1
- Gradle 8.11.1

## User flow

1. User opens SNAPVERE.
2. The dark home surface shows capture readiness and the latest local capture, if one is still readable.
3. User taps **Capture screen**.
4. Android displays the system MediaProjection consent UI.
5. After consent, SNAPVERE starts its mediaProjection foreground service while the Activity is still foreground.
6. SNAPVERE requests `moveTaskToBack(true)`.
7. `MainActivity.onStop()` signals that the SNAPVERE task is no longer visible.
8. Only then does the service create the VirtualDisplay and ImageReader.
9. The first completed frame is validated, converted from the RGBA image plane into a cropped ARGB bitmap and written as PNG through MediaStore.
10. The URI/name of the latest capture are kept in private SharedPreferences for Open/Share/Delete convenience.
11. Projection/display/reader/thread resources are released and the foreground service stops.

A bounded five-second task-hide timeout prevents a handoff from remaining active forever. After the VirtualDisplay starts, a separate bounded seven-second frame-delivery timeout prevents a stalled ImageReader/driver path from leaving the foreground service and global capture lock active indefinitely. Neither timeout starts another capture or bypasses Android consent.

Android 14+ requires fresh user consent for each MediaProjection capture session and one `createVirtualDisplay()` invocation per MediaProjection instance. SNAPVERE follows that model: a consent token is not cached or reused and each capture receives a fresh projection instance. Android also recommends registering `MediaProjection.Callback.onStop()` and releasing capture resources when projection ends; SNAPVERE does so. See the Android platform documentation for MediaProjection behavior.

## Concurrency and teardown

`CaptureService` owns a process-local single-active-capture guard. A second accidental start cannot overlap the existing MediaProjection session.

The service uses atomic completion and capture-start state. Cleanup is idempotent and each platform resource is released independently, so a vendor/API exception while releasing one resource does not prevent later resources or the global capture ownership flag from being released. Unexpected service destruction also records and best-effort broadcasts a local failure status.

Resources covered by deterministic cleanup:

- task-hide and frame-delivery timeout callbacks
- MediaProjection callback
- MediaProjection
- VirtualDisplay
- ImageReader and acquired Image
- handler callbacks
- HandlerThread
- foreground notification/service ownership
- process-local capture ownership

`ImageReader.acquireLatestImage()` is guarded because Android may throw when the queue is exhausted or when a producer/format mismatch occurs on affected API levels. Every successfully acquired image is closed even when conversion or saving fails.

## Capture-buffer validation

Before allocating the padded bitmap, SNAPVERE validates:

- visible width is positive;
- pixel stride is positive;
- row stride is at least the number of bytes required by the visible row;
- visible-row arithmetic does not overflow an `int`;
- padded width addition does not overflow.

The pure `CaptureBufferLayout` helper is covered by JVM unit tests for tight rows, padded rows, invalid dimensions/strides and overflow cases.

## Storage

Captures are saved through Android MediaStore as `image/png` with relative path:

```text
Pictures/SNAPVERE
```

The app does not request broad storage access. MediaStore pending-state finalization is checked; if finalization fails, cleanup of the incomplete item is best-effort and cannot mask the original save error.

The latest-capture UI validates that the stored URI is still readable before enabling Open, Share or Delete. A stale URI is removed from app-private preferences instead of leaving broken actions visible. MediaStore/provider failures are handled as visible UI failures rather than process crashes.

## Dark design system and responsive UI

The Android distribution shares the core dark SNAPVERE palette with the Windows distribution. Canonical desktop tokens come from `src/Snapvere.App/App.xaml`; Android mirrors them in `android/app/src/main/res/values/colors.xml`.

Core shared tokens:

| Token | Value |
| --- | --- |
| Canvas | `#0B0D12` |
| Surface | `#12151C` |
| Raised surface | `#181C25` |
| Border | `#2A3140` |
| Primary text | `#F6F7FB` |
| Secondary text | `#98A2B3` |
| Muted text | `#727C90` |
| Primary accent | `#7C6CFF` |
| Success | `#45D6A2` |

Android-specific strong-border, strong-accent, warning and destructive tokens extend that base without changing the shared product identity. OEM `forceDark` is explicitly disabled so SNAPVERE's already-dark palette is not transformed a second time.

UI hierarchy:

- identity header with SNAPVERE icon, tagline and Android/local badge
- emphasized Capture card
- live accessibility-aware status panel
- Latest Capture card with validated Open / Share / Delete actions
- Private by Design card
- About/support/legal card
- version/platform footer

The page is vertically scrollable and respects system-bar insets. Phone layouts use compact horizontal spacing; tablet-class layouts use wider 48 dp horizontal padding. Action pairs automatically stack vertically on narrow displays or when Android font scale is 1.25x or greater, preventing clipped labels and undersized touch targets. Buttons keep a 52 dp minimum touch height, ripple feedback and explicit disabled states.

## Action reliability

All user-facing actions have explicit failure paths:

- **Capture** handles unavailable MediaProjection services and launcher failures without leaving the Capture button disabled.
- **Open** and **Share** revalidate the MediaStore URI immediately before delegating to Android.
- **Delete** confirms first, handles stale/provider failures and never crashes on a bad URI.
- **Website**, **Privacy** and **Terms** delegate to external browser handlers and surface a local error if none is available.
- **Support** tries `mailto:` first, then copies `info@snapvere.com`; a missing clipboard service also produces a visible local error instead of an exception escaping the UI.
- Local capture-result receiver registration/unregistration is guarded against lifecycle/provider edge cases.

None of these actions add an `INTERNET` permission or a first-party networking client. External URLs open only after explicit user input.

## Localization

English is the default resource set. Croatian is provided in `values-hr`. New recovery/status messages are maintained in both resource sets. Android performs normal resource fallback when the device uses another locale.

## Manifest security contract

CI fails if the Android manifest adds `android.permission.INTERNET`. CI also verifies that:

- `FOREGROUND_SERVICE_MEDIA_PROJECTION` is declared;
- CaptureService remains `android:exported="false"`;
- CaptureService remains `android:foregroundServiceType="mediaProjection"`;
- cleartext traffic remains disabled;
- app backup remains disabled.

No telemetry, analytics SDK, advertising SDK, cloud upload client, WebView or remote command channel is part of this Android milestone.

## GitHub Actions evidence

`.github/workflows/android-ci.yml` is the build source of truth. For every Android PR and Android change on `main`, CI:

1. validates the manifest privacy/service contract;
2. sets up JDK 17 and Gradle 8.11.1;
3. verifies Android SDK 36 / Build Tools 35.0.0;
4. runs `clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease`;
5. treats lint warnings as errors;
6. executes local JVM unit tests, including capture-buffer layout validation;
7. builds both debug and minified/shrunk release variants;
8. verifies the debug APK with `apksigner`;
9. verifies alignment with `zipalign`;
10. computes SHA-256;
11. uploads APK + digest as a 30-day Actions artifact.

Artifact name:

```text
snapvere-android-apk-<commit-sha>
```

Artifact payload:

```text
SNAPVERE-Android-0.0.9-debug.apk
SNAPVERE-Android-0.0.9-debug.apk.sha256
```

Generated APKs are deliberately excluded from Git source so a source edit cannot silently leave an obsolete binary committed beside it.

## Signing

The CI APK is debug-signed for development/internal distribution and can be installed for testing. It is not represented as a production Play Store/release-signed binary. Production signing requires a separately managed private release key and must not place signing secrets in the repository.

## Evidence boundaries

A green Android CI run proves source compilation, debug/release lint, JVM unit tests, debug/release variant build, debug APK signature, APK alignment and artifact generation. It does not by itself prove physical-device interaction on every OEM/Android combination. Device/emulator runtime QA should be cited separately when actually performed.

Final pull-request evidence must be generated from the final source revision being merged. A green run whose base predates another merged QA or packaging fix is useful historical evidence, but it is not accepted as the final Android merge proof.

## Platform parity boundary

The Android application is complete for its implemented full-screen MediaProjection workflow. Android does not expose the same general top-level-window capture primitive that SNAPVERE uses on Windows; therefore Windows-style Window Capture is not claimed on Android. Region selection and annotation parity are separate product capabilities and are not falsely described as present until implemented and device-tested.
