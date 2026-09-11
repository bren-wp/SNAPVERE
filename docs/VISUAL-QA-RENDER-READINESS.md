# Rendered UI visual QA readiness

SNAPVERE's Windows visual regression gate captures six real rendered application surfaces during CI. WinUI can expose an HWND before its content has finished entering the visual tree and before DirectComposition produces stable pixels for that HWND, so the gate uses explicit probe-only synchronization instead of desktop capture fallbacks.

The gate therefore follows these rules:

- `CopyFromScreen` is not used as a fallback. Desktop pixels cannot be treated as application-owned merely because a window is foreground or topmost.
- Region Capture and Window Capture publish their ready marker only after an `Image` with a non-null `Source` is observable in the probe visual tree.
- The external PowerShell probe waits for that application-side marker before trying to capture the HWND.
- Tray menu, Options, Language and About are tested sequentially with a per-surface ready/acknowledgement handshake. The application keeps the current surface alive until its caller acknowledges that exact surface; only then does the probe advance.
- Every secondary-UI probe invocation creates a new GUID session ID. Ready, acknowledgement and final-completion marker filenames are scoped to that session, so a file left by an earlier process cannot satisfy a later visual or package-lifecycle run.
- Both secondary-UI callers participate in the protocol: rendered visual QA writes an acknowledgement only after a stable HWND-owned image was saved, while Setup/Portable lifecycle validation explicitly acknowledges each rendered surface before continuing.
- Secondary-surface readiness requires a real rendered root size before the ready marker is published. Application-side and external waits are bounded and fail closed on timeout.
- Secondary probe failures are diagnostic-log + nonzero-exit events; CI probe failures do not open an interactive fatal MessageBox that could block an unattended runner.
- `PrintWindow` is the only pixel source accepted for rendered snapshots.
- A non-empty frame is not enough: the sampled visual fingerprint must match on three consecutive captures separated by a short bounded delay.
- Probe-only readiness polling is bounded and exists only when an explicit visual-QA probe mode is active. It does not run during normal SNAPVERE startup or capture use.
- Visual comparison thresholds are unchanged. A readiness fix must not be implemented by weakening the regression gate.
- Final pull-request evidence must come from a merge-ref generated against the current `main`; a green run against an older base is informative but is not final merge evidence.

A PR that changes this mechanism is not considered verified until CI also passes x86 build, x64 build/tests, ARM64 build, six-surface rendered capture, baseline comparison, the two-file universal package contract, and x64/x86 Setup and Portable lifecycle validation.
