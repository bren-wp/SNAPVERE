#!/usr/bin/env python3
"""Guard Tailwind 4 CSS-first tokens and its locked install contract."""
from pathlib import Path
import json
import re

root = Path(__file__).resolve().parents[1]
app = root / "apps/desktop"
css = (app / "src/styles.css").read_text(encoding="utf-8")
postcss = (app / "postcss.config.js").read_text(encoding="utf-8")
pkg = json.loads((app / "package.json").read_text(encoding="utf-8"))
lock = json.loads((app / "package-lock.json").read_text(encoding="utf-8"))
assert (app / "tailwind.config.ts").exists() is False, "Legacy Tailwind config should be removed"
assert css.startswith('@import "tailwindcss" source(none);')
assert '@source "../index.html";' in css and '@source "./";' in css
assert '@custom-variant dark ' in css
assert not re.search(r"@tailwind\s+(?:base|components|utilities)\s*;", css)
assert '@theme inline {' in css
for token in ("bg", "surface", "elevated", "border", "muted", "text",
              "accent", "accent-soft", "danger", "warn"):
    assert f"--color-{token}: rgb(var(--snapvere-{token}));" in css, token
assert '"@tailwindcss/postcss": {}' in postcss
assert not re.search(r'(?m)^\s*tailwindcss\s*:', postcss), "Legacy PostCSS plugin"
assert not re.search(r'(?m)^\s*autoprefixer\s*:', postcss), "Unused PostCSS plugin"
dev = pkg["devDependencies"]
assert dev.get("tailwindcss", "").startswith("^4."), "Tailwind 4 is required"
assert dev.get("@tailwindcss/postcss", "").startswith("^4."), "Tailwind 4 PostCSS plugin is required"
assert "autoprefixer" not in dev, "Remove unused autoprefixer from manifest"
for name in ("tailwindcss", "@tailwindcss/postcss"):
    locked = lock["packages"][f"node_modules/{name}"]["version"]
    assert locked.startswith("4."), f"Invalid locked {name} version: {locked}"
assert "autoprefixer" not in lock["packages"], "Unused autoprefixer remains locked"
print("Tailwind 4 CSS-first migration, theme tokens, PostCSS plugin and lockfile OK")
