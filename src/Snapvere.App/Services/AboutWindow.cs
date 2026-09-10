using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace Snapvere.App.Services;

public sealed class AboutWindow : Window
{
    private bool _sizeApplied;

    public AboutWindow()
    {
        Title = "About SNAPVERE";
        Content = BuildContent();
        Activated += AboutWindow_Activated;
    }

    private UIElement BuildContent()
    {
        var root = new StackPanel
        {
            RequestedTheme = ElementTheme.Dark,
            Background = Brush(0x0D, 0x12, 0x20),
            Padding = new Thickness(24),
            Spacing = 14
        };

        var brand = new StackPanel { Spacing = 3 };
        brand.Children.Add(Text("SNAPVERE", 24, Brush(0xF4, 0xF1, 0xFF), Microsoft.UI.Text.FontWeights.SemiBold));
        brand.Children.Add(Text("Capture. Edit. Done.", 13, Brush(0xA8, 0x9E, 0xFF)));
        root.Children.Add(brand);

        var version = typeof(AboutWindow).Assembly.GetName().Version?.ToString(3) ?? "dev";
        root.Children.Add(Text($"Version {version}", 12, Brush(0xC5, 0xCC, 0xD8)));

        var description = Text(
            "Fast local-first region, window and screen capture for Windows. Screenshots stay on your device unless you explicitly share them.",
            12,
            Brush(0xB4, 0xBE, 0xCE));
        description.TextWrapping = TextWrapping.Wrap;
        root.Children.Add(description);

        var details = Text(
            "Built by Brendigo · MPL-2.0\nPrint Screen: Region Capture\nCtrl + Shift + 2: Window Capture\nCtrl + Shift + 4: Screen Capture",
            11,
            Brush(0x8F, 0x9A, 0xAF));
        details.TextWrapping = TextWrapping.Wrap;
        root.Children.Add(details);

        var close = new Button
        {
            Content = "Close",
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(18, 8, 18, 8),
            Background = Brush(0x63, 0x4A, 0xD8),
            Foreground = Brush(0xFF, 0xFF, 0xFF)
        };
        close.Click += (_, _) => Close();
        root.Children.Add(close);
        return root;
    }

    private void AboutWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_sizeApplied)
        {
            return;
        }

        _sizeApplied = true;
        AppWindow.Resize(new SizeInt32(470, 360));
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

    private static SolidColorBrush Brush(byte red, byte green, byte blue)
        => new(Windows.UI.Color.FromArgb(0xFF, red, green, blue));
}
