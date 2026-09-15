# SNAPVERE 0.1.1 Troubleshooting

## Windows does not capture

Try the capture again after closing protected/full-screen content. SNAPVERE prefers the modern Windows capture path and can use its compatibility monitor fallback for expected acquisition failures. Protected content can intentionally remain blank.

## Shortcut does not respond

Another application may own the same global shortcut. Use the tray menu and review the Windows startup/shortcut state.

## Region save or copy fails

Confirm the capture folder is writable and that the Windows clipboard is available. SNAPVERE uses staged file writes so an interrupted PNG encode is not presented as a completed capture.

## Browser capture fails

Privileged browser pages can block script injection or screenshot APIs. Switch to a regular web page and try again. If the active tab changes during capture, SNAPVERE discards the frame instead of saving content from the wrong tab.

## Full page is rejected

The page exceeded the bounded tile/canvas/pixel budget. This is a stability safeguard, not a hidden background failure.

Windows startup diagnostics, when needed, are stored locally under `%LOCALAPPDATA%\SNAPVERE\Logs`.
