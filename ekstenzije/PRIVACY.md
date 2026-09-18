# SNAPVERE Browser Extension Privacy Policy

This policy applies to the SNAPVERE extensions for Chrome, Edge, Opera and Firefox.

## Summary

SNAPVERE is local-first screenshot software. The extension does not collect, sell, rent or transmit screenshot data to SNAPVERE, Brendigo, analytics providers, advertising networks or cloud services. It has no telemetry or analytics client and does not require an account.

## Local processing

When the user explicitly starts a capture, SNAPVERE processes only the data needed for that request:

- pixels returned by the browser screenshot API for the active tab;
- page/viewport dimensions and scroll position for bounded full-page capture;
- the rectangle chosen for region capture;
- short-lived decoded image data while a tile/region is being rendered;
- the local save preference;
- short-lived capture-session metadata such as a random token, tab/window identifiers and start time.

Decoded full-page tiles are drawn into one bounded local canvas and released after drawing. Completed PNG files are saved through the browser download flow. SNAPVERE does not automatically upload screenshots.

The capture filename prefix is fixed to **SNAPVERE**; it is not a user profile or customizable brand field.

## Permissions

The extension requests only `activeTab`, `scripting`, `downloads`, `downloads.open` and `storage`. `downloads.open` is used only when the user explicitly chooses **Open** for a completed SNAPVERE item in Recent captures. It does not request `<all_urls>` or broad host permissions.

## Data transmission

There is no background network client, remote-code loader, advertising SDK, analytics SDK or telemetry endpoint in the extension. SNAPVERE does not transmit browsing history, page contents, screenshot pixels, settings or usage events to a SNAPVERE server.

## Security and user control

Capture starts after explicit user action. Capture messages are allow-listed; session tokens and sender tab/window identity are validated. Full-page allocation is bounded. Users can change the local save preference, delete downloaded files or remove the extension through normal browser controls.

Protected browser pages can refuse capture or script injection; SNAPVERE does not attempt to bypass those restrictions.

Support: **info@snapvere.com**  
Official site: https://snapvere.com
