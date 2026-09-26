# SNAPVERE Security Policy

Current public line: **SNAPVERE 0.1.19** — https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.19

SNAPVERE currently maintains the **Windows application** plus **Chrome, Edge, Opera and Firefox extensions**. Security reports affecting capture behavior, local screenshot data, settings/history, package extraction, Setup/Portable lifecycle, extension permissions/runtime, release integrity, CI or dependency supply chain are in scope.

`main` may contain later unreleased security/reliability fixes that are not retroactively part of the immutable v0.1.19 binaries. Historical release tags and assets are not rewritten in place.

## Report a vulnerability privately

Do **not** open a public issue for an undisclosed vulnerability.

- If GitHub shows a **Report a vulnerability** / private security reporting option for this repository, use that channel.
- Otherwise contact **info@snapvere.com** and clearly mark the message as a security report.

A useful report should include the affected SNAPVERE surface/version, the smallest reproducible sequence, expected versus actual behavior and the security impact. Include logs or proof-of-concept material only when necessary and redact unrelated personal data, screenshots, credentials, tokens, signing material and server/browser secrets.

No fixed response or disclosure SLA is promised in this repository. Please avoid publishing sensitive exploit details before a fix or mitigation can be evaluated.

## Local-first security model

Core capture does not require a SNAPVERE account, first-party cloud upload, analytics service or advertising service. SNAPVERE does not attempt to bypass protected-content restrictions, privileged browser-page rules or OS capture policy.

A capture failure on protected content or a browser-reserved page is not by itself a security defect when the platform intentionally denies access.

## Windows security boundaries

- Region capture operates on a frozen local frame.
- Screen capture prefers Windows.Graphics.Capture and uses a compatibility monitor backend only for expected acquisition failures.
- Caller cancellation is not converted into fallback work.
- Local PNG output uses staged writes before final atomic publication. Expected access-denied, storage-full and local write failures are classified without exposing raw filesystem exception text or private paths in user-facing recovery copy.
- Local screen recording uses Windows.Graphics.Capture with bounded frame ownership and H.264 MP4 encoding; completed recordings are atomically published, while cancellation, zero-frame output and failures leave no completed partial recording.
- Embedded payload extraction rejects unsafe paths and reparse-point escapes before payload files are admitted. Setup also rejects existing install-directory chains that traverse symbolic links or reparse points and requires an exact trusted installation-marker header before removal.
- Setup/Portable payload integrity is checked against architecture-specific trusted metadata.
- Portable cache corruption or unexpected content causes rejection/rebuild rather than silent execution.
- Package lifecycle is validated for x64 and x86; x86/x64/ARM64 application payloads are built and structurally checked.
- Startup diagnostics are local and size-bounded. Desktop and Portable user-facing fatal startup messages use sanitized categories rather than raw exception text; technical exception details remain only in the local log. Product Contract CI rejects direct Portable `exception.Message` exposure.

## Browser extension security boundaries

The exact permission set is:

```text
activeTab
scripting
downloads
downloads.open
storage
```

`downloads.open` grants only the browser API capability needed to open a completed download after an explicit user action; it does not grant host access. There is no broad host access, remote runtime script, first-party telemetry, advertising SDK, automatic screenshot uploader or remote-control channel.

Capture messages use explicit types and bounded session state. Region completion validates sender tab/window identity. Visible/full/region capture revalidates the active tab around frame acquisition so a tab switch cannot silently admit another tab's frame. Full-page capture is bounded by tile, canvas and pixel limits and releases decoded tile resources after drawing.

The SNAPVERE product name, visible wordmark and saved filename prefix are locked by source and CI checks; user settings cannot rebrand the extension.

## Supply chain and release integrity

NuGet restore enables vulnerability auditing and configured build warnings are treated as errors. GitHub Actions used by active CI are pinned to audited commit SHAs. CodeQL covers C#, JavaScript/TypeScript, Python and GitHub Actions. Dependabot monitors NuGet and GitHub Actions dependencies.

`product-version.json` is the canonical active product contract for Windows and browser distribution. Product Contract CI checks platform/version/package alignment and active documentation links. Browser packages are built reproducibly by CI and public release assets are not mutated under an existing tag.

External browser-store review, publisher authentication/signing and Windows code-signing status must be represented exactly as they exist; repository CI is not a substitute for an external publisher identity.

## Public issue hygiene

Public issues are appropriate for ordinary bugs and feature requests, not undisclosed vulnerabilities or sensitive capture data. Never attach private screenshots, credentials, authentication cookies, signing keys, tokens, crash dumps containing user material or confidential browser/server data to a public issue.
