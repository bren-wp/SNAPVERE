#!/usr/bin/env python3
"""Read-only checks for Snapvere public branding and Windows installer terms."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "site"
APP = ROOT / "apps/desktop"
TAURI = APP / "src-tauri"

config = json.loads((TAURI / "tauri.conf.json").read_text(encoding="utf-8"))
license_file = config.get("bundle", {}).get("licenseFile")
assert isinstance(license_file, str) and license_file.endswith(".rtf"), (
    "Both NSIS and MSI must include an RTF commercial license"
)
eula = (TAURI / license_file).resolve()
assert eula.is_relative_to(TAURI.resolve()) and eula.is_file()
rtf = eula.read_text(encoding="ascii")
assert rtf.startswith("{\\rtf1") and rtf.rstrip().endswith("}")
source = (ROOT / "legal/SNAPVERE-COMMERCIAL-EULA.txt").read_text(encoding="utf-8")
assert "MIT" in source and "commercial" in source.lower()
for clause in ("COMMERCIAL END-USER LICENSE AGREEMENT", "MIT", "commercial", "Brendigo"):
    assert clause.lower() in rtf.lower(), f"Missing EULA clause: {clause}"

page = (SITE / "license/index.html").read_text(encoding="utf-8")
for clause in ("Commercial license agreement", "Permitted use",
               "Existing MIT and third-party licenses", "Warranties and liability"):
    assert clause.lower() in page.lower(), f"Missing published license clause: {clause}"
assert "/license/" in (SITE / "sitemap.xml").read_text(encoding="utf-8")
for name in ("index.html", "download/index.html", "privacy/index.html"):
    assert 'href="/license/"' in (SITE / name).read_text(encoding="utf-8"), name
assert "https://snapvere.com/license/" in (APP / "src/windows/Settings.tsx").read_text(encoding="utf-8")

for base, suffixes in ((SITE, {".html", ".js", ".css"}), (APP / "src", {".html", ".ts", ".tsx", ".css"})):
    for file in base.rglob("*"):
        if file.suffix not in suffixes:
            continue
        content = file.read_text(encoding="utf-8").lower()
        for domain in ("github.com", "github.io", "githubusercontent.com"):
            assert domain not in content, (
                f"GitHub references must not appear in the public UI: {file.relative_to(ROOT)}"
            )
print("Public Snapvere: license, installer EULA, links and brand check OK")
