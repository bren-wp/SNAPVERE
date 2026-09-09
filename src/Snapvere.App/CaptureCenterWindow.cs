using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snapvere.Application.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Domain.Capture;
using System.Diagnostics;

namespace Snapvere.App;

/// <summary>
/// Production capture shell that intentionally uses the standard Windows
/// title bar and a conservative WinUI control surface. Capture workflows,
/// hotkeys and tray commands remain fully connected while optional backdrop,
/// custom-title-bar and complex templated startup dependencies are avoided.
/// </summary>
public sealed class CaptureCenterWindow : Window
{
    private readonly ScreenCaptureWorkflow _screenCaptureWorkflow;
    private readonly RegionCaptureWorkflow _regionCaptureWorkflow;
    private readonly CaptureHistoryService _captureHistoryService;

    private readonly Button _regionCaptureButton;
    private readonly Button _screenCaptureButton;
    private readonly TextBlock _statusTitle;
    private readonly TextBlock _statusMessage;
    private readonly Border _statusPanel;
    private readonly TextBlock _recentSummary;
    private readonly StackPanel _recentItems;

    private RegionCaptureWindow? _regionCaptureWindow;
    private bool _captureInProgress;

    public CaptureCenterWindow(
        ScreenCaptureWorkflow screenCaptureWorkflow,
        RegionCaptureWorkflow regionCaptureWorkflow,
        CaptureHistoryService captureHistoryService)
    {
        _screenCaptureWorkflow = screenCaptureWorkflow ?? throw new ArgumentNullException(nameof(screenCaptureWorkflow));
        _regionCaptureWorkflow = regionCaptureWorkflow ?? throw new ArgumentNullException(nameof(regionCaptureWorkflow));
        _captureHistoryService = captureHistoryService ?? throw new ArgumentNullException(nameof(captureHistoryService));

        Title = "SNAPVERE — Capture Center";

        _regionCaptureButton = CreatePrimaryButton("Select region", RegionCaptureButton_Click);
        _screenCaptureButton = CreateSecondaryButton("Capture screen", ScreenCaptureButton_Click);
        _statusTitle = CreateText("Ready", 13, Brush(0xFF, 0xFF, 0xFF));
        _statusMessage = CreateText("Choose Region or Screen to capture locally.", 12, Brush(0xA9, 0xB2, 0xC3));
        _statusMessage.TextWrapping = TextWrapping.Wrap;
        _statusPanel = CreateStatusPanel();
        _recentSummary = CreateText("Local screenshots from Pictures\\SNAPVERE", 12, Brush(0x98, 0xA2, 0xB3));
        _recentItems = new StackPanel { Spacing = 8 };

        Content = BuildWindowContent();
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

        var gestures = string.Join(", ", report.Conflicts.Select(conflict => conflict.Binding.GestureText));
        ShowStatus(
            "Some global hotkeys are unavailable",
            $"Another application is already using: {gestures}. Capture buttons remain available.",
            StatusKind.Warning);
    }

    public void ReportHotkeyHostFailure()
        => ShowStatus(
            "Global hotkeys unavailable",
            "SNAPVERE could not start the Windows global-hotkey host. Capture buttons remain available.",
            StatusKind.Warning);

    public void ReportTrayHostFailure()
        => ShowStatus(
            "System tray unavailable",
            "SNAPVERE could not create its Windows notification-area icon. Capture buttons and global hotkeys remain available.",
            StatusKind.Warning);

    private UIElement BuildWindowContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Brush(0x0B, 0x0D, 0x12),
            Padding = new Thickness(28)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = BuildHeader();
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        _statusPanel.Margin = new Thickness(0, 18, 0, 0);
        Grid.SetRow(_statusPanel, 1);
        root.Children.Add(_statusPanel);

        var actions = BuildCaptureActions();
        actions.Margin = new Thickness(0, 18, 0, 0);
        Grid.SetRow(actions, 2);
        root.Children.Add(actions);

        var recent = BuildRecentSection();
        recent.Margin = new Thickness(0, 22, 0, 0);
        Grid.SetRow(recent, 3);
        root.Children.Add(recent);

        return root;
    }

    private Grid BuildHeader()
    {
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var brand = new StackPanel { Spacing = 5 };
        brand.Children.Add(CreateText("SNAPVERE", 12, Brush(0xA8, 0x9E, 0xFF)));
        brand.Children.Add(CreateText("Capture center", 30, Brush(0xFF, 0xFF, 0xFF)));
        brand.Children.Add(CreateText(
            "Fast capture, precise selection, local PNG output.",
            14,
            Brush(0xA9, 0xB2, 0xC3)));
        header.Children.Add(brand);

        var tools = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        var version = typeof(CaptureCenterWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.2";
        var versionBadge = new Border
        {
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(8),
            Background = Brush(0x1A, 0x1F, 0x2A),
            Child = CreateText($"v{version}", 11, Brush(0xD8, 0xDC, 0xE5))
        };
        tools.Children.Add(versionBadge);

        var openFolder = CreateSecondaryButton("Open folder", OpenCaptureFolderButton_Click);
        tools.Children.Add(openFolder);

        Grid.SetColumn(tools, 1);
        header.Children.Add(tools);
        return header;
    }

    private Border CreateStatusPanel()
    {
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(_statusTitle);
        stack.Children.Add(_statusMessage);

        return new Border
        {
            Padding = new Thickness(14, 11, 14, 11),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0x16, 0x1B, 0x24),
            BorderBrush = Brush(0x2A, 0x31, 0x40),
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private Grid BuildCaptureActions()
    {
        var actions = new Grid { ColumnSpacing = 14 };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

        var region = BuildRegionCard();
        actions.Children.Add(region);

        var screen = BuildScreenCard();
        Grid.SetColumn(screen, 1);
        actions.Children.Add(screen);

        return actions;
    }

    private Border BuildRegionCard()
    {
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(CreateShortcutRow("REGION", "Ctrl + Shift + 1"));
        stack.Children.Add(CreateText("Capture a precise region", 22, Brush(0xFF, 0xFF, 0xFF)));

        var description = CreateText(
            "Freeze the primary display, drag an exact area, fine-tune the selection, then save it locally as PNG.",
            12,
            Brush(0xD8, 0xD6, 0xF2));
        description.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(description);
        stack.Children.Add(_regionCaptureButton);

        return new Border
        {
            MinHeight = 220,
            Padding = new Thickness(22),
            CornerRadius = new CornerRadius(16),
            Background = Brush(0x30, 0x2A, 0x63),
            BorderBrush = Brush(0x57, 0x49, 0xC9),
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private Border BuildScreenCard()
    {
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(CreateShortcutRow("SCREEN", "Ctrl + Shift + 4"));
        stack.Children.Add(CreateText("Full screen", 20, Brush(0xFF, 0xFF, 0xFF)));

        var description = CreateText(
            "Capture the primary display directly to Pictures\\SNAPVERE.",
            12,
            Brush(0xA9, 0xB2, 0xC3));
        description.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(description);
        stack.Children.Add(_screenCaptureButton);

        var privacy = CreateText(
            "Private by default · no account or telemetry required.",
            11,
            Brush(0x7F, 0xC9, 0xA5));
        privacy.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(privacy);

        return new Border
        {
            MinHeight = 220,
            Padding = new Thickness(22),
            CornerRadius = new CornerRadius(16),
            Background = Brush(0x16, 0x1B, 0x24),
            BorderBrush = Brush(0x2A, 0x31, 0x40),
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private Grid CreateShortcutRow(string label, string shortcut)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        row.Children.Add(CreateText(label, 10, Brush(0xA8, 0x9E, 0xFF)));

        var shortcutBadge = new Border
        {
            Padding = new Thickness(8, 4, 8, 4),
            CornerRadius = new CornerRadius(7),
            Background = Brush(0x22, 0x27, 0x33),
            Child = CreateText(shortcut, 10, Brush(0xD8, 0xDC, 0xE5))
        };
        Grid.SetColumn(shortcutBadge, 1);
        row.Children.Add(shortcutBadge);
        return row;
    }

    private Border BuildRecentSection()
    {
        var section = new Grid();
        section.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        section.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new StackPanel { Spacing = 3 };
        title.Children.Add(CreateText("Recent captures", 18, Brush(0xFF, 0xFF, 0xFF)));
        title.Children.Add(_recentSummary);
        header.Children.Add(title);

        var refresh = CreateSecondaryButton("Refresh", RefreshHistoryButton_Click);
        Grid.SetColumn(refresh, 1);
        header.Children.Add(refresh);
        section.Children.Add(header);

        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 240,
            Content = _recentItems,
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetRow(scroller, 1);
        section.Children.Add(scroller);

        return new Border
        {
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(14),
            Background = Brush(0x12, 0x15, 0x1C),
            BorderBrush = Brush(0x2A, 0x31, 0x40),
            BorderThickness = new Thickness(1),
            Child = section
        };
    }

    private static Button CreatePrimaryButton(string text, RoutedEventHandler handler)
    {
        var button = new Button
        {
            Content = text,
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(18, 10, 18, 10),
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
            Padding = new Thickness(12, 8, 12, 8)
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
            "Preparing region capture",
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
            var overlay = new RegionCaptureWindow(_regionCaptureWorkflow, session);
            _regionCaptureWindow = overlay;

            var outcome = await overlay.ShowAsync();
            if (outcome.IsCancelled || outcome.SaveResult is null)
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
            var captures = _captureHistoryService.GetRecentCaptures(limit: 8);
            _recentSummary.Text = captures.Count switch
            {
                0 => "Local screenshots from Pictures\\SNAPVERE",
                1 => "1 recent local capture",
                _ => $"{captures.Count} recent local captures"
            };

            if (captures.Count == 0)
            {
                var empty = CreateText(
                    "No local captures yet. Completed Region and Screen captures will appear here.",
                    12,
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
        var text = new StackPanel { Spacing = 2 };
        text.Children.Add(CreateText(item.FileName, 12, Brush(0xFF, 0xFF, 0xFF)));
        text.Children.Add(CreateText(item.MetadataText, 10, Brush(0x98, 0xA2, 0xB3)));

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 9, 12, 9),
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
        _recentSummary.Text = "Local history is currently unavailable";
        _recentItems.Children.Clear();
        var text = CreateText(message, 12, Brush(0xD9, 0xA2, 0x64));
        text.TextWrapping = TextWrapping.Wrap;
        _recentItems.Children.Add(text);
    }

    private void SetCaptureButtonsEnabled(bool isEnabled)
    {
        _regionCaptureButton.IsEnabled = isEnabled;
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
            InvalidOperationException => exception.Message,
            _ => "The display configuration may have changed, or Windows may have blocked this capture. Try again."
        };

    private enum StatusKind
    {
        Information,
        Success,
        Warning,
        Error
    }
}
