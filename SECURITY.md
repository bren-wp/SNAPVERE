# SNAPVERE Security Policy

Current public line: **SNAPVERE 0.1.1** — https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

Security reports affecting the Windows application, Chrome/Edge/Opera/Firefox extensions, packaging, local capture data, settings/history, release integrity or dependency supply chain are in scope.

## Local-first security model

Core capture does not require a SNAPVERE account, first-party cloud upload, analytics service or advertising service. SNAPVERE does not attempt to bypass protected-content restrictions, privileged browser-page rules or OS capture policy.

## Windows

- Region capture operates on a frozen local frame.
- Screen capture prefers Windows.Graphics.Capture and uses a compatibility monitor backend only for expected acquisition failures.
- Caller cancellation is not converted into fallback work.
- Local PNG output uses staged writes before final move.
- Setup/Portable extraction rejects unsafe paths and validates architecture payload integrity.
- Package lifecycle is tested for x64 and x86; x86/x64/ARM64 payloads are built and structurally validated.
- Startup diagnostics are local and size-bounded. User-facing fatal startup messages do not expose raw exception details; technical information remains in the local log.

## Browser extensions

The permission set is exactly:

```text
activeTab
scripting
downloads
storage
```

There is no broad host access, remote runtime script, first-party telemetry, advertising SDK or automatic screenshot uploader.

Capture messages use explicit types and session tokens. Region completion validates sender tab/window identity. Visible/full/region capture revalidates the active tab around frame acquisition. Full-page capture is bounded by tile, canvas and pixel limits and releases decoded tiles after they are drawn.

The SNAPVERE product name and saved filename prefix are locked by source and CI checks; user settings cannot rebrand the extension.

## Supply chain and release integrity

NuGet restore enables vulnerability auditing and build warnings are treated as errors where configured. GitHub Actions are pinned to commit SHAs. CodeQL covers C#, JavaScript/TypeScript, Python and workflow code. Dependabot monitors NuGet and GitHub Actions dependencies.

`product-version.json` is the canonical active product contract for Windows and browser distribution. Published tags/assets are historical output and are not silently rewritten by post-release maintenance.

## Reporting

Use GitHub private security reporting for sensitive issues. General security/support contact: **info@snapvere.com**.

Do not post private screenshots, credentials, signing material or sensitive proof-of-concept data in a public issue.
