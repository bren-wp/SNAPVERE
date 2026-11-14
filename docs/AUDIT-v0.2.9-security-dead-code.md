# SNAPVERE v0.2.9 — security and dead-code audit

**Review date:** 14 November 2026. **Branch:** `feature/snapvere-v0.2.9-premium-modular-audit`. The scope is the Tauri 2 / Rust / React repository edition, not the separately deployed snapvere.com WinUI edition. This audit documents what was actually inspected and fixed; it does **not** certify zero crashes, zero vulnerabilities, an external penetration test, or 1:1 pixel parity.

## Scope and evidence

I inspected 19 Rust modules (including Windows DXGI, Media Foundation, audio and cursor FFI), 25 TypeScript/TSX modules and four first-party website JS files. Checked dangerous Rust `panic!()`, `todo!()`, `unimplemented!()`, `unwrap()`, `expect()` and unsafe-block patterns and browser JS evaluation, DOM HTML sinks and localStorage access. Reviewed Tauri capabilities, the 35-command typed IPC bridge, SQLite gallery and packaging/CI. Production Rust modules examined contained no direct `panic!()`, `todo!()` or `unimplemented!()` calls; observed `expect()` calls were confined to tests. Windows FFI unsafe blocks still need platform/runtime inspection.

Automated gates: TypeScript `tsc --noEmit`, Node tests, Rust `cargo fmt --all -- --check`, `cargo test --locked`, strict Clippy `-D warnings`, `npm audit --audit-level=high`, `tools/check_ipc_contract.py`, `tools/check_public_distribution.py`, site checksum checks, visual brand token checks, Playwright UI previews and new `tools/check_frontend_security.py`. All gates apply to the new commit; a successful previous revision does not verify later changes.

## Findings and remediation

| Priority | Issue / regression | Status |
|---|---|---|
| Critical data reliability | Recording startup could overlap before the native session slot was populated. An older worker could clear a new one. | Fixed with atomic start guard, session identity check under mutex, cleanup ordering and tests |
| High | Same-second recordings chose identical filenames and risked overwriting media. | Fixed by adding a randomized suffix; add stress/recovery tests on actual Windows |
| High | Negative recording region coordinates were shifted rather than correctly clipped. | Fixed i64 intersection and Rust regression tests |
| High | Truncated BGRA buffers could panic inside privacy-redaction pixel loop. | Added length and overflow guards and truncation test |
| Medium | Corrupt or blocked WebView localStorage could prevent app startup or break media preferences. | Fallback to safe theme, validate volume and Settings model |
| Medium | Web download/cookie-consent JavaScript used `innerHTML` string assignments. | Replaced with DOM element creation and new CI injection-sink check |
| Medium | Overlong viewer/Settings/record/export modules were hard to audit. | Split viewer stages, controls, preference model, recording-pixel and export-validation modules |
| Medium | Video player rendered progress at frame-rate, and scrubber lacked full keyboard access. | Throttle to 10Hz; add keyboard seek and additional speeds |
| Medium | No keyboard fine-tuning of selected recording/screenshot region. | Add 1px nudge, Shift+10px and Alt+arrow resize with tested geometry |
| Medium | Autostart setting could diverge from Windows on persistence failure. | Restore prior OS state when persistence fails; test on physical Windows still required |
| High, open | Save As still accepts an arbitrary destination path from a privileged webview IPC call. | Native file-picker authorisation / single-use export capability required for strict trust-boundary separation |
| High, open | Broad Tauri permissions assigned across windows (including webview creation). | Split per-window capabilities and confirm screenshots/recording/Settings still work |
| High, open | Windows binaries are not Authenticode-signed. | Add signing and installer/SmartScreen QA before claiming trusted production distribution |
| High, open | Site `site/files` binary provenance and deployed WinUI packaging differ from repository Tauri release history. | Do not overwrite public downloads until release identity and SHA-256 provenance are verified |
| Medium, open | Website CSP currently permissive relative to a nonce-/hash-based policy. | Harden after validating consent-only GA behavior, downloads and front-page scripts |
| Medium, open | Rust dependency advisory scanning not automated. | Add pinned RustSec / cargo-audit integration to supplement npm audit |

## Dead-code observations

- `tsc` and strict Rust Clippy check unused code in compiled targets; `tools/check_ipc_contract.py` confirms that all 35 declared typed IPC calls match native handler registrations.
- `tauri-plugin-fs` and `tauri-plugin-os` were removed from live runtime/capabilities in v0.2.8 but still appear as direct Cargo dependencies and in Cargo.lock. They are candidates for controlled removal with lockfile regeneration; untested deletion would break `--locked` CI.
- `src-tauri/examples/captest.rs` is a manual diagnostic example, not production startup. Do not delete until hardware capture regression tests replace it.
- `Session.join` is intentionally retained with `#[allow(dead_code)]` to hold the worker handle; review thread ownership before changing shutdown semantics.
- SQLite remote-sharing fields are currently dormant but belong to the persisted schema. Do not drop them without a reversible migration preserving existing user databases and tags.
- `docs/screenshots` / `site/screenshots` are used in marketing and previews. They are actual rendered frontend images with a mock Tauri bridge, not evidence of production hardware output.

## Performance/UX results and remaining acceptance

A single gallery viewer file has been decomposed into focused video/image/error stages; overlay geometry and Settings preferences can now be unit-tested independently. PNG encoder already avoids full-frame copies, capture and search pagination remains bounded, and video progress now generates fewer React state updates. No exact CPU/heap improvement figure is claimed without a reproducible performance benchmark.

Before final release, verify real Windows 10/11 and mixed-DPI multi-monitor capture, H.264/GIF lifecycle, microphone and system loopback, redaction, large-file export, interrupted disk writes, browser window lifecycle, NSIS/MSI license acceptance/decline and upgrade, 500+ gallery items, hotkey conflicts, keyboard navigation and zoom 200–400%, then compare every screen with correct reference images at the same resolution/theme/scaling.

**Distribution boundary:** GitHub repository Tauri Windows release and the live snapvere.com .NET/WinUI distribution are separate products. Do not silently sync binaries or claim 1:1 UI equality between them.

## Privilege reduction (v0.2.9)

- Extracted `autostart:default` and `opener:default` from the shared WebView capability to a new `settings-privileged` capability restricted to the `settings` window.
- Removed `global-shortcut:default` and all frontend `clipboard-manager:*` image/text permissions: shortcut registration and clipboard writes run in native Rust handlers, not through direct untrusted WebView plugin calls. Kept native registered plugins, so native functionality remains available.
- Added a CI verifier that rejects any accidental reintroduction of shared privileged permissions and imports from other windows.
- This narrows WebView plugin permissions but is *not* a guarantee of isolated Rust commands: custom `invoke` handlers must still validate inputs and intended window contexts before acting on local files.


## Release credential isolation and dependency lifecycle (v0.2.9)

The published Windows release job requires `contents: write` to create releases. Prior to v0.2.9 its checkout persisted the write-capable Git credentials, and `npm ci` allowed third-party dependency lifecycle scripts to run. That combination provides a potential supply-chain exfiltration path if a dependency or build script is compromised. The new release workflow uses `persist-credentials: false` and `npm ci --ignore-scripts` so npm packages cannot read a persisted Git write token or run arbitrary post-install scripts during CI dependency installation. Rust build scripts still execute as part of compiling trusted dependency sources and must be covered by RustSec and release provenance review. The release-publishing action still has a write-scoped token, so third-party GitHub Actions should be commit-SHA pinned or replaced with reviewed first-party tooling before high-assurance production distribution.
