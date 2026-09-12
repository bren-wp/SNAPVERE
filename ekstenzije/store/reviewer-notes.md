# SNAPVERE Browser Extension — Reviewer Notes

## Purpose

SNAPVERE has one purpose: capture a user-requested screenshot of the active browser tab as the visible viewport, a bounded full page, or a user-selected rectangular region, then save the result locally as PNG.

No account, login, payment, remote server or network connection is required for extension functionality.

## Suggested functional test

1. Install/load the submitted package.
2. Open a normal `https://` webpage with enough content to scroll.
3. Open the SNAPVERE toolbar popup.
4. Choose **Capture visible area** and confirm that a PNG download starts locally.
5. Choose **Capture full page** and confirm that SNAPVERE scrolls/stitches the page and starts a local PNG download. The page should return to its original scroll position.
6. Choose **Select region**, drag a rectangle on the page and confirm that only the selected area is downloaded as PNG. Pressing Escape instead should cancel cleanly.
7. Open **Settings**, change the filename prefix/save preference, save, reopen settings and confirm the local preference persists.
8. Optionally test on a browser-internal/protected page. A controlled unsupported-page error is expected; the extension does not attempt to bypass browser restrictions.

## Permissions

- `activeTab`: required to capture only the active tab after explicit user invocation.
- `scripting`: required to inject the local full-page/region helper into that active page.
- `downloads`: required to save requested PNG captures locally.
- `storage`: required for local preferences and short-lived capture-session state.

There are no `host_permissions` and no `<all_urls>` permission.

## Privacy and network behavior

Screenshot pixels are processed locally. There is no telemetry, analytics, ad SDK, cloud upload, remote-code loader or background network client. The package is self-contained.

## Full-page safety bounds

Full-page capture intentionally rejects pages that exceed configured tile count, canvas dimensions or total-pixel limits. Highly dynamic pages, video/canvas/WebGL content, virtual scrolling and cross-origin frames can produce browser-dependent results; this is documented behavior rather than hidden functionality.

## Firefox-specific note

The Firefox ZIP contains readable, unminified source directly. There is no bundling, transpilation, minification or generated runtime source, so a separate source-code package is not required by SNAPVERE's build process. Mozilla still performs its own validation/review and signing before installation in normal Release/Beta Firefox channels.
