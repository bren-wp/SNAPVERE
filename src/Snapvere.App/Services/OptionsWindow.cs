using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snapvere.Application.Capture;
using System.Diagnostics;
using Windows.Graphics;

namespace Snapvere.App.Services;

public enum OptionsSection
{
    Preferences,
    RecentCaptures
}

/// <summary>
/// Premium secondary SNAPVERE surface for real local preferences and recent
/// captures. Capture actions stay in tray/hotkeys/overlays.
/// </summary>
public sealed class OptionsWindow : Window
{
    private const int RecentCaptureLimit = 10;

    private readonly CaptureHistoryService _history;
    private readonly CapturePreferencesService _preferences;
    private readonly StartupRegistrationService _startupRegistration;
    private readonly Grid _preferencesPanel;
    private readonly Grid _recentPanel;
    private readonly StackPanel _recentItems;
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

        Title = "SNAPVERE Options";
        _preferencesPanel = new Grid();
        _recentPanel = new Grid { Visibility = Visibility.Collapsed };
        _recentItems = new StackPanel { Spacing = 8 };
        _recentSummary = Text("Pictures\\SNAPVERE", 10, Subtle);
        _statusText = Text("Preferences are local to this Windows account.", 10, Success);
        _statusText.TextWrapping = TextWrapping.Wrap;

        _startupToggle = CreateToggle("Start SNAPVERE with Windows");
        _cursorToggle = CreateToggle("Include cursor on capture");
        _startupToggle.Toggled += StartupToggle_Toggled;
        _cursorToggle.Toggled += CursorToggle_Toggled;

        _preferencesTab = CreateTabButton("Preferences", () => ShowSection(OptionsSection.Preferences));
        _recentTab = CreateTabButton("Recent captures", () => ShowSection(OptionsSection.RecentCaptures));

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

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Brush(0xFF, 0x07, 0x08, 0x0D),
            Padding = new Thickness(24)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        header.Children.Add(BuildBrandMark());
        var identity = new StackPanel { Spacing = 2, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var brand = Text("SNAPVERE", 11, AccentText, Microsoft.UI.Text.FontWeights.Bold);
        brand.CharacterSpacing = 70;
        identity.Children.Add(brand);
        identity.Children.Add(Text("Options", 25, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);

        var version = typeof(OptionsWindow).Assembly.GetName().Version?.ToString(3) ?? "dev";
        var versionBadge = new Border
        {
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(11),
            Background = Brush(0xFF, 0x12, 0x14, 0x1D),
            BorderBrush = Brush(0xFF, 0x2B, 0x2F, 0x3D),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = Text($"v{version}", 10, Muted, Microsoft.UI.Text.FontWeights.SemiBold)
        };
        Grid.SetColumn(versionBadge, 2);
        header.Children.Add(versionBadge);
        root.Children.Add(header);

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

        var contentFrame = new Border
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(20),
            Background = Brush(0xE8, 0x0D, 0x0F, 0x16),
            BorderBrush = Brush(0xFF, 0x25, 0x29, 0x36),
            BorderThickness = new Thickness(1)
        };
        var contentHost = new Grid();
        contentHost.Children.Add(_preferencesPanel);
        contentHost.Children.Add(_recentPanel);
        contentFrame.Child = contentHost;
        Grid.SetRow(contentFrame, 2);
        root.Children.Add(contentFrame);

        var footer = new Grid { Margin = new Thickness(2, 14, 2, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _statusText.VerticalAlignment = VerticalAlignment.Center;
        footer.Children.Add(_statusText);

        var close = new Button
        {
            Content = "Close",
            Padding = new Thickness(18, 8, 18, 8),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0xFF, 0x18, 0x1B, 0x25),
            BorderBrush = Brush(0xFF, 0x32, 0x36, 0x45),
            BorderThickness = new Thickness(1)
        };
        AutomationProperties.SetName(close, "Close SNAPVERE Options");
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1);
        footer.Children.Add(close);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        return root;
    }

    private void BuildPreferencesPanel()
    {
        _preferencesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _preferencesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _preferencesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _preferencesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var intro = new StackPanel { Spacing = 4 };
        intro.Children.Add(Text("Preferences", 19, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var description = Text("Only settings that are implemented and persisted locally are shown here.", 11, Muted);
        description.TextWrapping = TextWrapping.Wrap;
        intro.Children.Add(description);
        _preferencesPanel.Children.Add(intro);

        var startup = BuildSettingCard(
            "STARTUP",
            "Start SNAPVERE with Windows",
            "Launch quietly into the notification area after sign-in. No large window is opened.",
            "\uE7E7",
            _startupToggle);
        startup.Margin = new Thickness(0, 16, 0, 0);
        Grid.SetRow(startup, 1);
        _preferencesPanel.Children.Add(startup);

        var cursor = BuildSettingCard(
            "CAPTURE",
            "Include cursor on capture",
            "Include the pointer when the active capture backend supports cursor composition.",
            "\uE7C9",
            _cursorToggle);
        cursor.Margin = new Thickness(0, 10, 0, 0);
        Grid.SetRow(cursor, 2);
        _preferencesPanel.Children.Add(cursor);

        var local = new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(14),
            Background = Brush(0x38, 0x24, 0x4B, 0x42),
            BorderBrush = Brush(0x55, 0x4A, 0xB8, 0x98),
            BorderThickness = new Thickness(1)
        };
        var localCopy = new StackPanel { Spacing = 3 };
        localCopy.Children.Add(Text("LOCAL-FIRST", 9, Success, Microsoft.UI.Text.FontWeights.Bold));
        var path = Text($"Settings: {_preferences.SettingsPath}", 10, Muted);
        path.TextWrapping = TextWrapping.Wrap;
        localCopy.Children.Add(path);
        local.Child = localCopy;
        Grid.SetRow(local, 3);
        _preferencesPanel.Children.Add(local);
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
        copy.Children.Add(Text("Recent captures", 19, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        copy.Children.Add(_recentSummary);
        header.Children.Add(copy);

        var refresh = CreateSmallAction("Refresh", "\uE72C", RefreshRecentCaptures);
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

        var folder = CreateSmallAction("Open capture folder", "\uE838", OpenCaptureFolder);
        folder.HorizontalAlignment = HorizontalAlignment.Left;
        folder.Margin = new Thickness(0, 14, 0, 0);
        Grid.SetRow(folder, 2);
        _recentPanel.Children.Add(folder);
    }

    private static Border BuildSettingCard(string eyebrow, string title, string description, string glyph, ToggleSwitch toggle)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new Border
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
        };
        grid.Children.Add(icon);

        var copy = new StackPanel { Spacing = 3, Margin = new Thickness(8, 0, 20, 0), VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(Text(eyebrow, 9, AccentText, Microsoft.UI.Text.FontWeights.Bold));
        copy.Children.Add(Text(title, 12, Strong, Microsoft.UI.Text.FontWeights.SemiBold));
        var detail = Text(description, 10, Muted);
        detail.TextWrapping = TextWrapping.Wrap;
        copy.Children.Add(detail);
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);

        toggle.Header = null;
        toggle.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(toggle, 2);
        grid.Children.Add(toggle);

        return new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(15),
            Background = Brush(0xFF, 0x12, 0x14, 0x1D),
            BorderBrush = Brush(0xFF, 0x2A, 0x2E, 0x3A),
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
            SetStatus("Preferences are stored locally on this PC.", Success);
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

        try
        {
            _startupRegistration.SetEnabled(_startupToggle.IsOn);
            SetStatus(
                _startupToggle.IsOn
                    ? "Windows startup enabled — SNAPVERE will start quietly in the tray."
                    : "Windows startup disabled.",
                Success);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _updatingControls = true;
            try
            {
                _startupToggle.IsOn = _startupRegistration.IsEnabled();
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
                    ? "Cursor capture enabled for supported capture paths."
                    : "Cursor capture disabled.",
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
                1 => "1 local capture",
                _ => $"{captures.Count} local captures"
            };

            if (captures.Count == 0)
            {
                var empty = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
                empty.Children.Add(new FontIcon
                {
                    Glyph = "\uE91B",
                    FontFamily = new FontFamily("Segoe Fluent Icons"),
                    FontSize = 24,
                    Foreground = AccentText
                });
                empty.Children.Add(Text("No captures yet", 12, Strong, Microsoft.UI.Text.FontWeights.SemiBold, HorizontalAlignment.Center));
                empty.Children.Add(Text("Use Print Screen or the tray icon to create your first region capture.", 10, Subtle, null, HorizontalAlignment.Center));
                _recentItems.Children.Add(new Border
                {
                    Padding = new Thickness(22),
                    CornerRadius = new CornerRadius(15),
                    Background = Brush(0xFF, 0x10, 0x12, 0x1A),
                    BorderBrush = Brush(0xFF, 0x28, 0x2C, 0x38),
                    BorderThickness = new Thickness(1),
                    Child = empty
                });
                return;
            }

            foreach (var capture in captures)
            {
                _recentItems.Children.Add(CreateRecentCaptureButton(capture));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _recentSummary.Text = "Local history unavailable";
            var error = Text(exception.Message, 10, Warning);
            error.TextWrapping = TextWrapping.Wrap;
            _recentItems.Children.Add(error);
        }
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
            Background = Brush(0xFF, 0x12, 0x14, 0x1D),
            BorderBrush = Brush(0xFF, 0x28, 0x2C, 0x38),
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
        AppWindow.Resize(new SizeInt32(720, 620));
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
        button.BorderBrush = active ? Brush(0x78, 0x9D, 0x86, 0xFF) : Brush(0x00, 0, 0, 0);
        button.Foreground = active ? Strong : Muted;
    }

    private static Button CreateSmallAction(string label, string glyph, Action action)
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
            Background = Brush(0xFF, 0x16, 0x18, 0x22),
            BorderBrush = Brush(0xFF, 0x2C, 0x30, 0x3D),
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

    private static SolidColorBrush Strong => Brush(0xFF, 0xF6, 0xF5, 0xFB);
    private static SolidColorBrush Muted => Brush(0xFF, 0xAE, 0xAC, 0xBC);
    private static SolidColorBrush Subtle => Brush(0xFF, 0x7D, 0x7C, 0x8D);
    private static SolidColorBrush AccentText => Brush(0xFF, 0xAE, 0x9C, 0xFF);
    private static SolidColorBrush Success => Brush(0xFF, 0x72, 0xD8, 0xB4);
    private static SolidColorBrush Warning => Brush(0xFF, 0xE2, 0xB5, 0x72);
    private static SolidColorBrush Error => Brush(0xFF, 0xF0, 0x8C, 0x9A);
    private static SolidColorBrush Transparent => Brush(0x00, 0, 0, 0);
}
