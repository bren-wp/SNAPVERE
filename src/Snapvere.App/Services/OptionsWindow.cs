using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Snapvere.Application.Capture;
using Snapvere.Shared;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.System;

namespace Snapvere.App.Services;

public enum OptionsSection
{
    Preferences, // Real screen-capture preferences; remains the external default.
    RecentCaptures,
    General,
    Recording,
    Shortcuts,
    About
}

/// <summary>
/// On-demand settings and recent-captures surface. It reads local preferences
/// only when shown and performs no polling while SNAPVERE is idle.
/// </summary>
public sealed class OptionsWindow : Window
{
    private const int RecentCaptureLimit = 10;

    private readonly CaptureHistoryService _history;
    private readonly CapturePreferencesService _preferences;
    private readonly StartupRegistrationService _startupRegistration;
    private readonly string _languageCode;
    private readonly Grid _preferencesPanel = new();
    private readonly Grid _generalPanel = new();
    private readonly Grid _recordingPanel = new();
    private readonly Grid _shortcutsPanel = new();
    private readonly Grid _aboutPanel = new();
    private TextBlock? _sectionTitle;
    private TextBlock? _sectionDescription;
    private readonly ScrollViewer _preferencesScroller = new()
    {
        VerticalScrollMode = ScrollMode.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollMode = ScrollMode.Disabled,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        IsTabStop = false
    };
    private readonly Grid _recentPanel = new() { Visibility = Visibility.Collapsed };
    private readonly StackPanel _recentItems = new() { Spacing = 8 };
    private readonly TextBlock _recentSummary;
    private readonly TextBlock _statusText;
    private readonly ToggleSwitch _startupToggle;
    private readonly ToggleSwitch _cursorToggle;
    private readonly StackPanel _recordingActions;
    private readonly Button _recordingStartButton;
    private readonly Button _recordingStopButton;
    private readonly Button _preferencesTab;
    private readonly Button _recentTab;
    private readonly Button _generalTab;
    private readonly Button _recordingTab;
    private readonly Button _shortcutsTab;
    private readonly Button _aboutTab;
    private bool _sizeApplied;
    private bool _updatingControls;
    private Action? _startScreenRecording;
    private Action? _stopScreenRecording;

    public OptionsWindow(
        CaptureHistoryService history,
        CapturePreferencesService preferences,
        StartupRegistrationService startupRegistration)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _startupRegistration = startupRegistration ?? throw new ArgumentNullException(nameof(startupRegistration));
        _languageCode = SnapvereLocalization.NormalizeLanguageCode(_preferences.Current.LanguageCode);

        Title = $"SNAPVERE — {L("Settings")}";
        _recentSummary = Text("Pictures\\SNAPVERE", 10, Subtle);
        _statusText = Text(LocalStatusText(), 10, Success);
        _statusText.TextWrapping = TextWrapping.Wrap;
        AutomationProperties.SetLiveSetting(_statusText, AutomationLiveSetting.Polite);

        _startupToggle = CreateToggle(L("StartWithWindows"));
        _cursorToggle = CreateToggle(L("IncludeCursor"));
        var recordingActions = CreateRecordingActions();
        _recordingActions = recordingActions.Root;
        _recordingStartButton = recordingActions.Start;
        _recordingStopButton = recordingActions.Stop;
        _startupToggle.Toggled += StartupToggle_Toggled;
        _cursorToggle.Toggled += CursorToggle_Toggled;

        _generalTab = CreateTabButton(UserText("General", "Općenito"), "\uE713", () => ShowSection(OptionsSection.General));
        _preferencesTab = CreateTabButton(UserText("Screen capture", "Snimanje zaslona"), "\uE722", () => ShowSection(OptionsSection.Preferences));
        _recordingTab = CreateTabButton(UserText("Video recording", "Video snimanje"), "\uE714", () => ShowSection(OptionsSection.Recording));
        _shortcutsTab = CreateTabButton(UserText("Shortcuts", "Prečaci"), "\uE765", () => ShowSection(OptionsSection.Shortcuts));
        _recentTab = CreateTabButton(UserText("Storage", "Pohrana"), "\uE838", () => ShowSection(OptionsSection.RecentCaptures));
        _aboutTab = CreateTabButton(L("About"), "\uE946", () => ShowSection(OptionsSection.About));

        BuildPreferencesPanel();
        BuildGeneralPanel();
        BuildRecordingPanel();
        BuildShortcutsPanel();
        BuildAboutPanel();
        BuildRecentPanel();
        var content = BuildContent();
        content.KeyDown += Root_KeyDown;
        Content = content;
        ShowSection(OptionsSection.Preferences);
        Activated += OptionsWindow_Activated;
    }

    public void ConfigureScreenRecording(
        Action startScreenRecording,
        Action stopScreenRecording,
        bool active)
    {
        _startScreenRecording = startScreenRecording ?? throw new ArgumentNullException(nameof(startScreenRecording));
        _stopScreenRecording = stopScreenRecording ?? throw new ArgumentNullException(nameof(stopScreenRecording));
        SetScreenRecordingState(active);
    }

    public void SetScreenRecordingState(bool active)
    {
        var configured = _startScreenRecording is not null && _stopScreenRecording is not null;
        _recordingStartButton.IsEnabled = configured && !active;
        _recordingStopButton.IsEnabled = configured && active;

        _recordingStartButton.Background = active
            ? Brush(0x28, 0x5E, 0x48, 0xBD)
            : Brush(0xFF, 0x39, 0x28, 0x78);
        _recordingStartButton.BorderBrush = active
            ? Brush(0x38, 0x9D, 0x86, 0xFF)
            : Brush(0xFF, 0x86, 0x67, 0xF4);

        _recordingStopButton.Background = active
            ? Brush(0xFF, 0x73, 0x24, 0x3A)
            : Brush(0x24, 0x73, 0x24, 0x3A);
        _recordingStopButton.BorderBrush = active
            ? Brush(0xFF, 0xEC, 0x5F, 0x74)
            : Brush(0x35, 0xEC, 0x5F, 0x74);
    }

    public void ShowSection(OptionsSection section)
    {
        _preferencesScroller.Visibility = section == OptionsSection.Preferences ? Visibility.Visible : Visibility.Collapsed;
        _recentPanel.Visibility = section == OptionsSection.RecentCaptures ? Visibility.Visible : Visibility.Collapsed;
        _generalPanel.Visibility = section == OptionsSection.General ? Visibility.Visible : Visibility.Collapsed;
        _recordingPanel.Visibility = section == OptionsSection.Recording ? Visibility.Visible : Visibility.Collapsed;
        _shortcutsPanel.Visibility = section == OptionsSection.Shortcuts ? Visibility.Visible : Visibility.Collapsed;
        _aboutPanel.Visibility = section == OptionsSection.About ? Visibility.Visible : Visibility.Collapsed;

        ApplyTabVisual(_generalTab, section == OptionsSection.General);
        ApplyTabVisual(_preferencesTab, section == OptionsSection.Preferences);
        ApplyTabVisual(_recordingTab, section == OptionsSection.Recording);
        ApplyTabVisual(_shortcutsTab, section == OptionsSection.Shortcuts);
        ApplyTabVisual(_recentTab, section == OptionsSection.RecentCaptures);
        ApplyTabVisual(_aboutTab, section == OptionsSection.About);

        if (_sectionTitle is not null && _sectionDescription is not null)
        {
            (_sectionTitle.Text, _sectionDescription.Text) = section switch
            {
                OptionsSection.General =>
                    (UserText("General", "Općenito"),
                     UserText("Local-first application preferences.", "Postavke aplikacije s lokalnom pohranom.")),
                OptionsSection.Recording =>
                    (UserText("Video recording", "Video snimanje"),
                     UserText("Control the implemented primary-display recorder.", "Upravljajte snimanjem primarnog zaslona.")),
                OptionsSection.Shortcuts =>
                    (UserText("Shortcuts", "Prečaci"),
                     UserText("Current registered capture shortcuts.", "Trenutačno registrirani prečaci za snimanje.")),
                OptionsSection.RecentCaptures =>
                    (UserText("Storage", "Pohrana"),
                     UserText("Review and open captures stored on this PC.", "Pregledajte snimke spremljene na ovom računalu.")),
                OptionsSection.About =>
                    (L("About"), UserText("Application and support information.", "Informacije o aplikaciji i podršci.")),
                _ => (UserText("Screen capture", "Snimanje zaslona"),
                      UserText("Capture behavior adjusted to your workflow.", "Ponašanje snimanja prilagođeno vašem načinu rada."))
            };
        }

        if (section == OptionsSection.RecentCaptures)
        {
            RefreshRecentCaptures();
        }
        else if (section == OptionsSection.Preferences)
        {
            LoadPreferences();
        }
    }

    private string L(string key) => SnapvereLocalization.T(key, _languageCode);

    private string LF(string key, params object[] arguments)
        => string.Format(L(key), arguments);

    private string LocalStatusText() => L("PreferencesStoredLocally");

    private string UserText(string english, string croatian)
        => string.Equals(_languageCode, "hr", StringComparison.OrdinalIgnoreCase)
            ? croatian
            : english;

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = SnapvereBrand.Obsidian,
            Padding = new Thickness(12)
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(222) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var railStack = new StackPanel { Spacing = 6 };
        var brandRow = new Grid { Margin = new Thickness(2, 2, 2, 14) };
        brandRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        brandRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        brandRow.Children.Add(SnapvereBrand.CreateMark(42));
        var brandCopy = SnapvereBrand.CreateWordmark(19);
        brandCopy.VerticalAlignment = VerticalAlignment.Center;
        brandCopy.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(brandCopy, 1);
        brandRow.Children.Add(brandCopy);
        railStack.Children.Add(brandRow);

        var navLabel = Text(L("Settings").ToUpperInvariant(), 9, SnapvereBrand.Subtle, Microsoft.UI.Text.FontWeights.SemiBold);
        navLabel.CharacterSpacing = 100;
        navLabel.Margin = new Thickness(8, 2, 0, 6);
        railStack.Children.Add(navLabel);
        railStack.Children.Add(_generalTab);
        railStack.Children.Add(_preferencesTab);
        railStack.Children.Add(_recordingTab);
        railStack.Children.Add(_shortcutsTab);
        railStack.Children.Add(_recentTab);
        railStack.Children.Add(_aboutTab);

        var rail = new Border
        {
            Background = SnapvereBrand.Surface,
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(12),
            Child = railStack
        };
        root.Children.Add(rail);

        var main = new Grid { Margin = new Thickness(30, 12, 12, 0) };
        main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        main.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        main.Children.Add(BuildHeader());

        _preferencesScroller.Content = _preferencesPanel;
        var host = new Grid();
        host.Children.Add(_preferencesScroller);
        host.Children.Add(_generalPanel);
        host.Children.Add(_recordingPanel);
        host.Children.Add(_shortcutsPanel);
        host.Children.Add(_recentPanel);
        host.Children.Add(_aboutPanel);
        var contentFrame = new Border
        {
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(2, 0, 4, 0),
            Background = Brush(0x00, 0, 0, 0),
            Child = host
        };
        Grid.SetRow(contentFrame, 1);
        main.Children.Add(contentFrame);

        var footer = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _statusText.VerticalAlignment = VerticalAlignment.Center;
        footer.Children.Add(_statusText);
        var close = CreateSecondaryAction(L("Close"), "\uE711", Close);
        Grid.SetColumn(close, 1);
        footer.Children.Add(close);
        Grid.SetRow(footer, 2);
        main.Children.Add(footer);
        Grid.SetColumn(main, 1);
        root.Children.Add(main);
        return root;
    }

    private FrameworkElement BuildHeader()
    {
        var identity = new StackPanel { Spacing = 5 };
        _sectionTitle = Text(UserText("Screen capture", "Snimanje zaslona"),
            26, SnapvereBrand.Strong, Microsoft.UI.Text.FontWeights.SemiBold);
        _sectionDescription = Text(
            UserText("Capture behavior adjusted to your workflow.",
                     "Ponašanje snimanja prilagođeno vašem načinu rada."),
            11, SnapvereBrand.Muted);
        _sectionDescription.TextWrapping = TextWrapping.Wrap;
        identity.Children.Add(_sectionTitle);
        identity.Children.Add(_sectionDescription);
        return identity;
    }

    private void BuildPreferencesPanel()
    {
        for (var i = 0; i < 5; i++)
        {
            _preferencesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddInlinePreferenceRow(0,
            L("IncludeCursor"),
            UserText("Include the pointer in supported screenshots.", "Uključi pokazivač miša na podržanim snimkama."),
            _cursorToggle);

        AddInlinePreferenceRow(1,
            L("StartWithWindows"),
            UserText("SNAPVERE is ready in the notification area.", "SNAPVERE je spreman u sistemskoj traci."),
            _startupToggle);

        // The reference shows the currently selected language, not a generic
        // "Choose language" command. Keep the existing fully functional,
        // localized picker when the user activates this compact selector.
        var currentLanguage = SnapvereLocalization.SupportedLanguages.First(language =>
            string.Equals(language.Code, _preferences.Current.LanguageCode, StringComparison.OrdinalIgnoreCase));
        var languageButton = CreateSecondaryAction(
            currentLanguage.NativeName,
            "\uE70D",
            () =>
            {
                Close();
                LanguagePickerWindow.ShowStandalone(_preferences);
            });
        languageButton.MinWidth = 154;
        AutomationProperties.SetName(languageButton, L("ChooseLanguage"));
        ToolTipService.SetToolTip(languageButton, L("ChooseLanguage"));
        AddInlinePreferenceRow(2,
            UserText("Interface language", "Jezik sučelja"),
            CurrentLanguageDescription(),
            languageButton);

        var privacy = new Border
        {
            Margin = new Thickness(0, 22, 0, 0),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(16),
            Background = Brush(0xFF, 0x17, 0x29, 0x43),
            BorderBrush = Brush(0x77, 0x57, 0x94, 0xB0),
            BorderThickness = new Thickness(1)
        };
        // The cyan lock is part of the supplied Settings reference. Render it
        // from the installed Windows glyph set, never from a missing web font.
        var privacyLayout = new Grid { ColumnSpacing = 14 };
        privacyLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        privacyLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var privacyLock = new FontIcon
        {
            Glyph = "\uE72E",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 20,
            Foreground = SnapvereBrand.Ice,
            VerticalAlignment = VerticalAlignment.Center
        };
        privacyLayout.Children.Add(privacyLock);
        var info = new StackPanel { Spacing = 7 };
        info.Children.Add(Text(UserText("Your captures stay local", "Tvoje snimke ostaju lokalne"),
            14, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var note = Text(UserText(
            "No automatic upload and no required user account.",
            "Bez automatskog prijenosa u oblak i bez obveznog korisničkog računa."),
            10.5, Muted);
        note.TextWrapping = TextWrapping.Wrap;
        info.Children.Add(note);
        Grid.SetColumn(info, 1);
        privacyLayout.Children.Add(info);
        privacy.Child = privacyLayout;
        Grid.SetRow(privacy, 3);
        _preferencesPanel.Children.Add(privacy);
    }

    private void AddInlinePreferenceRow(int index, string title, string description, FrameworkElement trailing)
    {
        var grid = new Grid
        {
            MinHeight = 91,
            Padding = new Thickness(2, 13, 0, 13)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(Text(title, 14, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var detail = Text(description, 11, Muted);
        detail.TextWrapping = TextWrapping.Wrap;
        copy.Children.Add(detail);
        grid.Children.Add(copy);
        trailing.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(trailing, 1);
        grid.Children.Add(trailing);
        var row = new Border
        {
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid
        };
        Grid.SetRow(row, index);
        _preferencesPanel.Children.Add(row);
    }

    private void BuildGeneralPanel()
    {
        _generalPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _generalPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var intro = Text(UserText("Local-first by design", "Lokalna pohrana po dizajnu"),
            19, Strong, Microsoft.UI.Text.FontWeights.SemiBold);
        _generalPanel.Children.Add(intro);
        var message = Text(UserText(
            "Preferences and screenshots are saved on this computer. Capture startup, cursor and language settings are available under Screen capture.",
            "Postavke i snimke spremaju se na ovo računalo. Opcije pokretanja, pokazivača i jezika nalaze se pod Snimanje zaslona."),
            12, Muted);
        message.TextWrapping = TextWrapping.Wrap;
        message.Margin = new Thickness(0, 15, 0, 0);
        Grid.SetRow(message, 1);
        _generalPanel.Children.Add(message);
    }

    private void BuildRecordingPanel()
    {
        _recordingPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _recordingPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _recordingPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _recordingPanel.Children.Add(Text(
            UserText("Record the primary display", "Snimanje primarnog zaslona"),
            19, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var description = Text(UserText(
            "Start or stop the implemented screen recorder. Microphone and system audio are not recorded.",
            "Pokrenite ili zaustavite snimanje zaslona. Mikrofon i zvuk sustava ne snimaju se."),
            12, Muted);
        description.TextWrapping = TextWrapping.Wrap;
        description.Margin = new Thickness(0, 12, 0, 24);
        Grid.SetRow(description, 1);
        _recordingPanel.Children.Add(description);
        _recordingActions.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetRow(_recordingActions, 2);
        _recordingPanel.Children.Add(_recordingActions);
    }

    private void BuildShortcutsPanel()
    {
        for (var i = 0; i < 4; i++)
        {
            _shortcutsPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
        var title = Text(UserText("Registered capture shortcuts", "Registrirani prečaci snimanja"),
            18, Strong, Microsoft.UI.Text.FontWeights.SemiBold);
        _shortcutsPanel.Children.Add(title);
        var names = new[]
        {
            UserText("Capture region", "Odabir regije"),
            UserText("Capture window", "Snimka prozora"),
            UserText("Capture screen", "Cijeli zaslon")
        };
        for (var i = 0; i < names.Length; i++)
        {
            var line = new Grid
            {
                MinHeight = 70,
                Margin = new Thickness(0, 8, 0, 0)
            };
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var name = Text(names[i], 13, Strong, Microsoft.UI.Text.FontWeights.SemiBold);
            name.VerticalAlignment = VerticalAlignment.Center;
            line.Children.Add(name);
            var hotkey = Text($"Ctrl + Shift + {i + 1}", 11, SnapvereBrand.Lavender);
            hotkey.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(hotkey, 1);
            line.Children.Add(hotkey);
            Grid.SetRow(line, i + 1);
            _shortcutsPanel.Children.Add(line);
        }
    }

    private void BuildAboutPanel()
    {
        _aboutPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _aboutPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var version = typeof(OptionsWindow).Assembly.GetName().Version?.ToString(3);
        _aboutPanel.Children.Add(Text(version is null ? "SNAPVERE" : $"SNAPVERE v{version}", 18, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var button = CreateSecondaryAction(L("About"), "\uE946", () =>
        {
            var about = new AboutWindow();
            about.Activate();
        });
        button.HorizontalAlignment = HorizontalAlignment.Left;
        button.Margin = new Thickness(0, 18, 0, 0);
        Grid.SetRow(button, 1);
        _aboutPanel.Children.Add(button);
    }

    private string CurrentLanguageDescription()
    {
        var selected = SnapvereLocalization.SupportedLanguages.First(language =>
            string.Equals(language.Code, _preferences.Current.LanguageCode, StringComparison.OrdinalIgnoreCase));
        return UserText(
            $"Selected language: {selected.NativeName}.",
            $"Odabrani jezik: {selected.NativeName}.");
    }

    private void BuildRecentPanel()
    {
        _recentPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _recentPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _recentPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel { Spacing = 3 };
        copy.Children.Add(Text(L("RecentCaptures"), 19, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        copy.Children.Add(_recentSummary);
        header.Children.Add(copy);

        var refresh = CreateSecondaryAction(L("Refresh"), "\uE72C", RefreshRecentCaptures);
        Grid.SetColumn(refresh, 1);
        header.Children.Add(refresh);
        _recentPanel.Children.Add(header);

        var scroller = new ScrollViewer
        {
            Margin = new Thickness(0, 15, 0, 0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _recentItems
        };
        Grid.SetRow(scroller, 1);
        _recentPanel.Children.Add(scroller);

        var folder = CreateSecondaryAction(L("OpenCaptureFolder"), "\uE838", OpenCaptureFolder);
        folder.HorizontalAlignment = HorizontalAlignment.Left;
        folder.Margin = new Thickness(0, 14, 0, 0);
        Grid.SetRow(folder, 2);
        _recentPanel.Children.Add(folder);
    }

    private void LoadPreferences()
    {
        _updatingControls = true;
        try
        {
            _cursorToggle.IsOn = _preferences.Current.IncludeCursorOnCapture;
            _startupToggle.IsOn = _startupRegistration.IsEnabled();
            SetStatus(LocalStatusText(), Success);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            StartupDiagnostics.Record("Read settings", exception);
            SetStatus(
                UserText(
                    "Settings could not be loaded. SNAPVERE is using safe local defaults.",
                    "Postavke nije moguće učitati. SNAPVERE koristi sigurne lokalne zadane postavke."),
                Warning);
        }
        finally
        {
            _updatingControls = false;
        }
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        var requestedState = _startupToggle.IsOn;
        try
        {
            _startupRegistration.SetEnabled(requestedState);
            SetStatus(requestedState ? L("StartupEnabled") : L("StartupDisabled"), Success);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            System.Security.SecurityException or
            InvalidOperationException)
        {
            _updatingControls = true;
            try
            {
                _startupToggle.IsOn = !requestedState;
            }
            finally
            {
                _updatingControls = false;
            }

            StartupDiagnostics.Record("Change Windows startup setting", exception);
            SetStatus(
                UserText(
                    "Windows startup could not be changed. Check Windows permissions and try again.",
                    "Automatsko pokretanje nije moguće promijeniti. Provjerite Windows dozvole i pokušajte ponovno."),
                Error);
        }
    }

    private void CursorToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        try
        {
            _preferences.SetIncludeCursorOnCapture(_cursorToggle.IsOn);
            SetStatus(_cursorToggle.IsOn ? L("CursorEnabled") : L("CursorDisabled"), Success);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            System.Security.SecurityException)
        {
            _updatingControls = true;
            try
            {
                _cursorToggle.IsOn = _preferences.Current.IncludeCursorOnCapture;
            }
            finally
            {
                _updatingControls = false;
            }

            StartupDiagnostics.Record("Save cursor preference", exception);
            SetStatus(
                UserText(
                    "The cursor setting could not be saved. Try again or restart SNAPVERE.",
                    "Postavku pokazivača nije moguće spremiti. Pokušajte ponovno ili ponovno pokrenite SNAPVERE."),
                Error);
        }
    }

    private void RefreshRecentCaptures()
    {
        _recentItems.Children.Clear();
        try
        {
            var captures = _history.GetRecentCaptures(RecentCaptureLimit);
            _recentSummary.Text = captures.Count switch
            {
                0 => "Pictures\\SNAPVERE",
                1 => L("OneLocalCapture"),
                _ => LF("LocalCaptureCount", captures.Count)
            };

            if (captures.Count == 0)
            {
                _recentItems.Children.Add(BuildEmptyHistory());
                return;
            }

            foreach (var capture in captures)
            {
                _recentItems.Children.Add(CreateRecentCaptureRow(capture));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StartupDiagnostics.Record("Read recent captures", exception);
            _recentSummary.Text = L("LocalHistoryUnavailable");
            var error = Text(
                UserText(
                    "Recent captures could not be read. Open the capture folder or try Refresh again.",
                    "Nedavne snimke nije moguće učitati. Otvorite mapu snimki ili ponovno odaberite Osvježi."),
                10,
                Warning);
            error.TextWrapping = TextWrapping.Wrap;
            _recentItems.Children.Add(error);
        }
    }

    private Border BuildEmptyHistory()
    {
        var empty = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        empty.Children.Add(new FontIcon
        {
            Glyph = "\uE91B",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 24,
            Foreground = AccentText
        });
        empty.Children.Add(Text(
            L("NoCapturesYet"),
            12,
            Strong,
            Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment.Center));
        var detail = Text(
            L("EmptyHistoryHelp"),
            10,
            Subtle,
            null,
            HorizontalAlignment.Center);
        detail.TextWrapping = TextWrapping.Wrap;
        empty.Children.Add(detail);
        return new Border
        {
            Padding = new Thickness(22),
            CornerRadius = new CornerRadius(15),
            Background = Brush(0xFF, 0x10, 0x12, 0x1A),
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            Child = empty
        };
    }

    private FrameworkElement CreateRecentCaptureRow(CaptureHistoryItem capture)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.Children.Add(new Border
        {
            Width = 38,
            Height = 38,
            CornerRadius = new CornerRadius(11),
            Background = Brush(0x55, 0x53, 0x45, 0x9D),
            BorderBrush = Brush(0x55, 0x8D, 0x77, 0xE8),
            BorderThickness = new Thickness(1),
            Child = new FontIcon
            {
                Glyph = "\uE91B",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                Foreground = Strong
            }
        });

        var text = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        var fileName = Text(capture.FileName, 11, Strong, Microsoft.UI.Text.FontWeights.SemiBold);
        fileName.TextWrapping = TextWrapping.NoWrap;
        fileName.TextTrimming = TextTrimming.CharacterEllipsis;
        ToolTipService.SetToolTip(fileName, capture.FileName);
        text.Children.Add(fileName);

        var metadata = Text(capture.MetadataText, 9, Subtle);
        metadata.TextWrapping = TextWrapping.NoWrap;
        metadata.TextTrimming = TextTrimming.CharacterEllipsis;
        text.Children.Add(metadata);
        Grid.SetColumn(text, 1);
        content.Children.Add(text);

        var arrow = new FontIcon
        {
            Glyph = "\uE72A",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 11,
            Foreground = Subtle,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(arrow, 2);
        content.Children.Add(arrow);

        var openButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(11, 9, 11, 9),
            CornerRadius = new CornerRadius(13),
            Background = Elevated,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            Content = content
        };
        AutomationProperties.SetName(openButton, LF("OpenCaptureNamed", capture.FileName));
        openButton.Click += (_, _) => OpenCapture(capture);
        row.Children.Add(openButton);

        var copyPath = CreateSecondaryAction(
            L("CopyPath"),
            "\uE8C8",
            () => CopyCapturePath(capture));
        copyPath.Margin = new Thickness(8, 0, 0, 0);
        copyPath.VerticalAlignment = VerticalAlignment.Stretch;
        Grid.SetColumn(copyPath, 1);
        row.Children.Add(copyPath);

        row.SizeChanged += (_, args) =>
        {
            var compact = args.NewSize.Width < 520;
            Grid.SetRow(copyPath, compact ? 1 : 0);
            Grid.SetColumn(copyPath, compact ? 0 : 1);
            Grid.SetColumnSpan(copyPath, compact ? 2 : 1);
            copyPath.HorizontalAlignment = compact
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Stretch;
            copyPath.Margin = compact
                ? new Thickness(0, 7, 0, 0)
                : new Thickness(8, 0, 0, 0);
        };

        return row;
    }

    private void OpenCapture(CaptureHistoryItem capture)
    {
        try
        {
            if (!File.Exists(capture.FilePath))
            {
                RefreshRecentCaptures();
                SetStatus(L("CaptureUnavailable"), Warning);
                return;
            }
            LocalShellAction.Open(capture.FilePath);
        }
        catch (Exception exception) when (LocalShellActionFailurePolicy.IsExpected(exception))
        {
            StartupDiagnostics.Record("Open recent capture", exception);
            SetStatus(L("OpenCaptureFailed"), Error);
        }
    }

    private void CopyCapturePath(CaptureHistoryItem capture)
    {
        if (!File.Exists(capture.FilePath))
        {
            RefreshRecentCaptures();
            SetStatus(L("CaptureUnavailable"), Warning);
            return;
        }

        try
        {
            var package = new DataPackage();
            package.SetText(capture.FilePath);
            Clipboard.SetContent(package);
            Clipboard.Flush();
            SetStatus(L("CapturePathCopied"), Success);
        }
        catch (Exception exception) when (
            exception is System.Runtime.InteropServices.COMException or
            InvalidOperationException or
            UnauthorizedAccessException or
            System.Security.SecurityException)
        {
            StartupDiagnostics.Record("Copy recent capture path", exception);
            SetStatus(L("CapturePathCopyFailed"), Error);
        }
    }

    private void OpenCaptureFolder()
    {
        try
        {
            var directory = _history.GetCaptureDirectory();
            Directory.CreateDirectory(directory);
            LocalShellAction.Open(directory);
        }
        catch (Exception exception) when (LocalShellActionFailurePolicy.IsExpected(exception))
        {
            StartupDiagnostics.Record("Open capture folder", exception);
            SetStatus(L("OpenCaptureFolderFailed"), Error);
        }
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
        {
            return;
        }

        e.Handled = true;
        Close();
    }

    private void OptionsWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_sizeApplied)
        {
            return;
        }
        _sizeApplied = true;
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSizeToWorkArea(this, 980, 680));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
        }
    }

    private void ExecuteRecordingAction(string operation, Action? action)
    {
        if (action is null)
        {
            return;
        }

        try
        {
            action();
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record(operation, exception);
            SetStatus(
                UserText(
                    "Screen recording could not complete this action. Try again.",
                    "Snimanje zaslona nije uspjelo izvršiti ovu radnju. Pokušajte ponovno."),
                Error);
        }
    }

    private (StackPanel Root, Button Start, Button Stop) CreateRecordingActions()
    {
        var root = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var start = CreateRecordingCircleButton(
            "▶",
            L("StartScreenRecording"),
            Brush(0xFF, 0x39, 0x28, 0x78),
            Brush(0xFF, 0x86, 0x67, 0xF4),
            () => ExecuteRecordingAction("Start screen recording from Settings", _startScreenRecording));

        var stop = CreateRecordingCircleButton(
            "■",
            L("StopScreenRecording"),
            Brush(0x24, 0x73, 0x24, 0x3A),
            Brush(0x35, 0xEC, 0x5F, 0x74),
            () => ExecuteRecordingAction("Stop screen recording from Settings", _stopScreenRecording));

        root.Children.Add(start);
        root.Children.Add(stop);
        return (root, start, stop);
    }

    private static Button CreateRecordingCircleButton(
        string glyph,
        string accessibleName,
        SolidColorBrush background,
        SolidColorBrush border,
        Action action)
    {
        var button = new Button
        {
            Width = 42,
            Height = 42,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(21),
            Background = background,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            Foreground = Strong,
            IsEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe UI Symbol"),
                FontSize = glyph == "■" ? 12 : 14,
                Foreground = Strong,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        AutomationProperties.SetName(button, accessibleName);
        ToolTipService.SetToolTip(button, accessibleName);
        button.Click += (_, _) => action();
        return button;
    }

    private ToggleSwitch CreateToggle(string accessibleName)
    {
        var toggle = new ToggleSwitch
        {
            OffContent = string.Empty,
            OnContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(toggle, accessibleName);
        return toggle;
    }

    private static Button CreateTabButton(string label, string glyph, Action action)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14
        });
        content.Children.Add(Text(label, 12, SnapvereBrand.Muted, Microsoft.UI.Text.FontWeights.SemiBold));

        var button = new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            MinHeight = 46,
            Padding = new Thickness(12, 8, 12, 8),
            CornerRadius = new CornerRadius(12),
            Background = Transparent,
            BorderBrush = Transparent,
            BorderThickness = new Thickness(1)
        };
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) => action();
        return button;
    }

    private static void ApplyTabVisual(Button button, bool active)
    {
        button.Background = active ? Brush(0x66, 0x76, 0x55, 0xF6) : Transparent;
        button.BorderBrush = active ? Brush(0x99, 0xA4, 0x8B, 0xFF) : Transparent;
        var labelBrush = active ? SnapvereBrand.Strong : SnapvereBrand.Muted;
        button.Foreground = labelBrush;

        // The visible label and icon are children of the button's content
        // StackPanel. Setting only Button.Foreground leaves their explicit
        // brushes unchanged, making selected/inactive tabs look identical.
        if (button.Content is StackPanel contents)
        {
            foreach (var child in contents.Children)
            {
                if (child is TextBlock label)
                {
                    label.Foreground = labelBrush;
                }
                else if (child is FontIcon icon)
                {
                    icon.Foreground = active ? SnapvereBrand.Lavender : SnapvereBrand.Muted;
                }
            }
        }
    }

    private static Button CreateSecondaryAction(string label, string glyph, Action action)
    {
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        stack.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 12
        });
        stack.Children.Add(Text(label, 10, Strong, Microsoft.UI.Text.FontWeights.SemiBold));

        var button = new Button
        {
            Content = stack,
            Padding = new Thickness(11, 7, 11, 7),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0xFF, 0x16, 0x1A, 0x28),
            BorderBrush = Outline,
            BorderThickness = new Thickness(1)
        };
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) => action();
        return button;
    }

    private void SetStatus(string message, SolidColorBrush color)
    {
        _statusText.Text = message;
        _statusText.Foreground = color;
    }

    private static TextBlock Text(
        string value,
        double size,
        SolidColorBrush foreground,
        Windows.UI.Text.FontWeight? weight = null,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left)
        => new()
        {
            Text = value,
            FontSize = size,
            Foreground = foreground,
            FontWeight = weight ?? Microsoft.UI.Text.FontWeights.Normal,
            HorizontalAlignment = horizontalAlignment
        };

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Canvas => SnapvereBrand.Obsidian;
    private static SolidColorBrush Surface => SnapvereBrand.Surface;
    private static SolidColorBrush Elevated => SnapvereBrand.Slate;
    private static SolidColorBrush Outline => SnapvereBrand.Outline;
    private static SolidColorBrush Strong => SnapvereBrand.Strong;
    private static SolidColorBrush Muted => SnapvereBrand.Muted;
    private static SolidColorBrush Subtle => SnapvereBrand.Subtle;
    private static SolidColorBrush AccentText => SnapvereBrand.Lavender;
    private static SolidColorBrush Success => SnapvereBrand.Success;
    private static SolidColorBrush Warning => Brush(0xFF, 0xE2, 0xB5, 0x72);
    private static SolidColorBrush Error => Brush(0xFF, 0xF0, 0x8C, 0x9A);
    private static SolidColorBrush Transparent => Brush(0x00, 0, 0, 0);
}
