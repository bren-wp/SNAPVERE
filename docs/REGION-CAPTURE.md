# Region Capture

## Status

Region Capture is implemented for the primary display with a frozen-frame selection/editor workflow. SNAPVERE freezes the display before showing the overlay, maps WinUI pointer coordinates into physical pixels, supports drag/move/eight-handle resize, provides inline annotation tools, and finishes through Copy or Save.

The primary tray-first entry points are:

- **left-click the SNAPVERE tray icon** → Region Capture immediately;
- **Print Screen** → Region Capture when Windows allows registration;
- `Ctrl+Shift+1` → independent Region fallback.

The normal application launch does not open a capture dashboard.

Current Region Capture intentionally targets one display per capture. Coordinated cross-monitor freeze composition and mixed-DPI selection spanning multiple monitors remain follow-up work.

## Workflow

```text
tray left-click / Print Screen / Ctrl+Shift+1
  ↓
keep SNAPVERE coordinator hidden
  ↓
freeze primary display frame
  ↓
open borderless always-on-top Region editor
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

`DpiCoordinateTransformer` converts sub-DIP pointer coordinates to physical display coordinates before geometry operations. This avoids premature rounding at common scaling factors including 125%, 150%, 175% and 200%.

The overlay adds the display's physical desktop origin after local conversion, so a primary display positioned at a negative virtual-desktop coordinate remains valid.

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

The dimming layer is composed around the selected region so active pixels remain visible while the surrounding frozen desktop is darkened.

## Inline annotation

After selecting a region, the same overlay provides lightweight markup without opening a standalone editor:

- Move;
- Pen;
- Line;
- Arrow;
- Box/rectangle;
- Highlight;
- selectable annotation colors;
- Undo.

Annotations are not presentation-only decorations. `CaptureFrameAnnotator` applies them to the selected frozen image before Copy or Save so the resulting clipboard image/PNG contains the edits.

Text, ellipse, blur/pixelate and numbered-step annotations are intentionally not shown as completed tools because they are not yet production-implemented.

## Keyboard behavior

Implemented keyboard actions include:

- `Ctrl+Z` → Undo annotation;
- `Ctrl+C` → Copy selected result;
- `Enter` → Save selected result;
- `Esc` → cancel/close;
- Arrow / Shift+Arrow → move selection by physical pixels;
- Delete → clear selection.

The application-level global shortcuts are independent from editor shortcuts.

## Copy and Save

**Copy** renders the selected image plus annotations and places the result on the Windows clipboard. It does not create a PNG file.

**Save** renders the same result and persists it through `CaptureFileWriter`, which writes to a temporary/staging path and atomically moves it to the final collision-safe path.

Default output:

```text
Pictures\SNAPVERE\SNAPVERE_yyyy-MM-dd_HHmmss.png
```

## Cursor preference

Options / Preferences exposes a real **Include cursor on capture** setting. The preference is stored locally in `%LOCALAPPDATA%\SNAPVERE\settings.json` and is consumed by `RegionCaptureWorkflow` before the frozen monitor frame is acquired.

On supported WGC builds, cursor state is applied through the capture session. On the GDI fallback path, the cursor is drawn only when Windows reports it visible and it lies inside the captured display.

## Capture backend

Region preparation consumes `IScreenCaptureService`. On Windows 10 build 19041 and later, the production service prefers `WindowsGraphicsCaptureService` using Windows.Graphics.Capture and Direct3D 11. Expected supported fallback failures can use `GdiScreenCaptureService` through `ResilientScreenCaptureService`.

Caller cancellation does not trigger a second fallback capture. On supported Windows builds older than 19041, GDI remains the monitor compatibility path without raising the application minimum above Windows 10 1809 / build 17763.

## Runtime stability

The Region editor visual tree is created programmatically. This avoids dependence on a secondary Window XAML resource path that has previously been less stable in packaged runtime materialization.

A templated `ProgressRing` status element is intentionally not used because earlier runtime QA showed it could terminate WinUI on the CI Windows Server environment. Status feedback uses simpler surfaces that pass installed/Portable runtime probes.

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
- annotation rendering into the result frame;
- atomic PNG persistence and temporary-file cleanup;
- WGC preferred/fallback orchestration and cancellation behavior;
- x64/x86 Installed Region-editor materialization;
- x64/x86 Portable Region-editor materialization.

The hosted runtime probe validates that the Region editor materializes in packaged builds. It is not treated as proof of capturing arbitrary protected end-user content.

## Remaining Region work

- coordinated freeze/overlay composition spanning multiple monitors;
- cross-monitor selection across mixed DPI and negative origins;
- magnifier/crosshair polish;
- text, ellipse, blur/pixelate and numbered-step tools;
- adjustable annotation thickness UI;
- touch/pen interaction QA;
- portrait/rotated monitor QA;
- broader high-contrast/accessibility review.
