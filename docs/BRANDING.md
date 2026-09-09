# SNAPVERE Branding

## Brand purpose

SNAPVERE is a premium Windows capture product built around speed, precision and local-first privacy. The identity should feel technical and calm rather than playful, gaming-oriented or generic SaaS.

## Brand line

**Capture anything.**

This is the current primary tagline. It is short enough for the application shell, README and installer surfaces while leaving feature-specific messaging to supporting copy.

## Symbol

The canonical symbol is `assets/branding/symbol/snapvere-symbol.svg`. It combines four precise selection corners with a continuous S-shaped capture stroke. The mark is designed to remain identifiable without the wordmark and to translate later into application, tray and installer icon families.

## Logo variants

- `assets/branding/readme/snapvere-logo-dark.svg`
- `assets/branding/readme/snapvere-logo-light.svg`

Both are derived from the same symbol geometry. Raster and ICO exports must be generated from canonical vector sources rather than independently redrawn.

## Palette

### Core

| Token | Value | Use |
| --- | --- | --- |
| Graphite 950 | `#101116` | dark symbol/application surfaces |
| Ink 900 | `#17181D` | primary text on light surfaces |
| Neutral 50 | `#F7F7FB` | primary text/mark on dark surfaces |
| Indigo 500 | `#7667F5` | primary capture/focus accent |
| Indigo 600 | `#5C45CF` | light-theme accent |
| Indigo 400 | `#9C91FF` | small emphasis/details |
| Neutral 400 | `#A8ABB5` | secondary dark-theme text |
| Neutral 600 | `#5E626C` | secondary light-theme text |

Accent color is intentionally constrained to capture affordances, focus, selection and key actions. Full-window gradients are not part of the core identity.

## Typography

Use Windows-native Segoe UI / system typography. Do not bundle a custom web font solely for branding.

Recommended application scale:

- Display: 32–40 px, Semibold
- Heading: 20–24 px, Semibold
- Body: 14–16 px, Regular
- Caption: 12–13 px, Regular/Medium
- Technical metadata: system monospace where code-like alignment is useful

## Clear space

Keep clear space around the standalone symbol of at least 16% of the symbol width. Do not let adjacent text, window chrome or installer decoration intersect the capture-corner geometry.

## Minimum size

The full wordmark should not be used when the rendered height makes the tagline unreadable. Use the standalone symbol for tray/small icon contexts.

## Misuse

Do not rotate the mark, recolor individual corners randomly, add glow/RGB effects, stretch non-proportionally, use stock camera glyphs as substitutes, or redraw the S independently between assets.

## Windows application direction

Dark theme is the strongest brand showcase, but System theme is the intended default. Surfaces should remain Windows-native and Fluent-aligned. Mica/backdrop effects are optional accents, not the identity itself.

## Capture overlay

Overlay treatment should use graphite dimming, high-contrast neutral selection borders and controlled indigo focus/handle accents. Selection geometry must remain readable over arbitrary screen content.

## Accessibility

Brand colors are not allowed to override system high-contrast behavior. Focus indication must remain visible without relying on color alone.

## Asset production status

Implemented: canonical symbol and README dark/light logos.

Planned from the canonical source: application ICO/PNG set, tray icon, installer icon, monochrome variants, social preview, splash/about artwork and installer artwork. These should be added only as real derived assets rather than placeholder graphics.
