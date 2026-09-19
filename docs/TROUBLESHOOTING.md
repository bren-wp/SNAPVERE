# SNAPVERE Troubleshooting

Current public release: **v0.1.6**. The `main` branch may contain later unreleased reliability and security hardening.

## Windows does not capture

Try the capture again after closing protected/full-screen content. SNAPVERE prefers the modern Windows capture path and can use its compatibility monitor fallback for expected acquisition failures. Protected content can intentionally remain blank.

## Shortcut does not respond

Another application may own the same global shortcut. Use the tray menu and review the Windows startup/shortcut state.

## Capture save or Region copy fails

If saving fails, SNAPVERE distinguishes a blocked capture folder, a full storage device and another local PNG write failure. Check Pictures/SNAPVERE permissions, confirm free disk space and retry. Region clipboard failures remain separate from file persistence. SNAPVERE uses staged file writes and an atomic final move so an interrupted PNG encode is not presented as a completed capture. Technical filesystem exception text remains in local diagnostics instead of being shown as recovery copy.

## Portable does not start

Close any running SNAPVERE process and retry the Portable executable. If SNAPVERE reports a package/cache validation problem, download a fresh copy from the official release. For filesystem failures, confirm that Windows can write to the current user's temporary and local application-data folders and that sufficient disk space is available.

Portable startup dialogs intentionally show sanitized error categories rather than raw runtime exception messages. Technical details are written locally to `%LOCALAPPDATA%\SNAPVERE\Logs\startup.log`; SNAPVERE does not automatically upload that log.

## Browser capture fails

Privileged browser pages can block script injection or screenshot APIs. Switch to a regular web page and try again. If the active tab changes during capture, SNAPVERE discards the frame instead of saving content from the wrong tab.

## Full page is rejected

The page exceeded the bounded tile/canvas/pixel budget. This is a stability safeguard, not a hidden background failure.

Windows startup diagnostics, when needed, are stored locally under `%LOCALAPPDATA%\SNAPVERE\Logs`.
