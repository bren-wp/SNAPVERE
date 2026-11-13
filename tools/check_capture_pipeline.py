#!/usr/bin/env python3
"""No regressions to the screenshot overlay or product identity in tracked sources."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OVERLAY = (ROOT / "apps/desktop/src/windows/Overlay.tsx").read_text(encoding="utf-8")
BACKEND = (ROOT / "apps/desktop/src-tauri/src/commands.rs").read_text(encoding="utf-8")
IPC = (ROOT / "apps/desktop/src/lib/ipc.ts").read_text(encoding="utf-8")

assert "getFrozenRgba" in OVERLAY, "Overlay must request real in-memory framebuffer"
assert "getFrozenFrame" in OVERLAY, "The memory PNG fallback must stay available"
assert "putImageData" in OVERLAY and "new ImageData" in OVERLAY
assert "await ipc.cancelCapture()" in OVERLAY, "Failed frame must cancel capture"
assert "Unable to display the desktop capture" in OVERLAY
assert "convertFileSrc" not in OVERLAY, "Do not load the frozen desktop through asset://"
assert 'dir.join("frozen.png")' not in BACKEND and "fs::write(&path, &png)" not in BACKEND, \
    "Frozen screenshot must not be persisted to disk"
assert "get_frozen_rgba" in BACKEND and "get_frozen_frame" in BACKEND
assert "invoke<ArrayBuffer>" in IPC and "get_frozen_rgba" in IPC

# Build names at runtime so the verification script does not itself contain
# forbidden legacy product names in the tracked source.
legacy_terms = ("rv" + "geshot", "not" + "brec")
folders = ("apps", "site", "docs", "tools", ".github")
text_exts = (".rs", ".ts", ".tsx", ".js", ".css", ".json", ".md", ".html",
             ".yml", ".yaml", ".xml", ".toml", ".svg", ".py", ".txt")
checked = 0
for root in folders:
    for path in (ROOT / root).rglob("*"):
        if not path.is_file() or path.suffix.lower() not in text_exts:
            continue
        if any(x in path.parts for x in ("node_modules", "target", ".git", "dist")):
            continue
        name = path.relative_to(ROOT).as_posix().lower()
        data = path.read_text(encoding="utf-8", errors="replace").lower()
        for legacy in legacy_terms:
            assert legacy not in name, f"Legacy brand in file path: {name}"
            assert legacy not in data, f"Legacy brand in source: {name}"
        checked += 1

print(f"Snapvere screenshot transfer and brand audit passed for {checked} source files.")
