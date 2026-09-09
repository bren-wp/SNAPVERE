# SNAPVERE Architecture

## Status

Foundation implemented. Capture backends, editor, OCR, history and scrolling subsystems are progressively added behind explicit contracts; this document does not mark planned functionality as complete.

## Design goals

SNAPVERE prioritizes capture latency, pixel accuracy, mixed-DPI correctness, deterministic resource cleanup, local-first privacy and a small understandable dependency surface.

## Project boundaries

### Snapvere.App
WinUI 3 composition root and presentation layer. It owns windows, navigation and view wiring, but must not own capture algorithms, image encoding or editor rasterization.

### Snapvere.Domain
Stable value objects and product-domain types. Pixel geometry is represented explicitly to avoid accidental mixing of logical and physical coordinates.

### Snapvere.Capture
Capture orchestration, display/window discovery, DPI transforms, Windows.Graphics.Capture integration, freeze frames and selection geometry.

### Snapvere.Imaging
Encoding, cropping, pixel transforms, color-space handling and export pipeline. UI-independent by design.

### Snapvere.Shared
Small cross-cutting primitives only. It must not become a dumping ground for unrelated helpers.

## Capture pipeline

```text
Hotkey / UI command
        ↓
Capture request
        ↓
Display/window discovery
        ↓
Windows capture backend
        ↓
CaptureFrame (BGRA8 + physical pixel geometry)
        ↓
Image pipeline
        ├── Clipboard
        ├── Save
        ├── Editor
        ├── OCR
        └── Pin
```

## Coordinate systems

All capture boundaries are ultimately expressed in physical pixels. Presentation may use logical pixels, but every boundary crossing must use an explicit DPI transform. Virtual desktop coordinates may be negative and no code may assume the primary monitor starts at the virtual origin.

The initial `DpiCoordinateTransformer` has tests for 100%, 125%, 150%, 200%, mixed-axis DPI and negative coordinates. Native monitor discovery will attach per-monitor DPI to each `DisplayDescriptor`.

## Capture frame contract

`CaptureFrame` currently represents a validated BGRA8 buffer with dimensions, stride, timestamp and optional source id. Validation rejects empty dimensions, undersized stride and undersized buffers before the frame enters downstream processing.

## Resource lifetime

Windows.Graphics.Capture, Direct3D devices, frame pools, textures, streams and temporary bitmaps must have deterministic ownership. Long-lived tray infrastructure may cache lightweight services but must not retain unnecessary full-resolution frames.

## Threading

UI thread work is limited to presentation and Windows APIs that require it. Capture acquisition, encoding, OCR, history thumbnails and update operations must use asynchronous/cancellable boundaries where safe.

## Security and privacy boundaries

Screenshot pixels, OCR text, clipboard contents and file contents are sensitive. They are not valid structured-log payloads. Temporary files must be scoped and cleaned. Network operations for licensing/update services are architecturally separate from user image data.

## Planned next implementation

1. Native monitor enumeration with per-monitor DPI
2. Virtual desktop coordinate map
3. Windows.Graphics.Capture backend
4. Full-screen/monitor capture command
5. Freeze-frame region selection overlay
6. Clipboard and PNG encoding
7. Hotkey and tray integration
