# Installation and Universal Portable Builds

## Public release contract

Starting with SNAPVERE **0.0.7**, each public release contains exactly two user-facing downloads:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

There are no separate x86/x64 downloads, no public ZIP payloads and no Demo executable in the v0.0.7+ release contract.

## Architecture selection

Each public host is intentionally built as an x86-compatible Windows executable and embeds native application payloads for:

- **x86 / x32 / 32-bit Windows**;
- **x64 / AMD64 Windows**;
- **ARM64 Windows**.

At runtime the host resolves the Windows architecture and launches/extracts the compatible native application payload. Users do not need to choose an architecture-specific download.

Architecture support does not imply support for every historical Windows release. The application target remains Windows 10 version 1809 / build 17763 or later. WGC-dependent capture paths require Windows 10 version 2004 / build 19041 or later.

## Tray-first startup

A normal SNAPVERE launch initializes its capture coordinator, global hotkeys and notification-area icon without opening the former Capture Center as a normal user surface.

- left-click tray → Region Capture;
- right-click tray → branded quick-actions flyout;
- Print Screen → Region Capture when available;
- `Ctrl+Shift+1` → Region fallback;
- `Ctrl+Shift+2` → Window Capture;
- `Ctrl+Shift+4` → Screen Capture.

Options, Language, Recent Captures and About are created only when requested.

## Setup

Default install directory:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

The default install is per-user and does not require Program Files administration.

Interactive Setup requires acceptance of the **SNAPVERE Commercial Software License Agreement**. Setup defaults are opt-out, not forced:

- Start menu shortcut — **On**;
- Desktop icon — **On**;
- Start SNAPVERE with Windows — **On**.

The user can clear any optional checkbox before installation.

The installed maintenance binary is copied to:

```text
%LOCALAPPDATA%\Programs\SNAPVERE\SNAPVERE-Setup.exe
```

The same binary owns install/update/remove.

### Silent install

```text
SNAPVERE-Setup.exe --silent --accept-license
```

A silent install without `--accept-license` exits with code `2`. Silent installation uses the same default Start menu, Desktop and Start-with-Windows choices.

## Start with Windows

The per-user startup registration lives under:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SNAPVERE
```

It points to the stable SNAPVERE executable. Normal launch is tray-first, so no separate startup-only executable is required.

Portable builds use the original stable Portable launcher path for this preference rather than a versioned child path inside the extraction cache.

## Same-Setup uninstall contract

Windows Installed apps invokes:

```text
SNAPVERE-Setup.exe --uninstall
```

Quiet removal uses:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

SNAPVERE intentionally installs no separate `uninstall.exe`, `uninstaller.exe` or Inno-style `unins*.exe`.

Before recursive deletion, Setup validates the SNAPVERE installation marker and expected files. Uninstall removes application files, Setup-created shortcuts, Installed apps metadata and the startup registration only when it belongs to the validated installation.

User screenshots remain in:

```text
Pictures\SNAPVERE
```

## Portable

`SNAPVERE-Portable.exe` embeds all supported native payloads and extracts only the compatible one into the controlled SNAPVERE Portable cache.

Portable guarantees include:

- no Installed apps registration;
- no forced Start menu/Desktop shortcut creation;
- self-contained application payload;
- rejection of archive path traversal and absolute extraction targets;
- bounded archive extraction;
- version/architecture cache reuse;
- best-effort cleanup of stale SNAPVERE Portable caches;
- launcher preparation mutex;
- stable original Portable path for optional Windows startup registration;
- startup diagnostics when the child cannot be launched.

## Runtime release gate

CI and release QA require:

1. x64 build/test succeeds;
2. x86 build succeeds;
3. ARM64 cross-build succeeds;
4. all three embedded payload archives contain root `Snapvere.exe`;
5. the public package directory contains exactly `SNAPVERE-Setup.exe` and `SNAPVERE-Portable.exe`;
6. silent Setup rejects missing commercial-license acceptance;
7. silent Setup installs successfully with license acceptance;
8. default Desktop shortcut and Start-with-Windows registration exist after install;
9. Installed apps metadata points to the installed `SNAPVERE-Setup.exe`;
10. tray, Region, Window, Options and About runtime probes materialize successfully;
11. installed and Portable x64/x86 runtime lifecycle succeeds;
12. same-Setup uninstall removes installed files, Desktop shortcut, startup registration and Installed apps metadata;
13. user capture files remain outside uninstall scope.

ARM64 is cross-built/package-validated on the hosted x64 Windows runner. That runner is not represented as a real ARM64 runtime device.

## Diagnostics

Startup diagnostics are local:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Preferences are local:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Screenshot pixels are not written to the startup log.

## License and product identity

SNAPVERE 0.0.7 and later use the commercial license shipped in the repository root `LICENSE` file. Product: **SNAPVERE**. Developer/publisher: **Brendigo**. Product site: **https://snapvere.com**. Developer site: **https://brendigo.com**.

Historical releases remain under the terms distributed with those versions.
