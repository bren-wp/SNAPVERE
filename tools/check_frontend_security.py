#!/usr/bin/env python3
"""Fail closed on DOM injection patterns in application and first-party website JS.

Cheap source guard, not a replacement for full dependency review or a penetration test.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "site"
APP = ROOT / "apps/desktop/src"
PATTERNS = {
    "JS dynamic code execution": re.compile(r"\b(?:eval|Function)\s*\("),
    "unsafe HTML sink": re.compile(r"\.\s*(?:innerHTML|outerHTML)\s*=|\.insertAdjacentHTML\s*\("),
    "React HTML injection": re.compile(r"\bdangerouslySetInnerHTML\b"),
    "document.write": re.compile(r"\bdocument\.write\s*\("),
}
count = 0
for base, extensions in ((SITE, {".js"}), (APP, {".ts", ".tsx"})):
    for path in sorted(base.rglob("*")):
        if path.suffix not in extensions:
            continue
        count += 1
        text = path.read_text(encoding="utf-8")
        for line_no, line in enumerate(text.splitlines(), 1):
            # Ignore whole-line comments describing a forbidden API.
            if line.lstrip().startswith(("//", "*", "/*")):
                continue
            for name, pattern in PATTERNS.items():
                if pattern.search(line):
                    raise AssertionError(
                        f"{name}: {path.relative_to(ROOT)}:{line_no}: {line.strip()}"
                    )
assert count >= 25, f"Source scan coverage dropped unexpectedly ({count} files)"
print(f"Browser security scan: {count} modules, 0 unsafe HTML or code execution sinks")
