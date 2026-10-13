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
assert "OpenLanguagePickerSafely);" in options
picker_start = options.index("private void OpenLanguagePickerSafely()")
picker_end = options.index("private void AddInlinePreferenceRow", picker_start)
picker_handler = options[picker_start:picker_end]
assert picker in picker_handler
assert picker_handler.count("catch (Exception exception)") == 2
assert 'StartupDiagnostics.Record("Open Language picker from Settings", exception);' in picker_handler
assert "SetStatus(" in picker_handler and "return;" in picker_handler
assert picker_handler.index(picker) < picker_handler.index("Close();")
assert "OversizedSettings_FallBackSafely" in (ROOT / "tests/Snapvere.UnitTests/CapturePreferencesServiceTests.cs").read_text(encoding="utf-8")
assert "DoesNotFollowSymlinks" in (ROOT / "tests/Snapvere.UnitTests/CaptureHistoryServiceTests.cs").read_text(encoding="utf-8")

# A full-repository text inventory catches potential declaration-only code in
# all production C# files, JavaScript runtime files, and tests. It does not
# attempt to decide whether reflection, WinUI events, COM or generated code
# can reach a symbol. The audit never removes candidates automatically.
all_csharp = {
    path: path.read_text(encoding="utf-8") for path in (*source_files, *test_files)
}
all_browser_js = {
    path: path.read_text(encoding="utf-8") for path in javascript_files
}
browser_html = sorted(path for root in extension_roots for path in root.glob("*.html"))
browser_css = sorted(path for root in extension_roots for path in root.glob("*.css"))
browser_manifests = sorted(root / "manifest.json" for root in extension_roots)
assert len(browser_html) == 8 and len(browser_css) == 8 and all(
    path.is_file() for path in browser_manifests
), "Incomplete browser HTML/CSS/manifest inventory"

# The authoring sources of the four browser variants are intentionally
# duplicated for direct extension packaging. Report parity in the dead-code
# audit rather than misclassifying copies as removable runtime code.
shared_browser_assets = (
    "background.js", "capture.js", "options.js", "popup.js",
    "options.html", "popup.html", "options.css", "popup.css",
)
divergent_browser_assets = []
for name in shared_browser_assets:
    contents = [(root / name).read_bytes() for root in extension_roots]
    if any(content != contents[0] for content in contents[1:]):
        divergent_browser_assets.append(name)
if divergent_browser_assets:
    raise RuntimeError(
        "Shared browser runtime source parity changed: "
        + ", ".join(divergent_browser_assets)
    )

# Reject an already-confirmed no-op Windows tray style assignment. The
# original action-stack margin was immediately overwritten to zero before
# rendering, so it did not affect the UI and should not be reintroduced.
tray_source = (ROOT / "src/Snapvere.App/Services/TrayMenuWindow.cs").read_text(encoding="utf-8")
if "actions.Margin = new Thickness(0);" in tray_source or (
    "var actions = new StackPanel { Spacing = 8, Margin =" in tray_source
):
    raise RuntimeError("Dead tray action-stack margin assignment was reintroduced.")

private_method_declaration = re.compile(
    r"(?m)^\\s*private\\s+(?:(?:static|async|sealed|override|new)\\s+)*"
    r"[A-Za-z_][\\w<>,.?\\[\\]]*(?:\\s+[A-Za-z_][\\w<>,.?\\[\\]]*)*"
    r"\\s+([A-Za-z_]\\w*)\\s*\\("
)
javascript_function_declaration = re.compile(
    r"(?m)^\\s*(?:async\\s+)?function\\s+([A-Za-z_$][\\w$]*)\\s*\\("
)
candidate_private_methods = set()
for path, source in all_csharp.items():
    if path not in source_files:
        continue
    for name in private_method_declaration.findall(source):
        # A reference in the same file is enough to exclude the candidate.
        # For names without a local reference, scan *all* production and tests
        # before listing. This is an informational, conservative heuristic.
        total = sum(
            len(re.findall(r"\\b" + re.escape(name) + r"\\b", text))
            for text in all_csharp.values()
        )
        if total == 1:
            candidate_private_methods.add(f"{path.relative_to(ROOT).as_posix()}::{name}")

candidate_javascript_functions = set()
for path, source in all_browser_js.items():
    for name in javascript_function_declaration.findall(source):
        total = len(re.findall(r"(?<![\\w$])" + re.escape(name) + r"(?![\\w$])", source))
        if total == 1:
            candidate_javascript_functions.add(
                f"{path.relative_to(ROOT).as_posix()}::{name}"
            )

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
print(
    f"Browser source inventory: {len(browser_html)} HTML, {len(browser_css)} CSS, "
    f"{len(browser_manifests)} manifests; {len(shared_browser_assets)} shared asset groups byte-identical."
)
print(f"Declaration-only private method review candidates (heuristic only): {len(candidate_private_methods)}")
for candidate in sorted(candidate_private_methods):
    print(f"  review-method: {candidate}")
print(f"Declaration-only browser JavaScript function review candidates (heuristic only): {len(candidate_javascript_functions)}")
for candidate in sorted(candidate_javascript_functions):
    print(f"  review-js: {candidate}")
print("Confirmed unused tray-stack margin initialization and overwrite removed.")
print("Cross-browser source parity and runtime tests are independently enforced by Extensions CI.")
