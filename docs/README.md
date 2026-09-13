# SNAPVERE Documentation

This directory is the documentation hub for the current **SNAPVERE 0.1.1** product line. It describes the implemented Windows application, native Android application, browser extensions, packaging, privacy model, QA evidence and release process.

For the product overview and downloads, start with the [main README](../README.md). Croatian documentation is indexed in [`docs/hr/README.md`](hr/README.md).

## Start here

- [User Guide](USER-GUIDE.md) — everyday Windows, Android and browser workflows.
- [Installation](INSTALLATION.md) — Setup, Portable, APK and unpacked browser-extension installation.
- [Troubleshooting](TROUBLESHOOTING.md) — common capture, installation and browser limitations.
- [Product Status](PRODUCT-STATUS.md) — what is shipped, validated and still externally pending.
- [Privacy](PRIVACY.md) — platform-by-platform local-first data behavior.
- [QA Matrix](QA-MATRIX.md) — what CI proves and what it does not prove.
- [Versioning & Releases](VERSIONING-RELEASES.md) — canonical version contract and immutable release policy.
- [Complete Release History](../RELEASES.md) — one detailed canonical history for all releases.

## Windows application

- [Architecture](ARCHITECTURE.md)
- [Capture Engine](CAPTURE-ENGINE.md)
- [Region Capture](REGION-CAPTURE.md)
- [Window Capture](WINDOW-CAPTURE.md)
- [Multi-monitor & DPI](MULTI-MONITOR.md)
- [Image Pipeline](IMAGE-PIPELINE.md)
- [Tray UX](TRAY-UX.md)
- [Settings](SETTINGS.md)
- [Visual QA](VISUAL-QA.md)
- [Visual QA Render Readiness](VISUAL-QA-RENDER-READINESS.md)

## Android

- [Android Architecture & QA](ANDROID.md)
- [Android Source/Build Guide](../android/README.md)

## Browser extensions

- [Browser Extensions](BROWSER-EXTENSIONS.md)
- [Extension Source Guide](../ekstenzije/README.md)
- [Browser Extension Privacy Policy](../ekstenzije/PRIVACY.md)

## Release, security and branding

- [Complete Release History](../RELEASES.md)
- [SNAPVERE 0.1.1 Release Contract](RELEASE-0.1.1.md)
- [SNAPVERE 0.1.1 Security & Performance Evidence](SECURITY-PERFORMANCE-0.1.1.md)
- [Security Policy](../SECURITY.md)
- [Branding](BRANDING.md)
- [About & Support Links](ABOUT-SUPPORT-LINKS.md)
- [Changelog](../CHANGELOG.md)

## Canonical version and release sources

The machine-readable source of truth is [`product-version.json`](../product-version.json). Detailed human-readable release notes and history live in [`RELEASES.md`](../RELEASES.md). `CHANGELOG.md` remains the concise engineering summary.

`eng/validate-product-contract.py` and **Product Contract CI** verify that the active documentation, .NET version, Android version, browser manifests/store metadata, canonical release history and the eight-file public release contract remain aligned.

Historical sections in `RELEASES.md` preserve what was true at each release; they are not rewritten to pretend older releases had current features.

## Support

Official product site: **https://snapvere.com**  
Support: **info@snapvere.com**  
Developer and publisher: **Brendigo** — https://brendigo.com
