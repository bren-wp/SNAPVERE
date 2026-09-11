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
5. Keep capture/session ownership bounded; a platform timeout, driver failure or provider exception must not leave capture permanently active.
6. Add tests for geometry/data transformations, buffer arithmetic and regressions when practical.
7. Preserve the local-first privacy model: no telemetry, cloud upload, hidden first-party networking or background capture without an explicit product/security review.
8. Keep documentation aligned with the actual implementation and evidence level.

## Windows validation

```powershell
dotnet restore SNAPVERE.sln
dotnet build SNAPVERE.sln -c Release -p:Platform=x64
dotnet test tests/Snapvere.UnitTests/Snapvere.UnitTests.csproj -c Release -p:Platform=x64
```

Repository CI additionally builds x86, cross-builds ARM64, creates x86/x64/ARM64 native payloads and integrity manifests, captures six real WinUI surfaces, enforces the exact two-file universal package contract and runs x64/x86 Setup/Portable lifecycle and tray-first checks.

Do not weaken lifecycle timeouts, visual thresholds or package/security gates merely to make CI green. Fix the underlying regression.

## Android validation

For a correctly configured local Android toolchain:

```bash
gradle -p android --no-daemon clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease
```

Android changes must preserve the manifest privacy/service contract:

- no `android.permission.INTERNET` for the current local-first product;
- cleartext traffic disabled;
- app backup disabled;
- MediaProjection foreground-service permission/type retained;
- `CaptureService` remains non-exported.

CI also verifies the debug APK signature, ZIP alignment and SHA-256 artifact. A debug-signed CI APK is development/internal evidence and must not be represented as production Play Store signing.

## UI/UX review

Windows tray/secondary surfaces and Android controls must remain usable with text scaling and constrained space. Prefer real platform accessibility/input behavior over decorative effects. Any user-facing control must have a working action, meaningful disabled state and controlled failure path.

For Android action pairs, preserve the narrow-screen/larger-font stacking behavior. For Windows tray integration, preserve `NOTIFYICON_VERSION_4` negotiation, keyboard activation and Explorer recreation handling.

## Documentation

Functional or behavioral changes require corresponding documentation updates. At minimum review:

- `README.md` and `README.hr.md`;
- the relevant `docs/` and `docs/hr/` pages;
- `CHANGELOG.md` for release-visible changes;
- `RELEASE_NOTES_0.0.9.md` while working on the 0.0.9 line;
- `SECURITY.md` when trust boundaries, permissions, network behavior, packaging or signing change.

Do not claim hardware/device/runtime coverage that CI did not actually execute.

## Commit style

Use specific imperative messages such as:

- `Implement mixed-DPI display mapping`
- `Harden Android capture cleanup`
- `Validate Portable cache integrity`

Avoid generic messages such as `update`, `fix`, `changes`, `final` or `stuff`.

## Sensitive data

Never commit screenshots containing private data, signing keys, production credentials, license secrets, debug dumps, user runtime data or Android/Windows private signing material.
