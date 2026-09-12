# SNAPVERE Browser Extensions

Production browser-extension source and release packages for Google Chrome, Microsoft Edge, Opera and Mozilla Firefox.

Browser extensions were developed after v0.1.0 and become official public GitHub Release assets starting with **SNAPVERE v0.1.1**. The historical v0.1.0 release remains immutable with exactly its original four Windows/Android assets.

## Version

Current release: **0.1.1**.

The four distributable manifests and `store/listing.json` must carry the same release version. CI rejects browser-version drift.

## Supported browsers

- Google Chrome — Manifest V3, service-worker background;
- Microsoft Edge — Manifest V3, service-worker background;
- Opera — Manifest V3, service-worker background;
- Mozilla Firefox — Manifest V3 WebExtension with Firefox-compatible `background.scripts`.

Each browser directory is self-contained and can be loaded directly without a bundling or transpilation step.

## Features

- capture the visible viewport;
- bounded full-page capture using controlled scrolling and local canvas stitching;
- rectangular region selection and capture;
- local PNG output;
- English and Croatian UI;
- SNAPVERE dark/violet branding;
- keyboard-accessible controls and visible focus states;
- durable capture-session state for MV3 background suspension/resume;
- bounded tile count, canvas dimensions and total pixel allocation;
- deterministic cleanup of overlays, fixed/sticky visibility changes and the original scroll position.

## Privacy model

SNAPVERE browser extensions are local-first. Screenshot pixels are processed inside the browser and are not uploaded by the extension.

The source contains no telemetry, analytics, advertising SDK, cloud-upload client, remote-control channel, remote runtime code or background network client. No account or payment is required.

See [`PRIVACY.md`](PRIVACY.md) for the browser-extension privacy policy.

## Permissions

The extensions request only:

- `activeTab` — access the tab on which the user explicitly invokes SNAPVERE;
- `scripting` — inject the local capture helper into that active page for region/full-page capture;
- `downloads` — save user-requested PNG captures;
- `storage` — keep local settings and short-lived capture-session metadata.

There is no `<all_urls>` permission and no broad `host_permissions` entry. Protected browser pages may reject injection/capture; SNAPVERE surfaces that as a controlled error instead of attempting to bypass browser restrictions.

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

Firefox temporary loading is for development. AMO distribution/signing is a separate external store process.

## v0.1.1 public browser packages

The v0.1.1 GitHub Release contains these four browser assets in addition to the Windows/Android release files:

```text
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

The release workflow builds these packages from the exact validated release commit and verifies their SHA-256 values before and after publication.

## Reproducible packaging

To create the deterministic browser packages manually from the repository root on a Unix-like system with `bash`, `zip` and `sha256sum`:

```bash
bash ekstenzije/tools/package-extensions.sh
```

The packaging helper stages source, normalizes file/directory timestamps to the portable ZIP epoch, sorts archive paths and uses `zip -X`. CI performs two independent packaging passes and requires every corresponding ZIP plus `SHA256SUMS.txt` to match byte-for-byte.

Development/cache artifacts such as `node_modules`, `__pycache__`, `.cache`, `.DS_Store` and source maps are forbidden from release packages.

## Validation

`.github/workflows/extensions-ci.yml` and the v0.1.1 release workflow validate:

- Manifest V3 shape and Firefox background compatibility;
- the exact permission allow-list and absence of broad host permissions;
- EN/HR locale parity and valid JSON;
- referenced runtime files and icon dimensions;
- JavaScript syntax and unsafe/remote-code policy;
- byte-identical shared runtime source across all four browser variants;
- identical Chrome/Edge/Opera manifests and normalized Firefox semantics;
- exact 16/32/48/128 PNG icon dimensions and cross-browser icon parity;
- no inline scripts/styles/event handlers or remote HTML runtime resources;
- store metadata/privacy declarations against the actual manifests;
- deterministic store listing/promo graphics;
- reproducible ZIP creation, package cleanliness and SHA-256 integrity.

Static/package validation is not the same as exhaustive manual GUI testing on every browser build or web application.

## Store submission kit

`ekstenzije/store/` contains canonical EN/HR listing copy, permission justifications, privacy/data-practice declarations and reviewer notes. Store screenshots/promo graphics are generated deterministically in CI.

The repository prepares store-submission material, but actual publication to Chrome Web Store, Microsoft Edge Add-ons, Opera Add-ons or Mozilla Add-ons requires authenticated publisher accounts plus external review/certification/signing. A GitHub v0.1.1 release does not imply store approval.

## Known browser limitations

Full-page capture uses controlled scrolling and viewport stitching. Results can vary on highly dynamic pages, video, canvas/WebGL surfaces, lazy-loading content, cross-origin iframes, sticky/fixed-heavy interfaces or application-specific virtual scrolling. Browser-protected pages can block screenshot/script APIs.

SNAPVERE restores the original scroll position and temporarily hidden floating elements in success/error paths and also uses a content-side watchdog. These safeguards reduce stale-page-state risk but cannot make every web application perfectly capturable.
