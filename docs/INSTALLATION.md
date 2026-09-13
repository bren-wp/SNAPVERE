# Installation and release packages

This guide describes the current public **SNAPVERE 0.1.1** packages. Official release: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

## Public v0.1.1 release contract

A valid v0.1.1 GitHub Release contains exactly eight SNAPVERE assets:

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

Historical releases keep their own historical asset contracts. In particular, v0.1.0 remains the original four-asset Windows/Android release and is not retroactively modified.

## Windows architecture selection

`SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe` are universal public hosts containing native application payloads for:

- x86 / x32 / 32-bit Windows;
- x64 / AMD64 Windows;
- ARM64 Windows.

The host resolves the running Windows architecture and selects the compatible payload automatically. Users do not need separate architecture downloads.

Minimum application target: Windows 10 1809 / build 17763. Windows.Graphics.Capture-dependent paths require Windows 10 2004 / build 19041 or later.

## Windows tray-first startup

Normal launch initializes capture coordination, hotkeys and the notification-area icon without opening a permanent dashboard.

- left-click tray → Region Capture;
- right-click tray → quick actions;
- **Print Screen** → Region Capture when available;
- `Ctrl+Shift+1` → Region Capture fallback;
- `Ctrl+Shift+2` → Window Capture;
- `Ctrl+Shift+3` → Screen Capture.

Settings, language, recent captures and About are opened only when requested.

## Windows Setup

Download: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Setup.exe

Default per-user installation directory:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

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

Silent installation without `--accept-license` exits with code `2`.

### Start with Windows

Per-user startup registration:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SNAPVERE
```

It points to the stable SNAPVERE launcher.

### Uninstall with the same Setup binary

Windows Installed apps invokes:

```text
SNAPVERE-Setup.exe --uninstall
```

Quiet removal:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

SNAPVERE does not install a separate `uninstall.exe`, `uninstaller.exe` or `unins*.exe`.

Before recursive installation-directory removal, Setup validates the SNAPVERE installation marker and expected files. User captures remain outside the installation directory under:

```text
Pictures\SNAPVERE
```

## Windows Portable

Download: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Portable.exe

The Portable host embeds all supported native payloads and extracts only the compatible architecture into a controlled cache. Protections include:

- no Installed apps registration;
- no forced Start menu/Desktop shortcuts;
- self-contained application payload;
- archive traversal/absolute-target rejection;
- bounded extraction and duplicate-destination rejection;
- architecture-specific SHA-256 integrity manifests;
- path/length/hash validation before cached execution;
- reparse-point and unexpected-file rejection;
- transactional invalid-cache rebuild/revalidation;
- version/architecture cache reuse only after validation;
- launcher-preparation mutex and local startup diagnostics.

Portable startup registration, when explicitly enabled by the user, points to the original Portable launcher rather than a versioned child in the extraction cache.

## Android APK

Download: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE.apk

Requirements and identity:

- Android 10 / API 29 or later;
- `versionName 0.1.1` / `versionCode 11`;
- native Java 17 application;
- no `android.permission.INTERNET`;
- fresh Android MediaProjection approval for each capture.

The public v0.1.1 APK uses the validated **CI/debug signing identity**. It is installable but is not represented as Google Play/production-signed. A future release using a different production signing identity may require uninstall/reinstall rather than an in-place update.

When installing outside an app store, Android can require explicit permission to install from the chosen source. SNAPVERE does not attempt to bypass Android package-installation policy.

The release gate verifies APK signature, ZIP alignment and SHA-256 integrity.

## Android source package

Download: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Android-Source.zip

The source ZIP is generated from the validated tracked `android/` tree with `git archive`. The release gate checks expected Gradle, manifest and application source paths and rejects generated `build/` / `.gradle/` cache content. Private signing material is not part of the archive.

## Chrome manual installation

Package: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Chrome.zip

1. Extract the ZIP to a stable local directory.
2. Open `chrome://extensions`.
3. Enable **Developer mode**.
4. Choose **Load unpacked**.
5. Select the extracted directory containing `manifest.json`.

## Microsoft Edge manual installation

Package: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Edge.zip

1. Extract the ZIP.
2. Open `edge://extensions`.
3. Enable **Developer mode**.
4. Choose **Load unpacked**.
5. Select the extracted extension directory.

## Opera manual installation

Package: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Opera.zip

1. Extract the ZIP.
2. Open `opera://extensions`.
3. Enable developer mode if required by the current Opera UI.
4. Choose the unpacked-extension load option.
5. Select the extracted extension directory.

## Firefox temporary/development installation

Package: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Firefox.zip

For development/testing, extract the ZIP, open `about:debugging`, choose **This Firefox**, select **Load Temporary Add-on**, and choose the extracted `manifest.json`.

Firefox store distribution has separate AMO review/signing requirements. The GitHub ZIP is not represented as AMO-signed or store-approved.

## Browser store status

The four browser ZIPs are official v0.1.1 GitHub release assets, but GitHub publication does **not** imply publication or approval in Chrome Web Store, Edge Add-ons, Opera Add-ons or Mozilla Add-ons. Those channels require authenticated publisher accounts and external review/signing.

## Release validation gate

Before v0.1.1 publication, automation required:

### Windows

1. audited restore/build/test;
2. x86/x64/ARM64 payload generation;
3. architecture integrity manifests;
4. native payload validation;
5. universal Setup/Portable generation;
6. exact public package checks;
7. x64/x86 Setup+Portable lifecycle and tray-first probes.

### Android

1. privacy/service/version contract;
2. `lintDebug` and `lintRelease`;
3. JVM unit tests;
4. debug and release-variant builds;
5. APK signature and ZIP-alignment verification;
6. source ZIP validation;
7. SHA-256 transfer validation.

### Browser extensions

1. MV3 manifest/permission/source validation;
2. cross-browser parity validation;
3. EN/HR locale checks;
4. store metadata/privacy validation;
5. reproducible two-pass packaging;
6. exact four ZIP package names and SHA-256 checks.

### Publication

The final release directory had to contain exactly the eight files listed at the top of this guide. The release workflow then created/verified `v0.1.1`, published the GitHub Release and compared GitHub's published digest for every asset with its locally validated SHA-256 value.

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

## Next steps

- [User Guide](USER-GUIDE.md)
- [Troubleshooting](TROUBLESHOOTING.md)
- [Privacy](PRIVACY.md)
- [QA Matrix](QA-MATRIX.md)
- [Versioning & Releases](VERSIONING-RELEASES.md)

SNAPVERE is developed and published by **Brendigo**. Product site: https://snapvere.com · Support: **info@snapvere.com**.
