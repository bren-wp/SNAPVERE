#!/usr/bin/env python3
"""Prevent regression to bundled SQLite vulnerable to CVE-2026-119375.

Lockfile must be produced by real cargo update (never hand-edit checksums).
RUSTSEC-2026-0459: libsqlite3-sys <0.39.0 bundles vulnerable SQLite versions.
"""
from pathlib import Path
import re
import tomllib

root = Path(__file__).resolve().parents[1]
cargo = root / "apps/desktop/src-tauri"
manifest = (cargo / "Cargo.toml").read_text(encoding="utf-8")
assert 'rusqlite = { version = "0.42", features = ["bundled"] }' in manifest, (
    "Bundled SQLite must use a patched rusqlite release"
)
lock = tomllib.loads((cargo / "Cargo.lock").read_text(encoding="utf-8"))
packages = lock.get("package", [])
sqlite = [item for item in packages if item["name"] == "libsqlite3-sys"]
assert len(sqlite) == 1, f"Expected one libsqlite3-sys dependency, got {len(sqlite)}"
version = sqlite[0]["version"]
m = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)", version)
assert m, f"Unexpected libsqlite3-sys version {version}"
v = tuple(int(p) for p in m.groups())
assert v >= (0, 39, 0), (
    f"VULNERABLE bundled SQLite: libsqlite3-sys {version}, needs >= 0.39.0 "
    "(RUSTSEC-2026-0459, CVE-2026-119375)"
)
print(f"SQLite supply-chain check passed: libsqlite3-sys {version} patched")
