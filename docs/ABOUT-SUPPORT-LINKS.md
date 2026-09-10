# About support and legal links

The SNAPVERE **About** window exposes the product's official support and legal destinations without adding any network work to normal application startup or tray idle operation.

## Destinations

- Product website: `https://snapvere.com`
- Support email: `info@snapvere.com` via `mailto:`
- Privacy: `https://snapvere.com/privacy`
- Terms: `https://snapvere.com/terms`
- Developer: `https://brendigo.com`

These are user-initiated links only. SNAPVERE does not contact any of these endpoints in the background and does not send screenshot content, settings, telemetry, or identifiers when the About window is opened.

If Windows has no registered handler for `mailto:`, the support action falls back to copying `info@snapvere.com` to the Windows clipboard and changes the visible button label to confirm the copied address. This fallback remains fully local.

The `/privacy` and `/terms` URLs are canonical product paths and should remain stable so installed application builds do not need to be changed when the website content is updated.
