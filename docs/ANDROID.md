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
9. The first completed frame is converted from RGBA image planes into a cropped ARGB bitmap and written as PNG through MediaStore.
10. The URI/name of the latest capture are kept in private SharedPreferences for Open/Share/Delete convenience.
11. Projection/display/reader/thread resources are released and the foreground service stops.

A bounded five-second task-hide timeout is failure protection only. It never starts capture by itself.

## Concurrency and teardown

`CaptureService` owns a process-local single-active-capture guard. A second accidental start cannot overlap the existing MediaProjection session.

The service uses atomic completion and capture-start state. If Android destroys the service while capture work is running, teardown is serialized through the capture handler when possible. Capture ownership is released only after cleanup, preventing a subsequent session from overlapping resource teardown from the previous session.

Resources covered by deterministic cleanup:

- MediaProjection callback
- MediaProjection
- VirtualDisplay
- ImageReader
- handler callbacks
- HandlerThread
- foreground notification/service ownership

## Storage

Captures are saved through Android MediaStore as `image/png` with relative path:

```text
Pictures/SNAPVERE
```

The app does not request broad storage access. MediaStore pending-state finalization is checked; if finalization fails, the incomplete item is deleted.

The latest-capture UI validates that the stored URI is still readable before enabling Open, Share or Delete. A stale URI is removed from app-private preferences instead of leaving broken actions visible.

## Dark design system

The Android distribution now shares the core dark SNAPVERE palette with the Windows distribution instead of maintaining a separate look. Canonical desktop tokens come from `src/Snapvere.App/App.xaml`; Android mirrors them in `android/app/src/main/res/values/colors.xml`.

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

Android-specific strong-border, strong-accent, warning and destructive tokens extend that base without changing the shared product identity.

`MainActivity` consumes resource tokens rather than embedding an unrelated palette. UI hierarchy:

- identity header with SNAPVERE icon, tagline and Android/local badge
- emphasized Capture card
- live accessibility-aware status panel
- Latest Capture card with validated Open / Share / Delete actions
- Private by Design card
- About/support/legal card
- version/platform footer

The page is vertically scrollable and respects system-bar insets. Phone layouts use compact horizontal spacing; tablet-class layouts use wider 48 dp horizontal padding. Interactive buttons use a 52 dp minimum height, native ripple feedback and explicit disabled-state treatment so touch targets remain comfortable without adding a heavyweight UI framework.

## Latest capture actions

### Open

Delegates the MediaStore URI to an Android image viewer using `ACTION_VIEW` and a temporary read grant.

### Share

Delegates the PNG to Android's share sheet with `ACTION_SEND` and a temporary read grant. Sharing is always initiated by the user.

### Delete

Displays a native confirmation dialog. The app deletes only the stored latest-capture MediaStore URI. If the URI is already gone, the stale local reference is cleared.

## Website, support and legal actions

Destinations are explicit user actions:

- website: `https://snapvere.com`
- support: `mailto:info@snapvere.com`
- privacy: `https://snapvere.com/privacy`
- terms: `https://snapvere.com/terms`

The application does not prefetch them and has no `INTERNET` permission. Android delegates HTTP(S) links to an external browser. If no mail application handles `mailto:`, the support address is copied to the local clipboard and the app shows a visible status message.

## Localization

English is the default resource set. Croatian is provided in `values-hr`. Android performs the normal resource fallback when the device uses another locale.

## Manifest security contract

CI fails if the Android manifest adds `android.permission.INTERNET`. CI also verifies that:

- `FOREGROUND_SERVICE_MEDIA_PROJECTION` is declared
- CaptureService remains `android:exported="false"`
- CaptureService remains `android:foregroundServiceType="mediaProjection"`
- cleartext traffic remains disabled
- app backup remains disabled

No telemetry, analytics SDK, advertising SDK, cloud upload client, WebView or remote command channel is part of this Android milestone.

## GitHub Actions evidence

`.github/workflows/android-ci.yml` is the build source of truth. For every Android PR and Android change on `main`, CI:

1. validates the manifest privacy/service contract
2. sets up JDK 17 and Gradle 8.11.1
3. verifies Android SDK 36 / Build Tools 35.0.0
4. runs `clean lintDebug assembleDebug assembleRelease`
5. treats lint warnings as errors
6. verifies the debug APK with `apksigner`
7. verifies alignment with `zipalign`
8. computes SHA-256
9. uploads APK + digest as a 30-day Actions artifact

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

A green Android CI run proves source compilation, Android lint, debug/release variant build, debug APK signature, APK alignment and artifact generation. It does not by itself prove physical-device interaction on every OEM/Android combination. Device/emulator runtime QA should be cited separately when actually performed.

## Platform parity boundary

The Android application is complete for its implemented full-screen MediaProjection workflow. Android does not expose the same top-level-window capture primitive that SNAPVERE uses on Windows; therefore Windows-style Window Capture is not claimed on Android. Region selection and annotation parity remain future features until implemented and device-tested.
