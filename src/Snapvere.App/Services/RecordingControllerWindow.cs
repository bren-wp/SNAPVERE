using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snapvere.Shared;
using System.Diagnostics;
using System.Globalization;

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
        (_elapsedText, _stopButton, Content) = BuildContent();

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

    private (TextBlock Elapsed, Button Stop, FrameworkElement Root) BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Surface,
            Padding = new Thickness(18)
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var indicator = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(18),
            Background = Brush(0x26, 0xEC, 0x5F, 0x74),
            BorderBrush = Brush(0x90, 0xEC, 0x5F, 0x74),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = RecordingRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        AutomationProperties.SetName(indicator, L("ScreenRecordingActive"));
        root.Children.Add(indicator);

        var status = new StackPanel
        {
            Spacing = 3,
            Margin = new Thickness(12, 0, 18, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        status.Children.Add(new TextBlock
        {
            Text = L("ScreenRecordingActive"),
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Strong
        });
        var elapsed = new TextBlock
        {
            Text = "00:00",
            FontSize = 20,
            FontFamily = new FontFamily("Consolas"),
            Foreground = Muted
        };
        AutomationProperties.SetName(elapsed, "Recording elapsed time");
        status.Children.Add(elapsed);
        Grid.SetColumn(status, 1);
        root.Children.Add(status);

        var stop = new Button
        {
            Content = L("StopScreenRecording"),
            MinWidth = 150,
            MinHeight = 42,
            Padding = new Thickness(16, 8, 16, 8),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0xFF, 0x9D, 0x2F, 0x48),
            BorderBrush = Brush(0xFF, 0xEC, 0x5F, 0x74),
            BorderThickness = new Thickness(1),
            Foreground = Strong,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(stop, L("StopScreenRecording"));
        stop.Click += (_, _) => RequestStop();
        Grid.SetColumn(stop, 2);
        root.Children.Add(stop);

        return (elapsed, stop, root);
    }

    private void RequestStop()
    {
        if (_stopRequested)
        {
            return;
        }

        _stopRequested = true;
        _stopButton.IsEnabled = false;
        StopTimer();
        _stopAction();
    }

    private void Timer_Tick(object? sender, object e)
        => _elapsedText.Text = FormatElapsed(_elapsed.Elapsed);

    internal static string FormatElapsed(TimeSpan elapsed)
    {
        var totalHours = (int)Math.Floor(Math.Max(0, elapsed.TotalHours));
        return totalHours > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{totalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}")
            : string.Create(CultureInfo.InvariantCulture, $"{(int)Math.Max(0, elapsed.TotalMinutes):00}:{elapsed.Seconds:00}");
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
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSizeToWorkArea(this, 520, 112));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }
    }

    private void RecordingControllerWindow_Closed(object sender, WindowEventArgs args)
    {
        StopTimer();
        if (!_closingFromOwner)
        {
            RequestStop();
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

    private static SolidColorBrush Surface => Brush(0xFF, 0x0D, 0x13, 0x21);
    private static SolidColorBrush Strong => Brush(0xFF, 0xF7, 0xF5, 0xFF);
    private static SolidColorBrush Muted => Brush(0xFF, 0xAF, 0xB6, 0xC8);
    private static SolidColorBrush RecordingRed => Brush(0xFF, 0xEC, 0x5F, 0x74);
}
