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

    public MainWindow(
        ScreenCaptureWorkflow screenCaptureWorkflow,
        RegionCaptureWorkflow regionCaptureWorkflow,
        CaptureHistoryService captureHistoryService)
    {
        _screenCaptureWorkflow = screenCaptureWorkflow ?? throw new ArgumentNullException(nameof(screenCaptureWorkflow));
        _regionCaptureWorkflow = regionCaptureWorkflow ?? throw new ArgumentNullException(nameof(regionCaptureWorkflow));
        _captureHistoryService = captureHistoryService ?? throw new ArgumentNullException(nameof(captureHistoryService));

        InitializeComponent();
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
