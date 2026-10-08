# SNAPVERE Branding

**SNAPVERE** is the product name and visible wordmark. **Brendigo** is the developer and publisher identity.

## Brand assets

Repository assets:

- `assets/branding/readme/snapvere-logo-dark.svg` — README/logo treatment for dark backgrounds.
- `assets/branding/readme/snapvere-logo-light.svg` — README/logo treatment for light backgrounds.
- `assets/branding/symbol/snapvere-symbol.svg` — standalone product symbol.
- `ekstenzije/*/icons/` — browser extension application icons.
- `assets/branding/premium/SNAPVERE.ico` — canonical multiresolution Windows executable icon, containing original 16, 32, 48, and 128 px PNG frames without resampling.
- `src/Snapvere.App/Assets/SNAPVERE.ico` and `src/Snapvere.Setup/Assets/SNAPVERE.ico` — identical `ApplicationIcon` copies embedded into the main/Portable and Setup executables.
- The tray uses the supplied 32 px PNG, and the installer UI embeds the supplied 128 px PNG directly (no procedural approximation).
- `eng/validate-ui-contract.py` checks icon headers, the original PNG bytes of every frame, identical executable copies, and Windows `ApplicationIcon` wiring.


The root README uses adaptive dark/light logo markup so GitHub can present the appropriate asset without changing the brand.

## Product identity rules

Production Windows and browser surfaces must not expose controls that rename the product, replace the visible wordmark or change the saved capture prefix to another brand. Browser downloads use the SNAPVERE product prefix and browser CI verifies the brand lock across Chrome, Edge, Opera and Firefox.

Do not describe external store approval, signing or publication unless that external state can actually be verified. Product marketing may describe implemented capture behavior and CI evidence, but it must not turn automated test coverage into a guarantee that a platform can never fail.

## Voice

SNAPVERE copy should be short, technical and useful. Preferred positioning is local-first capture, focused workflow, transparent quality evidence and minimal friction. Avoid inflated claims such as “unbreakable”, “zero bugs” or “works on every device”.

## Publisher

Official site: https://snapvere.com  
Support: info@snapvere.com  
Publisher: https://brendigo.com  
Current release: https://github.com/bren-wp/SNAPVERE/releases/latest
