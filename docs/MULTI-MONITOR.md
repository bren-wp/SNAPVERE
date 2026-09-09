# Multi-monitor and virtual desktop behavior

## Status

Native monitor enumeration and pure virtual-desktop geometry are implemented as the first multi-monitor foundation. Capture backend integration and manual QA across physical monitor configurations remain in progress.

## Rules

SNAPVERE treats the Windows virtual desktop as a physical-pixel coordinate space. The primary monitor is not assumed to start at the virtual origin, and monitors may exist left of or above it, producing negative coordinates.

Each `DisplayDescriptor` records:

- physical monitor bounds
- work area
- effective DPI X/Y
- primary-display flag
- stable Windows device identifier when available

`Win32DisplayDiscovery` uses `EnumDisplayMonitors` and `GetMonitorInfoW` for monitor geometry. Effective monitor DPI is read through `GetDpiForMonitor` with a conservative 96-DPI fallback if DPI discovery is unavailable for a specific monitor.

## Virtual desktop bounds

`VirtualDesktopLayout.GetBounds` computes a union across all monitor rectangles. It does not use resolution assumptions, monitor ordering, or primary-monitor anchoring.

Example:

```text
          [ portrait monitor ]
          y = -1600
                 │
[ left ] ───── [ primary ]
 x = -1920       x = 0
```

The resulting virtual desktop may therefore begin at a negative X and/or negative Y coordinate.

## Hit testing

Monitor hit testing treats left/top edges as inclusive and right/bottom edges as exclusive. This avoids assigning a boundary pixel to two adjacent monitors.

## DPI

All capture engine geometry ultimately uses physical pixels. XAML/logical coordinates must cross an explicit DPI transform before entering capture geometry. No capture subsystem may assume 96 DPI.

Automated coverage currently includes 100%, 125%, 150%, 200%, negative coordinates, mixed-axis DPI, adjacent-monitor edge ownership, a monitor left of primary and a portrait monitor above primary.

## Manual QA matrix before 1.0

- one monitor
- two monitors
- three or more monitors
- primary in center/right positions
- monitor left of primary
- monitor above primary
- portrait orientation
- 100/125/150/175/200% scaling combinations
- HDR + SDR combinations
- x64 and ARM64 where hardware is available

## Next implementation

1. Map Windows.Graphics.Capture targets to `DisplayDescriptor` instances.
2. Capture physical monitor frames without coordinate conversion drift.
3. Build freeze-frame region selection across the complete virtual desktop.
4. Validate cursor placement across mixed-DPI monitor boundaries.
