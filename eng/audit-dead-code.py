#!/usr/bin/env python3
"""Read-only, repository-wide dead-code inventory and privacy regression checks.

A private member only mentioned at its declaration is a review candidate, not
proof of dead code: generated sources, partial classes, reflection, and
interop are not fully modeled by a textual scan. Never auto-delete candidates.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]

source_files = sorted((ROOT / "src").rglob("*.cs"))
test_files = sorted((ROOT / "tests").rglob("*Tests.cs"))
extension_roots = [ROOT / "ekstenzije" / name for name in ("chrome", "edge", "opera", "firefox")]
javascript_files = sorted(path for root in extension_roots for path in root.glob("*.js"))
assert source_files and test_files and len(javascript_files) == 16, "Incomplete source inventory"

removed_members = {
    "src/Snapvere.App/Services/OptionsWindow.cs": ("Canvas",),
    "src/Snapvere.App/Services/TrayMenuWindow.cs": ("Accent", "Transparent"),
    "src/Snapvere.App/Services/RecordingControllerWindow.cs": ("RecordingRed",),
}
property_declaration = re.compile(r"(?m)^\s*private\s+static\s+\w+\s+(\w+)\s*=>")
for path, names in removed_members.items():
    text = (ROOT / path).read_text(encoding="utf-8")
    found = set(property_declaration.findall(text))
    for name in names:
        if name in found:
            raise RuntimeError(f"Confirmed dead UI member reintroduced: {path}::{name}")

preferences = (ROOT / "src/Snapvere.Application/Capture/CapturePreferencesService.cs").read_text(encoding="utf-8")
history = (ROOT / "src/Snapvere.Application/Capture/CaptureHistoryService.cs").read_text(encoding="utf-8")
options = (ROOT / "src/Snapvere.App/Services/OptionsWindow.cs").read_text(encoding="utf-8")
assert "MaximumSettingsFileBytes" in preferences
assert "stream.ReadExactly(json)" in preferences
assert "File.ReadAllText(_settingsPath)" not in preferences
assert "FileAttributes.ReparsePoint" in history
assert "IsCurrentCaptureFile(CaptureHistoryItem item)" in history
assert options.count("_history.IsCurrentCaptureFile(capture)") == 2
picker = "LanguagePickerWindow.ShowStandalone(_preferences);"
assert picker in options and "Close();" in options[options.index(picker):options.index(picker) + 110]
assert "OversizedSettings_FallBackSafely" in (ROOT / "tests/Snapvere.UnitTests/CapturePreferencesServiceTests.cs").read_text(encoding="utf-8")
assert "DoesNotFollowSymlinks" in (ROOT / "tests/Snapvere.UnitTests/CaptureHistoryServiceTests.cs").read_text(encoding="utf-8")

# Enumerate the entire C# source tree. Single-file textual liveness analysis
# can be wrong, so the following output is informational and non-destructive.
candidates = set()
for path in source_files:
    source = path.read_text(encoding="utf-8")
    for name in property_declaration.findall(source):
        if len(re.findall(r"\b" + re.escape(name) + r"\b", source)) == 1:
            candidates.add(f"{path.relative_to(ROOT).as_posix()}::{name}")

print(
    f"SNAPVERE source audit: {len(source_files)} production C# sources, "
    f"{len(test_files)} unit-test sources, {len(javascript_files)} browser JS sources."
)
print("Confirmed: 4 dead UI aliases removed; bounded local settings; capture-history link isolation.")
print(f"Additional private static alias review candidates (heuristic only): {len(candidates)}")
for candidate in sorted(candidates):
    print(f"  review: {candidate}")
print("Cross-browser source parity and runtime tests are independently enforced by Extensions CI.")
