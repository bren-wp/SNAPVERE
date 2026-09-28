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

    ci = read_text(ROOT / ".github" / "workflows" / "ci.yml")
    if re.search(r"(?m)^\s*SNAPVERE_VERSION:\s*\d+\.\d+\.\d+\s*$", ci):
        fail("standard CI must derive SNAPVERE_VERSION from product-version.json instead of hardcoding a release")
    for required in (
        "Resolve package version from product contract",
        "product-version.json",
        "SNAPVERE_VERSION=$version",
        "GITHUB_ENV",
    ):
        if required not in ci:
            fail(f"standard CI is missing canonical version resolution fragment: {required}")

    portable_program = read_text(ROOT / "src" / "Snapvere.Portable" / "Program.cs")
    if re.search(r"\bexception\.Message\b", portable_program):
        fail("Portable startup UI must not expose raw exception.Message details")
    for required in (
        "PortableStartupDiagnostics.RecordLaunchFailure(exception)",
        "PortableStartupDiagnostics.GetUserFacingFailureMessage(exception)",
    ):
        if required not in portable_program:
            fail(f"Portable startup error containment is missing required fragment: {required}")

    startup_diagnostics = read_text(ROOT / "src" / "Snapvere.App" / "Services" / "StartupDiagnostics.cs")
    show_fatal_match = re.search(
        r"public static void ShowFatal\(.*?\n    \}\n\n    private static void AppendException",
        startup_diagnostics,
        re.DOTALL,
    )
    if show_fatal_match is None:
        fail("Windows startup fatal UI contract could not locate ShowFatal")
    show_fatal = show_fatal_match.group(0)
    if "LogFilePath" in show_fatal:
        fail("Windows startup fatal UI must not expose the local diagnostics-log path")
    if "UserFacingDiagnosticsText.StartupFailureMessage" not in show_fatal:
        fail("Windows startup fatal UI must use the centralized sanitized diagnostics message")

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

    localization = contract.get("localization", {})
    if localization.get("windows") != ["en", "hr"] or localization.get("browsers") != ["en", "hr"]:
        fail("public localization support matrix must be exactly English and Croatian until more locales are complete")

    localization_source = read_text(ROOT / "src" / "Snapvere.Shared" / "SnapvereLocalization.cs")
    languages_match = re.search(
        r"Languages\s*=\s*new\s*\(\s*\[(.*?)\]\s*\);",
        localization_source,
        re.DOTALL,
    )
    if languages_match is None:
        fail("could not locate Windows public language catalog")
    windows_language_codes = re.findall(r'new\("([^"]+)"\s*,', languages_match.group(1))
    if windows_language_codes != localization["windows"]:
        fail(f"Windows public language catalog mismatch: {windows_language_codes}")

    store_listing = read_json(ROOT / "ekstenzije" / "store" / "listing.json")
    if store_listing.get("extensionVersion") != version:
        fail("browser store listing version mismatch")
    if store_listing.get("developmentChannel") != f"v{version}-release":
        fail("browser store development channel mismatch")

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
        actual_locales = sorted(path.name for path in (base / "_locales").iterdir() if path.is_dir())
        expected_locales = sorted(localization["browsers"])
        if actual_locales != expected_locales:
            fail(f"{browser} browser locale matrix mismatch: {actual_locales}")
        for locale in localization["browsers"]:
            messages = read_json(base / "_locales" / locale / "messages.json")
            if messages.get("extensionName", {}).get("message") != "SNAPVERE":
                fail(f"{browser} {locale} product name must remain SNAPVERE")
        source = read_text(base / "background.js")
        if not re.search(r"const\s+FILE_PREFIX\s*=\s*[\"']SNAPVERE[\"']", source):
            fail(f"{browser} capture filename brand is not locked")

    msi_project = read_text(ROOT / "src" / "Snapvere.Msi" / "Snapvere.Msi.wixproj")
    if 'WixToolset.Sdk/5.0.2' not in msi_project:
        fail("MSI build toolchain must remain pinned to the reviewed WiX 5.0.2 SDK")
    for required in (
        "<MsiArchitecture Condition=\"'$(MsiArchitecture)' == ''\">x64</MsiArchitecture>",
        "<InstallerPlatform>$(MsiArchitecture)</InstallerPlatform>",
        "<SuppressValidation>false</SuppressValidation>",
        "<SuppressIces>ICE03</SuppressIces>",
        "SnapverePayloadDir",
    ):
        if required not in msi_project:
            fail(f"MSI project is missing required build contract: {required}")

    if xml_property(ROOT / "src" / "Snapvere.Msi" / "Snapvere.Msi.wixproj", "SuppressIces") != "ICE03":
        fail("MSI initial build may suppress only ICE03; the final package must be normalized and fully revalidated")

    msi_normalizer = read_text(ROOT / "eng" / "Normalize-SnapvereMsiLanguageMetadata.ps1")
    for required in (
        "IsValidLocale",
        "LocaleSupported",
        "GetMethod(",
        ".Invoke($null, $arguments)",
        "NormalizedLanguage = '0'",
        "database.Commit()",
        "File.Language",
    ):
        if required not in msi_normalizer:
            fail(f"MSI language normalizer is missing required safety fragment: {required}")

    try:
        msi_root = ET.fromstring(read_text(ROOT / "src" / "Snapvere.Msi" / "Package.wxs"))
    except ET.ParseError as exc:
        fail(f"invalid MSI WiX authoring: {exc}")
    ns = {"w": "http://wixtoolset.org/schemas/v4/wxs"}
    package = msi_root.find("w:Package", ns)
    if package is None:
        fail("MSI authoring does not contain a Package element")
    expected_package_attributes = {
        "Name": "SNAPVERE",
        "Manufacturer": "Brendigo",
        "Version": "$(var.SnapvereVersion)",
        "ProductCode": "*",
        "UpgradeCode": "{A680451A-7E71-4E13-9814-8517D3BC6A89}",
        "UpgradeStrategy": "majorUpgrade",
        "Scope": "perMachine",
    }
    for name, expected in expected_package_attributes.items():
        if package.get(name) != expected:
            fail(f"MSI Package/{name} mismatch: expected {expected!r}, got {package.get(name)!r}")
    if package.find("w:MajorUpgrade", ns) is None:
        fail("MSI package must author an explicit MajorUpgrade policy")

    program_menu = package.find("w:StandardDirectory[@Id='ProgramMenuFolder']", ns)
    if program_menu is None or program_menu.find("w:Directory[@Id='ApplicationProgramsFolder']", ns) is None:
        fail("MSI package must place SNAPVERE shortcuts under ProgramMenuFolder/ApplicationProgramsFolder")
    if package.find("w:StandardDirectory[@Id='CommonProgramsFolder']", ns) is not None:
        fail("CommonProgramsFolder is not valid StandardDirectory authoring")

    executable = package.find(".//w:File[@Id='SnapvereExecutable']", ns)
    if executable is None or executable.get("Source") != r"$(var.SnapverePayloadDir)\Snapvere.exe":
        fail("MSI package must explicitly author SnapvereExecutable from the release payload")
    if executable.get("KeyPath") != "yes":
        fail("SnapvereExecutable must be the MainExecutable component key path")

    shortcut = executable.find("w:Shortcut[@Id='StartMenuShortcut']", ns)
    if shortcut is None:
        fail("MSI package must install the SNAPVERE Start Menu shortcut from SnapvereExecutable")
    expected_shortcut = {
        "Directory": "ApplicationProgramsFolder",
        "WorkingDirectory": "INSTALLFOLDER",
        "Advertise": "yes",
    }
    for name, expected in expected_shortcut.items():
        if shortcut.get(name) != expected:
            fail(f"MSI StartMenuShortcut/{name} mismatch: expected {expected!r}, got {shortcut.get(name)!r}")
    if shortcut.get("Target") is not None:
        fail("advertised StartMenuShortcut must inherit its target from parent SnapvereExecutable")

    remove_folder = package.find(".//w:RemoveFolder[@Id='RemoveApplicationProgramsFolder']", ns)
    if (
        remove_folder is None
        or remove_folder.get("Directory") != "ApplicationProgramsFolder"
        or remove_folder.get("On") != "uninstall"
    ):
        fail("MSI must remove ApplicationProgramsFolder when MainExecutable is uninstalled")

    main_component = package.find(".//w:Component[@Id='MainExecutable']", ns)
    if main_component is None or main_component.find("w:File[@Id='SnapvereExecutable']", ns) is None:
        fail("MSI SnapvereExecutable must be owned by MainExecutable")

    excludes = [
        element.get("Files")
        for element in package.findall(".//w:Files/w:Exclude", ns)
        if element.get("Files")
    ]
    if r"$(var.SnapverePayloadDir)\Snapvere.exe" not in excludes:
        fail("MSI wildcard harvesting must exclude explicitly authored Snapvere.exe")
    if package.find(".//w:RegistryValue[@Root='HKLM']", ns) is not None:
        fail("MSI Start Menu ownership must not use an HKLM registry key path")

    if package.find(".//w:CustomAction", ns) is not None:
        fail("MSI package must not introduce custom actions without an explicit reviewed need")

    arm64_ci = read_text(ROOT / ".github" / "workflows" / "arm64-runtime-ci.yml")
    for required in (
        "runs-on: windows-11-vs2026-arm",
        "Confirm native ARM64 runner",
        "-Arch arm64",
        "-p:MsiArchitecture=arm64",
        "Run native ARM64 MSI lifecycle",
    ):
        if required not in arm64_ci:
            fail(f"ARM64 runtime CI is missing native-runtime gate: {required}")

    msi_ci = read_text(ROOT / ".github" / "workflows" / "msi-ci.yml")
    for required in (
        "Build current production MSI",
        "Build synthetic previous-version MSI for major-upgrade QA",
        "Assert-SnapvereMsi.ps1",
        "Normalize-SnapvereMsiLanguageMetadata.ps1",
        "./artifacts/wix-cli/wix.exe msi validate",
        "Test-SnapvereMsiLifecycle.ps1",
        "clean install repair major upgrade and uninstall",
        "if-no-files-found: warn",
    ):
        if required not in msi_ci:
            fail(f"MSI CI is missing lifecycle gate: {required}")

    expected_assets = [
        "SNAPVERE-Setup.exe",
        "SNAPVERE-Setup.msi",
        "SNAPVERE-Portable.exe",
        "SNAPVERE-Chrome.zip",
        "SNAPVERE-Edge.zip",
        "SNAPVERE-Opera.zip",
        "SNAPVERE-Firefox.zip",
    ]
    if contract.get("releaseAssets") != expected_assets:
        fail("active release asset contract mismatch")

    release_workflow = ROOT / ".github" / "workflows" / "release.yml"
    release_workflow_text = read_text(release_workflow)
    if re.search(r"(?m)^\s*SNAPVERE_VERSION:\s*\d+\.\d+\.\d+\s*$", release_workflow_text):
        fail("generic release workflow must not hardcode SNAPVERE_VERSION")
    for required in (
        "name: Release SNAPVERE",
        "product-version.json",
        ".github/release-triggers/",
        "python eng/validate-product-contract.py",
        "SNAPVERE-Setup.msi",
        "Assert-SnapvereMsi.ps1",
        "Normalize-SnapvereMsiLanguageMetadata.ps1",
        "./artifacts/wix-cli/wix.exe msi validate",
        "Test-SnapvereMsiLifecycle.ps1",
        "SHA256SUMS",
        "gh release create",
    ):
        if required not in release_workflow_text:
            fail(f"generic release workflow is missing required release gate: {required}")

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
        ROOT / "docs" / "BROWSER-EXTENSIONS.md",
        ROOT / "docs" / "PRIVACY.md",
        ROOT / "docs" / "TROUBLESHOOTING.md",
        ROOT / "docs" / "PRODUCT-STATUS.md",
        ROOT / "docs" / "QA-MATRIX.md",
        ROOT / "docs" / "ARCHITECTURE.md",
        ROOT / "docs" / "WINDOW-CAPTURE.md",
        ROOT / "docs" / "IMAGE-PIPELINE.md",
        ROOT / "docs" / "TRAY-LIFECYCLE.md",
        ROOT / "docs" / "BRANDING.md",
        ROOT / "docs" / "VERSIONING-RELEASES.md",
        ROOT / "docs" / "hr" / "README.md",
        ROOT / "docs" / "hr" / "USER-GUIDE.md",
        ROOT / "docs" / "hr" / "INSTALLATION.md",
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

    evergreen_docs = [
        ROOT / "docs" / "BRANDING.md",
        ROOT / "docs" / "INSTALLATION.md",
        ROOT / "docs" / "QA-MATRIX.md",
        ROOT / "docs" / "USER-GUIDE.md",
        ROOT / "docs" / "TRAY-LIFECYCLE.md",
        ROOT / "docs" / "TROUBLESHOOTING.md",
        ROOT / "docs" / "PRIVACY.md",
        ROOT / "docs" / "WINDOW-CAPTURE.md",
        ROOT / "docs" / "hr" / "INSTALLATION.md",
        ROOT / "docs" / "hr" / "QA-MATRIX.md",
        ROOT / "docs" / "hr" / "USER-GUIDE.md",
        ROOT / "docs" / "hr" / "TRAY-LIFECYCLE.md",
        ROOT / "docs" / "hr" / "TROUBLESHOOTING.md",
        ROOT / "docs" / "hr" / "PRIVACY.md",
        ROOT / "docs" / "hr" / "WINDOW-CAPTURE.md",
    ]
    for path in evergreen_docs:
        stale_versions = re.findall(r"\bv\d+\.\d+\.\d+\b", read_text(path))
        if stale_versions:
            fail(f"evergreen documentation must not hardcode release versions in {path.relative_to(ROOT)}: {stale_versions}")

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
        text = read_text(path)
        if version not in text:
            fail(f"current release marker missing from {path.relative_to(ROOT)}")
        for stale in re.findall(r"\bv(\d+\.\d+\.\d+)\b", text):
            if stale != version:
                fail(f"stale current-release marker v{stale} in {path.relative_to(ROOT)}")

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
