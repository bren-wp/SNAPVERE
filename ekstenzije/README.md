# SNAPVERE Browser Extensions

Production extension packages are maintained for Google Chrome, Microsoft Edge, Opera and Mozilla Firefox.

## Capture features

- visible-area capture;
- rectangular region capture;
- bounded full-page capture;
- local PNG download;
- English and Croatian UI.

## Locked SNAPVERE identity

The extension name, visible wordmark and capture filename prefix are fixed to **SNAPVERE**. The options page does not expose product renaming, custom branding or a custom filename prefix. Legacy stored prefix values are ignored by the runtime.

Saved captures use the form:

```text
SNAPVERE-<capture-type>-<timestamp>.png
```

## Permissions

The exact permission set is:

```text
activeTab
scripting
downloads
downloads.open
storage
```

`downloads.open` is limited to the user-triggered **Open** action for a completed SNAPVERE item in Recent captures. There is no `<all_urls>` permission and no broad `host_permissions` entry. Protected browser pages can reject capture; SNAPVERE reports a controlled error instead of attempting to bypass browser policy.

## Privacy

Screenshot pixels are processed locally in the browser. The extension contains no first-party telemetry, analytics, advertising SDK, cloud-upload client, remote-control channel, remote runtime code or background network client. No SNAPVERE account is required for capture.

## Stability and memory

Capture sessions are serialized and protected by bounded session locks. Active-tab ownership is checked around screenshot acquisition so a tab switch cannot silently save a frame from another tab.

Full-page capture has explicit limits for tile count, canvas dimensions and total pixels. Each decoded tile is drawn into one bounded destination canvas and released immediately, avoiding retention of a second full set of tile bitmaps. A watchdog restores modified page state if orchestration disappears.

Region capture keeps the page-side selection flow alive after the popup closes and shows a transient localized SNAPVERE error when the background crop/download fails.

## Validation

Browser CI verifies:

- Manifest V3 shape and browser-specific background model;
- exact permissions and absence of broad host access;
- locked SNAPVERE name/wordmark/filename prefix;
- EN/HR locale parity;
- source parity across variants;
- runtime capture-lock and active-tab behavior;
- bounded full-page memory lifecycle;
- region error feedback;
- icon/store metadata integrity;
- deterministic ZIP packaging and SHA-256 output.

Current GitHub release packages:

```text
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

External browser-store publication remains a separate authenticated review/signing process and is not claimed by the repository unless a real listing exists.

See [Browser Extension Guide](../docs/BROWSER-EXTENSIONS.md), [Privacy](PRIVACY.md) and [Troubleshooting](../docs/TROUBLESHOOTING.md).
