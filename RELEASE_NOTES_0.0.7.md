# SNAPVERE 0.0.7

SNAPVERE 0.0.7 is a packaging, UI consistency, localization and reliability release. It does not rewrite or mutate previously published releases.

## Two universal downloads

Starting with 0.0.7, the public GitHub Release contains exactly:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`

Each executable embeds x86, x64 and ARM64 native application payloads and automatically selects a compatible payload for the current Windows architecture. Users no longer choose a separate x86/x64 download.

The application target remains Windows 10 version 1809 / build 17763 or later. WGC-dependent capture paths require Windows 10 version 2004 / build 19041 or later.

## UI and branding

- Tray flyout geometry and graphite/violet/cyan styling aligned with the maintained README UI reference.
- Region Capture editor aligned with the reference layout: vertical tool rail, eight selection handles and separate Copy / Save / Close actions.
- Options, Language, About and Setup use the same SNAPVERE visual system.
- SNAPVERE is the product brand; Brendigo is the developer and publisher.
- Official product site: `snapvere.com`.
- Developer site: `brendigo.com`.

## Languages

English is the canonical default and fallback language. The built-in selector exposes 28 language choices, including Croatian and more than 20 additional languages. Language preference is stored locally in `%LOCALAPPDATA%\SNAPVERE\settings.json` and does not require a network translation service.

## Setup defaults

Interactive Setup now defaults to:

- Start menu shortcut: On
- Desktop icon: On
- Start SNAPVERE with Windows: On

These remain user-selectable and can be disabled before installation. Silent Setup uses the same defaults after explicit commercial-license acceptance.

## Commercial license

SNAPVERE 0.0.7 and later are distributed under the SNAPVERE Commercial Software License Agreement shipped in `LICENSE`. Historical releases remain governed by the license terms distributed with those versions.

## Reliability, security and performance

- Removed obsolete Demo packaging branches and duplicate launcher behavior.
- Shared universal architecture resolver handles x86/x32, x64/AMD64 and ARM64 payload selection.
- ZIP extraction keeps path-traversal protections and bounded extraction behavior.
- Settings writes remain local and atomic.
- Startup/shortcut/uninstall lifecycle is validated in CI.
- Tray/hotkey idle behavior remains event-driven; localization adds no polling thread or network worker.
- Capture/D3D resources remain lazy rather than being initialized solely for tray residency.

## Release validation

Publication is blocked unless:

- x64 build/test passes;
- x86 build passes;
- ARM64 cross-build passes;
- all three native payload archives contain `Snapvere.exe`;
- exactly two public EXE files are produced;
- x64/x86 universal Setup and Portable runtime lifecycle passes;
- commercial-license acceptance is enforced;
- default Desktop shortcut and Start-with-Windows registration are created;
- same-Setup uninstall removes application/registration artifacts;
- Tray, Region, Window, Options and About runtime probes materialize.

ARM64 package validation on the hosted x64 runner is not represented as a real ARM64 hardware runtime test.
