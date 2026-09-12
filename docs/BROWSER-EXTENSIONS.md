# Browser Extensions

SNAPVERE 0.1.1 includes official public browser-extension packages for Chrome, Edge, Opera and Firefox under [`ekstenzije/`](../ekstenzije/). The browser code was developed after v0.1.0; v0.1.1 is the first release that promotes it into the public GitHub Release contract.

## Release version and assets

All four extension manifests and the canonical store listing use version **0.1.1**.

The v0.1.1 GitHub Release contains:

```text
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

These four browser files are part of the new eight-asset v0.1.1 release contract. Historical v0.1.0 remains unchanged with only its original Windows/Android assets.

## Architecture

Chromium-family builds use Manifest V3 with `background.service_worker`. Firefox uses Manifest V3 with Firefox-compatible `background.scripts`. All variants share the same capture logic, popup/options UI, localization keys and local-first privacy model.

The browser packages do not use external runtime dependencies.

### Visible capture

The background context acquires a bounded capture lock, captures the current active-tab viewport with the browser screenshot API, and saves a PNG using the downloads API.

### Region capture

The background stores the capture token, tab/window identity and start time in `storage.local` before injecting the local region selector. This avoids relying only on service-worker RAM while the user decides what to select.

The content helper removes its overlay before the screenshot is taken. The captured viewport is cropped locally using a canvas. Escape cancels and pointer/keyboard listeners plus injected overlay nodes are cleaned in final paths.

### Full-page capture

Full-page capture:

1. records the original scroll position;
2. measures document/viewport dimensions;
3. discovers visible fixed/sticky elements;
4. builds a bounded viewport-tile grid;
5. scrolls to each tile with paint settling;
6. captures and transfers each tile to the content helper;
7. hides floating elements after the first tile to reduce repeated overlays;
8. assembles tiles into a bounded final canvas;
9. downloads a local PNG;
10. restores floating elements and the original scroll position.

Safety limits reject pages that exceed supported canvas dimensions, pixel count or tile count instead of attempting uncontrolled memory allocation. A content-side watchdog restores page state if orchestration disappears unexpectedly.

## Security controls

Messages are accepted only for explicit known message types. Capture tokens and sender tab/window identifiers are validated for region completion. Page text is not inserted with `innerHTML`; extension UI uses fixed extension-owned markup and `textContent` for localized strings/status.

No `<all_urls>` permission or broad `host_permissions` entry is requested. The extension does not attempt to bypass browser-protected pages.

## Privacy

There is no telemetry, analytics, ad SDK, cloud upload, automatic screenshot transfer, remote runtime code or background network client. Settings and active-capture metadata stay in browser-local storage.

The version-controlled browser privacy policy is [`ekstenzije/PRIVACY.md`](../ekstenzije/PRIVACY.md).

## Localization

English is the default/fallback locale. Dedicated English and Croatian catalogs are included in every distributable browser package.

## Reproducible packaging

`ekstenzije/tools/package-extensions.sh` creates the four release ZIP files from staged source. It normalizes file/directory timestamps to the portable ZIP epoch, sorts archive paths and uses `zip -X` to remove nonessential host metadata. `SHA256SUMS.txt` is generated next to the packages.

CI and the 0.1.1 release workflow execute packaging twice in separate output directories. Corresponding ZIPs and checksum manifests must be byte-identical before publication.

## Cross-browser parity controls

`ekstenzije/tools/verify-extension-parity.mjs` prevents silent divergence by verifying:

- identical paths and SHA-256 content for shared runtime files across Chrome, Edge, Opera and Firefox;
- identical Chrome/Edge/Opera manifests;
- only expected background/Gecko differences in Firefox;
- aligned manifest versions;
- exact icon dimensions and bytes;
- no inline script/style blocks, inline event handlers or remote HTML runtime resources.

These checks complement the permission, locale, source-policy and manifest validator.

## Store submission readiness

`ekstenzije/store/listing.json` is the canonical machine-readable store contract. It contains EN/HR listing copy, the single-purpose statement, permission justifications, local-first privacy/data-practice declarations, exact ZIP names and store-asset references.

`ekstenzije/store/reviewer-notes.md` gives external reviewers deterministic functional test steps and documents protected-page behavior, permission use, network/privacy behavior, full-page limits and the Firefox source/signing boundary.

`ekstenzije/tools/generate-store-assets.py` deterministically creates listing screenshots and promo tiles. Generated PNGs are CI artifacts rather than committed binaries.

`ekstenzije/tools/validate-store-readiness.mjs` validates store material against actual manifests, including version alignment, the four-permission allow-list, no broad host permissions, local-first privacy flags, EN/HR metadata completeness, exact package names, required asset references and exact PNG dimensions.

Store publication itself is external. Authenticated publisher access, store-side submission, certification/review and Firefox signing are not implied by a GitHub release or green repository CI run.

## Release and QA boundary

`.github/workflows/extensions-ci.yml` provides syntax, manifest, permission, locale, cross-browser parity, source-policy, store-readiness, generated-asset, deterministic packaging, SHA-256 and package-content validation.

`.github/workflows/release-0.1.1.yml` repeats the browser validation and reproducible packaging for the exact release commit, transfers the four browser ZIPs into the final release job, recomputes hashes and verifies GitHub's published asset digests after publication.

A green workflow is automated static/package evidence; it is not a claim that every browser build/web application has been manually exercised or that external stores have approved the extension.

See [`ekstenzije/README.md`](../ekstenzije/README.md) for development loading, packaging, privacy, store-submission material and known limitations.
