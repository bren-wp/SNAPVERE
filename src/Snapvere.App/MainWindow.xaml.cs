using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snapvere.Application.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Domain.Capture;

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
        VersionText.Text = $"v{typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.1"}";
        RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
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

        CaptureStatus.IsOpen = true;
        CaptureStatus.Severity = InfoBarSeverity.Warning;
        CaptureStatus.Title = "Some global hotkeys are unavailable";
        CaptureStatus.Message = $"Another application is already using: {gestures}. Capture buttons remain available.";
    }

    public void ReportHotkeyHostFailure()
    {
        CaptureStatus.IsOpen = true;
        CaptureStatus.Severity = InfoBarSeverity.Warning;
        CaptureStatus.Title = "Global hotkeys unavailable";
        CaptureStatus.Message = "SNAPVERE could not start the Windows global-hotkey host. Capture buttons remain available.";
    }

    public void ReportTrayHostFailure()
    {
        CaptureStatus.IsOpen = true;
        CaptureStatus.Severity = InfoBarSeverity.Warning;
        CaptureStatus.Title = "System tray unavailable";
        CaptureStatus.Message = "SNAPVERE could not create its Windows notification-area icon. Capture buttons and global hotkeys remain available.";
    }

    private async void RegionCaptureButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteRegionCaptureAsync();

    private async void ScreenCaptureButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteScreenCaptureAsync();

    private void RefreshHistoryButton_Click(object sender, RoutedEventArgs e)
        => RefreshRecentCaptures();

    private async Task ExecuteRegionCaptureAsync()
    {
        if (!TryBeginCapture())
        {
            return;
        }

        CaptureStatus.IsOpen = true;
        CaptureStatus.Severity = InfoBarSeverity.Informational;
        CaptureStatus.Title = "Preparing region capture";
        CaptureStatus.Message = "Freezing the primary display before the selection overlay opens.";

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
                CaptureStatus.Severity = InfoBarSeverity.Informational;
                CaptureStatus.Title = "Region capture cancelled";
                CaptureStatus.Message = "No file was created.";
            }
            else
            {
                CaptureStatus.Severity = InfoBarSeverity.Success;
                CaptureStatus.Title = "Region captured";
                CaptureStatus.Message =
                    $"Saved {outcome.SaveResult.Width}×{outcome.SaveResult.Height} PNG to {outcome.SaveResult.FilePath}";
                RefreshRecentCaptures();
            }
        }
        catch (Exception exception)
        {
            CaptureStatus.Severity = InfoBarSeverity.Error;
            CaptureStatus.Title = "SNAPVERE couldn't capture the region";
            CaptureStatus.Message = GetUserFacingCaptureError(exception);
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

        CaptureStatus.IsOpen = true;
        CaptureStatus.Severity = InfoBarSeverity.Informational;
        CaptureStatus.Title = "Capturing screen";
        CaptureStatus.Message = "SNAPVERE is capturing the primary display.";

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

            CaptureStatus.Severity = InfoBarSeverity.Success;
            CaptureStatus.Title = "Screen captured";
            CaptureStatus.Message = $"Saved {result.Width}×{result.Height} PNG to {result.FilePath}";
            RefreshRecentCaptures();
        }
        catch (Exception exception)
        {
            CaptureStatus.Severity = InfoBarSeverity.Error;
            CaptureStatus.Title = "SNAPVERE couldn't capture the screen";
            CaptureStatus.Message = GetUserFacingCaptureError(exception);
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

            if (captures.Count == 0)
            {
                HistoryEmptyTitle.Text = "No local captures yet.";
                HistoryEmptyMessage.Text = "Completed Screen and Region captures will appear here after they are saved locally.";
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
    }

    private void SetCaptureButtonsEnabled(bool isEnabled)
    {
        RegionCaptureButton.IsEnabled = isEnabled;
        ScreenCaptureButton.IsEnabled = isEnabled;
    }

    private void RootNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is string tag)
        {
            Title = tag switch
            {
                "capture" => "SNAPVERE — Capture",
                _ => "SNAPVERE"
            };
        }
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
