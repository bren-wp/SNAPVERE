# SNAPVERE 0.1.1 QA Matrix

## Windows gates

- .NET restore with vulnerability auditing.
- x64 build and unit tests.
- x86 build and ARM64 cross-build.
- rendered WinUI snapshot capture and PR comparison.
- universal Setup/Portable build.
- exact two-file public package contract.
- x64 and x86 Setup/Portable lifecycle completion markers.
- package-size regression budget for payloads and public executables.

## Browser gates

- Manifest V3 and exact permission validation.
- locked SNAPVERE name/wordmark/filename-prefix contract.
- Chrome/Edge/Opera parity and documented Firefox differences.
- EN/HR locale-key parity.
- runtime background smoke tests.
- active-tab ownership checks.
- bounded full-page memory behavior.
- region failure feedback after popup closure.
- deterministic ZIP packaging and store-readiness metadata validation.

## Repository gates

- Product Contract CI.
- CodeQL for C#, JavaScript/TypeScript, Python and GitHub Actions.
- pinned workflow actions and dependency monitoring.
