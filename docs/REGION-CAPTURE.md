# Region Capture

## Status

The primary-display freeze-frame Region Capture workflow is implemented in the current development branch. SNAPVERE now freezes the selected display, opens a borderless overlay over the exact physical display bounds, maps XAML pointer coordinates through the centralized DPI transformer, supports drag/move/eight-handle resize, crops pixels from the same frozen frame and persists the result through the atomic PNG writer.

The current implementation intentionally targets the primary display only. Cross-monitor region selection, smart window/control targeting, magnifier/crosshair polish and virtual-desktop freeze composition remain follow-up work and are not presented as completed features.

## Implemented workflow

```text
Region action
  ↓
hide SNAPVERE main window
  ↓
freeze primary display frame
  ↓
open borderless always-on-top overlay
  ↓
dim non-selected area
  ↓
drag / move / resize selection
  ↓
map logical XAML input → physical display pixels
  ↓
crop the same frozen frame
  ↓
encode PNG
  ↓
temp file + atomic move to Pictures\SNAPVERE
```

## Coordinate contract

Region selection geometry uses physical pixels. XAML logical coordinates never enter `RegionSelectionGeometry` directly.

`DpiCoordinateTransformer` is the central conversion layer and now supports both integer geometry and sub-DIP `double` pointer coordinates. This avoids rounding too early at 125%, 150%, 175% and 200% scaling.

The overlay converts a local XAML pointer position into a physical display-local pixel position and then adds the display's physical desktop origin. Negative desktop origins therefore remain valid.

## Selection interaction

The overlay currently supports:

- drag in any direction with normalization;
- move an existing selection by dragging its body;
- eight visible resize handles;
- one-physical-pixel minimum size;
- arrow-key movement by one physical pixel;
- `Shift` + arrow movement by 10 physical pixels;
- `Delete` to clear the active selection;
- `Enter` to save;
- double-click to save;
- `Esc` to cancel without creating a file;
- a live physical-pixel `W × H` badge.

The dimming layer is composed from four rectangles around the selection so the selected area remains an unobscured view of the frozen frame.

## Freeze frame and pixel extraction

`RegionCaptureWorkflow.PreparePrimaryDisplayAsync` captures the frame before the overlay is shown and rejects the session if the returned frame dimensions no longer match the display bounds.

`SaveSelectionAsync` maps absolute desktop coordinates back into frame-local coordinates, then calls `CaptureFrameCropper`. The cropper copies BGRA8 scanlines while respecting source stride, so padded source frames remain correct.

The overlay preview and final crop therefore originate from the same `CaptureFrame` instance.

## Persistence

Screen and Region workflows share `CaptureFileWriter`. PNG files are written to a uniquely named temporary file, flushed, and moved atomically into the final capture path. Failed writes perform best-effort temporary-file cleanup.

Default output remains:

```text
Pictures\SNAPVERE\SNAPVERE_yyyy-MM-dd_HHmmss.png
```

## Automated QA

Automated coverage now includes:

- 100%, 125%, 150%, 175% and 200% DPI scaling;
- sub-DIP pointer conversion at 125%;
- negative virtual-desktop coordinates;
- reverse drag and bounds clamping;
- body move and eight-handle resize geometry;
- minimum selection dimensions;
- padded-stride frame cropping;
- negative desktop origin → local crop mapping;
- display/frame geometry change detection;
- atomic PNG persistence and temporary-file cleanup checks.

## Remaining Region Capture work

The following is still required before the Region milestone is considered production-complete:

1. one coordinated overlay/freeze model spanning multiple monitors;
2. cross-monitor selection across mixed DPI and negative X/Y origins;
3. smart window/control targeting before manual drag;
4. magnifier and crosshair cursor polish;
5. touch/pen interaction QA;
6. portrait and rotated monitor QA;
7. high-contrast/accessibility review;
8. Windows.Graphics.Capture/D3D primary acquisition path with GDI retained only as compatibility fallback.
