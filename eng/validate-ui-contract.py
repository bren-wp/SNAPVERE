#!/usr/bin/env python3
"""Validate critical SNAPVERE Windows UI interaction/accessibility contracts."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(path: str, fragments: tuple[str, ...]) -> None:
    text = read(path)
    for fragment in fragments:
        if fragment not in text:
            raise RuntimeError(f"{path} is missing UI contract fragment: {fragment}")


def main() -> int:
    for path in (
        "src/Snapvere.App/Services/OptionsWindow.cs",
        "src/Snapvere.App/Services/LanguagePickerWindow.cs",
        "src/Snapvere.App/Services/AboutWindow.cs",
        "src/Snapvere.App/Services/TrayMenuWindow.cs",
        "src/Snapvere.App/Services/RecordingControllerWindow.cs",
    ):
        require(path, ("KeyDown +=", "VirtualKey.Escape", "e.Handled = true"))

    for path in (
        "src/Snapvere.App/Services/OptionsWindow.cs",
        "src/Snapvere.App/RegionCaptureWindow.cs",
        "src/Snapvere.App/Services/RecordingControllerWindow.cs",
    ):
        require(path, ("using Microsoft.UI.Xaml.Automation.Peers;",))

    require(
        "src/Snapvere.App/Services/OptionsWindow.cs",
        ("AutomationLiveSetting.Polite", "_statusText"),
    )
    require(
        "src/Snapvere.App/RegionCaptureWindow.cs",
        (
            "AutomationLiveSetting.Polite",
            "_overlayStatusText",
            "ApplyResponsiveOverlayChrome(e.NewSize.Width, e.NewSize.Height)",
            "_captureHint.Width = Math.Min(460d, availableWidth)",
            "_overlayStatus.MaxWidth = Math.Min(620d, availableWidth)",
            "hint.TextWrapping = TextWrapping.Wrap",
            "hint.MaxLines = 3",
        ),
    )
    require(
        "src/Snapvere.App/WindowTargetOverlayWindow.cs",
        (
            "ApplyResponsiveOverlayChrome(e.NewSize.Width, e.NewSize.Height)",
            "_hint.MaxWidth = Math.Min(520d, availableWidth)",
            "_targetText.MaxWidth = Math.Min(",
            "description.TextWrapping = TextWrapping.Wrap",
            "description.MaxLines = 3",
        ),
    )
    require(
        "src/Snapvere.App/Services/RecordingControllerWindow.cs",
        (
            "AutomationLiveSetting.Polite",
            "new ProgressRing",
            "IsActive = true",
            "CreateStopGlyph()",
            "Finishing screen recording",
            "ToolTipService.SetToolTip",
            "_stopButton.Focus(FocusState.Programmatic)",
        ),
    )

    require(
        "src/Snapvere.App/Services/AboutWindow.cs",
        (
            "if (!_dispatcherQueue.TryEnqueue(RefreshLanguageSafely))",
            "About language refresh was not queued because the UI dispatcher is shutting down.",
        ),
    )
    require(
        "src/Snapvere.App/Services/TrayMenuWindow.cs",
        (
            'CloseForActionBestEffort("Close tray menu for command")',
            'CloseForActionBestEffort("Close tray menu for action")',
            "StartupDiagnostics.Record(operation, exception);",
            "private const int FlyoutWidth = 392;",
            "private const int FlyoutHeight = 542;",
            "private const int FlyoutEdgeMargin = 4;",
            "TrayPopupPlacementPolicy.Place(",
            "MinHeight = primary ? 46 : 38",
            "hint.TextAlignment = TextAlignment.Right",
        ),
    )

    require(
        "src/Snapvere.App/Services/CaptureFeedbackWindow.cs",
        (
            "OpenCaptureFolderFailed",
            "Mapa snimki nije dostupna",
            "Capture folder is unavailable",
            "Postojeće snimke nisu mijenjane",
            "Existing captures were not changed",
        ),
    )
    require(
        "src/Snapvere.App/CaptureCenterWindow.cs",
        (
            "public void ShowCaptureFolderOpenFailure()",
            "ShowCaptureFeedback(CaptureFeedbackKind.OpenCaptureFolderFailed)",
        ),
    )

    app_text = read("src/Snapvere.App/App.xaml.cs")
    for fragment in (
        "if (!queue.TryEnqueue",
        "was not queued because the UI dispatcher is shutting down.",
        "CloseStandaloneLanguageBestEffort();",
        "LanguagePickerWindow.CloseStandalone();",
        "StartupDiagnostics.Record(\"Open capture folder from tray\", exception);",
        "_window?.ShowCaptureFolderOpenFailure();",
        'command => ExecuteUiBoundary(',
        '$"Tray menu command {command}"',
        '"Tray menu recent captures"',
        '"Tray menu language"',
    ):
        if fragment not in app_text:
            raise RuntimeError(f"src/Snapvere.App/App.xaml.cs is missing UI contract fragment: {fragment}")

    if app_text.count("LanguagePickerWindow.CloseStandalone();") != 1:
        raise RuntimeError(
            "Standalone Language cleanup must have exactly one direct implementation call, inside the best-effort helper."
        )

    helper_start = app_text.index("private static void CloseStandaloneLanguageBestEffort()")
    helper_end = app_text.index("private void ExecuteTrayCommand", helper_start)
    helper_text = app_text[helper_start:helper_end]
    if "LanguagePickerWindow.CloseStandalone();" not in helper_text:
        raise RuntimeError("Language cleanup helper must call LanguagePickerWindow.CloseStandalone().")
    if helper_text.count("CloseStandaloneLanguageBestEffort();") != 0:
        raise RuntimeError("Language cleanup helper must not recursively call itself.")

    shutdown_start = app_text.index("private void OnMainWindowClosed")
    shutdown_text = app_text[shutdown_start:]
    if "CloseStandaloneLanguageBestEffort();" not in shutdown_text:
        raise RuntimeError("Final shutdown must use the contained Language cleanup helper.")

    print("Validated Windows UI interaction and accessibility contract.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
