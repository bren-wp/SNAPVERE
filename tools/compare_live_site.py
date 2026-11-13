#!/usr/bin/env python3
"""Measure actual pixel deviation between public snapvere.com and repo website.

Report differences instead of claiming 1:1 parity. Neither page is modified.
"""
import json
from datetime import datetime, timezone
from io import BytesIO
from pathlib import Path
from PIL import Image, ImageChops, ImageStat
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "visual-audit"
OUT.mkdir(parents=True, exist_ok=True)
CASES = [
    ("/", 1440, 900, "home-desktop"),
    ("/", 390, 844, "home-mobile"),
    ("/download/", 1440, 900, "download-desktop"),
    ("/download/", 390, 844, "download-mobile"),
    ("/privacy/", 1440, 900, "privacy-desktop"),
    ("/privacy/", 390, 844, "privacy-mobile"),
]


def capture(browser, base, route, width, height):
    context = browser.new_context(viewport={"width": width, "height": height},
                                  device_scale_factor=1, reduced_motion="reduce")
    try:
        page = context.new_page()
        page.goto(base + route.lstrip("/"), wait_until="domcontentloaded", timeout=20000)
        page.evaluate("() => document.fonts.ready")
        page.wait_for_timeout(900)
        return Image.open(BytesIO(page.screenshot(full_page=False))).convert("RGB")
    finally:
        context.close()


results = []
with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    try:
        for route, w, h, name in CASES:
            row = {"route": route, "viewport": f"{w}x{h}", "name": name}
            try:
                live = capture(browser, "https://snapvere.com/", route, w, h)
                proposed = capture(browser, "http://127.0.0.1:4173/", route, w, h)
                live.save(OUT / f"{name}-live.png")
                proposed.save(OUT / f"{name}-pr.png")
                if live.size != proposed.size:
                    row["error"] = "Image dimensions differ"
                else:
                    diff = ImageChops.difference(live, proposed)
                    diff.save(OUT / f"{name}-diff.png")
                    changed = sum(1 for rgb in diff.getdata() if max(rgb) > 0)
                    row["changed_pixels"] = changed
                    row["difference_percent"] = round(100 * changed / (w * h), 3)
                    row["mean_rgb_error"] = round(sum(ImageStat.Stat(diff).mean) / 3, 3)
            except Exception as error:
                row["error"] = str(error)
            print(json.dumps(row))
            results.append(row)
    finally:
        browser.close()

report = {
    "project": "SNAPVERE",
    "publisher": "BRENDIGO",
    "created_utc": datetime.now(timezone.utc).isoformat(),
    "method": "same-size viewport, 1x device scale, exact RGB pixel delta",
    "note": "The published website is a separate deployment. Results measure, not certify, 1:1 design.",
    "results": results,
}
(OUT / "report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
