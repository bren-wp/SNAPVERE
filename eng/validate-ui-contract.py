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
        ("AutomationLiveSetting.Polite", "_overlayStatusText"),
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
        ),
    )

    print("Validated Windows UI interaction and accessibility contract.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
