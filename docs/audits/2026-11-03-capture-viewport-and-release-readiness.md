# SNAPVERE — scoped security, dead-code and release-readiness audit (2026-11-03)

## Evidence and scope
- Baseline: `main` commit `97245ad4a20790d20ef01016e60f6ac7739ffdf5` (verified against GitHub); release `v0.1.37`, published 2026-10-29, contains all nine expected artifact filenames.
- Current GitHub PR API returned no open pull requests; Issues search returned none.
- GitHub Actions run list for the baseline showed CI, Product Contract CI, Browser Extensions CI, Localization Completeness CI, MSI Build CI, Windows ARM64 Runtime CI and CodeQL Advanced successful. These are historical checks, **not** results for this proposed change.
- GitHub rulesets endpoint returned an empty list. Standard branch-protection read returned `403 Resource not accessible by integration`, so its status cannot be verified through this connector.
- Code inspected for this specific change: `ekstenzije/chrome/background.js`, `ekstenzije/chrome/capture.js`, `ekstenzije/tools/smoke-test-background.mjs`, `ekstenzije/tools/test-capture-runtime.mjs`, `ekstenzije/tools/verify-extension-parity.mjs`, `.github/workflows/extensions-ci.yml`, `.github/workflows/release.yml`, `eng/audit-dead-code.py`, both READMEs, and `docs/CODE-AUDIT.md`. This is **not** a completed whole-repository source review, threat model, dead-code proof, or interactive Windows UI audit.

## Finding SV-2026-11-03-01 — P2 — off-viewport region geometry accepted
- Location: `ekstenzije/chrome/background.js` in `handleRegionSelected()`, and `ekstenzije/chrome/capture.js` in `cropRegion()`. Source copies for Edge, Opera, and Firefox are intended to remain byte-identical to Chrome.
- Cause: the background validates finite coordinates, minimum width/height and viewport positivity but not origin bounds or the right/bottom edges. The content script likewise validates finite coordinates and viewport changes but did not reject off-viewport or undersized region rectangles before decoding the screenshot.
- Impact: invalid or internally inconsistent geometry can proceed into screen pixel capture and PNG decoding before failing or cropping the wrong bounds. No external exploitation or data exfiltration has been established.
- Reproduction / regression coverage: add negative-origin, right/bottom overflow, and very large finite coordinate cases; assert rejection before pixel capture/download/decode, and that the background capture lock is released.
- Remediation in this PR: validate rect against the captured viewport at both the background and content-script boundaries. Retain existing size limits and callback ownership validation.
- Verification status: **code change prepared; GitHub CI checks pending for the exact PR SHA**. Do not report as tested until the actual CI result is read.

## Finding SV-2026-11-03-02 — P1 process control gap — no repo rulesets listed
- GitHub API `GET /repos/bren-wp/SNAPVERE/rulesets` returned `[]`; this does not prove there is no separate classic branch protection because its endpoint returned 403.
- Recommendation: repository owner should require PR review, status checks bound to the current head SHA, and block direct changes to `main`. Confirm the seven existing workflow job check names in the GitHub Rules UI before enforcing them. GitHub App connector has no branch-protection administration endpoint available here.
- Verification status: **not configured by this change**.

## Dead-code inventory and pixel-perfect validation limitations
- Existing `eng/audit-dead-code.py` uses lexical/regex heuristics with cross-file scans and known regressions, not a semantic guarantee. No additional symbol is classified as proven dead or deleted in this PR.
- Original reference ZIPs `SNAPVERE-Premium-Branding-Concept(3).zip` and `(4).zip` were not found by exact-title Library/conversation search. No claim of reference-based pixel-perfect 1:1 parity is made.
- Three existing Windows screenshots remain under `assets/branding/readme/screenshots/`. No new runtime-captured screenshots are claimed. Browser screenshot generation requires real Chrome/Edge/Opera/Firefox or UI automation and verified reference images.
- README changes make existing release packages individually downloadable via stable `releases/latest/download` links; links should be smoke-tested on publication.

## Required release gates for the next release
1. Run all seven GitHub checks on the actual PR head SHA and examine any failures.
2. Run browser parity, capture runtime tests and packaging reproducibility on the same code.
3. Validate Windows functional and MSI lifecycle tests, ARM64, localization, NuGet audit, and CodeQL.
4. Capture actual Windows/browser screenshots and compare with the original premium references before claiming pixel-perfect 1:1.
5. Only then merge and prepare the next synchronized version, publish the nine package artifacts, and verify SHA-256/SBOM/signatures as applicable.

This document intentionally distinguishes observed facts, planned regressions, and unverified work.
