#!/usr/bin/env python3
"""Capture REAL React UI renders for README (with a stubbed IPC preview bridge)."""
from pathlib import Path
from base64 import b64encode
import json
from shutil import copyfile
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "docs" / "screenshots"
OUTPUT.mkdir(parents=True, exist_ok=True)

TAURI_PREVIEW = r"""
(() => {
  let nextCallback = 1;
  const callbacks = new Map();
  const eventCallbacks = new Map();
  async function previewRgbaPacket() {
    const img = new Image();
    img.src = window.__SNAPVERE_PREVIEW_FRAME_URL;
    await img.decode();
    const canvas = document.createElement("canvas");
    canvas.width = img.naturalWidth;
    canvas.height = img.naturalHeight;
    const ctx = canvas.getContext("2d", { willReadFrequently: true });
    ctx.drawImage(img, 0, 0);
    const rgba = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
    const packet = new Uint8Array(8 + rgba.length);
    const hdr = new DataView(packet.buffer);
    hdr.setUint32(0, canvas.width, true);
    hdr.setUint32(4, canvas.height, true);
    packet.set(rgba, 8);
    return packet.buffer;
  }
  window.__TAURI_INTERNALS__ = {
    metadata: { currentWindow: { label: "home" }, windows: [{ label: "home" }] },
    convertFileSrc(path) { return "http://127.0.0.1:4173/screenshots/home.png"; },
    transformCallback(cb) { const id = nextCallback++; callbacks.set(id, cb); return id; },
    unregisterCallback(id) { callbacks.delete(id); },
    previewListenerReady() { return eventCallbacks.has("snapvere:capture"); },
    previewEmitCapture(payload) {
      const callback = callbacks.get(eventCallbacks.get("snapvere:capture"));
      if (!callback) throw new Error("Native screenshot event listener not registered");
      callback({ event: "snapvere:capture", id: 1, payload });
    },
    async invoke(command, args) {
      if (command === "plugin:event|listen") {
        eventCallbacks.set(args.event, args.handler);
        return 1;
      }
      if (command === "get_frozen_rgba") return previewRgbaPacket();
      if (command === "get_frozen_frame") return window.__SNAPVERE_PREVIEW_FRAME_URL;
      if (command === "plugin:window|scale_factor") return 1;
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
        settings.get_by_role("navigation").get_by_role("button", name=tab + " settings", exact=True).click()
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
    # Guard the new responsive split hero: marketing copy stays left of the
    # genuine product image at desktop widths, and both stay within the viewport.
    hero_title = page.locator(".hero__title").bounding_box()
    hero_preview = page.locator(".snapvere-hero-product").bounding_box()
    if not hero_title or not hero_preview or hero_title["x"] >= hero_preview["x"]:
        raise RuntimeError("SNAPVERE desktop hero no longer uses the intended split layout")
    if hero_preview["x"] + hero_preview["width"] > 1442:
        raise RuntimeError("SNAPVERE desktop hero image overflows the viewport")
    for address, filename in (("/download/", "download.png"), ("/privacy/", "privacy.png")):
        subpage = site.new_page()
        subpage.goto("http://127.0.0.1:4173" + address, wait_until="networkidle")
        subpage.screenshot(path=str(OUTPUT / filename))
    site.close()
    mobile_site = browser.new_context(viewport={"width": 390, "height": 844}, device_scale_factor=1)
    mobile_page = mobile_site.new_page()
    mobile_page.goto("http://127.0.0.1:4173/", wait_until="networkidle")
    mobile_page.screenshot(path=str(OUTPUT / "website-mobile.png"), full_page=False)
    # No horizontal overflow on narrow phones.
    horizontal_overflow = mobile_page.evaluate(
        "() => document.documentElement.scrollWidth > window.innerWidth + 2"
    )
    if horizontal_overflow:
        raise RuntimeError("SNAPVERE mobile homepage has horizontal overflow")
    for address, filename in (("/download/", "download-mobile.png"),
                              ("/privacy/", "privacy-mobile.png")):
        subpage = mobile_site.new_page()
        subpage.goto("http://127.0.0.1:4173" + address, wait_until="networkidle")
        subpage.screenshot(path=str(OUTPUT / filename))
    mobile_site.close()

    # Exercise the ACTUAL React screenshot overlay using a real website render
    # transported through the same in-memory RGBA wire format as the Rust IPC.
    # This verifies a visible, non-black frozen frame without pretending that
    # the preview harness can exercise native Windows display capture.
    preview_frame = "data:image/png;base64," + b64encode(
        (OUTPUT / "website.png").read_bytes()
    ).decode("ascii")
    selection = browser.new_context(
        viewport={"width": 1440, "height": 900}, device_scale_factor=1,
        color_scheme="dark"
    )
    selection.add_init_script(TAURI_PREVIEW)
    selection.add_init_script("window.__SNAPVERE_PREVIEW_FRAME_URL = " + json.dumps(preview_frame))
    capture_page = selection.new_page()
    capture_page.goto("http://127.0.0.1:1420/#overlay", wait_until="networkidle")
    capture_page.wait_for_function(
        "() => window.__TAURI_INTERNALS__?.previewListenerReady()",
        timeout=20000
    )
    capture_page.evaluate("""() => window.__TAURI_INTERNALS__.previewEmitCapture({
        token: Date.now(), id: 0, mode: "shot",
        x: 0, y: 0, width: 1440, height: 900
    })""")
    capture_page.wait_for_function(
        "() => document.querySelector('canvas')?.width === 1440",
        timeout=20000
    )
    capture_page.wait_for_timeout(400)
    lit_pixels = capture_page.evaluate("""() => {
      const c = document.querySelector('canvas');
      const ctx = c.getContext('2d');
      let visible = 0;
      for (let y = 100; y < c.height; y += 125) {
        for (let x = 100; x < c.width; x += 125) {
          const p = ctx.getImageData(x, y, 1, 1).data;
          if (p[3] > 100 && (p[0] + p[1] + p[2]) > 95) visible++;
        }
      }
      return visible;
    }""")
    if lit_pixels < 8:
        raise RuntimeError("Capture overlay rendered black or empty pixels instead of the desktop")
    capture_page.screenshot(path=str(OUTPUT / "capture-overlay.png"))
    selection.close()
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
