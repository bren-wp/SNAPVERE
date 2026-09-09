# Region Capture

## Status

Region Capture is implemented for the primary display with a frozen-frame selection/editor workflow. SNAPVERE freezes the display before showing the overlay, maps WinUI pointer coordinates into physical pixels, supports drag/move/eight-handle resize, provides inline annotation tools, and finishes through Copy or Save.

`Print Screen` is the preferred global Region shortcut. `Ctrl+Shift+1` is registered independently as a fallback so a Windows or third-party Print Screen conflict does not remove keyboard access to Region Capture.

The current implementation intentionally targets one display per capture. Coordinated cross-monitor freeze composition and mixed-DPI selection spanning multiple monitors remain follow-up work.

## Workflow

```text
Print Screen / Ctrl+Shift+1 / tray / launcher
  ↓
hide SNAPVERE launcher when visible
  ↓
freeze primary display frame
  ↓
open borderless always-on-top editor
  ↓
drag / move / resize physical-pixel selection
  ↓
optional Pen / Line / Arrow / Box / Highlight annotations
  ↓
Copy → Windows clipboard
or
Save → PNG → atomic move to Pictures\SNAPVERE
```

The preview, crop and annotation render all originate from the same frozen `CaptureFrame`; the desktop is not recaptured after selection.

## Coordinate contract

Region geometry uses physical pixels. WinUI logical coordinates never enter `RegionSelectionGeometry` directly.

`DpiCoordinateTransformer` converts sub-DIP `double` pointer coordinates to physical display coordinates before geometry operations. This avoids premature rounding at common scaling factors including 125%, 150%, 175% and 200%.

The overlay adds the display's physical desktop origin after local conversion, so negative virtual-desktop coordinates remain valid.

## Selection interaction

The editor supports:

- drag in any direction with normalization;
- move an existing selection by dragging its body;
- eight resize handles;
- one-physical-pixel minimum size;
- Arrow movement by 1 physical pixel;
- Shift+Arrow movement by 10 physical pixels;
- Delete to clear the selection;
- a live physical-pixel `W × H` badge;
- Enter and double-click to save;
- Esc to cancel without creating a file.

The dimming layer is composed from four rectangles around the selected region so the active pixels remain unobscured.

## Inline annotation

After selecting a region, the same overlay provides lightweight markup without opening a separate editor window:

- Pen for freehand strokes;
- Line;
- Arrow;
- Box/rectangle;
- Highlight;
- selectable annotation color;
- Undo for annotation operations.

Annotations are rendered against the selected frozen image for final Copy or Save output. Selection geometry remains independent from annotation geometry so editing tools do not alter the capture rectangle.

## Copy and Save

**Copy** renders the selected image plus annotations and places the result on the Windows clipboard. It does not create a PNG file.

**Save** renders the same result and persists it through `CaptureFileWriter`, which writes a temporary PNG, flushes it and atomically moves it to the final collision-safe path.

Default output remains:

```text
Pictures\SNAPVERE\SNAPVERE_yyyy-MM-dd_HHmmss.png
```

## Capture backend

Region preparation consumes `IScreenCaptureService`. On Windows 10 2004 / build 19041 and later, the production service prefers `WindowsGraphicsCaptureService`, which uses Windows.Graphics.Capture and Direct3D 11. Expected unsupported/native acquisition failures fall back to `GdiScreenCaptureService` through `ResilientScreenCaptureService`.

On supported Windows builds older than 19041, the GDI compatibility backend remains available without raising the minimum application OS above Windows 10 1809 / build 17763.

## Runtime stability

The Region editor visual tree is created programmatically. This avoids dependence on a secondary Window XAML resource at runtime. CI also launches the actual editor in installed and Portable packages on x64 and x86 and waits for its root surface to load.

A previous templated `ProgressRing` status element was removed after runtime QA showed it could terminate WinUI during editor materialization on the CI Windows Server environment. Status feedback now uses a simpler text surface that passes the installed/Portable runtime probe.

## Automated QA

Automated coverage includes:

- 100%, 125%, 150%, 175% and 200% DPI scaling;
- sub-DIP pointer conversion;
- negative virtual-desktop coordinates;
- reverse drag and bounds clamping;
- body move and eight-handle resize geometry;
- minimum selection dimensions;
- padded-stride BGRA8 cropping;
- negative desktop origin → local crop mapping;
- display/frame geometry-change detection;
- atomic PNG persistence and temporary-file cleanup;
- WGC preferred/fallback orchestration and cancellation behavior;
- x64/x86 installed Region-editor materialization;
- x64/x86 Portable Region-editor materialization.

The hosted CI runtime probe validates editor startup, not a real interactive end-user WGC screenshot.

## Remaining Region work

The following remains intentionally deferred:

1. coordinated freeze/overlay composition spanning multiple monitors;
2. cross-monitor selection across mixed DPI and negative X/Y origins;
3. smart window/control targeting;
4. magnifier and crosshair polish;
5. richer editor tools such as text, blur/pixelate and numbered steps;
6. touch/pen interaction QA;
7. portrait and rotated monitor QA;
8. high-contrast/accessibility review.
