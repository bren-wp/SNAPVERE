using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Snapvere.Shared;
using Windows.System;

namespace Snapvere.App.Services;

public enum CaptureFeedbackKind
{
    Busy,
    ShutdownBlocked,
    WindowUnsupported,
    RegionFailed,
    WindowFailed,
    ScreenFailed,
    SaveAccessDenied,
    StorageFull,
    SaveFailed
}

/// <summary>
/// Small, on-demand user-facing capture status surface. It deliberately shows
/// fixed product copy instead of exception details so operational diagnostics
/// stay in StartupDiagnostics rather than leaking implementation text into UX.
/// </summary>
public sealed class CaptureFeedbackWindow : Window
{
    private readonly string _languageCode;
    private readonly CaptureFeedbackKind _kind;
    private bool _sizeApplied;

    public CaptureFeedbackWindow(CaptureFeedbackKind kind, string? languageCode)
    {
        _kind = kind;
        _languageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
        Title = TitleText();
        Content = BuildContent();
        Activated += CaptureFeedbackWindow_Activated;
    }

    private bool IsCroatian
        => _languageCode.StartsWith("hr", StringComparison.OrdinalIgnoreCase);

    private string TitleText()
        => (_kind, IsCroatian) switch
        {
            (CaptureFeedbackKind.Busy, true) => "Snimanje je već aktivno",
            (CaptureFeedbackKind.Busy, false) => "Capture already active",
            (CaptureFeedbackKind.ShutdownBlocked, true) => "Snimanje je još aktivno",
            (CaptureFeedbackKind.ShutdownBlocked, false) => "Capture is still active",
            (CaptureFeedbackKind.WindowUnsupported, true) => "Snimanje prozora nije dostupno",
            (CaptureFeedbackKind.WindowUnsupported, false) => "Window capture is unavailable",
            (CaptureFeedbackKind.SaveAccessDenied or CaptureFeedbackKind.StorageFull or CaptureFeedbackKind.SaveFailed, _) =>
                L("CaptureSaveFailedTitle"),
            (_, true) => "Snimanje nije dovršeno",
            _ => "Capture could not be completed"
        };

    private string MessageText()
        => (_kind, IsCroatian) switch
        {
            (CaptureFeedbackKind.Busy, true) =>
                "Završi ili odustani od trenutačnog snimanja prije pokretanja novog.",
            (CaptureFeedbackKind.Busy, false) =>
                "Finish or cancel the current capture before starting another one.",
            (CaptureFeedbackKind.ShutdownBlocked, true) =>
                "Završi ili odustani od trenutačnog snimanja prije izlaska iz SNAPVERE-a.",
            (CaptureFeedbackKind.ShutdownBlocked, false) =>
                "Finish or cancel the current capture before exiting SNAPVERE.",
            (CaptureFeedbackKind.WindowUnsupported, true) =>
                "Ova verzija sustava Windows ne podržava SNAPVERE snimanje pojedinačnog prozora. Snimanje područja i zaslona i dalje je dostupno.",
            (CaptureFeedbackKind.WindowUnsupported, false) =>
                "This Windows version does not support SNAPVERE single-window capture. Region and screen capture are still available.",
            (CaptureFeedbackKind.RegionFailed, true) =>
                "SNAPVERE nije uspio dovršiti snimanje područja. Pokušaj ponovno; ako se problem ponovi, provjeri dozvole za mapu Slike i međuspremnik.",
            (CaptureFeedbackKind.RegionFailed, false) =>
                "SNAPVERE could not complete the region capture. Try again; if it keeps failing, check access to Pictures and the clipboard.",
            (CaptureFeedbackKind.WindowFailed, true) =>
                "SNAPVERE nije uspio dovršiti snimanje odabranog prozora. Provjeri je li prozor još otvoren i pokušaj ponovno.",
            (CaptureFeedbackKind.WindowFailed, false) =>
                "SNAPVERE could not complete the selected window capture. Make sure the window is still open and try again.",
            (CaptureFeedbackKind.ScreenFailed, true) =>
                "SNAPVERE nije uspio dovršiti snimanje zaslona. Pokušaj ponovno; ako se problem ponovi, zatvori zaštićeni ili full-screen sadržaj.",
            (CaptureFeedbackKind.ScreenFailed, false) =>
                "SNAPVERE could not complete the screen capture. Try again; if it keeps failing, close protected or full-screen content.",
            (CaptureFeedbackKind.SaveAccessDenied, _) => L("CaptureSaveAccessDenied"),
            (CaptureFeedbackKind.StorageFull, _) => L("CaptureStorageFull"),
            (CaptureFeedbackKind.SaveFailed, _) => L("CaptureSaveFailed"),
            _ =>
                "SNAPVERE could not complete the capture. Try again."
        };

    private string L(string key)
        => SnapvereLocalization.T(key, _languageCode);

    private string CloseText() => IsCroatian ? "Zatvori" : "Close";

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Surface,
            Padding = new Thickness(22)
        };
        root.KeyDown += Root_KeyDown;
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.Children.Add(new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(13),
            Background = AccentGradient(),
            BorderBrush = Brush(0x70, 0xC9, 0xC0, 0xFF),
            BorderThickness = new Thickness(1),
            Child = new FontIcon
            {
                Glyph = _kind == CaptureFeedbackKind.Busy ? "\uE823" : "\uE7BA",
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 17,
                Foreground = Strong
            }
        });
        var identity = new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        identity.Children.Add(Text("SNAPVERE", 10, Accent, Microsoft.UI.Text.FontWeights.Bold));
        var title = Text(TitleText(), 18, Strong, Microsoft.UI.Text.FontWeights.SemiBold);
        title.TextWrapping = TextWrapping.Wrap;
        identity.Children.Add(title);
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);
        root.Children.Add(header);

        var card = new Border
        {
            Margin = new Thickness(0, 18, 0, 16),
            Padding = new Thickness(15),
            CornerRadius = new CornerRadius(14),
            Background = Elevated,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1)
        };
        var message = Text(MessageText(), 11, Muted);
        message.TextWrapping = TextWrapping.Wrap;
        card.Child = message;
        Grid.SetRow(card, 1);
        root.Children.Add(card);

        var close = new Button
        {
            Content = CloseText(),
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(18, 8, 18, 8),
            CornerRadius = new CornerRadius(10),
            Background = Brush(0xFF, 0x39, 0x28, 0x78),
            BorderBrush = Brush(0xFF, 0x86, 0x67, 0xF4),
            BorderThickness = new Thickness(1),
            Foreground = Strong
        };
        AutomationProperties.SetName(close, CloseText());
        close.Click += (_, _) => Close();
        close.Loaded += (_, _) => _ = close.Focus(FocusState.Programmatic);
        Grid.SetRow(close, 2);
        root.Children.Add(close);

        return root;
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
        {
            return;
        }

        e.Handled = true;
        Close();
    }

    private void CaptureFeedbackWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_sizeApplied)
        {
            return;
        }

        _sizeApplied = true;
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSize(this, 500, 300));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
        }
    }

    private static TextBlock Text(
        string value,
        double size,
        SolidColorBrush foreground,
        Windows.UI.Text.FontWeight? weight = null)
        => new()
        {
            Text = value,
            FontSize = size,
            Foreground = foreground,
            FontWeight = weight ?? Microsoft.UI.Text.FontWeights.Normal
        };

    private static LinearGradientBrush AccentGradient()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1)
        };
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x61, 0x4A, 0xE8), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0x37, 0xB6, 0xD4), Offset = 1 });
        return brush;
    }

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Surface => Brush(0xFF, 0x0D, 0x13, 0x21);
    private static SolidColorBrush Elevated => Brush(0xFF, 0x12, 0x17, 0x24);
    private static SolidColorBrush Outline => Brush(0xFF, 0x52, 0x61, 0x7F);
    private static SolidColorBrush Strong => Brush(0xFF, 0xF7, 0xF5, 0xFF);
    private static SolidColorBrush Muted => Brush(0xFF, 0xAF, 0xB6, 0xC8);
    private static SolidColorBrush Accent => Brush(0xFF, 0xA7, 0x7C, 0xFF);
}
