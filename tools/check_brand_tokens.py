#!/usr/bin/env python3
"""Prevent branding, color and wordmark drift across Snapvere surfaces."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SITE = (ROOT / "site/styles.css").read_text(encoding="utf-8")
APP = (ROOT / "apps/desktop/src/styles.css").read_text(encoding="utf-8")
TOKENS = ("bg", "surface", "elevated", "accent", "accent-hi", "text",
          "muted", "border", "card-radius", "control-radius")

def value(css: str, name: str) -> str:
    match = re.search(r"^\s*--brand-" + re.escape(name) + r":\s*([^;]+);", css, re.M)
    if not match:
        raise AssertionError(f"Missing shared brand token: {name}")
    return re.sub(r"\s+", " ", match.group(1).strip())

for token in TOKENS:
    assert value(SITE, token) == value(APP, token), (
        f"Brand design mismatch for --brand-{token}: "
        f"web={value(SITE, token)!r}, app={value(APP, token)!r}")

for relative in ("index.html", "download/index.html", "privacy/index.html"):
    html = (ROOT / "site" / relative).read_text(encoding="utf-8")
    assert re.search(
        r'<a class="brand"[^>]*>[\s\S]*?SNAPVERE\s*</a>', html
    ), f"Missing identical visible SNAPVERE wordmark in {relative}"

frame = (ROOT / "apps/desktop/src/components/WindowFrame.tsx").read_text(encoding="utf-8")
mark = (ROOT / "apps/desktop/src/components/BrandMark.tsx").read_text(encoding="utf-8")
logo = (ROOT / "site/favicon.svg").read_text(encoding="utf-8")
assert "SNAPVERE" in frame and "Capture. Edit. Done." in frame
assert 'viewBox="0 0 128 128"' in mark and 'viewBox="0 0 128 128"' in logo
assert "snapvere-app-brandbar" in frame
print("Snapvere: all 10 design tokens, wordmarks and logo geometry match.")
