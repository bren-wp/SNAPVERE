# SNAPVERE 0.1.0

SNAPVERE 0.1.0 is the first release line that packages the validated Windows desktop application and the native Android companion together as one release contract.

## Windows

- Keeps the tray-first capture workflow for Region, Window and Screen capture.
- Keeps universal Windows Setup and Portable hosts with embedded x86, x64 and ARM64 application payloads.
- Preserves payload-integrity verification and transactional Portable cache recovery before execution.
- Hardens local preference, capture-history and temporary-file cleanup paths against filesystem policy/security failures so secondary cleanup problems cannot replace the original capture result.
- Continues strict NuGet auditing, analyzer enforcement, real rendered WinUI QA and x64/x86 Setup + Portable lifecycle validation before publication.

## Android

- Version is now 0.1.0 / versionCode 10.
- Keeps one explicit user-approved MediaProjection session per capture and no background/continuous recording.
- Keeps `INTERNET` permission absent, cleartext disabled, backup disabled and the capture service non-exported.
- Hardens foreground-service initialization so a notification/service provider fault is converted into a controlled capture failure instead of escaping service creation.
- Checks Handler scheduling failures, bounded task-hide/frame-delivery timeouts and capture ownership teardown.
- Prevents an older service teardown from clearing ownership belonging to another active service instance.
- Validates RGBA pixel stride, row stride, row-padding alignment and available buffer bytes before bitmap copying.
- Rewinds the ImageReader buffer before copy and closes the Image before capture cleanup is finalized.
- Contains image conversion allocation/provider failures, including `OutOfMemoryError`, and returns a localized recoverable status instead of intentionally terminating the Activity.
- Improves English and Croatian capture/privacy/status copy while retaining responsive action stacking and large touch targets.
- Expands JVM regression coverage for invalid image-row and pixel-stride layouts.

## Public release files

A valid v0.1.0 GitHub Release contains exactly these four public assets:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`
- `SNAPVERE.apk`
- `SNAPVERE-Android-Source.zip`

The two Windows executables are universal x86/x64/ARM64 hosts. `SNAPVERE.apk` is built from the minified Android release variant and must be signed with SNAPVERE's stable Android release signing identity. The Android source ZIP is generated directly from the validated Git tree, not from a working directory containing build output.

The release workflow fails before tag creation or publication when Android release signing material is unavailable, when any Windows/Android validation gate fails, or when the release directory does not contain exactly the four approved assets.

## Release validation

Before the immutable `v0.1.0` tag can be created, automation requires:

- Windows audited restore/build/test plus x86 and ARM64 build validation;
- architecture-specific Windows payload and SHA-256 integrity-manifest validation;
- six real rendered WinUI surfaces;
- universal Windows Setup/Portable generation;
- x64 and x86 Setup + Portable lifecycle and tray-first tests;
- Android manifest privacy/service/version validation;
- Android `lintDebug`, `lintRelease`, JVM unit tests, debug build and minified release build;
- APK ZIP alignment and cryptographic release-signature verification;
- Android source archive structural verification;
- SHA-256 verification across the Android Actions transfer and all four final release assets;
- post-publication verification that the GitHub Release contains exactly the expected four assets and that GitHub's published digests match the locally validated SHA-256 values.

## Validation boundary

Hosted CI proves compilation, automated tests, package structure, signatures, digests and the Windows runtime probes exercised by the workflow. ARM64 Windows remains cross-build/package evidence on the hosted x64 runner. Android CI/release automation is not a claim of exhaustive runtime coverage across every OEM device, Android skin, display configuration or permission-management implementation.
