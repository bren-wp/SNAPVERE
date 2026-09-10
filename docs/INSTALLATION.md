# Installation and Portable Builds

## Supported release architectures

SNAPVERE 0.0.4 publishes native Windows artifacts for:

- **x64** — standard 64-bit Windows systems;
- **x86** — 32-bit Windows systems. `x32` is another informal name for the same architecture.

ARM64 remains a source/build target but is not part of the 0.0.4 public binary release while release QA is focused on x64 and x86.

## Platform target

The application targets Windows 10 version 1809 / build 17763 or later. Windows 11 is supported through the same Windows App SDK application model.

Region and primary Screen Capture retain the monitor compatibility path on supported older Windows versions. Window Capture uses Windows.Graphics.Capture and requires Windows 10 version 2004 / build 19041 or later.

GitHub Actions runtime validation uses `windows-2022`, a Windows App SDK 1.8 supported server target.

## Tray-first startup

A normal SNAPVERE launch **does not open the Capture Center**. The process initializes global capture hotkeys and the Windows notification-area icon and remains ready in the tray.

- left-click the SNAPVERE tray icon → Region Capture starts immediately;
- right-click the tray icon → branded capture/options flyout;
- Print Screen → Region Capture when the key is available;
- `Ctrl+Shift+1` → Region fallback;
- `Ctrl+Shift+2` → Window Capture;
- `Ctrl+Shift+4` → Screen Capture.

Options/Recent and About windows appear only when explicitly opened from the right-click menu.

## Setup

Default install directory:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

This is a per-user location and the default installation does not require administrator privileges.

Interactive Setup requires acceptance of the Mozilla Public License 2.0. Setup can create Start menu and Desktop shortcuts.

### Uninstall

SNAPVERE registers under the current user's Windows Installed apps list. Windows invokes:

```text
SNAPVERE-Setup.exe --uninstall
```

There is intentionally **no standalone uninstaller executable**. SNAPVERE does not install `uninstall.exe`, `unins000.exe`, other `uninstall*.exe` files or Inno-style `unins*.exe` files. The same Setup binary handles install, maintenance/update and removal.

Before destructive cleanup, Setup validates the SNAPVERE installation marker and expected application files. An invalid/missing marker prevents directory removal.

Uninstall removes application files, shortcuts and uninstall registration. Screenshots under `Pictures\SNAPVERE` are preserved.

CI verifies `UninstallString`, `QuietUninstallString`, install location and package version and rejects a separate uninstaller payload.

### Silent mode

```text
SNAPVERE-0.0.4-Setup-x64.exe --silent --accept-license
```

A silent install without `--accept-license` exits with code `2` without installing.

Silent uninstall:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

## Portable

The Portable release is one launcher EXE per architecture. It embeds the self-contained application payload, extracts it into a private versioned SNAPVERE directory beneath the Windows temporary directory and launches the application from that cache.

Portable:

- does not register an Installed apps entry;
- does not create Start menu/Desktop shortcuts;
- does not require a separately installed .NET or Windows App SDK runtime;
- validates extraction destinations and blocks traversal/absolute paths;
- bounds archive entry count and expanded size;
- reuses the same version/architecture cache;
- removes stale SNAPVERE portable caches when possible;
- uses the same tray-first startup behavior as Setup builds.

Screenshots still save to `Pictures\SNAPVERE`.

## Runtime release gate

For both x64 and x86, CI/release QA require:

1. Setup rejects silent installation without explicit license acceptance;
2. Setup installs with `--silent --accept-license`;
3. Windows Installed apps uses the installed `SNAPVERE-Setup.exe` for uninstall and no standalone uninstaller exists;
4. the legacy activated-window startup probe succeeds;
5. **the tray-only startup probe initializes tray/hotkey hosts without opening the Capture Center and emits `TRAY_READY`;**
6. the Region editor materializes and emits `REGION_OVERLAY_READY`;
7. the Window Capture picker materializes and emits `WINDOW_OVERLAY_READY`;
8. a normal installed tray-first launch remains alive;
9. Setup-based uninstall removes the app and Installed apps registration;
10. Portable passes the same startup/tray/Region/Window probes;
11. Portable normal launch leaves the real SNAPVERE process alive.

Any failure blocks release publication.

## Diagnostics

Startup diagnostics:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

The log records startup stages and exception metadata. Screenshot pixels and capture content are not written to it.

## Integrity

Every GitHub Release includes `SHA256SUMS.txt`. Current public binaries are intentionally not Authenticode-signed; use the SHA-256 file when integrity verification is required.
