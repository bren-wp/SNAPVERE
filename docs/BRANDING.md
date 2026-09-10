# SNAPVERE Branding

## Product identity

SNAPVERE is a commercial Windows capture utility built around speed, precision and local-first privacy.

- Product: **SNAPVERE**
- Brand line: **Capture. Edit. Done.**
- Developer / publisher: **Brendigo**
- Official product website: **https://snapvere.com**
- Developer website: **https://brendigo.com**

The product should feel immediate, technical and calm: it remains in the notification area until needed, presents compact task-focused surfaces, then gets out of the way again.

## Canonical symbol

Canonical vector symbol:

```text
assets/branding/symbol/snapvere-symbol.svg
```

The current mark is the SNAPVERE violet/cyan capture shard. It is intentionally simple enough to remain recognizable at notification-area sizes. Do not reintroduce older capture-corner/S logo families into new surfaces.

README wordmarks:

- `assets/branding/readme/snapvere-logo-dark.svg`
- `assets/branding/readme/snapvere-logo-light.svg`

## Product palette

| Token | Value | Use |
| --- | --- | --- |
| Graphite 950 | `#0D1321` | tray / dark product canvas |
| Graphite 900 | `#101622` | editor palettes / cards |
| Outline | `#52617F` | strong flyout outline |
| Violet 650 | `#6547D8` | primary control fill |
| Violet 550 | `#8667F4` | active border / focus |
| Violet 400 | `#A77CFF` | text/accent highlight |
| Cyan 500 | `#37B6D4` | secondary brand accent |
| Strong text | `#F7F5FF` | primary dark-theme text |
| Muted text | `#AFB6C8` | secondary text |

Violet identifies primary capture/focus/active states. Cyan is a restrained secondary accent rather than a competing primary color.

## Typography and iconography

Use Windows-native Segoe UI/system typography. Do not bundle a custom font only for branding.

Use Segoe Fluent / Windows-native icons for product actions where appropriate. Do not use emoji as action icons. Tray icon, executable/installer icon, README wordmark and in-app brand mark should use one recognizable SNAPVERE identity.

## Surface hierarchy

### Tray

Tray is the primary persistent surface.

- left-click → Region Capture;
- right-click → compact command flyout;
- Print Screen → Region Capture where available;
- Language / Options / About are explicit secondary surfaces.

The flyout reference is `docs/images/tray-menu.svg`, with a 418×540 graphite/violet visual contract.

### Region editor

The Region editor reference is `docs/images/region-editor.svg`: violet 3 px selection frame, eight handles, physical dimensions badge, vertical tool rail and separate Copy / Save / Close bar. The application should follow this layout rather than the old horizontal toolbar design.

### Window picker

Window picker uses the same graphite/violet/cyan hierarchy, high-contrast target outline and concise focus/title feedback. It must remain capture-oriented rather than becoming another settings/dashboard surface.

### Options and Language

Options and Language use the same cards, outlines, radii and accent hierarchy as Tray/Region. Language is a real persisted setting, not a mock control. English is the canonical default; Croatian and more than 20 additional built-in languages are selectable.

### About

About must identify SNAPVERE as commercial software, Brendigo as developer/publisher, `snapvere.com` as the product site and `brendigo.com` as the developer site. New v0.0.7 surfaces must not show legacy MPL product-license copy.

### Setup

Setup uses the same dark brand system and canonical mark. Default Start menu, Desktop icon and Start-with-Windows options are visible user choices; Desktop and startup are checked by default but remain opt-out.

## Documentation images

Repository UI references live under `docs/images/`:

- `tray-first-region.svg`
- `tray-menu.svg`
- `region-editor.svg`

These are design/reference assets and must not be mislabeled as real Windows screenshots. A real product screenshot may be committed only when produced from an actual executing SNAPVERE build through a reproducible capture path.

The reason for this rule is product integrity: README imagery must never promise a UI the executable does not render.

## Accessibility

- color is not the only state indicator;
- keyboard capture shortcuts remain independent of pointer/tray input;
- readable contrast wins over subtle decoration;
- compact icon actions need automation names/tooltips where applicable;
- Windows high-contrast/accessibility behavior takes precedence over decoration.

## Commercial identity

SNAPVERE 0.0.7 and later are distributed under the SNAPVERE Commercial Software License Agreement in the repository root `LICENSE` file. Branding, artwork, logos and product identity remain proprietary to Brendigo. Historical releases retain the license terms that accompanied those releases.

## Misuse

Do not mix old and current symbol families, distort the canonical mark, introduce random multicolor segments, use stock camera art as the main product identity, use emoji for product commands, or present concept/reference artwork as a real application screenshot.
