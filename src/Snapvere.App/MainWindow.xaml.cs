using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snapvere.Application.Capture;

namespace Snapvere.App;

public sealed partial class MainWindow : Window
{
    private readonly ScreenCaptureWorkflow _screenCaptureWorkflow;

    public MainWindow(ScreenCaptureWorkflow screenCaptureWorkflow)
    {
        _screenCaptureWorkflow = screenCaptureWorkflow ?? throw new ArgumentNullException(nameof(screenCaptureWorkflow));
        InitializeComponent();
        RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
    }

    private async void ScreenCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        ScreenCaptureButton.IsEnabled = false;
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
            ScreenCaptureButton.IsEnabled = true;
        }
    }

    private void RootNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            Title = "SNAPVERE — Settings";
            return;
        }

        if (args.SelectedItemContainer?.Tag is string tag)
        {
            Title = tag switch
            {
                "capture" => "SNAPVERE — Capture",
                "history" => "SNAPVERE — History",
                "editor" => "SNAPVERE — Editor",
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
