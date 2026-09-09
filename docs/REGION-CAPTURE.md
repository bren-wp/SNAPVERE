# Region Capture

## Status

Selection geometry foundation is implemented and tested. Freeze-frame acquisition, overlay rendering, pointer integration and final pixel extraction are the next implementation steps.

## Required workflow

```text
Hotkey
  ↓
Freeze current virtual desktop frame
  ↓
Dim non-selected area
  ↓
Pointer / smart-window target
  ↓
Drag or select region
  ↓
Move / resize / keyboard fine adjustment
  ↓
Capture selected physical-pixel rectangle
```

## Coordinate contract

Region selection geometry uses physical pixels. The overlay may render in XAML logical units, but pointer positions must be transformed explicitly before entering `RegionSelectionGeometry`.

This prevents mixed-DPI drift when a selection crosses monitor boundaries.

## Implemented geometry operations

`RegionSelectionGeometry` currently supports:

- reverse-direction drag normalization
- clamp to the full virtual desktop
- moving a selection without changing its size
- single-pixel fine movement
- eight resize handles
- minimum width/height enforcement
- negative virtual-desktop coordinates

`SelectionHandle.Body` maps resize input to a move operation, allowing the overlay controller to use one interaction contract for drag/move/resize.

## Overlay responsibilities

The overlay presentation layer must not duplicate geometry calculations. It should:

1. transform pointer input to physical pixels;
2. call the region geometry engine;
3. render the returned rectangle;
4. display dimensions/coordinates;
5. commit or cancel the selection.

## Freeze frame

Before selection begins, SNAPVERE must capture a stable desktop frame. The visual shown during selection and the pixels ultimately cropped must come from the same frozen capture set where practical, preventing animated content from changing between user selection and final output.

## Keyboard behavior

Planned overlay behavior:

- Arrow keys: move selection by 1 physical pixel
- Shift + Arrow: move by a larger configurable step
- handle-focused Arrow keys: resize by 1 physical pixel
- Enter: capture
- Escape: cancel

## QA

Automated tests cover reverse drag, negative-coordinate clamping, boundary-limited movement, handle-specific resize, minimum size and one-physical-pixel adjustment.

Manual QA must additionally cover pointer capture, cross-monitor drag, 100/125/150/175/200% mixed scaling, portrait displays and high-contrast rendering.
