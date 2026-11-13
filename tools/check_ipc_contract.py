#!/usr/bin/env python3
"""Fail when UI exposes a command that the Rust invoke handler does not register."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "apps/desktop/src"
frontend = (SRC / "lib/ipc.ts").read_text(encoding="utf-8")
native = (ROOT / "apps/desktop/src-tauri/src/lib.rs").read_text(encoding="utf-8")
match = re.search(r"tauri::generate_handler!\[([\s\S]*?)\]", native)
assert match is not None, "Missing Tauri command registry"
handlers = set(re.findall(r"(?:commands|shortcuts)::([a-z][a-z0-9_]*)", match.group(1)))
calls = set(re.findall(r'invoke(?:<[^>]*>)?\(\s*"([^"]+)"', frontend))
assert len(calls) >= 30, f"Unexpectedly small IPC contract: {len(calls)}"
missing = sorted(calls - handlers)
assert not missing, f"Frontend calls unregistered Rust commands: {missing}"
for source in list(SRC.rglob("*.ts")) + list(SRC.rglob("*.tsx")):
    if source.name == "ipc.ts":
        continue
    text = source.read_text(encoding="utf-8")
    assert not re.search(r'\binvoke(?:<|\s*\()', text), (
        f"Direct Tauri IPC bypasses the typed wrapper: {source.relative_to(ROOT)}"
    )
print(f"IPC contract OK: {len(calls)} frontend commands and {len(handlers)} native handlers")
