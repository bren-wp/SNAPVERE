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
    Preferences,
    RecentCaptures
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

        _preferencesTab = CreateTabButton(L("Settings"), "\uE713", () => ShowSection(OptionsSection.Preferences));
        _recentTab = CreateTabButton(L("RecentCaptures"), "\uE81C", () => ShowSection(OptionsSection.RecentCaptures));

        BuildPreferencesPanel();
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
        ApplyTabVisual(_preferencesTab, section == OptionsSection.Preferences);
        ApplyTabVisual(_recentTab, section == OptionsSection.RecentCaptures);

        if (section == OptionsSection.RecentCaptures)
        {
            RefreshRecentCaptures();
        }
        else
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
            Padding = new Thickness(18)
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(196) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var railStack = new StackPanel { Spacing = 10 };
        var brandRow = new Grid { Margin = new Thickness(2, 2, 2, 10) };
        brandRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        brandRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        brandRow.Children.Add(SnapvereBrand.CreateMark(42));
        var brandCopy = new StackPanel
        {
            Spacing = 1,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        brandCopy.Children.Add(SnapvereBrand.CreateWordmark(17));
        brandCopy.Children.Add(Text(
            UserText("Capture. Edit. Done.", "Snimi. Uredi. Gotovo."),
            8.5,
            SnapvereBrand.Muted));
        Grid.SetColumn(brandCopy, 1);
        brandRow.Children.Add(brandCopy);
        railStack.Children.Add(brandRow);

        var navLabel = Text(L("Settings").ToUpperInvariant(), 9, SnapvereBrand.Subtle, Microsoft.UI.Text.FontWeights.SemiBold);
        navLabel.CharacterSpacing = 120;
        navLabel.Margin = new Thickness(6, 4, 0, 4);
        railStack.Children.Add(navLabel);
        railStack.Children.Add(_preferencesTab);
        railStack.Children.Add(_recentTab);

        var local = new Border
        {
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(14),
            Background = Brush(0x20, 0x80, 0xE1, 0xE5),
            BorderBrush = Brush(0x55, 0x80, 0xE1, 0xE5),
            BorderThickness = new Thickness(1)
        };
        var localCopy = new StackPanel { Spacing = 3 };
        localCopy.Children.Add(Text(L("LocalFirst").ToUpperInvariant(), 8.5, SnapvereBrand.Ice, Microsoft.UI.Text.FontWeights.Bold));
        var localDetail = Text(
            UserText("Settings and captures stay local.", "Postavke i snimke ostaju lokalne."),
            9,
            SnapvereBrand.Muted);
        localDetail.TextWrapping = TextWrapping.Wrap;
        localCopy.Children.Add(localDetail);
        local.Child = localCopy;
        railStack.Children.Add(local);

        var rail = new Border
        {
            Background = SnapvereBrand.Surface,
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(14),
            Child = railStack
        };
        root.Children.Add(rail);

        var main = new Grid { Margin = new Thickness(18, 0, 0, 0) };
        main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        main.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        main.Children.Add(BuildHeader());

        _preferencesScroller.Content = _preferencesPanel;
        var host = new Grid();
        host.Children.Add(_preferencesScroller);
        host.Children.Add(_recentPanel);
        var contentFrame = new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(18),
            Background = SnapvereBrand.Surface,
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(1),
            Child = host
        };
        Grid.SetRow(contentFrame, 1);
        main.Children.Add(contentFrame);

        var footer = new Grid { Margin = new Thickness(2, 12, 2, 0) };
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
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var identity = new StackPanel { Spacing = 3 };
        identity.Children.Add(Text(L("Settings"), 27, SnapvereBrand.Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var subtitle = Text(
            UserText(
                "Fast access to capture behavior, local storage and recent files.",
                "Brz pristup ponašanju snimanja, lokalnoj pohrani i nedavnim datotekama."),
            11,
            SnapvereBrand.Muted);
        subtitle.TextWrapping = TextWrapping.Wrap;
        identity.Children.Add(subtitle);
        header.Children.Add(identity);

        var version = typeof(OptionsWindow).Assembly.GetName().Version?.ToString(3);
        var versionBadge = new Border
        {
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(12),
            Background = SnapvereBrand.Slate,
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Top,
            Child = Text(version is null ? "SNAPVERE" : $"v{version}", 9.5, SnapvereBrand.Muted, Microsoft.UI.Text.FontWeights.SemiBold)
        };
        Grid.SetColumn(versionBadge, 1);
        header.Children.Add(versionBadge);
        return header;
    }

    private void BuildPreferencesPanel()
    {
        for (var index = 0; index < 6; index++)
        {
            _preferencesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var intro = new StackPanel { Spacing = 4 };
        intro.Children.Add(Text(L("Settings"), 19, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var description = Text(L("PreferencesStoredLocally"), 11, Muted);
        description.TextWrapping = TextWrapping.Wrap;
        intro.Children.Add(description);
        _preferencesPanel.Children.Add(intro);

        AddPreferenceCard(
            row: 1,
            eyebrow: L("Startup").ToUpperInvariant(),
            title: L("StartWithWindows"),
            description: L("StartupDescription"),
            glyph: "\uE7E7",
            trailing: _startupToggle);

        AddPreferenceCard(
            row: 2,
            eyebrow: L("Capture").ToUpperInvariant(),
            title: L("IncludeCursor"),
            description: UserText(
                "Include the mouse pointer when the selected capture mode can include it.",
                "Uključi pokazivač miša kada ga odabrani način snimanja može prikazati."),
            glyph: "\uE7C9",
            trailing: _cursorToggle);

        AddPreferenceCard(
            row: 3,
            eyebrow: L("Capture").ToUpperInvariant(),
            title: UserText("Screen recording", "Snimanje zaslona"),
            description: UserText(
                "Start or stop local primary-display recording without returning to the tray menu.",
                "Pokrenite ili zaustavite lokalno snimanje primarnog zaslona bez povratka u tray izbornik."),
            glyph: "\uE714",
            trailing: _recordingActions);

        var languageButton = CreateSecondaryAction(
            L("ChooseLanguage"),
            "\uE774",
            () =>
            {
                Close();
                LanguagePickerWindow.ShowStandalone(_preferences);
            });
        AddPreferenceCard(
            row: 4,
            eyebrow: L("Language").ToUpperInvariant(),
            title: L("Language"),
            description: CurrentLanguageDescription(),
            glyph: "\uE774",
            trailing: languageButton);

        var local = new Border
        {
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(14),
            Background = Brush(0x38, 0x24, 0x4B, 0x42),
            BorderBrush = Brush(0x55, 0x4A, 0xB8, 0x98),
            BorderThickness = new Thickness(1)
        };
        var localCopy = new StackPanel { Spacing = 3 };
        localCopy.Children.Add(Text(L("LocalFirst").ToUpperInvariant(), 9, Success, Microsoft.UI.Text.FontWeights.Bold));
        var localDetail = Text(
            UserText(
                "Your preferences stay on this PC and are not uploaded by SNAPVERE.",
                "Vaše postavke ostaju na ovom računalu i SNAPVERE ih ne prenosi u oblak."),
            10,
            Muted);
        localDetail.TextWrapping = TextWrapping.Wrap;
        localCopy.Children.Add(localDetail);
        local.Child = localCopy;
        Grid.SetRow(local, 5);
        _preferencesPanel.Children.Add(local);
    }

    private string CurrentLanguageDescription()
    {
        var selected = SnapvereLocalization.SupportedLanguages.First(language =>
            string.Equals(language.Code, _preferences.Current.LanguageCode, StringComparison.OrdinalIgnoreCase));
        return UserText(
            $"Selected language: {selected.NativeName}.",
            $"Odabrani jezik: {selected.NativeName}.");
    }

    private void AddPreferenceCard(int row, string eyebrow, string title, string description, string glyph, FrameworkElement trailing)
    {
        var card = BuildSettingCard(eyebrow, title, description, glyph, trailing);
        card.Margin = new Thickness(0, row == 1 ? 16 : 10, 0, 0);
        Grid.SetRow(card, row);
        _preferencesPanel.Children.Add(card);
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

    private static Border BuildSettingCard(
        string eyebrow,
        string title,
        string description,
        string glyph,
        FrameworkElement trailing)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        grid.Children.Add(new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(11),
            Background = Brush(0x55, 0x61, 0x49, 0xC8),
            BorderBrush = Brush(0x66, 0x91, 0x7A, 0xF4),
            BorderThickness = new Thickness(1),
            Child = new FontIcon
            {
                Glyph = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 15,
                Foreground = Strong
            }
        });

        var copy = new StackPanel
        {
            Spacing = 3,
            Margin = new Thickness(8, 0, 20, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        copy.Children.Add(Text(eyebrow, 9, AccentText, Microsoft.UI.Text.FontWeights.Bold));
        copy.Children.Add(Text(title, 12, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var detail = Text(description, 10, Muted);
        detail.TextWrapping = TextWrapping.Wrap;
        copy.Children.Add(detail);
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);

        if (trailing is ToggleSwitch toggle)
        {
            toggle.Header = null;
        }
        trailing.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(trailing, 2);
        grid.Children.Add(trailing);

        grid.SizeChanged += (_, args) =>
        {
            var compact = args.NewSize.Width < 470;
            Grid.SetRow(trailing, compact ? 1 : 0);
            Grid.SetColumn(trailing, compact ? 1 : 2);
            Grid.SetColumnSpan(trailing, compact ? 2 : 1);
            trailing.HorizontalAlignment = compact
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Stretch;
            trailing.Margin = compact
                ? new Thickness(8, 12, 0, 0)
                : new Thickness(0);
            copy.Margin = compact
                ? new Thickness(8, 0, 0, 0)
                : new Thickness(8, 0, 20, 0);
        };

        return new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(15),
            Background = Elevated,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            Child = grid
        };
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
            FontFamily = new FontFamily("Segoe Fluent Icons"),
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
                FontFamily = new FontFamily("Segoe Fluent Icons"),
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
            FontFamily = new FontFamily("Segoe Fluent Icons"),
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
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSizeToWorkArea(this, 760, 700));
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
            OffContent = L("Off"),
            OnContent = L("On"),
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
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 14
        });
        content.Children.Add(Text(label, 10.5, SnapvereBrand.Strong, Microsoft.UI.Text.FontWeights.SemiBold));

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
        button.Foreground = active ? SnapvereBrand.Strong : SnapvereBrand.Muted;
    }

    private static Button CreateSecondaryAction(string label, string glyph, Action action)
    {
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        stack.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons"),
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
