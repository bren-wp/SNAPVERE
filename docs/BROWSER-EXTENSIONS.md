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

## Localization

English is the default/fallback locale. Dedicated English and Croatian catalogs are included in every distributable browser folder.

## QA boundary

`extensions-ci.yml` provides syntax, manifest, permission, locale, source-policy and package validation. A green CI run is not a claim that all four extensions were manually exercised in real browser GUIs.

See [`ekstenzije/README.md`](../ekstenzije/README.md) for development loading, packaging and known limitations.
