# Capture Engine

## Status

SNAPVERE keeps capture acquisition behind explicit service contracts. Monitor capture uses a resilient preferred/fallback path, while Window Capture uses Windows.Graphics.Capture directly because it needs a real top-level HWND capture item.

Presentation code does not call native capture APIs directly. Region and Screen workflows consume `IScreenCaptureService`; Window Capture consumes `IWindowDiscovery`, `IWindowCaptureService` and `WindowTargetPicker`.

## Monitor backend policy

```text
IScreenCaptureService
    ↓
ResilientScreenCaptureService
    ├── preferred → WindowsGraphicsCaptureService
    │               Windows.Graphics.Capture + Direct3D 11
    └── fallback  → GdiScreenCaptureService
                    BitBlt + GetDIBits
```

Fallback is deliberately narrow. Expected platform/acquisition failures such as unsupported WGC, frame timeout, COM/native errors and invalid capture state can use GDI. Caller-requested cancellation is propagated and never converted into a second capture attempt. Programming errors such as invalid arguments are not silently hidden behind fallback.

## Windows.Graphics.Capture monitor path

The monitor WGC backend:

1. requires Windows 10 build 19041 or later for deterministic cursor-control support;
2. verifies `GraphicsCaptureSession.IsSupported()`;
3. resolves the native monitor handle from physical display bounds;
4. creates a D3D11 hardware device with BGRA support;
5. projects that device to a WinRT `IDirect3DDevice`;
6. creates a monitor `GraphicsCaptureItem` through `IGraphicsCaptureItemInterop`;
7. creates a free-threaded `Direct3D11CaptureFramePool`;
8. starts capture with explicit cursor inclusion/exclusion;
9. waits up to two seconds for a frame;
10. obtains the captured `ID3D11Texture2D`;
11. copies it into a CPU-readable staging texture;
12. maps row-pitched GPU memory into a packed BGRA8 byte buffer;
13. rejects blank or dimension-mismatched frames;
14. returns a validated `CaptureFrame`.

The D3D/COM boundary is generated with Microsoft.Windows.CsWin32 plus source-generated COM interop. Native and WinRT resources are released deterministically where ownership is explicit.

## Window Capture path

Window Capture uses the same D3D11 device/readback machinery but obtains the capture item through `IGraphicsCaptureItemInterop.CreateForWindow`.

Before the picker opens, `Win32WindowDiscovery` snapshots visible capturable top-level windows in native Z-order. It filters SNAPVERE's own process, tool windows, invisible/cloaked windows, untitled windows and invalid/empty bounds. DWM extended-frame bounds are preferred so target chrome matches the actual rendered window footprint.

The picker intentionally does not call `WindowFromPoint` after its always-on-top surfaces are visible. Instead, it hit-tests the frozen Z-order snapshot geometrically. This prevents a SNAPVERE picker overlay from becoming the selected target simply because it sits above the application the user is pointing at.

For mixed-DPI and multi-monitor layouts, the picker creates one frozen borderless overlay per display. Each overlay converts between local DIPs and physical desktop pixels with that monitor's DPI. A shared target is then clipped/highlighted per monitor, including windows that span display boundaries or displays with negative desktop coordinates.

After selection, `WindowCaptureWorkflow` asks `IWindowCaptureService` for the selected HWND frame and sends the validated BGRA8 frame through the shared atomic PNG writer. Window Capture requires WGC support and therefore Windows 10 version 2004 / build 19041 or later.

## Compatibility path

`GdiScreenCaptureService` captures a physical monitor rectangle with `BitBlt`, converts it to top-down 32-bit BGRA pixels using `GetDIBits`, optionally draws the current cursor with hotspot correction, and normalizes the GDI high byte to opaque alpha.

The GDI backend is isolated and is not the intended HDR/protected-content strategy. It exists for monitor compatibility and resilient fallback. It is not used as a fake replacement for real Window Capture because a screen crop cannot reliably reproduce an occluded window.

## Supported Windows policy

SNAPVERE keeps the application minimum at Windows 10 version 1809 / build 17763.

- **17763–19040:** GDI monitor compatibility path; Window Capture is unavailable.
- **19041 and later:** WGC/D3D11 preferred for monitor acquisition with GDI fallback; WGC Window Capture available.

This avoids raising the minimum application OS solely because the newer capture paths are unavailable on earlier builds.

## CaptureFrame

Every backend produces a validated `CaptureFrame` containing:

- physical width and height;
- explicit stride;
- BGRA8 pixel buffer;
- UTC capture timestamp;
- source identifier.

WGC monitor frames use a source identifier prefixed with `wgc:` and Window Capture frames use a window-specific WGC source identifier. Validation rejects empty dimensions, invalid stride and undersized pixel buffers before downstream image work.

## Failure and cancellation semantics

`ResilientScreenCaptureService` falls back only for known monitor acquisition failures:

- `PlatformNotSupportedException`;
- `TimeoutException`;
- `COMException`;
- `Win32Exception`;
- `InvalidOperationException`.

If the caller cancellation token is cancelled, `OperationCanceledException` is propagated and the fallback backend is not called. Unexpected programming failures are also propagated rather than hidden.

Window Capture surfaces WGC/platform failures to the application instead of silently substituting a screen crop. This keeps target semantics correct.

## Cursor

Cursor inclusion is explicit per capture request. WGC uses `GraphicsCaptureSession.IsCursorCaptureEnabled` on build 19041+. GDI draws a cursor only when Windows reports it visible and its screen coordinates lie within the selected display rectangle. Current Region, Window and Screen UI entry points capture without the cursor.

## Protected content

SNAPVERE does not attempt to bypass DRM or operating-system capture restrictions. A blocked/blank WGC frame is treated as an acquisition failure. No backend is designed to defeat protected-content enforcement.

## Automated validation

CI validates WGC source generation and compilation on x64 and x86, self-contained publishing, Setup/Portable packaging and application lifecycle. Unit tests cover preferred-backend success, expected monitor fallback behavior, cancellation propagation, Window Capture workflow persistence and overlay-safe Z-order hit testing.

Installed and Portable package lifecycle tests materialize both the Region editor and Window picker WinUI surfaces on x64 and x86. The hosted Windows CI lifecycle is not considered proof of a real interactive end-user WGC screenshot; actual desktop capture still depends on Windows capture policy, GPU/desktop state and protected-content rules.
