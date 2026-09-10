# Installation and Portable Builds

## Supported release architectures

SNAPVERE 0.0.6 publishes native Windows artifacts for:

- **x64** — standard 64-bit Windows systems;
- **x86** — 32-bit Windows systems. `x32` is an informal name for the same architecture.

ARM64 remains a source/build target but is not part of the 0.0.6 public binary release while release QA is focused on x64 and x86.

## Platform target

The application targets Windows 10 version 1809 / build 17763 or later. Windows 11 is supported through the same Windows App SDK application model.

Region and primary Screen Capture retain the GDI monitor compatibility path on supported older Windows versions. Window Capture uses Windows.Graphics.Capture and requires the newer WGC path used by SNAPVERE on Windows 10 version 2004 / build 19041 or later.

GitHub Actions runtime validation uses `windows-2022`.

## Tray-first startup

A normal SNAPVERE launch **does not open the Capture Center**. It initializes capture services, global hotkeys and the Windows notification-area icon, keeps its WinUI runtime/capture coordinator hidden and remains ready in the tray.

- left-click tray → Region Capture immediately;
- right-click tray → branded SNAPVERE quick-actions flyout;
- Print Screen → Region Capture when available;
- `Ctrl+Shift+1` → Region fallback;
- `Ctrl+Shift+2` → Window Capture;
- `Ctrl+Shift+4` → Screen Capture.

Recent Captures, Options / Preferences and About are secondary surfaces opened only from explicit actions.

## Setup

Default install directory:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

This is a per-user location and the default installation does not require administrator privileges.

Interactive Setup requires acceptance of the Mozilla Public License 2.0. Setup can create Start menu and optional Desktop shortcuts.

The installed setup binary is copied to:

```text
%LOCALAPPDATA%\Programs\SNAPVERE\SNAPVERE-Setup.exe
```

That same executable owns maintenance and uninstall.

### Uninstall contract

SNAPVERE registers under the current user's Windows Installed apps list. Windows invokes:

```text
SNAPVERE-Setup.exe --uninstall
```

Quiet uninstall uses:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

There is intentionally **no standalone uninstaller executable**. SNAPVERE must not install `uninstall.exe`, `uninstaller.exe`, `unins000.exe`, `uninstall*.exe` or Inno-style `unins*.exe` payloads.

Before destructive cleanup, Setup validates the SNAPVERE installation marker and expected application/maintenance files. An invalid or missing marker prevents directory removal.

Uninstall removes:

- installed application files;
- Start menu/Desktop shortcuts created for SNAPVERE;
- Installed apps registry metadata;
- the per-user SNAPVERE startup `Run` value **only when it points exactly to the validated installed `Snapvere.exe`**.

That last condition prevents Setup uninstall from deleting an unrelated or Portable startup registration that happens to use the same value name.

Uninstall does **not** remove user screenshots under:

```text
Pictures\SNAPVERE
```

### Start with Windows

Options / Preferences can register SNAPVERE for per-user startup through:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

Installed builds register the installed `Snapvere.exe` with no separate background-only argument. On sign-in, the same tray-first startup contract applies: no Capture Center should appear.

Portable builds are also able to use this preference. The extracted child receives the stable Portable launcher path from its parent and registers that original Portable EXE rather than a versioned file inside the temporary cache.

Using the same normal tray-first executable command for manual launch and Windows startup keeps the registration and uninstall matching rules deterministic.

### Silent install

```text
SNAPVERE-0.0.6-Setup-x64.exe --silent --accept-license
```

A silent install without `--accept-license` exits with code `2` and does not install.

## Portable

The Portable release is one launcher EXE per architecture. It embeds the self-contained application payload, extracts to a private versioned SNAPVERE directory beneath Windows temporary storage, and launches the application from that cache.

Portable behavior:

- no Installed apps registration;
- no Start menu/Desktop shortcuts created by the launcher;
- no separately installed .NET or Windows App SDK runtime required;
- path traversal and absolute extraction destinations rejected;
- archive entry count and expanded size bounded;
- version/architecture cache reused;
- stale SNAPVERE Portable caches cleaned when possible;
- launcher preparation protected by a mutex;
- startup failure path includes local diagnostics location;
- normal launch is tray-first;
- stable original Portable launcher path is used for optional Windows startup registration;
- the launcher does not report success if the child exits during the initial startup validation window.

Captures still save to `Pictures\SNAPVERE`.

## Runtime release gate

For both x64 and x86, CI/release QA requires:

1. Setup rejects silent installation without explicit license acceptance;
2. Setup installs with `--silent --accept-license`;
3. Installed apps metadata points to the installed `SNAPVERE-Setup.exe` and no standalone uninstaller exists;
4. the explicit hidden runtime-host construction probe succeeds;
5. **the tray-only startup probe initializes tray/hotkey hosts with the runtime coordinator hidden and emits `TRAY_READY`;**
6. the Region editor materializes and emits `REGION_OVERLAY_READY`;
7. the Window Capture picker materializes and emits `WINDOW_OVERLAY_READY`;
8. **the tray flyout, Options and About surfaces materialize in sequence and emit `SECONDARY_UI_READY`;**
9. a normal installed tray-first launch remains alive without a visible main window;
10. an installed SNAPVERE `Run` registration is removed by uninstall;
11. Setup-based uninstall removes app files and Installed apps metadata while preserving user captures;
12. Portable passes startup/tray/Region/Window/secondary-UI probes;
13. Portable normal launch leaves the real SNAPVERE child process alive;
14. release publication contains exactly the expected x64/x86 package set and a validated SHA-256 manifest.

Any failure blocks release publication.

## Diagnostics

Startup diagnostics are local:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

The log records startup stages and exception metadata. Screenshot pixels and capture content are not written to it.

Local preferences are stored separately at:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

## Integrity

Every GitHub Release includes `SHA256SUMS.txt`. The 0.0.6 public binaries are not Authenticode-signed; use the published SHA-256 list when integrity verification is required.
