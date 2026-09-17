#!/usr/bin/env python3
"""Prepare the SNAPVERE 0.1.3 source/version contract on the release branch."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OLD = "0.1.2"
NEW = "0.1.3"
DATE = "2026-09-17"


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    (ROOT / path).write_text(text, encoding="utf-8", newline="\n")


def replace_required(path: str, old: str, new: str, count: int | None = None) -> None:
    text = read(path)
    found = text.count(old)
    if found == 0:
        raise SystemExit(f"{path}: expected text not found: {old!r}")
    if count is not None and found != count:
        raise SystemExit(f"{path}: expected {count} occurrence(s) of {old!r}, found {found}")
    write(path, text.replace(old, new))


def update_json(path: str, mutate) -> None:
    data = json.loads(read(path))
    mutate(data)
    write(path, json.dumps(data, ensure_ascii=False, indent=2) + "\n")


def update_contract() -> None:
    def mutate(data):
        data["currentRelease"] = NEW
        data["releaseTag"] = f"v{NEW}"
        data["releaseUrl"] = f"https://github.com/bren-wp/SNAPVERE/releases/tag/v{NEW}"
        data["windows"]["productVersion"] = NEW
        data["windows"]["assemblyVersion"] = f"{NEW}.0"
        data["windows"]["fileVersion"] = f"{NEW}.0"
        data["browsers"]["extensionVersion"] = NEW
    update_json("product-version.json", mutate)


def update_windows_version() -> None:
    text = read("Directory.Build.props")
    replacements = {
        f"<VersionPrefix>{OLD}</VersionPrefix>": f"<VersionPrefix>{NEW}</VersionPrefix>",
        f"<AssemblyVersion>{OLD}.0</AssemblyVersion>": f"<AssemblyVersion>{NEW}.0</AssemblyVersion>",
        f"<FileVersion>{OLD}.0</FileVersion>": f"<FileVersion>{NEW}.0</FileVersion>",
    }
    for old, new in replacements.items():
        if old not in text:
            raise SystemExit(f"Directory.Build.props missing {old}")
        text = text.replace(old, new)
    write("Directory.Build.props", text)


def update_browser_versions() -> None:
    for browser in ("chrome", "edge", "opera", "firefox"):
        path = f"ekstenzije/{browser}/manifest.json"
        def mutate(data):
            if data.get("version") != OLD:
                raise SystemExit(f"{path}: expected version {OLD}, got {data.get('version')}")
            data["version"] = NEW
        update_json(path, mutate)

    def mutate_listing(data):
        if data.get("extensionVersion") != OLD:
            raise SystemExit("store listing extensionVersion is not the previous release")
        data["extensionVersion"] = NEW
        data["developmentChannel"] = f"v{NEW}-release"
    update_json("ekstenzije/store/listing.json", mutate_listing)


def update_current_docs() -> None:
    # These documents explicitly advertise the current public line. They are
    # current-product documents, not historical release records.
    current_marketing_docs = [
        "README.md",
        "README.hr.md",
        "SECURITY.md",
        "CONTRIBUTING.md",
        "docs/README.md",
        "docs/hr/README.md",
        "docs/PRODUCT-STATUS.md",
        "docs/hr/PRODUCT-STATUS.md",
    ]
    for path in current_marketing_docs:
        text = read(path)
        if OLD not in text:
            raise SystemExit(f"{path}: expected current-release marker {OLD}")
        write(path, text.replace(f"v{OLD}", f"v{NEW}").replace(OLD, NEW))

    # Release/versioning guidance can contain historical examples. Update only
    # explicit current-release statements/URLs and leave historical facts alone.
    path = "docs/VERSIONING-RELEASES.md"
    text = read(path)
    patterns = [
        (f"Current public release: **v{OLD}**", f"Current public release: **v{NEW}**"),
        (f"releases/tag/v{OLD}", f"releases/tag/v{NEW}"),
        (f"current release `{OLD}`", f"current release `{NEW}`"),
        (f"current release **{OLD}**", f"current release **{NEW}**"),
    ]
    changed = False
    for old, new in patterns:
        if old in text:
            text = text.replace(old, new)
            changed = True
    if not changed and NEW not in text:
        text = text.replace("# ", f"# ", 1)
        lines = text.splitlines()
        lines.insert(1, "")
        lines.insert(2, f"Current public release: **v{NEW}**.")
        text = "\n".join(lines) + ("\n" if not text.endswith("\n") else "")
    write(path, text)


def update_validator() -> None:
    path = "eng/validate-product-contract.py"
    text = read(path)
    old = '''    for path in current_docs:\n        text = read_text(path)\n        if version not in text and path.name not in {"README.md", "PRIVACY.md"}:\n            fail(f"active documentation does not identify current release: {path.relative_to(ROOT)}")\n        if re.search(r"\\bandroid\\b", text, re.IGNORECASE):\n'''
    new = '''    for path in current_docs:\n        text = read_text(path)\n        if re.search(r"\\bandroid\\b", text, re.IGNORECASE):\n'''
    if old not in text:
        raise SystemExit("validate-product-contract.py: expected documentation version gate not found")
    text = text.replace(old, new)
    anchor = '''    for readme in (ROOT / "README.md", ROOT / "README.hr.md"):\n'''
    addition = '''    version_pinned_docs = [\n        ROOT / "SECURITY.md",\n        ROOT / "CONTRIBUTING.md",\n        ROOT / "docs" / "README.md",\n        ROOT / "docs" / "PRODUCT-STATUS.md",\n        ROOT / "docs" / "VERSIONING-RELEASES.md",\n        ROOT / "docs" / "hr" / "README.md",\n        ROOT / "docs" / "hr" / "PRODUCT-STATUS.md",\n    ]\n    for path in version_pinned_docs:\n        if version not in read_text(path):\n            fail(f"current release marker missing from {path.relative_to(ROOT)}")\n\n'''
    if anchor not in text:
        raise SystemExit("validate-product-contract.py: README validation anchor missing")
    text = text.replace(anchor, addition + anchor)
    write(path, text)


def update_releases() -> None:
    path = "RELEASES.md"
    text = read(path)
    text = text.replace(f"Current public release: **v{OLD}**", f"Current public release: **v{NEW}**", 1)
    text = text.replace(f"releases/tag/v{OLD}", f"releases/tag/v{NEW}", 1)
    unreleased_old = f"## Unreleased\n\nNo post-v{OLD} release changes are documented yet.\n\n---\n\n## v{OLD} —"
    release_section = f'''## Unreleased\n\nNo post-v{NEW} release changes are documented yet.\n\n---\n\n## v{NEW} — {DATE}\n\nSNAPVERE {NEW} promotes the post-v{OLD} reliability and UX work into the public **Windows + Chrome/Edge/Opera/Firefox** release line while preserving the local-first privacy model and exact six-package release contract.\n\n### Windows UX and failure recovery\n\n- Tray **Settings** and **Recent captures** are fully wired instead of presentation-only surfaces.\n- User-triggered Region, Window and Screen capture failures now receive sanitized feedback for busy, unsupported and capture-failure states without exposing raw exception text.\n- Existing tray-first startup, capture-folder access, language switching, About, Exit, global shortcuts, x86/x64 payloads and ARM64 build coverage remain intact.\n\n### Browser capture parity and accessibility\n\n- Chrome, Edge, Opera and Firefox now expose Settings and Recent captures with the same EN/HR UX, including Open, Refresh and **Open downloads folder** actions using the existing downloads permission.\n- Recent-capture views ignore completed download records whose local file no longer exists, avoiding stale **Open** actions after a file has been removed or moved.\n- Settings/Recent navigation follows the ARIA keyboard tab pattern with roving `tabIndex`, Arrow Left/Right, Home and End behavior.\n- Browser capture hotkeys are now functional: `Ctrl+Shift+1` Region, `Ctrl+Shift+2` Visible Area and `Ctrl+Shift+3` Full Page, with Command+Shift equivalents on macOS. They reuse the existing serialized capture pipeline and capture lock rather than introducing a second implementation.\n- Hotkey-triggered capture failures now surface localized, non-invasive action badge/title feedback for capture busy, unsupported pages, active-tab changes, oversized full-page captures and generic region/capture failures. Raw exceptions and stack traces remain internal.\n- Opening browser Settings provides progress and localized failure feedback when the options page cannot be opened.\n\n### CI and contract hardening\n\n- Behavioral browser tests activate real command handlers and cover visible, region-lock/cleanup, full-page completion, unknown-command no-op and localized hotkey-failure feedback paths.\n- Extension validation locks the expected command IDs, default/macOS shortcuts and localization keys, and also catches popup localization-reference drift.\n- Chrome, Edge, Opera and Firefox shared source parity remains enforced; Firefox retains only required Gecko differences.\n- No new extension permission, broad host access, telemetry, analytics, cloud upload, tracking or remote runtime dependency is introduced.\n\n### Public packages\n\nThe v{NEW} GitHub Release contains exactly:\n\n```text\nSNAPVERE-Setup.exe\nSNAPVERE-Portable.exe\nSNAPVERE-Chrome.zip\nSNAPVERE-Edge.zip\nSNAPVERE-Opera.zip\nSNAPVERE-Firefox.zip\n```\n\nPublication re-runs the audited Windows/browser build gates, validates x64/x86 lifecycle completion, verifies deterministic browser ZIPs and checks SHA-256 digests before and after GitHub Release publication. Browser-store publication remains a separate external publisher/reviewer process.\n\n---\n\n## v{OLD} —'''
    if unreleased_old not in text:
        raise SystemExit("RELEASES.md: expected Unreleased/v0.1.2 boundary not found")
    text = text.replace(unreleased_old, release_section, 1)
    write(path, text)


def update_changelog() -> None:
    path = "CHANGELOG.md"
    text = read(path)
    boundary = f"## [Unreleased]\n\nNo post-{OLD} changes are documented yet.\n\n## [{OLD}] -"
    section = f'''## [Unreleased]\n\nNo post-{NEW} changes are documented yet.\n\n## [{NEW}] - {DATE}\n\n### Windows UX and recovery\n\n- Wire tray Settings and Recent captures into production behavior.\n- Surface sanitized user-facing feedback for busy, unsupported and failed Region/Window/Screen capture actions without exposing raw exceptions.\n\n### Browser UX, accessibility and capture reliability\n\n- Add Settings/Recent captures parity, Open/Refresh/Open-downloads-folder actions and stale deleted/moved download filtering across Chrome, Edge, Opera and Firefox.\n- Add keyboard-accessible ARIA tab navigation for Settings/Recent panels.\n- Add functional Region/Visible/Full Page keyboard commands that reuse the existing serialized capture/lock pipeline.\n- Add localized action badge/title feedback for hotkey-triggered busy, unsupported-page, active-tab-changed, full-page-too-large and generic capture failures.\n- Harden popup localization-key validation and Settings-opening failure feedback.\n\n### Validation and packaging\n\n- Expand behavioral command/failure-feedback coverage and lock command IDs, shortcut mappings and EN/HR localization references in CI.\n- Preserve the exact six-package Windows/browser release contract, deterministic browser packaging and local-first permission/privacy boundaries.\n\n## [{OLD}] -'''
    if boundary not in text:
        raise SystemExit("CHANGELOG.md: expected Unreleased/0.1.2 boundary not found")
    text = text.replace(boundary, section, 1)
    write(path, text)


def main() -> None:
    update_contract()
    update_windows_version()
    update_browser_versions()
    update_current_docs()
    update_validator()
    update_releases()
    update_changelog()
    print(f"Prepared SNAPVERE {NEW} source contract and release documentation.")


if __name__ == "__main__":
    main()
