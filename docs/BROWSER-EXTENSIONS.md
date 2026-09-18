# SNAPVERE Browser Extensions

Supported variants: Chrome, Edge, Opera and Firefox.

## Capture modes

- **Visible area** — captures the current viewport.
- **Select region** — lets the user drag over the required area.
- **Full page** — scrolls and stitches a bounded page capture locally.

## Brand contract

The extension name, visible wordmark and saved capture prefix are fixed to **SNAPVERE**. Settings do not expose a rebrand, custom product name or custom filename prefix.

## Permission contract

The permission set is exactly:

```text
activeTab
scripting
downloads
downloads.open
storage
```

`downloads.open` is used only from the user-triggered **Open** control for a completed item in Recent captures. There is no broad host permission. Internal/protected browser pages can reject capture; SNAPVERE reports that failure instead of attempting to bypass browser policy.

## Stability and memory

Full-page capture uses explicit limits for tile count, canvas dimensions and total pixels. Tiles are decoded, drawn into one bounded destination canvas and released immediately instead of being retained as a second full image set. Capture-session tokens, tab ownership checks and cleanup watchdogs prevent stale work from silently completing against the wrong tab.

Region-capture failures that occur after the popup closes are shown as a transient localized in-page SNAPVERE status.

## Keyboard shortcuts

Default browser capture shortcuts are `Ctrl+Shift+1` (Region), `Ctrl+Shift+2` (Visible area) and `Ctrl+Shift+3` (Full page). On macOS the defaults are `Command+Shift+1`, `Command+Shift+2` and `Command+Shift+7`; Full page intentionally avoids the system-reserved `Shift+Command+3` screenshot shortcut. Users can remap extension commands through their browser's extension-shortcut UI.
