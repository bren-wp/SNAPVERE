# SNAPVERE — Code, Dead-Code, Security and Privacy Audit

This audit is performed against the current maintained Windows and browser source tree. It is an inventory and regression baseline, **not** a guarantee that every possible defect or unused runtime path has been eliminated.

## Verified changes

- **Settings JSON:** bounded to 16 KiB before deserialization. Oversized or malformed settings fall back to safe defaults without overwriting the original file merely by reading it; a unit test covers the oversized case.
- **Capture history:** ignores Windows reparse-point/symbolic-link entries so a file named like a capture cannot expose metadata and open actions for a target outside the designated local capture directory. A unit test covers links when platform permissions allow their creation.
- **Language picker:** the Settings window closes only after the real picker successfully activates. If activation fails, the original Settings window remains.
- **Proven dead code:** removed four declaration-only private aliases: OptionsWindow.Canvas, TrayMenuWindow.Accent, TrayMenuWindow.Transparent and RecordingControllerWindow.RecordingRed.
- **Browser recent-download ownership:** when the browser provides `byExtensionId` metadata, ignore captures initiated by other extensions even when their filenames match SNAPVERE's convention. Revalidate identity immediately before Open, and preserve compatibility for older browser records without provenance metadata. Runtime tests cover listing and click-time revalidation.
- **Browser parity:** the existing browser CI checks that shared files in Chrome, Edge, Opera and Firefox remain byte-identical (except supported manifest differences), rejects remote executable runtime resources and runs capture/options race tests.

## Repeatable inventory and limitations

\`python3 eng/audit-dead-code.py\` scans the complete C# source and test trees and four packaged browser JavaScript source trees; fails if the proven-dead aliases or privacy guards regress; and lists additional private-property liveness candidates. **An unreferenced symbol in text is not necessarily dead:** reflection, WinUI binding, assembly-generated code, interop and partial classes require separate review. These candidates are never auto-deleted.

\`dotnet test\`, x86/x64/ARM64 builds, MSI lifecycle/major upgrade, native ARM64 runtime, rendered visual regression checks, NuGet audit and CodeQL remain separate required checks. A CI success is not proof that every UI action has been exercised interactively or that every conceptual screenshot is pixel-perfect.

No capture pixel data, settings content, user path or secrets are uploaded by the audit script. Production capture and settings remain local.
