# SNAPVERE — native Save As trust boundary

**Status:** Implemented in PR #20; CI and real Windows 10/11 verification must complete before release. Applies to the Tauri/Rust repository edition, **not** to the separately deployed snapvere.com WinUI desktop edition.

## Problem identified

The prior `save_image_as` custom IPC command accepted `path: String` directly from the renderer. Any webview that could invoke the native command could provide an absolute path and ask the privileged Rust process to overwrite a chosen image file. A frontend file picker alone does not secure the boundary because it can be bypassed by another IPC caller.

## Change

- Native Rust `save_image_as` does **not** accept a path argument. The Rust process invokes the OS Save As dialog.
- The injected Tauri window label must be exactly `editor` or `overlay`. Gallery, settings, recording controls and similar webviews cannot request an export through this command.
- The user chooses a PNG/JPEG/WebP destination in the OS dialog. The native encoder is selected from the final destination extension, not a renderer-controlled path.
- Cancellation returns `false` without writing. The original canvas and annotations remain available for retry.
- Output is first written to an exclusively created, random temporary file next to the selected destination. An incomplete staged write is cleaned up and never truncates a pre-existing exported file.
- Existing capture-overlay Save As and editor Save As now use the same backend dialog, so changing the IPC contract does not remove a known workflow.

## Regression coverage

`apps/desktop/src-tauri/src/commands/image_export.rs` contains unit tests for accepted/rejected source-window labels, absolute-path requirements, supported extensions and codec mapping. The existing frontend CI runs the IPC contract checker, typecheck, Node tests and production build.

## Windows acceptance checks — not yet verified by source review

1. In Editor and capture overlay, choose PNG, JPG/JPEG and WebP; decode the resulting files and verify dimensions, image content and output format.
2. Cancel the picker and verify no image is written and the original image remains editable.
3. Choose an existing image and confirm the native overwrite prompt is shown; test denied permission, read-only folder and disk-full cases.
4. Confirm all non-editor/non-overlay windows receive an error when invoking `save_image_as` and a manually forged `path` IPC argument has no effect.
5. Test long/Unicode file paths, concurrent clicks, multiple monitors, Windows display scaling and closing the editor during a picker.

## Residual risk

The operating-system dialog authorizes the user-selected path but is **not** a universal filesystem sandbox. Changes to files between dialog confirmation and final rename, reparse points/symlinks, antivirus locking and unexpected filesystem behaviors still require hostile-path and Windows runtime tests. The command also processes a bounded base64 image provided by the renderer; retain pixel and payload guards.

No claim is made that this constitutes a full penetration test, proves 1:1 UI parity, or verifies installed Windows binaries. Do not merge or mark the change production-ready if mandatory CI jobs fail.
