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
        new("hr", "Croatian", "Hrvatski"),
        new("de", "German", "Deutsch"),
        new("fr", "French", "Français"),
        new("es", "Spanish", "Español"),
        new("it", "Italian", "Italiano"),
        new("pt", "Portuguese", "Português"),
        new("nl", "Dutch", "Nederlands"),
        new("pl", "Polish", "Polski"),
        new("cs", "Czech", "Čeština"),
        new("sk", "Slovak", "Slovenčina"),
        new("sl", "Slovenian", "Slovenščina"),
        new("hu", "Hungarian", "Magyar"),
        new("ro", "Romanian", "Română"),
        new("bg", "Bulgarian", "Български"),
        new("el", "Greek", "Ελληνικά"),
        new("sv", "Swedish", "Svenska"),
        new("da", "Danish", "Dansk"),
        new("no", "Norwegian", "Norsk"),
        new("fi", "Finnish", "Suomi"),
        new("et", "Estonian", "Eesti"),
        new("lv", "Latvian", "Latviešu"),
        new("lt", "Lithuanian", "Lietuvių"),
        new("uk", "Ukrainian", "Українська"),
        new("tr", "Turkish", "Türkçe"),
        new("ja", "Japanese", "日本語"),
        new("ko", "Korean", "한국어"),
        new("zh-CN", "Chinese (Simplified)", "简体中文")
    ]);

    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CaptureRegion"] = "Capture region",
        ["CaptureWindow"] = "Capture window",
        ["CaptureScreen"] = "Capture screen",
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
        ["LanguageSaved"] = "Language saved. New SNAPVERE surfaces will use this language.",
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
        ["PreferencesStoredLocally"] = "Preferences are stored locally for this Windows account.",
        ["ImplementedSettingsOnly"] = "Only implemented, locally persisted settings are shown here.",
        ["Startup"] = "Startup",
        ["StartupDescription"] = "Launch SNAPVERE quietly in the notification area after Windows sign-in.",
        ["Capture"] = "Capture",
        ["CursorDescription"] = "Include the pointer when the active capture backend supports cursor composition.",
        ["CurrentLanguageDescription"] = "Current: {0}. English is the default fallback language.",
        ["Refresh"] = "Refresh",
        ["PreferenceReadFailed"] = "Could not read one or more preferences: {0}",
        ["StartupEnabled"] = "Windows startup enabled — SNAPVERE will start quietly in the tray.",
        ["StartupDisabled"] = "Windows startup disabled.",
        ["StartupChangeFailed"] = "Windows startup setting could not be changed: {0}",
        ["CursorEnabled"] = "Cursor capture enabled for supported capture paths.",
        ["CursorDisabled"] = "Cursor capture disabled.",
        ["CursorSaveFailed"] = "Cursor preference could not be saved: {0}",
        ["OneLocalCapture"] = "1 local capture",
        ["LocalCaptureCount"] = "Local captures: {0}",
        ["LocalHistoryUnavailable"] = "Local history unavailable",
        ["NoCapturesYet"] = "No captures yet",
        ["EmptyHistoryHelp"] = "Use Print Screen or the tray icon to create your first region capture.",
        ["OpenCaptureNamed"] = "Open {0}",
        ["CaptureUnavailable"] = "That capture is no longer available at its original path.",
        ["OpenCaptureFailed"] = "Windows could not open that capture: {0}",
        ["OpenCaptureFolderFailed"] = "Windows could not open the capture folder: {0}",
        ["Off"] = "Off",
        ["On"] = "On",
        ["LanguagePickerIntro"] = "English is the default language. Choose any supported language below; the preference is stored only on this PC.",
        ["LanguageSaveFailed"] = "Could not save language preference: {0}",
        ["AboutLocalFirstTitle"] = "Local-first capture for Windows",
        ["AboutCaptureDescription"] = "Region, window and screen capture without cloud dependency.",
        ["AboutPrivacyDescription"] = "Screenshots stay on your device unless you explicitly copy, save or share them through Windows.",
        ["CommercialSoftware"] = "SNAPVERE commercial software • © Brendigo"
    };

    public static IReadOnlyList<SnapvereLanguage> SupportedLanguages => Languages;

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
            ("hr", "LanguageSaved") => "Jezik je spremljen. Novi SNAPVERE prozori koristit će ovaj jezik.",
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
            ("hr", "PreferencesStoredLocally") => "Postavke su spremljene lokalno za ovaj Windows račun.",
            ("hr", "ImplementedSettingsOnly") => "Prikazane su samo postavke koje su stvarno implementirane i lokalno spremljene.",
            ("hr", "Startup") => "Pokretanje",
            ("hr", "StartupDescription") => "Pokreće SNAPVERE tiho u području obavijesti nakon prijave u Windows.",
            ("hr", "Capture") => "Snimanje",
            ("hr", "CursorDescription") => "Uključi pokazivač kada aktivni način snimanja podržava njegovo uključivanje.",
            ("hr", "CurrentLanguageDescription") => "Trenutačno: {0}. Engleski je zadani rezervni jezik.",
            ("hr", "Refresh") => "Osvježi",
            ("hr", "PreferenceReadFailed") => "Nije moguće pročitati jednu ili više postavki: {0}",
            ("hr", "StartupEnabled") => "Automatsko pokretanje s Windowsima je uključeno.",
            ("hr", "StartupDisabled") => "Automatsko pokretanje s Windowsima je isključeno.",
            ("hr", "StartupChangeFailed") => "Postavku automatskog pokretanja s Windowsima nije moguće promijeniti: {0}",
            ("hr", "CursorEnabled") => "Snimanje pokazivača je uključeno za podržane načine snimanja.",
            ("hr", "CursorDisabled") => "Snimanje pokazivača je isključeno.",
            ("hr", "CursorSaveFailed") => "Postavku pokazivača nije moguće spremiti: {0}",
            ("hr", "OneLocalCapture") => "1 lokalna snimka",
            ("hr", "LocalCaptureCount") => "Lokalne snimke: {0}",
            ("hr", "LocalHistoryUnavailable") => "Lokalna povijest nije dostupna",
            ("hr", "NoCapturesYet") => "Još nema snimki",
            ("hr", "EmptyHistoryHelp") => "Koristi Print Screen ili ikonu u području obavijesti za prvu snimku područja.",
            ("hr", "OpenCaptureNamed") => "Otvori {0}",
            ("hr", "CaptureUnavailable") => "Ta snimka više nije dostupna na izvornoj lokaciji.",
            ("hr", "OpenCaptureFailed") => "Windows nije uspio otvoriti tu snimku: {0}",
            ("hr", "OpenCaptureFolderFailed") => "Windows nije uspio otvoriti mapu snimki: {0}",
            ("hr", "Off") => "Isključeno",
            ("hr", "On") => "Uključeno",
            ("hr", "LanguagePickerIntro") => "Engleski je zadani jezik. Odaberi bilo koji podržani jezik; postavka se sprema samo na ovom računalu.",
            ("hr", "LanguageSaveFailed") => "Postavku jezika nije moguće spremiti: {0}",
            ("hr", "AboutLocalFirstTitle") => "Lokalno snimanje za Windows",
            ("hr", "AboutCaptureDescription") => "Snimanje područja, prozora i zaslona bez ovisnosti o oblaku.",
            ("hr", "AboutPrivacyDescription") => "Snimke ostaju na uređaju osim ako ih izričito ne kopiraš, spremiš ili podijeliš putem Windowsa.",
            ("hr", "CommercialSoftware") => "SNAPVERE komercijalni softver • © Brendigo",

            ("de", "Language") => "Sprache", ("de", "CaptureRegion") => "Bereich aufnehmen", ("de", "CaptureWindow") => "Fenster aufnehmen", ("de", "CaptureScreen") => "Bildschirm aufnehmen", ("de", "Settings") => "Einstellungen", ("de", "Exit") => "Beenden", ("de", "Copy") => "Kopieren", ("de", "Save") => "Speichern", ("de", "Close") => "Schließen",
            ("fr", "Language") => "Langue", ("fr", "CaptureRegion") => "Capturer une zone", ("fr", "CaptureWindow") => "Capturer une fenêtre", ("fr", "CaptureScreen") => "Capturer l’écran", ("fr", "Settings") => "Paramètres", ("fr", "Exit") => "Quitter", ("fr", "Copy") => "Copier", ("fr", "Save") => "Enregistrer", ("fr", "Close") => "Fermer",
            ("es", "Language") => "Idioma", ("es", "CaptureRegion") => "Capturar región", ("es", "CaptureWindow") => "Capturar ventana", ("es", "CaptureScreen") => "Capturar pantalla", ("es", "Settings") => "Configuración", ("es", "Exit") => "Salir", ("es", "Copy") => "Copiar", ("es", "Save") => "Guardar", ("es", "Close") => "Cerrar",
            ("it", "Language") => "Lingua", ("it", "CaptureRegion") => "Cattura area", ("it", "CaptureWindow") => "Cattura finestra", ("it", "CaptureScreen") => "Cattura schermo", ("it", "Settings") => "Impostazioni", ("it", "Exit") => "Esci", ("it", "Copy") => "Copia", ("it", "Save") => "Salva", ("it", "Close") => "Chiudi",
            ("pt", "Language") => "Idioma", ("pt", "CaptureRegion") => "Capturar região", ("pt", "CaptureWindow") => "Capturar janela", ("pt", "CaptureScreen") => "Capturar ecrã", ("pt", "Settings") => "Definições", ("pt", "Exit") => "Sair", ("pt", "Copy") => "Copiar", ("pt", "Save") => "Guardar", ("pt", "Close") => "Fechar",
            ("nl", "Language") => "Taal", ("nl", "CaptureRegion") => "Gebied vastleggen", ("nl", "CaptureWindow") => "Venster vastleggen", ("nl", "CaptureScreen") => "Scherm vastleggen", ("nl", "Settings") => "Instellingen", ("nl", "Exit") => "Afsluiten", ("nl", "Copy") => "Kopiëren", ("nl", "Save") => "Opslaan", ("nl", "Close") => "Sluiten",
            ("pl", "Language") => "Język", ("pl", "CaptureRegion") => "Przechwyć obszar", ("pl", "CaptureWindow") => "Przechwyć okno", ("pl", "CaptureScreen") => "Przechwyć ekran", ("pl", "Settings") => "Ustawienia", ("pl", "Exit") => "Wyjście", ("pl", "Copy") => "Kopiuj", ("pl", "Save") => "Zapisz", ("pl", "Close") => "Zamknij",
            ("cs", "Language") => "Jazyk", ("cs", "CaptureRegion") => "Zachytit oblast", ("cs", "CaptureWindow") => "Zachytit okno", ("cs", "CaptureScreen") => "Zachytit obrazovku", ("cs", "Settings") => "Nastavení", ("cs", "Exit") => "Ukončit", ("cs", "Copy") => "Kopírovat", ("cs", "Save") => "Uložit", ("cs", "Close") => "Zavřít",
            ("sk", "Language") => "Jazyk", ("sk", "CaptureRegion") => "Zachytiť oblasť", ("sk", "CaptureWindow") => "Zachytiť okno", ("sk", "CaptureScreen") => "Zachytiť obrazovku", ("sk", "Settings") => "Nastavenia", ("sk", "Exit") => "Ukončiť", ("sk", "Copy") => "Kopírovať", ("sk", "Save") => "Uložiť", ("sk", "Close") => "Zavrieť",
            ("sl", "Language") => "Jezik", ("sl", "CaptureRegion") => "Zajemi območje", ("sl", "CaptureWindow") => "Zajemi okno", ("sl", "CaptureScreen") => "Zajemi zaslon", ("sl", "Settings") => "Nastavitve", ("sl", "Exit") => "Izhod", ("sl", "Copy") => "Kopiraj", ("sl", "Save") => "Shrani", ("sl", "Close") => "Zapri",
            ("hu", "Language") => "Nyelv", ("hu", "CaptureRegion") => "Terület rögzítése", ("hu", "CaptureWindow") => "Ablak rögzítése", ("hu", "CaptureScreen") => "Képernyő rögzítése", ("hu", "Settings") => "Beállítások", ("hu", "Exit") => "Kilépés", ("hu", "Copy") => "Másolás", ("hu", "Save") => "Mentés", ("hu", "Close") => "Bezárás",
            ("ro", "Language") => "Limbă", ("ro", "CaptureRegion") => "Capturează zona", ("ro", "CaptureWindow") => "Capturează fereastra", ("ro", "CaptureScreen") => "Capturează ecranul", ("ro", "Settings") => "Setări", ("ro", "Exit") => "Ieșire", ("ro", "Copy") => "Copiază", ("ro", "Save") => "Salvează", ("ro", "Close") => "Închide",
            ("bg", "Language") => "Език", ("bg", "CaptureRegion") => "Заснемане на област", ("bg", "CaptureWindow") => "Заснемане на прозорец", ("bg", "CaptureScreen") => "Заснемане на екран", ("bg", "Settings") => "Настройки", ("bg", "Exit") => "Изход", ("bg", "Copy") => "Копирай", ("bg", "Save") => "Запази", ("bg", "Close") => "Затвори",
            ("el", "Language") => "Γλώσσα", ("el", "CaptureRegion") => "Λήψη περιοχής", ("el", "CaptureWindow") => "Λήψη παραθύρου", ("el", "CaptureScreen") => "Λήψη οθόνης", ("el", "Settings") => "Ρυθμίσεις", ("el", "Exit") => "Έξοδος", ("el", "Copy") => "Αντιγραφή", ("el", "Save") => "Αποθήκευση", ("el", "Close") => "Κλείσιμο",
            ("sv", "Language") => "Språk", ("sv", "CaptureRegion") => "Fånga område", ("sv", "CaptureWindow") => "Fånga fönster", ("sv", "CaptureScreen") => "Fånga skärm", ("sv", "Settings") => "Inställningar", ("sv", "Exit") => "Avsluta", ("sv", "Copy") => "Kopiera", ("sv", "Save") => "Spara", ("sv", "Close") => "Stäng",
            ("da", "Language") => "Sprog", ("da", "CaptureRegion") => "Optag område", ("da", "CaptureWindow") => "Optag vindue", ("da", "CaptureScreen") => "Optag skærm", ("da", "Settings") => "Indstillinger", ("da", "Exit") => "Afslut", ("da", "Copy") => "Kopiér", ("da", "Save") => "Gem", ("da", "Close") => "Luk",
            ("no", "Language") => "Språk", ("no", "CaptureRegion") => "Ta område", ("no", "CaptureWindow") => "Ta vindu", ("no", "CaptureScreen") => "Ta skjerm", ("no", "Settings") => "Innstillinger", ("no", "Exit") => "Avslutt", ("no", "Copy") => "Kopier", ("no", "Save") => "Lagre", ("no", "Close") => "Lukk",
            ("fi", "Language") => "Kieli", ("fi", "CaptureRegion") => "Kaappaa alue", ("fi", "CaptureWindow") => "Kaappaa ikkuna", ("fi", "CaptureScreen") => "Kaappaa näyttö", ("fi", "Settings") => "Asetukset", ("fi", "Exit") => "Poistu", ("fi", "Copy") => "Kopioi", ("fi", "Save") => "Tallenna", ("fi", "Close") => "Sulje",
            ("et", "Language") => "Keel", ("et", "CaptureRegion") => "Jäädvusta ala", ("et", "CaptureWindow") => "Jäädvusta aken", ("et", "CaptureScreen") => "Jäädvusta ekraan", ("et", "Settings") => "Seaded", ("et", "Exit") => "Välju", ("et", "Copy") => "Kopeeri", ("et", "Save") => "Salvesta", ("et", "Close") => "Sulge",
            ("lv", "Language") => "Valoda", ("lv", "CaptureRegion") => "Uzņemt apgabalu", ("lv", "CaptureWindow") => "Uzņemt logu", ("lv", "CaptureScreen") => "Uzņemt ekrānu", ("lv", "Settings") => "Iestatījumi", ("lv", "Exit") => "Iziet", ("lv", "Copy") => "Kopēt", ("lv", "Save") => "Saglabāt", ("lv", "Close") => "Aizvērt",
            ("lt", "Language") => "Kalba", ("lt", "CaptureRegion") => "Fiksuoti sritį", ("lt", "CaptureWindow") => "Fiksuoti langą", ("lt", "CaptureScreen") => "Fiksuoti ekraną", ("lt", "Settings") => "Nustatymai", ("lt", "Exit") => "Išeiti", ("lt", "Copy") => "Kopijuoti", ("lt", "Save") => "Išsaugoti", ("lt", "Close") => "Uždaryti",
            ("uk", "Language") => "Мова", ("uk", "CaptureRegion") => "Зняти область", ("uk", "CaptureWindow") => "Зняти вікно", ("uk", "CaptureScreen") => "Зняти екран", ("uk", "Settings") => "Налаштування", ("uk", "Exit") => "Вийти", ("uk", "Copy") => "Копіювати", ("uk", "Save") => "Зберегти", ("uk", "Close") => "Закрити",
            ("tr", "Language") => "Dil", ("tr", "CaptureRegion") => "Bölge yakala", ("tr", "CaptureWindow") => "Pencere yakala", ("tr", "CaptureScreen") => "Ekranı yakala", ("tr", "Settings") => "Ayarlar", ("tr", "Exit") => "Çıkış", ("tr", "Copy") => "Kopyala", ("tr", "Save") => "Kaydet", ("tr", "Close") => "Kapat",
            ("ja", "Language") => "言語", ("ja", "CaptureRegion") => "範囲をキャプチャ", ("ja", "CaptureWindow") => "ウィンドウをキャプチャ", ("ja", "CaptureScreen") => "画面をキャプチャ", ("ja", "Settings") => "設定", ("ja", "Exit") => "終了", ("ja", "Copy") => "コピー", ("ja", "Save") => "保存", ("ja", "Close") => "閉じる",
            ("ko", "Language") => "언어", ("ko", "CaptureRegion") => "영역 캡처", ("ko", "CaptureWindow") => "창 캡처", ("ko", "CaptureScreen") => "화면 캡처", ("ko", "Settings") => "설정", ("ko", "Exit") => "종료", ("ko", "Copy") => "복사", ("ko", "Save") => "저장", ("ko", "Close") => "닫기",
            ("zh-CN", "Language") => "语言", ("zh-CN", "CaptureRegion") => "截取区域", ("zh-CN", "CaptureWindow") => "截取窗口", ("zh-CN", "CaptureScreen") => "截取屏幕", ("zh-CN", "Settings") => "设置", ("zh-CN", "Exit") => "退出", ("zh-CN", "Copy") => "复制", ("zh-CN", "Save") => "保存", ("zh-CN", "Close") => "关闭",
            _ => null
        };
}
