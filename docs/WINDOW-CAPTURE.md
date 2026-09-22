# SNAPVERE 0.1.2 Window Capture

Window Capture is a Windows-only capture path that separates **target selection** from **target acquisition**. The picker freezes the visible desktop and capturable-window ordering before SNAPVERE displays its own topmost overlays; the selected native window is then acquired through the window capture service and saved through the shared PNG publication pipeline.

## Target discovery

`Win32WindowDiscovery` enumerates top-level windows in native Z-order. A target is excluded when it is not visible, belongs to the current SNAPVERE process, is DWM-cloaked, has no non-empty title or has no positive-area bounds.

Window bounds prefer `DWMWA_EXTENDED_FRAME_BOUNDS` and fall back to `GetWindowRect` when DWM bounds are unavailable. Bounds are stored as physical pixel rectangles and may contain negative desktop coordinates on layouts with displays positioned left or above the primary monitor.

Hit testing is performed against the ordered `WindowDescriptor` snapshot rather than asking Windows again after SNAPVERE overlays appear. That keeps selection stable while the picker owns topmost surfaces.

## Frozen multi-monitor picker

`WindowTargetPicker` first discovers all active displays and the current capturable-window list. Before showing any picker surface it captures one cursor-free frozen frame per display.

One `WindowTargetOverlayWindow` is then created for each display. Hover changes are propagated to every overlay so a target remains visually consistent across the virtual desktop. Selecting a target completes the shared picker task; cancellation completes it with no target. Every overlay is closed from a `finally` block regardless of selection, cancellation or failure.

Overlay instances take ownership of their frozen frames. After construction the coordinator clears its frame dictionary before showing the overlays, allowing raw BGRA monitor buffers to become collectible as soon as each overlay has loaded its bitmap instead of retaining every monitor frame for the whole picker lifetime.

## Window acquisition

The current window backend is `Windows.Graphics.Capture` (WGC). Window acquisition requires Windows 10 version 2004 / build 19041 or later and WGC support. A zero native handle is rejected before native work begins.

For each acquisition SNAPVERE creates a BGRA-capable D3D11 hardware device, creates a `GraphicsCaptureItem` for the selected `HWND`, then uses a free-threaded two-buffer frame pool and a capture session. Cursor capture is enabled only when the effective capture preference requests it.

The first frame must arrive within two seconds. A caller cancellation remains caller cancellation; an internal first-frame timeout is converted to a controlled `TimeoutException`. If WGC returns a blank frame, the capture is rejected rather than published as a valid screenshot.

The delivered frame is copied into a validated `CaptureFrame` with packed BGRA data and source metadata. Frame-event handlers are detached and WinRT/D3D resources are disposed on every exit path.

Window Capture does **not** claim a separate legacy window backend in the current 0.1.2 implementation. Platform restrictions or protected content can still prevent a successful window frame.

## Save path and cancellation

`WindowCaptureWorkflow` validates caller cancellation before invoking the backend, validates the returned frame, then uses the same `CaptureFileWriter` used by other Windows capture workflows. The effective cursor setting is the explicit request OR the persisted capture preference.

The writer encodes to a unique temporary file, flushes it, checks cancellation once more and only then atomically moves it to the final PNG path. A cancelled or failed operation therefore does not intentionally expose a partial PNG as a completed capture.

See [Image Pipeline](IMAGE-PIPELINE.md) for encoding and publication details.

## Regression evidence

The unit suite covers Window Capture discovery forwarding, native descriptors with negative coordinates, PNG publication through the shared writer and cancellation before backend invocation. Separate hit-testing tests cover topmost target selection.

Windows CI also renders the Window Capture overlay as part of visual QA and compares it with the last successful `main` baseline. Universal package CI subsequently exercises installed and Portable lifecycle paths on x64 and x86.

These automated checks reduce regression risk; they do not prove that every third-party window, graphics driver or protected-content policy is capturable.

Related documents: [Architecture](ARCHITECTURE.md), [Image Pipeline](IMAGE-PIPELINE.md), [Performance & Stability](PERFORMANCE.md), [QA Matrix](QA-MATRIX.md).
