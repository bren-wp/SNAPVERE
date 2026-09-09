# Installation and Portable Builds

## Supported release architectures

SNAPVERE 0.0.2 publishes native Windows artifacts for:

- **x64** — normal 64-bit Windows systems;
- **x86** — 32-bit Windows systems. `x32` is another informal name for the same architecture.

ARM64 remains a source/build target but is not part of the 0.0.2 public binary release while release QA is focused on x64 and x86.

## Platform target

The application targets Windows 10 version 1809 / build 17763 or later. Windows 11 is supported through the same Windows App SDK application model.

GitHub Actions runtime launch validation for v0.0.2 uses the `windows-2022` runner. The release gate does not use `windows-latest` because that label currently resolves to Windows Server 2025, which is outside the Windows App SDK 1.8 server support matrix used by this project.

## Setup

The default Setup install directory is:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

This is a per-user location, so the default installation does not require administrator privileges.

Interactive Setup requires acceptance of the repository's Mozilla Public License 2.0 terms. The installer can create Start menu and Desktop shortcuts.

### Uninstall

Setup registers SNAPVERE under the current user's Windows Installed apps list. Windows invokes the installed `SNAPVERE-Setup.exe --uninstall` maintenance mode. There is no standalone `uninstall.exe` payload.

Before destructive cleanup, the maintenance path must contain the SNAPVERE installation marker and expected application/Setup files. If the marker is missing or invalid, Setup refuses to remove the directory.

Uninstall removes application files, shortcuts and uninstall registration. It deliberately does **not** remove screenshots in `Pictures\SNAPVERE`.

### Silent mode

Silent installation is explicit about license acceptance:

```text
SNAPVERE-0.0.2-Setup-x64.exe --silent --accept-license
```

A silent install without `--accept-license` exits with code `2` without installing.

Silent uninstall:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

## Portable

The Portable release is one launcher executable per architecture. It embeds the self-contained SNAPVERE application payload, extracts that payload into a versioned directory beneath the Windows temporary directory and launches `Snapvere.exe` from that private cache.

The portable launcher:

- does not register SNAPVERE as an installed application;
- does not create Start menu or Desktop shortcuts;
- does not require a separately installed .NET runtime or Windows App SDK runtime;
- validates extraction paths before writing files;
- bounds archive entry count and expanded size;
- reuses the cache for the same SNAPVERE version and architecture;
- removes stale SNAPVERE portable caches on later runs when possible.

Screenshots are still saved to the normal `Pictures\SNAPVERE` capture folder.

## Runtime validation

For each x64 and x86 package, CI and release QA require all of the following:

1. the Setup executable rejects silent installation without `--accept-license`;
2. silent installation succeeds with explicit license acceptance;
3. installed `Snapvere.exe` reaches an activated WinUI main window and emits `SNAPVERE 0.0.2 READY`;
4. a normal installed launch remains alive through the startup validation window;
5. Setup-based silent uninstall completes and removes the application payload;
6. Portable reaches the same activated-window READY marker;
7. Portable normal launch leaves a real `Snapvere.exe` process alive.

A failure in any of these steps blocks release publication.

Startup diagnostics are written to:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

The log records startup stages and exception metadata; screenshot pixels and capture content are not written to it.

## Integrity

Every GitHub Release includes `SHA256SUMS.txt`. Compare downloaded files against those hashes before deployment when integrity verification is required.

The 0.0.2 binaries are intentionally not Authenticode-signed. SHA-256 checksums are published for artifact integrity verification.
