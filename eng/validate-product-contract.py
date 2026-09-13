#!/usr/bin/env python3
"""Validate SNAPVERE's cross-platform version, documentation and release contract."""

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


def require_regex(text: str, pattern: str, label: str) -> None:
    if re.search(pattern, text, re.MULTILINE) is None:
        fail(f"{label} does not match canonical product contract")


def xml_property(path: Path, name: str) -> str:
    try:
        root = ET.fromstring(read_text(path))
    except ET.ParseError as exc:
        fail(f"invalid XML in {path.relative_to(ROOT)}: {exc}")
    for element in root.iter(name):
        if element.text:
            return element.text.strip()
    fail(f"missing <{name}> in {path.relative_to(ROOT)}")


def android_string_keys(path: Path) -> set[str]:
    try:
        root = ET.fromstring(read_text(path))
    except ET.ParseError as exc:
        fail(f"invalid Android resources XML in {path.relative_to(ROOT)}: {exc}")
    keys: set[str] = set()
    for child in root:
        name = child.attrib.get("name")
        if name:
            keys.add(name)
    return keys


def validate_markdown_links() -> None:
    markdown_roots = [
        ROOT / "README.md",
        ROOT / "README.hr.md",
        ROOT / "SECURITY.md",
        ROOT / "CONTRIBUTING.md",
        ROOT / "android" / "README.md",
        ROOT / "ekstenzije" / "README.md",
        ROOT / "ekstenzije" / "PRIVACY.md",
    ]
    markdown_roots.extend(sorted((ROOT / "docs").rglob("*.md")))

    link_pattern = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")
    for path in markdown_roots:
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

    windows = contract.get("windows", {})
    props = ROOT / "Directory.Build.props"
    if xml_property(props, "VersionPrefix") != windows.get("productVersion") or windows.get("productVersion") != version:
        fail("Windows VersionPrefix must match currentRelease")
    if xml_property(props, "AssemblyVersion") != windows.get("assemblyVersion"):
        fail("Windows AssemblyVersion mismatch")
    if xml_property(props, "FileVersion") != windows.get("fileVersion"):
        fail("Windows FileVersion mismatch")

    android = contract.get("android", {})
    gradle = read_text(ROOT / "android" / "app" / "build.gradle")
    for key in ("versionCode", "minSdk", "targetSdk", "compileSdk"):
        value = android.get(key)
        if not isinstance(value, int):
            fail(f"android.{key} must be an integer")
        require_regex(gradle, rf"\b{re.escape(key)}\s+{value}\b", f"Android {key}")
    require_regex(
        gradle,
        rf"\bversionName\s+['\"]{re.escape(str(android.get('versionName')))}['\"]",
        "Android versionName",
    )
    if android.get("versionName") != version:
        fail("Android versionName must match currentRelease")
    if android.get("publicApkSigning") != "ci-debug":
        fail("Android public signing disclosure changed; update validator and documentation deliberately")

    default_strings = android_string_keys(ROOT / "android" / "app" / "src" / "main" / "res" / "values" / "strings.xml")
    hr_strings = android_string_keys(ROOT / "android" / "app" / "src" / "main" / "res" / "values-hr" / "strings.xml")
    if default_strings != hr_strings:
        missing_hr = sorted(default_strings - hr_strings)
        extra_hr = sorted(hr_strings - default_strings)
        fail(f"Android EN/HR string-key parity mismatch; missing_hr={missing_hr}, extra_hr={extra_hr}")

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

    assets = contract.get("releaseAssets")
    expected_assets = [
        "SNAPVERE-Setup.exe",
        "SNAPVERE-Portable.exe",
        "SNAPVERE.apk",
        "SNAPVERE-Android-Source.zip",
        "SNAPVERE-Chrome.zip",
        "SNAPVERE-Edge.zip",
        "SNAPVERE-Opera.zip",
        "SNAPVERE-Firefox.zip",
    ]
    if assets != expected_assets:
        fail("releaseAssets must match the exact eight-file v0.1.1 public contract")

    required_current_docs = [
        ROOT / "README.md",
        ROOT / "README.hr.md",
        ROOT / "SECURITY.md",
        ROOT / "CONTRIBUTING.md",
        ROOT / "android" / "README.md",
        ROOT / "ekstenzije" / "README.md",
        ROOT / "RELEASE_NOTES_0.1.1.md",
        ROOT / "docs" / "README.md",
        ROOT / "docs" / "hr" / "README.md",
        ROOT / "docs" / "ARCHITECTURE.md",
        ROOT / "docs" / "hr" / "ARCHITECTURE.md",
        ROOT / "docs" / "INSTALLATION.md",
        ROOT / "docs" / "hr" / "INSTALLATION.md",
        ROOT / "docs" / "ANDROID.md",
        ROOT / "docs" / "hr" / "ANDROID.md",
        ROOT / "docs" / "BROWSER-EXTENSIONS.md",
        ROOT / "docs" / "hr" / "BROWSER-EXTENSIONS.md",
        ROOT / "docs" / "PRODUCT-STATUS.md",
        ROOT / "docs" / "hr" / "PRODUCT-STATUS.md",
        ROOT / "docs" / "QA-MATRIX.md",
        ROOT / "docs" / "hr" / "QA-MATRIX.md",
        ROOT / "docs" / "PRIVACY.md",
        ROOT / "docs" / "hr" / "PRIVACY.md",
        ROOT / "docs" / "VERSIONING-RELEASES.md",
        ROOT / "docs" / "hr" / "VERSIONING-RELEASES.md",
        ROOT / "docs" / "RELEASE-0.1.1.md",
        ROOT / "docs" / "SECURITY-PERFORMANCE-0.1.1.md",
        ROOT / "docs" / "hr" / "SECURITY-PERFORMANCE-0.1.1.md",
    ]
    for path in required_current_docs:
        text = read_text(path)
        if version not in text:
            fail(f"current documentation does not mention {version}: {path.relative_to(ROOT)}")

    stale_current_patterns = [
        re.compile(r"current\s+(?:public\s+)?release(?:\s+line)?\s+is\s+\*\*0\.1\.0\*\*", re.IGNORECASE),
        re.compile(r"aktualna\s+release\s+linija\s+je\s+\*\*0\.1\.0\*\*", re.IGNORECASE),
        re.compile(r"javni\s+release\s+ugovor\s+0\.1\.0", re.IGNORECASE),
    ]
    for path in required_current_docs:
        text = read_text(path)
        for pattern in stale_current_patterns:
            if pattern.search(text):
                fail(f"stale 0.1.0 current-release wording remains in {path.relative_to(ROOT)}")

    for readme in (ROOT / "README.md", ROOT / "README.hr.md"):
        text = read_text(readme)
        if release_url not in text:
            fail(f"{readme.name} must link directly to the current GitHub release")
        for asset in expected_assets:
            if asset not in text:
                fail(f"{readme.name} missing release asset: {asset}")

    hr_index = read_text(ROOT / "docs" / "hr" / "README.md")
    if "0.0.7" in hr_index:
        fail("Croatian documentation index still contains stale 0.0.7 product-contract text")

    validate_markdown_links()
    print(
        f"SNAPVERE product contract validation passed for {tag} "
        f"({len(expected_assets)} public assets, {len(required_current_docs)} current documents)."
    )
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except RuntimeError as exc:
        print(str(exc), file=sys.stderr)
        sys.exit(1)
