# Contributing to SNAPVERE

SNAPVERE is developed as a production Windows and Android capture product. Contributions should improve a real user workflow, correctness, reliability, accessibility, performance, security or maintainability.

## Requirements

### Windows

- Windows 11 development environment
- .NET SDK pinned by `global.json`
- Windows application development prerequisites for WinUI 3
- x64 build capability; changes must preserve x86 and ARM64 compatibility

### Android

- JDK 17
- Gradle 8.11.1
- Android SDK 36
- Android SDK Build Tools 35.0.0
- Android Gradle Plugin version pinned by the project

## Before opening a change

1. Keep capture, imaging, storage and UI responsibilities separated.
2. Do not add fake or nonfunctional controls to stable UI.
3. Do not hardcode 96-DPI, primary-monitor or single-size Android assumptions.
4. Avoid blocking the UI thread for capture, encoding or storage work.
5. Keep capture/session ownership bounded; a timeout, driver failure or provider exception must not leave capture permanently active.
6. Validate buffer/stride/size assumptions before native or bitmap copy work.
7. Add regression tests for geometry/data transformations, buffer arithmetic and known failures when practical.
8. Preserve the local-first privacy model: no telemetry, cloud upload, hidden first-party networking or background capture without explicit product/security review.
9. Keep user-visible failures controlled and actionable rather than exposing raw internal exception text.
10. Keep documentation aligned with actual implementation and actual evidence level.

## Windows validation

```powershell
dotnet restore SNAPVERE.sln
dotnet build SNAPVERE.sln -c Release -p:Platform=x64
dotnet test tests/Snapvere.UnitTests/Snapvere.UnitTests.csproj -c Release -p:Platform=x64
```

Repository CI additionally builds x86, cross-builds ARM64, creates x86/x64/ARM64 native payloads and integrity manifests, captures six real WinUI surfaces, builds universal Setup/Portable and runs x64/x86 Setup/Portable lifecycle plus tray-first checks.

Do not weaken lifecycle timeouts, visual thresholds, dependency auditing or package/security gates merely to make CI green. Fix the underlying regression.

## Android validation

For a configured local Android toolchain:

```bash
gradle -p android --no-daemon clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease
```

Android changes must preserve the manifest privacy/service contract:

- no `android.permission.INTERNET` for the current local-first product;
- cleartext traffic disabled;
- app backup disabled;
- MediaProjection foreground-service permission/type retained;
- `CaptureService` remains non-exported;
- release versionName/versionCode remain synchronized with the intended release line.

CI verifies the debug APK signature, ZIP alignment and SHA-256 artifact. A debug-signed CI APK is development/internal evidence and must not be represented as the public release identity.

## v0.1.0 release contract

A v0.1.0 GitHub Release is valid only when it contains exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

The public Android APK must come from the minified release variant and use SNAPVERE's stable Android release signing identity. Never commit keystores/passwords and never replace missing production signing material with a newly generated/debug key merely to pass publication.

The release workflow expects these repository secrets, managed outside Git source:

```text
SNAPVERE_ANDROID_KEYSTORE_BASE64
SNAPVERE_ANDROID_KEY_ALIAS
SNAPVERE_ANDROID_KEYSTORE_PASSWORD
SNAPVERE_ANDROID_KEY_PASSWORD
```

Release automation must fail before immutable tag creation when stable Android signing cannot be verified.

`SNAPVERE-Android-Source.zip` must be generated from the exact validated Git tree, not from an untracked working directory containing build output or secrets.

## UI/UX review

Windows tray/secondary surfaces and Android controls must remain usable with text scaling and constrained space. Prefer real platform accessibility/input behavior over decorative effects. Every user-facing control requires a working action, meaningful disabled state and controlled failure path.

For Android action pairs, preserve narrow-screen/larger-font stacking and at least 52 dp touch height. For Windows tray integration, preserve `NOTIFYICON_VERSION_4` negotiation, keyboard activation and Explorer recreation handling.

## Documentation

Functional or behavioral changes require corresponding documentation updates. At minimum review:

- `README.md` and `README.hr.md`;
- relevant `docs/` and `docs/hr/` pages;
- `android/README.md` for Android build/release behavior;
- `CHANGELOG.md` for release-visible changes;
- the current release notes (`RELEASE_NOTES_0.1.0.md` for the 0.1.0 line);
- `SECURITY.md` when trust boundaries, permissions, network behavior, packaging or signing change.

Historical release notes/security reports remain historical records and should not be silently rewritten to describe a later release contract.

Do not claim hardware/device/runtime coverage that CI did not actually execute.

## Commit style

Use specific imperative messages such as:

- `Implement mixed-DPI display mapping`
- `Harden Android capture cleanup`
- `Validate Portable cache integrity`

Avoid generic messages such as `update`, `fix`, `changes`, `final` or `stuff`.

## Sensitive data

Never commit screenshots containing private data, signing keys, production credentials, license secrets, debug dumps, user runtime data or Android/Windows private signing material.
