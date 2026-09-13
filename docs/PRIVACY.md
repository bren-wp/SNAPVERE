# SNAPVERE Privacy

This document describes the implemented first-party privacy behavior of **SNAPVERE 0.1.1**. It is technical product documentation, not a substitute for any jurisdiction-specific legal notice published on the official website.

## Product principle

SNAPVERE is designed as a **local-first capture tool**. Capturing and encoding screenshots does not require a SNAPVERE account, first-party cloud upload, analytics pipeline or advertising SDK in the current Windows, Android and browser implementations.

## Windows

Windows captures are acquired and encoded locally. The default destination is the user's `Pictures\SNAPVERE` directory. Save As is an explicit user action through the native file picker.

The Windows capture runtime does not include a first-party telemetry uploader or automatic screenshot cloud synchronization worker. Startup diagnostics can be written to `%LOCALAPPDATA%\SNAPVERE\Logs\startup.log`; screenshot pixel data is not intentionally logged there.

Opening, copying, saving or later sharing a generated file through Windows or another application is controlled by the user and may involve software outside SNAPVERE.

## Android

The Android manifest intentionally does **not** request `android.permission.INTERNET`. The application uses Android MediaProjection for user-approved one-shot screen capture and MediaStore for local PNG storage in `Pictures/SNAPVERE`.

For every capture, Android presents a fresh system MediaProjection consent flow. SNAPVERE does not cache or reuse the consent token for silent future captures.

The app stores only lightweight local preferences needed to identify the latest capture URI/name for Open/Share/Delete UI. If that URI becomes unreadable, the stale reference is removed.

Explicit Website, Support, Privacy, Terms, Open and Share actions can launch other Android applications. Once the user chooses an external application, that application's own privacy behavior applies.

Android application backup and cleartext traffic are disabled in the current manifest, and the capture foreground service is non-exported.

## Browser extensions

The Chrome, Edge, Opera and Firefox packages process screenshot pixels locally. They do not include a first-party network uploader, analytics SDK, telemetry SDK, ad SDK or remote runtime script.

The current permission set is exactly:

- `activeTab` — access the user-selected active tab when capture is requested;
- `scripting` — inject the local capture script for region/full-page workflows;
- `downloads` — initiate PNG downloads for visible/region capture;
- `storage` — persist local capture settings and bounded capture-session state.

The extensions do not request `<all_urls>` or a broad `host_permissions` grant. Privileged browser pages can therefore remain unavailable for capture.

Full-page capture temporarily stores captured tile image objects in the page's extension content-script context, assembles the final image locally and releases temporary state during cleanup/watchdog handling.

The browser-specific policy used for store preparation is [Browser Extension Privacy Policy](../ekstenzije/PRIVACY.md).

## Data SNAPVERE does not require for core capture

The implemented product does not require a SNAPVERE account, payment identity, advertising identifier, contact list, location, microphone, camera or first-party cloud storage for its core screenshot capture workflows.

## External services and links

The application/documentation can link to `snapvere.com`, Brendigo, GitHub releases or the user's email/browser/share applications. Visiting or sending data to an external service is outside the local screenshot-processing boundary and is governed by that service.

## Release and signing transparency

The public Android v0.1.1 APK is CI/debug-signed, not represented as a Google Play production-signed package. Browser ZIPs are published on GitHub but are not represented as approved browser-store listings until actual external publisher review/signing occurs.

## Security reports

Do not disclose a suspected vulnerability publicly before reviewing [SECURITY.md](../SECURITY.md). For ordinary product support use **info@snapvere.com**.
