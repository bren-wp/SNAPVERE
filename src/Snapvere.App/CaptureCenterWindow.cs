using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snapvere.App.Services;
using Snapvere.Application.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;
using System.Diagnostics;

namespace Snapvere.App;

/// <summary>
/// Premium Windows shell for SNAPVERE. Manual launches expose the product shell,
/// while Windows startup can remain background/tray-first. Capture operations
/// are the same production workflows used by hotkeys and the tray.
/// </summary>
public sealed class CaptureCenterWindow : Window
{
    private const int RecentCaptureLimit = 5;

    private readonly ScreenCaptureWorkflow _screenCaptureWorkflow;
    private readonly RegionCaptureWorkflow _regionCaptureWorkflow;
    private readonly WindowCaptureWorkflow _windowCaptureWorkflow;
    private readonly WindowTargetPicker _windowTargetPicker;
    private readonly CaptureHistoryService _captureHistoryService;
    private readonly CapturePreferencesService _capturePreferencesService;
    private readonly StartupRegistrationService _startupRegistrationService;
    private readonly PngCaptureEncoder _pngEncoder;

    private readonly Button _regionCaptureButton;
    private readonly Button _windowCaptureButton;
    private readonly Button _screenCaptureButton;
    private readonly TextBlock _statusTitle;
    private readonly TextBlock _statusMessage;
    private readonly Border _statusPanel;
    private readonly TextBlock _recentSummary;
    private readonly StackPanel _recentItems;
    private readonly StackPanel _historyItems;
    private readonly Grid _contentHost;
    private readonly Grid _root;
    private readonly Dictionary<ShellPage, Button> _navigationButtons = [];

    private RegionCaptureWindow? _regionCaptureWindow;
    private ShellPage _activePage = ShellPage.Capture;
    private bool _captureInProgress;
    private bool _initialSizeApplied;
    private bool _suppressSettingsEvents;

    public CaptureCenterWindow(
        ScreenCaptureWorkflow screenCaptureWorkflow,
        RegionCaptureWorkflow regionCaptureWorkflow,
        WindowCaptureWorkflow windowCaptureWorkflow,
        WindowTargetPicker windowTargetPicker,
        CaptureHistoryService captureHistoryService,
        CapturePreferencesService capturePreferencesService,
        StartupRegistrationService startupRegistrationService,
        PngCaptureEncoder pngEncoder)
    {
        _screenCaptureWorkflow = screenCaptureWorkflow ?? throw new ArgumentNullException(nameof(screenCaptureWorkflow));
        _regionCaptureWorkflow = regionCaptureWorkflow ?? throw new ArgumentNullException(nameof(regionCaptureWorkflow));
        _windowCaptureWorkflow = windowCaptureWorkflow ?? throw new ArgumentNullException(nameof(windowCaptureWorkflow));
        _windowTargetPicker = windowTargetPicker ?? throw new ArgumentNullException(nameof(windowTargetPicker));
        _captureHistoryService = captureHistoryService ?? throw new ArgumentNullException(nameof(captureHistoryService));
        _capturePreferencesService = capturePreferencesService ?? throw new ArgumentNullException(nameof(capturePreferencesService));
        _startupRegistrationService = startupRegistrationService ?? throw new ArgumentNullException(nameof(startupRegistrationService));
        _pngEncoder = pngEncoder ?? throw new ArgumentNullException(nameof(pngEncoder));

        Title = "SNAPVERE — Capture. Edit. Done.";

        _regionCaptureButton = CreateAccentButton("Capture region", "\uE722", RegionCaptureButton_Click);
        _windowCaptureButton = CreateModeButton("Capture window", "\uE7F4", WindowCaptureButton_Click);
        _screenCaptureButton = CreateModeButton("Capture screen", "\uE7F8", ScreenCaptureButton_Click);

        _statusTitle = Text("Ready to capture", 13, ForegroundStrong, FontWeights.SemiBold);
        _statusMessage = Text(
            "Press Print Screen or choose a capture mode. Your screenshots stay local.",
            11,
            ForegroundMuted);
        _statusMessage.TextWrapping = TextWrapping.Wrap;
        _statusPanel = BuildStatusPanel();

        _recentSummary = Text("Pictures\\SNAPVERE", 10, ForegroundSubtle);
        _recentItems = new StackPanel { Spacing = 7 };
        _historyItems = new StackPanel { Spacing = 9 };
        _contentHost = new Grid();

        _root = BuildWindowContent();
        Content = _root;
        Activated += CaptureCenterWindow_Activated;

        NavigateTo(ShellPage.Capture);
        RefreshRecentCaptures();
    }

    public void StartCaptureFromHotkey(CaptureMode mode)
    {
        switch (mode)
        {
            case CaptureMode.Region:
                _ = ExecuteRegionCaptureAsync();
                break;
            case CaptureMode.Window:
                _ = ExecuteWindowCaptureAsync();
                break;
            case CaptureMode.FullScreen:
            case CaptureMode.Monitor:
                _ = ExecuteScreenCaptureAsync();
                break;
        }
    }

    public void ShowFromTray()
    {
        if (_captureInProgress)
        {
            return;
        }

        AppWindow.Show();
        Activate();
    }

    public void ApplyHotkeyRegistrationReport(GlobalHotkeyRegistrationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (!report.HasConflicts)
        {
            ShowStatus(
                "Capture hotkeys are ready",
                "Print Screen starts Region Capture. Ctrl + Shift + 2 captures a window and Ctrl + Shift + 4 captures the screen.",
                StatusKind.Success);
            return;
        }

        var printScreenConflict = report.Conflicts.Any(
            conflict => string.Equals(conflict.Binding.GestureText, "Print Screen", StringComparison.Ordinal));
        var fallbackRegistered = report.Registered.Any(
            binding => string.Equals(binding.GestureText, "Ctrl+Shift+1", StringComparison.Ordinal));

        if (printScreenConflict && fallbackRegistered)
        {
            ShowStatus(
                "Print Screen is already in use",
                "Windows or another application owns Print Screen. Ctrl + Shift + 1 remains available for Region Capture.",
                StatusKind.Warning);
            return;
        }

        var gestures = string.Join(", ", report.Conflicts.Select(conflict => conflict.Binding.GestureText));
        ShowStatus(
            "Some global hotkeys are unavailable",
            $"Already in use: {gestures}. Capture buttons and tray actions remain available.",
            StatusKind.Warning);
    }

    public void ReportHotkeyHostFailure()
        => ShowStatus(
            "Global hotkeys unavailable",
            "SNAPVERE could not start the Windows global-hotkey host. Capture buttons and tray actions remain available.",
            StatusKind.Warning);

    public void ReportTrayHostFailure()
        => ShowStatus(
            "System tray unavailable",
            "SNAPVERE could not create its notification-area icon. Capture buttons and global hotkeys remain available.",
            StatusKind.Warning);

    private void CaptureCenterWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_initialSizeApplied)
        {
            return;
        }

        _initialSizeApplied = true;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1180, 760));
    }

    private Grid BuildWindowContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Brush(0xFF, 0x07, 0x08, 0x0D)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var topBar = BuildTopBar();
        Grid.SetRow(topBar, 0);
        root.Children.Add(topBar);

        var body = new Grid { Margin = new Thickness(16, 0, 16, 16) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(306) });

        var nav = BuildNavigationRail();
        Grid.SetColumn(nav, 0);
        body.Children.Add(nav);

        var center = new Border
        {
            Margin = new Thickness(12, 0, 12, 0),
            Background = Brush(0xB8, 0x0D, 0x0F, 0x17),
            BorderBrush = Brush(0x28, 0xA4, 0x92, 0xFF),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Child = _contentHost
        };
        Grid.SetColumn(center, 1);
        body.Children.Add(center);

        var right = BuildRightPanel();
        Grid.SetColumn(right, 2);
        body.Children.Add(right);

        Grid.SetRow(body, 1);
        root.Children.Add(body);
        return root;
    }

    private UIElement BuildTopBar()
    {
        var bar = new Grid
        {
            Margin = new Thickness(18, 10, 18, 8)
        };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var brand = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        var symbol = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(13),
            Background = AccentGradient(),
            BorderBrush = Brush(0x55, 0xD7, 0xCA, 0xFF),
            BorderThickness = new Thickness(1),
            Child = Text("S", 18, ForegroundStrong, FontWeights.Bold, HorizontalAlignment.Center, VerticalAlignment.Center)
        };
        brand.Children.Add(symbol);

        var identity = new StackPanel { Spacing = 0, VerticalAlignment = VerticalAlignment.Center };
        identity.Children.Add(Text("SNAPVERE", 15, ForegroundStrong, FontWeights.Bold));
        identity.Children.Add(Text("Capture. Edit. Done.", 10, ForegroundSubtle));
        brand.Children.Add(identity);
        bar.Children.Add(brand);

        var privacy = new Border
        {
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(12),
            Background = Brush(0x55, 0x11, 0x24, 0x22),
            BorderBrush = Brush(0x45, 0x45, 0xD6, 0xA2),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = Text("Local-first  •  no upload", 10, Brush(0xFF, 0x83, 0xE6, 0xBE), FontWeights.SemiBold)
        };
        Grid.SetColumn(privacy, 2);
        bar.Children.Add(privacy);
        return bar;
    }

    private UIElement BuildNavigationRail()
    {
        var rail = new Grid
        {
            Background = Brush(0x9A, 0x0D, 0x0F, 0x17)
        };
        rail.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var nav = new StackPanel
        {
            Spacing = 7,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        };
        nav.Children.Add(CreateNavigationButton(ShellPage.Capture, "Capture", "\uE722"));
        nav.Children.Add(CreateNavigationButton(ShellPage.History, "History", "\uE81C"));
        nav.Children.Add(CreateNavigationButton(ShellPage.Editor, "Editor", "\uE70F"));
        nav.Children.Add(CreateNavigationButton(ShellPage.Settings, "Settings", "\uE713"));
        nav.Children.Add(CreateNavigationButton(ShellPage.About, "About", "\uE946"));
        rail.Children.Add(nav);

        var version = typeof(CaptureCenterWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        var versionText = Text($"v{version}", 9, ForegroundSubtle, FontWeights.SemiBold, HorizontalAlignment.Center);
        versionText.Margin = new Thickness(0, 0, 0, 14);
        Grid.SetRow(versionText, 1);
        rail.Children.Add(versionText);

        return new Border
        {
            CornerRadius = new CornerRadius(22),
            BorderBrush = Brush(0x24, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            Child = rail
        };
    }

    private Button CreateNavigationButton(ShellPage page, string label, string glyph)
    {
        var content = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        content.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 18,
            Foreground = ForegroundMuted
        });
        content.Children.Add(Text(label, 9, ForegroundMuted, FontWeights.SemiBold, HorizontalAlignment.Center));

        var button = new Button
        {
            Width = 68,
            Height = 62,
            Padding = new Thickness(4),
            Background = TransparentBrush,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = content,
            Tag = page
        };
        AutomationProperties.SetName(button, label);
        button.Click += NavigationButton_Click;
        _navigationButtons[page] = button;
        return button;
    }

    private UIElement BuildRightPanel()
    {
        var layout = new Grid
        {
            Padding = new Thickness(18),
            Background = Brush(0xA8, 0x0D, 0x0F, 0x17)
        };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var quick = new StackPanel { Spacing = 9 };
        quick.Children.Add(Text("QUICK CAPTURE", 10, Brush(0xFF, 0xAA, 0x9D, 0xFF), FontWeights.Bold));
        quick.Children.Add(CreateCompactAction("Region", "Print Screen", "\uE722", RegionCaptureButton_Click, true));
        quick.Children.Add(CreateCompactAction("Window", "Ctrl + Shift + 2", "\uE7F4", WindowCaptureButton_Click, false));
        quick.Children.Add(CreateCompactAction("Screen", "Ctrl + Shift + 4", "\uE7F8", ScreenCaptureButton_Click, false));
        layout.Children.Add(quick);

        _statusPanel.Margin = new Thickness(0, 18, 0, 16);
        Grid.SetRow(_statusPanel, 1);
        layout.Children.Add(_statusPanel);

        var recent = new Grid();
        recent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        recent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var recentHeader = new Grid();
        recentHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        recentHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var recentTitle = new StackPanel { Spacing = 2 };
        recentTitle.Children.Add(Text("Recent", 14, ForegroundStrong, FontWeights.SemiBold));
        recentTitle.Children.Add(_recentSummary);
        recentHeader.Children.Add(recentTitle);
        var refresh = CreateGhostButton("\uE72C", "Refresh recent captures", RefreshHistoryButton_Click);
        Grid.SetColumn(refresh, 1);
        recentHeader.Children.Add(refresh);
        recent.Children.Add(recentHeader);

        var recentScroller = new ScrollViewer
        {
            Margin = new Thickness(0, 10, 0, 0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _recentItems
        };
        Grid.SetRow(recentScroller, 1);
        recent.Children.Add(recentScroller);
        Grid.SetRow(recent, 2);
        layout.Children.Add(recent);

        var folder = CreateWideSecondaryButton("Open capture folder", "\uE838", OpenCaptureFolderButton_Click);
        folder.Margin = new Thickness(0, 12, 0, 0);
        Grid.SetRow(folder, 3);
        layout.Children.Add(folder);

        return new Border
        {
            CornerRadius = new CornerRadius(22),
            BorderBrush = Brush(0x24, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            Child = layout
        };
    }

    private UIElement BuildCapturePage()
    {
        var page = new Grid { Padding = new Thickness(28) };
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var heading = new StackPanel { Spacing = 5 };
        heading.Children.Add(Text("Capture anything.", 30, ForegroundStrong, FontWeights.Bold));
        var subtitle = Text(
            "Fast screenshots with a focused workflow — choose a target, capture, then edit only when you need to.",
            12,
            ForegroundMuted);
        subtitle.TextWrapping = TextWrapping.Wrap;
        heading.Children.Add(subtitle);
        page.Children.Add(heading);

        var modes = new Grid { Margin = new Thickness(0, 22, 0, 20), ColumnSpacing = 12 };
        modes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        modes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        modes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(_regionCaptureButton, 0);
        modes.Children.Add(_regionCaptureButton);
        Grid.SetColumn(_windowCaptureButton, 1);
        modes.Children.Add(_windowCaptureButton);
        Grid.SetColumn(_screenCaptureButton, 2);
        modes.Children.Add(_screenCaptureButton);
        Grid.SetRow(modes, 1);
        page.Children.Add(modes);

        var preview = BuildPreviewCanvas();
        Grid.SetRow(preview, 2);
        page.Children.Add(preview);
        return page;
    }

    private UIElement BuildPreviewCanvas()
    {
        var grid = new Grid
        {
            Padding = new Thickness(32),
            Background = PreviewGradient()
        };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var center = new StackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var focus = new Border
        {
            Width = 104,
            Height = 72,
            CornerRadius = new CornerRadius(16),
            BorderBrush = Brush(0xE8, 0xA8, 0x8C, 0xFF),
            BorderThickness = new Thickness(2),
            Background = Brush(0x28, 0x7C, 0x5C, 0xFF),
            Child = new FontIcon
            {
                Glyph = "\uE722",
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 26,
                Foreground = Brush(0xFF, 0xEE, 0xE9, 0xFF)
            }
        };
        center.Children.Add(focus);
        center.Children.Add(Text("Ready for your next capture", 19, ForegroundStrong, FontWeights.SemiBold, HorizontalAlignment.Center));
        center.Children.Add(Text("Print Screen starts Region Capture instantly", 11, ForegroundMuted, FontWeights.Normal, HorizontalAlignment.Center));
        grid.Children.Add(center);

        var hints = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        hints.Children.Add(CreateChip("Local only", "\uE72E"));
        hints.Children.Add(CreateChip("Pixel accurate", "\uE799"));
        hints.Children.Add(CreateChip("Clipboard ready", "\uE8C8"));
        Grid.SetRow(hints, 1);
        grid.Children.Add(hints);

        return new Border
        {
            MinHeight = 330,
            CornerRadius = new CornerRadius(22),
            BorderBrush = Brush(0x3D, 0x9B, 0x83, 0xFF),
            BorderThickness = new Thickness(1),
            Child = grid
        };
    }

    private UIElement BuildHistoryPage()
    {
        var page = new Grid { Padding = new Thickness(28) };
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel { Spacing = 5 };
        copy.Children.Add(Text("History", 30, ForegroundStrong, FontWeights.Bold));
        copy.Children.Add(Text("Recent screenshots stored in your local Pictures\\SNAPVERE folder.", 12, ForegroundMuted));
        header.Children.Add(copy);
        var refresh = CreateWideSecondaryButton("Refresh", "\uE72C", RefreshHistoryButton_Click);
        Grid.SetColumn(refresh, 1);
        header.Children.Add(refresh);
        page.Children.Add(header);

        var scroller = new ScrollViewer
        {
            Margin = new Thickness(0, 22, 0, 0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _historyItems
        };
        Grid.SetRow(scroller, 1);
        page.Children.Add(scroller);
        return page;
    }

    private UIElement BuildEditorPage()
    {
        var page = new Grid { Padding = new Thickness(28) };
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var heading = new StackPanel { Spacing = 5 };
        heading.Children.Add(Text("Editor", 30, ForegroundStrong, FontWeights.Bold));
        heading.Children.Add(Text("The editor opens on the frozen capture — no separate project or cloud document.", 12, ForegroundMuted));
        page.Children.Add(heading);

        var card = new Grid
        {
            Margin = new Thickness(0, 22, 0, 0),
            Padding = new Thickness(28),
            Background = PreviewGradient()
        };
        card.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var center = new StackPanel
        {
            Spacing = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        center.Children.Add(new FontIcon
        {
            Glyph = "\uE70F",
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 34,
            Foreground = Brush(0xFF, 0xAE, 0x99, 0xFF)
        });
        center.Children.Add(Text("Capture a region to enter the editor", 18, ForegroundStrong, FontWeights.SemiBold, HorizontalAlignment.Center));
        center.Children.Add(Text("Move • Pen • Line • Arrow • Rectangle • Highlight • Undo • Copy • Save", 11, ForegroundMuted, FontWeights.Normal, HorizontalAlignment.Center));
        var launch = CreateAccentButton("Capture region", "\uE722", RegionCaptureButton_Click);
        launch.HorizontalAlignment = HorizontalAlignment.Center;
        launch.MinWidth = 170;
        center.Children.Add(launch);
        card.Children.Add(center);

        var tools = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 7,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        foreach (var tool in new[] { "Move", "Pen", "Line", "Arrow", "Box", "Highlight", "Undo" })
        {
            tools.Children.Add(CreateTextChip(tool));
        }
        Grid.SetRow(tools, 1);
        card.Children.Add(tools);

        var container = new Border
        {
            CornerRadius = new CornerRadius(22),
            BorderBrush = Brush(0x32, 0x9B, 0x83, 0xFF),
            BorderThickness = new Thickness(1),
            Child = card
        };
        Grid.SetRow(container, 1);
        page.Children.Add(container);
        return page;
    }

    private UIElement BuildSettingsPage()
    {
        _suppressSettingsEvents = true;
        var page = new Grid { Padding = new Thickness(28) };
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var heading = new StackPanel { Spacing = 5 };
        heading.Children.Add(Text("Settings", 30, ForegroundStrong, FontWeights.Bold));
        heading.Children.Add(Text("Small defaults that keep capture fast and predictable.", 12, ForegroundMuted));
        page.Children.Add(heading);

        var settings = new StackPanel { Spacing = 12, Margin = new Thickness(0, 22, 0, 0) };
        var startup = new ToggleSwitch
        {
            Header = "Start SNAPVERE with Windows",
            OffContent = "Off",
            OnContent = "On",
            IsOn = SafeReadStartupEnabled()
        };
        AutomationProperties.SetName(startup, "Start SNAPVERE with Windows");
        startup.Toggled += StartupToggle_Toggled;
        settings.Children.Add(CreateSettingCard(
            "Startup",
            "Keep SNAPVERE resident in the notification area after you sign in. Windows startup uses background mode and does not open this window.",
            startup));

        var cursor = new ToggleSwitch
        {
            Header = "Include cursor on capture",
            OffContent = "Off",
            OnContent = "On",
            IsOn = _capturePreferencesService.Current.IncludeCursorOnCapture
        };
        AutomationProperties.SetName(cursor, "Include cursor on capture");
        cursor.Toggled += CursorToggle_Toggled;
        settings.Children.Add(CreateSettingCard(
            "Capture",
            "Include the pointer where the active Windows capture backend supports it.",
            cursor));

        var privacy = new StackPanel { Spacing = 4 };
        privacy.Children.Add(Text("Privacy", 13, ForegroundStrong, FontWeights.SemiBold));
        privacy.Children.Add(Text("Screenshots, recent-capture history and settings stay on this PC. SNAPVERE does not upload capture pixels for analytics.", 11, ForegroundMuted));
        settings.Children.Add(Card(privacy, 18));

        var scroller = new ScrollViewer { Content = settings };
        Grid.SetRow(scroller, 1);
        page.Children.Add(scroller);
        _suppressSettingsEvents = false;
        return page;
    }

    private UIElement BuildAboutPage()
    {
        var version = typeof(CaptureCenterWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        var page = new Grid { Padding = new Thickness(28) };
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var heading = new StackPanel { Spacing = 5 };
        heading.Children.Add(Text("About SNAPVERE", 30, ForegroundStrong, FontWeights.Bold));
        heading.Children.Add(Text("Premium screen capture for Windows, developed and published by Brendigo.", 12, ForegroundMuted));
        page.Children.Add(heading);

        var identity = new StackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        identity.Children.Add(new Border
        {
            Width = 88,
            Height = 88,
            CornerRadius = new CornerRadius(26),
            Background = AccentGradient(),
            BorderBrush = Brush(0x60, 0xD8, 0xCC, 0xFF),
            BorderThickness = new Thickness(1),
            Child = Text("S", 34, ForegroundStrong, FontWeights.Bold, HorizontalAlignment.Center, VerticalAlignment.Center)
        });
        identity.Children.Add(Text("SNAPVERE", 26, ForegroundStrong, FontWeights.Bold, HorizontalAlignment.Center));
        identity.Children.Add(Text("Capture. Edit. Done.", 13, ForegroundMuted, FontWeights.Normal, HorizontalAlignment.Center));
        identity.Children.Add(Text($"Version {version}  •  Brendigo", 11, ForegroundSubtle, FontWeights.SemiBold, HorizontalAlignment.Center));
        identity.Children.Add(CreateTextChip("Local-first Windows capture"));
        Grid.SetRow(identity, 1);
        page.Children.Add(identity);
        return page;
    }

    private Border BuildStatusPanel()
    {
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(_statusTitle);
        stack.Children.Add(_statusMessage);
        return new Border
        {
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(16),
            Background = Brush(0x72, 0x12, 0x15, 0x20),
            BorderBrush = Brush(0x35, 0x79, 0x6A, 0xD9),
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private static Border CreateSettingCard(string eyebrow, string description, Control control)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var copy = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 24, 0) };
        copy.Children.Add(Text(eyebrow.ToUpperInvariant(), 9, Brush(0xFF, 0xAA, 0x9D, 0xFF), FontWeights.Bold));
        var descriptionText = Text(description, 11, ForegroundMuted);
        descriptionText.TextWrapping = TextWrapping.Wrap;
        copy.Children.Add(descriptionText);
        grid.Children.Add(copy);

        control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return Card(grid, 18);
    }

    private Button CreateCompactAction(string title, string shortcut, string glyph, RoutedEventHandler handler, bool accent)
    {
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new Border
        {
            Width = 31,
            Height = 31,
            CornerRadius = new CornerRadius(10),
            Background = accent ? Brush(0x7B, 0x73, 0x55, 0xE9) : Brush(0x55, 0x21, 0x25, 0x34),
            Child = new FontIcon
            {
                Glyph = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 14,
                Foreground = ForegroundStrong
            }
        };
        content.Children.Add(icon);

        var copy = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(Text(title, 11, ForegroundStrong, FontWeights.SemiBold));
        copy.Children.Add(Text(shortcut, 9, ForegroundSubtle));
        Grid.SetColumn(copy, 1);
        content.Children.Add(copy);

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(10),
            Background = accent ? Brush(0x42, 0x5F, 0x49, 0xC8) : Brush(0x45, 0x16, 0x19, 0x23),
            BorderBrush = accent ? Brush(0x55, 0xA6, 0x8E, 0xFF) : Brush(0x24, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            Content = content
        };
        button.Click += handler;
        return button;
    }

    private static Button CreateGhostButton(string glyph, string accessibleName, RoutedEventHandler handler)
    {
        var button = new Button
        {
            Width = 34,
            Height = 34,
            Padding = new Thickness(0),
            Background = Brush(0x45, 0x18, 0x1B, 0x26),
            BorderBrush = Brush(0x24, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            Content = new FontIcon
            {
                Glyph = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 13,
                Foreground = ForegroundMuted
            }
        };
        AutomationProperties.SetName(button, accessibleName);
        button.Click += handler;
        return button;
    }

    private static Button CreateWideSecondaryButton(string text, string glyph, RoutedEventHandler handler)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        content.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 13,
            Foreground = ForegroundStrong
        });
        content.Children.Add(Text(text, 11, ForegroundStrong, FontWeights.SemiBold));

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(13, 9, 13, 9),
            Background = Brush(0x55, 0x18, 0x1B, 0x26),
            BorderBrush = Brush(0x28, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            Content = content
        };
        button.Click += handler;
        return button;
    }

    private static Button CreateAccentButton(string title, string glyph, RoutedEventHandler handler)
        => CreateModeCard(title, "Select a precise area", "Print Screen", glyph, handler, true);

    private static Button CreateModeButton(string title, string glyph, RoutedEventHandler handler)
    {
        var description = title.Contains("window", StringComparison.OrdinalIgnoreCase)
            ? "Pick a visible window"
            : "Capture the primary display";
        var shortcut = title.Contains("window", StringComparison.OrdinalIgnoreCase)
            ? "Ctrl + Shift + 2"
            : "Ctrl + Shift + 4";
        return CreateModeCard(title, description, shortcut, glyph, handler, false);
    }

    private static Button CreateModeCard(
        string title,
        string description,
        string shortcut,
        string glyph,
        RoutedEventHandler handler,
        bool accent)
    {
        var content = new Grid { MinHeight = 88 };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var top = new Grid();
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        top.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 20,
            Foreground = accent ? Brush(0xFF, 0xD7, 0xCD, 0xFF) : ForegroundMuted
        });
        var shortcutText = Text(shortcut, 8, accent ? Brush(0xFF, 0xD8, 0xD0, 0xFF) : ForegroundSubtle, FontWeights.SemiBold);
        Grid.SetColumn(shortcutText, 1);
        top.Children.Add(shortcutText);
        content.Children.Add(top);

        var titleText = Text(title, 13, ForegroundStrong, FontWeights.SemiBold);
        titleText.Margin = new Thickness(0, 12, 0, 0);
        Grid.SetRow(titleText, 1);
        content.Children.Add(titleText);

        var descriptionText = Text(description, 10, ForegroundMuted);
        descriptionText.Margin = new Thickness(0, 3, 0, 0);
        Grid.SetRow(descriptionText, 2);
        content.Children.Add(descriptionText);

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(15),
            Background = accent ? Brush(0x92, 0x55, 0x3A, 0xA8) : Brush(0x66, 0x15, 0x18, 0x23),
            BorderBrush = accent ? Brush(0xA0, 0xA8, 0x8B, 0xFF) : Brush(0x28, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            Content = content
        };
        AutomationProperties.SetName(button, title);
        button.Click += handler;
        return button;
    }

    private static Border CreateChip(string text, string glyph)
    {
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        stack.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 11,
            Foreground = Brush(0xFF, 0xBC, 0xAD, 0xFF)
        });
        stack.Children.Add(Text(text, 9, ForegroundMuted, FontWeights.SemiBold));
        return new Border
        {
            Padding = new Thickness(9, 6, 9, 6),
            CornerRadius = new CornerRadius(11),
            Background = Brush(0x58, 0x14, 0x16, 0x20),
            BorderBrush = Brush(0x2C, 0xA8, 0x8D, 0xFF),
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private static Border CreateTextChip(string text)
        => new()
        {
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0x55, 0x18, 0x1B, 0x27),
            BorderBrush = Brush(0x24, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            Child = Text(text, 9, ForegroundMuted, FontWeights.SemiBold)
        };

    private static Border Card(UIElement child, double padding)
        => new()
        {
            Padding = new Thickness(padding),
            CornerRadius = new CornerRadius(17),
            Background = Brush(0x72, 0x13, 0x16, 0x20),
            BorderBrush = Brush(0x25, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            Child = child
        };

    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ShellPage page })
        {
            NavigateTo(page);
        }
    }

    private void NavigateTo(ShellPage page)
    {
        _activePage = page;
        _contentHost.Children.Clear();
        _contentHost.Children.Add(page switch
        {
            ShellPage.History => BuildHistoryPage(),
            ShellPage.Editor => BuildEditorPage(),
            ShellPage.Settings => BuildSettingsPage(),
            ShellPage.About => BuildAboutPage(),
            _ => BuildCapturePage()
        });

        foreach (var pair in _navigationButtons)
        {
            var selected = pair.Key == _activePage;
            pair.Value.Background = selected ? Brush(0x66, 0x62, 0x4B, 0xC5) : TransparentBrush;
            pair.Value.BorderBrush = selected ? Brush(0x70, 0xA9, 0x91, 0xFF) : TransparentBrush;
            pair.Value.BorderThickness = selected ? new Thickness(1) : new Thickness(0);

            if (pair.Value.Content is StackPanel stack)
            {
                foreach (var child in stack.Children)
                {
                    if (child is FontIcon icon)
                    {
                        icon.Foreground = selected ? ForegroundStrong : ForegroundMuted;
                    }
                    else if (child is TextBlock label)
                    {
                        label.Foreground = selected ? ForegroundStrong : ForegroundMuted;
                    }
                }
            }
        }

        if (page == ShellPage.History)
        {
            RefreshRecentCaptures();
        }
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingsEvents || sender is not ToggleSwitch toggle)
        {
            return;
        }

        try
        {
            _startupRegistrationService.SetEnabled(toggle.IsOn);
            ShowStatus(
                toggle.IsOn ? "Windows startup enabled" : "Windows startup disabled",
                toggle.IsOn
                    ? "SNAPVERE will start quietly in the notification area after sign-in."
                    : "SNAPVERE will no longer launch automatically after sign-in.",
                StatusKind.Success);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or InvalidOperationException)
        {
            _suppressSettingsEvents = true;
            toggle.IsOn = SafeReadStartupEnabled();
            _suppressSettingsEvents = false;
            ShowStatus("Could not change Windows startup", "Windows did not allow SNAPVERE to update the current-user startup setting.", StatusKind.Error);
        }
    }

    private void CursorToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingsEvents || sender is not ToggleSwitch toggle)
        {
            return;
        }

        try
        {
            _capturePreferencesService.SetIncludeCursorOnCapture(toggle.IsOn);
            ShowStatus(
                toggle.IsOn ? "Cursor capture enabled" : "Cursor capture disabled",
                toggle.IsOn ? "Supported capture modes will include the pointer." : "New captures will omit the pointer where supported.",
                StatusKind.Success);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            _suppressSettingsEvents = true;
            toggle.IsOn = _capturePreferencesService.Current.IncludeCursorOnCapture;
            _suppressSettingsEvents = false;
            ShowStatus("Could not save capture preference", "SNAPVERE could not update the local settings file.", StatusKind.Error);
        }
    }

    private bool SafeReadStartupEnabled()
    {
        try
        {
            return _startupRegistrationService.IsEnabled();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    private async void RegionCaptureButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteRegionCaptureAsync();

    private async void WindowCaptureButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteWindowCaptureAsync();

    private async void ScreenCaptureButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteScreenCaptureAsync();

    private void RefreshHistoryButton_Click(object sender, RoutedEventArgs e)
        => RefreshRecentCaptures();

    private void OpenCaptureFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var directory = _captureHistoryService.GetCaptureDirectory();
            Directory.CreateDirectory(directory);
            _ = Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            ShowStatus("Capture folder unavailable", "Windows could not open the local SNAPVERE capture folder.", StatusKind.Error);
        }
    }

    private async Task ExecuteRegionCaptureAsync()
    {
        if (!TryBeginCapture())
        {
            return;
        }

        ShowStatus("Preparing Region Capture", "Freezing the display before the selection overlay opens.", StatusKind.Information);
        var restoreMainWindow = AppWindow.IsVisible;
        try
        {
            if (restoreMainWindow)
            {
                AppWindow.Hide();
                await Task.Delay(120);
            }

            var session = await _regionCaptureWorkflow.PreparePrimaryDisplayAsync(includeCursor: false);
            var overlay = new RegionCaptureWindow(_regionCaptureWorkflow, _pngEncoder, session);
            _regionCaptureWindow = overlay;

            var outcome = await overlay.ShowAsync();
            if (outcome.CopiedToClipboard)
            {
                ShowStatus("Region copied", "The selected image and annotations are on the Windows clipboard.", StatusKind.Success);
            }
            else if (outcome.IsCancelled || outcome.SaveResult is null)
            {
                ShowStatus("Region capture cancelled", "No file was created.", StatusKind.Information);
            }
            else
            {
                ShowStatus("Region captured", $"Saved {outcome.SaveResult.Width}×{outcome.SaveResult.Height} PNG locally.", StatusKind.Success);
                RefreshRecentCaptures();
            }
        }
        catch (Exception exception)
        {
            ShowStatus("SNAPVERE couldn't capture the region", GetUserFacingCaptureError(exception), StatusKind.Error);
        }
        finally
        {
            _regionCaptureWindow = null;
            RestoreMainWindowIfNeeded(restoreMainWindow);
            EndCapture();
        }
    }

    private async Task ExecuteWindowCaptureAsync()
    {
        if (!WindowsGraphicsCaptureService.IsSupported())
        {
            ShowStatus(
                "Window Capture is unavailable",
                "Window Capture requires Windows.Graphics.Capture support on Windows 10 version 2004 / build 19041 or later.",
                StatusKind.Warning);
            return;
        }

        if (!TryBeginCapture())
        {
            return;
        }

        ShowStatus("Preparing Window Capture", "Freezing the desktop and discovering visible windows.", StatusKind.Information);
        var restoreMainWindow = AppWindow.IsVisible;
        try
        {
            if (restoreMainWindow)
            {
                AppWindow.Hide();
                await Task.Delay(120);
            }

            var target = await _windowTargetPicker.PickAsync();
            if (target is null)
            {
                ShowStatus("Window capture cancelled", "No file was created.", StatusKind.Information);
                return;
            }

            var result = await _windowCaptureWorkflow.CaptureWindowToDefaultFolderAsync(target, includeCursor: false);
            ShowStatus("Window captured", $"Saved {result.Width}×{result.Height} PNG from “{target.Title}” locally.", StatusKind.Success);
            RefreshRecentCaptures();
        }
        catch (Exception exception)
        {
            ShowStatus("SNAPVERE couldn't capture the window", GetUserFacingCaptureError(exception), StatusKind.Error);
        }
        finally
        {
            RestoreMainWindowIfNeeded(restoreMainWindow);
            EndCapture();
        }
    }

    private async Task ExecuteScreenCaptureAsync()
    {
        if (!TryBeginCapture())
        {
            return;
        }

        ShowStatus("Capturing screen", "SNAPVERE is capturing the primary display.", StatusKind.Information);
        var restoreMainWindow = AppWindow.IsVisible;
        try
        {
            if (restoreMainWindow)
            {
                AppWindow.Hide();
                await Task.Delay(120);
            }

            var result = await _screenCaptureWorkflow.CapturePrimaryDisplayToDefaultFolderAsync(includeCursor: false);
            ShowStatus("Screen captured", $"Saved {result.Width}×{result.Height} PNG locally.", StatusKind.Success);
            RefreshRecentCaptures();
        }
        catch (Exception exception)
        {
            ShowStatus("SNAPVERE couldn't capture the screen", GetUserFacingCaptureError(exception), StatusKind.Error);
        }
        finally
        {
            RestoreMainWindowIfNeeded(restoreMainWindow);
            EndCapture();
        }
    }

    private bool TryBeginCapture()
    {
        if (_captureInProgress)
        {
            return false;
        }

        _captureInProgress = true;
        SetCaptureButtonsEnabled(false);
        return true;
    }

    private void EndCapture()
    {
        _captureInProgress = false;
        SetCaptureButtonsEnabled(true);
    }

    private void RestoreMainWindowIfNeeded(bool restore)
    {
        if (!restore)
        {
            return;
        }

        AppWindow.Show();
        Activate();
    }

    private void RefreshRecentCaptures()
    {
        _recentItems.Children.Clear();
        _historyItems.Children.Clear();

        try
        {
            var captures = _captureHistoryService.GetRecentCaptures(limit: RecentCaptureLimit);
            _recentSummary.Text = captures.Count switch
            {
                0 => "Pictures\\SNAPVERE",
                1 => "1 local capture",
                _ => $"{captures.Count} local captures"
            };

            if (captures.Count == 0)
            {
                var empty = BuildEmptyHistory();
                _recentItems.Children.Add(empty);
                _historyItems.Children.Add(BuildEmptyHistory());
                return;
            }

            foreach (var item in captures)
            {
                _recentItems.Children.Add(CreateRecentCaptureRow(item, compact: true));
                _historyItems.Children.Add(CreateRecentCaptureRow(item, compact: false));
            }
        }
        catch (UnauthorizedAccessException)
        {
            ShowRecentUnavailable("Windows denied access to the SNAPVERE capture folder.");
        }
        catch (IOException)
        {
            ShowRecentUnavailable("SNAPVERE could not read the local capture folder.");
        }
    }

    private Border BuildEmptyHistory()
    {
        var copy = new StackPanel { Spacing = 4 };
        copy.Children.Add(Text("No captures yet", 11, ForegroundStrong, FontWeights.SemiBold));
        var detail = Text("Press Print Screen or choose Region Capture to create your first screenshot.", 10, ForegroundSubtle);
        detail.TextWrapping = TextWrapping.Wrap;
        copy.Children.Add(detail);
        return Card(copy, 13);
    }

    private Button CreateRecentCaptureRow(CaptureHistoryItem item, bool compact)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var thumbnail = new Border
        {
            Width = compact ? 42 : 58,
            Height = compact ? 34 : 46,
            CornerRadius = new CornerRadius(9),
            Background = PreviewGradient(),
            BorderBrush = Brush(0x32, 0x9E, 0x88, 0xFF),
            BorderThickness = new Thickness(1),
            Child = new FontIcon
            {
                Glyph = "\uE91B",
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = compact ? 12 : 16,
                Foreground = Brush(0xFF, 0xB7, 0xA7, 0xFF)
            }
        };
        grid.Children.Add(thumbnail);

        var copy = new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        copy.Children.Add(Text(item.FileName, compact ? 10 : 12, ForegroundStrong, FontWeights.SemiBold));
        copy.Children.Add(Text(item.MetadataText, compact ? 9 : 10, ForegroundSubtle));
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(compact ? 9 : 12),
            Background = Brush(0x48, 0x15, 0x18, 0x22),
            BorderBrush = Brush(0x20, 0xFF, 0xFF, 0xFF),
            BorderThickness = new Thickness(1),
            Content = grid
        };
        AutomationProperties.SetName(button, $"Open {item.FileName}");
        button.Click += (_, _) => OpenCapture(item);
        return button;
    }

    private void OpenCapture(CaptureHistoryItem item)
    {
        try
        {
            if (!File.Exists(item.FilePath))
            {
                RefreshRecentCaptures();
                ShowStatus("Capture moved", "That screenshot is no longer available at its original path.", StatusKind.Warning);
                return;
            }

            _ = Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            ShowStatus("Could not open capture", "Windows could not open that screenshot with the default image application.", StatusKind.Error);
        }
    }

    private void ShowRecentUnavailable(string message)
    {
        _recentSummary.Text = "History unavailable";
        _recentItems.Children.Clear();
        _historyItems.Children.Clear();
        var recent = Text(message, 10, Brush(0xFF, 0xE0, 0xB0, 0x79));
        recent.TextWrapping = TextWrapping.Wrap;
        _recentItems.Children.Add(Card(recent, 12));
        var history = Text(message, 11, Brush(0xFF, 0xE0, 0xB0, 0x79));
        history.TextWrapping = TextWrapping.Wrap;
        _historyItems.Children.Add(Card(history, 14));
    }

    private void SetCaptureButtonsEnabled(bool isEnabled)
    {
        _regionCaptureButton.IsEnabled = isEnabled;
        _windowCaptureButton.IsEnabled = isEnabled;
        _screenCaptureButton.IsEnabled = isEnabled;
    }

    private void ShowStatus(string title, string message, StatusKind kind)
    {
        _statusTitle.Text = title;
        _statusMessage.Text = message;
        _statusPanel.BorderBrush = kind switch
        {
            StatusKind.Success => Brush(0x72, 0x45, 0xD6, 0xA2),
            StatusKind.Warning => Brush(0x72, 0xFF, 0xC8, 0x57),
            StatusKind.Error => Brush(0x80, 0xFF, 0x67, 0x7D),
            _ => Brush(0x70, 0x8D, 0x79, 0xFF)
        };
    }

    private static string GetUserFacingCaptureError(Exception exception)
        => exception switch
        {
            UnauthorizedAccessException => "Windows denied access to the selected save location.",
            IOException => "The screenshot was captured, but SNAPVERE could not save the PNG file.",
            TimeoutException => "Windows did not provide the requested capture frame in time. Try the capture again.",
            PlatformNotSupportedException => exception.Message,
            InvalidOperationException => exception.Message,
            _ => "The display or window configuration may have changed, or Windows may have blocked this capture. Try again."
        };

    private static TextBlock Text(
        string value,
        double size,
        SolidColorBrush color,
        FontWeight? weight = null,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment verticalAlignment = VerticalAlignment.Stretch)
        => new()
        {
            Text = value,
            FontSize = size,
            Foreground = color,
            FontWeight = weight ?? FontWeights.Normal,
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = verticalAlignment
        };

    private static LinearGradientBrush AccentGradient()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1)
        };
        brush.GradientStops.Add(new GradientStop { Color = Color(0xFF, 0x69, 0x4C, 0xFF), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = Color(0xFF, 0x9A, 0x52, 0xF2), Offset = 0.56 });
        brush.GradientStops.Add(new GradientStop { Color = Color(0xFF, 0x42, 0xC8, 0xE8), Offset = 1 });
        return brush;
    }

    private static LinearGradientBrush PreviewGradient()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1)
        };
        brush.GradientStops.Add(new GradientStop { Color = Color(0xFF, 0x12, 0x10, 0x21), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = Color(0xFF, 0x0C, 0x10, 0x1A), Offset = 0.52 });
        brush.GradientStops.Add(new GradientStop { Color = Color(0xFF, 0x0D, 0x18, 0x1F), Offset = 1 });
        return brush;
    }

    private static Windows.UI.Color Color(byte alpha, byte red, byte green, byte blue)
        => Windows.UI.Color.FromArgb(alpha, red, green, blue);

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Color(alpha, red, green, blue));

    private static SolidColorBrush ForegroundStrong => Brush(0xFF, 0xF7, 0xF5, 0xFF);
    private static SolidColorBrush ForegroundMuted => Brush(0xFF, 0xB7, 0xB3, 0xC7);
    private static SolidColorBrush ForegroundSubtle => Brush(0xFF, 0x7F, 0x7D, 0x90);
    private static SolidColorBrush TransparentBrush => Brush(0x00, 0x00, 0x00, 0x00);

    private enum ShellPage
    {
        Capture,
        History,
        Editor,
        Settings,
        About
    }

    private enum StatusKind
    {
        Information,
        Success,
        Warning,
        Error
    }
}
