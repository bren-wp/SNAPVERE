#!/usr/bin/env python3
"""Fail on unreferenced native Rust source modules and dead-code suppressions.

Clippy -D warnings checks unused Rust symbols; this independent check catches
orphaned source files that are never compiled or linked into either entrypoint.
"""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1] / "apps/desktop/src-tauri"
src = root / "src"
sources = {p.resolve() for p in src.rglob("*.rs")}
module_pattern = re.compile(
    r"(?m)^\s*(?:pub(?:\([^)]*\))?\s+)?mod\s+([A-Za-z_]\w*)\s*;"
)
checked = set()
pending = [src / "lib.rs", src / "main.rs"]
while pending:
    source = pending.pop().resolve()
    if source in checked:
        continue
    if source not in sources:
        raise AssertionError(f"Referenced Rust source does not exist: {source}")
    checked.add(source)
    code = source.read_text(encoding="utf-8")
    if re.search(r"#\s*\[\s*(?:allow|expect)\s*\(\s*dead_code\b", code):
        raise AssertionError(f"Dead-code suppression requires removal or justification: {source}")
    folder = source.parent if source.name in {"lib.rs", "main.rs", "mod.rs"} else source.with_suffix("")
    for module in module_pattern.findall(code):
        candidate = [folder / f"{module}.rs", folder / module / "mod.rs"]
        existing = [c.resolve() for c in candidate if c.is_file()]
        if len(existing) != 1:
            raise AssertionError(
                f"{source}: module {module} must resolve to exactly one file: {candidate}"
            )
        pending.extend(existing)

unreferenced = sorted(sources - checked)
if unreferenced:
    raise AssertionError(
        "Orphan Rust modules (not referenced by lib.rs/main.rs):\n" +
        "\n".join(str(p.relative_to(src)) for p in unreferenced)
    )
assert len(sources) >= 20, f"Suspiciously incomplete Rust source tree: {len(sources)}"
print(f"Rust module reachability OK: {len(checked)} of {len(sources)} Rust files reachable")
