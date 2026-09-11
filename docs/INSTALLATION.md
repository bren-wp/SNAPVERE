# Installation and release packages

## Public v0.1.0 release contract

A valid SNAPVERE **0.1.0** GitHub Release contains exactly four user-facing assets:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

The two Windows hosts are universal x86/x64/ARM64 launchers. The Android APK is a release build signed with SNAPVERE's stable Android release identity. The Android source ZIP is generated from the exact validated Git tree.

Historical releases keep their historical asset contracts and are not rewritten.

## Windows architecture selection

`SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe` embed native application payloads for:

- x86 / x32 / 32-bit Windows;
- x64 / AMD64 Windows;
- ARM64 Windows.

The host resolves the running Windows architecture and selects the compatible native payload automatically. Users do not choose a separate architecture download.

The application target remains Windows 10 version 1809 / build 17763 or later. WGC-dependent capture paths require Windows 10 version 2004 / build 19041 or later.

## Tray-first startup

Normal Windows launch initializes the capture coordinator, global hotkeys and notification-area icon without opening a launcher dashboard.

- left-click tray → Region Capture;
- right-click tray → quick actions;
- Print Screen → Region Capture when available;
- `Ctrl+Shift+1` → Region Capture fallback;
- `Ctrl+Shift+2` → Window Capture;
- `Ctrl+Shift+3` → Screen Capture.

Options, Language, Recent Captures and About are created only when requested.

## Windows Setup

Default install directory:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

The default install is per-user and does not require Program Files elevation.

Interactive Setup requires acceptance of the **SNAPVERE Commercial Software License Agreement**. Optional defaults are opt-out rather than forced:

- Start menu shortcut — On;
- Desktop icon — On;
- Start SNAPVERE with Windows — On.

The installed maintenance binary is:

```text
%LOCALAPPDATA%\Programs\SNAPVERE\SNAPVERE-Setup.exe
```

The same binary owns install/update/remove.

### Silent install

```text
SNAPVERE-Setup.exe --silent --accept-license
```

Silent install without `--accept-license` exits with code `2`.

## Start with Windows

Per-user startup registration:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SNAPVERE
```

It points to the stable SNAPVERE launcher. Portable mode uses the original Portable launcher path rather than a versioned child inside the extraction cache.

## Same-Setup uninstall

Windows Installed apps invokes:

```text
SNAPVERE-Setup.exe --uninstall
```

Quiet removal:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

SNAPVERE installs no separate `uninstall.exe`, `uninstaller.exe` or `unins*.exe`.

Before recursive install-directory deletion, Setup validates the SNAPVERE installation marker and expected files. Uninstall removes app files, Setup-created shortcuts, Installed apps metadata and the startup registration only when it belongs to the validated installation.

User screenshots remain outside the installation under:

```text
Pictures\SNAPVERE
```

## Windows Portable

`SNAPVERE-Portable.exe` embeds all supported native payloads and extracts only the compatible one into the controlled Portable cache.

Portable protections include:

- no Installed apps registration;
- no forced Start menu/Desktop shortcuts;
- self-contained application payload;
- archive traversal/absolute-target rejection;
- bounded extraction and duplicate-destination rejection;
- trusted architecture-specific embedded SHA-256 integrity manifest;
- validation of expected paths, lengths and hashes before cached execution;
- reparse-point and unexpected-file rejection;
- transactional rebuild/revalidation of invalid reusable cache;
- version/architecture cache reuse only after validation;
- launcher-preparation mutex and local startup diagnostics.

## Android APK installation

`SNAPVERE.apk` targets Android 10 / API 29 or newer. It is produced from the minified/shrunk release variant and is accepted for public release only after `zipalign` and `apksigner` verification.

The Android package intentionally requests no `INTERNET` permission. Screen capture requires Android's system MediaProjection approval for every capture session.

When installing outside an app store, Android may require the user to explicitly allow installation from the chosen package/file source. SNAPVERE does not attempt to bypass Android package-installation policy.

A future Android update must use the same stable release signing identity as the installed public APK. For that reason the release workflow never substitutes an ephemeral CI debug key when release signing material is missing.

## Android source package

`SNAPVERE-Android-Source.zip` is generated from the validated release commit's tracked `android/` tree with `git archive`.

The release gate verifies expected Gradle, manifest and MainActivity paths and rejects generated `build/` / `.gradle/` cache content. The archive does not contain private signing material.

## Release validation gate

Before `v0.1.0` publication, automation requires:

### Windows

1. audited x64 restore/build/tests;
2. x86 build;
3. ARM64 cross-build;
4. root `Snapvere.exe` in all three native payloads;
5. valid architecture integrity manifests;
6. six real rendered WinUI surfaces;
7. universal Setup and Portable generation;
8. x64 and x86 Setup+Portable lifecycle and tray-first probes;
9. uninstall cleanup while preserving user captures.

### Android

1. privacy/service/version manifest contract;
2. `lintDebug` and `lintRelease` with warnings as errors;
3. JVM unit tests;
4. debug and minified release build;
5. stable release signing material available outside Git source;
6. ZIP alignment and cryptographic APK-signature verification;
7. structurally valid Android source ZIP;
8. SHA-256 transfer verification from Android job to final release job.

### Publication

The final release directory must contain exactly:

```text
SNAPVERE-Android-Source.zip
SNAPVERE-Portable.exe
SNAPVERE-Setup.exe
SNAPVERE.apk
```

Only then may the immutable `v0.1.0` tag be created. After publication GitHub's SHA-256 asset digest for every file must match the locally validated digest.

ARM64 Windows evidence on the hosted x64 runner is cross-build/package validation, not physical ARM64 runtime evidence. Android automation is not a claim of exhaustive testing across every physical OEM device.

## Diagnostics

Windows startup diagnostics:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Windows preferences:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Screenshot pixels are not intentionally written to the startup log.

## License and product identity

SNAPVERE 0.0.7 and later use the commercial license in the repository root `LICENSE`. Product: **SNAPVERE**. Developer/publisher: **Brendigo**. Product site: **https://snapvere.com**. Developer site: **https://brendigo.com**.

Historical releases remain under the terms shipped with those versions.
