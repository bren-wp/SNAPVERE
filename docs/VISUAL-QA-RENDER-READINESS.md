# Rendered UI visual QA readiness

SNAPVERE's Windows visual regression gate captures six real rendered application surfaces during CI. Region Capture and Window Capture require extra synchronization because WinUI can expose an HWND before the frozen capture image has entered the visual tree and before DirectComposition produces stable pixels for that HWND.

The gate therefore follows these rules:

- `CopyFromScreen` is not used as a fallback. Desktop pixels cannot be treated as application-owned merely because a window is foreground or topmost.
- Single-surface Region and Window probes publish their ready marker only after an `Image` with a non-null `Source` is observable in the probe visual tree.
- The external PowerShell probe waits for that application-side marker before trying to capture the HWND.
- `PrintWindow` is the only pixel source accepted for those rendered snapshots.
- A non-empty frame is not enough: the sampled visual fingerprint must match on three consecutive captures separated by a short bounded delay.
- Probe-only readiness polling is bounded and exists only when an explicit visual-QA probe mode is active. It does not run during normal SNAPVERE startup or capture use.
- Visual comparison thresholds are unchanged. A readiness fix must not be implemented by weakening the regression gate.

A PR that changes this mechanism is not considered verified until CI also passes x86 build, x64 build/tests, ARM64 build, six-surface rendered capture, baseline comparison, the two-file universal package contract, and x64/x86 Setup and Portable lifecycle validation.
