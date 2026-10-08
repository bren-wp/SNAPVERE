using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.UI;

namespace Snapvere.App;

/// <summary>
/// Production branding tokens shared by every visible SNAPVERE Windows surface.
/// Values mirror the premium brand package so tray, capture chrome, settings,
/// recording controls and supporting windows cannot drift independently.
/// </summary>
public static class SnapvereBrand
{
    public const string ObsidianHex = "#070912";
    public const string SurfaceHex = "#111526";
    public const string SlateHex = "#161B2E";
    public const string VioletHex = "#7655F6";
    public const string LavenderHex = "#A48BFF";
    public const string IceHex = "#80E1E5";
    public const string StrongTextHex = "#F8F9FF";
    public const string MutedTextHex = "#8E9AB6";
    public const string ProductionMarkFile = "SNAPVERE-app-icon-48.png";

    public static SolidColorBrush Obsidian => Brush(0xFF, 0x07, 0x09, 0x12);
    public static SolidColorBrush Surface => Brush(0xFF, 0x11, 0x15, 0x26);
    public static SolidColorBrush Slate => Brush(0xFF, 0x16, 0x1B, 0x2E);
    public static SolidColorBrush SlateRaised => Brush(0xFF, 0x1B, 0x21, 0x38);
    public static SolidColorBrush Violet => Brush(0xFF, 0x76, 0x55, 0xF6);
    public static SolidColorBrush Lavender => Brush(0xFF, 0xA4, 0x8B, 0xFF);
    public static SolidColorBrush Ice => Brush(0xFF, 0x80, 0xE1, 0xE5);
    public static SolidColorBrush Strong => Brush(0xFF, 0xF8, 0xF9, 0xFF);
    public static SolidColorBrush Muted => Brush(0xFF, 0x8E, 0x9A, 0xB6);
    public static SolidColorBrush Subtle => Brush(0xFF, 0x6F, 0x7A, 0x95);
    public static SolidColorBrush Outline => Brush(0xFF, 0x34, 0x3C, 0x5B);
    public static SolidColorBrush OutlineSoft => Brush(0x88, 0x4B, 0x42, 0x7F);
    public static SolidColorBrush Success => Brush(0xFF, 0x72, 0xD8, 0xB4);
    public static SolidColorBrush Danger => Brush(0xFF, 0xF0, 0x63, 0x82);
    public static SolidColorBrush Transparent => Brush(0x00, 0, 0, 0);

    public static LinearGradientBrush AccentGradient()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };
        brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0x76, 0x55, 0xF6), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0x8B, 0x6D, 0xFF), Offset = 0.5 });
        brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0xA4, 0x8B, 0xFF), Offset = 1 });
        return brush;
    }

    public static FrameworkElement CreateMark(double size)
    {
        if (!double.IsFinite(size) || size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", ProductionMarkFile);
        var source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
        return new Image
        {
            Width = size,
            Height = size,
            Source = source,
            Stretch = Stretch.Uniform
        };
    }

    public static TextBlock CreateWordmark(double fontSize, bool includeTagline = false)
    {
        var wordmark = new TextBlock
        {
            FontSize = fontSize,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            CharacterSpacing = 24,
            VerticalAlignment = VerticalAlignment.Center
        };
        wordmark.Inlines.Add(new Run { Text = "SNAP", Foreground = Strong });
        wordmark.Inlines.Add(new Run { Text = "VERE", Foreground = Lavender });
        return wordmark;
    }

    public static Border Card(UIElement child, double radius = 16, Thickness? padding = null)
        => new()
        {
            Background = Surface,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(radius),
            Padding = padding ?? new Thickness(16),
            Child = child
        };

    public static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue)
        => new(Color.FromArgb(alpha, red, green, blue));
}
