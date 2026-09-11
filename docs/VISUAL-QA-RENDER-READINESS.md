# Rendered UI visual QA readiness

SNAPVERE's Windows visual regression gate captures six real rendered application surfaces during CI. WinUI can expose an HWND before its content has finished entering the visual tree and before DirectComposition produces stable pixels for that HWND, so the gate uses explicit probe-only synchronization instead of desktop capture fallbacks.

The gate therefore follows these rules:

- `CopyFromScreen` is not used as a fallback. Desktop pixels cannot be treated as application-owned merely because a window is foreground or topmost.
- Region Capture and Window Capture publish their ready marker only after an `Image` with a non-null `Source` is observable in the probe visual tree.
- The external PowerShell probe waits for that application-side marker before trying to capture the HWND.
- Tray menu, Options, Language and About are tested sequentially with a per-surface ready/acknowledgement handshake. The application keeps the current surface alive until PowerShell has captured a stable frame and written that surface's acknowledgement marker; only then does the probe advance to the next surface.
- Secondary-surface readiness requires a real rendered root size before the ready marker is published. Both the application-side acknowledgement wait and external capture wait are bounded and fail closed on timeout.
- `PrintWindow` is the only pixel source accepted for rendered snapshots.
- A non-empty frame is not enough: the sampled visual fingerprint must match on three consecutive captures separated by a short bounded delay.
- Probe-only readiness polling is bounded and exists only when an explicit visual-QA probe mode is active. It does not run during normal SNAPVERE startup or capture use.
- Visual comparison thresholds are unchanged. A readiness fix must not be implemented by weakening the regression gate.
- Final pull-request evidence must come from a merge-ref generated against the current `main`; a green run against an older base is informative but is not final merge evidence.

A PR that changes this mechanism is not considered verified until CI also passes x86 build, x64 build/tests, ARM64 build, six-surface rendered capture, baseline comparison, the two-file universal package contract, and x64/x86 Setup and Portable lifecycle validation.
