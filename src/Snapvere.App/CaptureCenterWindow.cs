using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snapvere.Application.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;
using System.Diagnostics;

namespace Snapvere.App;

/// <summary>
/// Compact production launcher for SNAPVERE. The application is intentionally
/// capture-first and tray-friendly: Region Capture is the primary action,
/// Window and Screen Capture remain directly available, and local recent
/// captures stay visible without turning startup into a large dashboard.
/// </summary>
public sealed class CaptureCenterWindow : Window
{
    private const int RecentCaptureLimit = 4;

    private readonly ScreenCaptureWorkflow _screenCaptureWorkflow;
    private readonly RegionCaptureWorkflow _regionCaptureWorkflow;
    private readonly WindowCaptureWorkflow _windowCaptureWorkflow;
    private readonly WindowTargetPicker _windowTargetPicker;
    private readonly CaptureHistoryService _captureHistoryService;
    private readonly PngCaptureEncoder _pngEncoder;

    private readonly Button _regionCaptureButton;
    private readonly Button _windowCaptureButton;
    private readonly Button _screenCaptureButton;
    private readonly TextBlock _statusTitle;
    private readonly TextBlock _statusMessage;
    private readonly Border _statusPanel;
    private readonly TextBlock _recentSummary;
    private readonly StackPanel _recentItems;

    private RegionCaptureWindow? _regionCaptureWindow;
    private bool _captureInProgress;
    private bool _initialSizeApplied;

    public CaptureCenterWindow(
        ScreenCaptureWorkflow screenCaptureWorkflow,
        RegionCaptureWorkflow regionCaptureWorkflow,
        WindowCaptureWorkflow windowCaptureWorkflow,
        WindowTargetPicker windowTargetPicker,
        CaptureHistoryService captureHistoryService,
        PngCaptureEncoder pngEncoder)
    {
        _screenCaptureWorkflow = screenCaptureWorkflow ?? throw new ArgumentNullException(nameof(screenCaptureWorkflow));
        _regionCaptureWorkflow = regionCaptureWorkflow ?? throw new ArgumentNullException(nameof(regionCaptureWorkflow));
        _windowCaptureWorkflow = windowCaptureWorkflow ?? throw new ArgumentNullException(nameof(windowCaptureWorkflow));
        _windowTargetPicker = windowTargetPicker ?? throw new ArgumentNullException(nameof(windowTargetPicker));
        _captureHistoryService = captureHistoryService ?? throw new ArgumentNullException(nameof(captureHistoryService));
        _pngEncoder = pngEncoder ?? throw new ArgumentNullException(nameof(pngEncoder));

        Title = "SNAPVERE";

        _regionCaptureButton = CreatePrimaryButton("Region capture", RegionCaptureButton_Click);
        _windowCaptureButton = CreateSecondaryButton("Window capture", WindowCaptureButton_Click);
        _screenCaptureButton = CreateSecondaryButton("Screen capture", ScreenCaptureButton_Click);
        _statusTitle = CreateText("Ready", 12, Brush(0xFF, 0xFF, 0xFF));
        _statusMessage = CreateText(
            "Press Print Screen for Region Capture. Ctrl + Shift + 2 starts Window Capture.",
            11,
            Brush(0xA9, 0xB2, 0xC3));
        _statusMessage.TextWrapping = TextWrapping.Wrap;
        _statusPanel = CreateStatusPanel();
        _recentSummary = CreateText("Pictures\\SNAPVERE", 11, Brush(0x98, 0xA2, 0xB3));
        _recentItems = new StackPanel { Spacing = 6 };

        Content = BuildWindowContent();
        Activated += CaptureCenterWindow_Activated;
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
                "Print Screen starts Region Capture, Ctrl + Shift + 2 starts Window Capture and Ctrl + Shift + 4 captures the screen.",
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
                "Windows or another application owns Print Screen. Ctrl + Shift + 1 still starts Region Capture; other registered SNAPVERE shortcuts remain available.",
                StatusKind.Warning);
            return;
        }

        var gestures = string.Join(", ", report.Conflicts.Select(conflict => conflict.Binding.GestureText));
        ShowStatus(
            "Some global hotkeys are unavailable",
            $"Already in use: {gestures}. The launcher and tray capture actions remain available.",
            StatusKind.Warning);
    }

    public void ReportHotkeyHostFailure()
        => ShowStatus(
            "Global hotkeys unavailable",
            "SNAPVERE could not start the Windows global-hotkey host. Launcher and tray capture actions remain available.",
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
        AppWindow.Resize(new Windows.Graphics.SizeInt32(560, 620));
    }

    private UIElement BuildWindowContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Brush(0x0B, 0x0D, 0x12),
            Padding = new Thickness(20)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = BuildHeader();
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var actions = BuildCaptureActions();
        actions.Margin = new Thickness(0, 16, 0, 0);
        Grid.SetRow(actions, 1);
        root.Children.Add(actions);

        _statusPanel.Margin = new Thickness(0, 12, 0, 0);
        Grid.SetRow(_statusPanel, 2);
        root.Children.Add(_statusPanel);

        var recent = BuildRecentSection();
        recent.Margin = new Thickness(0, 14, 0, 0);
        Grid.SetRow(recent, 3);
        root.Children.Add(recent);

        var footer = BuildFooter();
        footer.Margin = new Thickness(0, 12, 0, 0);
        Grid.SetRow(footer, 4);
        root.Children.Add(footer);

        return root;
    }

    private Grid BuildHeader()
    {
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var brand = new StackPanel { Spacing = 2 };
        brand.Children.Add(CreateText("SNAPVERE", 11, Brush(0xA8, 0x9E, 0xFF)));
        brand.Children.Add(CreateText("Capture. Edit. Done.", 24, Brush(0xFF, 0xFF, 0xFF)));
        var subtitle = CreateText(
            "Fast region, window and screen capture with local-first output.",
            12,
            Brush(0xA9, 0xB2, 0xC3));
        subtitle.TextWrapping = TextWrapping.Wrap;
        brand.Children.Add(subtitle);
        header.Children.Add(brand);

        var version = typeof(CaptureCenterWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.3";
        var versionBadge = new Border
        {
            Padding = new Thickness(8, 5, 8, 5),
            CornerRadius = new CornerRadius(7),
            Background = Brush(0x1A, 0x1F, 0x2A),
            Child = CreateText($"v{version}", 10, Brush(0xD8, 0xDC, 0xE5))
        };
        Grid.SetColumn(versionBadge, 1);
        header.Children.Add(versionBadge);
        return header;
    }

    private Border CreateStatusPanel()
    {
        var stack = new StackPanel { Spacing = 3 };
        stack.Children.Add(_statusTitle);
        stack.Children.Add(_statusMessage);

        return new Border
        {
            Padding = new Thickness(12, 9, 12, 9),
            CornerRadius = new CornerRadius(9),
            Background = Brush(0x16, 0x1B, 0x24),
            BorderBrush = Brush(0x2A, 0x31, 0x40),
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private Border BuildCaptureActions()
    {
        var stack = new StackPanel { Spacing = 10 };

        var shortcut = new Grid();
        shortcut.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        shortcut.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        shortcut.Children.Add(CreateText("REGION CAPTURE", 10, Brush(0xB6, 0xAE, 0xFF)));

        var shortcutBadge = new Border
        {
            Padding = new Thickness(9, 4, 9, 4),
            CornerRadius = new CornerRadius(7),
            Background = Brush(0x22, 0x27, 0x33),
            Child = CreateText("Print Screen", 10, Brush(0xFF, 0xFF, 0xFF))
        };
        Grid.SetColumn(shortcutBadge, 1);
        shortcut.Children.Add(shortcutBadge);
        stack.Children.Add(shortcut);

        var description = CreateText(
            "Select a region and annotate it, target a desktop window, or save the primary screen directly to PNG.",
            12,
            Brush(0xE1, 0xDF, 0xF6));
        description.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(description);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        buttons.Children.Add(_regionCaptureButton);
        buttons.Children.Add(_windowCaptureButton);
        buttons.Children.Add(_screenCaptureButton);
        stack.Children.Add(buttons);

        var fallback = CreateText(
            "Region fallback: Ctrl + Shift + 1   ·   Window: Ctrl + Shift + 2   ·   Screen: Ctrl + Shift + 4",
            10,
            Brush(0xA9, 0xB2, 0xC3));
        fallback.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(fallback);

        return new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(13),
            Background = Brush(0x2B, 0x26, 0x58),
            BorderBrush = Brush(0x57, 0x49, 0xC9),
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private Border BuildRecentSection()
    {
        var section = new Grid();
        section.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        section.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new StackPanel { Spacing = 2 };
        title.Children.Add(CreateText("Recent captures", 14, Brush(0xFF, 0xFF, 0xFF)));
        title.Children.Add(_recentSummary);
        header.Children.Add(title);

        var refresh = CreateSecondaryButton("Refresh", RefreshHistoryButton_Click);
        Grid.SetColumn(refresh, 1);
        header.Children.Add(refresh);
        section.Children.Add(header);

        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 165,
            Content = _recentItems,
            Margin = new Thickness(0, 9, 0, 0)
        };
        Grid.SetRow(scroller, 1);
        section.Children.Add(scroller);

        return new Border
        {
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(11),
            Background = Brush(0x12, 0x15, 0x1C),
            BorderBrush = Brush(0x2A, 0x31, 0x40),
            BorderThickness = new Thickness(1),
            Child = section
        };
    }

    private Grid BuildFooter()
    {
        var footer = new Grid();
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var hint = CreateText(
            "Tray-first: hide this launcher and keep capture hotkeys ready.",
            10,
            Brush(0x7F, 0xC9, 0xA5));
        hint.VerticalAlignment = VerticalAlignment.Center;
        hint.TextWrapping = TextWrapping.Wrap;
        footer.Children.Add(hint);

        var openFolder = CreateSecondaryButton("Open folder", OpenCaptureFolderButton_Click);
        openFolder.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(openFolder, 1);
        footer.Children.Add(openFolder);

        var hide = CreateSecondaryButton("Hide to tray", HideToTrayButton_Click);
        hide.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(hide, 2);
        footer.Children.Add(hide);
        return footer;
    }

    private static Button CreatePrimaryButton(string text, RoutedEventHandler handler)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(18, 9, 18, 9),
            Background = Brush(0x7C, 0x6C, 0xFF),
            Foreground = Brush(0xFF, 0xFF, 0xFF)
        };
        button.Click += handler;
        return button;
    }

    private static Button CreateSecondaryButton(string text, RoutedEventHandler handler)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(11, 7, 11, 7)
        };
        button.Click += handler;
        return button;
    }

    private static TextBlock CreateText(string text, double size, SolidColorBrush foreground)
        => new()
        {
            Text = text,
            FontSize = size,
            Foreground = foreground
        };

    private static SolidColorBrush Brush(byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(0xFF, red, green, blue));

    private async void RegionCaptureButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteRegionCaptureAsync();

    private async void WindowCaptureButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteWindowCaptureAsync();

    private async void ScreenCaptureButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteScreenCaptureAsync();

    private void RefreshHistoryButton_Click(object sender, RoutedEventArgs e)
        => RefreshRecentCaptures();

    private void HideToTrayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_captureInProgress)
        {
            return;
        }

        AppWindow.Hide();
    }

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
            ShowStatus(
                "Capture folder unavailable",
                "Windows could not open the local SNAPVERE capture folder.",
                StatusKind.Error);
        }
    }

    private async Task ExecuteRegionCaptureAsync()
    {
        if (!TryBeginCapture())
        {
            return;
        }

        ShowStatus(
            "Preparing Region Capture",
            "Freezing the primary display before the selection overlay opens.",
            StatusKind.Information);

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
                ShowStatus(
                    "Region copied",
                    "The selected image and annotations are on the Windows clipboard.",
                    StatusKind.Success);
            }
            else if (outcome.IsCancelled || outcome.SaveResult is null)
            {
                ShowStatus("Region capture cancelled", "No file was created.", StatusKind.Information);
            }
            else
            {
                ShowStatus(
                    "Region captured",
                    $"Saved {outcome.SaveResult.Width}×{outcome.SaveResult.Height} PNG to {outcome.SaveResult.FilePath}",
                    StatusKind.Success);
                RefreshRecentCaptures();
            }
        }
        catch (Exception exception)
        {
            ShowStatus(
                "SNAPVERE couldn't capture the region",
                GetUserFacingCaptureError(exception),
                StatusKind.Error);
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

        ShowStatus(
            "Preparing Window Capture",
            "Freezing the desktop and discovering visible windows before the target picker opens.",
            StatusKind.Information);

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

            var result = await _windowCaptureWorkflow.CaptureWindowToDefaultFolderAsync(
                target,
                includeCursor: false);
            ShowStatus(
                "Window captured",
                $"Saved {result.Width}×{result.Height} PNG from “{target.Title}” to {result.FilePath}",
                StatusKind.Success);
            RefreshRecentCaptures();
        }
        catch (Exception exception)
        {
            ShowStatus(
                "SNAPVERE couldn't capture the window",
                GetUserFacingCaptureError(exception),
                StatusKind.Error);
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
            ShowStatus(
                "Screen captured",
                $"Saved {result.Width}×{result.Height} PNG to {result.FilePath}",
                StatusKind.Success);
            RefreshRecentCaptures();
        }
        catch (Exception exception)
        {
            ShowStatus(
                "SNAPVERE couldn't capture the screen",
                GetUserFacingCaptureError(exception),
                StatusKind.Error);
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

        try
        {
            var captures = _captureHistoryService.GetRecentCaptures(limit: RecentCaptureLimit);
            _recentSummary.Text = captures.Count switch
            {
                0 => "Pictures\\SNAPVERE",
                1 => "1 recent local capture",
                _ => $"{captures.Count} recent local captures"
            };

            if (captures.Count == 0)
            {
                var empty = CreateText(
                    "No local captures yet. Region, Window and Screen captures will appear here.",
                    11,
                    Brush(0x98, 0xA2, 0xB3));
                empty.TextWrapping = TextWrapping.Wrap;
                _recentItems.Children.Add(empty);
                return;
            }

            foreach (var item in captures)
            {
                _recentItems.Children.Add(CreateRecentCaptureRow(item));
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

    private Button CreateRecentCaptureRow(CaptureHistoryItem item)
    {
        var text = new StackPanel { Spacing = 1 };
        text.Children.Add(CreateText(item.FileName, 11, Brush(0xFF, 0xFF, 0xFF)));
        text.Children.Add(CreateText(item.MetadataText, 9, Brush(0x98, 0xA2, 0xB3)));

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 7, 10, 7),
            Content = text
        };
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
            ShowStatus(
                "Could not open capture",
                "Windows could not open that screenshot with the default image application.",
                StatusKind.Error);
        }
    }

    private void ShowRecentUnavailable(string message)
    {
        _recentSummary.Text = "Local history unavailable";
        _recentItems.Children.Clear();
        var text = CreateText(message, 11, Brush(0xD9, 0xA2, 0x64));
        text.TextWrapping = TextWrapping.Wrap;
        _recentItems.Children.Add(text);
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
            StatusKind.Success => Brush(0x35, 0x8B, 0x68),
            StatusKind.Warning => Brush(0x9A, 0x6D, 0x2A),
            StatusKind.Error => Brush(0xA9, 0x45, 0x45),
            _ => Brush(0x3C, 0x59, 0x82)
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

    private enum StatusKind
    {
        Information,
        Success,
        Warning,
        Error
    }
}
