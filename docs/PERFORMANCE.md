# SNAPVERE 0.1.2 Performance & Stability

SNAPVERE is optimized around short-lived capture work instead of a permanently busy desktop process. Performance work therefore focuses on idle cost, peak memory during capture, predictable cleanup and regression detection.

## Windows memory ownership

Region Capture writes frozen BGRA rows directly into the WinUI bitmap instead of allocating a second full packed frame. Clipboard PNG transfer uses the existing compressed buffer instead of materializing another complete byte array. Draft annotation rendering avoids rebuilding the full point array for every pointer-move event.

Window Capture uses the same direct bitmap strategy for each monitor overlay. Once an overlay has finished loading its frozen image, the raw `CaptureFrame` reference is released. The picker also clears its coordinator dictionary after ownership has moved to the overlays so captured monitor buffers do not remain rooted for the full picker lifetime.

For a packed 3840×2160 BGRA frame, one redundant full-frame allocation is roughly 31.6 MiB. Removing one such staging allocation per monitor materially reduces multi-monitor peak managed memory.

## Capture pipeline

- expensive PNG compression is kept away from the WinUI thread;
- file writes stage before the final move;
- capture dimensions, strides and buffer lengths are validated before pixel processing;
- multi-monitor geometry is handled with native display bounds and DPI conversion;
- browser full-page capture uses explicit bounds and releases decoded tile resources after drawing;
- browser capture checks active-tab ownership before and after frame collection.

## Idle behavior

The Windows product is tray-first. Recent-capture enumeration is performed when the corresponding UI is opened rather than through a resident filesystem watcher. The normal application path does not run the visual-QA polling loops used by CI probes.

## Stability controls

SNAPVERE contains controlled failure paths for capture, filesystem, tray, hotkey, package and UI-host operations. Native and managed resources are released through explicit lifecycle ownership. A failure is recorded or surfaced rather than being treated as successful output.

No software can guarantee that every Windows driver, graphics stack or browser will never fail. The engineering goal is bounded work, explicit cleanup and strong regression evidence.

## Regression gates

CI for 0.1.2 validates:

- x64 build and unit tests;
- x86 and ARM64 builds;
- rendered WinUI surfaces and visual comparison with the successful `main` baseline;
- universal Setup/Portable construction;
- public package contract and package-size budgets;
- x64/x86 Setup and Portable lifecycle completion;
- browser runtime, permission, parity and deterministic-package checks;
- Product Contract CI and CodeQL.

See [QA Matrix](QA-MATRIX.md), [Architecture](ARCHITECTURE.md), [Multi-monitor](MULTI-MONITOR.md) and [Product Status](PRODUCT-STATUS.md).
