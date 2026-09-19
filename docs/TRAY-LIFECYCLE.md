# SNAPVERE Tray and Process Lifecycle

Current public release: **v0.1.6**. The `main` branch may contain later unreleased lifecycle hardening.

The Windows application is designed as a **tray-first** process. A normal launch keeps SNAPVERE available for capture without opening a permanent dashboard window, while global shortcuts and the notification-area host provide the primary entry points.

## Single-instance boundary

`SingleInstanceGuard` runs through a module initializer before normal WinUI startup. It acquires a named per-user desktop mutex and keeps that mutex for the complete process lifetime.

If another normal SNAPVERE process already owns the mutex, the duplicate process signals a per-user activation event and then exits successfully before tray and hotkey hosts are created. The primary instance dispatches that signal to WinUI and surfaces its Preferences window, so relaunching an already-running tray-first app produces visible feedback without creating another app process. If the previous process crashed and abandoned the mutex, the new launch treats the acquired abandoned mutex as valid ownership so a stale process state cannot permanently block startup.

Dedicated CI automation probe launches are exempt from the production singleton guard so visual and lifecycle probes can validate surfaces deterministically. That exemption is limited to the explicit probe environment variables/command-line switches used by repository validation.

The Portable launcher probes the same desktop-instance identity before expensive payload verification/extraction. If an instance is already running, the fast path signals the same activation event and returns without hashing or extracting the large payload. The child application remains the final race-safe owner of the mutex.

## Native tray host

`Win32TrayIconService` owns a dedicated background thread and a message-only Win32 window. `Start()` waits until native initialization either completes or reports a startup exception, so the application does not silently continue after a failed tray host initialization.

The tray icon uses `Shell_NotifyIcon` with notification protocol version 4. A left click, double click or keyboard selection requests Region Capture. Right click/context-menu activation requests the branded WinUI tray menu.

Rapid Region Capture clicks are debounced. Exceptions raised by UI command subscribers are contained so they cannot terminate the native tray message loop.

## Explorer recovery and localization

The tray host registers the `TaskbarCreated` message. When Explorer recreates the taskbar, SNAPVERE immediately attempts to add its notification icon again. If Explorer's notification area is not ready yet, recovery uses bounded non-blocking retries after 250 ms, 500 ms, 1 second and 2 seconds, for at most five total registration attempts. Retry timers only post a private message back to the native tray loop; they do not sleep or block tray input. A generation token rejects stale timer callbacks after a newer recovery cycle or a successful restore. If all attempts fail, the process remains alive and registered global hotkeys can continue to work.

When the active SNAPVERE language changes, the message loop receives a private refresh message and updates the localized tray tooltip without recreating the application process.

## Shutdown and native resource cleanup

`Dispose()` unsubscribes language events, posts `WM_CLOSE` to the message-only window and joins the tray thread for a bounded interval when appropriate. Window destruction posts the quit message for the native loop.

Cleanup removes the notification icon, destroys the generated icon handle, destroys the message-only window and unregisters its temporary window class. Native bitmap handles used while constructing the SNAPVERE tray icon are released after `CreateIconIndirect` completes.

## Installed and Portable lifecycle evidence

The repository's tray-first lifecycle validation runs against both the installed application and the Portable package for x64 and x86.

For Setup it verifies that the setup UI can materialize, performs a silent install, confirms the installed app exists, runs a tray initialization probe, launches normally and requires the process to stay alive without a visible main window. It then launches a second instance and requires the duplicate to exit successfully, requires the original process to receive the activation signal, and verifies that the original remains the only SNAPVERE app process. Finally it uninstalls through the installed Setup executable.

For Portable it runs the same tray probe, starts the Portable launcher, requires the launcher to return after starting the tray-only child, confirms exactly one SNAPVERE application process, checks that no main window appears and verifies that a second Portable launch activates the existing child through the same per-user signal without creating a duplicate application process.

The full package lifecycle gate separately validates the universal Setup/Portable contract and writes explicit architecture completion markers only after both x64 and x86 lifecycle scripts finish. CI requires those markers instead of inferring success from ambiguous PowerShell process state.

## User-visible behavior

Normal tray-first operation is intentionally different from an application that opens a dashboard on every launch. Capture can be started from the tray or global shortcuts; secondary surfaces such as Options, Language and About are opened only when requested. Relaunching SNAPVERE while it is already running is treated as an explicit request to surface the existing instance, so the primary Preferences window is brought forward instead of silently discarding the launch.

CI evidence proves the repository's declared startup/lifecycle scenarios on its Windows runners. It is not a guarantee that Explorer, third-party shell software, security products or every Windows configuration can never interfere with notification-area behavior.

Related documents: [Installation](INSTALLATION.md), [Settings](SETTINGS.md), [Performance & Stability](PERFORMANCE.md), [Architecture](ARCHITECTURE.md), [QA Matrix](QA-MATRIX.md).
