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
    RecordingSaved,
    RecordingFailed,
    RecordingUnsupported,
    OpenCaptureFolderFailed,
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
            (CaptureFeedbackKind.ShutdownBlocked, true) => "Priprema izlaska",
            (CaptureFeedbackKind.ShutdownBlocked, false) => "Preparing to exit",
            (CaptureFeedbackKind.RecordingSaved, _) => L("RecordingSavedTitle"),
            (CaptureFeedbackKind.RecordingFailed, _) => L("RecordingFailedTitle"),
            (CaptureFeedbackKind.RecordingUnsupported, _) => L("RecordingUnsupportedTitle"),
            (CaptureFeedbackKind.OpenCaptureFolderFailed, true) => "Mapa snimki nije dostupna",
            (CaptureFeedbackKind.OpenCaptureFolderFailed, false) => "Capture folder is unavailable",
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
                "SNAPVERE će sigurno dovršiti aktivno snimanje, po potrebi zaustaviti snimanje zaslona i automatski izaći nakon dovršetka čišćenja.",
            (CaptureFeedbackKind.ShutdownBlocked, false) =>
                "SNAPVERE will finish the active capture safely, stop screen recording when needed, and exit automatically after cleanup completes.",
            (CaptureFeedbackKind.RecordingSaved, _) => L("RecordingSavedMessage"),
            (CaptureFeedbackKind.RecordingFailed, _) => L("RecordingFailedMessage"),
            (CaptureFeedbackKind.RecordingUnsupported, _) => L("RecordingUnsupportedMessage"),
            (CaptureFeedbackKind.OpenCaptureFolderFailed, true) =>
                "SNAPVERE nije mogao otvoriti mapu snimki u Eksploreru datoteka. Postojeće snimke nisu mijenjane. Pokušaj ponovno; ako se problem ponavlja, provjeri pristup mapi Slike.",
            (CaptureFeedbackKind.OpenCaptureFolderFailed, false) =>
                "SNAPVERE could not open the capture folder in File Explorer. Existing captures were not changed. Try again; if the problem continues, check access to your Pictures folder.",
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
            Background = SnapvereBrand.AccentGradient(),
            BorderBrush = Brush(0x88, 0xA4, 0x8B, 0xFF),
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
        card.Child = new ScrollViewer
        {
            Content = message,
            VerticalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            IsTabStop = false
        };
        Grid.SetRow(card, 1);
        root.Children.Add(card);

        var close = new Button
        {
            Content = CloseText(),
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(18, 8, 18, 8),
            CornerRadius = new CornerRadius(10),
            Background = SnapvereBrand.AccentGradient(),
            BorderBrush = SnapvereBrand.Lavender,
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
        AppWindow.Resize(DpiAwareWindowSizing.ScaleSizeToWorkArea(this, 500, 300));
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

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(alpha, red, green, blue));

    private static SolidColorBrush Surface => SnapvereBrand.Obsidian;
    private static SolidColorBrush Elevated => SnapvereBrand.Surface;
    private static SolidColorBrush Outline => SnapvereBrand.Outline;
    private static SolidColorBrush Strong => SnapvereBrand.Strong;
    private static SolidColorBrush Muted => SnapvereBrand.Muted;
    private static SolidColorBrush Accent => SnapvereBrand.Lavender;
}
