# Browser Extensions

SNAPVERE includes post-v0.1.0 browser-extension development for Chrome, Edge, Opera and Firefox under [`ekstenzije/`](../ekstenzije/).

## Architecture

Chromium-family builds use Manifest V3 with `background.service_worker`. Firefox remains Manifest V3 but uses Firefox-compatible `background.scripts`. All variants share the same capture logic, popup/options UI, localization keys and local-first privacy model.

The browser package does not use external runtime dependencies.

### Visible capture

The background context acquires a bounded capture lock, captures the current active-tab viewport with the browser screenshot API, and saves a PNG using the downloads API.

### Region capture

The background stores the capture token, tab/window identity and start time in `storage.local` before injecting the local region selector. This avoids relying only on service-worker RAM while the user is deciding what to select.

The content helper removes its overlay before the screenshot is taken. The captured viewport is cropped locally using a canvas. Escape cancels and all pointer/keyboard listeners and injected overlay nodes are cleaned.

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

Safety limits reject pages that exceed the supported canvas dimensions, pixel count or tile count instead of attempting uncontrolled memory allocation.

A content-side watchdog restores state if orchestration disappears unexpectedly.

## Security controls

Messages are accepted only for explicit known message types. Capture tokens and sender tab/window identifiers are validated for region completion. Page text is not inserted with `innerHTML`; extension UI uses fixed extension-owned markup and `textContent` for localized strings/status.

No `<all_urls>` permission is requested. The extension does not bypass browser-protected pages.

## Privacy

There is no telemetry, analytics, ad SDK, cloud upload, automatic screenshot transfer or background network client. Settings and active capture metadata stay in browser-local storage.

The version-controlled public browser-extension privacy policy is maintained at [`ekstenzije/PRIVACY.md`](../ekstenzije/PRIVACY.md).

## Localization

English is the default/fallback locale. Dedicated English and Croatian catalogs are included in every distributable browser folder.

## Reproducible packaging

`ekstenzije/tools/package-extensions.sh` creates the four store-ready ZIP files from staged source. It normalizes staged file and directory timestamps to the portable ZIP epoch, sorts archive paths and uses `zip -X` to remove nonessential host metadata. A `SHA256SUMS.txt` file is generated next to the packages.

CI executes the packaging helper twice in separate output directories. Every corresponding ZIP must be byte-identical and both SHA-256 manifests must match before the artifact can be uploaded.

## Cross-browser parity controls

`ekstenzije/tools/verify-extension-parity.mjs` prevents silent divergence between browser variants. It verifies that:

- all shared runtime files have identical paths and SHA-256 content across Chrome, Edge, Opera and Firefox;
- Chrome, Edge and Opera manifests remain identical;
- Firefox differs from the Chromium manifest only in the expected background/Gecko metadata;
- all four manifest versions remain aligned;
- branded PNG icons have the exact required dimensions and identical bytes across browsers;
- popup/options HTML contains no inline script/style blocks, inline event handlers or remote runtime resources.

These checks complement the existing permission, locale, source-policy and manifest validator.

## Store submission readiness

`ekstenzije/store/listing.json` is the canonical machine-readable store contract. It keeps the EN/HR listing copy, single-purpose statement, permission justifications, local-first privacy/data-practice declarations, exact ZIP names and store asset references aligned for Chrome Web Store, Microsoft Edge Add-ons, Opera Add-ons and Mozilla Add-ons.

`ekstenzije/store/reviewer-notes.md` gives external reviewers a deterministic functional test path and documents expected protected-page behavior, permission usage, network/privacy behavior, full-page limits and the Firefox source/signing boundary. `ekstenzije/store/README.md` describes the final manual submission steps and links to the official publisher documentation.

`ekstenzije/tools/generate-store-assets.py` deterministically generates three 1280×800 listing screenshots, two Opera 612×408 screenshots, a 440×280 small promotional tile and a 1400×560 large/marquee promotional tile from the SNAPVERE dark/violet UI model. These generated PNGs are CI artifacts rather than committed binaries.

`ekstenzije/tools/validate-store-readiness.mjs` validates the store material against the actual extension manifests. Among other checks it enforces version alignment, the exact four-permission allow-list, no broad host permissions, local-first privacy flags, EN/HR metadata completeness, exact package names, required asset paths and exact PNG dimensions.

Extension CI therefore produces two independently scoped artifacts: the four deterministic extension ZIPs plus their SHA-256 manifest, and a separate `snapvere-browser-store-kit-<sha>` bundle containing the privacy policy, canonical listing metadata, reviewer notes and generated store graphics.

Store publication itself remains an external boundary. Authenticated publisher access, store-side submission, certification/review and Firefox signing cannot be inferred from a green repository CI run and must not be claimed until the corresponding store completes them.

## QA boundary

`extensions-ci.yml` provides syntax, manifest, permission, locale, cross-browser parity, source-policy, store-readiness, generated-asset dimension, deterministic packaging, SHA-256 and package-content validation. A green CI run is not a claim that all four extensions were manually exercised in real browser GUIs or approved by external stores.

See [`ekstenzije/README.md`](../ekstenzije/README.md) for development loading, packaging, store-submission material and known limitations.
