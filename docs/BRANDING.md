# SNAPVERE Branding

## Brand purpose

SNAPVERE is a premium Windows capture utility built around speed, precision and local-first privacy. The identity should feel immediate, technical and calm: the product stays in the tray until the user needs it, then gets out of the way again.

## Brand line

**Capture. Edit. Done.**

This line mirrors the actual interaction model: start a capture from the tray/hotkey, annotate in place when needed, then Copy or Save.

## Canonical symbol

The canonical vector symbol is:

```text
assets/branding/symbol/snapvere-symbol.svg
```

The current mark is the violet SNAPVERE capture shard/feather. It represents speed and a precise directional stroke rather than a generic camera glyph. The silhouette is intentionally simple enough to remain recognizable in notification-area sizes.

Do not reintroduce the former capture-corners + S family in new product surfaces.

## Logo variants

- `assets/branding/readme/snapvere-logo-dark.svg`
- `assets/branding/readme/snapvere-logo-light.svg`

Both use the canonical shard geometry and violet identity. Small contexts use the standalone symbol.

## Palette

| Token | Value | Use |
| --- | --- | --- |
| Night 950 | `#0D1220` | tray flyout / darkest product surfaces |
| Night 900 | `#101622` | editor palettes and cards |
| Violet 650 | `#6547F6` | primary brand stroke |
| Violet 550 | `#8C5CFF` | primary interaction accent |
| Violet 400 | `#C18BFF` | highlight / wordmark detail |
| Lavender 100 | `#F5EFFF` | high-contrast mark detail |
| Neutral 50 | `#F7F7FB` | primary text on dark surfaces |
| Neutral 400 | `#A8B0C1` | secondary dark-theme text |
| Ink 900 | `#171A24` | primary light-theme text |

Violet identifies primary capture/focus/active states. It should not turn every product surface into a heavy full-screen glow.

## Typography and iconography

Use Windows-native Segoe UI/system typography. Do not bundle a custom font solely for branding.

Use clean local Windows/Fluent-style iconography for product actions. Do not use emoji as icons. Product mark, tray icon, executable/installer icon and documentation assets should converge on the same SNAPVERE identity.

## Tray-first product hierarchy

The notification-area icon is the primary persistent SNAPVERE surface.

- **Left click:** Region Capture immediately.
- **Right click:** branded compact command flyout.
- **Print Screen:** Region Capture when available.
- **Options / Preferences:** secondary real settings window.
- **Recent captures:** secondary local history view.
- **About:** secondary factual product window.

Normal startup must not show a large capture dashboard or flash the hidden coordinator.

## Branded tray flyout

The implemented flyout uses a compact graphite/navy surface, rounded treatment, violet/indigo accenting, the SNAPVERE brand mark and shortcut hints. It is a quick-action surface rather than a dashboard.

Implemented menu groups:

1. Capture Region / Capture Window / Capture Screen
2. Open Capture Folder / Recent captures / Options / Preferences / About SNAPVERE
3. Exit SNAPVERE

Unavailable features must not be inserted merely to make the menu appear fuller.

## Options surface

Options follows the same graphite/navy/violet language but remains restrained. Only implemented settings are visible:

- Start SNAPVERE with Windows;
- Include cursor on capture.

The same window can show Recent Captures as a separate section. Capture launch buttons do not belong in this secondary settings surface.

## Capture overlay

Region Capture uses frozen-screen dimming, a high-contrast SNAPVERE selection treatment, compact tool/action palettes and physical-pixel dimensions. Annotation colors may vary for utility while selection/focus styling remains tied to the core violet identity.

The current editor intentionally favors stable programmatic WinUI surfaces over decorative templated controls that have caused runtime instability in previous QA.

## Tray icon requirements

The runtime tray icon must remain readable on light and dark taskbars and in the hidden-icons panel. The smallest practical notification-area sizes must prioritize silhouette/contrast over internal detail.

Canonical vector branding is the source of truth. Binary icon exports should be validated visually at native Windows sizes before replacing working executable/tray resources.

## Documentation visuals

Repository-maintained workflow illustrations live under `docs/images/`:

- `tray-first-region.svg`
- `tray-menu.svg`
- `region-editor.svg`

They document implemented product flow but are **not** represented as pixel-identical runtime screenshots. Real product screenshots may be added only when they can be reproducibly captured from the actual implemented UI and kept synchronized with the release.

## Accessibility

- brand color must not be the only state indicator;
- keyboard capture shortcuts remain independent from tray/pointer input;
- readable contrast takes priority over subtle decoration;
- tooltip/focus behavior should accompany compact icon actions where implemented;
- high-contrast/system accessibility behavior takes precedence over visual branding.

## Misuse

Do not use stock camera imagery as the product mark, rotate the shard arbitrarily, stretch it non-proportionally, mix old/new symbol families, add random multicolor segments, use emoji for product commands, or present concept art as a real application screenshot.

## Asset production status

Implemented:

- canonical shard symbol SVG;
- README dark/light wordmarks;
- branded runtime notification-area icon;
- branded WinUI tray flyout mark;
- README tray-first/Region workflow SVG illustrations.

Future work:

- reproducible real product screenshots after an actual Windows UI capture pipeline is trustworthy;
- audited ICO/PNG export matrix derived from the canonical symbol where binary resource replacement is justified.
