# Security Policy

## Scope

Security issues affecting SNAPVERE capture data, temporary files, clipboard behavior, update verification, licensing, image parsing or local history should be treated as sensitive.

## Sensitive information rules

SNAPVERE must not intentionally write screenshot pixels, OCR text, clipboard contents, file contents, license secrets or private keys to routine logs.

Temporary screenshot material must be scoped to the current user, cleaned after use and removed by startup cleanup when stale.

## Update security

Production update manifests must be digitally signed and include at minimum version, architecture, channel, download URL, SHA-256, published date and signature. Desktop code must verify manifest signature, artifact hash, architecture, version direction and expected origin before applying an update.

## Repository hygiene

Never commit:

- Authenticode/private signing keys
- production API credentials or passwords
- private user screenshots
- local history/settings data
- crash dumps containing user material
- generated package output

## Reporting

Please use a private security reporting channel rather than a public issue when disclosure could expose users or a practical exploit. Public security documentation will be expanded before the first production release.
