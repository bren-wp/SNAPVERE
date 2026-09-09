# Security Policy

## Scope

Security issues affecting SNAPVERE capture data, temporary files, installer/portable extraction, clipboard behavior, future update verification, image parsing or local history should be treated as sensitive.

## Sensitive information rules

SNAPVERE must not intentionally write screenshot pixels, OCR text, clipboard contents, file contents, license secrets or private keys to routine logs.

Temporary screenshot material must be scoped to the current user, cleaned after use and removed when stale where the workflow creates temporary state.

## Release packaging security

SNAPVERE 0.0.1 Setup and Portable packages use guarded embedded ZIP extraction. Extraction rejects absolute paths and destination escapes, bounds archive entries and total expanded size, and writes through staging/temp paths before final replacement.

Setup installs per-user by default. Destructive uninstall cleanup is allowed only for a directory containing the SNAPVERE installation marker and expected application/maintenance files. Windows Installed apps invokes the installed `SNAPVERE-Setup.exe --uninstall`; SNAPVERE does not install a separate `uninstall.exe`.

The 0.0.1 binaries are intentionally not Authenticode-signed. GitHub Release publishes `SHA256SUMS.txt` so downloaded artifacts can be verified against the release checksums.

## Update security

SNAPVERE 0.0.1 does not include an automatic updater. A future production update mechanism must verify signed update metadata plus artifact SHA-256, architecture, version direction and expected origin before applying an update. Transport security alone must not be treated as sufficient update authenticity.

## Repository hygiene

Never commit:

- Authenticode/private signing keys
- production API credentials or passwords
- private user screenshots
- local history/settings data
- crash dumps containing user material
- generated package output

## Reporting

Please use a private security reporting channel rather than a public issue when disclosure could expose users or a practical exploit.
