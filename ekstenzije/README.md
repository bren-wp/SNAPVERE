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

No remote runtime dependencies or CDN scripts are used.

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

The extension CI creates:

```text
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

To create the same packages manually from the repository root on a system with `zip`:

```bash
mkdir -p artifacts/extensions
(cd ekstenzije/chrome  && zip -r ../../artifacts/extensions/SNAPVERE-Chrome.zip .)
(cd ekstenzije/edge    && zip -r ../../artifacts/extensions/SNAPVERE-Edge.zip .)
(cd ekstenzije/opera   && zip -r ../../artifacts/extensions/SNAPVERE-Opera.zip .)
(cd ekstenzije/firefox && zip -r ../../artifacts/extensions/SNAPVERE-Firefox.zip .)
```

Do not include `node_modules`, caches, source maps, `.DS_Store` or other development artifacts.

## Validation

`.github/workflows/extensions-ci.yml` validates:

- all four browser variants;
- Manifest V3 shape and Firefox background compatibility;
- strict permission allow-list;
- absence of broad host permissions;
- EN/HR locale parity and valid JSON;
- referenced files/icons;
- JavaScript syntax;
- absence of remote scripts, insecure `http://`, `eval`, `new Function` and common telemetry endpoints;
- distributable ZIP creation and package cleanliness.

The workflow performs static/package validation. It is **not** equivalent to manual GUI/runtime testing in four real browsers.

## Known browser limitations

Full-page capture uses controlled scrolling plus viewport stitching. Results can vary on pages with rapidly changing content, animated/sticky/fixed interfaces, video, canvas/WebGL surfaces, lazy-loading, cross-origin iframes or application-specific virtual scrolling. Browser-protected pages can prohibit script injection or screenshot APIs.

SNAPVERE restores the original scroll position and hidden floating elements in success/error paths and also uses a content-side watchdog. This reduces stale-page state risk but does not make every possible web application perfectly capturable.

## Store submission

The source and CI ZIPs are prepared for manual submission. This repository does not automatically publish to Chrome Web Store, Mozilla Add-ons, Microsoft Edge Add-ons or Opera Add-ons because those channels require external publisher accounts, review and credentials.
