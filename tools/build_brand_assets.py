#!/usr/bin/env python3
"""Rebuild SNAPVERE's real launcher assets from one versioned SVG logo.

Dependencies: pip install cairosvg pillow
Exports the same symbol to the website, Windows native icon, PNG sizes,
macOS icon and social preview so no obsolete product logo survives.
"""
from io import BytesIO
from pathlib import Path

import cairosvg
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "site/favicon.svg"
ICONS = ROOT / "apps/desktop/src-tauri/icons"
ICONS.mkdir(parents=True, exist_ok=True)

raw = cairosvg.svg2png(bytestring=SOURCE.read_bytes(), output_width=1024, output_height=1024)
base = Image.open(BytesIO(raw)).convert("RGBA")


def rgba_icon(size: int) -> Image.Image:
    return base.resize((size, size), Image.Resampling.LANCZOS)


rgba_icon(512).save(ROOT / "site/icon.png", optimize=True)
rgba_icon(32).save(ICONS / "32x32.png", optimize=True)
rgba_icon(128).save(ICONS / "128x128.png", optimize=True)
rgba_icon(256).save(ICONS / "128x128@2x.png", optimize=True)
base.save(ICONS / "icon.ico", format="ICO",
          sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64),
                 (128, 128), (256, 256)])
base.save(ICONS / "icon.icns", format="ICNS")

# This is marketing artwork built from the actual brand SVG, never a fake
# screenshot of the product or its recording engine.
og = Image.new("RGB", (1200, 630), (15, 19, 43))
draw = ImageDraw.Draw(og)
for y in range(630):
    progress = y / 629
    draw.line((0, y, 1200, y),
              fill=(int(15 + 10 * progress), int(19 + 6 * progress), int(43 + 30 * progress)))
og.paste(rgba_icon(240), (104, 175), rgba_icon(240))
font_candidates = [
    ROOT / "site/fonts/inter-latin.woff2",
    Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"),
]
font = None
for candidate in font_candidates[1:]:
    if candidate.exists():
        font = ImageFont.truetype(str(candidate), size=76)
        break
if font is None:
    font = ImageFont.load_default()
draw.text((390, 196), "SNAPVERE", fill="#f7f6ff", font=font, stroke_width=0)
try:
    small = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", size=28)
except OSError:
    small = ImageFont.load_default()
draw.text((392, 310), "Capture. Edit. Done.", fill="#c4b5fd", font=small)
draw.text((392, 375), "A Windows capture workspace by BRENDIGO", fill="#b9c4df", font=small)
og.save(ROOT / "site/og.png", optimize=True)

for filename in ("site/icon.png", "site/og.png"):
    assert (ROOT / filename).stat().st_size > 1000, filename
for size, filename in ((32, "32x32.png"), (128, "128x128.png"), (256, "128x128@2x.png")):
    with Image.open(ICONS / filename) as image:
        assert image.size == (size, size), filename
print("SNAPVERE: SVG, native Windows/macOS icons, web icons and OG artwork synchronized")
