#!/usr/bin/env python3
"""Validate SNAPVERE's active Windows/browser product contract."""

from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
CONTRACT_PATH = ROOT / "product-version.json"


def fail(message: str) -> None:
    raise RuntimeError(f"Product contract validation failed: {message}")


def read_text(path: Path) -> str:
    if not path.is_file():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def read_json(path: Path):
    try:
        return json.loads(read_text(path))
    except json.JSONDecodeError as exc:
        fail(f"invalid JSON in {path.relative_to(ROOT)}: {exc}")


def xml_property(path: Path, name: str) -> str:
    try:
        root = ET.fromstring(read_text(path))
    except ET.ParseError as exc:
        fail(f"invalid XML in {path.relative_to(ROOT)}: {exc}")
    for element in root.iter(name):
        if element.text:
            return element.text.strip()
    fail(f"missing <{name}> in {path.relative_to(ROOT)}")


def validate_links(paths: list[Path]) -> None:
    pattern = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")
    for path in paths:
        text = read_text(path)
        for raw_target in pattern.findall(text):
            target = raw_target.strip()
            if not target or target.startswith(("http://", "https://", "mailto:", "#")):
                continue
            target = target.split(" ", 1)[0].strip("<>")
            target = target.split("#", 1)[0].split("?", 1)[0]
            if not target:
                continue
            resolved = (path.parent / unquote(target)).resolve()
            try:
                resolved.relative_to(ROOT)
            except ValueError:
                fail(f"Markdown link escapes repository root in {path.relative_to(ROOT)}: {raw_target}")
            if not resolved.exists():
                fail(f"broken relative Markdown link in {path.relative_to(ROOT)}: {raw_target}")


def main() -> int:
    contract = read_json(CONTRACT_PATH)
    if contract.get("schemaVersion") != 1 or contract.get("product") != "SNAPVERE":
        fail("product-version.json schema/product mismatch")

    version = contract.get("currentRelease")
    if not isinstance(version, str) or not re.fullmatch(r"\d+\.\d+\.\d+", version):
        fail("currentRelease must use x.y.z")
    tag = f"v{version}"
    release_url = f"https://github.com/bren-wp/SNAPVERE/releases/tag/{tag}"
    if contract.get("releaseTag") != tag or contract.get("releaseUrl") != release_url:
        fail("release tag/URL mismatch")
    if contract.get("supportedPlatforms") != ["windows", "browsers"]:
        fail("supportedPlatforms must be exactly windows and browsers")
    if "android" in contract:
        fail("retired mobile product data must not exist in the active contract")

    forbidden_paths = [
        ROOT / "android",
        ROOT / ".github" / "workflows" / "android-ci.yml",
        ROOT / "docs" / "ANDROID.md",
        ROOT / "docs" / "hr" / "ANDROID.md",
    ]
    for path in forbidden_paths:
        if path.exists():
            fail(f"retired product path still exists: {path.relative_to(ROOT)}")

    dependabot = read_text(ROOT / ".github" / "dependabot.yml")
    if re.search(r"package-ecosystem:\s*[\"']?gradle", dependabot, re.IGNORECASE):
        fail("Gradle dependency automation must not remain after mobile removal")
    codeql = read_text(ROOT / ".github" / "workflows" / "codeql.yml")
    if "java-kotlin" in codeql:
        fail("Java/Kotlin CodeQL target must not remain after mobile removal")

    windows = contract.get("windows", {})
    props = ROOT / "Directory.Build.props"
    if xml_property(props, "VersionPrefix") != version or windows.get("productVersion") != version:
        fail("Windows product version mismatch")
    if xml_property(props, "AssemblyVersion") != windows.get("assemblyVersion"):
        fail("Windows AssemblyVersion mismatch")
    if xml_property(props, "FileVersion") != windows.get("fileVersion"):
        fail("Windows FileVersion mismatch")

    browsers = contract.get("browsers", {})
    if browsers.get("extensionVersion") != version:
        fail("browser version mismatch")
    variants = ["chrome", "edge", "opera", "firefox"]
    if browsers.get("variants") != variants:
        fail("browser variant list mismatch")
    if browsers.get("storePublication") != "not-published":
        fail("browser store publication status must reflect actual external state")
    for browser in variants:
        base = ROOT / "ekstenzije" / browser
        manifest = read_json(base / "manifest.json")
        if manifest.get("version") != version or manifest.get("name") != "__MSG_extensionName__":
            fail(f"{browser} manifest product/version mismatch")
        for locale in ("en", "hr"):
            messages = read_json(base / "_locales" / locale / "messages.json")
            if messages.get("extensionName", {}).get("message") != "SNAPVERE":
                fail(f"{browser} {locale} product name must remain SNAPVERE")
        source = read_text(base / "background.js")
        if not re.search(r"const\s+FILE_PREFIX\s*=\s*[\"']SNAPVERE[\"']", source):
            fail(f"{browser} capture filename brand is not locked")

    expected_assets = [
        "SNAPVERE-Setup.exe",
        "SNAPVERE-Portable.exe",
        "SNAPVERE-Chrome.zip",
        "SNAPVERE-Edge.zip",
        "SNAPVERE-Opera.zip",
        "SNAPVERE-Firefox.zip",
    ]
    if contract.get("releaseAssets") != expected_assets:
        fail("active release asset contract mismatch")

    releases = read_text(ROOT / "RELEASES.md")
    if f"Current public release: **v{version}**" not in releases or "## Unreleased" not in releases:
        fail("RELEASES.md current/unreleased structure mismatch")

    current_docs = [
        ROOT / "README.md",
        ROOT / "README.hr.md",
        ROOT / "SECURITY.md",
        ROOT / "CONTRIBUTING.md",
        ROOT / "ekstenzije" / "README.md",
        ROOT / "ekstenzije" / "PRIVACY.md",
        ROOT / "docs" / "README.md",
        ROOT / "docs" / "USER-GUIDE.md",
        ROOT / "docs" / "INSTALLATION.md",
        ROOT / "docs" / "SETTINGS.md",
        ROOT / "docs" / "BROWSER-EXTENSIONS.md",
        ROOT / "docs" / "PRIVACY.md",
        ROOT / "docs" / "TROUBLESHOOTING.md",
        ROOT / "docs" / "PRODUCT-STATUS.md",
        ROOT / "docs" / "QA-MATRIX.md",
        ROOT / "docs" / "ARCHITECTURE.md",
        ROOT / "docs" / "CAPTURE-ENGINE.md",
        ROOT / "docs" / "REGION-CAPTURE.md",
        ROOT / "docs" / "WINDOW-CAPTURE.md",
        ROOT / "docs" / "IMAGE-PIPELINE.md",
        ROOT / "docs" / "MULTI-MONITOR.md",
        ROOT / "docs" / "TRAY-LIFECYCLE.md",
        ROOT / "docs" / "BRANDING.md",
        ROOT / "docs" / "VERSIONING-RELEASES.md",
        ROOT / "docs" / "hr" / "README.md",
        ROOT / "docs" / "hr" / "USER-GUIDE.md",
        ROOT / "docs" / "hr" / "INSTALLATION.md",
        ROOT / "docs" / "hr" / "SETTINGS.md",
        ROOT / "docs" / "hr" / "WINDOW-CAPTURE.md",
        ROOT / "docs" / "hr" / "IMAGE-PIPELINE.md",
        ROOT / "docs" / "hr" / "TRAY-LIFECYCLE.md",
        ROOT / "docs" / "hr" / "BROWSER-EXTENSIONS.md",
        ROOT / "docs" / "hr" / "PRIVACY.md",
        ROOT / "docs" / "hr" / "TROUBLESHOOTING.md",
        ROOT / "docs" / "hr" / "PRODUCT-STATUS.md",
        ROOT / "docs" / "hr" / "QA-MATRIX.md",
    ]
    for path in current_docs:
        text = read_text(path)
        if re.search(r"\bandroid\b", text, re.IGNORECASE):
            fail(f"active documentation still describes the retired mobile product: {path.relative_to(ROOT)}")
        if re.search(r"\b(?:placeholder|coming soon|todo|dev build)\b", text, re.IGNORECASE):
            fail(f"active documentation contains development-only wording: {path.relative_to(ROOT)}")

    version_pinned_docs = [
        ROOT / "SECURITY.md",
        ROOT / "CONTRIBUTING.md",
        ROOT / "docs" / "README.md",
        ROOT / "docs" / "PRODUCT-STATUS.md",
        ROOT / "docs" / "VERSIONING-RELEASES.md",
        ROOT / "docs" / "hr" / "README.md",
        ROOT / "docs" / "hr" / "PRODUCT-STATUS.md",
    ]
    for path in version_pinned_docs:
        if version not in read_text(path):
            fail(f"current release marker missing from {path.relative_to(ROOT)}")

    for readme in (ROOT / "README.md", ROOT / "README.hr.md"):
        text = read_text(readme)
        if release_url not in text:
            fail(f"{readme.name} must link directly to the current release")
        for asset in expected_assets:
            if asset not in text:
                fail(f"{readme.name} missing active package name: {asset}")

    validate_links(current_docs)
    print(f"SNAPVERE product contract passed for {tag}: Windows + browsers, {len(expected_assets)} active packages.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except RuntimeError as exc:
        print(str(exc), file=sys.stderr)
        sys.exit(1)
