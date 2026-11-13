#!/usr/bin/env python3
"""Capture REAL React UI renders for README (with a stubbed IPC preview bridge)."""
from pathlib import Path
from base64 import b64encode
from shutil import copyfile
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "docs" / "screenshots"
OUTPUT.mkdir(parents=True, exist_ok=True)

TAURI_PREVIEW = r"""
(() => {
  let nextCallback = 1;
  const callbacks = new Map();
  window.__TAURI_INTERNALS__ = {
    metadata: { currentWindow: { label: "home" }, windows: [{ label: "home" }] },
    convertFileSrc(path) { return "http://127.0.0.1:4173/screenshots/home.png"; },
    transformCallback(cb) { const id = nextCallback++; callbacks.set(id, cb); return id; },
    unregisterCallback(id) { callbacks.delete(id); },
    async invoke(command, args) {
      if (command === "list_screenshots" || command === "search_screenshots" || command === "list_monitors") return [];
      if (command === "active_hotkeys") return ["PrintScreen", "Ctrl+Shift+R"];
      if (command === "plugin:autostart|is_enabled") return false;
      if (command.startsWith("plugin:event|")) return 1;
      if (command.startsWith("plugin:store|")) {
        if (command === "plugin:store|load" || command === "plugin:store|new") return 1;
        return null;
      }
      if (command === "recording_status") return { state: "idle" };
      if (command === "plugin:window|is_visible") return true;
      return null;
    }
  };
})();
"""

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True, args=["--disable-web-security"])
    app = browser.new_context(viewport={"width": 1120, "height": 740},
                              device_scale_factor=1, color_scheme="dark")
    app.add_init_script(TAURI_PREVIEW)
    page = app.new_page()
    page.goto("http://127.0.0.1:1420/", wait_until="networkidle")
    page.get_by_role("banner").get_by_role("button", name="Capture region").wait_for(timeout=30000)
    page.wait_for_timeout(1200)
    page.screenshot(path=str(OUTPUT / "home.png"), full_page=False)
    settings = app.new_page()
    settings.goto("http://127.0.0.1:1420/#settings", wait_until="networkidle")
    settings.get_by_role("navigation").get_by_role("button", name="General").wait_for(timeout=30000)
    settings.wait_for_timeout(1500)
    settings.screenshot(path=str(OUTPUT / "settings.png"), full_page=False)
    for tab in ("Hotkeys", "Capture", "Recording", "Save", "Privacy", "About"):
        settings.get_by_role("navigation").get_by_role("button", name=tab, exact=True).click()
        settings.wait_for_timeout(200)
        settings.screenshot(path=str(OUTPUT / ("settings-" + tab.lower() + ".png")))

    # The editor shows an existing, real React UI screenshot as its test image.
    # The preview transport is mocked: this is a UI render, not a native capture test.
    editor = app.new_page()
    editor_path = b64encode(b"Snapvere-frontend-preview.png").decode("ascii")
    editor.goto("http://127.0.0.1:1420/#editor?p=" + editor_path, wait_until="networkidle")
    editor.wait_for_function(
        "() => document.querySelector('canvas')?.width > 1", timeout=20000
    )
    editor.screenshot(path=str(OUTPUT / "editor.png"))
    app.close()
    compact = browser.new_context(viewport={"width": 920, "height": 650},
                                  device_scale_factor=1, color_scheme="dark")
    compact.add_init_script(TAURI_PREVIEW)
    compact_page = compact.new_page()
    compact_page.goto("http://127.0.0.1:1420/", wait_until="networkidle")
    compact_page.get_by_role("banner").get_by_role("button", name="Capture region").wait_for(timeout=30000)
    compact_page.screenshot(path=str(OUTPUT / "home-compact.png"))
    compact.close()
    site = browser.new_context(viewport={"width": 1440, "height": 900}, device_scale_factor=1)
    page = site.new_page()
    page.goto("http://127.0.0.1:4173/", wait_until="networkidle")
    page.screenshot(path=str(OUTPUT / "website.png"), full_page=False)
    for address, filename in (("/download/", "download.png"), ("/privacy/", "privacy.png")):
        subpage = site.new_page()
        subpage.goto("http://127.0.0.1:4173" + address, wait_until="networkidle")
        subpage.screenshot(path=str(OUTPUT / filename))
    site.close()
    mobile_site = browser.new_context(viewport={"width": 390, "height": 844}, device_scale_factor=1)
    mobile_page = mobile_site.new_page()
    mobile_page.goto("http://127.0.0.1:4173/", wait_until="networkidle")
    mobile_page.screenshot(path=str(OUTPUT / "website-mobile.png"), full_page=False)
    for address, filename in (("/download/", "download-mobile.png"),
                              ("/privacy/", "privacy-mobile.png")):
        subpage = mobile_site.new_page()
        subpage.goto("http://127.0.0.1:4173" + address, wait_until="networkidle")
        subpage.screenshot(path=str(OUTPUT / filename))
    mobile_site.close()
    browser.close()

for path in OUTPUT.glob("*.png"):
    if path.stat().st_size < 5000:
        raise RuntimeError(f"Screenshot output seems invalid: {path}")
    print(f"Screenshot: {path.relative_to(ROOT)} ({path.stat().st_size} bytes)")

# Website preview cards must always show the same current React UI captures.
SITE_PREVIEWS = ROOT / "site" / "screenshots"
SITE_PREVIEWS.mkdir(parents=True, exist_ok=True)
for name in ("home.png", "settings.png", "editor.png"):
    copyfile(OUTPUT / name, SITE_PREVIEWS / name)
    print(f"Synced website preview: site/screenshots/{name}")
