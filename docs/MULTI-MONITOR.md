# Multi-monitor and virtual desktop behavior

## Status

SNAPVERE now uses the Windows virtual desktop as a physical-pixel coordinate space across display discovery and Window Capture. The Window picker creates one frozen overlay per monitor and supports mixed-DPI layouts, negative coordinates and windows spanning display boundaries.

Region Capture is intentionally still single-display: it freezes the primary display and constrains selection to that frame. Coordinated cross-monitor Region composition is not exposed as a completed feature.

Screen Capture currently captures the primary display. Monitor-under-cursor and all-monitors options remain deferred until implemented and release-gated.

## Display model

Each `DisplayDescriptor` records:

- physical monitor bounds;
- work area;
- effective DPI X/Y;
- primary-display flag;
- stable Windows device identifier when available.

`Win32DisplayDiscovery` uses `EnumDisplayMonitors` and `GetMonitorInfoW`. Effective monitor DPI is obtained through Windows DPI APIs with a conservative 96-DPI fallback when discovery is unavailable for a specific monitor.

## Virtual desktop rules

The primary monitor is never assumed to begin at `(0, 0)`. A display may sit left of or above primary and therefore use negative X/Y coordinates.

```text
          [ portrait monitor ]
          y = -1600
                 │
[ left ] ───── [ primary ]
 x = -1920       x = 0
```

`VirtualDesktopLayout.GetBounds` computes the union of monitor rectangles without assuming resolution, ordering or primary anchoring.

Monitor hit testing treats left/top edges as inclusive and right/bottom edges as exclusive so one physical boundary pixel does not belong to two adjacent displays.

## DPI contract

Capture geometry ultimately uses physical pixels. WinUI pointer positions are logical/DIP values and must cross an explicit DPI conversion before entering capture geometry.

Each Window picker overlay uses the DPI for the display it covers. The selected HWND bounds remain desktop-physical coordinates; individual overlays clip and convert the shared target into their own local presentation coordinates.

Automated geometry coverage includes common scale factors, mixed-axis DPI, adjacent-monitor boundaries and negative virtual origins.

## Window Capture across monitors

Window Capture performs these steps before any always-on-top picker window exists:

1. enumerate active displays;
2. snapshot capturable windows in native Z-order;
3. freeze one desktop frame per display;
4. create one `WindowTargetOverlayWindow` per display;
5. show overlays;
6. hit-test the frozen window list geometrically;
7. project the shared target highlight onto every display the window intersects.

This architecture avoids selecting SNAPVERE's own overlay and remains stable for a target window that spans two monitors.

A display can use a negative physical origin; no selection code requires a positive coordinate system.

## Region Capture boundary

Current Region Capture freezes one primary-display frame. The Region overlay converts local WinUI interaction into physical pixels relative to that display and then adds the display's desktop origin. The resulting crop is still constrained to the selected display frame.

Cross-monitor Region capture requires more than simply increasing an overlay rectangle: SNAPVERE would need a coordinated frozen virtual-desktop composition, per-display DPI mapping and deterministic crop/render behavior across display seams. That work remains intentionally deferred.

## Screen Capture boundary

Current Screen Capture uses the primary `DisplayDescriptor`. The codebase has the display-discovery and virtual-desktop primitives needed for future monitor-under-cursor/all-monitor choices, but those choices are not shown in product UI today.

## Cursor behavior

The final Region/Window/Screen capture path can include the cursor through the implemented local preference. Window picker background frames remain cursor-free because the picker is a targeting UI surface, not final image output.

Future cross-monitor cursor work must preserve hotspot placement and physical coordinates across mixed-DPI display boundaries.

## Runtime and QA coverage

Automated coverage includes:

- negative virtual desktop coordinates;
- monitor union and hit-test boundaries;
- physical/logical DPI transforms;
- one frozen Window picker overlay per display;
- window targets that intersect multiple displays;
- overlay-safe frozen Z-order hit testing;
- x64/x86 Window picker runtime materialization in Installed and Portable packages.

Hardware/manual QA remains valuable for combinations hosted CI cannot faithfully emulate:

- three or more physical monitors;
- primary in center/right positions;
- portrait/rotated displays;
- 100/125/150/175/200% mixed scaling;
- HDR + SDR combinations;
- unusual GPU/driver configurations.

## Remaining work

- coordinated cross-monitor Region freeze and selection;
- monitor-under-cursor Screen Capture;
- all-monitors / virtual-desktop Screen Capture;
- broader physical-hardware mixed-DPI validation;
- portrait/rotation-specific polish.
