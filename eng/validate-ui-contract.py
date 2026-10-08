#!/usr/bin/env python3
"""Validate critical SNAPVERE Windows UI interaction/accessibility contracts."""

from pathlib import Path
from hashlib import sha256
from struct import unpack_from

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(path: str, fragments: tuple[str, ...]) -> None:
    text = read(path)
    for fragment in fragments:
        if fragment not in text:
            raise RuntimeError(f"{path} is missing UI contract fragment: {fragment}")


def require_asset_hash(path: str, expected_hash: str) -> None:
    asset = ROOT / path
    if not asset.is_file():
        raise RuntimeError(f"Required SNAPVERE premium asset is missing: {path}")
    if sha256(asset.read_bytes()).hexdigest() != expected_hash:
        raise RuntimeError(f"SNAPVERE asset differs from the supplied branding ZIP: {path}")


def validate_windows_executable_icons() -> None:
    # Preserve the exact original PNG frames in the actual Windows EXE resources.
    canonical = ROOT / "assets/branding/premium/SNAPVERE.ico"
    if not canonical.is_file():
        raise RuntimeError("Canonical premium Windows EXE icon is missing.")
    icon = canonical.read_bytes()
    if sha256(icon).hexdigest() != "2050742ca22fa046a692eea05c20e2eb1456214648157a8f2e865000c1937a87":
        raise RuntimeError("Canonical premium Windows EXE icon changed unexpectedly.")

    sizes = (16, 32, 48, 128)
    if len(icon) < 6 or unpack_from("<HHH", icon) != (0, 1, len(sizes)):
        raise RuntimeError("Windows EXE icon has an invalid multi-resolution ICONDIR header.")

    next_offset = 6 + len(sizes) * 16
    for index, size in enumerate(sizes):
        metadata = unpack_from("<BBBBHHII", icon, 6 + index * 16)
        width, height, color_count, reserved, planes, bit_count, byte_count, offset = metadata
        if (width, height, color_count, reserved, planes, bit_count) != (size, size, 0, 0, 1, 32):
            raise RuntimeError(f"Windows EXE icon {size}px frame metadata is invalid.")
        if offset != next_offset or offset + byte_count > len(icon):
            raise RuntimeError(f"Windows EXE icon {size}px frame offset is invalid.")

        expected = (ROOT / f"assets/branding/premium/icon-{size}.png").read_bytes()
        if icon[offset:offset + byte_count] != expected:
            raise RuntimeError(f"Windows EXE icon {size}px frame differs from the original PNG.")
        next_offset += byte_count

    if next_offset != len(icon):
        raise RuntimeError("Windows EXE icon contains an unexpected trailing payload.")

    for target in (
        "src/Snapvere.App/Assets/SNAPVERE.ico",
        "src/Snapvere.Setup/Assets/SNAPVERE.ico",
    ):
        asset = ROOT / target
        if not asset.is_file() or asset.read_bytes() != icon:
            raise RuntimeError(f"{target} must match the canonical Windows EXE icon.")


def main() -> int:
    validate_windows_executable_icons()
    # CI visual captures exposed missing Segoe Fluent glyphs as empty boxes.
    # Use the Windows-provided MDL2 glyph font for all active WinUI icon surfaces.
    for icon_surface in (
        "src/Snapvere.App/Services/TrayMenuWindow.cs",
        "src/Snapvere.App/Services/OptionsWindow.cs",
        "src/Snapvere.App/RegionCaptureWindow.cs",
        "src/Snapvere.App/WindowTargetOverlayWindow.cs",
        "src/Snapvere.App/Services/CaptureFeedbackWindow.cs",
    ):
        source = read(icon_surface)
        if 'Segoe Fluent Icons' in source:
            raise RuntimeError(f"{icon_surface} still uses the unavailable Fluent icon font.")
        if 'new FontFamily("Segoe MDL2 Assets")' not in source:
            raise RuntimeError(f"{icon_surface} must use the supported Windows MDL2 glyph font.")

    require("src/Snapvere.App/Services/TrayMenuWindow.cs",
            ('L("CaptureRegion"), "Ctrl + Shift + 1"',))
    require("src/Snapvere.App/Services/OptionsWindow.cs",
            ("AddInlinePreferenceRow(0,",
             "railStack.Children.Add(_aboutTab);"))
    require_asset_hash(
        "src/Snapvere.App/Assets/SNAPVERE-app-icon-32.png",
        "b4139f4baa33c22921834272d4f62f403af178d9f80364f1035eb3228d29c709",
    )
    require_asset_hash(
        "src/Snapvere.Setup/Assets/SNAPVERE-app-icon-128.png",
        "e49808dad22d4afb68668b1d85ae7804549ed1a79dd5fe9b802e32be8ba49aad",
    )
    require(
        "src/Snapvere.App/Services/Win32TrayIconService.cs",
        ("SNAPVERE-app-icon-32.png", "GdipCreateBitmapFromFile", "GdipCreateHICONFromBitmap"),
    )
    require(
        "src/Snapvere.App/Snapvere.App.csproj",
        ('<Content Include="Assets\\SNAPVERE-app-icon-32.png">', '<ApplicationIcon>Assets\\SNAPVERE.ico</ApplicationIcon>'),
    )
    require(
        "src/Snapvere.Setup/Snapvere.Setup.csproj",
        ('LogicalName="Snapvere.Brand.AppIcon"', '<ApplicationIcon>Assets\\SNAPVERE.ico</ApplicationIcon>'),
    )

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
            "VerticalScrollBarVisibility = ScrollBarVisibility.Auto",
            "RegionOverlayLayoutPolicy.FitToolPaletteHeight(",
            "_toolPalette.Height = toolPaletteHeight",
            "totalHeight - toolPaletteHeight - 8d",
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
            "content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) })",
            "Grid.SetColumn(copy, 1)",
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
            "private const int FlyoutWidth = 420;",
            "private const int FlyoutHeight = 488;",
            "private const int FlyoutEdgeMargin = 4;",
            "TrayPopupPlacementPolicy.Place(",
            "MinHeight = recording ? 54 : primary ? 54 : 50",
            "SnapvereBrand.CreateMark(50)",
            "CreateFooterIconButton",
            "CreateFooterLinkButton",
        ),
    )

    tray_text = read("src/Snapvere.App/Services/TrayMenuWindow.cs")
    if '"Ctrl + Shift + 4"' in tray_text:
        raise RuntimeError("Recording must not advertise an unregistered Ctrl+Shift+4 shortcut.")
    recording_button = tray_text.split('L(_screenRecordingActive ? "StopScreenRecording" : "StartScreenRecording"),', 1)
    if len(recording_button) != 2 or not recording_button[1].lstrip().startswith("string.Empty,"):
        raise RuntimeError("Recording action must retain a blank shortcut hint until a hotkey is implemented.")

    require(
        "src/Snapvere.App/SnapvereBrand.cs",
        (
            'ObsidianHex = "#070912"',
            'SurfaceHex = "#111526"',
            'SlateHex = "#161B2E"',
            'VioletHex = "#7655F6"',
            'LavenderHex = "#A48BFF"',
            'IceHex = "#80E1E5"',
            'StrongTextHex = "#F8F9FF"',
            'MutedTextHex = "#8E9AB6"',
            "CreateMark(double size)",
            'ProductionMarkFile = "SNAPVERE-app-icon-48.png"',
            "new BitmapImage(",
            "CreateWordmark(double fontSize",
        ),
    )
    require(
        "src/Snapvere.App/Services/OptionsWindow.cs",
        (
            "SnapvereBrand.CreateMark(42)",
            "new GridLength(222)",
            "Tvoje snimke ostaju lokalne",
            "railStack.Children.Add(_generalTab)",
            "railStack.Children.Add(_preferencesTab)",
            "railStack.Children.Add(_recordingTab)",
            "railStack.Children.Add(_shortcutsTab)",
            "railStack.Children.Add(_recentTab)",
            "railStack.Children.Add(_aboutTab)",
            "AddInlinePreferenceRow(0,",
            "_recordingPanel.Children.Add(_recordingActions)",
            "Ctrl + Shift + {i + 1}",
        ),
    )
    options_source = read("src/Snapvere.App/Services/OptionsWindow.cs")
    if "BuildSettingCard(" in options_source or "AddPreferenceCard(" in options_source:
        raise RuntimeError("Old oversized Settings cards must not remain.")

    require(
        "src/Snapvere.Setup/SetupForm.cs",
        (
            "Color.FromArgb(7, 9, 18)",
            "Color.FromArgb(118, 85, 246)",
            "Color.FromArgb(164, 139, 255)",
            "Color.FromArgb(128, 225, 229)",
            "new Bitmap(decoded)",
            "e.Graphics.DrawImage(_brandImage, new Rectangle(0, 0, Width, Height))",
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
