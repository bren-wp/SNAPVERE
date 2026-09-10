# Window Capture

## Status

Window Capture is implemented as a real top-level window selection and Windows.Graphics.Capture (WGC) workflow. It does not emulate Window Capture by cropping a monitor screenshot.

Entry points:

- right-click tray → **Capture Window**;
- `Ctrl+Shift+2` global shortcut.

The feature requires the WGC Window Capture path used by SNAPVERE on Windows 10 version 2004 / build 19041 or later. On older supported Windows versions, Window Capture is unavailable rather than silently changing semantics.

## Workflow

```text
Capture Window command
    ↓
snapshot active displays
    ↓
snapshot capturable top-level windows in native Z-order
    ↓
freeze desktop frame for each monitor
    ↓
create one borderless picker overlay per monitor
    ↓
track pointer against frozen Z-order geometry
    ↓
highlight complete target across monitor boundaries
    ↓
left click → select HWND
Esc → cancel
    ↓
WindowsGraphicsCaptureService.CreateForWindow
    ↓
CaptureFrame validation
    ↓
CaptureFileWriter → Pictures\SNAPVERE
```

## Window discovery

`Win32WindowDiscovery` enumerates top-level windows in native Z-order before SNAPVERE picker overlays exist.

Targets unsuitable for user Window Capture are filtered, including:

- SNAPVERE's own process/windows;
- invisible windows;
- cloaked windows;
- tool/system-style windows covered by the discovery policy;
- windows without usable visible bounds/title;
- invalid or empty rectangles.

DWM extended-frame bounds are preferred so the picker aligns with the rendered window footprint instead of relying only on older client/window rectangle behavior.

## Overlay-safe targeting

Once SNAPVERE's always-on-top picker overlays are visible, asking Windows for the HWND under the cursor would naturally find SNAPVERE itself. The picker therefore does **not** depend on `WindowFromPoint` during interactive hover.

Instead:

1. the candidate list is frozen before overlays;
2. candidates retain native Z-order;
3. current pointer location is mapped to physical desktop coordinates;
4. the first candidate whose frozen physical bounds contain that point becomes the target.

This preserves the intended target even though SNAPVERE owns the visible topmost picker surfaces.

## Multi-monitor behavior

`WindowTargetPicker` creates one frozen picker surface per display.

Each overlay:

- displays its own frozen monitor frame;
- uses that monitor's DPI to convert local WinUI coordinates;
- participates in a shared selected-target state;
- clips the selected target frame to its local display bounds.

A window spanning two monitors is therefore represented as one shared HWND target with highlight segments on every intersected overlay.

Negative virtual desktop X/Y coordinates are valid. The primary monitor is not assumed to start at `(0, 0)`.

## Frozen desktop

Desktop frames are acquired before picker overlays appear. This gives the user a stable visual target surface and prevents movement beneath the pointer from continuously changing the selection context during the pick.

Picker background frames intentionally exclude the cursor. Cursor preference applies to the final captured window image, not to the temporary targeting surface.

## Final capture

After selection, `WindowCaptureWorkflow` requests the selected HWND through `IWindowCaptureService`. The production service uses `WindowsGraphicsCaptureService.CreateForWindow`, D3D11 frame acquisition and CPU readback to produce a validated BGRA8 `CaptureFrame`.

The frame is then saved as PNG through the same atomic `CaptureFileWriter` used by other durable capture paths and becomes visible in Recent Captures.

## Cursor preference

Options / Preferences exposes **Include cursor on capture**. When enabled, the Window workflow requests cursor inclusion on the final WGC session where supported.

This setting is stored locally and does not alter candidate discovery or frozen picker screenshots.

## Cancellation and failure semantics

- Esc cancels the picker without saving a file.
- caller cancellation is propagated;
- WGC/platform failures are surfaced to the application;
- Window Capture does not fall back to an arbitrary screen crop, because that could capture occlusion or unrelated content and violate the selected-window contract.

## Runtime stability

The picker UI is programmatic WinUI and shares target state across its per-monitor overlays. It avoids secondary templated resource paths known to have caused runtime instability in earlier overlay work.

Every overlay is closed in cleanup even when selection is cancelled or an exception occurs.

## Automated QA

Coverage includes:

- native window filtering;
- Z-order target ordering;
- overlay-safe geometric hit testing;
- DWM/physical bounds handling;
- negative coordinates and mixed-DPI transforms;
- targets crossing monitor boundaries;
- Window Capture workflow persistence;
- x64/x86 Installed picker materialization;
- x64/x86 Portable picker materialization;
- release package lifecycle probes emitting `WINDOW_OVERLAY_READY`.

The hosted CI probe validates picker materialization. Actual capture of arbitrary desktop applications still depends on the real interactive Windows desktop, GPU state and Windows capture policy.

## Deliberately deferred

- automatic inline Region-style annotation editor immediately after Window Capture;
- advanced child-control/UI Automation targeting;
- protected-content bypasses (not planned);
- product options that have not been implemented and release-tested.
