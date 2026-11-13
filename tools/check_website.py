#!/usr/bin/env python3
"""Snapvere website release checks. Only Python standard library is required."""
from __future__ import annotations

from hashlib import sha256
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urlparse

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "site"
EXPECTED = {
    "Snapvere-Portable.exe",
    "Snapvere-Setup.exe",
    "Snapvere-Setup.msi",
}
FORBIDDEN_PUBLIC_DOMAINS = ("github.com", "ko-fi.com", "paypal.com", "paypal.me")


class Links(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self.urls: list[tuple[str, str]] = []

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        attrs_dict = dict(attrs)
        for key in ("href", "src"):
            value = attrs_dict.get(key)
            if value:
                self.urls.append((tag, value))


def verify_site() -> None:
    html_files = sorted(SITE.rglob("*.html"))
    assert len(html_files) >= 4, "Expected home, download, privacy and license pages"

    for page in html_files:
        text = page.read_text(encoding="utf-8")
        parser = Links()
        parser.feed(text)
        for tag, url in parser.urls:
            host = urlparse(url).hostname
            if host and any(host == name or host.endswith("." + name) for name in FORBIDDEN_PUBLIC_DOMAINS):
                raise AssertionError(f"External public link in {page}: {url}")
            if tag in ("img", "script") and url.startswith("/"):
                asset = SITE / url.lstrip("/").split("?")[0]
                if not asset.is_file():
                    raise AssertionError(f"Missing linked static asset in {page}: {url}")

    for path in SITE.rglob("*"):
        if path.suffix not in {".html", ".js", ".css"}:
            continue
        text = path.read_text(encoding="utf-8").lower()
        for domain in FORBIDDEN_PUBLIC_DOMAINS:
            if domain in text:
                raise AssertionError(f"Unexpected third-party promotional URL in {path}: {domain}")
        for placeholder in ("support-links", "data-support-link", "private upload coming"):
            if placeholder in text:
                raise AssertionError(f"Unused feature left in {path}: {placeholder}")

    releases = SITE / "files"
    checksum_file = releases / "SHA256SUMS.txt"
    assert checksum_file.is_file(), "Release checksum file missing"
    recorded: dict[str, str] = {}
    for line in checksum_file.read_text(encoding="ascii").splitlines():
        if not line.strip():
            continue
        expected_hash, name = line.split(maxsplit=1)
        name = name.lstrip("*").strip()
        if name not in EXPECTED:
            raise AssertionError(f"Unexpected checksummed file: {name}")
        recorded[name] = expected_hash.lower()
    assert set(recorded) == EXPECTED, f"SHA256 manifest does not match required packages: {set(recorded)}"

    for name in EXPECTED:
        file = releases / name
        assert file.is_file(), f"Missing first-party download: {name}"
        assert file.stat().st_size > 1_000_000, f"Invalid download size: {name}"
        actual_hash = sha256(file.read_bytes()).hexdigest()
        assert recorded[name] == actual_hash, f"Invalid SHA256 for {name}"

    config = (SITE / "config.js").read_text(encoding="utf-8")
    for name in EXPECTED | {"SHA256SUMS.txt"}:
        assert f"/files/{name}" in config, f"Release link missing in config: {name}"

    homepage = (SITE / "index.html").read_text(encoding="utf-8")
    assert '/screenshots/home.png' in homepage
    assert '/screenshots/settings.png' in homepage
    assert "Brendigo" in homepage and "Snapvere" in homepage
    assert "snapvere.com" in homepage
    print(f"Snapvere site verified: {len(html_files)} pages, all 3 packages + checksums, 0 forbidden public links")


if __name__ == "__main__":
    verify_site()
