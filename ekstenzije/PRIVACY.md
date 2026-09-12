# SNAPVERE Browser Extension Privacy Policy

_Last updated: 2026-09-12_

This policy applies to the SNAPVERE browser extensions for Google Chrome, Microsoft Edge, Opera and Mozilla Firefox. Browser-extension development is post-v0.1.0 source development and is separate from the historical SNAPVERE v0.1.0 desktop/Android release assets.

## Summary

SNAPVERE is local-first screenshot software. The browser extension does not collect, sell, rent or transmit user data to SNAPVERE, Brendigo, analytics providers, advertising networks or cloud services. It has no telemetry or analytics client and does not require an account.

## What the extension processes locally

When the user explicitly starts a capture, SNAPVERE processes only the information needed to complete that requested screenshot:

- pixels from the active browser tab returned by the browser screenshot API;
- page dimensions, viewport dimensions and scroll position for bounded full-page capture;
- the rectangle chosen by the user for region capture;
- temporary image tiles used to assemble a full-page PNG;
- the filename prefix and save preference selected by the user;
- short-lived capture-session metadata such as a random capture token, tab/window identifiers and start time.

Screenshot pixels and full-page tiles are processed in browser memory. Completed images are saved through a user-initiated local download. SNAPVERE does not automatically upload screenshots anywhere.

Settings remain in browser-local storage until the user changes them, clears extension data or removes the extension. Active capture-session metadata is temporary, expires after a bounded period and is cleaned when the operation completes or fails.

## Permissions

SNAPVERE requests only these browser permissions:

- `activeTab` — access the tab on which the user explicitly invokes SNAPVERE for the requested capture;
- `scripting` — inject the local capture helper into the active page for region and full-page capture;
- `downloads` — save user-requested PNG captures locally;
- `storage` — persist local settings and short-lived capture-session state.

The extension does not request `<all_urls>` and does not declare broad `host_permissions`.

## Data collection and transmission

SNAPVERE does not transmit screenshot pixels, browsing history, page contents, identifiers, settings or usage events to a remote server. There is no background network client, remote-code loader, advertising SDK, analytics SDK or telemetry endpoint in the extension.

The extension does not sell user data and does not use user data for advertising, profiling, creditworthiness, lending or unrelated purposes.

## Third-party websites and protected pages

SNAPVERE can only operate where the browser allows extension capture or script injection. Browser-internal pages and other protected surfaces may reject capture; SNAPVERE reports a controlled error instead of attempting to bypass browser restrictions.

A webpage being captured may itself contain third-party content, but SNAPVERE does not send that content to those third parties or to SNAPVERE services.

## Security

The extension is self-contained and does not execute remotely hosted code. Capture message types are allow-listed, capture tokens and sender tab/window identity are validated where applicable, and full-page capture has explicit tile/canvas/pixel limits to avoid uncontrolled memory allocation.

## User control

Capture starts only after an explicit user action. Users can change local settings from the extension options page, clear extension storage through browser controls, remove downloaded PNG files at any time, or uninstall the extension to remove its local extension data.

## Changes to this policy

Material changes to browser-extension data handling must be reflected in this policy and in the corresponding browser-store privacy disclosures before publication of the affected extension update.

## Support

For browser-extension support or privacy questions, use the SNAPVERE repository issue tracker:

https://github.com/bren-wp/SNAPVERE/issues
