using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snapvere.Application.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Domain.Capture;
using System.Diagnostics;
using Windows.Graphics;

namespace Snapvere.App;

public sealed partial class MainWindow : Window
{
    private readonly ScreenCaptureWorkflow _screenCaptureWorkflow;
    private readonly RegionCaptureWorkflow _regionCaptureWorkflow;
    private readonly CaptureHistoryService _captureHistoryService;
    private RegionCaptureWindow? _regionCaptureWindow;
    private bool _captureInProgress;

    private Grid TitleBarDragRegion = null!;
    private TextBlock VersionText = null!;
    private InfoBar CaptureStatus = null!;
    private Button RegionCaptureButton = null!;
    private Button ScreenCaptureButton = null!;
    private ListView RecentCapturesList = null!;
    private TextBlock RecentCapturesCountText = null!;
    private Border HistoryEmptyState = null!;
    private TextBlock HistoryEmptyTitle = null!;
    private TextBlock HistoryEmptyMessage = null!;

    public MainWindow(
        ScreenCaptureWorkflow screenCaptureWorkflow,
        RegionCaptureWorkflow regionCaptureWorkflow,
        CaptureHistoryService captureHistoryService)
    {
        _screenCaptureWorkflow = screenCaptureWorkflow ?? throw new ArgumentNullException(nameof(screenCaptureWorkflow));
        _regionCaptureWorkflow = regionCaptureWorkflow ?? throw new ArgumentNullException(nameof(regionCaptureWorkflow));
        _captureHistoryService = captureHistoryService ?? throw new ArgumentNullException(nameof(captureHistoryService));

        InitializeComponent();
        BuildCaptureCenter();
        ConfigureWindowChrome();
        VersionText.Text = $"v{typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.2"}";
        RefreshRecentCaptures();
    }

    public void StartCaptureFromHotkey(CaptureMode mode)
    {
        switch (mode)
        {
            case CaptureMode.Region:
                _ = ExecuteRegionCaptureAsync();
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
            return;
        }

        var gestures = string.Join(
            ", ",
            report.Conflicts.Select(conflict => conflict.Binding.GestureText));

        ShowStatus(
            InfoBarSeverity.Warning,
            "Some global hotkeys are unavailable",
            $"Another application is already using: {gestures}. Capture buttons remain available.");
    }

    public void ReportHotkeyHostFailure()
        => ShowStatus(
            InfoBarSeverity.Warning,
            "Global hotkeys unavailable",
            "SNAPVERE could not start the Windows global-hotkey host. Capture buttons remain available.");

    public void ReportTrayHostFailure()
        => ShowStatus(
            InfoBarSeverity.Warning,
            "System tray unavailable",
            "SNAPVERE could not create its Windows notification-area icon. Capture buttons and global hotkeys remain available.");

    private void BuildCaptureCenter()
    {
        RootContent.RequestedTheme = ElementTheme.Dark;
        RootContent.Background = Brush(0x0B, 0x0D, 0x12);
        RootContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        RootContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        BuildTitleBar();
        BuildWorkspace();
    }

    private void BuildTitleBar()
    {
        TitleBarDragRegion = new Grid
        {
            Padding = new Thickness(18, 0, 148, 0),
            Background = Brush(0x0B, 0x0D, 0x12)
        };
        TitleBarDragRegion.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        TitleBarDragRegion.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        TitleBarDragRegion.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var logo = new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(8),
            Background = Brush(0x7C, 0x6C, 0xFF),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "S",
                Foreground = Brush(0xFF, 0xFF, 0xFF),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var brand = new TextBlock
        {
            Text = "SNAPVERE",
            Foreground = Brush(0xFF, 0xFF, 0xFF),
            FontSize = 13,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(brand, 1);

        VersionText = new TextBlock
        {
            Text = "v0.0.2",
            Foreground = Brush(0xD8, 0xDC, 0xE5),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };
        var versionBadge = new Border
        {
            Padding = new Thickness(10, 4, 10, 4),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0x20, 0x26, 0x33),
            VerticalAlignment = VerticalAlignment.Center,
            Child = VersionText
        };
        Grid.SetColumn(versionBadge, 2);

        TitleBarDragRegion.Children.Add(logo);
        TitleBarDragRegion.Children.Add(brand);
        TitleBarDragRegion.Children.Add(versionBadge);

        Grid.SetRow(TitleBarDragRegion, 0);
        RootContent.Children.Add(TitleBarDragRegion);
    }

    private void BuildWorkspace()
    {
        var workspace = new Grid();
        workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(workspace, 1);

        workspace.Children.Add(BuildSidebar());

        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = BuildMainContent()
        };
        Grid.SetColumn(scroller, 1);
        workspace.Children.Add(scroller);

        RootContent.Children.Add(workspace);
    }

    private Border BuildSidebar()
    {
        var sidebarGrid = new Grid();
        sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var navigation = new StackPanel { Spacing = 10 };
        navigation.Children.Add(new TextBlock
        {
            Text = "CAPTURE",
            Foreground = Brush(0x98, 0xA2, 0xB3),
            FontSize = 10,
            Margin = new Thickness(8, 0, 0, 4)
        });
        navigation.Children.Add(new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0x26, 0x21, 0x42),
            Child = new TextBlock
            {
                Text = "Capture center",
                Foreground = Brush(0xFF, 0xFF, 0xFF)
            }
        });
        navigation.Children.Add(new TextBlock
        {
            Text = "Region  ·  Ctrl + Shift + 1",
            Foreground = Brush(0x98, 0xA2, 0xB3),
            FontSize = 12,
            Margin = new Thickness(10, 4, 0, 0)
        });
        navigation.Children.Add(new TextBlock
        {
            Text = "Screen  ·  Ctrl + Shift + 4",
            Foreground = Brush(0x98, 0xA2, 0xB3),
            FontSize = 12,
            Margin = new Thickness(10, 0, 0, 0)
        });

        var localFirst = new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(12),
            Background = Brush(0x18, 0x1C, 0x25),
            BorderBrush = Brush(0x2A, 0x31, 0x40),
            BorderThickness = new Thickness(1)
        };
        var localStack = new StackPanel { Spacing = 5 };
        localStack.Children.Add(new TextBlock
        {
            Text = "Local-first",
            Foreground = Brush(0xFF, 0xFF, 0xFF),
            FontSize = 12
        });
        localStack.Children.Add(new TextBlock
        {
            Text = "No account, telemetry, or cloud upload is required for capture.",
            Foreground = Brush(0x98, 0xA2, 0xB3),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        });
        localFirst.Child = localStack;
        Grid.SetRow(localFirst, 2);

        sidebarGrid.Children.Add(navigation);
        sidebarGrid.Children.Add(localFirst);

        return new Border
        {
            Background = Brush(0x12, 0x15, 0x1C),
            BorderBrush = Brush(0x2A, 0x31, 0x40),
            BorderThickness = new Thickness(0, 1, 1, 0),
            Padding = new Thickness(16, 22, 16, 18),
            Child = sidebarGrid
        };
    }

    private Grid BuildMainContent()
    {
        var content = new Grid
        {
            Padding = new Thickness(34, 28, 34, 34),
            MaxWidth = 1080,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        content.Children.Add(BuildHeader());

        CaptureStatus = new InfoBar
        {
            Margin = new Thickness(0, 20, 0, 0),
            IsOpen = false,
            IsClosable = true
        };
        Grid.SetRow(CaptureStatus, 1);
        content.Children.Add(CaptureStatus);

        var captureGrid = new Grid
        {
            Margin = new Thickness(0, 24, 0, 0),
            ColumnSpacing = 14
        };
        captureGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        captureGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        captureGrid.Children.Add(BuildRegionCard());

        var rightCards = new StackPanel { Spacing = 14 };
        rightCards.Children.Add(BuildScreenCard());
        rightCards.Children.Add(BuildPrivacyCard());
        Grid.SetColumn(rightCards, 1);
        captureGrid.Children.Add(rightCards);
        Grid.SetRow(captureGrid, 2);
        content.Children.Add(captureGrid);

        var history = BuildHistorySection();
        Grid.SetRow(history, 3);
        content.Children.Add(history);

        return content;
    }

    private Grid BuildHeader()
    {
        var header = new Grid();
        var text = new StackPanel { Spacing = 6 };
        text.Children.Add(new TextBlock
        {
            Text = "Capture center",
            Foreground = Brush(0xFF, 0xFF, 0xFF),
            FontSize = 32
        });
        text.Children.Add(new TextBlock
        {
            Text = "Fast capture, precise selection, local PNG output.",
            Foreground = Brush(0x98, 0xA2, 0xB3),
            FontSize = 15
        });

        var openFolder = new Button
        {
            Content = "Open folder",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        openFolder.Click += OpenCaptureFolderButton_Click;

        header.Children.Add(text);
        header.Children.Add(openFolder);
        return header;
    }

    private Border BuildRegionCard()
    {
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var top = new Grid();
        top.Children.Add(new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(12),
            Background = Brush(0x46, 0x3F, 0x78),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = "R",
                Foreground = Brush(0xFF, 0xFF, 0xFF),
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        top.Children.Add(new Border
        {
            Padding = new Thickness(9, 5, 9, 5),
            CornerRadius = new CornerRadius(8),
            Background = Brush(0x42, 0x3B, 0x69),
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new TextBlock
            {
                Text = "Ctrl + Shift + 1",
                Foreground = Brush(0xFF, 0xFF, 0xFF),
                FontSize = 11
            }
        });

        var copy = new StackPanel
        {
            Margin = new Thickness(0, 22, 0, 20),
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        copy.Children.Add(new TextBlock
        {
            Text = "Capture a precise region",
            Foreground = Brush(0xFF, 0xFF, 0xFF),
            FontSize = 24
        });
        copy.Children.Add(new TextBlock
        {
            Text = "Freeze the display, drag an exact area, fine-tune the selection, then save it locally as PNG.",
            Foreground = Brush(0xD8, 0xD6, 0xF2),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 560
        });
        Grid.SetRow(copy, 1);

        RegionCaptureButton = new Button
        {
            Content = "Select region",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(18, 10, 18, 10),
            Background = Brush(0x7C, 0x6C, 0xFF),
            Foreground = Brush(0xFF, 0xFF, 0xFF)
        };
        RegionCaptureButton.Click += RegionCaptureButton_Click;
        Grid.SetRow(RegionCaptureButton, 2);

        layout.Children.Add(top);
        layout.Children.Add(copy);
        layout.Children.Add(RegionCaptureButton);

        return new Border
        {
            MinHeight = 260,
            Padding = new Thickness(26),
            CornerRadius = new CornerRadius(18),
            Background = Brush(0x30, 0x2A, 0x63),
            BorderBrush = Brush(0x57, 0x49, 0xC9),
            BorderThickness = new Thickness(1),
            Child = layout
        };
    }

    private Border BuildScreenCard()
    {
        var stack = new StackPanel { Spacing = 14 };
        var top = new Grid();
        top.Children.Add(new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(11),
            Background = Brush(0x26, 0x21, 0x42),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = "S",
                Foreground = Brush(0xA8, 0x9E, 0xFF),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        top.Children.Add(new Border
        {
            Padding = new Thickness(8, 4, 8, 4),
            CornerRadius = new CornerRadius(7),
            Background = Brush(0x20, 0x26, 0x33),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "Ctrl + Shift + 4",
                Foreground = Brush(0x98, 0xA2, 0xB3),
                FontSize = 10
            }
        });
        stack.Children.Add(top);

        var copy = new StackPanel { Spacing = 5 };
        copy.Children.Add(new TextBlock
        {
            Text = "Full screen",
            Foreground = Brush(0xFF, 0xFF, 0xFF),
            FontSize = 19
        });
        copy.Children.Add(new TextBlock
        {
            Text = "Capture the primary display directly to Pictures\\SNAPVERE.",
            Foreground = Brush(0x98, 0xA2, 0xB3),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(copy);

        ScreenCaptureButton = new Button
        {
            Content = "Capture screen",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12, 9, 12, 9)
        };
        ScreenCaptureButton.Click += ScreenCaptureButton_Click;
        stack.Children.Add(ScreenCaptureButton);

        return Card(stack, 22);
    }

    private Border BuildPrivacyCard()
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(new TextBlock
        {
            Text = "Private by default",
            Foreground = Brush(0xFF, 0xFF, 0xFF),
            FontSize = 13
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Captures stay on this PC. SNAPVERE does not require an account or telemetry connection.",
            Foreground = Brush(0x98, 0xA2, 0xB3),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        });
        return Card(stack, 18);
    }

    private StackPanel BuildHistorySection()
    {
        var section = new StackPanel
        {
            Margin = new Thickness(0, 30, 0, 0),
            Spacing = 12
        };

        var header = new Grid();
        var titleStack = new StackPanel { Spacing = 3 };
        titleStack.Children.Add(new TextBlock
        {
            Text = "Recent captures",
            Foreground = Brush(0xFF, 0xFF, 0xFF),
            FontSize = 21
        });
        RecentCapturesCountText = new TextBlock
        {
            Text = "Local screenshots from Pictures\\SNAPVERE",
            Foreground = Brush(0x98, 0xA2, 0xB3),
            FontSize = 12
        };
        titleStack.Children.Add(RecentCapturesCountText);

        var refresh = new Button
        {
            Content = "Refresh",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        refresh.Click += RefreshHistoryButton_Click;

        header.Children.Add(titleStack);
        header.Children.Add(refresh);
        section.Children.Add(header);

        RecentCapturesList = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = true,
            MaxHeight = 330,
            Visibility = Visibility.Collapsed
        };
        RecentCapturesList.ItemClick += RecentCapturesList_ItemClick;
        section.Children.Add(RecentCapturesList);

        HistoryEmptyTitle = new TextBlock
        {
            Text = "No local captures yet",
            Foreground = Brush(0xFF, 0xFF, 0xFF),
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        HistoryEmptyMessage = new TextBlock
        {
            Text = "Your completed Region and Screen captures will appear here.",
            Foreground = Brush(0x98, 0xA2, 0xB3),
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            MaxWidth = 460
        };
        var emptyStack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 7
        };
        emptyStack.Children.Add(HistoryEmptyTitle);
        emptyStack.Children.Add(HistoryEmptyMessage);
        HistoryEmptyState = new Border
        {
            Padding = new Thickness(24),
            CornerRadius = new CornerRadius(16),
            Background = Brush(0x18, 0x1C, 0x25),
            BorderBrush = Brush(0x2A, 0x31, 0x40),
            BorderThickness = new Thickness(1),
            Child = emptyStack
        };
        section.Children.Add(HistoryEmptyState);

        return section;
    }

    private static Border Card(UIElement child, double padding)
        => new()
        {
            Padding = new Thickness(padding),
            CornerRadius = new CornerRadius(18),
            Background = Brush(0x18, 0x1C, 0x25),
            BorderBrush = Brush(0x2A, 0x31, 0x40),
            BorderThickness = new Thickness(1),
            Child = child
        };

    private static SolidColorBrush Brush(byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(0xFF, red, green, blue));

    private void ConfigureWindowChrome()
    {
        Title = "SNAPVERE — Capture Center";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarDragRegion);

        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch
        {
            SystemBackdrop = null;
        }

        try
        {
            AppWindow.Resize(new SizeInt32(1180, 780));
        }
        catch
        {
            // Windows retains its default size when resize is unavailable.
        }
    }

    private async void RegionCaptureButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteRegionCaptureAsync();

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
            _ = Process.Start(new ProcessStartInfo(directory)
            {
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            ShowStatus(
                InfoBarSeverity.Error,
                "Capture folder unavailable",
                "Windows could not open the local SNAPVERE capture folder.");
        }
    }

    private void RecentCapturesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not CaptureHistoryItem item)
        {
            return;
        }

        try
        {
            if (!File.Exists(item.FilePath))
            {
                RefreshRecentCaptures();
                ShowStatus(InfoBarSeverity.Warning, "Capture moved", "That screenshot is no longer available at its original path.");
                return;
            }

            _ = Process.Start(new ProcessStartInfo(item.FilePath)
            {
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            ShowStatus(InfoBarSeverity.Error, "Could not open capture", "Windows could not open that screenshot with the default image application.");
        }
    }

    private async Task ExecuteRegionCaptureAsync()
    {
        if (!TryBeginCapture())
        {
            return;
        }

        ShowStatus(
            InfoBarSeverity.Informational,
            "Preparing region capture",
            "Freezing the primary display before the selection overlay opens.");

        var restoreMainWindow = AppWindow.IsVisible;

        try
        {
            if (restoreMainWindow)
            {
                AppWindow.Hide();
                await Task.Delay(120);
            }

            var session = await _regionCaptureWorkflow.PreparePrimaryDisplayAsync(includeCursor: false);
            var overlay = new RegionCaptureWindow(_regionCaptureWorkflow, session);
            _regionCaptureWindow = overlay;

            var outcome = await overlay.ShowAsync();

            if (outcome.IsCancelled || outcome.SaveResult is null)
            {
                ShowStatus(InfoBarSeverity.Informational, "Region capture cancelled", "No file was created.");
            }
            else
            {
                ShowStatus(
                    InfoBarSeverity.Success,
                    "Region captured",
                    $"Saved {outcome.SaveResult.Width}×{outcome.SaveResult.Height} PNG to {outcome.SaveResult.FilePath}");
                RefreshRecentCaptures();
            }
        }
        catch (Exception exception)
        {
            ShowStatus(
                InfoBarSeverity.Error,
                "SNAPVERE couldn't capture the region",
                GetUserFacingCaptureError(exception));
        }
        finally
        {
            _regionCaptureWindow = null;
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

        ShowStatus(
            InfoBarSeverity.Informational,
            "Capturing screen",
            "SNAPVERE is capturing the primary display.");

        var restoreMainWindow = AppWindow.IsVisible;

        try
        {
            if (restoreMainWindow)
            {
                AppWindow.Hide();
                await Task.Delay(120);
            }

            var result = await _screenCaptureWorkflow.CapturePrimaryDisplayToDefaultFolderAsync(
                includeCursor: false);

            ShowStatus(
                InfoBarSeverity.Success,
                "Screen captured",
                $"Saved {result.Width}×{result.Height} PNG to {result.FilePath}");
            RefreshRecentCaptures();
        }
        catch (Exception exception)
        {
            ShowStatus(
                InfoBarSeverity.Error,
                "SNAPVERE couldn't capture the screen",
                GetUserFacingCaptureError(exception));
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
        try
        {
            var captures = _captureHistoryService.GetRecentCaptures();
            RecentCapturesList.ItemsSource = captures;
            RecentCapturesList.Visibility = captures.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            HistoryEmptyState.Visibility = captures.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            RecentCapturesCountText.Text = captures.Count switch
            {
                0 => "Local screenshots from Pictures\\SNAPVERE",
                1 => "1 recent local capture",
                _ => $"{captures.Count} recent local captures"
            };

            if (captures.Count == 0)
            {
                HistoryEmptyTitle.Text = "No local captures yet";
                HistoryEmptyMessage.Text = "Your completed Region and Screen captures will appear here.";
            }
        }
        catch (UnauthorizedAccessException)
        {
            ShowHistoryUnavailable("Windows denied access to the SNAPVERE capture folder.");
        }
        catch (IOException)
        {
            ShowHistoryUnavailable("SNAPVERE could not read the local capture folder.");
        }
    }

    private void ShowHistoryUnavailable(string message)
    {
        RecentCapturesList.ItemsSource = null;
        RecentCapturesList.Visibility = Visibility.Collapsed;
        HistoryEmptyState.Visibility = Visibility.Visible;
        HistoryEmptyTitle.Text = "Recent captures unavailable";
        HistoryEmptyMessage.Text = message;
        RecentCapturesCountText.Text = "Local history is currently unavailable";
    }

    private void SetCaptureButtonsEnabled(bool isEnabled)
    {
        RegionCaptureButton.IsEnabled = isEnabled;
        ScreenCaptureButton.IsEnabled = isEnabled;
    }

    private void ShowStatus(InfoBarSeverity severity, string title, string message)
    {
        CaptureStatus.IsOpen = true;
        CaptureStatus.Severity = severity;
        CaptureStatus.Title = title;
        CaptureStatus.Message = message;
    }

    private static string GetUserFacingCaptureError(Exception exception)
        => exception switch
        {
            UnauthorizedAccessException => "Windows denied access to the selected save location.",
            IOException => "The screenshot was captured, but SNAPVERE could not save the PNG file.",
            InvalidOperationException => exception.Message,
            _ => "The display configuration may have changed, or Windows may have blocked this capture. Try again."
        };
}
