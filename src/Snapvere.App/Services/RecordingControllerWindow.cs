using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Snapvere.Shared;
using System.Diagnostics;
using System.Globalization;
using Windows.System;

namespace Snapvere.App.Services;

/// <summary>
/// Small visible controller that exists only while screen recording is active.
/// Closing the surface is treated as an explicit stop request so recording
/// cannot continue without a visible Stop control.
/// </summary>
public sealed class RecordingControllerWindow : Window
{
    private readonly Action _stopAction;
    private readonly string _languageCode;
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _statusText;
    private readonly TextBlock _elapsedText;
    private readonly Button _stopButton;
    private bool _stopRequested;
    private bool _closingFromOwner;
    private bool _sizeApplied;

    public RecordingControllerWindow(Action stopAction, string? languageCode)
    {
        _stopAction = stopAction ?? throw new ArgumentNullException(nameof(stopAction));
        _languageCode = SnapvereLocalization.NormalizeLanguageCode(
            languageCode ?? SnapvereLanguageState.CurrentLanguageCode);

        Title = $"SNAPVERE — {L("ScreenRecordingActive")}";
        var content = BuildContent();
        _statusText = content.Status;
        _elapsedText = content.Elapsed;
        _stopButton = content.Stop;
        Content = content.Root;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        Activated += RecordingControllerWindow_Activated;
        Closed += RecordingControllerWindow_Closed;
    }

    public void CloseFromOwner()
    {
        if (_closingFromOwner)
        {
            return;
        }

        _closingFromOwner = true;
        StopTimer();
        try
        {
            Close();
        }
        catch (InvalidOperationException)
        {
            // The window may already be closing through user chrome.
        }
    }

    private string L(string key)
        => SnapvereLocalization.T(key, _languageCode);

    private (TextBlock Status, TextBlock Elapsed, Button Stop, FrameworkElement Root) BuildContent()
    {
        var grid = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Padding = new Thickness(14, 12, 12, 12)
        };
        grid.KeyDown += Root_KeyDown;
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Match the floating recorder reference: a small actual recording
        // indicator rather than an unrelated application tile.
        var brand = new Border
        {
            Width = 14,
            Height = 14,
            CornerRadius = new CornerRadius(7),
            Background = SnapvereBrand.Danger,
            BorderBrush = Brush(0x88, 0xF0, 0x63, 0x82),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(brand);

        var status = new StackPanel
        {
            Spacing = 1,
            Margin = new Thickness(14, 0, 22, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var statusText = new TextBlock
        {
            Text = L("ScreenRecordingActive"),
            FontSize = 10.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = SnapvereBrand.Muted
        };
        AutomationProperties.SetLiveSetting(statusText, AutomationLiveSetting.Polite);
        status.Children.Add(statusText);

        var timerRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 9,
            VerticalAlignment = VerticalAlignment.Center
        };
        var elapsed = new TextBlock
        {
            Text = "00:00",
            FontSize = 26,
            FontFamily = new FontFamily("Segoe UI Semibold"),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = SnapvereBrand.Strong
        };
        AutomationProperties.SetName(elapsed, UserText("Recording elapsed time", "Proteklo vrijeme snimanja"));
        timerRow.Children.Add(elapsed);
        status.Children.Add(timerRow);
        Grid.SetColumn(status, 1);
        grid.Children.Add(status);

        var stop = new Button
        {
            MinWidth = 132,
            Height = 46,
            Padding = new Thickness(18, 0, 18, 0),
            CornerRadius = new CornerRadius(14),
            Background = Brush(0xFF, 0x43, 0x2A, 0x3E),
            BorderBrush = Brush(0xAA, 0xF0, 0x63, 0x82),
            BorderThickness = new Thickness(1),
            Foreground = SnapvereBrand.Strong,
            VerticalAlignment = VerticalAlignment.Center,
            Content = CreateStopContent()
        };
        AutomationProperties.SetName(stop, L("StopScreenRecording"));
        ToolTipService.SetToolTip(stop, L("StopScreenRecording"));
        stop.Click += (_, _) => RequestStop();
        Grid.SetColumn(stop, 2);
        grid.Children.Add(stop);

        var root = new Border
        {
            RequestedTheme = ElementTheme.Dark,
            Background = SnapvereBrand.Surface,
            BorderBrush = SnapvereBrand.Outline,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Child = grid
        };

        return (statusText, elapsed, stop, root);
    }

    private void RequestStop(bool recoverUiOnFailure = true)
    {
        if (_stopRequested)
        {
            return;
        }

        _stopRequested = true;
        _statusText.Text = UserText("Finishing recording…", "Dovršavanje snimanja…");
        var stoppingText = UserText(
            "Finishing screen recording",
            "Dovršavanje snimanja zaslona");
        _stopButton.Content = new ProgressRing
        {
            Width = 18,
            Height = 18,
            IsActive = true,
            IsTabStop = false
        };
        AutomationProperties.SetName(_stopButton, stoppingText);
        AutomationProperties.SetHelpText(_stopButton, stoppingText);
        ToolTipService.SetToolTip(_stopButton, stoppingText);
        _stopButton.IsEnabled = false;
        StopTimer();

        try
        {
            _stopAction();
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Stop screen recording from controller", exception);
            if (!recoverUiOnFailure)
            {
                return;
            }

            _stopRequested = false;
            _statusText.Text = UserText(
                "Recording is still active. Try Stop again.",
                "Snimanje je još aktivno. Pokušajte ponovno zaustaviti.");
            _stopButton.Content = CreateStopContent();
            var stopText = L("StopScreenRecording");
            AutomationProperties.SetName(_stopButton, stopText);
            AutomationProperties.SetHelpText(_stopButton, stopText);
            ToolTipService.SetToolTip(_stopButton, stopText);
            _stopButton.IsEnabled = true;
            _elapsed.Start();
            _timer.Start();
            _ = _stopButton.Focus(FocusState.Programmatic);
        }
    }

    private StackPanel CreateStopContent()
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 9,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(CreateStopGlyph());
        content.Children.Add(new TextBlock
        {
            Text = L("StopScreenRecording"),
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = SnapvereBrand.Strong,
            VerticalAlignment = VerticalAlignment.Center
        });
        return content;
    }

    private static TextBlock CreateStopGlyph()
        => new()
        {
            Text = "■",
            FontFamily = new FontFamily("Segoe UI Symbol"),
            FontSize = 13,
            Foreground = Strong,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

    private string UserText(string english, string croatian)
        => string.Equals(_languageCode, "hr", StringComparison.OrdinalIgnoreCase)
            ? croatian
            : english;

    private void Timer_Tick(object? sender, object e)
        => _elapsedText.Text = FormatElapsed(_elapsed.Elapsed);

    internal static string FormatElapsed(TimeSpan elapsed)
    {
        var totalHours = (int)Math.Floor(Math.Max(0, elapsed.TotalHours));
        return totalHours > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{totalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}")
            : string.Create(CultureInfo.InvariantCulture, $"{(int)Math.Max(0, elapsed.TotalMinutes):00}:{elapsed.Seconds:00}");
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
        {
            return;
        }

        e.Handled = true;
        RequestStop();
    }

    private void RecordingControllerWindow_Activated(
        object sender,
        WindowActivatedEventArgs args)
    {
        if (_sizeApplied)
        {
            return;
        }

        _sizeApplied = true;
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSizeToWorkArea(this, 500, 82));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        AppWindow.Move(new Windows.Graphics.PointInt32(
            workArea.X + Math.Max(0, workArea.Width - AppWindow.Size.Width - 16),
            workArea.Y + 16));

        _ = _stopButton.Focus(FocusState.Programmatic);
    }

    private void RecordingControllerWindow_Closed(object sender, WindowEventArgs args)
    {
        StopTimer();
        if (!_closingFromOwner)
        {
            RequestStop(recoverUiOnFailure: false);
        }
    }

    private void StopTimer()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }

        _elapsed.Stop();
    }

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Surface => SnapvereBrand.Surface;
    private static SolidColorBrush Strong => SnapvereBrand.Strong;
    private static SolidColorBrush Muted => SnapvereBrand.Muted;
    private static SolidColorBrush Outline => SnapvereBrand.Outline;
    private static SolidColorBrush RecordingRed => SnapvereBrand.Danger;
}
