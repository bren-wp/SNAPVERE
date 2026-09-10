# Tray UX

## Product contract

SNAPVERE is tray-first. The notification-area icon is the default persistent user surface; the hidden WinUI capture coordinator is an implementation detail and is not activated during normal startup.

Normal launch:

```text
Snapvere.exe
  → initialize services
  → create hidden capture coordinator
  → start global hotkeys
  → start tray host
  → remain alive with no Capture Center visible
```

This same behavior applies to installed and Portable packages.

## Left click

A normal single left-click on the SNAPVERE tray icon starts Region Capture immediately.

```text
WM_LBUTTONUP
  → TrayCommand.RegionCapture
  → App DispatcherQueue
  → Region capture workflow
```

No menu or large window is opened first. Double-click handling is debounced so it does not create duplicate Region Capture actions on top of the single-click contract.

## Right click

Right-click opens a branded programmatic WinUI flyout near the tray/cursor area. The native tray message thread raises `TrayCommand.ShowMenu`; WinUI window creation occurs only after routing through the application's UI `DispatcherQueue`.

Current commands:

- Capture Region — Print Screen / `Ctrl+Shift+1`
- Capture Window — `Ctrl+Shift+2`
- Capture Screen — `Ctrl+Shift+4`
- Open Capture Folder
- Recent captures
- Options / Preferences
- About SNAPVERE
- Exit SNAPVERE

The flyout closes when it loses activation and Esc closes it. It is borderless/compact and does not act as another application dashboard.

## Command ownership

`Win32TrayIconService` owns only native integration concerns:

- notification icon add/update/remove;
- left/right/double click native messages;
- command event raising;
- Explorer/taskbar recreation recovery;
- native icon lifetime.

The service must not directly create or mutate WinUI controls from its native message thread.

`App` owns user-facing command dispatch on the WinUI thread.

## Capture shortcuts

| Action | Primary input | Fallback / alternate |
| --- | --- | --- |
| Region Capture | tray left-click or Print Screen | `Ctrl+Shift+1` |
| Window Capture | tray right-click → Capture Window | `Ctrl+Shift+2` |
| Screen Capture | tray right-click → Capture Screen | `Ctrl+Shift+4` |
| Quick actions | tray right-click | — |

Print Screen may be unavailable when Windows or another application owns it. The independent Region fallback remains available.

## Secondary windows

### Options / Preferences

Opens the real `OptionsWindow`. It contains only implemented preferences and does not reintroduce the Capture Center as a visible launcher.

### Recent captures

Opens the Recent Captures section of `OptionsWindow`. The list is local filesystem-backed history and provides real open/refresh/folder actions.

### About

Opens the factual SNAPVERE About surface. No fake updater, licensing or account controls are exposed.

## Startup probe

`SNAPVERE_TRAY_STARTUP_PROBE=1` or `--tray-startup-probe` exercises the tray-first initialization path and writes a marker containing:

```text
SNAPVERE 0.0.X TRAY_READY
```

The probe is reached only after global hotkeys and tray host startup completes while the Capture Center remains hidden.

The legacy `READY` activated-window probe remains a separate technical construction check. It is not the expected normal-launch behavior.

## Stability rules

- do not initialize WGC/D3D merely to sit in the tray;
- do not manipulate WinUI controls on the native tray thread;
- do not use decorative templated controls known to destabilize packaged runtime probes;
- do not expose commands for unimplemented standalone editor/update/licensing features;
- do not make double-click behavior interfere with single-click Region Capture;
- keep tray icon/native handles deterministically owned and recover after Explorer restarts.

## Visual direction

The flyout uses the SNAPVERE graphite/navy and violet/indigo identity, compact rounded Windows 11-style spacing, local Fluent-style icons and concise shortcut metadata. Emoji are not used as product icons.

Brand specifics are documented in `docs/BRANDING.md`.
