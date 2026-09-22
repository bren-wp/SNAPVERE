# Installing SNAPVERE

Current public release: **v0.1.13**. Published tags and assets are immutable; later `main` hardening is not retroactively part of that release.

## Windows Setup

Download `SNAPVERE-Setup.exe` from the current release and run it normally. The universal package contains validated x86, x64 and ARM64 application payloads and chooses the compatible payload for the machine.

The interactive installer:

- requires the embedded commercial license to be readable before acceptance,
- installs per-user by default under `%LOCALAPPDATA%\Programs\SNAPVERE`,
- lets you choose a different folder without accidentally duplicating a trailing `SNAPVERE\SNAPVERE` segment,
- stages payload extraction before publication,
- refuses direct drive-root, Windows-system and reparse-point installation paths,
- refuses to replace a non-empty custom target unless it is a validated existing SNAPVERE installation,
- preserves the previous validated installation when an upgrade cannot complete,
- registers uninstall metadata for Windows Installed Apps,
- can create Start menu/Desktop shortcuts and enable current-user Windows startup,
- prevents the wizard from being closed while a file mutation is actively completing,
- keeps user-facing installation and removal errors sanitized,
- keeps Installed Apps metadata, startup registration and SNAPVERE-owned shortcuts in place if file deletion fails, so a blocked removal can be repaired or retried instead of leaving an unregistered installation,
- requires a positive parent PID for internal deferred cleanup, waits for that process when present and serializes cleanup with normal Setup operations so a delayed uninstall cannot race a newer install or repair.

Silent per-user installation is supported with:

```text
SNAPVERE-Setup.exe --silent --accept-license
```

Silent removal is supported with:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

A silent install without `--accept-license` exits without installing.

## Windows Portable

`SNAPVERE-Portable.exe` is the portable option. It validates its embedded payload before reuse and does not require a traditional installation.

## Browser extensions

The current release provides:

- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

The ZIP files are source-ready release packages for manual installation. Store publication is a separate external process and is not claimed unless an actual store listing exists.

Current release: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.6
