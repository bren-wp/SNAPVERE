# Contributing to SNAPVERE

SNAPVERE is developed as a production capture product for **Windows, Android, Chrome, Edge, Opera and Firefox**. Contributions should improve a real user workflow, correctness, reliability, accessibility, performance, security, documentation or maintainability without weakening the local-first product model.

Current public release: **0.1.1**. Canonical active version contract: [`product-version.json`](product-version.json). Canonical detailed release history: [`RELEASES.md`](RELEASES.md).

## Development requirements

### Windows

- Windows development environment capable of building WinUI 3;
- .NET SDK pinned by `global.json`;
- x64 build capability;
- changes must preserve x86 and ARM64 package compatibility.

### Android

- JDK 17;
- Gradle 8.11.1;
- Android SDK 36;
- Android Build Tools 35.0.0;
- Android Gradle Plugin version pinned by the project.

### Browser extensions

- a current Node.js runtime for source validators/smoke tests;
- `bash`, `zip` and `sha256sum` for reproducible packaging;
- Python 3 for deterministic store-image generation/validation tooling.

No bundler/transpiler is required for the current extension source.

## Before opening a change

1. Keep capture, imaging, storage, packaging and UI responsibilities separated.
2. Do not add fake or nonfunctional controls to stable UI.
3. Do not hardcode 96-DPI, primary-monitor, single-monitor or single-size Android assumptions.
4. Avoid blocking the UI thread for capture, encoding or storage work.
5. Keep capture/session ownership bounded; timeout/driver/provider errors must not leave capture permanently active.
6. Validate buffer/stride/size/path assumptions before native/bitmap/extraction work.
7. Add regression tests for geometry/data transformations, state machines and known failures when practical.
8. Preserve local-first privacy: no telemetry, cloud upload, hidden first-party networking, remote runtime code or background capture without explicit product/security review.
9. Keep user-visible failures controlled/actionable rather than exposing raw internal exception text.
10. Keep permissions minimal; browser changes must not casually introduce broad host access.
11. Keep English/Croatian resource keys aligned on Android and browser extensions.
12. Keep documentation aligned with actual implementation and actual evidence level.
13. Update `product-version.json` only as part of an intentional cross-platform version/release change.
14. Keep detailed release history in `RELEASES.md`; do not create a new root `RELEASE_NOTES_<version>.md` file.

## Windows validation

```powershell
dotnet restore SNAPVERE.sln
dotnet build SNAPVERE.sln -c Release -p:Platform=x64
dotnet test tests/Snapvere.UnitTests/Snapvere.UnitTests.csproj -c Release -p:Platform=x64
```

Repository CI additionally builds x86, cross-builds ARM64, creates native payloads/integrity manifests, captures real WinUI surfaces, builds universal Setup/Portable and runs x64/x86 Setup/Portable lifecycle plus tray-first checks.

Do not weaken lifecycle timeouts, visual thresholds, dependency auditing or package/security gates merely to make CI green. Fix the underlying regression.

## Android validation

For a configured local Android toolchain:

```bash
gradle -p android --no-daemon clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease
```

Android changes must preserve the current manifest privacy/service contract:

- no `android.permission.INTERNET` for the local-first product;
- cleartext traffic disabled;
- app backup disabled;
- MediaProjection foreground-service permission/type retained;
- `CaptureService` remains non-exported;
- `versionName`/`versionCode` remain synchronized with the intended release line.

The current public v0.1.1 `SNAPVERE.apk` intentionally uses the validated **CI/debug signing identity**. Do not describe it as Google Play/production-signed. A future production-signing channel requires deliberately managed credentials outside Git source and explicit migration documentation.

Never commit keystores, private signing keys or passwords merely to make publication pass.

## Browser-extension validation

Run the core source checks from repository root:

```bash
node ekstenzije/tools/validate-extensions.mjs
node ekstenzije/tools/verify-extension-parity.mjs
node ekstenzije/tools/smoke-test-background.mjs
node ekstenzije/tools/validate-store-readiness.mjs
```

Store readiness validation requires generated store graphics; CI installs the pinned Pillow version and runs `ekstenzije/tools/generate-store-assets.py` first.

For deterministic packaging:

```bash
bash ekstenzije/tools/package-extensions.sh
```

Browser changes must preserve:

- exact permission allow-list `activeTab`, `scripting`, `downloads`, `storage` unless an intentional reviewed product change says otherwise;
- no broad `host_permissions` / `<all_urls>` in the current product contract;
- no remote runtime scripts, analytics, telemetry, advertising or cloud-upload client;
- Chrome/Edge/Opera runtime parity and only documented Firefox differences;
- EN/HR locale parity;
- bounded capture/tile/canvas/pixel behavior;
- controlled protected-page failures;
- reproducible ZIP packaging.

A green static/smoke/package workflow is not a claim of exhaustive manual GUI coverage on every page/browser build.

## Product Contract validation

Run:

```bash
python3 eng/validate-product-contract.py
```

The validator uses [`product-version.json`](product-version.json) to verify Windows, Android and browser version alignment, Android EN/HR resource-key parity, exact release asset names, canonical release-history structure, active README/current-documentation state and relative Markdown links.

When bumping a future version, follow [Versioning & Releases](docs/VERSIONING-RELEASES.md) rather than editing one platform in isolation.

## v0.1.1 public release contract

The already published v0.1.1 release contains exactly:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

Published tags/assets are historical artifacts and must not be rewritten by post-release maintenance. A future binary change belongs in a future version/tag.

## UI/UX review

Windows tray/secondary surfaces, Android controls and extension popup/options/selection UI must remain usable with keyboard input, text scaling and constrained space.

- Windows: preserve tray keyboard activation, Explorer recreation handling, high-DPI/multi-monitor behavior and working disabled/error states.
- Android: preserve narrow-screen/larger-font stacking and large touch targets.
- Browser: preserve visible focus, ARIA/localized labels and clean cancellation/cleanup behavior.

Prefer real platform accessibility/input behavior over decorative effects.

## Documentation

Functional, behavioral, permission, signing or packaging changes require corresponding documentation updates. Review at minimum:

- `README.md` and `README.hr.md`;
- [Documentation Hub](docs/README.md) and Croatian hub;
- relevant `docs/` + `docs/hr/` pages;
- `android/README.md` for Android source/build behavior;
- `ekstenzije/README.md` for browser behavior;
- `CHANGELOG.md` for the concise engineering history;
- `RELEASES.md` for canonical detailed release notes/history;
- `SECURITY.md` when trust boundaries, permissions, networking, packaging or signing change;
- `product-version.json` only when the canonical product version/release contract truly changes.

When preparing a new release, prepend its detailed section to `RELEASES.md`; do not create another `RELEASE_NOTES_<version>.md`. Historical sections in `RELEASES.md` remain historical records and should not be silently rewritten to describe a later release contract.

Do not claim hardware/device/browser/store runtime coverage that CI did not execute.

## Commit style

Use specific imperative messages such as:

- `Implement mixed-DPI display mapping`
- `Harden Android capture cleanup`
- `Validate Portable cache integrity`
- `Test browser capture lock recovery`
- `Align product version documentation`

Avoid generic messages such as `update`, `fix`, `changes`, `final` or `stuff`.

## Sensitive data

Never commit screenshots containing private data, signing keys, production credentials, license secrets, debug dumps with user material, real-user runtime data or private store/publisher secrets.
