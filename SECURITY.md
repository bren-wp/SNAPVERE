# Security Policy

## Scope

Security issues affecting SNAPVERE capture data, clipboard behavior, local preferences/history, native window targeting, temporary files, Setup/Portable extraction, uninstall cleanup, release integrity or future update mechanisms should be treated as sensitive.

SNAPVERE is a local-first Windows capture application. Core capture does not require telemetry, cloud upload, accounts, credentials or an external network service.

## Sensitive information rules

SNAPVERE must not intentionally write the following to routine diagnostics:

- screenshot pixels or image contents;
- clipboard contents;
- OCR text;
- user file contents;
- credentials, API keys or signing material;
- private user capture history beyond paths/metadata needed by the local feature.

Startup diagnostics record stage and exception metadata only and remain under the current Windows account.

## Capture security boundaries

SNAPVERE does not attempt to bypass DRM, protected-content restrictions or Windows capture policy. A blocked/blank/protected capture is not an authorization to weaken OS protections or introduce a bypass backend.

Window Capture snapshots eligible top-level windows before always-on-top picker overlays appear and geometrically hit-tests that frozen list. Self, tool, invisible, cloaked and invalid targets are filtered so SNAPVERE overlays are not selected as normal capture targets.

Caller cancellation must not be converted into hidden fallback work. Expected WGC compatibility/native/timeout failures may use the documented monitor GDI fallback; unexpected programming errors are allowed to surface.

## Local settings

Implemented preferences are stored at:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Writes use a temporary file followed by atomic replacement/move. Corrupt or unreadable settings fall back to safe defaults rather than executing data from the file.

**Start SNAPVERE with Windows** uses the current user's Windows `Run` key. Installed Setup uninstall removes the SNAPVERE Run value only when its command points exactly to the validated installed `Snapvere.exe`. A different/Portable registration is deliberately preserved.

## Package extraction security

Setup and Portable packages use guarded embedded ZIP extraction. Extraction must:

- reject absolute paths;
- reject path traversal/destination escape;
- canonicalize target paths before writing;
- bound entry count and total expanded size;
- write through private staging/versioned cache locations;
- avoid shell command construction for child process arguments;
- clean stale staging/cache data when safe.

Portable launch preparation is mutex-protected and a launcher must not report normal-startup success if the extracted child exits immediately.

## Setup/uninstall security

Setup is per-user by default. Before destructive install-directory removal, Setup validates the SNAPVERE installation marker and expected application/maintenance files. An untrusted arbitrary directory must never be recursively removed merely because it was supplied as a command-line path or registry value.

Windows Installed apps invokes the installed:

```text
SNAPVERE-Setup.exe --uninstall
```

SNAPVERE intentionally does not create a separate `uninstall.exe`, `uninstaller.exe`, `unins000.exe` or `unins*.exe` payload.

User captures under `Pictures\SNAPVERE` are outside the install directory and are preserved by uninstall.

## Release integrity

The 0.0.6 release workflow builds x64/x86 self-contained application, Setup and Portable artifacts from the release commit, runs the strict package lifecycle plus tray-first and secondary-UI runtime gates, creates `SHA256SUMS.txt`, and only then may publish the immutable `v0.0.6` tag/release.

0.0.6 binaries are not Authenticode-signed. Consumers should verify the published SHA-256 checksums when integrity assurance is required.

The release tag must identify exactly the validated release commit; published assets must not be substituted with artifacts from a different tree. Existing release assets are treated as immutable by the workflow.

## Update security

SNAPVERE 0.0.6 does **not** include an automatic updater or licensing network feature. A future updater must verify signed metadata plus artifact hash, architecture, expected origin and version direction before applying an update. TLS transport alone is not sufficient authenticity.

## Repository hygiene

Never commit:

- Authenticode/private signing keys;
- production API credentials or passwords;
- private user screenshots;
- `%LOCALAPPDATA%\SNAPVERE` settings/logs/history from real users;
- crash dumps containing user material;
- generated release package output unless it is an intentionally tracked public artifact;
- secrets embedded in workflow files or source.

## Reporting

Use GitHub private security reporting or another private project security channel when disclosure could expose users or a practical exploit. Do not post sensitive proof-of-concept user data in a public issue.
