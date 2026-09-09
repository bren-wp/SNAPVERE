# Installation and Portable Builds

## Supported release architectures

SNAPVERE 0.0.1 publishes native Windows artifacts for:

- **x64** — normal 64-bit Windows systems;
- **x86** — 32-bit Windows systems. `x32` is another informal name for the same 32-bit architecture.

ARM64 remains a source/build target but is not part of the first public binary release while release QA is focused on x64 and x86.

## Setup

The default Setup install directory is:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

This is a per-user location, so the default installation does not require administrator privileges.

Interactive Setup requires acceptance of the repository's Mozilla Public License 2.0 terms. The installer can create Start menu and Desktop shortcuts.

### Uninstall

Setup registers SNAPVERE under the current user's Windows Installed apps list. Windows invokes the installed `SNAPVERE-Setup.exe --uninstall` maintenance mode. There is no standalone `uninstall.exe` payload.

Uninstall removes application files, shortcuts and uninstall registration. It deliberately does **not** remove screenshots in `Pictures\SNAPVERE`.

### Silent mode

Silent installation is explicit about license acceptance:

```text
SNAPVERE-0.0.1-Setup-x64.exe --silent --accept-license
```

A silent install without `--accept-license` exits without installing.

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
- reuses the cache for the same SNAPVERE version and architecture;
- removes stale SNAPVERE portable caches on later runs when possible.

Screenshots are still saved to the normal `Pictures\SNAPVERE` capture folder.

## Integrity

Every GitHub Release includes `SHA256SUMS.txt`. Compare downloaded files against those hashes before deployment when integrity verification is required.

The 0.0.1 binaries are not Authenticode-signed. Code signing is a separate release-hardening milestone and should not be simulated with an untrusted certificate.
