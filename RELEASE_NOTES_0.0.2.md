# SNAPVERE v0.0.2

SNAPVERE v0.0.2 is a startup-stability and release-QA update for the working local-first Windows capture foundation.

## What changed

The main application startup path has been replaced with a conservative programmatic WinUI Capture Center using the standard Windows title bar. This removes the former complex `MainWindow` XAML/composition path that failed during packaged runtime validation.

The capture workflows remain connected:

- Region Capture with frozen-frame selection and exact physical-pixel crop;
- Screen Capture to `Pictures\SNAPVERE`;
- `Ctrl+Shift+1` Region hotkey;
- `Ctrl+Shift+4` Screen hotkey;
- system tray Show, Region, Screen and Exit actions;
- recent local capture discovery;
- local PNG persistence with temporary-file + atomic-move writes.

## Startup fixes

Release QA identified two separate failure modes while hardening the first binary build:

- the former `MainWindow.xaml` load path could fail with `0x802B000A` during `Application.LoadComponent`;
- runtime testing on the `windows-latest` GitHub image later produced `0xC000027B` after successful activation because that image had moved to Windows Server 2025, outside the Windows App SDK 1.8 server support target used by SNAPVERE.

v0.0.2 removes the fragile MainWindow startup path and runs WinUI launch gates on the supported `windows-2022` runner.

## Release validation

Both **x64** and **x86** must pass the same end-to-end gate before this release is published:

1. Release build and self-contained app publish;
2. Setup and Portable executable generation;
3. silent Setup rejection without explicit MPL 2.0 acceptance;
4. silent Setup installation with `--accept-license`;
5. activated WinUI `SNAPVERE 0.0.2 READY` probe;
6. normal installed application launch remaining alive through the startup window;
7. Setup-based silent uninstall and payload cleanup;
8. Portable READY probe;
9. Portable normal launch leaving a real SNAPVERE process alive.

## Downloads

The release contains:

- `SNAPVERE-0.0.2-Setup-x64.exe`
- `SNAPVERE-0.0.2-Portable-x64.exe`
- `SNAPVERE-0.0.2-Setup-x86.exe`
- `SNAPVERE-0.0.2-Portable-x86.exe`
- `SNAPVERE-0.0.2-x64.zip`
- `SNAPVERE-0.0.2-x86.zip`
- `SHA256SUMS.txt`

The application payloads are self-contained. A separate .NET runtime or Windows App SDK runtime installation is not required for these packaged builds.

## Installation

Setup installs per user by default to:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

Uninstall is handled by the installed `SNAPVERE-Setup.exe --uninstall`; no separate `uninstall.exe` is installed. Screenshots in `Pictures\SNAPVERE` are preserved.

## Integrity and signing

v0.0.2 executables are intentionally not Authenticode-signed. Verify downloads with the included `SHA256SUMS.txt` when integrity checking is required.

## Still intentionally deferred

The following are not represented as finished in this release:

- Windows.Graphics.Capture/D3D primary acquisition backend;
- cross-monitor Region overlay;
- Window Capture;
- Scrolling Capture;
- clipboard Quick Actions;
- annotation Editor;
- full History management and Pin to Screen;
- OCR;
- automatic updater.

**SNAPVERE — Capture anything.**  
Developed and published by **Brendigo**.
