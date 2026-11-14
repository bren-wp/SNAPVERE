#!/usr/bin/env python3
"""Enforce the explicit per-window Tauri plugin permission boundary."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "apps/desktop/src-tauri/capabilities"

def read(name):
    data = json.loads((root / name).read_text(encoding="utf-8"))
    assert len(data["permissions"]) == len(set(data["permissions"])), name
    return data

shared = read("default.json")
home = read("home-privileged.json")
settings = read("settings-privileged.json")

expected = {"home", "overlay", "editor", "settings", "recbar", "recdraw", "recframe", "overlay-*"}
assert set(shared["windows"]) == expected, "Shared window set changed; review new WebView security"
assert home["windows"] == ["home"], "Home capability leaked into another window"
assert settings["windows"] == ["settings"], "Settings capability leaked into another window"

elevated = {
    "core:webview:allow-create-webview-window",
    "dialog:default",
    "dialog:allow-open",
    "dialog:allow-save",
    "dialog:allow-ask",
    "opener:default",
    "autostart:default",
    "global-shortcut:default",
    "clipboard-manager:default",
}
assert not (set(shared["permissions"]) & elevated), "Privileged plugin grant found on all WebViews"
assert set(home["permissions"]) == {
    "core:webview:allow-create-webview-window",
    "dialog:allow-ask",
}, "Home permissions expanded without review"
assert set(settings["permissions"]) == {
    "autostart:default", "opener:default", "dialog:allow-open"
}, "Settings permissions expanded without review"

for name in ("home-privileged.json", "settings-privileged.json"):
    assert "*" not in "".join(read(name)["windows"]), f"Wildcard privileged capability: {name}"

print("Tauri capability boundary OK: shared UI, Home-only creation/confirmation, Settings-only folder/startup/opener")
