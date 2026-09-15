# SNAPVERE 0.1.2 QA Matrix

This matrix documents automated regression evidence. A green gate means the tested contract passed on that commit; it does not convert platform behavior into an absolute guarantee.

## Windows build and runtime gates

- .NET restore with vulnerability auditing.
- x64 build and unit-test suite.
- x86 build and ARM64 cross-build.
- native payload-structure validation.
- real rendered WinUI snapshot capture for Region, Window, Tray, Options, Language and About surfaces.
- PR visual comparison against the last successful `main` baseline.
- universal Setup/Portable construction.
- exact two-file Windows public package contract.
- package-size regression budgets for payloads and public executables.
- x64 and x86 Setup/Portable lifecycle completion markers.
- tray-first launch behavior inside the package lifecycle probes.

## Capture-path regression focus

The current Windows gates exercise the same Region and Window overlay surfaces affected by recent memory hardening. Build/test plus rendered-surface validation therefore checks both compilation and actual WinUI publication of frozen capture images after direct-buffer and early-release changes.

## Browser gates

- Manifest V3 and exact permission validation.
- locked SNAPVERE name, wordmark and filename-prefix contract.
- Chrome/Edge/Opera parity plus documented Firefox differences.
- EN/HR locale-key parity.
- runtime background smoke tests.
- active-tab ownership checks before and after capture.
- bounded full-page memory behavior.
- region failure feedback after popup closure.
- deterministic ZIP packaging.
- store-readiness metadata validation without claiming external approval.

## Repository and security gates

- Product Contract CI.
- CodeQL for C#, JavaScript/TypeScript, Python and GitHub Actions.
- pinned workflow actions.
- dependency monitoring.
- Markdown/product-contract link validation for active documentation.

See [Performance & Stability](PERFORMANCE.md), [Product Status](PRODUCT-STATUS.md) and [Security Policy](../SECURITY.md).
