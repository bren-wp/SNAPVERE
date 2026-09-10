using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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
/// Secondary SNAPVERE control surface. Capture actions intentionally stay in
/// the tray, hotkeys and overlays; this window contains only real preferences
/// and local recent-capture management.
/// </summary>
public sealed class OptionsWindow : Window
{
    private const int RecentCaptureLimit = 10;

    private readonly CaptureHistoryService _history;
    private readonly CapturePreferencesService _preferences;
    private readonly StartupRegistrationService _startupRegistration;
    private readonly StackPanel _preferencesPanel;
    private readonly StackPanel _recentPanel;
    private readonly StackPanel _recentItems;
    private readonly TextBlock _recentSummary;
    private readonly TextBlock _statusText;
    private readonly ToggleSwitch _startupToggle;
    private readonly ToggleSwitch _cursorToggle;
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
        _preferencesPanel = new StackPanel { Spacing = 10 };
        _recentPanel = new StackPanel { Spacing = 10, Visibility = Visibility.Collapsed };
        _recentItems = new StackPanel { Spacing = 6 };
        _recentSummary = Text("Pictures\\SNAPVERE", 11, Brush(0x92, 0x9E, 0xB3));
        _statusText = Text(string.Empty, 10, Brush(0xA8, 0xB5, 0xCC));
        _statusText.TextWrapping = TextWrapping.Wrap;

        _startupToggle = CreateToggle(
            "Start SNAPVERE with Windows",
            "Starts silently in the notification area for this Windows account.");
        _cursorToggle = CreateToggle(
            "Include cursor on capture",
            "Applies to Region, Window and Screen Capture when the backend supports cursor capture.");

        BuildPreferencesPanel();
        BuildRecentPanel();
        Content = BuildContent();
        LoadPreferences();
        RefreshRecentCaptures();
        Activated += OptionsWindow_Activated;
    }

    public void ShowSection(OptionsSection section)
    {
        _preferencesPanel.Visibility = section == OptionsSection.Preferences
            ? Visibility.Visible
            : Visibility.Collapsed;
        _recentPanel.Visibility = section == OptionsSection.RecentCaptures
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (section == OptionsSection.RecentCaptures)
        {
            RefreshRecentCaptures();
        }
        else
        {
            LoadPreferences();
        }
    }

    private UIElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Brush(0x0B, 0x0F, 0x19),
            Padding = new Thickness(22)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var brand = new StackPanel { Spacing = 2 };
        brand.Children.Add(Text("SNAPVERE", 11, Brush(0xA7, 0x91, 0xFF), Microsoft.UI.Text.FontWeights.SemiBold));
        brand.Children.Add(Text("Options", 24, Brush(0xF7, 0xF5, 0xFF), Microsoft.UI.Text.FontWeights.SemiBold));
        var subtitle = Text("Tray-first preferences and local capture history.", 11, Brush(0x9C, 0xA8, 0xBB));
        subtitle.TextWrapping = TextWrapping.Wrap;
        brand.Children.Add(subtitle);
        header.Children.Add(brand);

        var version = typeof(OptionsWindow).Assembly.GetName().Version?.ToString(3) ?? "dev";
        var versionBadge = new Border
        {
            Padding = new Thickness(9, 5, 9, 5),
            CornerRadius = new CornerRadius(8),
            Background = Brush(0x19, 0x20, 0x30),
            BorderBrush = Brush(0x38, 0x46, 0x64),
            BorderThickness = new Thickness(1),
            Child = Text($"v{version}", 10, Brush(0xC9, 0xD1, 0xDF))
        };
        Grid.SetColumn(versionBadge, 1);
        header.Children.Add(versionBadge);
        root.Children.Add(header);

        var nav = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 18, 0, 14)
        };
        nav.Children.Add(CreateNavButton("Preferences", () => ShowSection(OptionsSection.Preferences), primary: true));
        nav.Children.Add(CreateNavButton("Recent captures", () => ShowSection(OptionsSection.RecentCaptures)));
        Grid.SetRow(nav, 1);
        root.Children.Add(nav);

        var contentHost = new Grid();
        contentHost.Children.Add(_preferencesPanel);
        contentHost.Children.Add(_recentPanel);
        Grid.SetRow(contentHost, 2);
        root.Children.Add(contentHost);

        var footer = new Grid
        {
            Margin = new Thickness(0, 14, 0, 0)
        };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _statusText.VerticalAlignment = VerticalAlignment.Center;
        footer.Children.Add(_statusText);

        var close = new Button
        {
            Content = "Close",
            Padding = new Thickness(18, 8, 18, 8),
            CornerRadius = new CornerRadius(8)
        };
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1);
        footer.Children.Add(close);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        return root;
    }

    private void BuildPreferencesPanel()
    {
        _preferencesPanel.Children.Add(CreateSectionTitle(
            "Preferences",
            "Only settings that are fully implemented are shown here."));
        _preferencesPanel.Children.Add(CreateSettingCard(_startupToggle));
        _preferencesPanel.Children.Add(CreateSettingCard(_cursorToggle));

        var path = Text(
            $"Settings are stored locally at {_preferences.SettingsPath}",
            10,
            Brush(0x7F, 0x8B, 0xA0));
        path.TextWrapping = TextWrapping.Wrap;
        _preferencesPanel.Children.Add(path);

        _startupToggle.Toggled += StartupToggle_Toggled;
        _cursorToggle.Toggled += CursorToggle_Toggled;
    }

    private void BuildRecentPanel()
    {
        var title = new Grid();
        title.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        title.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var heading = new StackPanel { Spacing = 2 };
        heading.Children.Add(Text("Recent captures", 16, Brush(0xF5, 0xF4, 0xFA), Microsoft.UI.Text.FontWeights.SemiBold));
        heading.Children.Add(_recentSummary);
        title.Children.Add(heading);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        var folder = CreateCompactButton("Open folder", OpenCaptureFolder);
        var refresh = CreateCompactButton("Refresh", RefreshRecentCaptures);
        actions.Children.Add(folder);
        actions.Children.Add(refresh);
        Grid.SetColumn(actions, 1);
        title.Children.Add(actions);
        _recentPanel.Children.Add(title);

        _recentPanel.Children.Add(new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(11),
            Background = Brush(0x11, 0x17, 0x24),
            BorderBrush = Brush(0x2B, 0x37, 0x50),
            BorderThickness = new Thickness(1),
            Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 385,
                Content = _recentItems
            }
        });
    }

    private void LoadPreferences()
    {
        _updatingControls = true;
        try
        {
            _cursorToggle.IsOn = _preferences.Current.IncludeCursorOnCapture;
            _startupToggle.IsOn = _startupRegistration.IsEnabled();
            _statusText.Text = "Preferences are local to this Windows account.";
            _statusText.Foreground = Brush(0x83, 0xC7, 0xA7);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _statusText.Text = $"Could not read one or more preferences: {exception.Message}";
            _statusText.Foreground = Brush(0xE2, 0xA4, 0x73);
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
            _statusText.Text = _startupToggle.IsOn
                ? "SNAPVERE will start silently in the tray when you sign in."
                : "Windows startup registration removed.";
            _statusText.Foreground = Brush(0x83, 0xC7, 0xA7);
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

            _statusText.Text = $"Windows startup setting could not be changed: {exception.Message}";
            _statusText.Foreground = Brush(0xE8, 0x8D, 0x8D);
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
            _statusText.Text = _cursorToggle.IsOn
                ? "Cursor capture enabled for supported capture paths."
                : "Cursor capture disabled.";
            _statusText.Foreground = Brush(0x83, 0xC7, 0xA7);
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

            _statusText.Text = $"Cursor preference could not be saved: {exception.Message}";
            _statusText.Foreground = Brush(0xE8, 0x8D, 0x8D);
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
                1 => "1 recent local capture",
                _ => $"{captures.Count} recent local captures"
            };

            if (captures.Count == 0)
            {
                var empty = Text("No local captures yet.", 11, Brush(0x8F, 0x9A, 0xAF));
                _recentItems.Children.Add(empty);
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
            var error = Text(exception.Message, 11, Brush(0xE2, 0xA4, 0x73));
            error.TextWrapping = TextWrapping.Wrap;
            _recentItems.Children.Add(error);
        }
    }

    private Button CreateRecentCaptureButton(CaptureHistoryItem capture)
    {
        var text = new StackPanel { Spacing = 2 };
        text.Children.Add(Text(capture.FileName, 11, Brush(0xF3, 0xF5, 0xF9), Microsoft.UI.Text.FontWeights.SemiBold));
        text.Children.Add(Text(capture.MetadataText, 9, Brush(0x8F, 0x9A, 0xAF)));

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 8, 10, 8),
            CornerRadius = new CornerRadius(8),
            Background = Brush(0x16, 0x1E, 0x2D),
            BorderBrush = Brush(0x2C, 0x38, 0x51),
            BorderThickness = new Thickness(1),
            Content = text
        };
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
                _statusText.Text = "That capture is no longer available at its original path.";
                _statusText.Foreground = Brush(0xE2, 0xA4, 0x73);
                return;
            }

            _ = Process.Start(new ProcessStartInfo(capture.FilePath) { UseShellExecute = true });
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            _statusText.Text = $"Windows could not open that capture: {exception.Message}";
            _statusText.Foreground = Brush(0xE8, 0x8D, 0x8D);
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
            _statusText.Text = $"Windows could not open the capture folder: {exception.Message}";
            _statusText.Foreground = Brush(0xE8, 0x8D, 0x8D);
        }
    }

    private void OptionsWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_sizeApplied)
        {
            return;
        }

        _sizeApplied = true;
        AppWindow.Resize(new SizeInt32(680, 620));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
        }
    }

    private static ToggleSwitch CreateToggle(string header, string description)
    {
        var toggle = new ToggleSwitch
        {
            Header = header,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        ToolTipService.SetToolTip(toggle, description);
        return toggle;
    }

    private static Border CreateSettingCard(ToggleSwitch toggle)
    {
        var description = toggle.Header?.ToString() switch
        {
            "Start SNAPVERE with Windows" => "Per-user startup registration. SNAPVERE starts tray-only; no Capture Center is opened.",
            _ => "Saved locally and applied by the capture workflows."
        };

        var stack = new StackPanel { Spacing = 5 };
        stack.Children.Add(toggle);
        var detail = Text(description, 10, Brush(0x8F, 0x9A, 0xAF));
        detail.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(detail);
        return new Border
        {
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(11),
            Background = Brush(0x12, 0x18, 0x26),
            BorderBrush = Brush(0x2C, 0x38, 0x51),
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private static StackPanel CreateSectionTitle(string title, string subtitle)
    {
        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(Text(title, 16, Brush(0xF5, 0xF4, 0xFA), Microsoft.UI.Text.FontWeights.SemiBold));
        var detail = Text(subtitle, 10, Brush(0x8F, 0x9A, 0xAF));
        detail.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(detail);
        return stack;
    }

    private static Button CreateNavButton(string label, Action action, bool primary = false)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(14, 7, 14, 7),
            CornerRadius = new CornerRadius(8),
            Background = primary ? Brush(0x43, 0x31, 0xA0) : Brush(0x16, 0x1E, 0x2D),
            BorderBrush = primary ? Brush(0x86, 0x6D, 0xFF) : Brush(0x2C, 0x38, 0x51),
            BorderThickness = new Thickness(1)
        };
        button.Click += (_, _) => action();
        return button;
    }

    private static Button CreateCompactButton(string label, Action action)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(7)
        };
        button.Click += (_, _) => action();
        return button;
    }

    private static TextBlock Text(
        string value,
        double size,
        SolidColorBrush foreground,
        Windows.UI.Text.FontWeight? weight = null)
        => new()
        {
            Text = value,
            FontSize = size,
            Foreground = foreground,
            FontWeight = weight ?? Microsoft.UI.Text.FontWeights.Normal
        };

    private static SolidColorBrush Brush(byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(0xFF, red, green, blue));
}
