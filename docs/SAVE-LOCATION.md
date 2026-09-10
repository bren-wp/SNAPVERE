# Capture save location

SNAPVERE lets the user choose the final location of every completed **Region**, **Window**, and **Screen** capture.

## User flow

1. SNAPVERE captures the selected pixels and completes PNG encoding through the normal capture pipeline.
2. The completed PNG is first written safely to the default local capture directory (`Pictures\SNAPVERE`) using the existing temp-file and atomic-move contract.
3. SNAPVERE opens the native Windows **Save As** picker with the generated capture name suggested and `.png` selected.
4. The user can keep the suggested location/name or choose another writable folder and filename.
5. When another path is selected, SNAPVERE relocates the already completed PNG to that path.

This ordering is intentional: an interactive picker is never allowed to put the only copy of an in-memory capture at risk.

## Cancel behavior

Closing or cancelling the Save As picker does **not** discard the screenshot. The already completed PNG remains in the default `Pictures\SNAPVERE` capture directory.

## Replacement behavior

The native Windows picker is configured to ask before replacing an existing file. After the user confirms replacement, SNAPVERE copies the completed capture to a short, destination-local staging file and then atomically replaces the selected destination.

The staging basename is independent of the user-selected filename (`.snapvere-<guid>.tmp`). This avoids overflowing the filesystem component-name limit when a valid long PNG filename is selected.

## Failure behavior

If relocation fails before the destination is complete, the original capture remains available in the default capture directory. Temporary relocation files are removed on a best-effort basis.

If the destination is already complete but Windows cannot remove the original file, SNAPVERE keeps the original as a recovery copy instead of risking capture loss.

## File format

The current public capture pipeline writes PNG. The Save As workflow therefore accepts `.png` destinations only. It does not silently transcode a capture when a different extension is typed.

## Clipboard captures

A Region capture explicitly completed with **Copy** remains clipboard-only from the user's perspective and does not open the Save As picker. The Save As picker is used for captures completed through the save path.

## Privacy and resource use

The save-location feature is local-only. It introduces no network access, telemetry, upload service, polling loop, timer, file watcher, or resident background worker. The native picker is created only after a capture has been completed and disposed by the Windows picker workflow after the user makes a choice.

## Architecture

- `CaptureCenterWindow` coordinates Region, Window, and Screen save completion and invokes the picker.
- `CaptureSaveLocationService` is the thin Windows App SDK picker adapter.
- `CaptureRelocationService` contains the UI-independent, testable relocation contract.
- Existing capture workflows and `CaptureFileWriter` continue to own capture acquisition, PNG encoding, default persistence, and atomic creation of the initial completed PNG.

Keeping relocation separate from capture acquisition prevents Save As UX changes from altering capture geometry, annotation, cursor handling, encoder behavior, or the tray-first runtime contract.
