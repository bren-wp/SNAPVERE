using System.Collections.ObjectModel;
using System.Globalization;

namespace Snapvere.Shared;

public sealed record SnapvereLanguage(string Code, string EnglishName, string NativeName);

/// <summary>
/// Lightweight built-in localization catalog. English is the canonical fallback.
/// No files, network calls, timers or background services are used at runtime.
/// </summary>
public static class SnapvereLocalization
{
    public const string DefaultLanguageCode = "en";

    private static readonly ReadOnlyCollection<SnapvereLanguage> Languages = new(
    [
        new("en", "English", "English"),
        new("hr", "Croatian", "Hrvatski")
    ]);

    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CaptureRegion"] = "Capture region",
        ["CaptureWindow"] = "Capture window",
        ["CaptureScreen"] = "Capture screen",
        ["StartScreenRecording"] = "Start screen recording",
        ["StopScreenRecording"] = "Stop screen recording",
        ["ScreenRecordingActive"] = "Screen recording active",
        ["RecordingSavedTitle"] = "Screen recording saved",
        ["RecordingSavedMessage"] = "The MP4 recording was saved locally in your SNAPVERE capture folder. Audio is not included in this recording mode.",
        ["RecordingFailedTitle"] = "Screen recording stopped",
        ["RecordingFailedMessage"] = "SNAPVERE could not complete the screen recording. Try again after closing protected or full-screen content.",
        ["RecordingUnsupportedTitle"] = "Screen recording is unavailable",
        ["RecordingUnsupportedMessage"] = "This Windows version or graphics environment does not support SNAPVERE screen recording.",
        ["OpenCaptureFolder"] = "Open capture folder",
        ["OptionsRecent"] = "Options & recent captures",
        ["Language"] = "Language",
        ["About"] = "About SNAPVERE",
        ["Exit"] = "Exit",
        ["Ready"] = "Ready",
        ["Copy"] = "Copy",
        ["Save"] = "Save",
        ["Close"] = "Close",
        ["Cancel"] = "Cancel",
        ["Settings"] = "Settings",
        ["RecentCaptures"] = "Recent captures",
        ["StartWithWindows"] = "Start SNAPVERE with Windows",
        ["IncludeCursor"] = "Include cursor on capture",
        ["ChooseLanguage"] = "Choose language",
        ["LanguageSaved"] = "Language saved. SNAPVERE now uses this language.",
        ["LocalFirst"] = "Local-first",
        ["NoUploads"] = "No account, telemetry or cloud upload required.",
        ["ProductWebsite"] = "Official product website",
        ["Developer"] = "Developed by Brendigo",
        ["Support"] = "Support",
        ["Privacy"] = "Privacy",
        ["Terms"] = "Terms",
        ["Install"] = "Install",
        ["Remove"] = "Remove",
        ["Finish"] = "Finish",
        ["DesktopShortcut"] = "Desktop shortcut",
        ["StartMenuShortcut"] = "Start menu shortcut",
        ["LaunchOnWindowsStartup"] = "Start SNAPVERE automatically with Windows",
        ["AcceptLicense"] = "I have read and accept the commercial license terms",
        ["RegionCaptureTitle"] = "SNAPVERE — Region Capture",
        ["WindowCaptureTitle"] = "SNAPVERE — Window Capture",
        ["RegionMove"] = "Move",
        ["RegionMoveHelp"] = "Move or resize selection",
        ["RegionPen"] = "Pen",
        ["RegionPenHelp"] = "Freehand pen",
        ["RegionLine"] = "Line",
        ["RegionLineHelp"] = "Straight line",
        ["RegionArrow"] = "Arrow",
        ["RegionArrowHelp"] = "Arrow",
        ["RegionBox"] = "Box",
        ["RegionBoxHelp"] = "Rectangle",
        ["RegionHighlight"] = "Mark",
        ["RegionHighlightHelp"] = "Highlighter",
        ["Undo"] = "Undo",
        ["UndoHelp"] = "Undo last annotation",
        ["CopySelectionHelp"] = "Copy selection to clipboard",
        ["SavePngHelp"] = "Save PNG",
        ["CancelCaptureHelp"] = "Cancel capture",
        ["RegionHint"] = "Drag to select · annotate inline · Enter save · Esc cancel",
        ["SavingRegion"] = "Saving selected region…",
        ["CopyingSelection"] = "Copying selection to clipboard…",
        ["Working"] = "Working",
        ["RegionKeyboardInput"] = "Region capture keyboard input",
        ["WindowKeyboardInput"] = "Window capture keyboard input",
        ["WindowHintTitle"] = "Window Capture",
        ["WindowHint"] = "Point to a window · click to capture · Esc to cancel",
        ["ResizeTopLeft"] = "Resize top left",
        ["ResizeTop"] = "Resize top",
        ["ResizeTopRight"] = "Resize top right",
        ["ResizeRight"] = "Resize right",
        ["ResizeBottomRight"] = "Resize bottom right",
        ["ResizeBottom"] = "Resize bottom",
        ["ResizeBottomLeft"] = "Resize bottom left",
        ["ResizeLeft"] = "Resize left",
        ["ColorCoral"] = "Coral",
        ["ColorAmber"] = "Amber",
        ["ColorMint"] = "Mint",
        ["ColorIndigo"] = "Indigo",
        ["AnnotationColor"] = "annotation color",
        ["RegionAccessDenied"] = "Windows denied access to the capture folder or clipboard.",
        ["RegionIoFailure"] = "The selected region was captured, but the PNG file or clipboard stream could not be completed.",
        ["RegionInvalidSelection"] = "The selected region is no longer valid. Select the region again.",
        ["RegionCaptureFailed"] = "SNAPVERE could not complete the region capture. Press Esc and try again.",
        ["CaptureSaveFailedTitle"] = "Capture could not be saved",
        ["CaptureSaveAccessDenied"] = "The capture was created, but Windows denied access to the SNAPVERE capture folder. Check Pictures permissions and try again.",
        ["CaptureStorageFull"] = "The capture was created, but the storage device is full. Free some space and try again.",
        ["CaptureSaveFailed"] = "The capture was created, but SNAPVERE could not write the PNG file. Check the capture folder and try again.",
        ["PreferencesStoredLocally"] = "Preferences are stored locally for this Windows account.",
        ["Startup"] = "Startup",
        ["StartupDescription"] = "Launch SNAPVERE quietly in the notification area after Windows sign-in.",
        ["Capture"] = "Capture",
        ["Refresh"] = "Refresh",
        ["StartupEnabled"] = "Windows startup enabled — SNAPVERE will start quietly in the tray.",
        ["StartupDisabled"] = "Windows startup disabled.",
        ["CursorEnabled"] = "Cursor capture enabled for supported capture paths.",
        ["CursorDisabled"] = "Cursor capture disabled.",
        ["OneLocalCapture"] = "1 local capture",
        ["LocalCaptureCount"] = "Local captures: {0}",
        ["LocalHistoryUnavailable"] = "Local history unavailable",
        ["NoCapturesYet"] = "No captures yet",
        ["EmptyHistoryHelp"] = "Use Print Screen or the tray icon to create your first region capture.",
        ["OpenCaptureNamed"] = "Open {0}",
        ["CopyPath"] = "Copy path",
        ["CapturePathCopied"] = "Capture path copied to the clipboard.",
        ["CapturePathCopyFailed"] = "The capture path could not be copied. Try again.",
        ["OpenCaptureFailed"] = "Windows could not open this capture. Check the file and try again.",
        ["OpenCaptureFolderFailed"] = "Windows could not open the capture folder. Check folder permissions and try again.",
        ["CaptureUnavailable"] = "That capture is no longer available at its original path.",
        ["Off"] = "Off",
        ["On"] = "On",
        ["LanguagePickerIntro"] = "English is the default language. Choose any supported language below; the preference is stored only on this PC.",
        ["AboutLocalFirstTitle"] = "Local-first capture for Windows",
        ["AboutCaptureDescription"] = "Region, window and screen capture without cloud dependency.",
        ["AboutPrivacyDescription"] = "Screenshots stay on your device unless you explicitly copy, save or share them through Windows.",
        ["CommercialSoftware"] = "SNAPVERE commercial software • © Brendigo"
    };

    public static IReadOnlyList<SnapvereLanguage> SupportedLanguages => Languages;

    public static IReadOnlyCollection<string> CanonicalKeys => English.Keys.ToArray();

    public static bool HasDedicatedTranslation(string key, string? languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var language = NormalizeLanguageCode(languageCode);
        if (string.Equals(language, DefaultLanguageCode, StringComparison.Ordinal))
        {
            return English.ContainsKey(key);
        }

        return English.ContainsKey(key) && TranslateCore(language, key) is not null;
    }

    public static string NormalizeLanguageCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return DefaultLanguageCode;
        }

        var trimmed = code.Trim();
        var exact = Languages.FirstOrDefault(language =>
            string.Equals(language.Code, trimmed, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact.Code;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(trimmed);
            var neutral = culture.TwoLetterISOLanguageName;
            var neutralMatch = Languages.FirstOrDefault(language =>
                string.Equals(language.Code, neutral, StringComparison.OrdinalIgnoreCase));
            return neutralMatch?.Code ?? DefaultLanguageCode;
        }
        catch (CultureNotFoundException)
        {
            return DefaultLanguageCode;
        }
    }

    public static string T(string key, string? languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var language = NormalizeLanguageCode(languageCode);
        if (string.Equals(language, DefaultLanguageCode, StringComparison.Ordinal))
        {
            return English.TryGetValue(key, out var english) ? english : key;
        }

        var translated = TranslateCore(language, key);
        return translated ?? (English.TryGetValue(key, out var fallback) ? fallback : key);
    }

    private static string? TranslateCore(string language, string key)
        => (language, key) switch
        {
            ("hr", "CaptureRegion") => "Snimi područje",
            ("hr", "CaptureWindow") => "Snimi prozor",
            ("hr", "CaptureScreen") => "Snimi zaslon",
            ("hr", "StartScreenRecording") => "Pokreni snimanje zaslona",
            ("hr", "StopScreenRecording") => "Zaustavi snimanje zaslona",
            ("hr", "ScreenRecordingActive") => "Snimanje zaslona je aktivno",
            ("hr", "RecordingSavedTitle") => "Snimanje zaslona je spremljeno",
            ("hr", "RecordingSavedMessage") => "MP4 snimka spremljena je lokalno u SNAPVERE mapu snimki. Zvuk nije uključen u ovaj način snimanja.",
            ("hr", "RecordingFailedTitle") => "Snimanje zaslona je zaustavljeno",
            ("hr", "RecordingFailedMessage") => "SNAPVERE nije mogao dovršiti snimanje zaslona. Pokušajte ponovno nakon zatvaranja zaštićenog ili full-screen sadržaja.",
            ("hr", "RecordingUnsupportedTitle") => "Snimanje zaslona nije dostupno",
            ("hr", "RecordingUnsupportedMessage") => "Ova verzija Windowsa ili grafičko okruženje ne podržava SNAPVERE snimanje zaslona.",
            ("hr", "OpenCaptureFolder") => "Otvori mapu snimki",
            ("hr", "OptionsRecent") => "Postavke i nedavne snimke",
            ("hr", "Language") => "Jezik",
            ("hr", "About") => "O programu SNAPVERE",
            ("hr", "Exit") => "Izlaz",
            ("hr", "Ready") => "Spremno",
            ("hr", "Copy") => "Kopiraj",
            ("hr", "Save") => "Spremi",
            ("hr", "Close") => "Zatvori",
            ("hr", "Cancel") => "Odustani",
            ("hr", "Settings") => "Postavke",
            ("hr", "RecentCaptures") => "Nedavne snimke",
            ("hr", "StartWithWindows") => "Pokreni SNAPVERE s Windowsima",
            ("hr", "IncludeCursor") => "Uključi pokazivač u snimku",
            ("hr", "ChooseLanguage") => "Odaberi jezik",
            ("hr", "LanguageSaved") => "Jezik je spremljen. SNAPVERE sada koristi ovaj jezik.",
            ("hr", "LocalFirst") => "Lokalno i privatno",
            ("hr", "NoUploads") => "Nije potreban račun, telemetrija ni prijenos u oblak.",
            ("hr", "ProductWebsite") => "Službena stranica proizvoda",
            ("hr", "Developer") => "Izradio Brendigo",
            ("hr", "Support") => "Podrška",
            ("hr", "Privacy") => "Privatnost",
            ("hr", "Terms") => "Uvjeti",
            ("hr", "Install") => "Instaliraj",
            ("hr", "Remove") => "Ukloni",
            ("hr", "Finish") => "Završi",
            ("hr", "DesktopShortcut") => "Prečac na radnoj površini",
            ("hr", "StartMenuShortcut") => "Prečac u izborniku Start",
            ("hr", "LaunchOnWindowsStartup") => "Automatski pokreni SNAPVERE s Windowsima",
            ("hr", "AcceptLicense") => "Pročitao/la sam i prihvaćam uvjete komercijalne licence",
            ("hr", "RegionCaptureTitle") => "SNAPVERE — Snimanje područja",
            ("hr", "WindowCaptureTitle") => "SNAPVERE — Snimanje prozora",
            ("hr", "RegionMove") => "Pomakni",
            ("hr", "RegionMoveHelp") => "Pomakni ili promijeni veličinu odabira",
            ("hr", "RegionPen") => "Olovka",
            ("hr", "RegionPenHelp") => "Slobodno crtanje olovkom",
            ("hr", "RegionLine") => "Linija",
            ("hr", "RegionLineHelp") => "Ravna linija",
            ("hr", "RegionArrow") => "Strelica",
            ("hr", "RegionArrowHelp") => "Strelica",
            ("hr", "RegionBox") => "Okvir",
            ("hr", "RegionBoxHelp") => "Pravokutnik",
            ("hr", "RegionHighlight") => "Označi",
            ("hr", "RegionHighlightHelp") => "Marker za isticanje",
            ("hr", "Undo") => "Poništi",
            ("hr", "UndoHelp") => "Poništi zadnju oznaku",
            ("hr", "CopySelectionHelp") => "Kopiraj odabir u međuspremnik",
            ("hr", "SavePngHelp") => "Spremi PNG",
            ("hr", "CancelCaptureHelp") => "Odustani od snimanja",
            ("hr", "RegionHint") => "Povuci za odabir · označi izravno · Enter spremi · Esc odustani",
            ("hr", "SavingRegion") => "Spremanje odabranog područja…",
            ("hr", "CopyingSelection") => "Kopiranje odabira u međuspremnik…",
            ("hr", "Working") => "Obrada",
            ("hr", "RegionKeyboardInput") => "Tipkovnički unos za snimanje područja",
            ("hr", "WindowKeyboardInput") => "Tipkovnički unos za snimanje prozora",
            ("hr", "WindowHintTitle") => "Snimanje prozora",
            ("hr", "WindowHint") => "Pokaži na prozor · klikni za snimanje · Esc za odustajanje",
            ("hr", "ResizeTopLeft") => "Promijeni veličinu gore lijevo",
            ("hr", "ResizeTop") => "Promijeni veličinu prema gore",
            ("hr", "ResizeTopRight") => "Promijeni veličinu gore desno",
            ("hr", "ResizeRight") => "Promijeni veličinu prema desno",
            ("hr", "ResizeBottomRight") => "Promijeni veličinu dolje desno",
            ("hr", "ResizeBottom") => "Promijeni veličinu prema dolje",
            ("hr", "ResizeBottomLeft") => "Promijeni veličinu dolje lijevo",
            ("hr", "ResizeLeft") => "Promijeni veličinu prema lijevo",
            ("hr", "ColorCoral") => "Koraljna",
            ("hr", "ColorAmber") => "Jantarna",
            ("hr", "ColorMint") => "Mint",
            ("hr", "ColorIndigo") => "Indigo",
            ("hr", "AnnotationColor") => "boja oznake",
            ("hr", "RegionAccessDenied") => "Windows je odbio pristup mapi snimki ili međuspremniku.",
            ("hr", "RegionIoFailure") => "Odabrano područje je snimljeno, ali PNG datoteku ili prijenos u međuspremnik nije bilo moguće dovršiti.",
            ("hr", "RegionInvalidSelection") => "Odabrano područje više nije valjano. Ponovno odaberite područje.",
            ("hr", "RegionCaptureFailed") => "SNAPVERE nije mogao dovršiti snimanje područja. Pritisnite Esc i pokušajte ponovno.",
            ("hr", "CaptureSaveFailedTitle") => "Snimku nije moguće spremiti",
            ("hr", "CaptureSaveAccessDenied") => "Snimka je izrađena, ali Windows je odbio pristup SNAPVERE mapi snimki. Provjerite dozvole za mapu Slike i pokušajte ponovno.",
            ("hr", "CaptureStorageFull") => "Snimka je izrađena, ali na uređaju za pohranu nema dovoljno prostora. Oslobodite prostor i pokušajte ponovno.",
            ("hr", "CaptureSaveFailed") => "Snimka je izrađena, ali SNAPVERE nije mogao zapisati PNG datoteku. Provjerite mapu snimki i pokušajte ponovno.",
            ("hr", "PreferencesStoredLocally") => "Postavke su spremljene lokalno za ovaj Windows račun.",
            ("hr", "Startup") => "Pokretanje",
            ("hr", "StartupDescription") => "Pokreće SNAPVERE tiho u području obavijesti nakon prijave u Windows.",
            ("hr", "Capture") => "Snimanje",
            ("hr", "Refresh") => "Osvježi",
            ("hr", "StartupEnabled") => "Automatsko pokretanje s Windowsima je uključeno.",
            ("hr", "StartupDisabled") => "Automatsko pokretanje s Windowsima je isključeno.",
            ("hr", "CursorEnabled") => "Snimanje pokazivača je uključeno za podržane načine snimanja.",
            ("hr", "CursorDisabled") => "Snimanje pokazivača je isključeno.",
            ("hr", "OneLocalCapture") => "1 lokalna snimka",
            ("hr", "LocalCaptureCount") => "Lokalne snimke: {0}",
            ("hr", "LocalHistoryUnavailable") => "Lokalna povijest nije dostupna",
            ("hr", "NoCapturesYet") => "Još nema snimki",
            ("hr", "EmptyHistoryHelp") => "Koristi Print Screen ili ikonu u području obavijesti za prvu snimku područja.",
            ("hr", "OpenCaptureNamed") => "Otvori {0}",
            ("hr", "CopyPath") => "Kopiraj putanju",
            ("hr", "CapturePathCopied") => "Putanja snimke kopirana je u međuspremnik.",
            ("hr", "CapturePathCopyFailed") => "Putanju snimke nije moguće kopirati. Pokušajte ponovno.",
            ("hr", "OpenCaptureFailed") => "Windows ne može otvoriti ovu snimku. Provjerite datoteku i pokušajte ponovno.",
            ("hr", "OpenCaptureFolderFailed") => "Windows ne može otvoriti mapu snimki. Provjerite dozvole mape i pokušajte ponovno.",
            ("hr", "CaptureUnavailable") => "Ta snimka više nije dostupna na izvornoj lokaciji.",
            ("hr", "Off") => "Isključeno",
            ("hr", "On") => "Uključeno",
            ("hr", "LanguagePickerIntro") => "Engleski je zadani jezik. Odaberi bilo koji podržani jezik; postavka se sprema samo na ovom računalu.",
            ("hr", "AboutLocalFirstTitle") => "Lokalno snimanje za Windows",
            ("hr", "AboutCaptureDescription") => "Snimanje područja, prozora i zaslona bez ovisnosti o oblaku.",
            ("hr", "AboutPrivacyDescription") => "Snimke ostaju na uređaju osim ako ih izričito ne kopiraš, spremiš ili podijeliš putem Windowsa.",
            ("hr", "CommercialSoftware") => "SNAPVERE komercijalni softver • © Brendigo",
            _ => null
        };
}
