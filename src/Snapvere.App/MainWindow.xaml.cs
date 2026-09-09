using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snapvere.Application.Capture;

namespace Snapvere.App;

public sealed partial class MainWindow : Window
{
    private readonly ScreenCaptureWorkflow _screenCaptureWorkflow;
    private readonly RegionCaptureWorkflow _regionCaptureWorkflow;
    private RegionCaptureWindow? _regionCaptureWindow;

    public MainWindow(
        ScreenCaptureWorkflow screenCaptureWorkflow,
        RegionCaptureWorkflow regionCaptureWorkflow)
    {
        _screenCaptureWorkflow = screenCaptureWorkflow ?? throw new ArgumentNullException(nameof(screenCaptureWorkflow));
        _regionCaptureWorkflow = regionCaptureWorkflow ?? throw new ArgumentNullException(nameof(regionCaptureWorkflow));

        InitializeComponent();
        RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
    }

    private async void RegionCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_regionCaptureWindow is not null)
        {
            return;
        }

        SetCaptureButtonsEnabled(false);
        CaptureStatus.IsOpen = true;
        CaptureStatus.Severity = InfoBarSeverity.Informational;
        CaptureStatus.Title = "Preparing region capture";
        CaptureStatus.Message = "Freezing the primary display before the selection overlay opens.";

        var mainWindowHidden = false;

        try
        {
            AppWindow.Hide();
            mainWindowHidden = true;

            // Give Desktop Window Manager one short presentation interval to remove
            // the SNAPVERE main window before freezing the display.
            await Task.Delay(120);

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

            if (mainWindowHidden)
            {
                AppWindow.Show();
                Activate();
            }

            SetCaptureButtonsEnabled(true);
        }
    }

    private async void ScreenCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        SetCaptureButtonsEnabled(false);
        CaptureStatus.IsOpen = true;
        CaptureStatus.Severity = InfoBarSeverity.Informational;
        CaptureStatus.Title = "Capturing screen";
        CaptureStatus.Message = "SNAPVERE is capturing the primary display.";

        try
        {
            var result = await _screenCaptureWorkflow.CapturePrimaryDisplayToDefaultFolderAsync(
                includeCursor: false);

            CaptureStatus.Severity = InfoBarSeverity.Success;
            CaptureStatus.Title = "Screen captured";
            CaptureStatus.Message = $"Saved {result.Width}×{result.Height} PNG to {result.FilePath}";
        }
        catch (Exception exception)
        {
            CaptureStatus.Severity = InfoBarSeverity.Error;
            CaptureStatus.Title = "SNAPVERE couldn't capture the screen";
            CaptureStatus.Message = GetUserFacingCaptureError(exception);
        }
        finally
        {
            SetCaptureButtonsEnabled(true);
        }
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
