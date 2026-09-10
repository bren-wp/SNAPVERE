# SNAPVERE Branding

## Brand purpose

SNAPVERE is a premium Windows capture utility built around speed, precision and local-first privacy. The identity should feel immediate, technical and calm: the product stays in the tray until the user needs it, then gets out of the way again.

## Brand line

**Capture. Edit. Done.**

The line mirrors the actual interaction model: capture from the tray/hotkey, make inline edits when needed, then Copy or Save.

## Symbol

The canonical symbol is `assets/branding/symbol/snapvere-symbol.svg`.

The current mark is a single violet capture shard / feather. It represents speed and a precise directional stroke rather than a camera glyph. The silhouette is intentionally strong enough for notification-area sizes where a full wordmark is impossible.

Do not revive the former capture-corners + S symbol in new product surfaces.

## Logo variants

- `assets/branding/readme/snapvere-logo-dark.svg`
- `assets/branding/readme/snapvere-logo-light.svg`

Both use the canonical shard geometry and the same violet family. Small contexts use the standalone symbol.

## Palette

| Token | Value | Use |
| --- | --- | --- |
| Night 950 | `#0D1220` | tray flyout / dark product surfaces |
| Night 900 | `#101622` | editor palettes and cards |
| Violet 650 | `#6547F6` | primary brand stroke |
| Violet 550 | `#8C5CFF` | primary interaction accent |
| Violet 400 | `#C18BFF` | highlight / wordmark detail |
| Lavender 100 | `#F5EFFF` | high-contrast mark detail |
| Neutral 50 | `#F7F7FB` | primary text on dark surfaces |
| Neutral 400 | `#A8B0C1` | secondary dark-theme text |
| Ink 900 | `#171A24` | primary light-theme text |

Violet indicates the capture action, active tool or brand. It should not become an uncontrolled full-screen glow.

## Typography

Use Windows-native Segoe UI / system typography. Do not bundle a custom font solely for branding.

- product name: Semibold/Bold with moderate tracking;
- flyout action: 12–15 px, Semibold for primary capture;
- body: 12–16 px, Regular;
- metadata / hotkeys: 9–12 px, Regular/Medium.

## Tray-first application direction

Normal startup does not open the Capture Center. The notification-area icon is the primary persistent product surface.

- **Left click**: Region Capture immediately.
- **Right click**: branded SNAPVERE flyout.
- **Print Screen**: Region Capture when available.
- Options/Recent and About are secondary windows opened only on request.

The tray icon should remain readable at 16–32 px and must use the same violet shard direction as the canonical symbol.

## Branded tray flyout

The flyout uses a compact dark navy surface, one dominant violet Region action, restrained blue/violet secondary iconography and clear shortcut hints. It must feel like a lightweight Windows command surface, not a dashboard.

Implemented menu groups:

1. Capture region / window / screen
2. Open capture folder / Options & recent captures / About
3. Exit

Unavailable features must not be inserted merely to fill the menu.

## Capture overlay

Region Capture uses graphite dimming, a high-contrast violet selection border, compact tool/action palettes and physical-pixel dimensions. Annotation colors may vary for utility, but the selection/focus treatment remains SNAPVERE violet.

## Documentation visuals

Repository-maintained UI illustrations are under `docs/images/`:

- `tray-first-region.svg`
- `tray-menu.svg`
- `region-editor.svg`

They document the implemented interaction model and may be used by the README. They are not substitutes for platform-specific runtime QA screenshots because Windows chrome, font rendering, DPI and system-tray layout vary by environment.

## Accessibility

- brand colors must not be the only state indicator;
- keyboard capture shortcuts remain available independently from pointer/tray actions;
- text contrast must remain readable on dark surfaces;
- high-contrast/system accessibility behavior takes precedence over brand decoration.

## Misuse

Do not use stock camera icons as the product mark, rotate the shard arbitrarily, recolor each segment randomly, stretch it non-proportionally, add heavy RGB glow, or mix the old and new symbol families in the same release.

## Asset production status

Implemented:

- canonical shard symbol SVG;
- README dark/light wordmarks;
- branded runtime notification-area icon;
- branded WinUI tray flyout mark;
- README tray-first and Region-editor SVG illustrations.

Future binary icon exports (ICO/PNG sizes) should be derived from the canonical shape and validated at native Windows icon sizes before replacing executable resources.
