#!/usr/bin/env python3
"""Validate SNAPVERE's active Windows/browser product and release contract."""

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


def validate_release_history(version: str) -> None:
    releases_path = ROOT / "RELEASES.md"
    text = read_text(releases_path)

    if f"Current public release: **v{version}**" not in text:
        fail("RELEASES.md current-public-release marker does not match currentRelease")
    if "## Unreleased" not in text:
        fail("RELEASES.md must contain an Unreleased section for post-release main work")

    historical_versions = [
        "0.1.1", "0.1.0", "0.0.9", "0.0.8", "0.0.7", "0.0.6",
        "0.0.5", "0.0.4", "0.0.3", "0.0.2", "0.0.1",
    ]
    positions: list[int] = []
    for historical_version in historical_versions:
        match = re.search(rf"^## v{re.escape(historical_version)}\b", text, re.MULTILINE)
        if match is None:
            fail(f"RELEASES.md missing historical section v{historical_version}")
        positions.append(match.start())

    if positions != sorted(positions):
        fail("RELEASES.md version sections must be newest-to-oldest")

    fragmented = sorted(ROOT.glob("RELEASE_NOTES_*.md"))
    if fragmented:
        names = ", ".join(path.name for path in fragmented)
        fail(f"version-specific root release-note files are forbidden; use RELEASES.md: {names}")


def validate_markdown_links(paths: list[Path]) -> None:
    link_pattern = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")
    for path in paths:
        text = read_text(path)
        for raw_target in link_pattern.findall(text):
            target = raw_target.strip()
            if not target:
                fail(f"empty Markdown link in {path.relative_to(ROOT)}")
            if target.startswith(("http://", "https://", "mailto:", "#")):
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
    if contract.get("schemaVersion") != 1:
        fail("product-version.json schemaVersion must be 1")
    if contract.get("product") != "SNAPVERE":
        fail("product-version.json product must be SNAPVERE")

    version = contract.get("currentRelease")
    tag = contract.get("releaseTag")
    release_url = contract.get("releaseUrl")
    if not isinstance(version, str) or not re.fullmatch(r"\d+\.\d+\.\d+", version):
        fail("currentRelease must use x.y.z")
    if tag != f"v{version}":
        fail("releaseTag must be v + currentRelease")
    if release_url != f"https://github.com/bren-wp/SNAPVERE/releases/tag/{tag}":
        fail("releaseUrl does not match releaseTag")

    if contract.get("supportedPlatforms") != ["windows", "browsers"]:
        fail("supportedPlatforms must contain only windows and browsers")
    if "android" in contract:
        fail("Android must not be present in the active product contract")

    forbidden_android_paths = [
        ROOT / "android",
        ROOT / ".github" / "workflows" / "android-ci.yml",
        ROOT / "docs" / "ANDROID.md",
        ROOT / "docs" / "hr" / "ANDROID.md",
    ]
    for path in forbidden_android_paths:
        if path.exists():
            fail(f"removed Android product path is still present: {path.relative_to(ROOT)}")

    windows = contract.get("windows", {})
    props = ROOT / "Directory.Build.props"
    if xml_property(props, "VersionPrefix") != windows.get("productVersion") or windows.get("productVersion") != version:
        fail("Windows VersionPrefix must match currentRelease")
    if xml_property(props, "AssemblyVersion") != windows.get("assemblyVersion"):
        fail("Windows AssemblyVersion mismatch")
    if xml_property(props, "FileVersion") != windows.get("fileVersion"):
        fail("Windows FileVersion mismatch")

    browsers = contract.get("browsers", {})
    browser_version = browsers.get("extensionVersion")
    if browser_version != version:
        fail("browser extensionVersion must match currentRelease")
    variants = browsers.get("variants")
    if variants != ["chrome", "edge", "opera", "firefox"]:
        fail("browser variants must be chrome, edge, opera, firefox in canonical order")
    for browser in variants:
        manifest = read_json(ROOT / "ekstenzije" / browser / "manifest.json")
        if manifest.get("version") != browser_version:
            fail(f"{browser} manifest version mismatch")
    listing = read_json(ROOT / "ekstenzije" / "store" / "listing.json")
    if listing.get("extensionVersion") != browser_version:
        fail("browser store listing version mismatch")
    if browsers.get("storePublication") != "not-published":
        fail("browser store publication status changed; update contract only after real external publication")

    expected_assets = [
        "SNAPVERE-Setup.exe",
        "SNAPVERE-Portable.exe",
        "SNAPVERE-Chrome.zip",
        "SNAPVERE-Edge.zip",
        "SNAPVERE-Opera.zip",
        "SNAPVERE-Firefox.zip",
    ]
    if contract.get("releaseAssets") != expected_assets:
        fail("releaseAssets must match the active Windows/browser distribution contract")

    validate_release_history(version)

    current_docs = [
        ROOT / "README.md",
        ROOT / "README.hr.md",
        ROOT / "SECURITY.md",
        ROOT / "CONTRIBUTING.md",
        ROOT / "ekstenzije" / "README.md",
        ROOT / "ekstenzije" / "PRIVACY.md",
        ROOT / "docs" / "README.md",
        ROOT / "docs" / "hr" / "README.md",
        ROOT / "docs" / "ARCHITECTURE.md",
        ROOT / "docs" / "hr" / "ARCHITECTURE.md",
        ROOT / "docs" / "INSTALLATION.md",
        ROOT / "docs" / "hr" / "INSTALLATION.md",
        ROOT / "docs" / "BROWSER-EXTENSIONS.md",
        ROOT / "docs" / "hr" / "BROWSER-EXTENSIONS.md",
        ROOT / "docs" / "SETTINGS.md",
        ROOT / "docs" / "hr" / "SETTINGS.md",
        ROOT / "docs" / "PRODUCT-STATUS.md",
        ROOT / "docs" / "hr" / "PRODUCT-STATUS.md",
        ROOT / "docs" / "QA-MATRIX.md",
        ROOT / "docs" / "hr" / "QA-MATRIX.md",
        ROOT / "docs" / "PRIVACY.md",
        ROOT / "docs" / "hr" / "PRIVACY.md",
        ROOT / "docs" / "USER-GUIDE.md",
        ROOT / "docs" / "hr" / "USER-GUIDE.md",
        ROOT / "docs" / "TROUBLESHOOTING.md",
        ROOT / "docs" / "hr" / "TROUBLESHOOTING.md",
        ROOT / "docs" / "VERSIONING-RELEASES.md",
        ROOT / "docs" / "hr" / "VERSIONING-RELEASES.md",
    ]
    for path in current_docs:
        text = read_text(path)
        if path.name not in {"SECURITY.md", "CONTRIBUTING.md"} and version not in text:
            fail(f"current documentation does not mention {version}: {path.relative_to(ROOT)}")
        if re.search(r"\bandroid\b", text, re.IGNORECASE):
            fail(f"active documentation still presents Android content: {path.relative_to(ROOT)}")

    for readme in (ROOT / "README.md", ROOT / "README.hr.md"):
        text = read_text(readme)
        if release_url not in text:
            fail(f"{readme.name} must link directly to the current GitHub release")
        for asset in expected_assets:
            if asset not in text:
                fail(f"{readme.name} missing active release asset: {asset}")

    markdown_paths = [ROOT / "RELEASES.md", *current_docs]
    markdown_paths.extend(sorted((ROOT / "docs").glob("*.md")))
    markdown_paths.extend(sorted((ROOT / "docs" / "hr").glob("*.md")))
    validate_markdown_links(list(dict.fromkeys(markdown_paths)))

    print(
        f"SNAPVERE product contract validation passed for {tag} "
        f"({len(expected_assets)} active assets; Windows + browsers only)."
    )
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except RuntimeError as exc:
        print(str(exc), file=sys.stderr)
        sys.exit(1)
