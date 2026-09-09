# Capture Engine

## Status

The capture engine now has production contracts, native monitor discovery, virtual-desktop geometry, region-selection geometry and a working GDI monitor-capture compatibility backend. Windows.Graphics.Capture remains the intended primary backend and will replace the fallback for normal production capture where supported.

## Backend policy

SNAPVERE does not hide backend choice inside the UI. Capture orchestration selects an implementation of `IScreenCaptureService`; presentation code consumes the contract rather than P/Invoke details.

### Primary path — planned/in progress

`Windows.Graphics.Capture` + Direct3D 11 frame acquisition, optimized for modern Windows 11 capture, HDR-aware processing and efficient GPU resource lifetime.

### Compatibility fallback — implemented

`GdiScreenCaptureService` captures a physical monitor rectangle with `BitBlt`, converts it to top-down 32-bit BGRA pixels using `GetDIBits`, optionally draws the current cursor with hotspot correction, and normalizes the GDI high byte to opaque alpha.

The GDI backend is deliberately isolated. It is not the long-term HDR or protected-content strategy.

## CaptureFrame

Every backend produces a validated `CaptureFrame`:

- physical width/height
- explicit stride
- BGRA8 pixel buffer
- capture timestamp
- source identifier

Validation rejects empty dimensions, invalid stride and undersized pixel buffers before downstream use.

## Resource ownership

The compatibility backend restores the previously selected GDI object and releases/deletes bitmap, memory DC, screen DC and cursor icon bitmaps in `finally` blocks. Native resource lifetime is not delegated to garbage collection.

## Cursor

Cursor inclusion is explicit per capture request. The fallback backend draws a cursor only when Windows reports it visible and its screen coordinates lie within the selected display rectangle.

## Protected content

No backend may attempt to bypass DRM or OS capture restrictions. A future capture-orchestration layer will translate protected/blocked capture failures into user-facing messages rather than raw native error codes.
