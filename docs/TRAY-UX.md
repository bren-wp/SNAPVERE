# Tray UX

## Product contract

SNAPVERE is tray-first. The notification-area icon is the default persistent user surface; the hidden WinUI runtime/capture coordinator is an implementation detail and is not activated during normal startup.

Normal launch:

```text
Snapvere.exe
  → initialize lightweight services
  → create hidden runtime/capture coordinator
  → start global hotkeys
  → start native tray host
  → remain alive with no Capture Center visible
```

This same behavior applies to installed and Portable packages and to optional Windows sign-in startup. No separate background-only command line is required.

## Notification-area protocol

v0.0.9 uses the modern `NOTIFYICON_VERSION_4` notification-area callback contract. Every successful `NIM_ADD` is followed by `NIM_SETVERSION`, including the re-add path after Explorer/taskbar recreation. If setting version 4 fails during initial startup, the just-added icon is removed and tray initialization fails rather than silently running with an ambiguous callback contract.

The icon keeps `NIF_SHOWTIP`, so the normal SNAPVERE tooltip is preserved under the version-4 protocol. Callback notifications are decoded from the low word of `lParam`; pointer and keyboard activation events are handled explicitly.

## Left click and keyboard activation

A normal single left-click on the SNAPVERE tray icon starts Region Capture immediately. Version-4 `NIN_SELECT` and `NIN_KEYSELECT` activation also map to the same Region Capture command.

```text
WM_LBUTTONUP / NIN_SELECT / NIN_KEYSELECT
  → TrayCommand.RegionCapture
  → App DispatcherQueue
  → Region capture workflow
```

No menu or large window is opened first. Double-click handling remains debounced so duplicate shell activation notifications cannot create overlapping Region Capture actions.

## Right click and keyboard context menu

Right-click or the version-4 `WM_CONTEXTMENU` notification opens the branded programmatic WinUI flyout near the tray/cursor area. The native tray message thread raises `TrayCommand.ShowMenu`; WinUI window creation occurs only after routing through the application UI `DispatcherQueue`.

The current flyout follows the maintained `docs/images/tray-menu.svg` product reference: **418×540**, graphite `#0D1321`, strong `#52617F` outline, violet primary capture treatment, restrained cyan accents and the canonical SNAPVERE mark.

Current user actions:

- Capture region — Print Screen / `Ctrl+Shift+1`;
- Capture window — `Ctrl+Shift+2`;
- Capture screen — `Ctrl+Shift+3`;
- Open capture folder;
- Options & recent captures;
- Language;
- About SNAPVERE;
- Exit.

Language is a real action, not a decorative entry. The flyout reads the persisted language code when it is created and opens the on-demand `LanguagePickerWindow` through the application coordinator.

The flyout closes when it loses activation, after a command is selected, or when Esc is pressed. It is a compact quick-action surface rather than a dashboard.

## Command ownership

`Win32TrayIconService` owns native integration concerns only:

- notification icon add/update/remove/version negotiation;
- pointer and keyboard tray callback decoding;
- command event raising;
- Explorer/taskbar recreation recovery;
- native icon lifetime.

The service must not create or mutate WinUI controls from its native message thread. `App` owns user-facing command dispatch on the WinUI thread. Subscriber exceptions are contained so a user-interface handler cannot terminate the native tray message loop.

## Capture shortcuts

| Action | Primary input | Fallback / alternate |
| --- | --- | --- |
| Region Capture | tray left-click or Print Screen | `Ctrl+Shift+1` |
| Window Capture | tray right-click → Capture window | `Ctrl+Shift+2` |
| Screen Capture | tray right-click → Capture screen | `Ctrl+Shift+3` |
| Quick actions | tray right-click / keyboard context menu | — |
| Language | tray language action or Options | — |

Print Screen may be unavailable when Windows or another application owns it. The independent Region fallback remains available.

## Secondary windows

### Options / Preferences

Opens the real `OptionsWindow`. It contains implemented local preferences and Recent Captures without reintroducing Capture Center as a visible launcher.

### Language

Opens one on-demand language picker. English is the canonical default and fallback; Croatian and more than 20 additional built-in languages are selectable. Selection is written to local settings and creates no resident timer, watcher, network worker or translation service.

### Recent captures

Uses local filesystem-backed history and provides real open/refresh/folder actions.

### About

Opens the factual commercial SNAPVERE About surface with Brendigo developer/publisher identity and the official `snapvere.com` / `brendigo.com` links, support email and Privacy/Terms destinations.

## Startup, runtime and visual probes

`SNAPVERE_TRAY_STARTUP_PROBE=1` or `--tray-startup-probe` exercises tray-first initialization and emits:

```text
SNAPVERE 0.0.X TRAY_READY
```

The marker is reached only after global hotkeys and the tray host initialize while the runtime coordinator remains hidden.

`SNAPVERE_SECONDARY_UI_PROBE=1` or `--secondary-ui-probe` runs the actual WinUI materialization sequence:

```text
tray flyout loaded
  → Options loaded
  → Language loaded
  → About loaded
  → SECONDARY_UI_READY
```

This marker probe is executed against installed and Portable x64/x86 package candidates. A compile-successful but non-materializable Tray, Options, Language or About surface therefore blocks package validation.

CI adds a stronger x64 visual check with `eng/Capture-SnapvereVisualQa.ps1`. It launches the actual application probe paths, captures rendered Region, Window, Tray, Options, Language and About PNGs, rejects empty/unexpectedly small frames, records capture provenance plus dimensions/size/SHA-256 in `manifest.json`, compares PR surfaces against the last clean successful-main baseline and uploads the result as an Actions artifact. Visual thresholds are not lowered to conceal regressions.

The legacy `READY` activated-window probe remains only a hidden runtime-host construction check. It is not expected normal-launch behavior.

## Stability and performance rules

- do not initialize WGC/D3D merely to sit in the tray;
- do not manipulate WinUI controls on the native tray thread;
- do not introduce periodic polling for tray, localization or recent-capture state;
- do not expose decorative controls without a real action;
- do not make double-click behavior interfere with single-click Region Capture;
- negotiate `NOTIFYICON_VERSION_4` after every icon add/re-add;
- decode version-4 callback events from the documented low-word event field;
- keep native tray handles deterministically owned and recover after Explorer restarts;
- keep manual launch and Windows startup on the same tray-first executable contract;
- create Tray/Options/Language/About windows only on demand and release them when closed.

## Accessibility and visual direction

The flyout uses Windows-native Segoe/Fluent iconography rather than emoji, exposes automation names for actionable controls, preserves keyboard Esc behavior and supports keyboard notification-area activation/context-menu events. Shape, border and state changes supplement color. High-contrast and readable text remain more important than decorative glow.

Brand specifics are documented in `docs/BRANDING.md`; Croatian tray documentation is in `docs/hr/TRAY-UX.md`.
