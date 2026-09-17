#!/usr/bin/env python3
"""Materialize the audited v0.1.3 release workflow as a staging blob."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source_path = root / ".github" / "release-archive" / "release-0.1.2.yml"
target_path = root / ".github" / "release-staging" / "release-0.1.3.yml"
trigger_path = root / ".github" / "release-triggers" / "v0.1.3"

if target_path.exists():
    raise SystemExit("staged release-0.1.3.yml already exists; refusing to overwrite")

source = source_path.read_text(encoding="utf-8")
if "Release 0.1.2" not in source or "SNAPVERE_VERSION: 0.1.2" not in source:
    raise SystemExit("archived 0.1.2 release workflow does not match the expected source contract")

workflow = source.replace("0.1.2", "0.1.3")
required = [
    "name: Release 0.1.3",
    "SNAPVERE_VERSION: 0.1.3",
    ".github/release-triggers/v0.1.3",
    "gh release create $tag",
    "SNAPVERE-Setup.exe",
    "SNAPVERE-Portable.exe",
    "SNAPVERE-Chrome.zip",
    "SNAPVERE-Edge.zip",
    "SNAPVERE-Opera.zip",
    "SNAPVERE-Firefox.zip",
]
for marker in required:
    if marker not in workflow:
        raise SystemExit(f"generated release workflow missing required marker: {marker}")
if "0.1.2" in workflow:
    raise SystemExit("generated release workflow still contains the previous version")

target_path.parent.mkdir(parents=True, exist_ok=True)
target_path.write_text(workflow, encoding="utf-8", newline="\n")
trigger_path.write_text(
    "SNAPVERE v0.1.3 release publication trigger.\n"
    "Publish only after the release PR is merged to main.\n",
    encoding="utf-8",
    newline="\n",
)
print("Staged audited SNAPVERE v0.1.3 release workflow and main-only publication trigger.")
