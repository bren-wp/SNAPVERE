using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snapvere.Application.Capture;
using Snapvere.Shared;
using System.Diagnostics;
using Windows.Graphics;

namespace Snapvere.App.Services;

public enum OptionsSection
{
    Preferences,
    RecentCaptures
}

/// <summary>
/// On-demand settings and recent-captures surface. It contains only implemented
/// local preferences and performs no polling while SNAPVERE is idle.
/// </summary>
public sealed class OptionsWindow : Window
{
    private const int RecentCaptureLimit = 10;

    private readonly CaptureHistoryService _history;
    private readonly CapturePreferencesService _preferences;
    private readonly StartupRegistrationService _startupRegistration;
    private readonly string _languageCode;
    private readonly Grid _preferencesPanel = new();
    private readonly Grid _recentPanel = new() { Visibility = Visibility.Collapsed };
    private readonly StackPanel _recentItems = new() { Spacing = 8 };
    private readonly TextBlock _recentSummary;
    private readonly TextBlock _statusText;
    private readonly ToggleSwitch _startupToggle;
    private readonly ToggleSwitch _cursorToggle;
    private readonly Button _preferencesTab;
    private readonly Button _recentTab;
    private bool _sizeApplied;
    private bool _updatingControls;

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

        _startupToggle = CreateToggle(L("StartWithWindows"));
        _cursorToggle = CreateToggle(L("IncludeCursor"));
        _startupToggle.Toggled += StartupToggle_Toggled;
        _cursorToggle.Toggled += CursorToggle_Toggled;

        _preferencesTab = CreateTabButton(L("Settings"), () => ShowSection(OptionsSection.Preferences));
        _recentTab = CreateTabButton(L("RecentCaptures"), () => ShowSection(OptionsSection.RecentCaptures));

        BuildPreferencesPanel();
        BuildRecentPanel();
        Content = BuildContent();
        LoadPreferences();
        RefreshRecentCaptures();
        ShowSection(OptionsSection.Preferences);
        Activated += OptionsWindow_Activated;
    }

    public void ShowSection(OptionsSection section)
    {
        _preferencesPanel.Visibility = section == OptionsSection.Preferences ? Visibility.Visible : Visibility.Collapsed;
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

    private string LocalStatusText()
        => _languageCode == "hr"
            ? "Postavke su spremljene lokalno za ovaj Windows račun."
            : "Preferences are stored locally for this Windows account.";

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Canvas,
            Padding = new Thickness(24)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(BuildHeader());

        var tabs = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 22, 0, 16)
        };
        tabs.Children.Add(_preferencesTab);
        tabs.Children.Add(_recentTab);
        Grid.SetRow(tabs, 1);
        root.Children.Add(tabs);

        var host = new Grid();
        host.Children.Add(_preferencesPanel);
        host.Children.Add(_recentPanel);
        var contentFrame = new Border
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(20),
            Background = Surface,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            Child = host
        };
        Grid.SetRow(contentFrame, 2);
        root.Children.Add(contentFrame);

        var footer = new Grid { Margin = new Thickness(2, 14, 2, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _statusText.VerticalAlignment = VerticalAlignment.Center;
        footer.Children.Add(_statusText);

        var close = CreateSecondaryAction(L("Close"), "\uE711", Close);
        Grid.SetColumn(close, 1);
        footer.Children.Add(close);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);
        return root;
    }

    private FrameworkElement BuildHeader()
    {
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(BuildBrandMark());

        var identity = new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var brand = Text("SNAPVERE", 11, AccentText, Microsoft.UI.Text.FontWeights.Bold);
        brand.CharacterSpacing = 70;
        identity.Children.Add(brand);
        identity.Children.Add(Text(L("Settings"), 25, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);

        var version = typeof(OptionsWindow).Assembly.GetName().Version?.ToString(3) ?? "dev";
        var versionBadge = new Border
        {
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(11),
            Background = Elevated,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = Text($"v{version}", 10, Muted, Microsoft.UI.Text.FontWeights.SemiBold)
        };
        Grid.SetColumn(versionBadge, 2);
        header.Children.Add(versionBadge);
        return header;
    }

    private void BuildPreferencesPanel()
    {
        for (var index = 0; index < 5; index++)
        {
            _preferencesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var intro = new StackPanel { Spacing = 4 };
        intro.Children.Add(Text(L("Settings"), 19, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var description = Text(
            _languageCode == "hr"
                ? "Prikazane su samo postavke koje su stvarno implementirane i lokalno spremljene."
                : "Only implemented, locally persisted settings are shown here.",
            11,
            Muted);
        description.TextWrapping = TextWrapping.Wrap;
        intro.Children.Add(description);
        _preferencesPanel.Children.Add(intro);

        AddPreferenceCard(
            row: 1,
            eyebrow: "STARTUP",
            title: L("StartWithWindows"),
            description: _languageCode == "hr"
                ? "Pokreće SNAPVERE tiho u području obavijesti nakon prijave u Windows."
                : "Launch SNAPVERE quietly in the notification area after Windows sign-in.",
            glyph: "\uE7E7",
            trailing: _startupToggle);

        AddPreferenceCard(
            row: 2,
            eyebrow: "CAPTURE",
            title: L("IncludeCursor"),
            description: _languageCode == "hr"
                ? "Uključi pokazivač kada aktivni capture backend podržava njegovo snimanje."
                : "Include the pointer when the active capture backend supports cursor composition.",
            glyph: "\uE7C9",
            trailing: _cursorToggle);

        var languageButton = CreateSecondaryAction(L("ChooseLanguage"), "\uE774", LanguagePickerWindow.ShowStandalone);
        AddPreferenceCard(
            row: 3,
            eyebrow: "LANGUAGE",
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
        var path = Text($"Settings: {_preferences.SettingsPath}", 10, Muted);
        path.TextWrapping = TextWrapping.Wrap;
        localCopy.Children.Add(path);
        local.Child = localCopy;
        Grid.SetRow(local, 4);
        _preferencesPanel.Children.Add(local);
    }

    private string CurrentLanguageDescription()
    {
        var selected = SnapvereLocalization.SupportedLanguages.First(language =>
            string.Equals(language.Code, _preferences.Current.LanguageCode, StringComparison.OrdinalIgnoreCase));
        return _languageCode == "hr"
            ? $"Trenutačno: {selected.NativeName}. Engleski je zadani fallback jezik."
            : $"Current: {selected.NativeName}. English is the default fallback language.";
    }

    private void AddPreferenceCard(int row, string eyebrow, string title, string description, string glyph, Control trailing)
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

        var refresh = CreateSecondaryAction(_languageCode == "hr" ? "Osvježi" : "Refresh", "\uE72C", RefreshRecentCaptures);
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
        Control trailing)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

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
            SetStatus($"Could not read one or more preferences: {exception.Message}", Warning);
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
            SetStatus(
                requestedState
                    ? (_languageCode == "hr" ? "Automatsko pokretanje s Windowsima je uključeno." : "Windows startup enabled — SNAPVERE will start quietly in the tray.")
                    : (_languageCode == "hr" ? "Automatsko pokretanje s Windowsima je isključeno." : "Windows startup disabled."),
                Success);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
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
            SetStatus($"Windows startup setting could not be changed: {exception.Message}", Error);
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
            SetStatus(
                _cursorToggle.IsOn
                    ? (_languageCode == "hr" ? "Snimanje pokazivača je uključeno." : "Cursor capture enabled for supported capture paths.")
                    : (_languageCode == "hr" ? "Snimanje pokazivača je isključeno." : "Cursor capture disabled."),
                Success);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
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
            SetStatus($"Cursor preference could not be saved: {exception.Message}", Error);
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
                1 => _languageCode == "hr" ? "1 lokalna snimka" : "1 local capture",
                _ => _languageCode == "hr" ? $"{captures.Count} lokalnih snimki" : $"{captures.Count} local captures"
            };

            if (captures.Count == 0)
            {
                _recentItems.Children.Add(BuildEmptyHistory());
                return;
            }

            foreach (var capture in captures)
            {
                _recentItems.Children.Add(CreateRecentCaptureButton(capture));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _recentSummary.Text = _languageCode == "hr" ? "Lokalna povijest nije dostupna" : "Local history unavailable";
            var error = Text(exception.Message, 10, Warning);
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
            _languageCode == "hr" ? "Još nema snimki" : "No captures yet",
            12,
            Strong,
            Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment.Center));
        var detail = Text(
            _languageCode == "hr"
                ? "Koristi Print Screen ili tray ikonu za prvu snimku područja."
                : "Use Print Screen or the tray icon to create your first region capture.",
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

    private Button CreateRecentCaptureButton(CaptureHistoryItem capture)
    {
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
        text.Children.Add(Text(capture.FileName, 11, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        text.Children.Add(Text(capture.MetadataText, 9, Subtle));
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

        var button = new Button
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
        AutomationProperties.SetName(button, $"Open {capture.FileName}");
        button.Click += (_, _) => OpenCapture(capture);
        return button;
    }

    private void OpenCapture(CaptureHistoryItem capture)
    {
        try
        {
            if (!File.Exists(capture.FilePath))
            {
                RefreshRecentCaptures();
                SetStatus("That capture is no longer available at its original path.", Warning);
                return;
            }
            _ = Process.Start(new ProcessStartInfo(capture.FilePath) { UseShellExecute = true });
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            SetStatus($"Windows could not open that capture: {exception.Message}", Error);
        }
    }

    private void OpenCaptureFolder()
    {
        try
        {
            var directory = _history.GetCaptureDirectory();
            Directory.CreateDirectory(directory);
            _ = Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            SetStatus($"Windows could not open the capture folder: {exception.Message}", Error);
        }
    }

    private void OptionsWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_sizeApplied)
        {
            return;
        }
        _sizeApplied = true;
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSize(this, 760, 700));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
        }
    }

    private static ToggleSwitch CreateToggle(string accessibleName)
    {
        var toggle = new ToggleSwitch
        {
            OffContent = "Off",
            OnContent = "On",
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(toggle, accessibleName);
        return toggle;
    }

    private static Button CreateTabButton(string label, Action action)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(14, 8, 14, 8),
            CornerRadius = new CornerRadius(10),
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
        button.Background = active ? Brush(0x55, 0x5E, 0x48, 0xBD) : Transparent;
        button.BorderBrush = active ? Brush(0x78, 0x9D, 0x86, 0xFF) : Transparent;
        button.Foreground = active ? Strong : Muted;
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

    private static FrameworkElement BuildBrandMark()
    {
        var mark = new Grid { Width = 44, Height = 44 };
        mark.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(14),
            Background = AccentGradient(),
            BorderBrush = Brush(0x66, 0xCA, 0xC1, 0xFF),
            BorderThickness = new Thickness(1)
        });
        mark.Children.Add(new FontIcon
        {
            Glyph = "\uE722",
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 18,
            Foreground = Strong,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        return mark;
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

    private static LinearGradientBrush AccentGradient()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1)
        };
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x61, 0x4A, 0xE8), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x8C, 0x5A, 0xF4), Offset = 0.62 });
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x36, 0xB6, 0xD5), Offset = 1 });
        return brush;
    }

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Canvas => Brush(0xFF, 0x07, 0x08, 0x0D);
    private static SolidColorBrush Surface => Brush(0xE8, 0x0D, 0x13, 0x21);
    private static SolidColorBrush Elevated => Brush(0xFF, 0x12, 0x17, 0x24);
    private static SolidColorBrush Outline => Brush(0xFF, 0x34, 0x40, 0x57);
    private static SolidColorBrush Strong => Brush(0xFF, 0xF6, 0xF5, 0xFB);
    private static SolidColorBrush Muted => Brush(0xFF, 0xAE, 0xB5, 0xC6);
    private static SolidColorBrush Subtle => Brush(0xFF, 0x7D, 0x87, 0x9E);
    private static SolidColorBrush AccentText => Brush(0xFF, 0xAE, 0x9C, 0xFF);
    private static SolidColorBrush Success => Brush(0xFF, 0x72, 0xD8, 0xB4);
    private static SolidColorBrush Warning => Brush(0xFF, 0xE2, 0xB5, 0x72);
    private static SolidColorBrush Error => Brush(0xFF, 0xF0, 0x8C, 0x9A);
    private static SolidColorBrush Transparent => Brush(0x00, 0, 0, 0);
}
