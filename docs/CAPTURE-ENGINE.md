# Capture Engine

## Status

SNAPVERE uses an explicit capture-service contract with two Windows monitor backends. `WindowsGraphicsCaptureService` is the preferred production path on Windows 10 version 2004 / build 19041 and later. `GdiScreenCaptureService` remains the compatibility fallback and also preserves support for the application's Windows 10 1809 / build 17763 minimum.

Presentation code does not call native capture APIs directly. Screen and Region workflows consume `IScreenCaptureService` through `ResilientScreenCaptureService`.

## Backend policy

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

## Windows.Graphics.Capture path

The WGC backend:

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

## Compatibility path

`GdiScreenCaptureService` captures a physical monitor rectangle with `BitBlt`, converts it to top-down 32-bit BGRA pixels using `GetDIBits`, optionally draws the current cursor with hotspot correction, and normalizes the GDI high byte to opaque alpha.

The GDI backend is isolated and is not the intended HDR/protected-content strategy. It exists for compatibility and resilient fallback.

## Supported Windows policy

SNAPVERE keeps the application minimum at Windows 10 version 1809 / build 17763.

- **17763–19040:** GDI compatibility path.
- **19041 and later:** WGC/D3D11 preferred, GDI fallback.

This avoids raising the minimum OS solely because the WGC cursor-capture property is unavailable on earlier builds.

## CaptureFrame

Every backend produces a validated `CaptureFrame` containing:

- physical width and height;
- explicit stride;
- BGRA8 pixel buffer;
- UTC capture timestamp;
- source identifier.

WGC frames use a source identifier prefixed with `wgc:`. Validation rejects empty dimensions, invalid stride and undersized pixel buffers before downstream image work.

## Failure and cancellation semantics

`ResilientScreenCaptureService` falls back only for known acquisition failures:

- `PlatformNotSupportedException`;
- `TimeoutException`;
- `COMException`;
- `Win32Exception`;
- `InvalidOperationException`.

If the caller cancellation token is cancelled, `OperationCanceledException` is propagated and the fallback backend is not called. Unexpected programming failures are also propagated rather than hidden.

## Cursor

Cursor inclusion is explicit per capture request. WGC uses `GraphicsCaptureSession.IsCursorCaptureEnabled` on build 19041+. GDI draws a cursor only when Windows reports it visible and its screen coordinates lie within the selected display rectangle.

## Protected content

SNAPVERE does not attempt to bypass DRM or operating-system capture restrictions. A blocked/blank WGC frame is treated as an acquisition failure; where appropriate, the compatibility path may be attempted, but no backend is designed to defeat protected-content enforcement.

## Automated validation

CI validates WGC source generation and compilation on x64 and x86, self-contained publishing, Setup/Portable packaging and application lifecycle. Unit tests verify preferred-backend success, expected fallback behavior, cancellation propagation and that unexpected errors are not hidden.

The hosted Windows CI lifecycle is not considered proof of a real interactive monitor WGC screenshot. End-user desktop capture behavior still relies on the production backend policy and fallback safeguards described above.
