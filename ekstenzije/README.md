# SNAPVERE Browser Extensions

Production-oriented browser extension source for Google Chrome, Microsoft Edge, Opera and Mozilla Firefox.

These extensions are **post-v0.1.0 source development**. They are not part of the immutable SNAPVERE v0.1.0 public release contract, which remains exactly four assets: `SNAPVERE-Setup.exe`, `SNAPVERE-Portable.exe`, `SNAPVERE.apk` and `SNAPVERE-Android-Source.zip`.

## Supported browsers

- Google Chrome — Manifest V3, service worker background
- Microsoft Edge — Manifest V3, service worker background
- Opera — Manifest V3, service worker background
- Mozilla Firefox — WebExtension Manifest V3 with Firefox-compatible `background.scripts`

Each browser folder is self-contained and can be loaded directly without a build step.

## Features

- capture the visible viewport;
- capture a full page using bounded viewport tiling and local canvas stitching;
- select and capture a rectangular region;
- local PNG output;
- English and Croatian UI;
- SNAPVERE dark/violet branding;
- accessible focus states and keyboard operation;
- persistent capture-session state so region selection survives MV3 background suspension/resume;
- bounded full-page tile count, canvas dimensions and total pixel allocation;
- deterministic cleanup of selection overlays, sticky/fixed visibility changes and original scroll position.

## Privacy model

SNAPVERE browser extensions are local-first. Screenshot pixels are processed in the browser and are not uploaded by the extension.

The extension source contains no telemetry, analytics, advertising SDK, cloud upload client, remote-control channel or background network client. It does not automatically send screenshots anywhere.

No remote runtime dependencies or CDN scripts are used. The public browser-extension privacy policy is maintained in [`PRIVACY.md`](PRIVACY.md).

## Permissions

The extensions request only:

- `activeTab` — access the page that the user explicitly invokes SNAPVERE on;
- `scripting` — inject the local capture helper only into the active page;
- `downloads` — save visible-area and region PNG captures;
- `storage` — local settings and durable capture-session state.

There is no `<all_urls>` host permission and no broad `host_permissions` entry.

Browser-internal pages and other protected surfaces may reject injection or capture. SNAPVERE treats those cases as controlled errors rather than attempting to bypass browser restrictions.

## Development loading

### Chrome

1. Open `chrome://extensions`.
2. Enable **Developer mode**.
3. Choose **Load unpacked**.
4. Select `ekstenzije/chrome`.

### Microsoft Edge

1. Open `edge://extensions`.
2. Enable **Developer mode**.
3. Choose **Load unpacked**.
4. Select `ekstenzije/edge`.

### Opera

1. Open `opera://extensions`.
2. Enable developer mode.
3. Choose **Load unpacked**.
4. Select `ekstenzije/opera`.

### Firefox

1. Open `about:debugging`.
2. Choose **This Firefox**.
3. Choose **Load Temporary Add-on**.
4. Select `ekstenzije/firefox/manifest.json`.

Firefox temporary add-ons are removed when Firefox restarts. Store signing/distribution is a separate process.

## Packaging

The extension CI creates exactly these browser ZIP packages:

```text
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

It also produces `SHA256SUMS.txt` inside the CI artifact so the four package digests can be checked independently.

To create the same deterministic packages manually from the repository root on a Unix-like system with `bash`, `zip` and `sha256sum`:

```bash
bash ekstenzije/tools/package-extensions.sh
```

The packaging helper stages the browser source, normalizes ZIP timestamps to the portable ZIP epoch, sorts all input paths and uses `zip -X`. CI runs the packaging process twice and requires every ZIP plus the checksum manifest to match byte-for-byte between the two builds.

Do not include `node_modules`, caches, source maps, `.DS_Store` or other development artifacts.

## Store submission kit

[`store/listing.json`](store/listing.json) is the canonical machine-readable store contract. It contains the EN/HR listing copy, the single-purpose statement, permission justifications, privacy/data-practice declarations, exact ZIP names and the required store-asset paths for all four browser variants.

[`store/reviewer-notes.md`](store/reviewer-notes.md) contains the functional reviewer test path, expected protected-page behavior, privacy/network notes, permission rationale and the Firefox signing/source-code note. [`store/README.md`](store/README.md) documents the manual submission boundary and links to the official publisher documentation for Chrome Web Store, Microsoft Edge Add-ons, Opera Add-ons and Mozilla Add-ons.

`tools/generate-store-assets.py` deterministically creates the submission graphics used by the store kit:

- three 1280×800 listing screenshots;
- two Opera 612×408 screenshots;
- one 440×280 small promotional tile;
- one 1400×560 large/marquee promotional tile.

Generated PNGs live under `ekstenzije/store/assets/` during CI and are intentionally ignored by Git. The source generator is version-controlled so the visuals remain reproducible from the repository.

`tools/validate-store-readiness.mjs` binds the store claims back to the implementation. It validates listing schema/version, manifest-version alignment, the exact permission set, the absence of broad host permissions, local-first privacy flags, EN/HR listing completeness, exact package names, prepared screenshot/promo paths and exact PNG dimensions.

CI uploads a separate `snapvere-browser-store-kit-<sha>` artifact containing the privacy policy, canonical listing JSON, reviewer notes and generated graphics. This keeps the store-submission material separate from the four distributable extension ZIPs.

## Validation

`.github/workflows/extensions-ci.yml` validates:

- all four browser variants;
- Manifest V3 shape and Firefox background compatibility;
- strict permission allow-list;
- absence of broad host permissions;
- EN/HR locale parity and valid JSON;
- referenced files/icons;
- JavaScript and store-generator syntax;
- absence of remote scripts, insecure `http://`, `eval`, `new Function` and common telemetry endpoints;
- byte-identical shared runtime source across Chrome, Edge, Opera and Firefox, except for the expected manifest differences;
- identical Chrome/Edge/Opera manifests and normalized Firefox manifest semantics;
- exact 16/32/48/128 PNG icon dimensions and cross-browser icon parity;
- absence of inline scripts, inline style blocks, inline event handlers and remote HTML runtime resources;
- store listing/privacy/reviewer metadata against the actual manifests and declared behavior;
- exact generated store-asset dimensions;
- reproducible browser ZIP creation across two clean packaging passes;
- SHA-256 verification and package cleanliness.

The workflow performs automated static/package/store-readiness validation. It is **not** equivalent to manual GUI/runtime testing in four real browsers or approval by an external browser store.

## Known browser limitations

Full-page capture uses controlled scrolling plus viewport stitching. Results can vary on pages with rapidly changing content, animated/sticky/fixed interfaces, video, canvas/WebGL surfaces, lazy-loading, cross-origin iframes or application-specific virtual scrolling. Browser-protected pages can prohibit script injection or screenshot APIs.

SNAPVERE restores the original scroll position and hidden floating elements in success/error paths and also uses a content-side watchdog. This reduces stale-page state risk but does not make every possible web application perfectly capturable.

## Store submission

The source, deterministic ZIPs, privacy policy, listing metadata, reviewer notes and generated store graphics are prepared for submission. This repository does not claim or automate publication to Chrome Web Store, Mozilla Add-ons, Microsoft Edge Add-ons or Opera Add-ons because those channels require external authenticated publisher accounts plus their own submission, review/certification and, for normal Firefox distribution, Mozilla signing.

Do not claim a store item ID, signature, approval, certification or public listing URL until the corresponding external store has actually issued it.
