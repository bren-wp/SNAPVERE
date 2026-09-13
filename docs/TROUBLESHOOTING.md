# SNAPVERE Troubleshooting

This guide applies to the current **SNAPVERE 0.1.1** product line. Start with the platform section that matches the package you are using.

## Before troubleshooting

1. Confirm you downloaded the package from the official v0.1.1 release: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1
2. Confirm the package name matches your platform.
3. Restart the affected application/browser once before changing system settings.
4. Do not disable OS/browser security protections just to make capture work.

## Windows

### SNAPVERE appears to do nothing after launch

Normal startup is tray-first. Check the Windows notification area and its overflow panel. The application is designed not to keep a launcher window open.

If the process exits instead of remaining in the tray, inspect:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

The startup log is intended for diagnostic state, not screenshot pixel data.

### Print Screen or a configured hotkey does not work

Another application can own the same global hotkey. Open the SNAPVERE tray menu and try the corresponding capture command directly. If the menu action works but the hotkey does not, resolve the hotkey conflict in the competing application or in SNAPVERE settings.

### Window capture cannot capture a specific app

Some windows are protected, minimized, composed with restricted overlays, destroyed during selection or otherwise unavailable to Windows.Graphics.Capture. Try bringing the target window to the foreground and keeping it visible. Do not assume a protected application can be bypassed.

### Screen/region output is wrong on mixed-DPI monitors

SNAPVERE contains physical-pixel/DPI conversion and virtual-desktop handling, but unusual driver/scaling combinations can still expose platform-specific behavior. Reproduce with the target monitor set as primary, record each monitor's scaling value and include that information in a support report.

### Setup is blocked by Windows reputation/security UI

The project does not claim a commercial Authenticode reputation/signing identity for the public executable. Verify that the file came from the official GitHub release and compare the SHA-256 digest shown by GitHub before deciding whether to run it. Do not disable Microsoft Defender or SmartScreen globally.

### Portable extraction/cache problem

Close every SNAPVERE process and run the Portable package again. The Portable host validates its embedded architecture payload and rebuilds its cache transactionally when required. Persistent failure should be reported with the startup log and Windows architecture/build number.

## Android

### Capture button returns to Ready without a screenshot

Every capture needs a fresh Android MediaProjection approval. If the system prompt is cancelled, no capture is performed. Start a new capture and approve the prompt.

### Capture reports that SNAPVERE could not hide itself

The capture service intentionally waits until the SNAPVERE task is no longer visible so the app UI is not captured. If the task cannot be moved behind the previous content, the capture is cancelled rather than silently saving the SNAPVERE screen.

### Capture times out

The app uses bounded task-hide and first-frame timeouts. OEM graphics/display behavior, screen transitions or system restrictions can prevent a usable frame from arriving. Retry from a stable foreground screen. If repeatable, include device model, Android version and display mode in the report.

### Open / Share / Delete is disabled

Those actions require the latest stored MediaStore URI to remain readable. If Android or another app removed the image, SNAPVERE clears the stale latest-capture reference.

### APK cannot update an older/newer installed build

The public v0.1.1 APK uses the validated CI/debug signing identity. A package signed with a different identity cannot be installed as an in-place update. Uninstalling removes the installed application package/settings; screenshots already stored in the system Pictures collection are separate MediaStore files.

### Network/privacy question

The Android manifest intentionally does not request `android.permission.INTERNET`. Website, support, privacy, terms, Open and Share actions are explicit user actions delegated to Android/another chosen app.

## Browser extensions

### The extension cannot capture a browser-internal page

Browsers restrict script injection/capture on internal and privileged pages such as settings, extension stores and some built-in viewers. SNAPVERE reports a controlled unsupported-page error rather than requesting broad host access to bypass those restrictions.

### Full-page capture says the page is too large

The extension deliberately limits tile count, canvas dimensions and total pixel count. These bounds prevent unbounded memory use. Capture a smaller region or the visible viewport instead.

### Full-page output duplicates or misses sticky/dynamic content

Full-page capture uses controlled scrolling and stitching. Highly dynamic layouts, animations, lazy loading, video, canvas content, sticky components and cross-origin frames can change while capture is in progress. Stabilize the page when possible or use visible/region capture.

### “Ask where to save” does not affect full-page capture

In 0.1.1 that option intentionally applies to visible and region captures. Full-page stitching is completed inside the content-script page context and initiates a local download from the generated Blob.

### Region selection does not start

Injection is blocked on privileged pages. On ordinary pages, reload the page and try again. If the extension was updated while the page was already open, a reload can also ensure the current extension context is active.

### Manual installation warning

The GitHub browser ZIPs are not evidence of Chrome Web Store, Edge Add-ons, Opera Add-ons or Mozilla Add-ons approval. Until those external stores are actually published, use the documented developer-mode/manual installation path if you choose to test the packages.

## What to include in a support report

- SNAPVERE version (`0.1.1` for the current public release);
- platform, OS/browser version and architecture;
- exact capture mode;
- exact error/status text;
- steps that reproduce the issue;
- whether the issue reproduces after restart;
- Windows startup log when relevant;
- monitor count/scaling for desktop capture issues;
- Android device model/version for Android issues;
- page type/URL category (ordinary page vs privileged browser page) for extension issues.

Do not send sensitive screenshots unless they are necessary to explain the issue and you intentionally choose to share them.

Support: **info@snapvere.com**
